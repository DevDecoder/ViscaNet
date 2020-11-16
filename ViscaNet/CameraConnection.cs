// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using DevDecoder.ViscaNet.Commands;
using DevDecoder.ViscaNet.Transports;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace DevDecoder.ViscaNet
{
    public sealed class CameraConnection : IDisposable
    {
        // We limit capacity as our writers will wait to write, but we allow a small queue to optimise throughput under heavy load.
        private readonly Channel<CommandTask> _commandChannel =
            Channel.CreateBounded<CommandTask>(new BoundedChannelOptions(4) {SingleReader = true});

        private readonly bool _disposeTransport;
        private readonly ILogger<CameraConnection>? _logger;
        private CancellationTokenSource? _monitorCancellationTokenSource = new CancellationTokenSource();
        private BehaviorSubject<CameraStatus>? _statusSubject = new BehaviorSubject<CameraStatus>(CameraStatus.Unknown);

        private IViscaTransport? _transport;

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

            Task.Run(() => MonitorAsync(_monitorCancellationTokenSource!.Token), _monitorCancellationTokenSource!.Token)
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

            Task.Run(() => MonitorAsync(_monitorCancellationTokenSource!.Token), _monitorCancellationTokenSource!.Token)
                .ConfigureAwait(false);
        }

        public uint RetryTimeout { get; }

        public string Name { get; }
        public bool IsConnected => _statusSubject?.Value?.Connection == ConnectionState.Connected;

        public IObservable<CameraStatus> Status =>
            _statusSubject ?? throw new ObjectDisposedException(nameof(CameraConnection));

        public CameraStatus CurrentStatus =>
            _statusSubject?.Value ?? throw new ObjectDisposedException(nameof(CameraConnection));

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
            {
                Interlocked.Exchange(ref _transport, null)?.Dispose();
            }

            var cameraStatusSubject = Interlocked.Exchange(ref _statusSubject, null);
            if (cameraStatusSubject != null)
            {
                cameraStatusSubject.OnCompleted();
                cameraStatusSubject.Dispose();
            }
        }

        private async Task MonitorAsync(CancellationToken cancellationToken)
        {
            var reader = _commandChannel.Reader;
            var transport = _transport ?? throw new ObjectDisposedException(nameof(CameraConnection));
            var statusSubject = _statusSubject ?? throw new ObjectDisposedException(nameof(CameraConnection));
            do
            {
                CameraStatus status;
                try
                {
                    // Attempt connection
                    if (await transport.ConnectAsync(cancellationToken))
                    {
                        await ExecuteAsync(transport, InquiryCommands.Version, cancellationToken).ConfigureAwait(false);
                        await ExecuteAsync(transport, InquiryCommands.Power, cancellationToken).ConfigureAwait(false);

                        status = statusSubject.Value;

                        _logger?.LogInformation($"Connected to '{Name}' camera.");
                        if (status.TryWith(out status, connected: ConnectionState.Connected))
                        {
                            statusSubject.OnNext(status);
                        }

                        while (transport.IsConnected &&
                               await reader.WaitToReadAsync(cancellationToken))
                        {
                            // Grab any waiting commands
                            while (transport.IsConnected &&
                                   reader.TryRead(out var commandTask) &&
                                   !commandTask.CancellationToken.IsCancellationRequested &&
                                   commandTask.TaskCompletionSource.Task.Status == TaskStatus.WaitingForActivation)
                            {
                                try
                                {
                                    using var cct = commandTask.CancellationToken.CombineWith(cancellationToken);
                                    var response = await ExecuteAsync(transport, commandTask.Command, cct.Token)
                                        .ConfigureAwait(false);

                                    Intercept(commandTask.Command, response);

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
                    else
                    {
                        _logger?.LogWarning(
                            $"Could not connect to '{Name}' camera, retrying in {RetryTimeout / 1000D:F3}s.");
                    }
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

                // Consider ourselves disconnected at this point.
                if (statusSubject.Value.TryWith(out status, connected: ConnectionState.Disconnected))
                {
                    statusSubject.OnNext(status);
                }

                await Task.Delay((int)RetryTimeout, cancellationToken)
                    .ConfigureAwait(false);
            } while (!cancellationToken.IsCancellationRequested);

            _logger?.LogInformation($"Disconnected '{Name}' camera.");
            // Stop the channel.
            _commandChannel.Writer.TryComplete();
        }

        private async Task<Response> ExecuteAsync(IViscaTransport transport, Command command,
            CancellationToken cancellationToken)
        {
            var response = await transport.SendAsync(command, cancellationToken).ConfigureAwait(false);
            Intercept(command, response);
            return response;
        }

        private void Intercept(Command command, Response response)
        {
            if (!response.IsValid)
            {
                return;
            }

            var statusSubject = _statusSubject ?? throw new ObjectDisposedException(nameof(CameraConnection));
            CameraStatus status;
            switch (response)
            {
                case InquiryResponse<CameraVersion> cameraVersionResponse:
                    if (statusSubject.Value.TryWith(out status, cameraVersionResponse.Result))
                    {
                        statusSubject.OnNext(status);
                    }

                    break;
                case InquiryResponse<PowerMode> powerModeResponse:
                    if (statusSubject.Value.TryWith(out status, powerMode: powerModeResponse.Result))
                    {
                        statusSubject.OnNext(status);
                    }

                    break;
                case InquiryResponse<double> doubleResponse:
                    if (command == InquiryCommands.Zoom)
                    {
                        // TODO Update zoom
                    }

                    break;
                default:
                    if (command == ViscaCommands.PowerOff)
                    {
                        if (statusSubject.Value.TryWith(out status, powerMode: PowerMode.Off))
                        {
                            statusSubject.OnNext(status);
                        }
                    }
                    else if (command == ViscaCommands.PowerOn)
                    {
                        if (statusSubject.Value.TryWith(out status, powerMode: PowerMode.On))
                        {
                            statusSubject.OnNext(status);
                        }
                    }

                    break;
            }
        }

        public Task ConnectAsync(CancellationToken cancellationToken = default) =>
            _statusSubject.FirstAsync(status => status.Connection == ConnectionState.Connected).ToTask(cancellationToken);

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

        public async Task<InquiryResponse<T>> SendAsync<T>(InquiryCommand<T> inquiry,
            CancellationToken cancellationToken = default)
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
            public readonly CancellationToken CancellationToken;

            public readonly Command Command;

            public readonly TaskCompletionSource<Response> TaskCompletionSource =
                new TaskCompletionSource<Response>(TaskCreationOptions.RunContinuationsAsynchronously);

            public CommandTask(Command command, CancellationToken cancellationToken)
            {
                Command = command;
                CancellationToken = cancellationToken;
                cancellationToken.Register(() => TaskCompletionSource.TrySetCanceled(cancellationToken));
            }
        }
    }
}
