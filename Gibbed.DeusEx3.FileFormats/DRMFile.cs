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
using System.Text;
using Gibbed.IO;

namespace Gibbed.DeusEx3.FileFormats
{
    public class DRMFile
    {
        public uint Version;
        public bool LittleEndian;
        public uint Flags;

        public List<DRM.Section> Sections
            = new List<DRM.Section>();

        public List<string> Unknown08s = new List<string>();
        public List<string> Unknown04s = new List<string>();

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(false);
            input.Seek(-4, SeekOrigin.Current);

            if (magic == CDRMFile.Magic)
            {
                input = CDRMFile.Decompress(input);
            }

            if (input.Position + 32 > input.Length)
            {
                throw new FormatException("not enough data for header");
            }

            var version = input.ReadValueU32();
            if (version != 21 && version.Swap() != 21)
            {
                throw new FormatException();
            }

            this.LittleEndian = version == 21;
            this.Version = this.LittleEndian == true ? version : version.Swap();

            var unknown04_Size = input.ReadValueU32(this.LittleEndian);
            var unknown08_Size = input.ReadValueU32(this.LittleEndian);
            var unknown0C = input.ReadValueU32(this.LittleEndian); // extra data after first block?
            var unknown10 = input.ReadValueU32(this.LittleEndian);
            this.Flags = input.ReadValueU32(this.LittleEndian);
            var sectionCount = input.ReadValueU32(this.LittleEndian);
            var unknown1C_Count = input.ReadValueU32(this.LittleEndian);

            if (unknown0C != 0)
            {
                throw new FormatException();

                if ((this.Flags & 1) != 0)
                {
                    input.Seek(input.Position.Align(16), SeekOrigin.Begin);
                }
            }

            var sectionHeaders = new DRM.SectionHeader[sectionCount];
            for (uint i = 0; i < sectionCount; i++)
            {
                sectionHeaders[i] = new DRM.SectionHeader();
                sectionHeaders[i].Deserialize(input, this.LittleEndian);
            }

            this.Unknown08s.Clear();
            using (var unknown08_Data = input.ReadToMemoryStream(unknown08_Size))
            {
                while (unknown08_Data.Position < unknown08_Data.Length)
                {
                    this.Unknown08s.Add(unknown08_Data.ReadStringZ(Encoding.ASCII));
                }
            }

            this.Unknown04s.Clear();
            using (var unknown04_Data = input.ReadToMemoryStream(unknown04_Size))
            {
                while (unknown04_Data.Position < unknown04_Data.Length)
                {
                    this.Unknown04s.Add(unknown04_Data.ReadStringZ(Encoding.ASCII));
                }
            }

            if ((this.Flags & 1) != 0)
            {
                input.Seek(input.Position.Align(16), SeekOrigin.Begin);
            }

            var sections = new DRM.Section[sectionCount];
            for (int i = 0; i < sectionCount; i++)
            {
                var sectionHeader = sectionHeaders[i];

                var section = new DRM.Section();
                section.NameHash = sectionHeader.NameHash;
                section.Type = sectionHeader.Type;
                section.Unknown05 = sectionHeader.Unknown05;
                section.Unknown06 = sectionHeader.Unknown06;
                section.Unknown10 = sectionHeader.Unknown10;

                if ((sectionHeader.Unknown05 & 1) != 0)
                {
                    throw new NotImplementedException();
                }

                if (section.Type == DRM.SectionType.RenderResource)
                {
                }

                if (sectionHeader.HeaderSize > 0)
                {
                    using (var buffer = input.ReadToMemoryStream(sectionHeader.HeaderSize))
                    {
                        var resolver = new DRM.SectionResolver();
                        resolver.Deserialize(buffer, this.LittleEndian);
                        section.Resolver = resolver;
                    }
                }

                if ((this.Flags & 1) != 0)
                {
                    input.Seek(input.Position.Align(16), SeekOrigin.Begin);
                }

                if (sectionHeader.DataSize > 0)
                {
                    section.Data = input.ReadToMemoryStream(sectionHeader.DataSize);
                }
                else
                {
                    section.Data = null;
                }

                if ((this.Flags & 1) != 0)
                {
                    input.Seek(input.Position.Align(16), SeekOrigin.Begin);
                }

                sections[i] = section;
            }

            this.Sections.Clear();
            this.Sections.AddRange(sections);
        }
    }
}
