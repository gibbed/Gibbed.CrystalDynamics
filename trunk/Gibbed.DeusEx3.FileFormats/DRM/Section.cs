﻿/* Copyright (c) 2011 Rick (rick 'at' gibbed 'dot' us)
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

using System.IO;

namespace Gibbed.DeusEx3.FileFormats.DRM
{
    public class Section
    {
        public uint NameHash;
        public byte Unknown05;
        public ushort Unknown06;
        public uint Unknown10;

        public SectionType Type;
        public SectionResolver Resolver = null;
        public MemoryStream Data = null;

        public override string ToString()
        {
            return string.Format("{0} : {1}",
                this.Type,
                this.Data == null ? 0 : this.Data.Length);
        }
    }
}
