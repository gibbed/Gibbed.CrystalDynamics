/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 * 
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 * 
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;

namespace Gibbed.CrystalDynamics.FileFormats
{
    [Flags]
    public enum ArchiveLocale : uint
    {
        // ReSharper disable InconsistentNaming
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
        // ReSharper restore InconsistentNaming
    }
}
