// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Fork additions Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using LibreHardwareMonitor.Windows.Forms.UI;

namespace LibreHardwareMonitor.Windows.Forms.Utilities;

// Screenshot mode for testing without a visible desktop (e.g. over ssh).
//
//   GoGoGadgetHardwareMonitor.exe --screenshot <folder> [--screenshot-delay <ms>]
//
// The app starts normally. After the delay (default 5000 ms, so the
// sensors have values) it saves every open window and every gadget as
// PNG files into <folder>, writes shots.txt (one line per file), and
// exits. Nothing is captured from the screen: forms draw themselves
// (Control.DrawToBitmap) and gadgets paint into an offscreen bitmap
// through their own OnPaint, so this works in a session with no
// desktop. Modal dialogs shown during startup are saved through
// CaptureDialog instead (dialog-*.png). Not covered: anything only the
// real screen shows (window stacking, always-on-top, the desktop behind
// a translucent gadget), and right-click menus.
//
// Fork-only file. Hooks in upstream files, each marked "GoGoGadget:":
// Program.Main (calls Init) and the Gadget constructor (calls Track).
internal static class SelfGrab
{
    private const int DefaultDelayMs = 5000;

    // Backgrounds the translucent gadget is composited onto for the
    // "-on-dark"/"-on-light" copies, roughly a dark and a light desktop.
    private static readonly Color DarkBackdrop = Color.FromArgb(32, 32, 32);
    private static readonly Color LightBackdrop = Color.FromArgb(225, 225, 225);

    private static readonly List<WeakReference<Gadget>> Gadgets = new();
    private static readonly List<string> EarlyLines = new();
    private static readonly HashSet<string> UsedNames = new(StringComparer.OrdinalIgnoreCase);
    private static string _outputDirectory;
    private static Timer _timer;
    private static bool _capturingDialog;

    public static bool Active => _outputDirectory != null;

    public static void Init(string[] args)
    {
        if (args == null)
            return;

        int delayMs = DefaultDelayMs;
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--screenshot")
                _outputDirectory = Path.GetFullPath(args[i + 1]);
            else if (args[i] == "--screenshot-delay" && int.TryParse(args[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int ms) && ms > 0)
                delayMs = ms;
        }

        if (!Active)
            return;

        Directory.CreateDirectory(_outputDirectory);

        // A WinForms timer ticks inside any message loop, including a
        // modal dialog's (e.g. the Startup Guide shown during startup),
        // so whatever is open at that moment gets captured.
        _timer = new Timer { Interval = delayMs };
        _timer.Tick += delegate
        {
            if (_capturingDialog)
                return; // CaptureDialog's DoEvents pumped this tick: try again next interval.

            _timer.Stop();
            int exitCode = 0;
            try
            {
                GrabAll();
            }
            catch (Exception ex)
            {
                File.WriteAllText(Path.Combine(_outputDirectory, "error.txt"), ex.ToString());
                exitCode = 1;
            }

            Environment.Exit(exitCode);
        };
        _timer.Start();
    }

    public static void Track(Gadget gadget)
    {
        Gadgets.Add(new WeakReference<Gadget>(gadget));
    }

    private static void GrabAll()
    {
        var lines = new List<string>(EarlyLines);
        HashSet<string> usedNames = UsedNames;

        foreach (Form form in OpenWindows())
        {
            if (!form.Visible || form.Width <= 0 || form.Height <= 0)
                continue;

            string name = UniqueName("window-" + Slug(string.IsNullOrEmpty(form.Text) ? form.GetType().Name : form.Text), usedNames);
            using (var bitmap = new Bitmap(form.Width, form.Height))
            {
                form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
                Save(bitmap, name, lines, $"{form.Width}x{form.Height}");
            }
        }

        int index = 0;
        foreach (WeakReference<Gadget> reference in Gadgets)
        {
            if (!reference.TryGetTarget(out Gadget gadget) || !gadget.Visible)
                continue;

            using Bitmap raw = gadget.RenderSnapshot();
            if (raw == null)
                continue;

            string name = UniqueName(index == 0 ? "gadget" : "gadget" + index, usedNames);
            index++;
            string size = $"{raw.Width}x{raw.Height} opacity={gadget.Opacity}";
            Save(raw, name, lines, size);
            using (Bitmap dark = Composite(raw, DarkBackdrop, gadget.Opacity))
                Save(dark, name + "-on-dark", lines, size);
            using (Bitmap light = Composite(raw, LightBackdrop, gadget.Opacity))
                Save(light, name + "-on-light", lines, size);
        }

        File.WriteAllText(Path.Combine(_outputDirectory, "shots.txt"), string.Join(Environment.NewLine, lines) + Environment.NewLine, Encoding.UTF8);
    }

