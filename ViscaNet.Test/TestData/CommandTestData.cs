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
    public class CommandTestData : TheoryData<ViscaCommand, byte[], object?, LogLevel, string?>
    {
        private static readonly IReadOnlyList<(ViscaCommand command, byte[]? expectedData, object? expectedResponse,
                LogLevel expectedLogLevel, string? expectedMessage)>
            s_commands =
                new (ViscaCommand, byte[]?, object?, LogLevel, string?)[]
                {
                    (ViscaCommand.IFClear, null, null, LogLevel.None, null),
                    (ViscaCommand.Cancel(0x0), null, null, LogLevel.None, null),
                    (ViscaCommand.Cancel(), null, null, LogLevel.None, null),
                    (ViscaCommand.Cancel(0xF), null, null, LogLevel.None, null),
                    (ViscaCommand.Reset, null, null, LogLevel.None, null),
                    (ViscaCommand.Home, null, null, LogLevel.None, null),
                    (ViscaCommand.PowerOn, null, null, LogLevel.None, null),
                    (ViscaCommand.PowerOff, null, null, LogLevel.None, null),
                    (ViscaCommand.InquirePower, new byte[] {0x02}, PowerMode.On, LogLevel.None, null),
                    (ViscaCommand.InquirePower, new byte[] {0x03}, PowerMode.Off, LogLevel.None, null),
                    (ViscaCommand.InquirePower, new byte[] {0x4}, PowerMode.Unknown, LogLevel.Error,
                        "Invalid power result '0x04' received."),
                    (ViscaCommand.InquireZoom, new byte[] {0x00, 0x00, 0x00, 0x00}, 0D, LogLevel.None, null),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x00, 0x00}, 1D, LogLevel.None, null),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x10, 0x00}, 0D, LogLevel.Error,
                        "Invalid zoom data received in MSBs: 0x04 0x00 0x10 0x00."),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x00, 0x01}, 1D, LogLevel.Warning,
                        "Invalid zoom position received '0x4001' > '0x4000'."),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x00, 0x01}, 1D, LogLevel.Warning,
                        "Invalid zoom position received '0x4001' > '0x4000'.")
                };

        public static readonly CommandTestData Instance = new CommandTestData();

        public CommandTestData()
        {
            foreach (var (command, expectedData, expectedResponse, expectedLogLevel, expectedMessage) in s_commands)
            {
                if (expectedLogLevel != LogLevel.None && string.IsNullOrEmpty(expectedMessage))
                    throw new InvalidDataException($"No message supplied for '{command.Name}' with log level '{expectedLogLevel}'.");

                byte[] data;
                var len = expectedData?.Length ?? 0;
                if (len > 0)
                {
                    data = new byte[len + 3];
                    Array.Copy(expectedData!,0, data, 2, len);
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
