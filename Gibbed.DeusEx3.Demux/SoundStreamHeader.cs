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

using System.Runtime.InteropServices;
using Gibbed.IO;

namespace Gibbed.DeusEx3.Demux
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SoundStreamHeader
    {
        public int SampleRate;
        public int Unknown004;
        public int SampleCount;
        public int ChannelCount;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Unknown010;

        public uint Unknown020;
        public uint Unknown024;
        public uint FaceDataSize;
        public uint Unknown02C;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Unknown030;

        public float SegmentCount;
        public float Unknown03C;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unknown040;

        public float Unknown044;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] Unknown048;

        public float Unknown06C;
        public float Unknown070;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Unknown074;

        public float Unknown078;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 596)]
        public byte[] Unknown07C;

        public void Swap()
        {
            this.SampleRate = this.SampleRate.Swap();
            this.Unknown004 = this.Unknown004.Swap();
            this.SampleCount = this.SampleCount.Swap();
            this.ChannelCount = this.ChannelCount.Swap();
            this.Unknown020 = this.Unknown020.Swap();
            this.Unknown024 = this.Unknown024.Swap();
            this.FaceDataSize = this.FaceDataSize.Swap();
            this.Unknown02C = this.Unknown02C.Swap();
            this.SegmentCount = this.SegmentCount.Swap();
            this.Unknown03C = this.Unknown03C.Swap();
            this.Unknown044 = this.Unknown044.Swap();
            this.Unknown06C = this.Unknown06C.Swap();
            this.Unknown070 = this.Unknown070.Swap();
            this.Unknown078 = this.Unknown078.Swap();
        }
    }
}
