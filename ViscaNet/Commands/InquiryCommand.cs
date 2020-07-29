// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public class InquiryCommand<T> : Command
    {
        private readonly int _payloadSize;
        private readonly TryParseInquiryResponseDelegate<T> _tryParseResponseDelegate;

        protected InquiryCommand(string name, int payloadSize,
            TryParseInquiryResponseDelegate<T> tryParseResponseDelegate, params byte[] payload)
            : base(CommandType.Inquiry, name, payload)
        {
            _payloadSize = payloadSize;
            _tryParseResponseDelegate = tryParseResponseDelegate;
        }

        public new static IReadOnlyList<InquiryCommand<T>> All => Command.All.OfType<InquiryCommand<T>>().ToArray();

        internal override Response UnknownResponse => InquiryResponse<T>.Unknown;

        public static bool TryGet(string name, [MaybeNullWhen(false)] out InquiryCommand<T> command)
        {
            if (!Command.TryGet(name, out var c))
            {
                command = null;
                return false;
            }

            command = c as InquiryCommand<T>;
            return command != null;
        }

        public new static InquiryCommand<T>? Get(string name) => TryGet(name, out var command) ? command : null;

        public static InquiryCommand<T> Register(string name, int payloadSize,
            TryParseInquiryResponseDelegate<T> tryParseResponseDelegate, params byte[] payload) =>
            new InquiryCommand<T>(name, payloadSize, tryParseResponseDelegate, payload);

        public new InquiryResponse<T> GetResponse(ReadOnlySpan<byte> response, ILogger? logger = null)
            => (InquiryResponse<T>)DoGetResponse(response, logger);

        protected override Response DoGetResponse(ReadOnlySpan<byte> response, ILogger? logger)
        {
            var baseResponse = base.DoGetResponse(response, logger);
            var deviceId = baseResponse.DeviceId;
            var type = baseResponse.Type;
            if (type != ResponseType.Inquiry)
            {
                // All other responses are effectively errors and should already have a log.
                return Response.Get(type, deviceId, baseResponse.Socket);
            }

            // Remove first 2 bytes, and last byte
            var payload = response.Slice(2, response.Length - 3);

            // Check remaining payload length
            if (payload.Length != _payloadSize)
            {
                logger?.LogError(
                    $"The inquiry response's payload length '{payload.Length}' is invalid, should be '{_payloadSize}'.");
                return Response.Get(ResponseType.Unknown, deviceId);
            }

            // Finally parse payload
            return !_tryParseResponseDelegate(payload, out var result, logger)
                // Trust the delegate to log any failure reasons
                ? InquiryResponse<T>.Get(ResponseType.Unknown, result, deviceId)
                : InquiryResponse<T>.Get(result, deviceId);
        }
    }
}
