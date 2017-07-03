using System;

namespace Jazz2.Compatibility
{
    [Flags]
    public enum JJ2Version : ushort
    {
        BaseGame = 0x0001,
        TSF = 0x0002,
        HH = 0x0004,
        CC = 0x0008,

        PlusExtension = 0x0100,

        Unknown = 0x0000,
        All = 0xffff
    }
}