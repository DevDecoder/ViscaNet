// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Test.TestData
{
    public abstract class ExpectedLog
    {
        protected ExpectedLog(LogLevel logLevel, string message, object? result = null)
        {
            LogLevel = logLevel;
            Message = message;
            Result = result;
        }

        public LogLevel LogLevel { get; }
        public string Message { get; }
        public object? Result { get; }
    }

    public sealed class ExpectedLog<T> : ExpectedLog
    {
        public ExpectedLog(LogLevel logLevel, string message, [MaybeNull] T result = default)
        : base(logLevel, message, result)
        {
        }

        [MaybeNull]
        public new T Result => (T)base.Result!;
    }
}
