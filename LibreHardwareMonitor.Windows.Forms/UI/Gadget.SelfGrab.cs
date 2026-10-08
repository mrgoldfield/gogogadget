// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Fork additions Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork-only half of Gadget (screenshot mode, see Utilities/SelfGrab.cs).
public abstract partial class Gadget
{
    // Paints the gadget through its normal OnPaint into a new offscreen
    // bitmap (premultiplied alpha, like GadgetWindow's own buffer), with
    // the same render hints GadgetWindow.CreateBuffer sets. Returns null
    // while the gadget has no size yet.
    internal Bitmap RenderSnapshot()
    {
        Size size = Size;
        if (size.Width <= 0 || size.Height <= 0)
            return null;

        var bitmap = new Bitmap(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.TextRenderingHint = TextRenderingHint.SystemDefault;
            g.SmoothingMode = SmoothingMode.HighQuality;
            OnPaint(new PaintEventArgs(g, new Rectangle(Point.Empty, size)));
        }

        return bitmap;
    }
}
