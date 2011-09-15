using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gibbed.IO;
using System.IO;

namespace Gibbed.DeusEx3.FileFormats.DRM
{
    public class ScriptSection
    {
        public void Deserialize(Stream input)
        {
            if (input.ReadValueU32() != 4)
            {
                throw new FormatException("unsupported script version");
            }


        }
    }
}
