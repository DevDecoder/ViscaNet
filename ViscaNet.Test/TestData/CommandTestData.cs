// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ViscaNet.Test.TestData
{
    public class CommandTestData : TheoryData<ViscaCommand, byte[], object?>
    {
        private static readonly IReadOnlyList<(ViscaCommand command, byte[]? expectedData, object? expectedResponse)>
            s_commands =
                new (ViscaCommand, byte[]?, object?)[]
                {
                    (ViscaCommand.Cancel, null, null), (ViscaCommand.Reset, null, null),
                    (ViscaCommand.Home, null, null), (ViscaCommand.PowerOn, null, null),
                    (ViscaCommand.PowerOff, null, null), (ViscaCommand.InquirePower, new byte[] {0x02}, PowerMode.On),
                    (ViscaCommand.InquirePower, new byte[] {0x03}, PowerMode.Off), (ViscaCommand.InquirePower,
                        new byte[] {0x4},
                        new ExpectedLog<PowerMode>(LogLevel.Error, "Invalid power result '0x04' received.")
                    ),
                    (ViscaCommand.InquireZoom, new byte[] {0x00, 0x00, 0x00, 0x00}, 0D),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x00, 0x00}, 1D), (ViscaCommand.InquireZoom,
                        new byte[] {0x04, 0x00, 0x10, 0x00},
                        new ExpectedLog<double>(LogLevel.Error,
                            "Invalid zoom data received in MSBs: 0x04 0x00 0x10 0x00.")
                    ),
                    (ViscaCommand.InquireZoom, new byte[] {0x04, 0x00, 0x00, 0x01},
                        new ExpectedLog<double>(LogLevel.Warning, "Invalid zoom position received '0x4001' > '0x4000'.",
                            1D)),
                    (ViscaCommand.InquireZoom,
                        new byte[] {0x04, 0x00, 0x00, 0x01},
                        new ExpectedLog<double>(LogLevel.Warning, "Invalid zoom position received '0x4001' > '0x4000'.",
                            1D)
                    )
                };

        public static readonly CommandTestData Instance = new CommandTestData();

        public CommandTestData()
        {
            foreach (var (command, expectedData, expectedResponse) in s_commands)
            {
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

                Add(command, data, expectedResponse);
            }
        }
    }
}
