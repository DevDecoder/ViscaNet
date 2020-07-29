// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet.Commands
{
    public enum FocusMode : byte
    {
        Auto = 0x02,
        Manual = 0x03,
        OnePush = 0x04,
        Unknown = 0xFF
    }
}
