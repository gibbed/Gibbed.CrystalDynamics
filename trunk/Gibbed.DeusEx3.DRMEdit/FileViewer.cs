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
using Gibbed.IO;
using System.Linq;
using System.Windows.Forms;

namespace Gibbed.DeusEx3.DRMEdit
{
    public partial class FileViewer : Form
    {
        private string FilePath;
        private FileFormats.DRMFile FileData;

        public FileViewer()
        {
            this.InitializeComponent();
            this.DoubleBuffered = true;
            this.hintLabel.Text = "";

            /* This following block is for Mono-build compatability
             * (ie, compiling this code via Mono and running via .NET)
             * 
             * Mono developers are asstwats:
             *   https://bugzilla.novell.com/show_bug.cgi?id=641826
             * 
             * So, instead of using the ImageListStreamer directly, we'll
             * load images from resources.
             */
            this.typeImageList.Images.Clear();
            this.typeImageList.Images.Add("Unknown", new System.Drawing.Bitmap(16, 16));
            this.typeImageList.Images.Add("__DRM", SectionTypeImages.__DRM);
            this.typeImageList.Images.Add("RenderResource", SectionTypeImages.RenderResource);
            this.typeImageList.Images.Add("Script", SectionTypeImages.Script);
            this.typeImageList.Images.Add("Wave", SectionTypeImages.Wave);
        }

        public void LoadResource(string path)
        {
            this.Text += string.Format(": {0}", Path.GetFileName(path));

            this.FilePath = path;

            using (var input = File.OpenRead(path))
            {
                var rez = new FileFormats.DRMFile();
                rez.Deserialize(input);
                this.FileData = rez;

                /*
                foreach (var section in rez.Sections)
                {
                    if (section.Resolver == null)
                    {
                        continue;
                    }

                    foreach (var unknown in section.Resolver.Unknown2s)
                    {
                        section.Data.Seek(unknown.Unknown0, SeekOrigin.Begin);
                        var id = section.Data.ReadValueU32();
                        if (id == 0x00013065)
                        {
                        }
                    }

                    foreach (var unknown in section.Resolver.Unknown4s)
                    {
                        section.Data.Seek(unknown.Unknown0, SeekOrigin.Begin);
                        var id = section.Data.ReadValueU32();
                        if (id == 0x00013065)
                        {
                        }
                    }
                }
                */
            }

            this.BuildTree();
        }

        private void BuildTree()
        {
            this.entryTreeView.BeginUpdate();
            this.entryTreeView.Nodes.Clear();

            var root = new TreeNode(Path.GetFileName(this.FilePath));
            root.ImageKey = "__DRM";
            root.SelectedImageKey = "__DRM";

            foreach (var section in this.FileData.Sections.OrderBy(s => s.Id))
            {
                var typeName = section.Type.ToString();

                var name = section.Id.ToString("X8");
                name += " : " + typeName;
                name += string.Format(" [{0:X2} {1:X2} {2:X4} {3:X8}]",
                    section.Flags,
                    section.Unknown05,
                    section.Unknown06,
                    section.Unknown10);

                if (section.Data != null)
                {
                    name += " (" + section.Data.Length.ToString() + ")";
                }

                var node = new TreeNode(name);

                if (this.entryTreeView.ImageList.Images.ContainsKey(typeName) == true)
                {
                    node.ImageKey = typeName;
                    node.SelectedImageKey = typeName;
                }
                else
                {
                    node.ImageKey = "";
                    node.SelectedImageKey = "";
                }

                node.Tag = section;
                root.Nodes.Add(node);
            }

            this.entryTreeView.Nodes.Add(root);
            root.Expand();
            this.entryTreeView.EndUpdate();
        }

        private void OpenSection(TreeNode node)
        {
            var section = node.Tag as FileFormats.DRM.Section;
            if (section == null)
            {
                return;
            }

            if (section.Data != null)
            {
                section.Data.Seek(0, SeekOrigin.Begin);
            }

            if (section.Type == FileFormats.DRM.SectionType.RenderResource)
            {
                var viewer = new TextureViewer()
                {
                    MdiParent = this.MdiParent,
                };
                viewer.LoadSection(section);
                viewer.Show();
            }
            else
            {
                var viewer = new RawViewer()
                {
                    MdiParent = this.MdiParent,
                };
                viewer.LoadSection(section);
                viewer.Show();
            }
            /*
            else
            {
                MessageBox.Show(
                    string.Format("Unimplemented section type ({0}).",
                        section.Type),
                    "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            */
        }

        private void OnViewObject(object sender, EventArgs e)
        {
            this.OpenSection(this.entryTreeView.SelectedNode);
        }
    }
}
