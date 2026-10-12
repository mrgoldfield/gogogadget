// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Upstream's hibernation fix (#2452) moves hardware/sensor add and remove onto the
// UI thread with Control.BeginInvoke. MainForm's constructor turns hardware groups on
// (which raises HardwareAdded) before its window handle exists, and BeginInvoke throws
// without a handle, so the app crashed on start. Before the handle exists we are still
// inside the constructor on the UI thread, so the work can run right away.
internal static class UiThread
{
    public static void Post(Control owner, Action action)
    {
        if (owner.IsHandleCreated)
            owner.BeginInvoke(action);
        else
            action();
    }
}
