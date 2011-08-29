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

namespace Gibbed.DeusEx3.FileFormats
{
    // PCD9 = PC D3D9 Texture?
    public class PCD9File
    {
        public PCD9.Format Format;
        public ushort Width;
        public ushort Height;
        public List<PCD9.Mipmap> Mipmaps = new List<PCD9.Mipmap>();

        public void Deserialize(Stream input)
        {
            if (input.ReadValueU32() != 0x39444350)
            {
                throw new FormatException();
            }

            this.Format = input.ReadValueEnum<PCD9.Format>();
            var dataSize = input.ReadValueU32();
            var unknown0C = input.ReadValueU32();
            this.Width = input.ReadValueU16();
            this.Height = input.ReadValueU16();
            var bpp = input.ReadValueU16();
            var unknown16 = input.ReadValueU8();
            var mipMapCount = 1 + input.ReadValueU8();
            var flags = input.ReadValueU16();
            var unknown1A = input.ReadValueU16();

            if ((flags & (0x8000 | 0x4000)) != 0)
            {
                throw new NotImplementedException();
            }

            this.Mipmaps.Clear();
            using (var data = input.ReadToMemoryStream(dataSize))
            {
                var mipWidth = this.Width;
                var mipHeight = this.Height;

                for (int i = 0; i < mipMapCount; i++)
                {
                    if (mipWidth == 0)
                    {
                        mipWidth = 1;
                    }

                    if (mipHeight == 0)
                    {
                        mipHeight = 1;
                    }

                    int blockCount = ((mipWidth + 3) / 4) * ((mipHeight + 3) / 4);
                    int blockSize = this.Format == PCD9.Format.DXT1 ? 8 : 16;

                    var buffer = new byte[blockCount * blockSize];
                    if (data.Read(buffer, 0, buffer.Length) != buffer.Length)
                    {
                        throw new EndOfStreamException();
                    }

                    this.Mipmaps.Add(new PCD9.Mipmap()
                        {
                            Width = mipWidth,
                            Height = mipHeight,
                            Data = buffer,
                        });

                    mipWidth >>= 1;
                    mipHeight >>= 1;
                }
            }
        }
    }
}
