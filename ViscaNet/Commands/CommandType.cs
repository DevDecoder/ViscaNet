// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace DevDecoder.ViscaNet.Commands
{
    public enum CommandType : byte
    {
        Command = 0x01,
        Inquiry = 0x09,
        Cancel = 0x20
    }
}
