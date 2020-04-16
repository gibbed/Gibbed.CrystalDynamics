/* Copyright (c) 2020 Rick (rick 'at' gibbed 'dot' us)
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
using System.Runtime.InteropServices;
using Gibbed.IO;
using NDesk.Options;

namespace Gibbed.DeusEx3.Demux
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool crashOnUnknownData = false;
            bool overwriteFiles = false;
            bool verbose = false;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                {
                    "c|crash-on-unknown-data", "crash when there is unknown data detected in the header",
                    v => crashOnUnknownData = v != null
                },
                { "v|verbose", "be verbose", v => verbose = v != null },
                {
                    "h|help", "show this message and exit", v => showHelp = v != null
                },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.mul [output_base]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputBasePath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null);

            var validSampleRates = new int[]
            {
                5512,
                11025,
                22050,
                44100,
                48000,
            };

            using (var input = File.OpenRead(inputPath))
            {
                var structureSize = Marshal.SizeOf(typeof(SoundStreamHeader));
                if (structureSize != 0x2D0)
                {
                    throw new InvalidOperationException();
                }

                var header = input.ReadStructure<SoundStreamHeader>(0x800);

                if (validSampleRates.Contains(header.SampleRate) == false &&
                    validSampleRates.Contains(header.SampleRate.Swap()) == false)
                {
                    throw new FormatException("unexpected sample rate");
                }

                var endian = validSampleRates.Contains(header.SampleRate) ? Endian.Little : Endian.Big;
                if (endian != Endian.Little)
                {
                    header.Swap();
                }

                if (verbose == true)
                {
                    Console.WriteLine("sample rate = {0}", header.SampleRate);
                    Console.WriteLine("unknown 4 = {0:X8}", header.Unknown004);
                    Console.WriteLine("unknown 8 = {0:X8}", header.SampleCount);
                    Console.WriteLine("channels = {0}", header.ChannelCount);
                }

                if (crashOnUnknownData == true)
                {
                    for (int i = 0; i < header.Unknown010.Length; i++)
                    {
                        if (header.Unknown010[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }

                    for (int i = 0; i < header.Unknown030.Length; i++)
                    {
                        if (header.Unknown030[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }

                    for (int i = 0; i < header.Unknown040.Length; i++)
                    {
                        if (header.Unknown040[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }

                    for (int i = 0; i < header.Unknown048.Length; i++)
                    {
                        if (header.Unknown048[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }

                    for (int i = 0; i < header.Unknown074.Length; i++)
                    {
                        if (header.Unknown074[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }

                    for (int i = 0; i < header.Unknown07C.Length; i++)
                    {
                        if (header.Unknown07C[i] != 0)
                        {
                            throw new FormatException();
                        }
                    }
                }

                var streams = new Stream[header.ChannelCount];
                for (uint i = 0; i < header.ChannelCount; i++)
                {
                    var outputPath = Path.ChangeExtension(string.Format("{0}_{1}", outputBasePath, i), ".fsb");
                    if (overwriteFiles == true ||
                        File.Exists(outputPath) == false)
                    {
                        streams[i] = File.Create(outputPath);
                    }
                }

                while (input.Position < input.Length)
                {
                    var segmentType = input.ReadValueU32(endian);
                    var segmentSize = input.ReadValueU32(endian);
                    var segmentUnknown4 = input.ReadValueU32(endian);
                    var segmentUnknown8 = input.ReadValueU32(endian);

                    if (verbose == true)
                    {
                        Console.WriteLine("segment : {0}, {1}, {2}, {3}",
                                          segmentType,
                                          segmentSize,
                                          segmentUnknown4,
                                          segmentUnknown8);
                    }

                    // 0 = audio
                    // 1 = cinematic
                    // 2 = subtitles
                    if (segmentType != 0 &&
                        segmentType != 1 &&
                        segmentType != 2)
                    {
                        throw new FormatException();
                    }

                    if ( /*segmentUnknown4 != 0 ||*/
                        segmentUnknown8 != 0)
                    {
                        throw new FormatException();
                    }

                    if (segmentType == 1 || segmentType == 2)
                    {
                        input.Seek(segmentSize.Align(16), SeekOrigin.Current);
                        continue;
                    }

                    using (var data = input.ReadToMemoryStream((int)segmentSize.Align(16)))
                    {
                        while (data.Position < segmentSize)
                        {
                            var blockSize = data.ReadValueU32(endian);
                            var blockStream = data.ReadValueU32(endian);
                            var blockFlags = data.ReadValueU32(endian);
                            var blockUnknown8 = data.ReadValueU32(endian);

                            if (verbose == true)
                            {
                                Console.WriteLine("block : {0}, {1}, {2}, {3}",
                                                  blockSize,
                                                  blockStream,
                                                  blockFlags,
                                                  blockUnknown8);
                            }

                            if (blockStream >= header.ChannelCount ||
                                blockFlags != 0x2001 ||
                                blockUnknown8 != 0)
                            {
                                throw new FormatException();
                            }

                            if (streams[blockStream] == null)
                            {
                                data.Seek(blockSize, SeekOrigin.Current);
                            }
                            else
                            {
                                streams[blockStream].WriteFromStream(data, blockSize);
                            }
                        }
                    }
                }

                foreach (var stream in streams)
                {
                    if (stream != null)
                    {
                        stream.Close();
                    }
                }
            }
        }
    }
}
