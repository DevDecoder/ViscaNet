// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public partial class ViscaCommand
    {
        public static readonly InquiryCommand<PowerMode> InquirePower = new InquiryCommand<PowerMode>(
            "Power Inquiry",
            1,
            (byte[] payload, int offset, int count, out PowerMode result, ILogger? logger) =>
            {
                var p = payload[offset];
                switch (p)
                {
                    case 0x02:
                        result = PowerMode.On;
                        return true;
                    case 0x03:
                        result = PowerMode.Standby;
                        return true;
                    default:
                        logger?.LogError(
                            $"Invalid power result '0x{p:X2}' received.");
                        result = PowerMode.Unknown;
                        return false;
                }
            },
            0x04, 0x00);

        public static readonly InquiryCommand<double> InquireZoom = new InquiryCommand<double>(
            "Zoom Inquiry",
            4,
            (byte[] payload, int offset, int count, out double result, ILogger? logger) =>
            {
                var b1 = payload[offset++];
                var b2 = payload[offset++];
                var b3 = payload[offset++];
                var b4 = payload[offset];

                // Ensure MSBs are not set
                if (((b1 & 0xf0) + (b2 & 0xf0) + (b3 & 0xf0) + (b4 & 0xf0)) != 0)
                {
                    logger?.LogError(
                        $"Invalid zoom data received in MSBs: 0x{b1:X2} 0x{b2:X2} 0x{b3:X2} 0x{b4:X2}.");
                    result = default;
                    return false;
                }

                // Combine LSBs into single ushort (note no need to mask with 0x0f as above check has ensured MSBs are 0)
                var raw = (ushort)((b1 << 12) +
                                   (b2 << 8) +
                                   (b3 << 4) +
                                   b4);

                if (raw > 0x4000)
                {
                    logger?.LogWarning(
                        $"Invalid zoom position received '0x{raw:x4}' > '0x4000'.");
                    result = 1D;
                    return true;
                }

                // Convert to value between 0 and 1.
                result = raw / 16384D;
                return true;
            },
            0x04, 0x47
        );
    }
}
