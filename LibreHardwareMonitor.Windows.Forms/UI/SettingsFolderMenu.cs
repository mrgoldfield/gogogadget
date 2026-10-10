// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LibreHardwareMonitor.Windows.Forms.Utilities;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition: File -> Open Settings Folder. Settings and logs live in
// %LOCALAPPDATA%\GoGoGadget (or next to the exe in portable mode, see
// AppPaths), which nobody can be expected to type. Added at runtime from
// one marked hook line in MainForm, so MainForm.Designer.cs stays upstream.
internal static class SettingsFolderMenu
{
    public static void Attach(ToolStripMenuItem fileMenu, ToolStripItem insertAfter)
    {
        ToolStripMenuItem item = new ToolStripMenuItem("Open Settings Folder")
        {
            Name = "openSettingsFolderMenuItem",
            ToolTipText = "Settings, themes and logs"
        };
        item.Click += (_, _) => Open();

        int index = fileMenu.DropDownItems.IndexOf(insertAfter);
        fileMenu.DropDownItems.Insert(index < 0 ? 0 : index + 1, item);
    }

    private static void Open()
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = AppPaths.DataRoot, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show("Could not open " + AppPaths.DataRoot + "\n\n" + ex.Message, "Open Settings Folder",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}
