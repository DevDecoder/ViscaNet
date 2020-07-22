// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ViscaNet
{
    public class ViscaCommand
    {
        public static readonly ViscaCommand Cancel = new ViscaCommand(ViscaCommandType.Cancel, "Cancel");

        #region Simple Commands
        public static readonly ViscaCommand PowerOn = new ViscaCommand("Power On", 0x04, 0x00, 0x02);
        public static readonly ViscaCommand PowerOff = new ViscaCommand("Power Off", 0x04, 0x00, 0x03);
        public static readonly ViscaCommand Home = new ViscaCommand("Home", 0x06, 0x04);
        public static readonly ViscaCommand Reset = new ViscaCommand("Reset", 0x06, 0x05);
        #endregion

        #region Inquiries

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
                        $"Invalid zoom data received in MSBs: {b1:X2} {b2:X2} {b3:X2} {b4:X2}.");
                    result = double.NaN;
                    return false;
                }

                // Combine LSBs into single ushort (note no need to mask with 0x0f as above check has ensured MSBs are 0)
                var raw = (ushort)((b1 << 12) +
                                   (b2 << 8) +
                                   (b3 << 4) +
                                   b4);

                if (raw > 0x4000)
                {
                    logger?.LogError(
                        $"Invalid zoom position received '0x{raw:x4}' > '0x4000'.");
                    result = double.NaN;
                    return false;
                }

                // Convert to value between 0 and 1.
                result = raw / 16384D;
                return true;
            }
        );
        #endregion

        public string Name { get; }
        public ViscaCommandType Type { get; }
        private readonly byte[] _payload;

        public ViscaCommand(string name, params byte[] payload)
            : this(ViscaCommandType.Command, name, payload)
        {
        }

        protected ViscaCommand(ViscaCommandType type, string name, params byte[] payload)
        {
            Name = name;
            Type = type;
            _payload = payload;
        }

        public IEnumerable<byte> GetMessage(byte deviceId = 1, byte socket = 1)
        {
            if (deviceId > 7)
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");
            yield return (byte)(0x80 + deviceId);

            if (Type == ViscaCommandType.Cancel)
            {
                if (socket > 0xf)
                    throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId,
                        $"The socket '{socket}' must be between 0 and 0xf (15) for a cancel command.");
                yield return (byte)(Type + socket);
            }
            else
            {
                yield return (byte)Type;
            }

            var payloadLength = _payload.Length;
            if (payloadLength > 0)
            {
                for (var i = 0; i < payloadLength; i++)
                {
                    yield return _payload[i];
                }
            }

            // Terminator
            yield return 0xFF;
        }

        public ViscaResponse GetResponse(byte[] response, int offset = 0, int count = -1, ILogger? logger = null)
        {
            var result = DoGetResponse(response, ref offset, ref count, logger);
            if (result.Type != ViscaResponseType.InquiryResponse || result.GetType() != typeof(ViscaResponse))
            {
                return result;
            }

            logger?.LogError(
                $"The {result.Type} is not a valid response type for a command.");
            return ViscaResponse.Get(ViscaResponseType.Unknown, result.DeviceId, result.Socket);
        }

        protected virtual ViscaResponse DoGetResponse(byte[] response, ref int offset, ref int count, ILogger? logger)
        {
            var payloadLength = response.Length;
            if (offset > payloadLength)
            {
                logger?.LogError($"The offset '{offset}' exceeds the response length '{payloadLength}'.");
                return ViscaResponse.Unknown;
            }

            if (count < 0)
            {
                count = payloadLength - offset;
            }
            else if ((offset + count) > payloadLength)
            {
                logger?.LogError(offset > 0
                    ? $"The offset '{offset}' plus count '{count}' exceeds the response length '{payloadLength}'."
                    : $"The count '{count}' exceeds the response length '{payloadLength}'.");
                return ViscaResponse.Unknown;
            }

            if (count < 1)
            {
                logger?.LogError($"The response was empty.");
                return ViscaResponse.Unknown;
            }

            // Calculate Device ID
            var deviceId = response[offset++];
            count--;
            if ((deviceId & 0xf) > 0)
            {
                logger?.LogError($"The response's device byte '{deviceId:X2}' was invalid.");
                return ViscaResponse.Unknown;
            }
            deviceId = (byte)(deviceId >> 4);
            if (deviceId < 8)
            {
                logger?.LogError($"The response's device id '{deviceId - 8}' was invalid, as it must be greater than 0 (usually 1).");
                return ViscaResponse.Unknown;
            }
            deviceId -= 8;

            if (count < 1)
            {
                logger?.LogError(
                    $"The response's length '{count+1}' was too short.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, 0);
            }

            // Calculate Socket
            var b = response[offset++];
            count--;
            var socket = (byte)(b & 0xf);

            if (count < 1)
            {
                logger?.LogError(
                    $"The response's length '{count+2}' was too short.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
            }

            // Calculate Response Type
            ViscaResponseType type;
            if (b == 0x50)
            {
                type = ViscaResponseType.InquiryResponse;
            }
            else
            {
                // look ahead
                var t = (ushort)(((b & 0xf0) << 8) + response[offset]);
                
                if (!Enum.IsDefined(typeof(ViscaResponseType), t))
                {
                    logger?.LogError(
                        $"The response's type '{t:x4}' is unknown.");
                    return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                }

                type = (ViscaResponseType)t;
            }

            // Validate terminator
            var end = response[offset+count-1];
            if (end != 0xFF)
            {
                logger?.LogError(
                    $"The response's last byte '{end:X2}' was not a termination '0xFF'.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
            }

            if (count == 1)
            {
                switch (type)
                {
                    case ViscaResponseType.ACK:
                    case ViscaResponseType.Completion:
                        return ViscaResponse.Get(type, deviceId, socket);
                    default:
                        logger?.LogError(
                            $"The response's length '{count+2}' was too short for it's type '{type}'.");
                        return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                }
            }

            if (count == 2)
            {

                switch (type)
                {
                    case ViscaResponseType.SyntaxError:
                    case ViscaResponseType.CommandBufferFull:
                    // Note this is only valid for an actual Inquiry Response
                    case ViscaResponseType.InquiryResponse:
                        if (socket != 0)
                        {
                            logger?.LogWarning(
                                $"The '{type}' response should not specify a socket, but specified '{socket}'.");
                        }
                        return ViscaResponse.Get(type, deviceId, 0);
                    case ViscaResponseType.MessageLengthError:
                    case ViscaResponseType.CommandCanceled:
                    case ViscaResponseType.NoSocket:
                    case ViscaResponseType.CommandNotExecutable:
                        return ViscaResponse.Get(type, deviceId, socket);
                    default:
                        logger?.LogError(
                            $"The response's length '{count+2}' was invalid for it's type '{type}'.");
                        return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                }
            }

            // Only inquiries have > 4 byte responses!
            if (type != ViscaResponseType.InquiryResponse)
            {
                logger?.LogError(
                    $"The response's length '{count+2}' was invalid for it's type '{type}'.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
            }

            // Note this is only valid for an actual Inquiry Response
            return ViscaResponse.Get(ViscaResponseType.InquiryResponse, deviceId, socket);
        }
    }
}
