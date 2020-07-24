// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace ViscaNet.Test
{
    public abstract class TestsBase : XunitContextBase
    {
        protected TestsBase(ITestOutputHelper outputHelper,
            [CallerFilePath] string sourceFile = "") 
            // ReSharper disable once ExplicitCallerInfoArgument
            : base(outputHelper, sourceFile!)
        {
        }

        public ILogger Logger => new ContextLogger<Unit>(Context, _logEntries);
        public ILogger<T> GetLogger<T>() => new ContextLogger<T>(Context, _logEntries);

        private readonly ConcurrentQueue<LogEntry> _logEntries = new ConcurrentQueue<LogEntry>();
        protected IReadOnlyList<LogEntry> LogEntries => _logEntries.ToArray();
        protected int LogEntryCount => _logEntries.Count;


        private class ContextLogger<T> : ILogger<T>
        {
            private readonly Context _context;
            private readonly ConcurrentQueue<LogEntry> _logEntries;
            public string Name => _context.Test.DisplayName;
            private Stack<object> _scopes = new Stack<object>();

            public ContextLogger(Context context, ConcurrentQueue<LogEntry> logEntries)
            {
                _context = context;
                _logEntries = logEntries;
            }

            public IDisposable BeginScope<TState>(TState state)
            {
                if (state is null)
                    return Disposable.Empty;

                _scopes.Push(state);
                return Disposable.Create(state, s =>
                {
                    if (!_scopes.TryPop(out var top) || !Equals(s, top))
                        throw new InvalidOperationException("Invalid scope disposed!");
                });
            }

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
            {
                if (formatter == null)
                {
                    throw new ArgumentNullException(nameof(formatter));
                }
                var message = formatter(state, exception);
                if (string.IsNullOrEmpty(message) && exception == null)
                {
                    return;
                }

                var entry = new LogEntry<TState>(logLevel, eventId, state, exception, message, _scopes.ToArray());
                _logEntries.Enqueue(entry);
                _context.WriteLine("[" + SimpleLogName(logLevel) +
                                   (!string.IsNullOrEmpty(eventId.Name)
                                       ? $":{Name}"
                                       : (eventId.Id > 0 ? $":{eventId.Id}" : string.Empty)) + "] " + message);
                if (exception != null)
                    _context.WriteLine(exception.ToString());
            }

            private static string SimpleLogName(LogLevel level) => level switch
            {
                LogLevel.Trace => "Trc",
                LogLevel.Debug => "Dbg",
                LogLevel.Information => "Inf",
                LogLevel.Warning => "Wrn",
                LogLevel.Error => "Err",
                LogLevel.Critical => "Crt",
                LogLevel.None => "Non"
            };
        }
    }
}
