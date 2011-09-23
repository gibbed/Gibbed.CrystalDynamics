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
using Gibbed.IO;

namespace Gibbed.CrystalDynamics.FileFormats
{
    public static class FileExtensions
    {
        public static string Detect(byte[] guess, int read)
        {
            if (read == 0)
            {
                return "null";
            }

            if (
                read >= 4 &&
                guess[0] == 'C' &&
                guess[1] == 'D' &&
                guess[2] == 'R' &&
                guess[3] == 'M')
            {
                return "drm";
            }
            else if (
                read >= 4 &&
                guess[0] == 'C' &&
                guess[1] == 'R' &&
                guess[2] == 'I' &&
                guess[3] == 'D')
            {
                return "usm";
            }
            if (
                read >= 4 &&
                guess[0] == 0x89 &&
                guess[1] == 'P' &&
                guess[2] == 'N' &&
                guess[3] == 'G')
            {
                return "png";
            }
            else if (
                read >= 4 &&
                guess[0] == 'F' &&
                guess[1] == 'S' &&
                guess[2] == 'B' &&
                guess[3] == '4')
            {
                return "sam";
            }
            else if (
                read >= 4 &&
                guess[0] == 'M' &&
                guess[1] == 'u' &&
                guess[2] == 's')
            {
                return "mus";
            }
            else if (
                read >= 4 &&
                guess[0] == 0x21 &&
                guess[1] == 'W' &&
                guess[2] == 'A' &&
                guess[3] == 'R')
            {
                return "raw";
            }

            // sound data
            if (read >= 4)
            {
                var sampleRate = BitConverter.ToUInt32(guess, 0);

                if (sampleRate == 22050 || sampleRate.Swap() == 22050 ||
                    sampleRate == 44100 || sampleRate.Swap() == 44100 ||
                    sampleRate == 48000 || sampleRate.Swap() == 48000 ||

                    (sampleRate >= 43900 && sampleRate <= 44300) ||
                    (sampleRate >= 31000 && sampleRate <= 32200)
                    )
                {
                    return "mul";
                }
            }

            return "unknown";
        }
    }
}
