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

namespace Gibbed.DeusEx3.FileFormats
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
                return "cpk";
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
                guess[0] == 0x44 &&
                guess[1] == 0xAC &&
                guess[2] == 0 &&
                guess[3] == 0)
            {
                return "mul";
            }
            else if (
                read >= 4 &&
                guess[3] == 0x44 &&
                guess[2] == 0xAC &&
                guess[1] == 0 &&
                guess[0] == 0)
            {
                return "mul";
            }
            else if (
                read >= 4 &&
                guess[0] == 0x80 &&
                guess[1] == 0xBB &&
                guess[2] == 0 &&
                guess[3] == 0)
            {
                return "mul";
            }
            else if (
                read >= 4 &&
                guess[3] == 0x80 &&
                guess[2] == 0xBB &&
                guess[1] == 0 &&
                guess[0] == 0)
            {
                return "mul";
            }
            else if (
                read >= 4 &&
                guess[0] == 'M' &&
                guess[1] == 'u' &&
                guess[2] == 's')
            {
                return "mus";
            }

            return "unknown";
        }
    }
}
