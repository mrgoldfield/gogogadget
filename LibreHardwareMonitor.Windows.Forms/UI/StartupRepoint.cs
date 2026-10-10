// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using LibreHardwareMonitor.Windows.Forms.Utilities;
using Microsoft.Win32.TaskScheduler;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition: keeps "Run On Windows Startup" working across updates.
// The autostart entry (Scheduled Task, or the Run registry value when not
// elevated) stores the full exe path, and every release is unzipped into a
// new folder. When the entry points to an exe that no longer exists (the
// old version's folder was deleted), StartupManager points it at this exe
// instead, so the user doesn't have to re-tick the option after an update.
// An entry pointing to an exe that still exists is left alone, so two
// kept copies never take the entry from each other. Screenshot runs never
// touch it. Hooks: two marked lines in StartupManager's constructor.
internal static class StartupRepoint
{
    /// <summary>True when <paramref name="storedPath"/> is set, is not this exe, and no longer exists.</summary>
    public static bool PointsToMissingExe(string storedPath)
    {
        if (SelfGrab.Active || string.IsNullOrWhiteSpace(storedPath))
            return false;

        try
        {
            string path = Environment.ExpandEnvironmentVariables(storedPath.Trim().Trim('"'));
            if (path.Equals(Application.ExecutablePath, StringComparison.OrdinalIgnoreCase))
                return false;

            return !File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>True when the task starts an exe that no longer exists (and none that does).</summary>
    public static bool PointsToMissingExe(Task task)
    {
        try
        {
            ExecAction[] actions = task.Definition.Actions.OfType<ExecAction>().ToArray();
            return actions.Length > 0 && actions.All(a => PointsToMissingExe(a.Path));
        }
        catch
        {
            return false;
        }
    }

    /// <summary>The task's exe paths, for the log.</summary>
    public static string ExePaths(Task task)
    {
        try
        {
            return string.Join("; ", task.Definition.Actions.OfType<ExecAction>().Select(a => a.Path));
        }
        catch
        {
            return "?";
        }
    }

    /// <summary>Runs <paramref name="repoint"/> (re-creates the entry for this exe); false if it threw.</summary>
    public static bool TryRepoint(System.Action repoint, string storedPath)
    {
        try
        {
            repoint();
            DebugLog.Write("Startup", $"Autostart pointed to a missing exe ({storedPath}); now points to {Application.ExecutablePath}");
            return true;
        }
        catch (Exception ex)
        {
            DebugLog.Write("Startup", $"Autostart repoint failed: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
