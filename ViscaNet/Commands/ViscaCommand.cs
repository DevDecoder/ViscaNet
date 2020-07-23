// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public partial class ViscaCommand
    {
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

        internal virtual ViscaResponse UnknownResponse => ViscaResponse.Unknown;

        public virtual IEnumerable<byte> GetMessage(byte deviceId = 1)
        {
            if (deviceId > 7)
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");
            yield return (byte)(0x80 + deviceId);
            yield return (byte)Type;

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
        => DoGetResponse(response, ref offset, ref count, logger);

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
                    $"The response's length '{count + 1}' was too short.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, 0);
            }

            // Calculate Socket
            var b = response[offset++];
            count--;
            var socket = (byte)(b & 0xf);

            if (count < 1)
            {
                logger?.LogError(
                    $"The response's length '{count + 2}' was too short.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
            }

            // Calculate Response Type
            ViscaResponseType type;
            if (b == 0x50)
            {
                // Normally we get back a 0x51 for completions, but the IFClear command uses broadcast device 0, so actually
                // return 0x50 0xFF for completion, so we disambiguate here.
                if (response[offset] == 0xFF)
                {
                    type = ViscaResponseType.Completion;
                }
                else
                {
                    type = ViscaResponseType.Inquiry;
                    if (Type != ViscaCommandType.Inquiry)
                    {
                        logger?.LogError(
                            $"The '{type}' response was not expected for the '{Type}' type.");
                        return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                    }
                }
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
            var end = response[(offset + count) - 1];
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
                        if (Type != ViscaCommandType.Command)
                        {
                            logger?.LogError(
                                $"The '{type}' response was not expected for the '{Type}' type.");
                            return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                        }
                        return ViscaResponse.Get(type, deviceId, socket);
                    default:
                        // Sanity-check: Can never be reached
                        logger?.LogError(
                            $"The response's length '{count + 2}' was too short for it's type '{type}'.");
                        return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                }
            }

            if (count == 2)
            {
                switch (type)
                {
                    case ViscaResponseType.SyntaxError:
                    case ViscaResponseType.BufferFull:
                    case ViscaResponseType.Inquiry:
                        if (socket != 0)
                        {
                            logger?.LogWarning(
                                $"The '{type}' response should not specify a socket, but specified '{socket}'.");
                        }
                        return ViscaResponse.Get(type, deviceId, 0);
                    case ViscaResponseType.MessageLengthError:
                    case ViscaResponseType.NoSocket:
                    case ViscaResponseType.NotExecutable:
                        return ViscaResponse.Get(type, deviceId, socket);
                    case ViscaResponseType.Canceled:
                        if (Type != ViscaCommandType.Cancel)
                        {
                            logger?.LogError(
                                $"The '{type}' response was not expected for the '{Type}' type.");
                            return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                        }
                        return ViscaResponse.Get(type, deviceId, socket);
                    default:
                        logger?.LogError(
                            $"The response's length '{count + 2}' was invalid for it's type '{type}'.");
                        return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
                }
            }

            // Only inquiries have > 4 byte responses!
            if (type != ViscaResponseType.Inquiry)
            {
                logger?.LogError(
                    $"The response's length '{count + 2}' was invalid for it's type '{type}'.");
                return ViscaResponse.Get(ViscaResponseType.Unknown, deviceId, socket);
            }
            
            // We have an inquiry response
            return ViscaResponse.Get(ViscaResponseType.Inquiry, deviceId, socket);
        }
    }
}
