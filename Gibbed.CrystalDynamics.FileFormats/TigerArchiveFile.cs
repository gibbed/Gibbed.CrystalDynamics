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
using System.Text;
using Gibbed.IO;

namespace Gibbed.CrystalDynamics.FileFormats
{
    public class TigerArchiveFile
    {
        private const uint _Signature = 0x53464154; // 'TAFS' => Tiger Archive File System

        private Endian _Endian = Endian.Little;
        private readonly List<Entry> _Entries = new List<Entry>();

        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public uint DataFileCount { get; set; }
        public uint Priority { get; set; }
        public string BasePath { get; set; }

        public List<Entry> Entries
        {
            get { return this._Entries; }
        }

        public static int EstimateHeaderSize(int count)
        {
            return
                (4 + // magic
                 4 + // version
                 4 + // data file count
                 4 + // entry count
                 4 + // priority
                 32 + // base path
                 (16 * count)) // entry table
                    .Align(2048); // aligned to 2048 bytes
        }

        public void Serialize(Stream output)
        {
            var endian = this.Endian;

            output.WriteValueU32(_Signature, endian);
            output.WriteValueU32(3, endian);
            output.WriteValueU32(this.DataFileCount, endian);
            output.WriteValueS32(this.Entries.Count, endian);
            output.WriteValueU32(this.Priority);
            output.WriteString(this.BasePath ?? "", 32, Encoding.ASCII);

            foreach (var entry in this.Entries)
            {
                output.WriteValueU32(entry.NameHash, endian);
                output.WriteValueU32(entry.Locale, endian);
                output.WriteValueU32(entry.Size, endian);

                if ((entry.Offset & 0x7FF) != 0)
                {
                    throw new InvalidOperationException("entry offsets must be aligned to 2048 bytes");
                }

                uint offset = 0;
                offset |= (entry.Offset & 0xFFFFF800) << 0;
                offset |= ((uint)entry.DataIndex & 0x0000000F) << 0;
                offset |= ((uint)entry.Priority & 0x0000007F) << 4;

                output.WriteValueU32(offset, endian);
            }
        }

        public void Deserialize(Stream input)
        {
            var magic = input.ReadValueU32(Endian.Little);
            if (magic != _Signature &&
                magic.Swap() != _Signature)
            {
                throw new FormatException();
            }
            var endian = magic == _Signature ? Endian.Little : Endian.Big;

            var version = input.ReadValueU32(endian);
            if (version != 3)
            {
                throw new FormatException();
            }

            this.DataFileCount = input.ReadValueU32(endian);
            var entryCount = input.ReadValueU32(endian);
            this.Priority = input.ReadValueU32(endian);
            this.BasePath = input.ReadString(32, true, Encoding.ASCII);

            this.Entries.Clear();
            for (uint i = 0; i < entryCount; i++)
            {
                var entry = new Entry
                {
                    NameHash = input.ReadValueU32(endian),
                    Locale = input.ReadValueU32(endian),
                    Size = input.ReadValueU32(endian)
                };

                var offset = input.ReadValueU32(endian);
                entry.Offset = (offset & 0xFFFFF800) >> 0;
                entry.DataIndex = (byte)((offset & 0x0000000F) >> 0);
                entry.Priority = (byte)((offset & 0x000007F0) >> 4);

                if ( /*entry.Priority != 0 &&*/
                    entry.Priority != this.Priority)
                {
                    throw new FormatException();
                }

                if (entry.DataIndex >= this.DataFileCount)
                {
                    throw new FormatException();
                }

                this.Entries.Add(entry);
            }
        }

        public class Entry
        {
            public uint NameHash { get; set; }

            /// <summary>
            /// Locale is a bitmask representing what languages this resource is
            /// valid for. 'Default' indicates all languages.
            /// 
            /// Typically languages that are not implemented will have their bits set
            /// for all non-'Default' resources.
            /// </summary>
            public uint Locale { get; set; }

            public uint Size { get; set; }
            public uint Offset { get; set; }
            public byte DataIndex { get; set; }
            public byte Priority { get; set; }

            public override string ToString()
            {
                return string.Format("{0:X8}:{1:X8} @ {4}:{2} ({3} bytes)",
                                     this.NameHash,
                                     this.Locale,
                                     this.Offset,
                                     this.Size,
                                     this.DataIndex);
            }
        }

        [Flags]
        public enum EntryFlags : byte
        {
            None = 0,
            IsCompressed = 1 << 0,
        }
    }
}
