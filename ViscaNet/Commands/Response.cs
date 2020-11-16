// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace DevDecoder.ViscaNet.Commands
{
    public class Response
    {
        private static readonly ConcurrentDictionary<uint, Response> s_cache =
            new ConcurrentDictionary<uint, Response>();

        /// <summary>
        ///     The response was completely unknown (could not even retrieve a device id).
        /// </summary>
        public static readonly Response Unknown = Get(ResponseType.Unknown, 0, 0);

        private readonly uint _data;

        protected Response(uint data) => _data = data;

        public ResponseType Type => (ResponseType)(_data & 0xFFFF);
        public byte DeviceId => (byte)((_data >> 16) & 0xFF);
        public byte Socket => (byte)((_data >> 24) & 0xFF);

        /// <summary>
        ///     Gets the response, if an <see cref="InquiryResponse{T}" />; otherwise <see langword="null" />.
        /// </summary>
        /// <value>The response.</value>
        [MaybeNull]
        public virtual object? ResultObject => null;

        /// <summary>
        ///     Returns true if this is a valid command response.
        /// </summary>
        /// <value><see langword="true" /> if this instance is valid; otherwise, <see langword="false" />.</value>
        public virtual bool IsValid
            => Type == ResponseType.Completion ||
               Type == ResponseType.Canceled;

        public static Response Get(ResponseType type, byte deviceId = 1, byte socket = 1)
            => s_cache.GetOrAdd(GetData(type, deviceId, socket), d => new Response(d));

        protected static uint GetData(ResponseType type, byte deviceId = 1, byte socket = 1)
        {
            if (deviceId > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId,
                    $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");
            }

            if (socket > 0xf)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId,
                    $"The socket '{socket}' must be between 0 and 0xf (15) for a cancel command.");
            }

            return (uint)((socket << 24) + (deviceId << 16) + (ushort)type);
        }
    }
}
