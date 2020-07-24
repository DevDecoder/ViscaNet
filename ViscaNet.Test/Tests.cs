// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ViscaNet.Commands;
using ViscaNet.Transports;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public sealed class Tests : TestsBase
    {
        public Tests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }
        
        [Fact(Timeout = 5000)]
        public async Task TestInquiriesAsync()
        {
            var cts = new CancellationTokenSource(10000);
            using var connection = new CameraConnection(IPAddress.Parse("192.168.1.201"), 1259,
                logger: GetLogger<CameraConnection>());

            var powerMode = await connection.SendAsync(Command.InquirePower, cts.Token)
                .ConfigureAwait(false);
            Context.WriteLine($"Power result: {powerMode.Result}");

            var zoom = await connection.SendAsync(Command.InquireZoom, cts.Token)
                .ConfigureAwait(false);
            Context.WriteLine($"Zoom result: {zoom.Result * 100:f2}%");
            Assert.True(connection.IsConnected);
        }

        [Fact(Skip="Long Running", Timeout = 22000)]
        public async Task TestInvalidConnectionDefaultTimeoutAsync()
        {
            using var connection = new CameraConnection(IPAddress.Parse("192.168.1.201"), 1259,
                logger: GetLogger<CameraConnection>());
            var timestamp = Stopwatch.GetTimestamp();
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.SendAsync(Command.Home));
            var delaySecs = (double)(Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency;
            Context.WriteLine($"Task cancelled after {delaySecs:F3}s");
            Assert.False(connection.IsConnected);
        }

        [Fact(Timeout = 2000)]
        public async Task Invalid_connection_explicit_timeout_Async()
        {
            const int msTimeout = 100;
            using var connection = new CameraConnection(IPAddress.Parse("127.0.0.1"), 65000,
                logger: GetLogger<CameraConnection>());
            var timestamp = Stopwatch.GetTimestamp();
            var cts = new CancellationTokenSource(msTimeout);
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.SendAsync(Command.Home, cts.Token));
            var msDelay = (1000D * (Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency);
            Context.WriteLine($"SendAsync returned after {msDelay:F3}ms");
            Assert.True(msDelay >= msTimeout && msDelay < (msTimeout * 1.5D));
            Assert.False(connection.IsConnected);
        }
    }
}
