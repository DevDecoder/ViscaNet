// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Net;
using System.Threading;
using DevDecoder.ViscaNet.Transports;
using Microsoft.Extensions.Logging;

namespace DevDecoder.ViscaNet
{
    public class Camera : IDisposable
    {
        public string? Name { get; }
        private readonly ILogger<Camera>? _logger;
        private CameraConnection? _connection;

        public Camera(IPAddress ipAddress, int port, string? name = null, ILoggerFactory? factory = null)
            : this(new IPEndPoint(ipAddress, port), name, factory)
        {
        }

        public Camera(IPEndPoint endPoint, string? name = null, ILoggerFactory? factory = null)
        {
            EndPoint = endPoint;
            Name = name;
            _logger = factory?.CreateLogger<Camera>();
            _connection = new CameraConnection(new TcpViscaTransport(endPoint), Name,
                logger: factory?.CreateLogger<CameraConnection>());
        }

        public IPEndPoint EndPoint { get; }

        public IObservable<CameraStatus> Status =>
            _connection?.Status ?? throw new ObjectDisposedException(nameof(CameraConnection));

        public CameraStatus CurrentStatus =>
            _connection?.CurrentStatus ?? throw new ObjectDisposedException(nameof(CameraConnection));

        /// <inheritdoc />
        public void Dispose()
        {
            Interlocked.Exchange(ref _connection, null)?.Dispose();
        }
    }
}
