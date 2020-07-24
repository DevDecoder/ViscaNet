// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using System;

namespace ViscaNet.Commands
{
    public partial class Command
    {
        public static readonly Command IFClear = new Command("Clear Command Buffers", 0x00, 0x01);
        public static readonly Command Reset = new Command("Reset", 0x06, 0x05);

        public static readonly Command PowerOn = new Command("Power On", 0x04, 0x00, 0x02);
        public static readonly Command PowerOff = new Command("Power Off", 0x04, 0x00, 0x03);
        public static readonly Command Home = new Command("Home", 0x06, 0x04);


        public static Command Cancel(byte socket = 1) => CancelCommand.Get(socket);
    }
}
