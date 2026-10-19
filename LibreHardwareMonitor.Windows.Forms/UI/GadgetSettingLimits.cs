// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Globalization;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition (Fable review H2): the allowed range of every gadget setting
// a Theme/Profile file can carry, used twice:
//  - at import, IsValidImportValue drops any value outside it (so a bad file
//    can't write it into the settings at all), and
//  - when the gadget applies a value (Clamp*), so a bad value that got into
//    the config some other way (hand edit, older build) can never take the
//    app down: a FontSize or ScaleMultiplier of 0/NaN used to throw in
//    new Font(...) on every start, an Opacity of 0 made the gadget an
//    invisible, click-through window.
// WinForms-free so the test project can link it.
public static class GadgetSettingLimits
{
    public const float DefaultFontSize = 7.5f;
    public const float MinFontSize = 4f;
    public const float MaxFontSize = 40f;

    public const float DefaultScaleMultiplier = 1f;
    public const float MinScaleMultiplier = 0.5f;
    public const float MaxScaleMultiplier = 4f;

    // 51 = the lowest step of the Opacity menu (20 %).
    public const int MinOpacity = 51;
    public const int MaxOpacity = 255;

    public const int MaxDisplayNameLength = 200;

    public static float ClampFontSize(float size) =>
        Clamp(size, MinFontSize, MaxFontSize, DefaultFontSize);

    public static float ClampScaleMultiplier(float scale) =>
        Clamp(scale, MinScaleMultiplier, MaxScaleMultiplier, DefaultScaleMultiplier);

    public static int ClampOpacity(int opacity) =>
        Math.Min(MaxOpacity, Math.Max(MinOpacity, opacity));

    private static float Clamp(float x, float min, float max, float fallback)
    {
        if (!BarColorResolver.IsFinite(x))
            return fallback;

        return Math.Min(max, Math.Max(min, x));
    }

    /// <summary>
    /// True when <paramref name="value"/> is a value the gadget can use for
    /// <paramref name="key"/> (a Theme key like "sensorGadget.FontSize", or a
    /// per-sensor/per-hardware key ending in "/gadget" or "/gadget.&lt;name&gt;").
    /// Unknown keys are not valid.
    /// </summary>
    public static bool IsValidImportValue(string key, string value)
    {
        if (string.IsNullOrEmpty(key) || value == null)
            return false;

        switch (key)
        {
            case "sensorGadget.FontSize":
                return TryFloat(value, out float size) && size >= MinFontSize && size <= MaxFontSize;
            case "sensorGadget.ScaleMultiplier":
                return TryFloat(value, out float scale) && scale >= MinScaleMultiplier && scale <= MaxScaleMultiplier;
            case "sensorGadget.Opacity":
                return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int opacity) &&
                       opacity >= MinOpacity && opacity <= MaxOpacity;
            case "sensorGadget.FontColor":
            case "sensorGadget.BackgroundColor":
                return IsColor(value);
            case "sensorGadget.HardwarenamesFull":
            case "sensorGadget.HardwarenamesMini":
            case "sensorGadget.gradientBarBackground":
                return IsBool(value);
            case "sensorGadget.RowIcons":
                return IsOneOf(value, "Off", "WithName", "IconsOnly");
            case "sensorGadget.GroupLayout":
                return IsOneOf(value, "Vertical", "Horizontal");
        }

        if (key[0] != '/')
            return false;

        string suffix = key.Substring(key.LastIndexOf('/') + 1);
        switch (suffix)
        {
            case "gadget":
            case "gadget.mini":
            case "gadget.gradientOff":
                return IsBool(value);
            case "gadget.order":
            case "gadget.hardwareOrder":
                return int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out _);
            case "gadget.barColor":
            case "gadget.textColor":
            case "gadget.nameColor":
                return IsColor(value);
            case "gadget.warnAt":
            case "gadget.critAt":
                return TryFloat(value, out _);
            case "gadget.displayName":
                return value.Trim().Length > 0 && value.Length <= MaxDisplayNameLength;
            case "gadget.valueDisplay":
                return IsOneOf(value, "Bar", "Percent", "Both");
            default:
                return false;
        }
    }

    // Finite only: "NaN"/"Infinity" parse as floats.
    private static bool TryFloat(string value, out float result) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) &&
        BarColorResolver.IsFinite(result);

    // PersistentSettings stores colours as 8 hex digits (ARGB, "X8").
    private static bool IsColor(string value) =>
        value.Length >= 1 && value.Length <= 8 &&
        int.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);

    private static bool IsBool(string value) => value == "true" || value == "false";

    private static bool IsOneOf(string value, params string[] names) =>
        Array.IndexOf(names, value) >= 0;
}