    // Modal dialogs can't be shown in a session without a desktop
    // (ShowDialog throws there), so in screenshot mode a dialog calls
    // this instead of ShowDialog: it is shown modeless just long enough
    // to lay itself out and draw, saved as dialog-<title>.png, and closed.
    // The caller then carries on as if the user pressed the default button.
    public static void CaptureDialog(Form form)
    {
        _capturingDialog = true;
        try
        {
            CaptureDialogCore(form);
        }
        finally
        {
            _capturingDialog = false;
            // Restart the countdown so the sensors still get the full
            // delay after a startup dialog.
            _timer?.Stop();
            _timer?.Start();
        }
    }

    private static void CaptureDialogCore(Form form)
    {
        form.StartPosition = FormStartPosition.Manual;
        form.Show();
        Application.DoEvents();
        string name = UniqueName("dialog-" + Slug(form.Text), UsedNames);
        using (var bitmap = new Bitmap(form.Width, form.Height))
        {
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            Save(bitmap, name, EarlyLines, $"{form.Width}x{form.Height}");
        }

        form.Close();
    }

    // Application.OpenForms misses forms whose handle was re-created
    // (a known WinForms quirk, e.g. ShowInTaskbar changed after the handle
    // exists - the Startup Guide is one), so the UI thread's top-level
    // windows are enumerated as well.
    private static List<Form> OpenWindows()
    {
        var forms = Application.OpenForms.Cast<Form>().ToList();
        NativeMethods.EnumThreadWindows(NativeMethods.GetCurrentThreadId(), (handle, _) =>
        {
            if (Control.FromHandle(handle) is Form form && !forms.Contains(form))
                forms.Add(form);
            return true;
        }, IntPtr.Zero);
        return forms;
    }

    private static class NativeMethods
    {
        public delegate bool EnumThreadWndProc(IntPtr handle, IntPtr parameter);

        [DllImport("user32.dll")]
        public static extern bool EnumThreadWindows(uint threadId, EnumThreadWndProc callback, IntPtr parameter);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();
    }

    private static Bitmap Composite(Bitmap source, Color backdrop, byte opacity)
    {
        var result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(result))
        {
            g.Clear(backdrop);
            using var attributes = new ImageAttributes();
            var matrix = new ColorMatrix { Matrix33 = opacity / 255f };
            attributes.SetColorMatrix(matrix);
            g.DrawImage(source, new Rectangle(Point.Empty, source.Size), 0, 0, source.Width, source.Height, GraphicsUnit.Pixel, attributes);
        }

        return result;
    }

    private static void Save(Bitmap bitmap, string name, List<string> lines, string info)
    {
        string fileName = name + ".png";
        bitmap.Save(Path.Combine(_outputDirectory, fileName), ImageFormat.Png);
        lines.Add(fileName + "\t" + info);
    }

    private static string Slug(string text)
    {
        var builder = new StringBuilder();
        foreach (char c in text.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(c))
                builder.Append(c);
            else if (builder.Length > 0 && builder[builder.Length - 1] != '-')
                builder.Append('-');
        }

        string slug = builder.ToString().Trim('-');
        if (slug.Length > 40)
            slug = slug.Substring(0, 40).TrimEnd('-');
        return slug.Length == 0 ? "window" : slug;
    }

    private static string UniqueName(string name, HashSet<string> used)
    {
        string candidate = name;
        for (int i = 2; !used.Add(candidate); i++)
            candidate = name + "-" + i;
        return candidate;
    }
}
