// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public class InquiryCommand<T> : ViscaCommand
    {
        private readonly int _payloadSize;
        private readonly TryParseInquiryResponseDelegate<T> _tryParseResponseDelegate;

        public InquiryCommand(string name, int payloadSize, TryParseInquiryResponseDelegate<T> tryParseResponseDelegate, params byte[] payload)
            : base(ViscaCommandType.Inquiry, name, payload)
        {
            _payloadSize = payloadSize;
            _tryParseResponseDelegate = tryParseResponseDelegate;
        }

        internal virtual ViscaResponse UnknownResponse => InquiryResponse<T>.Unknown;

        public new InquiryResponse<T> GetResponse(byte[] response, int offset = 0, int count = -1, ILogger? logger = null)
            => (InquiryResponse<T>)DoGetResponse(response, ref offset, ref count, logger);

        protected override ViscaResponse DoGetResponse(byte[] response, ref int offset, ref int count, ILogger? logger)
        {
            var baseResponse = base.DoGetResponse(response, ref offset, ref count, logger);
            var deviceId = baseResponse.DeviceId;
            var type = baseResponse.Type;
            if (type != ViscaResponseType.Inquiry)
            {
                // All other responses are effectively errors and should already have a log.
                return InquiryResponse<T>.Get(type, deviceId, baseResponse.Socket);
            }

            // Remove first two bytes, and final byte from count
            count--;

            // Check remaining payload length
            if (count != _payloadSize)
            {
                logger?.LogError(
                    $"The inquiry response's payload length '{count}' is invalid, should be '{_payloadSize}'.");
                return InquiryResponse<T>.Get(ViscaResponseType.Unknown, deviceId);
            }

            // Finally parse payload
            return !_tryParseResponseDelegate(response, offset, count, out var result, logger)
                // Trust the delegate to log any failure reasons
                ? InquiryResponse<T>.Get(ViscaResponseType.Unknown, deviceId)
                : InquiryResponse<T>.Get(result, deviceId);
        }
    }
}
