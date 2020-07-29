// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;

namespace ViscaNet.Commands
{
    public class CancelCommand : Command
    {
        private static readonly ConcurrentDictionary<byte, CancelCommand> s_cache =
            new ConcurrentDictionary<byte, CancelCommand>();

        /// <inheritdoc />
        private CancelCommand(byte socket) : base(CommandType.Cancel, $"Cancel Socket {socket}") => Socket = socket;

        public byte Socket { get; }

        /// <inheritdoc />
        public override int MessageSize => 3;

        /// <inheritdoc />
        public override void WriteMessage(Span<byte> buffer, byte deviceId = 1)
        {
            if (deviceId > 7)
            {
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId,
                    $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");
            }

            buffer[0] = (byte)(0x80 + deviceId);
            buffer[1] = (byte)(Type + Socket);
            buffer[2] = 0xFF;
        }

        public static CancelCommand Get(byte socket) => socket > 0xf
            ? throw new ArgumentOutOfRangeException(nameof(socket), socket,
                $"The socket '{socket}' must be between 0x0 and 0xf.")
            : s_cache.GetOrAdd(socket, s => new CancelCommand(s));
    }
}
