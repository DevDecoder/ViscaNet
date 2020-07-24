// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ViscaNet.Commands
{
    public class CancelCommand : Command
    {
        private static readonly ConcurrentDictionary<byte, CancelCommand> s_cache =
            new ConcurrentDictionary<byte, CancelCommand>();

        public byte Socket { get; }
        
        /// <inheritdoc />
        private CancelCommand(byte socket) : base(CommandType.Cancel, $"Cancel Socket {socket}")
        {
            Socket = socket;
        }

        /// <inheritdoc />
        public override IEnumerable<byte> GetMessage(byte deviceId = 1)
        {
            if (deviceId > 7)
                throw new ArgumentOutOfRangeException(nameof(deviceId), deviceId,
                    $"The device id '{deviceId}' must be between 0 and 7, usually it should be 1 for Visca over IP.");
            yield return (byte)(0x80 + deviceId);
            yield return (byte)(Type + Socket);
            yield return 0xFF;
        }

        public static CancelCommand Get(byte socket) => socket > 0xf
            ? throw new ArgumentOutOfRangeException(nameof(socket), socket,
                $"The socket '{socket}' must be between 0x0 and 0xf.")
            : s_cache.GetOrAdd(socket, s => new CancelCommand(s));
    }
}
