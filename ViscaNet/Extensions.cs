// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

namespace ViscaNet
{
    public static class Extensions
    {
        public static string ToHex(this IEnumerable<byte> bytes, string format = "X2")
        {
            if (bytes is null) return "null";
            
            StringBuilder builder = new StringBuilder();
            var first = true;
            foreach (var b in bytes)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(" ");
                }

                builder.Append(b.ToString(format));
            }

            return first ? "empty" : builder.ToString();
        }
    }
}
