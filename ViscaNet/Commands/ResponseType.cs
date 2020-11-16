// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace DevDecoder.ViscaNet.Commands
{
    /// <summary>
    ///     Enum ViscaResponseType indicates the type of a response from a Visca camera.
    /// </summary>
    public enum ResponseType : ushort
    {
        /// <summary>
        ///     The response type could not be calculated.
        /// </summary>
        Unknown = 0xFFFF,

        /// <summary>
        ///     The acknowledgement response.
        /// </summary>
        ACK = 0x40FF,

        /// <summary>
        ///     The inquiry completion response.
        /// </summary>
        Inquiry = 0x5000,

        /// <summary>
        ///     The completion response.
        /// </summary>
        Completion = 0x50FF,

        /// <summary>
        ///     The message length error response.
        /// </summary>
        MessageLengthError = 0x6001,

        /// <summary>
        ///     The syntax error response.
        /// </summary>
        SyntaxError = 0x6002,

        /// <summary>
        ///     The command buffer full response.
        /// </summary>
        BufferFull = 0x6003,

        /// <summary>
        ///     The command canceled response.
        /// </summary>
        Canceled = 0x6004,

        /// <summary>
        ///     The no socket response.
        /// </summary>
        NoSocket = 0x6005,

        /// <summary>
        ///     The command not executable response, occurs when the device is too busy,
        ///     or unable to execute a command due to it's current mode (e.g. auto focus).
        /// </summary>
        NotExecutable = 0x6041
    }
}
