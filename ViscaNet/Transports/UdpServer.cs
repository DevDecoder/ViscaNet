// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Threading;

namespace ViscaNet.Transports
{
    // TODO
    internal class UdpServer : IDisposable
    {
        private readonly Dictionary<IPEndPoint, (UdpConnection connection, PipeWriter writer)> _connections =
            new Dictionary<IPEndPoint, (UdpConnection, PipeWriter)>();

        private readonly ILogger<UdpServer>? _logger;
        private CancellationTokenSource? _cancellationTokenSource;

        public UdpServer(ILogger<UdpServer>? logger = null) => _logger = logger;

        /*
        static async Task UdpServerClient(string serverName, Pipe p)
        {
            while (true)
            {
                var readResult = await p.Reader.ReadAsync();
                var message = Encoding.ASCII.GetString(readResult.Buffer.FirstSpan.ToArray());
                Console.WriteLine($"Server: {serverName} Received: {message}");
                p.Reader.AdvanceTo(readResult.Buffer.End);
            }
        }
        */

        /// <inheritdoc />
        public void Dispose()
        {
            lock (_connections)
            {
                foreach (var connection in _connections.Values)
                {
                    connection.connection.Dispose();
                }
            }
        }

        public UdpConnection GetConnection(IPEndPoint endPoint)
        {
            lock (_connections)
            {
                if (_connections.TryGetValue(endPoint, out var existing))
                {
                    return existing.connection;
                }

                if (_cancellationTokenSource is null)
                {
                    Debug.Assert(_connections.Count < 1);
                    _cancellationTokenSource = new CancellationTokenSource();
                    // Kick off server
                    Task.Run(() => ListenAsync(_cancellationTokenSource.Token))
                        .ConfigureAwait(false);
                }

                var pipe = new Pipe();
                var connection = new UdpConnection(this, endPoint, pipe.Reader);
                _connections[endPoint] = (connection, pipe.Writer);
                _logger?.LogInformation($"Opening UDP connection to {endPoint}");
                return connection;
            }
        }

        internal void ReleaseConnection(UdpConnection connection)
        {
            lock (_connections)
            {
                var endPoint = connection.EndPoint;
                if (!_connections.TryGetValue(endPoint, out var existing))
                {
                    return;
                }

                // Sanity check
                if (!ReferenceEquals(existing.connection, connection))
                {
                    _logger?.LogError($"Cannot remove unmatched {endPoint} connection!");
                    return;
                }

                _connections.Remove(endPoint);

                if (_connections.Count > 0)
                {
                    return;
                }

                _cancellationTokenSource?.Cancel();
                Interlocked.Exchange(ref _cancellationTokenSource, null)?.Dispose();
                _logger?.LogInformation($"Closed UDP connection to {endPoint}");
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken = default)
        {
            var serverPort = GetAvailablePort();

            using var udpServer = new UdpClient(new IPEndPoint(IPAddress.Any, serverPort));
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait for some data to arrive
                var result = await udpServer.ReceiveAsync().WithCancellation(cancellationToken)
                    .ConfigureAwait(false);
                var endPoint = result.RemoteEndPoint;

                if (!_connections.TryGetValue(endPoint, out var connection))
                {
                    // Unknown endpoint, ignore
                    continue;
                }

                await connection.writer.WriteAsync(result.Buffer, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        ///     Gets the first available port in the dynamic range (49152-65535), if found; otherwise 0.
        /// </summary>
        /// <returns>The first available port, if found; otherwise 0.</returns>
        private static int GetAvailablePort()
        {
            const int start = 49152; // Inclusive start
            const int end = 65536; // Exclusive end

            var usedPorts = new HashSet<int>();
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            foreach (var port in properties.GetActiveTcpConnections()
                .Where(n => n.LocalEndPoint.Port >= start)
                .Select(n => n.LocalEndPoint.Port)
                .Concat(properties.GetActiveTcpListeners()
                    .Where(n => n.Port >= start)
                    .Select(n => n.Port))
                .Concat(properties.GetActiveUdpListeners()
                    .Where(n => n.Port >= start)
                    .Select(n => n.Port)))
            {
                usedPorts.Add(port);
            }

            // Start by trying to find a random port, because of the size of the range, this should almost always
            // succeed.
            var random = new Random();
            var fails = 0;
            do
            {
                var port = random.Next(start, end);
                if (!usedPorts.Contains(port))
                {
                    return port;
                }
            } while (++fails < 20);

            // After 20 failures just scan from a random offset, we've clearly got a very busy space!
            var len = end - start;
            var offset = random.Next(len);
            return Enumerable.Range(0, len).Select(p => start + ((p + offset) % len))
                .FirstOrDefault(port => !usedPorts.Contains(port));
        }
    }
}
