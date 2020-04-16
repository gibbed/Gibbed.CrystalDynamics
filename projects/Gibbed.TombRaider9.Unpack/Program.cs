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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using Gibbed.CrystalDynamics.FileFormats;
using Gibbed.IO;
using NDesk.Options;

namespace Gibbed.TombRaider9.Unpack
{
    internal class Program
    {
        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
        }

        private static bool Is000(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string[] extensions = { ".000", ".tiger" };
            foreach (var extension in extensions.Reverse())
            {
                var other = Path.GetExtension(path);
                if (other == null || other.ToLowerInvariant() != extension)
                {
                    return false;
                }
                path = Path.ChangeExtension(path, null);
            }
            return true;
        }

        private static string GetBasePath(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }

            string[] extensions = { ".000", ".tiger" };
            foreach (var extension in extensions.Reverse())
            {
                var other = Path.GetExtension(path);
                if (other == null || other.ToLowerInvariant() != extension)
                {
                    break;
                }
                path = Path.ChangeExtension(path, null);
            }
            return path;
        }

        private static string GetExtension(uint index)
        {
            return "." + index.ToString(CultureInfo.InvariantCulture).PadLeft(3, '0') + ".tiger";
        }

        public static void Main(string[] args)
        {
            bool showHelp = false;
            bool? extractUnknowns = null;
            bool overwriteFiles = false;
            bool verbose = true;
            string currentProject = null;
            string filterPattern = null;

            var options = new OptionSet()
            {
                { "o|overwrite", "overwrite existing files", v => overwriteFiles = v != null },
                {
                    "nu|no-unknowns", "don't extract unknown files",
                    v => extractUnknowns = v != null ? false : extractUnknowns
                },
                {
                    "ou|only-unknowns", "only extract unknown files",
                    v => extractUnknowns = v != null ? true : extractUnknowns
                },
                { "f|filter=", "filter files using pattern", v => filterPattern = v },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
                { "p|project=", "override current project", v => currentProject = v },
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

            if (extras.Count < 1 || extras.Count > 2 ||
                showHelp == true ||
                Is000(extras[0]) == false)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input_file.000.tiger [output_dir]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            var inputPath = extras[0];
            var basePath = GetBasePath(inputPath);
            var outputPath = extras.Count > 1 ? extras[1] : basePath + "_unpack";

            Regex filter = null;
            if (string.IsNullOrEmpty(filterPattern) == false)
            {
                filter = new Regex(filterPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            }

            var manager = ProjectData.Manager.Load(currentProject);
            if (manager.ActiveProject == null)
            {
                Console.WriteLine("Warning: no active project loaded.");
            }

            var header = new TigerArchiveFile();
            using (var input = File.OpenRead(inputPath))
            {
                header.Deserialize(input);
            }

            var hashes = manager.LoadLists("*.filelist", s => s.HashFileName(), s => s.ToLowerInvariant());

            Directory.CreateDirectory(outputPath);

            var settings = new XmlWriterSettings()
            {
                Indent = true,
            };

            var xmlPath = Path.Combine(outputPath, "@tiger.xml");
            using (var xml = XmlWriter.Create(xmlPath, settings))
            {
                xml.WriteStartDocument();
                xml.WriteStartElement("files");
                xml.WriteAttributeString("endian", header.Endian.ToString());
                xml.WriteAttributeString("basepath", header.BasePath);
                xml.WriteAttributeString("priority", header.Priority.ToString(CultureInfo.InvariantCulture));

                Stream data = null;
                byte? dataIndex = null;
                uint? lastLocale = null;
                {
                    long current = 0;
                    long total = header.Entries.Count;

                    //long? lastOffset = null;

                    foreach (var entry in header.Entries.OrderBy(e => e.Offset).ThenBy(e => e.DataIndex))
                    {
                        current++;

                        if (dataIndex.HasValue == false || dataIndex.Value != entry.DataIndex)
                        {
                            if (data != null)
                            {
                                data.Close();
                            }

                            dataIndex = entry.DataIndex;

                            var dataPath = basePath + GetExtension(dataIndex.Value);

                            if (verbose == true)
                            {
                                Console.WriteLine(dataPath);
                            }

                            data = File.OpenRead(dataPath);
                        }

                        string name = hashes[entry.NameHash];

                        if (name == null)
                        {
                            if (extractUnknowns.HasValue == true && extractUnknowns.Value == false)
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
                                    data.Position = entry.Offset;
                                    read = data.Read(guess, 0, (int)Math.Min(entry.Size, guess.Length));
                                }

                                extension = FileExtensions.Detect(guess, Math.Min(guess.Length, read));
                            }

                            name = entry.NameHash.ToString("X8", CultureInfo.InvariantCulture);
                            name = Path.ChangeExtension(name, "." + extension);
                            name = Path.Combine(extension, name);
                            name = Path.Combine("__UNKNOWN", name);
                        }
                        else
                        {
                            if (extractUnknowns.HasValue == true && extractUnknowns.Value == true)
                            {
                                continue;
                            }

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

                        if (filter != null && filter.IsMatch(name) == false)
                        {
                            continue;
                        }

                        var entryPath = Path.Combine(outputPath, name);

                        var entryParentPath = Path.GetDirectoryName(entryPath);
                        if (string.IsNullOrEmpty(entryParentPath) == false)
                        {
                            Directory.CreateDirectory(entryParentPath);
                        }

                        if (lastLocale.HasValue == false || lastLocale.Value != entry.Locale)
                        {
                            xml.WriteComment(string.Format(
                                " {0} = {1} ",
                                entry.Locale.ToString("X8", CultureInfo.InvariantCulture),
                                ((ArchiveLocale)entry.Locale)));
                            lastLocale = entry.Locale;
                        }

                        /*
                        if (lastOffset != null)
                        {
                            xml.WriteComment(string.Format(" padding = {0} @ {1}",
                                                           entry.Offset - lastOffset.Value,
                                                           lastOffset.Value));
                        }
                        lastOffset = entry.Offset + entry.Size;

                        xml.WriteComment(string.Format(" offset = {0} ", entry.Offset));
                        */

                        xml.WriteStartElement("entry");
                        xml.WriteAttributeString("hash", entry.NameHash.ToString("X8", CultureInfo.InvariantCulture));
                        xml.WriteAttributeString("locale", entry.Locale.ToString("X8", CultureInfo.InvariantCulture));

                        if (entry.Priority != header.Priority)
                        {
                            xml.WriteAttributeString("priority", entry.Priority.ToString(CultureInfo.InvariantCulture));
                        }

                        xml.WriteValue(name);
                        xml.WriteEndElement();

                        if (overwriteFiles == false && File.Exists(entryPath) == true)
                        {
                            continue;
                        }

                        if (verbose == true)
                        {
                            Console.WriteLine("[{0}/{1}] {2}", current, total, name);
                        }

                        using (var output = File.Create(entryPath))
                        {
                            if (entry.Size > 0)
                            {
                                data.Position = entry.Offset;
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
