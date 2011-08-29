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
using System.Collections.Generic;
using System.IO;
using Gibbed.IO;

namespace Gibbed.DeusEx3.FileFormats.DRM
{
    public class SectionResolver
    {
        // ((key & 0xFFFFFFFF00000000) >> 32) = dest data
        // ((key & 0x00000000FFFFFFFF) >>  0) = source data
        // updates pointer at data[key] to &data[value]
        public List<ulong> Unknown0 = new List<ulong>();

        // ((key & 0x0000000000003FFF) >>  0)     = source section index
        // ((key & 0x0000003FFFFFC000) >> 14) * 4 = data dest
        // ((key & 0xFFFFFFC000000000) >> 38) ?   = ???
        public List<ulong> Unknown1 = new List<ulong>();

        public List<uint> Unknown2 = new List<uint>();
        public List<uint> Unknown3 = new List<uint>();

        // ((key & 0x01FFFFFF) >>  0) * 4 = data dest
        // ((key & 0xFE000000) >> 25)     = section type
        // updates pointer at dest by finding the section with the hash at data dest with the type specified
        public List<uint> Unknown4 = new List<uint>();

        public void Deserialize(Stream input, bool littleEndian)
        {
            if (input.Length < 20)
            {
                throw new FormatException("bad section header size?");
            }

            var count0 = input.ReadValueU32(littleEndian);
            var count1 = input.ReadValueU32(littleEndian);
            var count2 = input.ReadValueU32(littleEndian);
            var count3 = input.ReadValueU32(littleEndian);
            var count4 = input.ReadValueU32(littleEndian);

            this.Unknown0.Clear();
            for (uint i = 0; i < count0; i++)
            {
                var a = input.ReadValueU64();
                this.Unknown0.Add(a);
            }

            this.Unknown1.Clear();
            for (uint i = 0; i < count1; i++)
            {
                var a = input.ReadValueU64();
                this.Unknown1.Add(a);
            }

            this.Unknown2.Clear();
            for (uint i = 0; i < count2; i++)
            {
                var a = input.ReadValueU32();
                this.Unknown2.Add(a);
            }

            this.Unknown3.Clear();
            for (uint i = 0; i < count3; i++)
            {
                var a = input.ReadValueU32();
                this.Unknown3.Add(a);
            }

            this.Unknown4.Clear();
            for (uint i = 0; i < count4; i++)
            {
                var a = input.ReadValueU32();
                this.Unknown4.Add(a);
            }
        }
    }
}
