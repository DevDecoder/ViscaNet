// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Extensions.Logging;

namespace ViscaNet.Commands
{
    public delegate bool TryParseInquiryResponseDelegate<T>(ReadOnlySpan<byte> payload, out T result, ILogger? logger);
}
