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
using System.Text;
using Gibbed.IO;
using Gibbed.DeusEx3.FileFormats;
using SectionType = Gibbed.DeusEx3.FileFormats.DRM.SectionType;
namespace Test
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            /*
            using (var input = File.OpenRead(@"T:\DXHR\bigfile\default\pc-w\art\texture_library\diffuse\asphalt_a_d_diffuse.drm"))
            {
                using (var output = File.Create("asphalt_a_d_diffuse.udrm"))
                {
                    using (var data = CDRMFile.Decompress(input))
                    {
                        output.WriteFromStream(data, data.Length);
                    }
                }
            }

            //using (var input = File.OpenRead("globaldlc.drm"))
            using (var input = File.OpenRead(@"T:\DXHR\bigfile\default\pc-w\art\texture_library\diffuse\asphalt_a_d_diffuse.drm"))
            {
                var drm = new DRMFile();
                drm.Deserialize(input);
            }
            */

            long? largest = null;
            foreach (var inputPath in Directory.GetFiles(@"T:\DXHR\bugfile", "*.drm", SearchOption.AllDirectories))
            {
                Console.WriteLine(inputPath);

                using (var input = File.OpenRead(inputPath))
                {
                    var drm = new DRMFile();
                    drm.Deserialize(input);

                    var candidates = drm.Sections
                        .Where(e => e.Type == SectionType.Script && e.Data != null);
                    if (candidates.Count() > 0)
                    {
                        if (largest.HasValue == false)
                        {
                            largest = candidates.Min(e => e.Data.Length);
                        }
                        else
                        {
                            largest = Math.Min(largest.Value, candidates.Min(e => e.Data.Length));
                        }
                    }
                }
            }

            Console.WriteLine("{0}", largest);

            /*
            var paths = new List<string>();

            foreach (var inputPath in Directory.GetFiles(@"T:\DXHR\bugfile", "*.drm", SearchOption.AllDirectories))
            {
                Console.WriteLine(inputPath);

                using (var input = File.OpenRead(inputPath))
                {
                    var drm = new DRMFile();
                    drm.Deserialize(input);

                    foreach (var path in drm.Unknown08s)
                    {
                        if (paths.Contains(path) == false)
                        {
                            paths.Add(path);
                        }
                    }

                    foreach (var path in drm.Unknown04s)
                    {
                        if (paths.Contains(path) == false)
                        {
                            paths.Add(path);
                        }
                    }
                }
            }

            Console.WriteLine("-----");
            paths.Sort();
            foreach (var path in paths)
            {
                Console.WriteLine(path);
            }
            */
        }
    }
}
