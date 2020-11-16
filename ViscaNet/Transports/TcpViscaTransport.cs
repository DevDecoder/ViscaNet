// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Buffers;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using DevDecoder.ViscaNet.Commands;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace DevDecoder.ViscaNet.Transports
{
    public sealed class TcpViscaTransport : IViscaTransport
    {
        private readonly ILogger? _logger;
        private byte[]? _buffer;
        private BehaviorSubject<bool>? _connectionState;
        private SemaphoreSlim? _semaphore;
        private NetworkStream? _stream;
        private TcpClient? _tcpClient;

        public TcpViscaTransport(
            IPEndPoint endPoint,
            byte deviceId = 1,
            uint maxTimeout = 20000,
            ushort connectionTimeout = 5000,
            ILogger? logger = null)
        {
            if (maxTimeout < 100 || maxTimeout > 86400000)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTimeout), maxTimeout,
                    "The maximum timeout must be > 100ms and <= 1 day.");
            }

            if (connectionTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionTimeout), connectionTimeout,
                    "The maximum timeout must be > 100ms.");
            }

            if (connectionTimeout > maxTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionTimeout), connectionTimeout,
                    "The connection timeout must be <= the maximum timeout.");
            }

            _logger = logger;
            EndPoint = endPoint;
            DeviceId = deviceId;
            MaxTimeout = maxTimeout;
            ConnectionTimeout = connectionTimeout;
            _connectionState = new BehaviorSubject<bool>(false);
            // Only allow one command at a time
            _semaphore = new SemaphoreSlim(1);
            _buffer = ArrayPool<byte>.Shared.Rent(16);
        }

        public IPEndPoint EndPoint { get; }
        public byte DeviceId { get; }
        public uint MaxTimeout { get; }
        public ushort ConnectionTimeout { get; }

        /// <inheritdoc />
        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            var semaphore = _semaphore ??
                            throw new ObjectDisposedException(nameof(TcpViscaTransport));

            // Create a combined token that combines the supplied token with the connection timeout.
            using var cts = new CancellationTokenSource(ConnectionTimeout);
            using var cct = cancellationToken.CombineWith(cts.Token);
            cancellationToken = cct.Token;

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                var tcpClient = _tcpClient;
                var stream = _stream;
                try
                {
                    if (tcpClient != null && stream != null && tcpClient.Connected)
                        // Already connected!
                    {
                        return true;
                    }

                    _connectionState?.OnNext(false);

                    stream = Interlocked.Exchange(ref _stream, null);
                    if (stream != null)
                    {
                        await stream.DisposeAsync().ConfigureAwait(false);
                    }

                    Interlocked.Exchange(ref _tcpClient, null)?.Dispose();

                    tcpClient = new TcpClient {ReceiveTimeout = (int)MaxTimeout, SendTimeout = (int)MaxTimeout};

                    // Try to connect, respecting Connection Timeout
                    await tcpClient.ConnectAsync(EndPoint.Address, EndPoint.Port)
                        .WithCancellation(cancellationToken)
                        .ConfigureAwait(false);

                    if (cancellationToken.IsCancellationRequested ||
                        !tcpClient.Connected)
                    {
                        return false;
                    }

                    _stream = tcpClient.GetStream();
                    _tcpClient = tcpClient;

                    _logger?.LogInformation($"Established TCP connection to {EndPoint}.");

                    // Send IFClear to camera
                    var response = await DoSendAsync(ViscaCommands.IFClear, cancellationToken)
                        .ConfigureAwait(false);

                    if (response.Type != ResponseType.Unknown)
                    {
                        // Ideally we want a Completion, but any kind of known response means there is at least a camera responding.
                        if (response.Type != ResponseType.Completion)
                        {
                            _logger?.LogWarning(
                                $"Received '{response.Type}' response to '{ViscaCommands.IFClear.Name}' from {EndPoint} instead of '{nameof(ResponseType.Completion)}', ignoring.");
                        }

                        // Signal connection
                        _connectionState?.OnNext(true);
                        return true;
                    }

                    _logger?.LogError($"No valid response from {EndPoint}.");
                }
                catch (Exception exception)
                {
                    _logger?.LogError(exception, $"Failed to establish TCP connection to {EndPoint}.");
                }

                // Ensure we dispose any connection
                if (stream != null)
                {
                    Interlocked.CompareExchange(ref _stream, null, stream);
                    await stream.DisposeAsync().ConfigureAwait(false);
                }

                if (tcpClient != null)
                {
                    Interlocked.CompareExchange(ref _tcpClient, null, tcpClient);
                    tcpClient.Dispose();
                }

                return false;
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task<Response> SendAsync(Command command,
            CancellationToken cancellationToken = default)
        {
            var semaphore = _semaphore ??
                            throw new ObjectDisposedException(nameof(TcpViscaTransport));

            // Create a combined token that combines the supplied token with the maximum timeout.
            using var cts = new CancellationTokenSource((int)MaxTimeout);
            using var cct = cancellationToken.CombineWith(cts.Token);
            cancellationToken = cct.Token;

            // Try to connect if no connection available.
            if ((_tcpClient is null || _stream is null || !_tcpClient.Connected) &&
                !await ConnectAsync(cancellationToken))
            {
                _logger.LogError(
                    $"Could not send '{command.Name}' command to '{EndPoint}', as a connection could not be established.");
                return command.UnknownResponse;
            }

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return await DoSendAsync(command, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        }

        /// <inheritdoc />
        public IObservable<bool> ConnectionState =>
            _connectionState ?? throw new ObjectDisposedException(nameof(CameraConnection));

        /// <inheritdoc />
        public bool IsConnected => _connectionState?.Value
                                   ?? throw new ObjectDisposedException(nameof(CameraConnection));

        /// <inheritdoc />
        public void Dispose()
        {
            var semaphore = Interlocked.Exchange(ref _semaphore, null);
            if (semaphore != null)
            {
                try
                {
                    // Try to give pending operations a chance to complete
                    semaphore.Wait(100);
                }
                finally
                {
                    semaphore.Dispose();
                }
            }

            Interlocked.Exchange(ref _stream, null)?.Dispose();
            Interlocked.Exchange(ref _tcpClient, null)?.Dispose();

            var buffer = Interlocked.Exchange(ref _buffer, null);
            if (buffer != null)
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            var connectionState = Interlocked.Exchange(ref _connectionState, null);
            if (connectionState == null)
            {
                return;
            }

            connectionState.OnCompleted();
            connectionState.Dispose();
        }

        private async Task<Response> DoSendAsync(Command command, CancellationToken cancellationToken)
        {
            try
            {
                var stream = _stream ?? throw new ObjectDisposedException(nameof(TcpViscaTransport));
                var buffer = _buffer ?? throw new ObjectDisposedException(nameof(TcpViscaTransport));

                var messageSize = command.MessageSize;
                var bufferLength = buffer.Length;
                if (messageSize > bufferLength)
                {
                    var currentBuffer = Interlocked.CompareExchange(ref _buffer, null!, buffer);

                    // Already disposed
                    if (currentBuffer is null)
                    {
                        throw new ObjectDisposedException(nameof(TcpViscaTransport));
                    }

                    // Return buffer
                    ArrayPool<byte>.Shared.Return(buffer);

                    // Get a bigger buffer!
                    buffer = ArrayPool<byte>.Shared.Rent(messageSize);
                    Interlocked.Exchange(ref _buffer, buffer);

                    _logger.LogWarning(
                        $"The '{command.Name} command requested a message size of '{messageSize}' which exceeded the current buffer's size '{bufferLength}', so a new buffer of size '{buffer.Length}' was created.  This is unexpected and exceeds the current VISCA specification so may cause problems on some devices!");
                }

                // Write the message into our current buffer.
                command.WriteMessage(buffer.AsSpan(0, messageSize), DeviceId);
                _logger?.LogDebug(
                    $"Sending '{command.Name}' data to '{EndPoint}': {buffer.Take(messageSize).ToHex()}");
                await stream.WriteAsync(buffer, 0, messageSize, cancellationToken)
                    .ConfigureAwait(false);

                int read;
                Response response;
                var socket = -1;
                if (command.Type == CommandType.Command)
                {
                    // We expect an ACK
                    read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                    if (read < 1)
                    {
                        _logger?.LogError($"No data returned from '{EndPoint}' whilst executing '{command.Name}'.");
                        return command.UnknownResponse;
                    }

                    response = command.GetResponse(buffer.AsSpan(0, read), _logger);
                    _logger?.LogDebug(
                        $"Received '{response.Type}' response to '{command.Name}' from '{EndPoint}': {buffer.Take(read).ToHex()}");
                    if (response.Type != ResponseType.ACK)
                    {
                        // The IFClear returns a completion without an ACK (as per spec.)
                        if (command != ViscaCommands.IFClear && response.Type != ResponseType.Completion)
                        {
                            _logger?.LogWarning(
                                $"Received a '{response.Type}' response from '{EndPoint}' whilst executing '{command.Name}' instead of an '{nameof(ResponseType.ACK)}' response.");
                        }

                        return response;
                    }

                    if (response.DeviceId != DeviceId)
                    {
                    }

                    socket = response.Socket;
                    // Continue to wait for completion response
                }

                read = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (read < 1)
                {
                    _logger?.LogError($"No data returned from '{EndPoint}' whilst executing '{command.Name}'.");
                    return command.UnknownResponse;
                }

                response = command.GetResponse(buffer.AsSpan(0, read), _logger);
                _logger?.LogDebug(
                    $"Received '{response.Type}' response to '{command.Name}' from '{EndPoint}': {buffer.Take(read).ToHex()}");

                if (response.DeviceId != DeviceId)
                {
                    _logger?.LogWarning(
                        $"The device Id '{response.DeviceId}' in the '{response.Type}' response from '{EndPoint}' whilst executing '{command.Name}' did not match the expected device Id '{DeviceId}'.");
                }

                if (socket > -1 && response.Socket != socket)
                {
                    _logger?.LogWarning(
                        $"The socket '{response.Socket}' in the '{response.Type}' response from '{EndPoint}' whilst executing '{command.Name}' did not match the socket '{socket}' returned from the '{nameof(ResponseType.ACK)}' response.");
                }

                return response;
            }
            catch (Exception exception)
            {
                _logger?.LogError(exception, $"Failed to send '{command.Name}' to '{EndPoint}'.");
                return command.UnknownResponse;
            }
        }

        /// <inheritdoc />
        public override string ToString() => EndPoint.ToString();
    }
}
