// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using DynamicData.Aggregation;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public partial class Command
    {
        public string Name { get; }
        public CommandType Type { get; }
        private readonly byte[] _payload;

        public Command(string name, params byte[] payload)
            : this(CommandType.Command, name, payload)
        {
        }

        protected Command(CommandType type, string name, params byte[] payload)
        {
            Name = name;
            Type = type;
            _payload = payload;
        }

        internal virtual Response UnknownResponse => Response.Unknown;

        public virtual int MessageSize => _payload.Length + 3;

        public virtual void WriteMessage(Span<byte> buffer, byte deviceId = 1)
        {
            if (deviceId > 7)
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId, $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");

            var len = _payload.Length;
            
            buffer[0] = (byte)(0x80 + deviceId);
            buffer[1]= (byte)Type;

            if (len > 0)
            {
                for (var i = 0; i < len; i++)
                {
                    buffer[i + 2] = _payload[i];
                }
            }

            // Terminator
            buffer[len + 2] = 0xff;
        }

        public Response GetResponse(ReadOnlySpan<byte> response, ILogger? logger = null)
        => DoGetResponse(response, logger);

        protected virtual Response DoGetResponse(ReadOnlySpan<byte> response, ILogger? logger)
        {
            var length = response.Length;
            if (length < 1)
            {
                logger?.LogError($"The response was empty.");
                return Response.Unknown;
            }

            // Calculate Device ID
            var deviceId = response[0];
            if ((deviceId & 0xf) > 0)
            {
                logger?.LogError($"The response's device byte '{deviceId:X2}' was invalid.");
                return Response.Unknown;
            }
            deviceId = (byte)(deviceId >> 4);
            if (deviceId < 8)
            {
                logger?.LogError($"The response's device id '{deviceId - 8}' was invalid, as it must be greater than 0 (usually 1).");
                return Response.Unknown;
            }
            deviceId -= 8;

            if (length < 2)
            {
                logger?.LogError(
                    $"The response's length '{length}' was too short.");
                return Response.Get(ResponseType.Unknown, deviceId, 0);
            }

            // Calculate Socket
            var b = response[1];
            var socket = (byte)(b & 0xf);

            if (length < 3)
            {
                logger?.LogError(
                    $"The response's length '{length}' was too short.");
                return Response.Get(ResponseType.Unknown, deviceId, socket);
            }

            // Validate terminator
            var end = response[^1];
            if (end != 0xFF)
            {
                logger?.LogError(
                    $"The response's last byte '{end:X2}' was not a termination '0xFF'.");
                return Response.Get(ResponseType.Unknown, deviceId, socket);
            }

            // Calculate Response Type
            ResponseType type;
            if ((b & 0xF0) == 0x50)
            {
                if (response[2] == 0xFF)
                {
                    type = ResponseType.Completion;
                }
                else
                {
                    type = ResponseType.Inquiry;
                    if (Type != CommandType.Inquiry)
                    {
                        logger?.LogError(
                            $"The '{type}' response was not expected for the '{Type}' type.");
                        return Response.Get(ResponseType.Unknown, deviceId, socket);
                    }
                }
            }
            else
            {
                // look ahead
                var t = (ushort)(((b & 0xf0) << 8) + response[2]);

                if (!Enum.IsDefined(typeof(ResponseType), t))
                {
                    logger?.LogError(
                        $"The response's type '{t:x4}' is unknown.");
                    return Response.Get(ResponseType.Unknown, deviceId, socket);
                }

                type = (ResponseType)t;
            }

            switch (length)
            {
                case 3:
                    switch (type)
                    {
                        case ResponseType.ACK:
                        case ResponseType.Completion:
                            if (Type != CommandType.Command)
                            {
                                logger?.LogError(
                                    $"The '{type}' response was not expected for the '{Type}' type.");
                                return Response.Get(ResponseType.Unknown, deviceId, socket);
                            }

                            return Response.Get(type, deviceId, socket);
                        default:
                            // Sanity-check: Can never be reached
                            logger?.LogError(
                                $"The response's length '{length}' was too short for it's type '{type}'.");
                            return Response.Get(ResponseType.Unknown, deviceId, socket);
                    }

                case 4:
                    switch (type)
                    {
                        case ResponseType.SyntaxError:
                        case ResponseType.BufferFull:
                        case ResponseType.Inquiry:
                            if (socket != 0)
                            {
                                logger?.LogWarning(
                                    $"The '{type}' response should not specify a socket, but specified '{socket}'.");
                            }

                            return Response.Get(type, deviceId, 0);
                        case ResponseType.MessageLengthError:
                        case ResponseType.NoSocket:
                        case ResponseType.NotExecutable:
                            return Response.Get(type, deviceId, socket);
                        case ResponseType.Canceled:
                            if (Type != CommandType.Cancel)
                            {
                                logger?.LogError(
                                    $"The '{type}' response was not expected for the '{Type}' type.");
                                return Response.Get(ResponseType.Unknown, deviceId, socket);
                            }

                            return Response.Get(type, deviceId, socket);
                        default:
                            logger?.LogError(
                                $"The response's length '{length}' was invalid for it's type '{type}'.");
                            return Response.Get(ResponseType.Unknown, deviceId, socket);
                    }

                default:
                    // Only inquiries have > 4 byte responses!
                    if (type != ResponseType.Inquiry)
                    {
                        logger?.LogError(
                            $"The response's length '{length}' was invalid for it's type '{type}'.");
                        return Response.Get(ResponseType.Unknown, deviceId, socket);
                    }

                    // We have an inquiry response
                    return Response.Get(ResponseType.Inquiry, deviceId, socket);
            }
        }
    }
}
