// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Test
{
    public class CustomLogFormatter : LoggingConfig, ILogFormatter
    {
        public CustomLogFormatter() => Formatter = this;

        public static CustomLogFormatter Current { get; } = new CustomLogFormatter();

        string ILogFormatter.Format(
            int scopeLevel,
            string name,
            LogLevel logLevel,
            EventId eventId,
            string message,
            Exception exception)
        {
            var builder = new StringBuilder();

            if (scopeLevel > 0)
            {
                builder.Append(' ', scopeLevel * 2);
            }

            builder.Append($"{logLevel} ");

            if (!string.IsNullOrEmpty(name))
            {
                builder.Append($"{name} ");
            }

            if (eventId.Id != 0)
            {
                builder.Append($"[{eventId.Id}]: ");
            }

            if (!string.IsNullOrEmpty(message))
            {
                builder.Append(message);
            }

            if (exception != null)
            {
                builder.Append($"\n{exception}");
            }

            return builder.ToString();
        }
    }
}
