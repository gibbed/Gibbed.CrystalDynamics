﻿/* Copyright (c) 2013 Rick (rick 'at' gibbed 'dot' us)
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
using System.Windows.Forms;

namespace Gibbed.DeusEx3.DRMEdit
{
    public partial class Explorer : Form
    {
        public Explorer(List<string> extras)
        {
            this.InitializeComponent();

            foreach (var path in extras)
            {
                var editor = new FileViewer()
                {
                    MdiParent = this
                };
                editor.LoadResource(path);
                editor.Show();
            }
        }

        private void OnExit(object sender, EventArgs e)
        {
            this.Close();
        }

        private void OnWindowCloseAll(object sender, EventArgs e)
        {
            foreach (var form in this.MdiChildren)
            {
                form.Close();
            }
        }

        private void OnWindowCascade(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.Cascade);
        }

        private void OnWindowTileVertical(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.TileVertical);
        }

        private void OnWindowTileHorizontal(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.TileHorizontal);
        }

        private void OnWindowArrangeIcons(object sender, EventArgs e)
        {
            this.LayoutMdi(MdiLayout.ArrangeIcons);
        }

        private void OnOpen(object sender, EventArgs e)
        {
            if (this.openDRMDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            foreach (var path in this.openDRMDialog.FileNames)
            {
                var editor = new FileViewer()
                {
                    MdiParent = this
                };
                editor.LoadResource(path);
                editor.Show();
            }
        }
    }
}
