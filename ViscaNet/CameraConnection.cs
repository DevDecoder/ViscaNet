// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ViscaNet.Commands;
using ViscaNet.Transports;

namespace ViscaNet
{
    public sealed class CameraConnection : IDisposable
    {
        private readonly ILogger<CameraConnection>? _logger;
        private readonly bool _disposeTransport;

        public CameraConnection(
            IPAddress address,
            uint port = 0,
            string? name = null,
            byte deviceId = 1,
            uint maxTimeout = 20000,
            ushort connectionTimeout = 5000,
            ILogger<CameraConnection>? logger = null)
        {
            if (port < 1) port = 52381;

            // Create and dispose our own transport
            // TODO - add UDP option
            _transport = new TcpViscaTransport(
                new IPEndPoint(address, (int)port),
                deviceId,
                maxTimeout,
                connectionTimeout,
                logger);
            _disposeTransport = true;
            Name = name ?? _transport.ToString();
            _logger = logger;
        }

        public CameraConnection(
            IViscaTransport transport,
            string? name = null,
            ILogger<CameraConnection>? logger = null)
        {
            // As transport supplied, we don't dispose ourselves
            _transport = transport;
            Name = name ?? transport.ToString();
            _logger = logger;
        }

        public string Name { get; }

        private IViscaTransport? _transport;

        public IViscaTransport Transport => _transport ?? throw new ObjectDisposedException(nameof(CameraConnection));

        public IObservable<bool> ConnectionState => Transport.ConnectionState;

        public bool IsConnected => Transport.ConnectionState.FirstOrDefaultAsync().Wait();

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposeTransport)
                Interlocked.Exchange(ref _transport, null)?.Dispose();
        }

        public Task<ViscaResponse> SendAsync(ViscaCommand command, CancellationToken cancellationToken = default)
            => Transport.SendAsync(command, cancellationToken);

        public async Task<InquiryResponse<T>> SendAsync<T>(InquiryCommand<T> inquiry, CancellationToken cancellationToken = default)
        {
            var result = await Transport.SendAsync(inquiry, cancellationToken);
            return (result as InquiryResponse<T>)!;
        }
    }
}
