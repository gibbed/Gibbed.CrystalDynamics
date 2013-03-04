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
using System.IO;
using Gibbed.IO;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;

namespace Gibbed.DeusEx3.FileFormats
{
    // Compression wrapper for DRMFile
    public static class CDRMFile
    {
        public const uint Magic = 0x4344524D;

        public class Block
        {
            public byte Type;
            public uint CompressedSize;
            public uint UncompressedSize;
        }

        public static MemoryStream Decompress(Stream input)
        {
            var basePosition = input.Position;

            if (input.ReadValueU32(false) != Magic) // CDRM
            {
                throw new FormatException();
            }

            var version = input.ReadValueU32(true);

            if (version != 0 &&
                version != 2 && version.Swap() != 2)
            {
                throw new FormatException();
            }

            bool littleEndian;
            uint count;
            uint padding;

            if (version == 0)
            {
                count = input.ReadValueU32(true);

                if (count > 0x7FFFFF)
                {
                    count = count.Swap();
                    littleEndian = false;
                }
                else
                {
                    littleEndian = true;
                }

                input.ReadValueU32(littleEndian);

                padding = (uint)(basePosition + 16 + (count * 8));
                padding = padding.Align(16) - padding;
            }
            else
            {
                littleEndian = version == 2;
                count = input.ReadValueU32(littleEndian);
                padding = input.ReadValueU32(littleEndian);
            }

            var startOfData = basePosition + 16 + (count * 8) + padding;

            var blocks = new Block[count];
            using (var buffer = input.ReadToMemoryStream((count * 8).Align(16)))
            {
                for (uint i = 0; i < count; i++)
                {
                    var block = new Block();
                    var flags = buffer.ReadValueU32(littleEndian);
                    block.UncompressedSize = (flags >> 8) & 0xFFFFFF;
                    block.Type = (byte)(flags & 0xFF);
                    block.CompressedSize = buffer.ReadValueU32(littleEndian);
                    blocks[i] = block;
                }
            }

            if (startOfData != input.Position)
            {
                throw new InvalidOperationException();
            }

            var output = new MemoryStream();

            long offset = 0;
            foreach (var block in blocks)
            {
                var nextPosition = input.Position + block.CompressedSize.Align(16);

                using (var buffer = input.ReadToMemoryStream(block.CompressedSize))
                {
                    if (block.Type == 1)
                    {
                        if (block.CompressedSize != block.UncompressedSize)
                        {
                            throw new InvalidOperationException();
                        }

                        output.Seek(offset, SeekOrigin.Begin);
                        output.WriteFromStream(buffer, block.CompressedSize);
                        offset += block.CompressedSize.Align(16);
                    }
                    else if (block.Type == 2)
                    {
                        var zlib = new InflaterInputStream(buffer);
                        output.Seek(offset, SeekOrigin.Begin);
                        output.WriteFromStream(zlib, block.UncompressedSize);
                        offset += block.UncompressedSize.Align(16);
                    }
                    else
                    {
                        throw new FormatException();
                    }
                }

                input.Seek(nextPosition, SeekOrigin.Begin);
            }

            output.Position = 0;
            return output;
        }
    }
}
