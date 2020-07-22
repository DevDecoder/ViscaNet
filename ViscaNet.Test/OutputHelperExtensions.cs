// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace Xunit.Abstractions
{
    public static class OutputHelperExtensions
    {
        /// <summary>
        ///     Builds a logger from the specified test output helper.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="memberName">
        ///     The member to create the logger for. This is automatically populated using <see cref="T:System.Runtime.CompilerServices.CallerMemberNameAttribute" />
        ///     .
        /// </param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger BuildDisposableLogger(
          this ITestOutputHelper output,
          [CallerMemberName] string? memberName = null) =>
            output.BuildDisposableLogger(null, memberName);

        /// <summary>
        ///     Builds a logger from the specified test output helper.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="logLevel">The minimum log level to output.</param>
        /// <param name="memberName">
        ///     The member to create the logger for. This is automatically populated using <see cref="T:System.Runtime.CompilerServices.CallerMemberNameAttribute" />
        ///     .
        /// </param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger BuildDisposableLogger(
          this ITestOutputHelper output,
          LogLevel logLevel,
          [CallerMemberName] string? memberName = null) =>
            output.BuildDisposableLogger(new LoggingConfig()
            {
                LogLevel = logLevel
            }, memberName);

        /// <summary>
        ///     Builds a logger from the specified test output helper.
        /// </summary>
        /// <param name="output">The test output helper.</param>
        /// <param name="config">Optional logging configuration.</param>
        /// <param name="memberName">
        ///     The member to create the logger for. This is automatically populated using <see cref="T:System.Runtime.CompilerServices.CallerMemberNameAttribute" />
        ///     .
        /// </param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger BuildDisposableLogger(
          this ITestOutputHelper output,
          LoggingConfig? config,
          [CallerMemberName] string? memberName = null) => new DisposabelCacheLogger(output, config, memberName!);

        /// <summary>
        ///     Builds a logger from the specified test output helper for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create the logger for.</typeparam>
        /// <param name="output">The test output helper.</param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger<T> BuildDisposableLoggerFor<T>(this ITestOutputHelper output)
            => output.BuildDisposableLoggerFor<T>(null);

        /// <summary>
        ///     Builds a logger from the specified test output helper for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create the logger for.</typeparam>
        /// <param name="output">The test output helper.</param>
        /// <param name="logLevel">The minimum log level to output.</param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger<T> BuildDisposableLoggerFor<T>(
            this ITestOutputHelper output,
            LogLevel logLevel) =>
            output.BuildDisposableLoggerFor<T>(new LoggingConfig() { LogLevel = logLevel });

        /// <summary>
        ///     Builds a logger from the specified test output helper for the specified type.
        /// </summary>
        /// <typeparam name="T">The type to create the logger for.</typeparam>
        /// <param name="output">The test output helper.</param>
        /// <param name="config">Optional logging configuration.</param>
        /// <returns>The logger.</returns>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="output" /> is <c>null</c>.</exception>
        public static IDisposableCacheLogger<T> BuildDisposableLoggerFor<T>(
            this ITestOutputHelper output,
            LoggingConfig? config) => new DisposabelCacheLogger<T>(output, config);

        private class DisposabelCacheLogger : IDisposableCacheLogger
        {
            public DisposabelCacheLogger(
                ITestOutputHelper output,
                LoggingConfig? config,
                string memberName)
            {
                if (output == null)
                    throw new ArgumentNullException(nameof(output));
                _factory = LogFactory.Create(output, config);
                _cacheLogger = _factory.CreateLogger(memberName).WithCache();
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _factory, null)?.Dispose();
            }

            private readonly ICacheLogger _cacheLogger;
            private ILoggerFactory? _factory;

            /// <inheritdoc />
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception,
                Func<TState, Exception, string> formatter)
            {
                _cacheLogger.Log(logLevel, eventId, state, exception, formatter);
            }

            /// <inheritdoc />
            public bool IsEnabled(LogLevel logLevel) => _cacheLogger.IsEnabled(logLevel);

            /// <inheritdoc />
            public IDisposable BeginScope<TState>(TState state) => _cacheLogger.BeginScope(state);

            /// <inheritdoc />
            public int Count => _cacheLogger.Count;

            /// <inheritdoc />
            public IReadOnlyCollection<LogEntry> Entries => _cacheLogger.Entries;

            /// <inheritdoc />
            public LogEntry Last => _cacheLogger.Last;
        }
        private class DisposabelCacheLogger<T> : IDisposableCacheLogger<T>
        {
            public DisposabelCacheLogger(
                ITestOutputHelper output,
                LoggingConfig? config)
            {
                if (output == null)
                    throw new ArgumentNullException(nameof(output));
                _factory = LogFactory.Create(output, config);
                _cacheLogger = _factory.CreateLogger<T>().WithCache();
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref _factory, null)?.Dispose();
            }

            private readonly ICacheLogger<T> _cacheLogger;
            private ILoggerFactory? _factory;

            /// <inheritdoc />
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                _cacheLogger.Log(logLevel, eventId, state, exception, formatter);
            }

            /// <inheritdoc />
            public bool IsEnabled(LogLevel logLevel) => _cacheLogger.IsEnabled(logLevel);

            /// <inheritdoc />
            public IDisposable BeginScope<TState>(TState state) => _cacheLogger.BeginScope(state);

            /// <inheritdoc />
            public int Count => _cacheLogger.Count;

            /// <inheritdoc />
            public IReadOnlyCollection<LogEntry> Entries => _cacheLogger.Entries;

            /// <inheritdoc />
            public LogEntry Last => _cacheLogger.Last;
        }
    }

    public interface IDisposableCacheLogger : ICacheLogger, IDisposable
    {
    }

    public interface IDisposableCacheLogger<out T> : ICacheLogger<T>, IDisposableCacheLogger
    {
    }
}
