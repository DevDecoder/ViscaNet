// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using ViscaNet.Commands;

namespace ViscaNet
{
    internal class ViscaException : Exception
    {
        public ViscaException(ResponseType responseType, string name)
            : base(GetMessage(responseType, name))
        {
            Name = name;
            ResponseType = responseType;
        }

        public ResponseType ResponseType { get; }
        public string Name { get; }

        private static string GetMessage(ResponseType responseType, string name) =>
            responseType switch
            {
                ResponseType.ACK => $"An unexpected ACK was received from {name}.",
                ResponseType.Inquiry => $"An unexpected inquiry response was received from {name}.",
                ResponseType.Completion => $"An unexpected completion was received from {name}.",
                ResponseType.MessageLengthError => $"A message length error was received from {name}.",
                ResponseType.SyntaxError => $"A syntax error response was received from {name}.",
                ResponseType.BufferFull => $"A command buffer full response was received from {name}.",
                ResponseType.Canceled =>
                $"An unexpected command canceled response was received from {name}.",
                ResponseType.NoSocket =>
                $"A response was received from {name} that indicates there was no appropriate socket command to cancel.",
                ResponseType.NotExecutable =>
                $"A response was received from {name} indicating that the command could not be executed at this time.",
                _ => $"An unknown response was received from {name}."
            };
    }
}
