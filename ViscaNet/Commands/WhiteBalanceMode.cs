// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace DevDecoder.ViscaNet.Commands
{
    public enum WhiteBalanceMode
    {
        Auto = 0x00,
        Indoor = 0x01,
        K3000 = Indoor,
        Outdoor = 0x02,
        K4000 = Outdoor,
        OnePush = 0x03,
        K5000 = 0x04,
        Manual = 0x05,
        K6500 = 0x06,
        K3500 = 0x07,
        K4500 = 0x08,
        K5500 = 0x09,
        K6000 = 0x0A,
        K7000 = 0x0B,
        Unknown = 0xFF
    }
}
