// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public class InquiryCommand<T> : Command
    {
        private readonly int _payloadSize;
        private readonly TryParseInquiryResponseDelegate<T> _tryParseResponseDelegate;

        public InquiryCommand(string name, int payloadSize, TryParseInquiryResponseDelegate<T> tryParseResponseDelegate, params byte[] payload)
            : base(CommandType.Inquiry, name, payload)
        {
            _payloadSize = payloadSize;
            _tryParseResponseDelegate = tryParseResponseDelegate;
        }

        internal override Response UnknownResponse => InquiryResponse<T>.Unknown;

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
                return InquiryResponse<T>.Get(type, deviceId, baseResponse.Socket);
            }

            // Remove first 2 bytes, and last byte
            var payload = response.Slice(2, response.Length - 3);

            // Check remaining payload length
            if (payload.Length != _payloadSize)
            {
                logger?.LogError(
                    $"The inquiry response's payload length '{payload.Length}' is invalid, should be '{_payloadSize}'.");
                return InquiryResponse<T>.Get(ResponseType.Unknown, deviceId);
            }

            // Finally parse payload
            return !_tryParseResponseDelegate(payload, out var result, logger)
                // Trust the delegate to log any failure reasons
                ? InquiryResponse<T>.Get(ResponseType.Unknown, deviceId)
                : InquiryResponse<T>.Get(result, deviceId);
        }
    }
}
