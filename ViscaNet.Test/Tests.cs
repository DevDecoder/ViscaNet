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
    public class Tests
    {
        public Tests(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [Fact]
        public async Task TestCancelAsync()
        {
            using var logger = OutputHelper.BuildDisposableLoggerFor<CameraConnection>(CustomLogFormatter.Current);
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.201"), 1259),
                logger: logger);
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

        [Fact]
        public async Task TestInquiriesAsync()
        {
            using var logger = OutputHelper.BuildDisposableLoggerFor<CameraConnection>(CustomLogFormatter.Current);
            var cts = new CancellationTokenSource(10000);
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.201"), 1259),
                logger: logger);

            var powerMode = await connection.PowerInquiryAsync(cts.Token);
            OutputHelper.WriteLine($"Power result: {powerMode}");

            var zoom = await connection.ZoomInquiryAsync(cts.Token);
            OutputHelper.WriteLine($"Zoom result: {zoom*100:f2}%");
            Assert.True(connection.IsConnected);
        }

        [Fact]
        public async Task TestInvalidConnectionDefaultTimeoutAsync()
        {
            using var logger = OutputHelper.BuildDisposableLoggerFor<CameraConnection>(CustomLogFormatter.Current);
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.200"), 1259),
                logger: logger);

            var timestamp = Stopwatch.GetTimestamp();
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.HomeAsync());
            var delaySecs = (double)(Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency;
            OutputHelper.WriteLine($"Task cancelled after {delaySecs:F3}s");
            Assert.False(connection.IsConnected);
        }

        [Fact]
        public async Task TestInvalidConnectionExplicitTimeoutAsync()
        {
            using var logger = OutputHelper.BuildDisposableLoggerFor<CameraConnection>(CustomLogFormatter.Current);
            const int timeoutSecs = 5;
            using var connection = new CameraConnection(
                new IPEndPoint(IPAddress.Parse("192.168.1.200"), 1259),
                logger: logger);

            var timestamp = Stopwatch.GetTimestamp();
            var cts = new CancellationTokenSource(timeoutSecs * 1000);
            await Assert.ThrowsAsync<TaskCanceledException>(() => connection.HomeAsync(cts.Token));
            var delaySecs = (double)(Stopwatch.GetTimestamp() - timestamp) / Stopwatch.Frequency;
            OutputHelper.WriteLine($"Task cancelled after {delaySecs:F3}s");
            Assert.True(delaySecs >= timeoutSecs && delaySecs < (timeoutSecs * 1.15D));
            Assert.False(connection.IsConnected);
        }
    }
}
