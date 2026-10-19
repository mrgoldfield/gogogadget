// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System.Globalization;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition: parses a number typed into the Gradient Threshold dialog.
// The PC's own decimal separator first (Danish "72,5"), then the invariant
// dot ("72.5") so a dot always works too (Fable review M10). No thousands
// separators: in da-DK "1.000" would otherwise read as 1000. Only finite
// numbers count: "NaN" / "Infinity" parse as floats but break the gradient
// on every paint (Fable review M9).
public static class ThresholdInput
{
    public static bool TryParse(string text, out float value) =>
        TryParse(text, CultureInfo.CurrentCulture, out value);

    public static bool TryParse(string text, CultureInfo culture, out float value)
    {
        if ((float.TryParse(text, NumberStyles.Float, culture, out value) ||
             float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)) &&
            BarColorResolver.IsFinite(value))
        {
            return true;
        }

        value = 0f;
        return false;
    }

    // Pre-fill text for the dialog, in the PC's own format.
    public static string Format(float? value) =>
        value?.ToString(CultureInfo.CurrentCulture) ?? string.Empty;
}
