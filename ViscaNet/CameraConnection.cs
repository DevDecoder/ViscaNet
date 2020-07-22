// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace ViscaNet
{
    public sealed class CameraConnection : IDisposable
    {
        private readonly ConcurrentQueue<Command> _commandQueue;
        private readonly int _connectionTimeout;

        private readonly ILogger<CameraConnection>? _logger;
        private readonly int _maxTimeout;
        private readonly ManualResetEvent? _newCommandReceivedEvent;
        private readonly int _retryTimeout;
        private CancellationTokenSource? _cancellationTokenSource;

        private BehaviorSubject<bool>? _connectionState;
        private NetworkStream? _stream;

        public CameraConnection(
            IPEndPoint endPoint,
            string? name = null,
            ushort maxTimeout = 20000,
            ushort retryTimeout = 1000,
            ushort connectionTimeout = 5000,
            ILogger<CameraConnection>? logger = null)
        {
            EndPoint = endPoint;
            Name = name ?? endPoint.ToString();

            // Sanity check on timeouts, ushort.MaxValue is ~65s which is a reasonable maximum anyway.
            if (maxTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTimeout), maxTimeout,
                    "The maximum timeout must be > 10ms.");
            }

            if (retryTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), maxTimeout,
                    "The maximum timeout must be > 10ms.");
            }

            if (connectionTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(connectionTimeout), maxTimeout,
                    "The maximum timeout must be > 10ms.");
            }

            _maxTimeout = maxTimeout;
            _retryTimeout = retryTimeout;
            _connectionTimeout = connectionTimeout;
            _logger = logger;
            _connectionState = new BehaviorSubject<bool>(false);
            _newCommandReceivedEvent = new ManualResetEvent(false);
            _commandQueue = new ConcurrentQueue<Command>();
            _cancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _cancellationTokenSource.Token;
            Task.Run(() => ProcessCommandQueueAsync(cancellationToken), cancellationToken)
                .ConfigureAwait(false);
        }

        public IPEndPoint EndPoint { get; }

        public string Name { get; }

        public IObservable<bool> ConnectionState =>
            _connectionState ?? throw new ObjectDisposedException(nameof(CameraConnection));

        public bool IsConnected => ConnectionState.FirstOrDefaultAsync().Wait();

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.Exchange(ref _stream, null)?.Dispose();

            var cts = Interlocked.Exchange(ref _cancellationTokenSource, null);
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
            }

            var connectionState = Interlocked.Exchange(ref _connectionState, null);
            if (connectionState != null)
            {
                connectionState.OnCompleted();
                connectionState.Dispose();
            }

            foreach (var command in _commandQueue)
            {
                command.TryCancel();
            }
        }

        public Task HomeAsync(CancellationToken cancellationToken = default) => SendAsync(HomeBytes, cancellationToken);

        public Task ResetAsync(CancellationToken cancellationToken = default) => SendAsync(ResetBytes, cancellationToken);

        public async Task CancelAsync(CancellationToken cancellationToken = default)
        {
            var commands = _commandQueue ?? throw new ObjectDisposedException(nameof(CameraConnection));
            var cleared = 0;
            while (!commands.IsEmpty)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (commands.TryDequeue(out var command))
                {
                    cleared++;
                    command.TryCancel();
                }
            }

            _logger?.LogInformation($"Cleared {cleared} queued commands.");

            try
            {
                await SendAsync(CancelBytes, cancellationToken);
            }
            catch (Exception exception)
            {
                // Suppress cancellation errors.
                _logger?.LogError(exception, $"Ignoring error when cancelling commands - {exception.Message}.");
            }
        }

        public Task<PowerMode> PowerInquiryAsync(CancellationToken cancellationToken = default)
            => SendAsync(InqPowerBytes, data => data[2] switch
            {
                // Format of response if y0 50 0? FF 
                0x02 => PowerMode.On,
                0x03 => PowerMode.Standby,
                _ => PowerMode.Unknown
            }, cancellationToken);

        public Task<double> ZoomInquiryAsync(CancellationToken cancellationToken = default)
            => SendAsync(InqZoomBytes, data =>
            {
                // Format of data is y0 50 0p 0q 0r 0s ff where pqrs goes from 0x0000 - 0x4000
                if (data.Length != 7)
                {
                    _logger.LogError($"Invalid zoom position received from {Name}, length '{data.Length}' was not 7: {data.ToHex()}.");
                    return double.NaN;
                }
                var b1 = data[2];
                var b2 = data[3];
                var b3 = data[4];
                var b4 = data[5];
                // Ensure MSBs are not set
                if (((b1 & 0xf0) + (b2 & 0xf0) + (b3 & 0xf0) + (b4 & 0xf0)) != 0)
                {
                    _logger.LogError($"Invalid zoom position received from {Name}, data received in MSBs: {data.ToHex()}.");
                    return double.NaN;
                }
                
                // Combine LSBs into single ushort (note no need to mask with 0x0f as above check has ensured MSBs are 0)
                var raw = (ushort)((b1 << 12) +
                                   (b2 << 8) +
                                   (b3 << 4) +
                                   b4);

                if (raw > 0x4000)
                {
                    _logger.LogError(
                        $"Invalid zoom position received from {Name}, {raw:x4} > 0x4000: {data.ToHex()}.");
                    return double.NaN;
                }

                // Convert to value between 0 and 1.
                return raw / 16384D;
            }, cancellationToken);

        private Task SendAsync(byte[] data, CancellationToken cancellationToken = default)
            => SendAsync<Unit>(data, null, cancellationToken);

        private Task<T> SendAsync<T>(byte[] data, Func<byte[], T>? parseMessageFunction,
            CancellationToken cancellationToken = default)
        {
            var commandQueue = _commandQueue ?? throw new ObjectDisposedException(nameof(CameraConnection));
            var command = new Command<T>(data, _maxTimeout, parseMessageFunction, cancellationToken);
            commandQueue.Enqueue(command);

            _newCommandReceivedEvent?.Set();
            return command.Task;
        }

        private async Task ProcessCommandQueueAsync(CancellationToken cancellationToken)
        {
            var newCommandReceivedEvent = _newCommandReceivedEvent;
            if (newCommandReceivedEvent is null)
            {
                return;
            }

            TcpClient? tcpClient = null;
            NetworkStream? stream = null;
            IAsyncEnumerator<(ViscaResponseType, byte[])>? messages = null;
            var resetConnection = true;
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (resetConnection || tcpClient?.Connected != true)
                    {
                        try
                        {
                            if (messages != null)
                            {
                                await messages.DisposeAsync();
                                messages = null;
                                resetConnection = false;
                            }

                            if (stream != null)
                            {
                                await stream.DisposeAsync();
                                stream = null;
                                resetConnection = false;
                            }

                            if (tcpClient != null)
                            {
                                tcpClient.Dispose();
                                tcpClient = null;
                                resetConnection = false;
                            }

                            if (!resetConnection)
                            {
                                _connectionState?.OnNext(false);

                                _logger?.LogInformation(
                                    $"Reset stream for {Name}, attempting reconnect in {_retryTimeout / 1000D:F3}s.");
                                // Don't immediately re-try a connection on reset.
                                await Task.Delay(_retryTimeout, cancellationToken).ConfigureAwait(false);
                            }
                            else
                            {
                                resetConnection = false;
                            }
                        }
                        catch (Exception exception)
                        {
                            _logger?.LogError(exception, $"Failed to reset stream for {Name}.");
                            resetConnection = true;
                        }
                    }

                    if (tcpClient is null)
                    {
                        try
                        {
                            // Open a stream and connect
                            tcpClient = new TcpClient();

                            // Try to connect, respecting Connection Timeout
                            using (var cts = new CancellationTokenSource(_connectionTimeout))
                            {
                                using (var cct = cancellationToken.CombineWith(cts.Token))
                                {
                                    await tcpClient.ConnectAsync(EndPoint.Address, EndPoint.Port)
                                        .WithCancellation(cct.Token)
                                        .ConfigureAwait(false);
                                }
                            }

                            stream = tcpClient.GetStream();

                            // Open a read stream
                            messages = ReadMessagesAsync(stream, cancellationToken)
                                .GetAsyncEnumerator(cancellationToken);

                            if (tcpClient.Connected)
                            {
                                _logger?.LogInformation($"Connected to {Name}.");
                                _connectionState?.OnNext(true);
                            }
                            else
                            {
                                _logger?.LogError($"Failed to connect to {Name}.");
                                resetConnection = true;
                            }
                        }
                        catch (Exception exception)
                        {
                            if (stream != null)
                            {
                                await stream.DisposeAsync();
                                stream = null;
                            }

                            _logger?.LogError(exception, $"Failed to connect to {Name}.");
                            resetConnection = true;
                        }
                    }

                    // Sanity check, ensure we're fully connected
                    if (stream is null || messages is null || tcpClient?.Connected != true)
                    {
                        resetConnection = true;
                        continue;
                    }

                    // Wait for new commands
                    await newCommandReceivedEvent.ToTask(-1, cancellationToken)
                        .ConfigureAwait(false);

                    // Check command is still valid
                    if (!_commandQueue.TryDequeue(out var command) ||
                        command.Status != TaskStatus.WaitingForActivation ||
                        command.CancellationToken.IsCancellationRequested)
                    {
                        // For safety, ensure there is at least a cancellation result
                        command?.TryCancel();
                        continue;
                    }

                    try
                    {
                        _logger?.LogDebug(
                            $"Sending to {Name}: {command.Message.ToArray().ToHex()}");

                        // Send message
                        await stream.WriteAsync(command.Message, cancellationToken).ConfigureAwait(false);

                        // Get response and parse
                        if (!await messages.MoveNextAsync())
                        {
                            // Stream closed
                            _logger?.LogInformation($"Camera stream closed for {Name}.");
                            resetConnection = true;
                            continue;
                        }

                        // Get response
                        var (type, message) = messages.Current;
                        switch (type)
                        {
                            case ViscaResponseType.ACK:
                                // Expected
                                break;
                            case ViscaResponseType.InquiryResponse:
                                if (!command.IsInquiry)
                                {
                                    goto default;
                                }
                                command.TrySetResult(message);
                                continue;
                            case ViscaResponseType.Completion:
                                // Mark message as completed
                                command.TrySetResult(message);
                                continue;
                            case ViscaResponseType.CommandCanceled:
                                command.TryCancel();
                                continue;
                            default:
                                var exception = new ViscaException(type, Name);
                                _logger?.LogError(exception, exception.Message);
                                command.TrySetException(exception);

                                // The command not executable does not indicate a serious issue, so we don't reset the connection for it.
                                if (type != ViscaResponseType.CommandNotExecutable)
                                {
                                    resetConnection = true;
                                }

                                continue;
                        }

                        // We received an ACK, so we need to wait for a completion.
                        if (!await messages.MoveNextAsync())
                        {
                            // Stream closed
                            _logger?.LogInformation($"Camera stream closed for {Name}.");
                            resetConnection = true;
                            continue;
                        }

                        // Get Completion
                        (type, message) = messages.Current;
                        switch (type)
                        {
                            case ViscaResponseType.Completion:
                                // Mark message as completed
                                command.TrySetResult(message);
                                continue;
                            case ViscaResponseType.CommandCanceled:
                                command.TryCancel();
                                continue;
                            default:
                                // Anything but a completion is unexpected.
                                var exception = new ViscaException(type, Name);
                                _logger?.LogError(exception, exception.Message);
                                command.TrySetException(exception);

                                // The command not executable does not indicate a serious issue, so we don't reset the connection for it.
                                if (type != ViscaResponseType.CommandNotExecutable)
                                {
                                    resetConnection = true;
                                }

                                continue;
                        }
                    }
                    catch (Exception exception)
                    {
                        // Ensure the command has a 'result'
                        command.TrySetException(exception);

                        // Close the stream as we can't trust it anymore
                        resetConnection = true;
                    }
                }
            }
            finally
            {
                if (messages != null)
                {
                    await messages.DisposeAsync();
                }

                if (stream != null)
                {
                    await stream.DisposeAsync();
                }

                tcpClient?.Dispose();
            }
        }

        private async IAsyncEnumerable<(ViscaResponseType, byte[])> ReadMessagesAsync(Stream stream,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            // Note a single payload has a maximum size of 16 bytes, so this buffer should never fill.
            var buffer = new byte[64];
            List<byte> result = new List<byte>(64);
            do
            {
                var read = await stream.ReadAsync(buffer, token).ConfigureAwait(false);
                if (read < 1)
                {
                    yield break;
                }

                for (var i = 0; i < read; i++)
                {
                    var next = buffer[i];
                    result.Add(next);
                    if (next != 0xFF)
                    {
                        continue;
                    }

                    // We have reached the end of a response message
                    var message = result.ToArray();
                    result.Clear();
                    var type = ViscaResponseType.Unknown;
                    var l = message.Length;
                    if (l > 2 && (message[0] & 0xf0) == 0x90)
                    {
                        // Last 4 bits = Socket (but only 0-2 is usually supported so)
                        switch (message[1] & 0xf0)
                        {
                            case 0x40:
                                if (l == 3)
                                {
                                    type = ViscaResponseType.ACK;
                                }

                                break;
                            case 0x50:
                                type = l == 3
                                    ? ViscaResponseType.Completion
                                    : ViscaResponseType.InquiryResponse;
                                break;
                            case 0x60:
                                type = l != 4
                                    ? ViscaResponseType.Unknown
                                    : message[2] switch
                                    {
                                        0x01 => ViscaResponseType.MessageLengthError,
                                        0x02 => ViscaResponseType.SyntaxError,
                                        0x03 => ViscaResponseType.CommandBufferFull,
                                        0x04 => ViscaResponseType.CommandCanceled,
                                        0x05 => ViscaResponseType.NoSocket,
                                        0x41 => ViscaResponseType.CommandNotExecutable,
                                        _ => ViscaResponseType.Unknown
                                    };
                                break;
                        }
                    }

                    _logger?.LogDebug(
                        $"Received {type} from {Name}: {message.ToHex()}");
                    yield return (type, message);
                }
            } while (true);
        }

        private abstract class Command
        {
            public readonly CancellationToken CancellationToken;
            protected readonly CancellationTokenExtensions.CombinedCancellationToken CombinedCancellationToken;
            public readonly ReadOnlyMemory<byte> Message;
            private CancellationTokenSource? _cancellationTokenSource;

            protected Command(ReadOnlyMemory<byte> message, int maxTimeout,
                CancellationToken cancellationToken = default)
            {
                Message = message;
                CancellationToken = cancellationToken;
                _cancellationTokenSource = new CancellationTokenSource(maxTimeout);
                CombinedCancellationToken = cancellationToken.CombineWith(_cancellationTokenSource.Token);
            }

            public abstract TaskStatus Status { get; }
            public abstract bool IsInquiry { get; }

            public abstract void TrySetResult(byte[] message);

            public abstract void TrySetException(Exception exception);

            public abstract void TryCancel();

            protected void Dispose()
            {
                Interlocked.Exchange(ref _cancellationTokenSource, null)?.Dispose();
                CombinedCancellationToken.Dispose();
            }
        }

        private class Command<T> : Command
        {
            private readonly Func<byte[], T>? _parseMessageFunction;
            private readonly TaskCompletionSource<T> _taskCompletionSource = new TaskCompletionSource<T>();

            public Command(
                ReadOnlyMemory<byte> message,
                int maxTimeout,
                Func<byte[], T>? parseMessageFunction = null,
                CancellationToken cancellationToken = default)
                : base(message, maxTimeout, cancellationToken)
            {
                _parseMessageFunction = parseMessageFunction;
                CombinedCancellationToken.Token.Register(() => _taskCompletionSource.TrySetCanceled(cancellationToken));
            }

            public Task<T> Task => _taskCompletionSource.Task;
            public override TaskStatus Status => _taskCompletionSource.Task.Status;
            public override bool IsInquiry => _parseMessageFunction != null;

            public override void TrySetResult(byte[] message)
            {
                T result;
                if (_parseMessageFunction != null)
                {
                    result = _parseMessageFunction(message);
                }
                else if (typeof(T).IsAssignableFrom(typeof(byte[])))
                {
                    result = (T)(object)message;
                }
                else
                {
                    result = default;
                }

                _taskCompletionSource.TrySetResult(result!);
                Dispose();
            }

            public override void TrySetException(Exception exception)
            {
                _taskCompletionSource.TrySetException(exception);
                Dispose();
            }

            public override void TryCancel()
            {
                _taskCompletionSource.TrySetCanceled(CancellationToken);
                Dispose();
            }
        }

        #region Visca Constants
        private const byte ViscaDeviceHeader = 0x81;
        private const byte ViscaCommand = 0x01;
        private const byte ViscaInquiry = 0x09;
        #endregion

        #region Visca Commands
        private static readonly byte[] HomeBytes = {ViscaDeviceHeader, ViscaCommand, 0x06, 0x04, 0xFF};
        private static readonly byte[] ResetBytes = {ViscaDeviceHeader, ViscaCommand, 0x06, 0x05, 0xFF};
        private static readonly byte[] CancelBytes = {ViscaDeviceHeader, 0x21, 0xFF};
        #endregion

        #region Visca Inquiries
        private static readonly byte[] InqPowerBytes = {ViscaDeviceHeader, ViscaInquiry, 0x04, 0x00, 0xFF};
        private static readonly byte[] InqZoomBytes = {ViscaDeviceHeader, ViscaInquiry, 0x04, 0x47, 0xFF};
        private static readonly byte[] InqFocusModeBytes = {ViscaDeviceHeader, ViscaInquiry, 0x04, 0x38, 0xFF};
        private static readonly byte[] InqFocusPosBytes = {ViscaDeviceHeader, ViscaInquiry, 0x04, 0x48, 0xFF};

        #endregion
    }
}
