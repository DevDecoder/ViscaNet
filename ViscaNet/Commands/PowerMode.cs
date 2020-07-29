// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet.Commands
{
    public enum PowerMode : byte
    {
        On = 0x02,
        Standby = 0x03,
        Off = Standby,
        Unknown = 0xFF,
    }
}
