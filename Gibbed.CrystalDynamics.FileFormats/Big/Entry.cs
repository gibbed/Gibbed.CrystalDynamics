using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.CrystalDynamics.FileFormats.Big
{
    public class Entry
    {
        public uint NameHash;
        public uint Size;
        public uint Offset;

        /// <summary>
        /// Locale is a bitmask representing what languages this resource is
        /// valid for. 'Default' indicates all languages.
        /// 
        /// Typically languages that are not implemented will have their bits set
        /// for all non-'Default' resources.
        /// </summary>
        public uint Locale;

        public uint Unknown4;

        public override string ToString()
        {
            return string.Format("{0:X8}:{1:X8} @ {2} ({3} bytes)",
                this.NameHash,
                this.Locale,
                this.Offset,
                this.Size);
        }
    }
}
