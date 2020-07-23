// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace ViscaNet
{
    public sealed class InquiryResponse<T> : ViscaResponse
    {
        private static readonly ConcurrentDictionary<uint, InquiryResponse<T>> s_cache =
            new ConcurrentDictionary<uint, InquiryResponse<T>>();

        /// <summary>
        /// The response was completely unknown (could not even retrieve a device id).
        /// </summary>
        public new static readonly InquiryResponse<T> Unknown = Get(ViscaResponseType.Unknown, 0, 0);

        /// <summary>
        /// Gets the response as an object.
        /// </summary>
        /// <value>The response.</value>
        public override object? ResponseObject => Response;

        /// <summary>
        /// Gets the response as an object.
        /// </summary>
        /// <value>The response.</value>
        [MaybeNull]
        public T Response { get; }

        /// <summary>
        /// Returns true if this is a valid inquiry response.
        /// </summary>
        /// <value><see langword="true" /> if this instance is valid; otherwise, <see langword="false" />.</value>
        public override bool IsValid => Type == ViscaResponseType.Inquiry;

        /// <inheritdoc />
        private InquiryResponse(uint data, [MaybeNull] T response) : base(data) 
            => Response = response;

        public new static InquiryResponse<T> Get(ViscaResponseType type, byte deviceId = 1, byte socket = 0)
            => s_cache.GetOrAdd(GetData(type, deviceId, socket), d => new InquiryResponse<T>(d, default!));

        public static InquiryResponse<T> Get(T result, byte deviceId = 1)
            => new InquiryResponse<T>(GetData(ViscaResponseType.Inquiry, deviceId, 0), result);
    }
}
