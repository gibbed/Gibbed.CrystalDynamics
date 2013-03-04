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
using System.Globalization;
using System.IO;
using System.Xml.XPath;
using Gibbed.CrystalDynamics.FileFormats;
using Gibbed.IO;
using NDesk.Options;

namespace Gibbed.DeusEx3.Pack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        public static void Main(string[] args)
        {
            bool verbose = false;
            bool showHelp = false;

            var options = new OptionSet()
            {
                {
                    "h|help",
                    "show this message and exit",
                    v => showHelp = v != null
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

            if (extras.Count < 1 || extras.Count > 2 || showHelp == true)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_directory [output_archive]", GetExecutableName());
                Console.WriteLine("Pack directory into an archive.");
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, ".000");

            if (Directory.Exists(inputPath) == true)
            {
                string testPath = Path.Combine(inputPath, "bigfile.xml");
                if (File.Exists(testPath) == true)
                {
                    inputPath = testPath;
                }
            }

            var entries = new List<MyEntry>();
            var big = new BigArchiveFileV2();

            using (var input = File.Open(
                inputPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var doc = new XPathDocument(input);
                var nav = doc.CreateNavigator();

                var root = nav.SelectSingleNode("/files");

                var _fileAlignment = root.GetAttribute("alignment", "");
                if (string.IsNullOrEmpty(_fileAlignment) == true)
                {
                    throw new FormatException("alignment cannot be null or empty");
                }
                big.FileAlignment = uint.Parse(_fileAlignment, NumberStyles.AllowHexSpecifier);

                var _endian = root.GetAttribute("endian", "");
                switch (_endian.ToLowerInvariant())
                {
                    case "big":
                    {
                        big.Endian = Endian.Big;
                        break;
                    }

                    case "little":
                    default:
                    {
                        big.Endian = Endian.Little;
                        break;
                    }
                }

                big.BasePath = root.GetAttribute("basepath", "") ?? "PC-W";

                var nodes = root.Select("entry");
                while (nodes.MoveNext() == true)
                {
                    var node = nodes.Current;

                    var _hash = node.GetAttribute("hash", "");
                    if (string.IsNullOrEmpty(_hash) == true)
                    {
                        throw new FormatException("entry hash cannot be null or empty");
                    }

                    var _locale = node.GetAttribute("locale", "");
                    if (string.IsNullOrEmpty(_locale) == true)
                    {
                        throw new FormatException("entry locale cannot be null or empty");
                    }

                    var path = node.Value;
                    if (string.IsNullOrEmpty(path) == true)
                    {
                        throw new FormatException("entry path cannot be null or empty");
                    }

                    if (Path.IsPathRooted(path) == false)
                    {
                        path = Path.Combine(Path.GetDirectoryName(inputPath), path);
                        path = Path.GetFullPath(path);
                    }

                    entries.Add(new MyEntry()
                    {
                        NameHash = uint.Parse(_hash, NumberStyles.AllowHexSpecifier),
                        UncompressedSize = 0,
                        Offset = 0,
                        Locale = uint.Parse(_locale, NumberStyles.AllowHexSpecifier),
                        CompressedSize = 0,
                        Path = path,
                    });
                }
            }

            uint? currentBigFile = null;
            Stream data = null;

            var headerSize = (uint)BigArchiveFileV1.EstimateHeaderSize(entries.Count);
            var firstOffset = headerSize / 2048;

            var maxBlocksPerFile = big.FileAlignment / 2048;

            var globalOffset = 0u;
            var localOffset = firstOffset;

            var entryBigFile = 0u;

            foreach (var entry in entries)
            {
                if (verbose == true)
                {
                    Console.WriteLine(entry.Path);
                }

                using (var input = File.OpenRead(entry.Path))
                {
                    var length = (uint)input.Length;

                    var blockCount = length.Align(2048) / 2048;

                    if (blockCount > maxBlocksPerFile)
                    {
                        Console.WriteLine("'{0}' can't fit in the archive! (writing as much as possible)", entry.Path);
                        blockCount = maxBlocksPerFile;
                        length = blockCount * 2048;
                    }

                    if (localOffset + blockCount > maxBlocksPerFile)
                    {
                        localOffset = 0;
                        globalOffset += maxBlocksPerFile;
                        entryBigFile++;
                    }

                    if (currentBigFile.HasValue == false ||
                        currentBigFile.Value != entryBigFile)
                    {
                        if (data != null)
                        {
                            data.Close();
                            data = null;
                        }

                        currentBigFile = entryBigFile;
                        data = File.Create(Path.ChangeExtension(outputPath,
                                                                "." + currentBigFile.Value.ToString().PadLeft(3, '0')));
                    }

                    data.Seek(localOffset * 2048, SeekOrigin.Begin);
                    data.WriteFromStream(input, length);

                    entry.UncompressedSize = length;
                    entry.Offset = globalOffset + localOffset;
                    big.Entries.Add(entry);

                    localOffset += blockCount;
                }
            }

            if (data != null)
            {
                data.Close();
            }

            using (var output = File.OpenWrite(Path.ChangeExtension(outputPath,
                                                                    ".000")))
            {
                big.Serialize(output);
            }
        }
    }
}
