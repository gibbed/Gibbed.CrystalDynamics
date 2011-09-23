using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.CrystalDynamics.FileFormats.Big
{
    [Flags]
    public enum Locale : uint
    {
        Default = 0xFFFFFFFF,
        UnusedFlags = 0xFFFFE000,
        UnusedLanguages = 0x00001D60,
        English = 1 << 0,
        French = 1 << 1,
        German = 1 << 2,
        Italian = 1 << 3,
        Spanish = 1 << 4,
        Japanese = 1 << 5,
        Portugese = 1 << 6,
        Polish = 1 << 7,
        EnglishUK = 1 << 8,
        Russian = 1 << 9,
        Czech = 1 << 10,
        Dutch = 1 << 11,
        Hungarian = 1 << 12,
    }
}
