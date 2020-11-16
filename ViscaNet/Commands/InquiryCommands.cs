// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace DevDecoder.ViscaNet.Commands
{
    public static class InquiryCommands
    {
        public static readonly InquiryCommand<CameraVersion> Version = InquiryCommand<CameraVersion>.Register(
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
            0x00, 0x02);

        public static readonly InquiryCommand<PowerMode> Power = InquiryCommand<PowerMode>.Register("Power Inquiry",
            1,
            (ReadOnlySpan<byte> payload, out PowerMode result, ILogger? logger)
                => ParseEnum(payload[0], out result, logger, true),
            0x04, 0x00);

        public static readonly InquiryCommand<byte> GainLimit = InquiryCommand<byte>.Register(
            "Auto Gain Control Limit Inquiry",
            1,
            (ReadOnlySpan<byte> payload, out byte result, ILogger? logger)
                => ByteFromLSBs(payload, out result, logger),
            0x04, 0x2C);

        public static readonly InquiryCommand<WhiteBalanceMode> WhiteBalanceMode =
            InquiryCommand<WhiteBalanceMode>.Register("White Balance Mode Inquiry",
                1,
                (ReadOnlySpan<byte> payload, out WhiteBalanceMode result, ILogger? logger)
                    => ParseEnum(payload[0], out result, logger),
                0x04, 0x35);

        public static readonly InquiryCommand<FocusMode> FocusMode = InquiryCommand<FocusMode>.Register(
            "Focus Mode Inquiry",
            1,
            (ReadOnlySpan<byte> payload, out FocusMode result, ILogger? logger)
                => ParseEnum(payload[0], out result, logger),
            0x04, 0x38);

        public static readonly InquiryCommand<AutoExposureMode> AutoExposureMode =
            InquiryCommand<AutoExposureMode>.Register("Auto Exposure Mode Inquiry",
                1,
                (ReadOnlySpan<byte> payload, out AutoExposureMode result, ILogger? logger)
                    => ParseEnum(payload[0], out result, logger),
                0x04, 0x39);

        public static readonly InquiryCommand<byte> RGain = InquiryCommand<byte>.Register("Red Gain Inquiry",
            2,
            (ReadOnlySpan<byte> payload, out byte result, ILogger? logger)
                => ByteFromLSBs(payload, out result, logger),
            0x04, 0x43);

        public static readonly InquiryCommand<byte> BGain = InquiryCommand<byte>.Register("Blue Gain Inquiry",
            2,
            (ReadOnlySpan<byte> payload, out byte result, ILogger? logger)
                => ByteFromLSBs(payload, out result, logger),
            0x04, 0x44);

        public static readonly InquiryCommand<ushort> Zoom = InquiryCommand<ushort>.Register("Zoom Inquiry",
            4,
            (ReadOnlySpan<byte> payload, out ushort result, ILogger? logger)
                => UInt16FromLSBs(payload, out result, logger),
            0x04, 0x47);

        public static readonly InquiryCommand<ushort> Focus = InquiryCommand<ushort>.Register("Focus Inquiry",
            4,
            (ReadOnlySpan<byte> payload, out ushort result, ILogger? logger)
                => UInt16FromLSBs(payload, out result, logger),
            0x04, 0x48);

        public static readonly InquiryCommand<ushort> Shutter = InquiryCommand<ushort>.Register("Shutter Inquiry",
            4,
            (ReadOnlySpan<byte> payload, out ushort result, ILogger? logger)
                => UInt16FromLSBs(payload, out result, logger),
            0x04, 0x4A);

        public static readonly InquiryCommand<ushort> Iris = InquiryCommand<ushort>.Register("Iris Inquiry",
            4,
            (ReadOnlySpan<byte> payload, out ushort result, ILogger? logger)
                => UInt16FromLSBs(payload, out result, logger),
            0x04, 0x4B);

        public static readonly InquiryCommand<uint> PanTilt = InquiryCommand<uint>.Register("Pan/Tilt Inquiry",
            8,
            (ReadOnlySpan<byte> payload, out uint result, ILogger? logger)
                => UInt32FromLSBs(payload, out result, logger),
            0x06, 0x12);

        private static bool ParseEnum<T>(byte b, out T result, ILogger? logger, bool strict = false) where T : Enum
        {
            result = (T)(object)b;
            if (b != 0xFF &&
                (!strict || Enum.IsDefined(typeof(T), result)))
            {
                return true;
            }

            logger?.LogError($"Invalid '{typeof(T).Name}' value '0x{b:X2}' received.");
            b = 0xFF;
            result = (T)(object)b;
            return false;
        }

        private static bool UInt64FromLSBs(ReadOnlySpan<byte> payload, out ulong result, ILogger? logger)
        {
            var len = payload.Length;
            if (len != 16)
            {
                throw new InvalidOperationException(
                    $"Payload length '{len}' must be 16 bytes to extract LSBs into a ulong.");
            }

            var shift = 4 * (len - 1);
            var warned = false;
            result = 0;
            for (var i = 0; i < len; i++)
            {
                var b = payload[i];
                if (!warned && (b & 0xf0) > 0)
                {
                    warned = true;
                    logger?.LogWarning(
                        "Invalid data received in MSBs, discarding.");
                }

                result += (ulong)((b & 0xf) << shift);
                shift -= 4;
            }

            return true;
        }

        private static bool UInt32FromLSBs(ReadOnlySpan<byte> payload, out uint result, ILogger? logger)
        {
            var len = payload.Length;
            if (len != 16)
            {
                throw new InvalidOperationException(
                    $"Payload length '{len}' must be 8 bytes to extract LSBs into a uint.");
            }

            var shift = 4 * (len - 1);
            var warned = false;
            result = 0;
            for (var i = 0; i < len; i++)
            {
                var b = payload[i];
                if (!warned && (b & 0xf0) > 0)
                {
                    warned = true;
                    logger?.LogWarning(
                        "Invalid data received in MSBs, discarding.");
                }

                result += (uint)((b & 0xf) << shift);
                shift -= 4;
            }

            return true;
        }

        private static bool UInt16FromLSBs(ReadOnlySpan<byte> payload, out ushort result, ILogger? logger)
        {
            var len = payload.Length;
            if (len != 4)
            {
                throw new InvalidOperationException(
                    $"Payload length '{len}' must be 4 bytes to extract LSBs into a ushort.");
            }

            var shift = 4 * (len - 1);
            var warned = false;
            result = 0;
            for (var i = 0; i < len; i++)
            {
                var b = payload[i];
                if (!warned && (b & 0xf0) > 0)
                {
                    warned = true;
                    logger?.LogWarning(
                        "Invalid data received in MSBs, discarding.");
                }

                result += (ushort)((b & 0xf) << shift);
                shift -= 4;
            }

            return true;
        }

        private static bool ByteFromLSBs(ReadOnlySpan<byte> payload, out byte result, ILogger? logger)
        {
            var len = payload.Length;
            if (len < 1 || len > 2)
            {
                throw new InvalidOperationException(
                    $"Payload length '{len}' must be 1 or 2 bytes to extract LSBs into a byte.");
            }

            result = payload[0];
            var warned = false;
            if ((result & 0xf0) > 0)
            {
                logger?.LogWarning(
                    "Invalid data received in MSBs, discarding.");
                result &= 0xf;
                warned = true;
            }

            if (len > 1)
            {
                var b = payload[1];
                if (!warned && (b & 0xf) > 0)
                {
                    logger?.LogWarning(
                        "Invalid data received in MSBs, discarding.");
                    b &= 0xf;
                }

                result += (byte)(b << 4);
            }

            return true;
        }
    }
}
