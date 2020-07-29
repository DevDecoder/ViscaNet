using System;

namespace ViscaNet.Commands
{
    public enum PowerMode : byte
    {
        On = 0x02,
        Standby = 0x03,
        Off=Standby,
        Unknown = 0xFF,
    }
}
