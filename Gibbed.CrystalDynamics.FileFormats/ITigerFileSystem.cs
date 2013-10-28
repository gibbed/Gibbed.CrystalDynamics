using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.CrystalDynamics.FileFormats
{
    public interface ITigerFileSystem
    {
        MemoryStream LoadFile();
    }
}
