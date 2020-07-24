// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;
using ViscaNet.Commands;
using ViscaNet.Transports;

namespace ViscaNet
{
    public sealed class CameraConnection : IDisposable
    {
        public uint RetryTimeout { get; }
        private readonly ILogger<CameraConnection>? _logger;
        private readonly bool _disposeTransport;
        private readonly Channel<CommandTask> _commandChannel;

        public CameraConnection(
            IPAddress address,
            uint port = 0,
            string? name = null,
            byte deviceId = 1,
            uint maxTimeout = 20000,
            ushort connectionTimeout = 5000,
            uint retryTimeout = 1000,
            ILogger<CameraConnection>? logger = null)
        {
            if (retryTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), retryTimeout,
                    "The retry timeout must be > 10ms.");
            }

            if (retryTimeout > maxTimeout)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), retryTimeout,
                    "The retry timeout must be <= the maximum timeout.");
            }

            // Create Transport TODO - support UDP here
            _transport = new TcpViscaTransport(
                new IPEndPoint(address, (int)(port < 1 ? 52381 : port)),
                deviceId,
                maxTimeout,
                connectionTimeout,
                logger);

            // As we created the transport, we dispose it.
            _disposeTransport = true;

            Name = name ?? _transport.ToString();
            RetryTimeout = retryTimeout;
            _logger = logger;

            // We limit capacity as our writers will wait to write, but we allow a small queue to optimise throughput under heavy load.
            _commandChannel = Channel.CreateBounded<CommandTask>(new BoundedChannelOptions(4) { SingleReader = true });
            _monitorCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorCamera(_monitorCancellationTokenSource.Token), _monitorCancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        public CameraConnection(
            IViscaTransport transport,
            string? name = null,
            uint retryTimeout = 1000,
            ILogger<CameraConnection>? logger = null)
        {
            if (retryTimeout < 100)
            {
                throw new ArgumentOutOfRangeException(nameof(retryTimeout), retryTimeout,
                    "The retry timeout must be > 10ms.");
            }


            _transport = transport;

            Name = name ?? transport.ToString();
            RetryTimeout = retryTimeout;
            _logger = logger;

            // We limit capacity as our writers will wait to write, but we allow a small queue to optimise throughput under heavy load.
            _commandChannel = Channel.CreateBounded<CommandTask>(new BoundedChannelOptions(4) { SingleReader = true });
            _monitorCancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => MonitorCamera(_monitorCancellationTokenSource.Token), _monitorCancellationTokenSource.Token)
                .ConfigureAwait(false);
        }

        private async Task MonitorCamera(CancellationToken cancellationToken)
        {
            var reader = _commandChannel.Reader;
            var transport = _transport ?? throw new ObjectDisposedException(nameof(CameraConnection));
            do
            {
                try
                {
                    // Attempt connection
                    if (await transport.ConnectAsync(cancellationToken))
                    {

                        while (await reader.WaitToReadAsync(cancellationToken))
                        {
                            // Grab any waiting commands
                            while (await transport.ConnectionState.FirstOrDefaultAsync() &&
                                   reader.TryRead(out var commandTask) &&
                                   !commandTask.CancellationToken.IsCancellationRequested &&
                                   commandTask.TaskCompletionSource.Task.Status == TaskStatus.WaitingForActivation)
                            {
                                try
                                {
                                    using var cct = commandTask.CancellationToken.CombineWith(cancellationToken);
                                    var response = await transport.SendAsync(commandTask.Command, cct.Token)
                                        .ConfigureAwait(false);

                                    // TODO Intercept interesting responses here (particularly enquiries

                                    commandTask.TaskCompletionSource.TrySetResult(response);
                                }
                                catch (OperationCanceledException)
                                {
                                    commandTask.TaskCompletionSource.TrySetCanceled(commandTask.CancellationToken);
                                }
                                catch (Exception exception)
                                {
                                    commandTask.TaskCompletionSource.TrySetException(exception);
                                }
                            }
                        }
                    }

                    _logger?.LogWarning(
                        $"Could not connect to '{Name}' camera, retrying in {RetryTimeout / 1000D:F3}s.");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger?.LogError(exception,
                        $"Communication error with '{Name}' camera, retrying in {RetryTimeout / 1000D:F3}s.");
                }

                await Task.Delay((int)RetryTimeout, cancellationToken)
                        .ConfigureAwait(false);
            } while (!cancellationToken.IsCancellationRequested);

            // Stop the channel.
            _commandChannel.Writer.TryComplete();
        }

        public string Name { get; }

        private IViscaTransport? _transport;
        private CancellationTokenSource? _monitorCancellationTokenSource;

        public IObservable<bool> ConnectionState => (_transport ?? throw new ObjectDisposedException(nameof(CameraConnection))).ConnectionState;

        public bool IsConnected => (_transport ?? throw new ObjectDisposedException(nameof(CameraConnection))).ConnectionState.FirstOrDefaultAsync().Wait();

        /// <inheritdoc />
        public void Dispose()
        {
            var cts = Interlocked.Exchange(ref _monitorCancellationTokenSource, null);
            if (cts != null)
            {
                cts.Cancel();
                cts?.Dispose();
            }

            if (_disposeTransport)
                Interlocked.Exchange(ref _transport, null)?.Dispose();
        }

        public async Task<Response> SendAsync(Command command, CancellationToken cancellationToken = default)
        {
            CommandTask? commandTask = null;
            // As we're using a bounded queue with limited capacity we can avoid the overhead
            // of creating a command task until there is space
            while (await _commandChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                commandTask ??= new CommandTask(command, cancellationToken);
                if (_commandChannel.Writer.TryWrite(commandTask))
                {
                    // We are queued
                    return await commandTask.TaskCompletionSource.Task.ConfigureAwait(false);
                }
            }

            // Failed to get writer at all
            return command.UnknownResponse;
        }

        public async Task<InquiryResponse<T>> SendAsync<T>(InquiryCommand<T> inquiry, CancellationToken cancellationToken = default)
        {
            CommandTask? commandTask = null;
            // As we're using a bounded queue with limited capacity we can avoid the overhead
            // of creating a command task until there is space
            while (await _commandChannel.Writer.WaitToWriteAsync(cancellationToken).ConfigureAwait(false))
            {
                commandTask ??= new CommandTask(inquiry, cancellationToken);
                if (_commandChannel.Writer.TryWrite(commandTask))
                {
                    // We are queued
                    return (InquiryResponse<T>)await commandTask.TaskCompletionSource.Task.ConfigureAwait(false);
                }
            }

            // Failed to get writer at all
            return (InquiryResponse<T>)inquiry.UnknownResponse;
        }

        private class CommandTask
        {
            public readonly TaskCompletionSource<Response> TaskCompletionSource =
                new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);

            public readonly Command Command;
            public readonly CancellationToken CancellationToken;

            public CommandTask(Command command, CancellationToken cancellationToken)
            {
                Command = command;
                CancellationToken = cancellationToken;
                cancellationToken.Register(() => TaskCompletionSource.TrySetCanceled(cancellationToken));
            }
        }
    }
}
