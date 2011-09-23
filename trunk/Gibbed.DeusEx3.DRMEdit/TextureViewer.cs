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
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Gibbed.DeusEx3.DRMEdit
{
    public partial class TextureViewer : Form, ISectionViewer
    {
        public FileFormats.PCD9File Texture;

        public TextureViewer()
        {
            this.InitializeComponent();
            this.hintLabel.Text = "";
        }

        public void LoadSection(FileFormats.DRM.Section section)
        {
            var texture = new FileFormats.PCD9File();
            texture.Deserialize(section.Data);

            this.hintLabel.Text = string.Format(
                "{0} : {1}x{2}",
                texture.Format,
                texture.Width, texture.Height);
            //this.Text += ": " + entry.Description;

            this.Texture = texture;

            this.UpdatePreview(true);
        }

        #region Bitmap stuff

        // TODO: make these less dumb

        private static Bitmap MakeBitmapFromTrueColor(
            uint width, uint height, byte[] input, bool keepAlpha)
        {
            var output = new byte[width * height * 4];
            var bitmap = new Bitmap(
                (int)width, (int)height,
                PixelFormat.Format32bppArgb);

            for (uint i = 0; i < width * height * 4; i += 4)
            {
                output[i + 0] = input[i + 2];
                output[i + 1] = input[i + 1];
                output[i + 2] = input[i + 0];
                output[i + 3] = keepAlpha == false ? (byte)0xFF : input[i + 3];
            }

            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }

        private static Bitmap MakeBitmapFromGrayscale(
            uint width, uint height, byte[] input)
        {
            var output = new byte[width * height * 4];
            var bitmap = new Bitmap(
                (int)width, (int)height,
                PixelFormat.Format32bppArgb);

            uint o = 0;
            for (uint i = 0; i < width * height; i++)
            {
                byte v = input[i];
                output[o + 0] = v;
                output[o + 1] = v;
                output[o + 2] = v;
                output[o + 3] = 0xFF;
                o += 4;
            }

            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }

        private static Bitmap MakeBitmapFromAlphaGrayscale(
            uint width, uint height, byte[] input, bool keepAlpha)
        {
            var output = new byte[width * height * 4];
            var bitmap = new Bitmap(
                (int)width, (int)height,
                PixelFormat.Format32bppArgb);

            uint o = 0;
            for (uint i = 0; i < width * height * 2; i += 2)
            {
                var c = input[i + 0];
                var a = input[i + 1];

                output[o + 0] = c;
                output[o + 1] = c;
                output[o + 2] = c;
                output[o + 3] = keepAlpha == false ? (byte)0xFF : a;

                o += 4;
            }

            var area = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var data = bitmap.LockBits(area, ImageLockMode.WriteOnly, bitmap.PixelFormat);
            Marshal.Copy(output, 0, data.Scan0, output.Length);
            bitmap.UnlockBits(data);
            return bitmap;
        }
        #endregion

        private void UpdatePreview(bool first)
        {
            Bitmap bitmap;
            int width;
            int height;

            if (this.Texture.Mipmaps.Count == 0)
            {
                bitmap = null;
                width = 0;
                height = 0;
            }
            else
            {
                var mip = this.Texture.Mipmaps[0];

                width = (int)mip.Width;
                height = (int)mip.Height;

                byte[] data;

                switch (this.Texture.Format)
                {
                    case FileFormats.PCD9.Format.A8R8G8B8:
                    {
                        data = mip.Data;
                        break;
                    }

                    case FileFormats.PCD9.Format.DXT1:
                    {
                        data = Squish.Native.DecompressImage(
                            mip.Data,
                            (int)mip.Width, (int)mip.Height,
                            Squish.Native.Flags.DXT1);
                        break;
                    }

                    case FileFormats.PCD9.Format.DXT3:
                    {
                        data = Squish.Native.DecompressImage(
                            mip.Data,
                            (int)mip.Width, (int)mip.Height,
                            Squish.Native.Flags.DXT3);
                        break;
                    }

                    case FileFormats.PCD9.Format.DXT5:
                    {
                        data = Squish.Native.DecompressImage(
                            mip.Data,
                            (int)mip.Width, (int)mip.Height,
                            Squish.Native.Flags.DXT5);
                        break;
                    }

                    default:
                    {
                        data = null;
                        break;
                    }
                }

                if (data == null)
                {
                    bitmap = null;
                }
                else
                {
                    bitmap = MakeBitmapFromTrueColor(
                        mip.Width, mip.Height, data, this.showAlphaButton.Checked);
                }
            }

            if (first == true)
            {
                if (width > this.previewPanel.Width ||
                    height > this.previewPanel.Height)
                {
                    this.zoomButton.Checked = true;
                }
                else
                {
                    this.zoomButton.Checked = false;
                }
            }

            if (this.zoomButton.Checked == true)
            {
                this.previewPictureBox.Dock = DockStyle.Fill;
                this.previewPictureBox.Image = bitmap;
                this.previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
            }
            else
            {
                this.previewPictureBox.Dock = DockStyle.None;
                this.previewPictureBox.Image = bitmap;
                this.previewPictureBox.Width = width;
                this.previewPictureBox.Height = height;
                this.previewPictureBox.SizeMode = PictureBoxSizeMode.Normal;
            }
        }

        private void OnZoom(object sender, EventArgs e)
        {
            this.UpdatePreview(false);
        }

        private void OnShowAlpha(object sender, EventArgs e)
        {
            this.UpdatePreview(false);
        }

        private void OnSaveToFile(object sender, EventArgs e)
        {
            if (this.saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            using (var output = this.saveFileDialog.OpenFile())
            {
                this.previewPictureBox.Image.Save(output, ImageFormat.Png);
            }
        }
    }
}
