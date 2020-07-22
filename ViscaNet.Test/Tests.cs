// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public sealed class Tests : TestsBase
    {
        public Tests(ITestOutputHelper outputHelper) : base(outputHelper)
        {
        }

        [Fact(Timeout=5000)]
        public async Task TestCancelAsync()
        {
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.201"), 1259),
                logger: Logger);
            var task1 = connection.ResetAsync();
            var task2 = connection.HomeAsync();
            var task3 = connection.CancelAsync();
            var task4 = connection.HomeAsync();
            try
            {
                await Task.WhenAll(task1, task2, task3, task4);
            }
            catch (TaskCanceledException)
            {
            }

            Assert.True(task1.IsCanceled);
            Assert.True(task2.IsCanceled);
            Assert.False(task3.IsCanceled);
            Assert.True(task3.IsCompleted);
            Assert.False(task4.IsCanceled);
            Assert.True(task4.IsCompleted);
            Assert.True(connection.IsConnected);
        }

        [Fact(Timeout = 5000)]
        public async Task TestInquiriesAsync()
        {
            var cts = new CancellationTokenSource(10000);
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.201"), 1259),
                logger: Logger);

            var powerMode = await connection.PowerInquiryAsync(cts.Token);
            Context.WriteLine($"Power result: {powerMode}");

            var zoom = await connection.ZoomInquiryAsync(cts.Token);
            Context.WriteLine($"Zoom result: {zoom * 100:f2}%");
            Assert.True(connection.IsConnected);
        }

        [Fact(Skip="Long Running", Timeout = 22000)]
        public async Task TestInvalidConnectionDefaultTimeoutAsync()
        {
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.200"), 1259),
                logger: Logger);

            var timestamp = Stopwatch.GetTimestamp();
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.HomeAsync());
            var delaySecs = (double)(Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency;
            Context.WriteLine($"Task cancelled after {delaySecs:F3}s");
            Assert.False(connection.IsConnected);
        }

        [Fact(Skip = "Long running", Timeout = 8000)]
        public async Task TestInvalidConnectionExplicitTimeoutAsync()
        {
            const int timeoutSecs = 5;
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.200"), 1259),
                logger: Logger);

            var timestamp = Stopwatch.GetTimestamp();
            var cts = new CancellationTokenSource(timeoutSecs * 1000);
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.HomeAsync(cts.Token));
            var delaySecs = (double)(Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency;
            Context.WriteLine($"Task cancelled after {delaySecs:F3}s");
            Assert.True(delaySecs >= timeoutSecs && delaySecs < (timeoutSecs * 1.15D));
            Assert.False(connection.IsConnected);
        }
    }
}
