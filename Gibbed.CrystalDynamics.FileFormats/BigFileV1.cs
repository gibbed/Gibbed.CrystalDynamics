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
using Gibbed.IO;

namespace Gibbed.CrystalDynamics.FileFormats
{
    public class BigFileV1
    {
        public bool LittleEndian = true;
        public uint FileAlignment = 0x7FF00000;
        public List<Big.Entry> Entries
            = new List<Big.Entry>();

        public static int EstimateHeaderSize(int count)
        {
            return
                (4 + // count
                (4 * count) + // name hashes
                (16 * count)) // entry table
                .Align(2048); // aligned to 2048 bytes
        }

        public void Serialize(Stream output)
        {
            output.WriteValueS32(this.Entries.Count, this.LittleEndian);

            var entries = this.Entries
                .OrderBy(e => e.UncompressedSize)
                .OrderBy(e => e.NameHash);

            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.NameHash, this.LittleEndian);
            }

            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.UncompressedSize, this.LittleEndian);
                output.WriteValueU32(entry.Offset, this.LittleEndian);
                output.WriteValueU32(entry.Locale, this.LittleEndian);
                output.WriteValueU32(entry.CompressedSize, this.LittleEndian);
            }
        }

        public void Deserialize(Stream input)
        {
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
                entry.UncompressedSize = input.ReadValueU32(this.LittleEndian);
                entry.Offset = input.ReadValueU32(this.LittleEndian);
                entry.Locale = input.ReadValueU32(this.LittleEndian);
                entry.CompressedSize = input.ReadValueU32(this.LittleEndian);
                this.Entries.Add(entry);
            }
        }
    }
}
