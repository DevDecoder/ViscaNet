// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;

namespace DevDecoder.ViscaNet.Commands
{
    public sealed class InquiryResponse<T> : Response
    {
        /// <summary>
        ///     The response was completely unknown (could not even retrieve a device id).
        /// </summary>
        public new static readonly InquiryResponse<T> Unknown = Get(ResponseType.Unknown, default!, 0);

        /// <inheritdoc />
        private InquiryResponse(uint data, [MaybeNull] T response) : base(data)
            => Result = response;

        /// <summary>
        ///     Gets the response as an object.
        /// </summary>
        /// <value>The response.</value>
        public override object? ResultObject => Result;

        /// <summary>
        ///     Gets the response as an object.
        /// </summary>
        /// <value>The response.</value>
        [MaybeNull]
        public T Result { get; }

        /// <summary>
        ///     Returns true if this is a valid inquiry response.
        /// </summary>
        /// <value><see langword="true" /> if this instance is valid; otherwise, <see langword="false" />.</value>
        public override bool IsValid => Type == ResponseType.Inquiry;

        public static InquiryResponse<T> Get(ResponseType type, [MaybeNull] T result, byte deviceId = 1)
            => new InquiryResponse<T>(GetData(type, deviceId, 0), result);

        public static InquiryResponse<T> Get(T result, byte deviceId = 1)
            => new InquiryResponse<T>(GetData(ResponseType.Inquiry, deviceId, 0), result);
    }
}
