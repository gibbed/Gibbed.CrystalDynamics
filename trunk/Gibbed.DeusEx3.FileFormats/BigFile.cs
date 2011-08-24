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
using System.Linq;
using System.Text;
using Gibbed.IO;

namespace Gibbed.DeusEx3.FileFormats
{
    public class BigFile
    {
        public bool LittleEndian;
        public uint FileAlignment;
        public string BasePath;
        public List<Big.Entry> Entries
            = new List<Big.Entry>();

        public static int EstimateHeaderSize(int count)
        {
            return
                (4 + // file alignment
                64 + // base path
                4 + // count
                (4 * count) + // name hashes
                (16 * count)) // entry table
                .Align(2048); // aligned to 2048 bytes
        }

        public void Serialize(Stream output)
        {
            output.WriteValueU32(this.FileAlignment, this.LittleEndian);
            output.WriteString(this.BasePath, 64, Encoding.ASCII);
            output.WriteValueS32(this.Entries.Count, this.LittleEndian);

            var entries = this.Entries
                .OrderBy(e => e.Size)
                .OrderBy(e => e.NameHash);
            
            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.NameHash, this.LittleEndian);
            }

            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.Size, this.LittleEndian);
                output.WriteValueU32(entry.Offset, this.LittleEndian);
                output.WriteValueU32(entry.Locale, this.LittleEndian);
                output.WriteValueU32(entry.Unknown4, this.LittleEndian);
            }
        }

        public void Deserialize(Stream input)
        {
            var fileAlignment = input.ReadValueU32(true);
            if (fileAlignment != 0x7FF00000 &&
                fileAlignment != 0x0000F07F &&
                fileAlignment != 0x62300000 &&
                fileAlignment != 0x00003062)
            {
                throw new FormatException("unexpected file alignment (should have been 0x7FF00000)");
            }

            this.LittleEndian =
                fileAlignment == 0x7FF00000 ||
                fileAlignment == 0x62300000;
            this.FileAlignment = this.LittleEndian == true ?
                fileAlignment : fileAlignment.Swap();

            this.BasePath = input.ReadString(64, true, Encoding.ASCII);

            var count = input.ReadValueU32(this.LittleEndian);
            
            var hashes = new uint[count];
            for (uint i = 0; i < count; i++)
            {
                hashes[i] = input.ReadValueU32(this.LittleEndian);
            }

            this.Entries.Clear();
            for (uint i = 0; i < count; i++)
            {
                var entry = new Big.Entry();
                entry.NameHash = hashes[i];
                entry.Size = input.ReadValueU32(this.LittleEndian);
                entry.Offset = input.ReadValueU32(this.LittleEndian);
                entry.Locale = input.ReadValueU32(this.LittleEndian);
                entry.Unknown4 = input.ReadValueU32(this.LittleEndian);
                this.Entries.Add(entry);

                if (entry.Unknown4 != 0)
                {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
