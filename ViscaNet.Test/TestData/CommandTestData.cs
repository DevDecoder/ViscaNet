// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using ViscaNet.Commands;
using Xunit;

namespace ViscaNet.Test.TestData
{
    public class CommandTestData : TheoryData<Command, byte[], object?, LogLevel, string?>
    {
        private static readonly IReadOnlyList<(Command command, byte[]? expectedData, object? expectedResponse,
                LogLevel expectedLogLevel, string? expectedMessage)>
            s_commands =
                new (Command, byte[]?, object?, LogLevel, string?)[]
                {
                    (ViscaCommands.IFClear, null, null, LogLevel.None, null),
                    (ViscaCommands.Cancel(0x0), null, null, LogLevel.None, null),
                    (ViscaCommands.Cancel(), null, null, LogLevel.None, null),
                    (ViscaCommands.Cancel(0xF), null, null, LogLevel.None, null),
                    (ViscaCommands.Reset, null, null, LogLevel.None, null),
                    (ViscaCommands.Home, null, null, LogLevel.None, null),
                    (ViscaCommands.PowerOn, null, null, LogLevel.None, null),
                    (ViscaCommands.PowerOff, null, null, LogLevel.None, null),
                    (InquiryCommands.Power, new byte[] {0x02}, PowerMode.On, LogLevel.None, null),
                    (InquiryCommands.Power, new byte[] {0x03}, PowerMode.Off, LogLevel.None, null), (
                        InquiryCommands.Power, new byte[] {0x04}, PowerMode.Unknown, LogLevel.Error,
                        "Invalid 'PowerMode' value '0x04' received."),
                    (InquiryCommands.Zoom, new byte[] {0x00, 0x00, 0x00, 0x00}, (ushort)0, LogLevel.None, null),
                    (InquiryCommands.Zoom, new byte[] {0x04, 0x00, 0x00, 0x00}, (ushort)0x4000, LogLevel.None, null), (
                        InquiryCommands.Zoom, new byte[] {0x04, 0x00, 0x10, 0x00}, (ushort)0x4000, LogLevel.Warning,
                        "Invalid data received in MSBs, discarding.")
                };

        public static readonly CommandTestData Instance = new CommandTestData();

        public CommandTestData()
        {
            foreach (var (command, expectedData, expectedResponse, expectedLogLevel, expectedMessage) in s_commands)
            {
                if (expectedLogLevel != LogLevel.None && string.IsNullOrEmpty(expectedMessage))
                {
                    throw new InvalidDataException(
                        $"No message supplied for '{command.Name}' with log level '{expectedLogLevel}'.");
                }

                byte[] data;
                var len = expectedData?.Length ?? 0;
                if (len > 0)
                {
                    data = new byte[len + 3];
                    Array.Copy(expectedData!, 0, data, 2, len);
                }
                else
                {
                    data = new byte[4];
                }

                data[0] = 0x90;
                data[1] = 0x50;
                data[^1] = 0xFF;

                Add(command, data, expectedResponse, expectedLogLevel, expectedMessage);
            }
        }
    }
}
