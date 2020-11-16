// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace DevDecoder.ViscaNet.Commands
{
    public enum AutoExposureMode
    {
        FullAuto = 0x00,
        Manual = 0x03,
        ShutterPriority = 0x0A,
        IrisPriority = 0x0B,
        Bright = 0x0D,
        Unknown = 0xFF
    }
}
