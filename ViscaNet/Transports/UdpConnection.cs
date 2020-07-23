// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;

namespace ViscaNet.Transports
{
    // TODO
    internal class UdpConnection : IDisposable
    {
        private UdpClient? _client;

        internal UdpConnection(UdpServer server, IPEndPoint endPoint, PipeReader reader)
        {
            Server = server;
            EndPoint = endPoint;
            Reader = reader;
            _client = new UdpClient();
            _client.Connect(endPoint);
        }

        public UdpServer Server { get; }
        public IPEndPoint EndPoint { get; }
        public PipeReader Reader { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            Server.ReleaseConnection(this);
            Interlocked.Exchange(ref _client, null)?.Dispose();
        }

        public Task<int> WriteAsync(byte[] source, CancellationToken cancellationToken = default)
            => _client?.SendAsync(source, source.Length).WithCancellation(cancellationToken)
               ?? throw new ObjectDisposedException(nameof(UdpConnection));
    }
}
