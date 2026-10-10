// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Fork additions Copyright (C) GoGoGadget Hardware Monitor Contributors.

using System;
using System.IO;
using System.Windows.Forms;

namespace LibreHardwareMonitor.Windows.Forms.Utilities;

// Fork addition: where the app keeps the files it writes - the settings
// (.config), the default sensor selection, exported themes, CSV logs and
// crash logs - in two clearly-named subfolders, UserSettings and log.
//
// Both live under %LOCALAPPDATA%\GoGoGadget, so every copy and every new
// version of the app on this PC finds the same settings, wherever its exe
// was unzipped. Local, not Roaming: the settings name sensors by hardware
// IDs that only fit this PC.
//
// Portable mode: an empty file named portable.txt next to the exe keeps
// both folders next to the exe instead (USB stick, side-by-side copies,
// the screenshot test's one-folder-per-scenario runs).
internal static class AppPaths
{
    public const string PortableMarkerFileName = "portable.txt";
    public const string AppDataFolderName = "GoGoGadget";

    private const string UserSettingsDirectoryName = "UserSettings";
    private const string LogDirectoryName = "log";

    // Renamed from "WidgetConfig" - kept only so GetSettingsFileName can
    // migrate a settings file that already landed there for anyone who
    // ran a build between that name and this one.
    private const string PreviousUserSettingsDirectoryName = "WidgetConfig";

    private static string _dataRoot;

    private static string ExeDirectory => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>True when portable.txt sits next to the exe.</summary>
    public static bool IsPortable => File.Exists(Path.Combine(ExeDirectory, PortableMarkerFileName));

    /// <summary>The folder holding UserSettings and log (shown by File -> Open Settings Folder).</summary>
    public static string DataRoot => _dataRoot ??= ResolveDataRoot();

    public static string UserSettingsDirectory => EnsureDirectory(Path.Combine(DataRoot, UserSettingsDirectoryName));
    public static string LogDirectory => EnsureDirectory(Path.Combine(DataRoot, LogDirectoryName));

    // Every user-changed gadget/app setting lives in this one file - see
    // PersistentSettings. One-time migration below brings a pre-existing
    // settings file over from an older location instead of silently
    // starting the user over on an update.
    public static string GetSettingsFileName()
    {
        string legacyFileName = Path.ChangeExtension(Application.ExecutablePath, ".config");
        string fileName = Path.Combine(UserSettingsDirectory, Path.GetFileName(legacyFileName));

        if (!File.Exists(fileName))
        {
            // Builds up to 0.9.7-gogogadget.7 kept UserSettings next to the
            // exe. Copied, not moved, so going back to that older version
            // still finds its own settings.
            string exeUserSettings = Path.Combine(ExeDirectory, UserSettingsDirectoryName);
            if (!PathsEqual(exeUserSettings, UserSettingsDirectory))
                TryCopyMissingFiles(exeUserSettings, UserSettingsDirectory);
        }

        if (!File.Exists(fileName))
        {
            string previousDirFileName = Path.Combine(ExeDirectory, PreviousUserSettingsDirectoryName, Path.GetFileName(legacyFileName));
            TryMigrate(previousDirFileName, fileName);
            TryMigrate(legacyFileName, fileName);
        }

        return fileName;
    }

    // Never throws: the crash logger asks for LogDirectory too, possibly
    // during a failing startup. Falls back to the exe folder.
    private static string ResolveDataRoot()
    {
        if (IsPortable)
            return ExeDirectory;

        try
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(localAppData))
            {
                string root = Path.Combine(localAppData, AppDataFolderName);
                Directory.CreateDirectory(root);
                return root;
            }
        }
        catch
        {
            // fall through to the exe folder
        }

        return ExeDirectory;
    }

    private static void TryCopyMissingFiles(string sourceDirectory, string destinationDirectory)
    {
        try
        {
            if (!Directory.Exists(sourceDirectory))
                return;

            foreach (string source in Directory.GetFiles(sourceDirectory))
            {
                string destination = Path.Combine(destinationDirectory, Path.GetFileName(source));
                if (!File.Exists(destination))
                    File.Copy(source, destination);
            }

            foreach (string sourceSub in Directory.GetDirectories(sourceDirectory))
            {
                string destinationSub = Path.Combine(destinationDirectory, Path.GetFileName(sourceSub));
                Directory.CreateDirectory(destinationSub);
                TryCopyMissingFiles(sourceSub, destinationSub);
            }
        }
        catch
        {
            // Best-effort - a failed copy leaves the old folder untouched
            // and the app starts with whatever arrived.
        }
    }

    private static bool TryMigrate(string sourceFileName, string destinationFileName)
    {
        if (File.Exists(destinationFileName) || !File.Exists(sourceFileName))
            return false;

        try
        {
            File.Move(sourceFileName, destinationFileName);

            string sourceBackup = sourceFileName + ".backup";
            if (File.Exists(sourceBackup))
                File.Move(sourceBackup, destinationFileName + ".backup");

            return true;
        }
        catch
        {
            // Best-effort - if migration fails, the caller just loads an
            // empty settings file from UserSettings instead of crashing
            // over it, and the old file is left in place.
            return false;
        }
    }

    private static bool PathsEqual(string a, string b) =>
        string.Equals(Path.GetFullPath(a).TrimEnd('\\', '/'), Path.GetFullPath(b).TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);

    private static string EnsureDirectory(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
