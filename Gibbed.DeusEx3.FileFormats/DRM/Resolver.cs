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
    public class Resolver
    {
        public List<Unknown0Resolver> Unknown0s = new List<Unknown0Resolver>();
        public List<Unknown1Resolver> Unknown1s = new List<Unknown1Resolver>();
        public List<Unknown2Resolver> Unknown2s = new List<Unknown2Resolver>();
        public List<uint> Unknown3s = new List<uint>();
        public List<Unknown4Resolver> Unknown4s = new List<Unknown4Resolver>();

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

            this.Unknown0s.Clear();
            for (uint i = 0; i < count0; i++)
            {
                var unknown = new Unknown0Resolver();
                unknown.Deserialize(input);
                this.Unknown0s.Add(unknown);
            }

            this.Unknown1s.Clear();
            for (uint i = 0; i < count1; i++)
            {
                var unknown = new Unknown1Resolver();
                unknown.Deserialize(input);
                this.Unknown1s.Add(unknown);
            }

            this.Unknown2s.Clear();
            for (uint i = 0; i < count2; i++)
            {
                var unknown = new Unknown2Resolver();
                unknown.Deserialize(input);
                this.Unknown2s.Add(unknown);
            }

            this.Unknown3s.Clear();
            for (uint i = 0; i < count3; i++)
            {
                var a = input.ReadValueU32();
                this.Unknown3s.Add(a);
            }

            this.Unknown4s.Clear();
            for (uint i = 0; i < count4; i++)
            {
                var unknown = new Unknown4Resolver();
                unknown.Deserialize(input);
                this.Unknown4s.Add(unknown);
            }
        }

        public class Unknown0Resolver
        {
            public uint Unknown0;
            public uint Unknown1;

            public void Deserialize(Stream input)
            {
                // ((key & 0xFFFFFFFF00000000) >> 32) = dest data
                // ((key & 0x00000000FFFFFFFF) >>  0) = source data
                // updates pointer at data[key] to &data[value]

                this.Unknown0 = input.ReadValueU32();
                this.Unknown1 = input.ReadValueU32();
            }

            public override string ToString()
            {
                return string.Format("{0:X4} {1:X8}",
                    this.Unknown0,
                    this.Unknown1);
            }
        }

        public class Unknown1Resolver
        {
            public ushort Unknown0;
            public uint Unknown1;
            public uint Unknown2;

            public void Deserialize(Stream input)
            {
                // ((key & 0x0000000000003FFF) >>  0)     = source section index
                // ((key & 0x0000003FFFFFC000) >> 14) * 4 = data dest
                // ((key & 0xFFFFFFC000000000) >> 38) ?   = ???
                
                var value = input.ReadValueU64();
                this.Unknown0 = (ushort)((value & 0x0000000000003FFF) >> 0);
                this.Unknown1 = (uint)(((value & 0x0000003FFFFFC000) >> 14) * 4);
                this.Unknown2 = (uint)(((value & 0xFFFFFFC000000000) >> 38));
            }

            public override string ToString()
            {
                return string.Format("{0:X4} {1:X8} {2:X8}",
                    this.Unknown0,
                    this.Unknown1,
                    this.Unknown2);
            }
        }

        public class Unknown2Resolver
        {
            public uint Unknown0;
            public byte Unknown1;

            public void Deserialize(Stream input)
            {
                // ((key & 0x01FFFFFF) >>  0) * 4 = data dest
                // ((key & 0xFE000000) >> 25)     = section type
                // updates pointer at dest by finding the section with the hash at data dest with the type specified
                var value = input.ReadValueU32();
                this.Unknown0 = (uint)(((value & 0x01FFFFFF) >> 0) * 4);
                this.Unknown1 = (byte)((value & 0xFE000000) >> 25);
            }

            public override string ToString()
            {
                return string.Format("{0:X8} : {1}", this.Unknown0, (SectionType)this.Unknown1);
            }
        }

        public class Unknown4Resolver
        {
            public uint Unknown0;
            public byte Unknown1;

            public void Deserialize(Stream input)
            {
                // ((key & 0x01FFFFFF) >>  0) * 4 = data dest
                // ((key & 0xFE000000) >> 25)     = section type
                // updates pointer at dest by finding the section with the hash at data dest with the type specified
                var value = input.ReadValueU32();
                this.Unknown0 = (uint)(((value & 0x01FFFFFF) >> 0) * 4);
                this.Unknown1 = (byte)((value & 0xFE000000) >> 25);
            }

            public override string ToString()
            {
                return string.Format("{0:X8} : {1}", this.Unknown0, (SectionType)this.Unknown1);
            }
        }
    }
}
