// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) GoGoGadget Hardware Monitor Contributors.

using LibreHardwareMonitor.Hardware.Storage;

namespace LibreHardwareMonitor.Windows.Forms.Utilities;

// Temporary diagnostic: routes StorageDevice.DiagnosticLog (drive activity stuck
// after the first reading) into GoGoGadgetDebug.log under [Storage]. Each event is
// logged once per drive. Remove together with StorageDevice.DiagnosticLog.
internal static class StorageDiagnostics
{
    public static void Attach() => StorageDevice.DiagnosticLog = message => DebugLog.Write("Storage", message);
}
