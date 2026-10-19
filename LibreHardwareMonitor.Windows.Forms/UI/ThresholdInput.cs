// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Globalization;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition: parses a number typed into the Gradient Threshold dialog.
// Only finite numbers count: "NaN" / "Infinity" parse as floats but break
// the gradient on every paint (Fable review M9).
public static class ThresholdInput
{
    public static bool TryParse(string text, out float value)
    {
        if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
            BarColorResolver.IsFinite(value))
        {
            return true;
        }

        value = 0f;
        return false;
    }
}
