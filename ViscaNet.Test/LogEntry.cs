// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace DevDecoder.ViscaNet.Test
{
    public class LogEntry
    {
        public LogEntry(LogLevel logLevel, EventId eventId, Exception? exception, string message,
            IReadOnlyList<object>? scope)
        {
            LogLevel = logLevel;
            EventId = eventId;
            Exception = exception;
            Message = message;
            Scope = scope ?? Array.Empty<object>();
        }

        public LogLevel LogLevel { get; }
        public EventId EventId { get; }
        public Exception? Exception { get; }
        public string Message { get; }
        public IReadOnlyList<object> Scope { get; }
    }

    public class LogEntry<TState> : LogEntry
    {
        internal LogEntry(LogLevel logLevel, EventId eventId, TState state, Exception exception, string message,
            IReadOnlyList<object> scope)
            : base(logLevel, eventId, exception, message, scope) =>
            State = state;

        public TState State { get; }
    }
}
