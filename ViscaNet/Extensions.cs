// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;

namespace ViscaNet
{
    public static class Extensions
    {
        public static string ToHex(this byte[] byteArray) => byteArray is null
            ? "null"
            : byteArray.Length < 1
                ? "empty"
                : string.Join(" ", byteArray.Select(b => b.ToString("X2")));
    }
}
