// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet
{
    /// <summary>
    ///     Enum ViscaResponseType indicates the type of a response from a Visca camera.
    /// </summary>
    public enum ViscaResponseType
    {
        /// <summary>
        ///     The response type could not be calculated.
        /// </summary>
        Unknown = 0,

        /// <summary>
        ///     The acknowledgement response.
        /// </summary>
        ACK,

        /// <summary>
        ///     The inquiry completion response.
        /// </summary>
        InquiryResponse,

        /// <summary>
        ///     The completion response.
        /// </summary>
        Completion,

        /// <summary>
        ///     The message length error response.
        /// </summary>
        MessageLengthError,

        /// <summary>
        ///     The syntax error response.
        /// </summary>
        SyntaxError,

        /// <summary>
        ///     The command buffer full response.
        /// </summary>
        CommandBufferFull,

        /// <summary>
        ///     The command canceled response.
        /// </summary>
        CommandCanceled,

        /// <summary>
        ///     The no socket response.
        /// </summary>
        NoSocket,

        /// <summary>
        ///     The command not executable response, occurs when the device is too busy,
        ///     or unable to execute a command due to it's current mode (e.g. auto focus).
        /// </summary>
        CommandNotExecutable
    }
}
