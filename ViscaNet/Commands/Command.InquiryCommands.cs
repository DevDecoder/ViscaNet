// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public partial class Command
    {
        public static readonly InquiryCommand<PowerMode> InquirePower = new InquiryCommand<PowerMode>(
            "Power Inquiry",
            1,
            (ReadOnlySpan<byte> payload, out PowerMode result, ILogger? logger) =>
            {
                var p = payload[0];
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
            (ReadOnlySpan<byte> payload, out double result, ILogger? logger) =>
            {
                var b1 = payload[0];
                var b2 = payload[1];
                var b3 = payload[2];
                var b4 = payload[3];

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

        public static readonly InquiryCommand<CameraVersion> InquireVersion = new InquiryCommand<CameraVersion>(
            "Camera Version Inquiry",
            7, 
            (ReadOnlySpan<byte> payload, out CameraVersion result, ILogger? logger) =>
            {
                result = new CameraVersion(
                    (ushort)((payload[0] << 8) + payload[1]),
                    (ushort)((payload[2] << 8) + payload[3]),
                    (ushort)((payload[4] << 8) + payload[5]),
                    payload[6]);
                return true;
            },
            0x00, 0x02
        );
    }
}
