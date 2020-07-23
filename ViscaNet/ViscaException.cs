using System;

namespace ViscaNet
{
    internal class ViscaException : Exception
    {
        public ViscaException(ViscaResponseType responseType, string name)
            : base(GetMessage(responseType, name))
        {
            Name = name;
            ResponseType = responseType;
        }

        public ViscaResponseType ResponseType { get; }
        public string Name { get; }

        private static string GetMessage(ViscaResponseType responseType, string name) =>
            responseType switch
            {
                ViscaResponseType.ACK => $"An unexpected ACK was received from {name}.",
                ViscaResponseType.Inquiry => $"An unexpected inquiry response was received from {name}.",
                ViscaResponseType.Completion => $"An unexpected completion was received from {name}.",
                ViscaResponseType.MessageLengthError => $"A message length error was received from {name}.",
                ViscaResponseType.SyntaxError => $"A syntax error response was received from {name}.",
                ViscaResponseType.BufferFull => $"A command buffer full response was received from {name}.",
                ViscaResponseType.Canceled =>
                $"An unexpected command canceled response was received from {name}.",
                ViscaResponseType.NoSocket =>
                $"A response was received from {name} that indicates there was no appropriate socket command to cancel.",
                ViscaResponseType.NotExecutable =>
                $"A response was received from {name} indicating that the command could not be executed at this time.",
                _ => $"An unknown response was received from {name}."
            };
    }
}