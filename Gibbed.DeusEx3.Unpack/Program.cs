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
using System.Xml;
using Gibbed.DeusEx3.FileFormats;
using Gibbed.IO;
using NDesk.Options;
using Big = Gibbed.DeusEx3.FileFormats.Big;

namespace Gibbed.DeusEx3.Unpack
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
            bool extractUnknowns = true;
            bool overwriteFiles = false;
            bool verbose = true;

            var options = new OptionSet()
            {
                {
                    "o|overwrite",
                    "overwrite existing files",
                    v => overwriteFiles = v != null
                },
                {
                    "nu|no-unknowns",
                    "don't extract unknown files",
                    v => extractUnknowns = v == null
                },
                {
                    "v|verbose",
                    "be verbose",
                    v => verbose = v != null
                },
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

            if (extras.Count < 1 ||
                extras.Count > 2 ||
                showHelp == true ||
                Path.GetExtension(extras[0]) != ".000")
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.000 [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            string inputPath = extras[0];
            string outputPath = extras.Count > 1 ? extras[1] : Path.ChangeExtension(inputPath, null) + "_unpack";

            var manager = ProjectData.Manager.Load();
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var big = new BigFile();
            using (var input = File.OpenRead(inputPath))
            {
                big.Deserialize(input);
            }

            var hashes = manager.LoadLists(
                "*.filelist",
                s => s.HashFileName(),
                s => s.ToLowerInvariant());

            Directory.CreateDirectory(outputPath);

            var settings = new XmlWriterSettings();
            settings.Indent = true;

            using (var xml = XmlWriter.Create(
                Path.Combine(outputPath, "bigfile.xml"), settings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("files");
                xml.WriteAttributeString("endian", big.LittleEndian == true ? "little" : "big");
                xml.WriteAttributeString("basepath", big.BasePath);
                xml.WriteAttributeString("alignment", big.FileAlignment.ToString("X8"));

                Stream data = null;
                uint? currentBigFile = null;
                uint? lastLocale = null;
                var maxBlocksPerFile = big.FileAlignment / 2048;
                {
                    long current = 0;
                    long total = big.Entries.Count;

                    foreach (var entry in big.Entries.OrderBy(e => e.Offset))
                    {
                        current++;

                        string name = hashes[entry.NameHash];
                        if (name == null)
                        {
                            if (extractUnknowns == false)
                            {
                                continue;
                            }

                            string extension;
                            // detect type
                            {
                                var guess = new byte[64];
                                int read = 0;

                                if (entry.Size > 0)
                                {
                                    var entryBigFile = entry.Offset / maxBlocksPerFile;
                                    var entryOffset = (entry.Offset % maxBlocksPerFile) * 2048;

                                    if (currentBigFile.HasValue == false ||
                                        currentBigFile.Value != entryBigFile)
                                    {
                                        if (data != null)
                                        {
                                            data.Close();
                                            data = null;
                                        }

                                        currentBigFile = entryBigFile;
                                        data = File.OpenRead(Path.ChangeExtension(inputPath,
                                            "." + currentBigFile.ToString().PadLeft(3, '0')));
                                    }

                                    data.Seek(entryOffset, SeekOrigin.Begin);
                                    read = data.Read(guess, 0, (int)Math.Min(
                                        entry.Size, guess.Length));
                                }

                                extension = FileExtensions.Detect(guess, Math.Min(guess.Length, read));
                            }

                            name = entry.NameHash.ToString("X8");
                            name = Path.ChangeExtension(name, "." + extension);
                            name = Path.Combine(extension, name);
                            name = Path.Combine("__UNKNOWN", name);
                        }
                        else
                        {
                            name = name.Replace("/", "\\");
                            if (name.StartsWith("\\") == true)
                            {
                                name = name.Substring(1);
                            }
                        }

                        if (entry.Locale == 0xFFFFFFFF)
                        {
                            name = Path.Combine("default", name);
                        }
                        else
                        {
                            name = Path.Combine(entry.Locale.ToString("X8"), name);
                        }

                        var entryPath = Path.Combine(outputPath, name);
                        Directory.CreateDirectory(Path.GetDirectoryName(entryPath));

                        if (lastLocale.HasValue == false ||
                            lastLocale.Value != entry.Locale)
                        {
                            xml.WriteComment(string.Format(" {0} = {1} ",
                                entry.Locale.ToString("X8"),
                                ((Big.Locale)entry.Locale)));
                            lastLocale = entry.Locale;
                        }

                        xml.WriteStartElement("entry");
                        xml.WriteAttributeString("hash", entry.NameHash.ToString("X8"));
                        xml.WriteAttributeString("locale", entry.Locale.ToString("X8"));
                        xml.WriteValue(name);
                        xml.WriteEndElement();

                        if (overwriteFiles == false &&
                            File.Exists(entryPath) == true)
                        {
                            continue;
                        }

                        if (verbose == true)
                        {
                            Console.WriteLine("[{0}/{1}] {2}",
                                current, total, name);
                        }

                        using (var output = File.Create(entryPath))
                        {
                            if (entry.Size > 0)
                            {
                                var entryBigFile = entry.Offset / maxBlocksPerFile;
                                var entryOffset = (entry.Offset % maxBlocksPerFile) * 2048;

                                if (currentBigFile.HasValue == false ||
                                    currentBigFile.Value != entryBigFile)
                                {
                                    if (data != null)
                                    {
                                        data.Close();
                                        data = null;
                                    }

                                    currentBigFile = entryBigFile;
                                    data = File.OpenRead(Path.ChangeExtension(inputPath,
                                        "." + currentBigFile.Value.ToString().PadLeft(3, '0')));
                                }

                                data.Seek(entryOffset, SeekOrigin.Begin);
                                output.WriteFromStream(data, entry.Size);
                            }
                        }
                    }
                }

                if (data != null)
                {
                    data.Close();
                }

                xml.WriteEndElement();
                xml.WriteEndDocument();
                xml.Flush();
            }
        }
    }
}
