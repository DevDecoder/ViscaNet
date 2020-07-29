// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet.Commands
{
    public static class ViscaCommands
    {
        public static readonly Command IFClear = Command.Register("Clear Command Buffers", 0x00, 0x01);
        public static readonly Command Reset = Command.Register("Reset", 0x06, 0x05);
        public static readonly Command PowerOn = Command.Register("Power On", 0x04, 0x00, 0x02);
        public static readonly Command PowerOff = Command.Register("Power Off", 0x04, 0x00, 0x03);
        public static readonly Command Home = Command.Register("Home", 0x06, 0x04);
        public static Command Cancel(byte socket = 1) => CancelCommand.Get(socket);
    }
}
