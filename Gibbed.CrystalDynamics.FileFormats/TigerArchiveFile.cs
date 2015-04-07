/* Copyright (c) 2015 Rick (rick 'at' gibbed 'dot' us)
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
        private const uint _Signature = 0x54414653; // 'TAFS' => Tiger Archive File System

        #region Fields
        private Endian _Endian = Endian.Little;
        private readonly List<Entry> _Entries;
        private uint _DataFileCount;
        private uint _Priority;
        private string _BasePath;
        #endregion

        public TigerArchiveFile()
        {
            this._Entries = new List<Entry>();
        }

        #region Properties
        public Endian Endian
        {
            get { return this._Endian; }
            set { this._Endian = value; }
        }

        public List<Entry> Entries
        {
            get { return this._Entries; }
        }

        public uint DataFileCount
        {
            get { return this._DataFileCount; }
            set { this._DataFileCount = value; }
        }

        public uint Priority
        {
            get { return this._Priority; }
            set { this._Priority = value; }
        }

        public string BasePath
        {
            get { return this._BasePath; }
            set { this._BasePath = value; }
        }
        #endregion

        public static int EstimateHeaderSize(int count)
        {
            return (4 + // magic
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

            output.WriteValueU32(_Signature, Endian.Big);
            output.WriteValueU32(3, endian);
            output.WriteValueU32(this._DataFileCount, endian);
            output.WriteValueS32(this.Entries.Count, endian);
            output.WriteValueU32(this._Priority, endian);
            output.WriteString(this._BasePath ?? "", 32, Encoding.ASCII);

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
            var magic = input.ReadValueU32(Endian.Big);
            if (magic != _Signature)
            {
                throw new FormatException("bad magic");
            }

            var version = input.ReadValueU32(Endian.Little);
            if (version != 3 && version.Swap() != 3)
            {
                throw new FormatException("bad version");
            }
            var endian = version == 3 ? Endian.Little : Endian.Big;

            this._Endian = endian;
            this._DataFileCount = input.ReadValueU32(endian);
            var entryCount = input.ReadValueU32(endian);
            this._Priority = input.ReadValueU32(endian);
            this._BasePath = input.ReadString(32, true, Encoding.ASCII);

            this._Entries.Clear();
            for (uint i = 0; i < entryCount; i++)
            {
                var nameHash = input.ReadValueU32(endian);
                var locale = input.ReadValueU32(endian);
                var size = input.ReadValueU32(endian);
                var bits = input.ReadValueU32(endian);

                var entry = new Entry
                {
                    NameHash = nameHash,
                    Locale = locale,
                    Size = size,
                    Offset = (bits & 0xFFFFF800) >> 0,
                    DataIndex = (byte)((bits & 0x0000000F) >> 0),
                    Priority = (byte)((bits & 0x000007F0) >> 4),
                };

                if (entry.Priority != this.Priority)
                {
                    throw new FormatException();
                }

                if (entry.DataIndex >= this.DataFileCount)
                {
                    throw new FormatException();
                }

                this._Entries.Add(entry);
            }
        }

        public struct Entry
        {
            public uint NameHash;

            /// <summary>
            /// Locale is a bitmask representing what languages this resource is
            /// valid for. 'Default' indicates all languages.
            /// 
            /// Typically languages that are not implemented will have their bits set
            /// for all non-'Default' resources.
            /// </summary>
            public uint Locale;

            public uint Size;
            public uint Offset;
            public byte DataIndex;
            public byte Priority;

            public override string ToString()
            {
                return string.Format(
                    "{0:X8}:{1:X8} @ {4}:{2} ({3} bytes)",
                    this.NameHash,
                    this.Locale,
                    this.Offset,
                    this.Size,
                    this.DataIndex);
            }
        }
    }
}
