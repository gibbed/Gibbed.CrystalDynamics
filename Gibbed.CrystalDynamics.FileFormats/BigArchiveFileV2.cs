/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
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

namespace Gibbed.CrystalDynamics.FileFormats
{
    public class BigArchiveFileV2
    {
        private Endian _Endian = Endian.Little;
        private uint _DataAlignment = 0x7FF00000;
        private readonly List<Entry> _Entries = new List<Entry>();

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public uint DataAlignment
        {
            get { return this._DataAlignment; }
            set { this._DataAlignment = value; }
        }

        public string BasePath { get; set; }

        public List<Entry> Entries
        {
            get { return this._Entries; }
        }

        public static int EstimateHeaderSize(int count)
        {
            return (4 + // file alignment
                    64 + // base path
                    4 + // count
                    (4 * count) + // name hashes
                    (16 * count)) // entry table
                .Align(2048); // aligned to 2048 bytes
        }

        public void Serialize(Stream output)
        {
            var endian = this.Endian;

            output.WriteValueU32(this.DataAlignment, endian);
            output.WriteString(this.BasePath, 64, Encoding.ASCII);
            output.WriteValueS32(this.Entries.Count, endian);

            var entries = this.Entries
                              .OrderBy(e => e.NameHash)
                              .ThenBy(e => e.UncompressedSize);

            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.NameHash, endian);
            }

            foreach (var entry in entries)
            {
                output.WriteValueU32(entry.UncompressedSize, endian);
                output.WriteValueU32(entry.Offset, endian);
                output.WriteValueU32(entry.Locale, endian);
                output.WriteValueU32(entry.CompressedSize, endian);
            }
        }

        public void Deserialize(Stream input)
        {
            var dataAlignment = input.ReadValueU32(Endian.Little);
            if (dataAlignment != 0x7FF00000 &&
                dataAlignment != 0x0000F07F &&
                dataAlignment != 0x62300000 &&
                dataAlignment != 0x00003062)
            {
                throw new FormatException("unexpected file alignment (should have been 0x7FF00000)");
            }

            Endian endian;

            if (dataAlignment == 0x7FF00000 ||
                dataAlignment == 0x62300000)
            {
                endian = Endian.Little;
            }
            else
            {
                endian = Endian.Big;
                dataAlignment = dataAlignment.Swap();
            }

            this.Endian = endian;
            this.DataAlignment = dataAlignment;
            this.BasePath = input.ReadString(64, true, Encoding.ASCII);

            var count = input.ReadValueU32(endian);

            var hashes = new uint[count];
            for (uint i = 0; i < count; i++)
            {
                hashes[i] = input.ReadValueU32(endian);
            }

            this.Entries.Clear();
            for (uint i = 0; i < count; i++)
            {
                var entry = new Entry
                {
                    NameHash = hashes[i],
                    UncompressedSize = input.ReadValueU32(endian),
                    Offset = input.ReadValueU32(endian),
                    Locale = input.ReadValueU32(endian),
                    CompressedSize = input.ReadValueU32(endian)
                };
                this.Entries.Add(entry);

                if (entry.CompressedSize != 0)
                {
                    throw new NotSupportedException();
                }
            }
        }

        public class Entry
        {
            public uint NameHash { get; set; }
            public uint UncompressedSize { get; set; }
            public uint Offset { get; set; }

            /// <summary>
            /// Locale is a bitmask representing what languages this resource is
            /// valid for. 'Default' indicates all languages.
            /// 
            /// Typically languages that are not implemented will have their bits set
            /// for all non-'Default' resources.
            /// </summary>
            public uint Locale { get; set; }

            public uint CompressedSize { get; set; }

            public override string ToString()
            {
                return string.Format("{0:X8}:{1:X8} @ {2} ({3} bytes)",
                                     this.NameHash,
                                     this.Locale,
                                     this.Offset,
                                     this.UncompressedSize);
            }
        }
    }
}
