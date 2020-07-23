// Licensed under the Apache License, Version 2.0 (the "License").
// See the LICENSE file in the project root for more information.

using Microsoft.Extensions.Logging;

namespace ViscaNet
{
    public partial class ViscaCommand
    {
        public static readonly ViscaCommand Cancel = new ViscaCommand(ViscaCommandType.Cancel, "Cancel");

        public static readonly ViscaCommand PowerOn = new ViscaCommand("Power On", 0x04, 0x00, 0x02);
        public static readonly ViscaCommand PowerOff = new ViscaCommand("Power Off", 0x04, 0x00, 0x03);
        public static readonly ViscaCommand Home = new ViscaCommand("Home", 0x06, 0x04);
        public static readonly ViscaCommand Reset = new ViscaCommand("Reset", 0x06, 0x05);
    }
}
