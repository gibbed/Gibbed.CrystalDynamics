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
using System.IO;
using System.Windows.Forms;
using Be.Windows.Forms;

namespace Gibbed.DeusEx3.DRMEdit
{
    public partial class RawViewer : Form
    {
        public byte[] Data;

        public RawViewer()
        {
            this.InitializeComponent();
            this.hexBox.ReadOnly = true;
        }

        public void LoadSection(FileFormats.DRM.Section section)
        {
            //this.Text += ": " + entry.Description;
            this.Data = new byte[section.Data.Length];
            section.Data.Read(this.Data, 0, this.Data.Length);

            this.UpdatePreview();
        }

        private void UpdatePreview()
        {
            this.hexBox.ByteProvider = new DynamicByteProvider((byte[])this.Data.Clone());
        }

        private void OnSaveToFile(object sender, EventArgs e)
        {
            if (this.saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            using (var output = this.saveFileDialog.OpenFile())
            {
                output.Write(this.Data, 0, this.Data.Length);
            }
        }

        private void OnLoadFromFile(object sender, EventArgs e)
        {
            if (this.openFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            using (var input = this.openFileDialog.OpenFile())
            {
                this.Data = new byte[input.Length];
                if (input.Read(this.Data, 0, this.Data.Length) != this.Data.Length)
                {
                    throw new EndOfStreamException();
                }
                this.UpdatePreview();
            }
        }
    }
}
