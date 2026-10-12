// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Drawing;
using System.Globalization;
using LibreHardwareMonitor.Hardware;
using LibreHardwareMonitor.Windows.Forms.UI;
using LibreHardwareMonitor.Windows.Forms.Utilities;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// Per-sensor gadget settings: built-in gradient defaults, the three-state
/// threshold logic (explicit / switched off / default), Value Display, colours,
/// and that it all survives a save + load of the settings file.
/// </summary>
public class GadgetSettingsTests
{
    // --- built-in defaults ---------------------------------------------

    [Theory]
    [InlineData(SensorType.Temperature)]
    [InlineData(SensorType.Load)]
    public void Defaults_TemperatureAndLoad_Are50To90(SensorType type)
    {
        Assert.True(GradientDefaults.TryGetDefault(type, out float warnAt, out float critAt));
        Assert.Equal(50f, warnAt);
        Assert.Equal(90f, critAt);
    }

    [Theory]
    [InlineData(SensorType.Voltage)]
    [InlineData(SensorType.Fan)]
    [InlineData(SensorType.Throughput)]
    [InlineData(SensorType.Data)]
    [InlineData(SensorType.Control)]
    public void Defaults_OtherTypes_HaveNone(SensorType type)
    {
        Assert.False(GradientDefaults.TryGetDefault(type, out _, out _));
    }

    // --- threshold three-state logic -------------------------------------

    [Fact]
    public void Thresholds_TemperatureWithNothingSet_UsesDefault()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Temperature);

        Assert.True(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out float w, out float c));
        Assert.Equal((50f, 90f), (w, c));
    }

    [Fact]
    public void Thresholds_VoltageWithNothingSet_HasNoGradient()
    {
        var settings = new PersistentSettings();
        Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(settings, new FakeSensor(SensorType.Voltage), out _, out _));
    }

    [Fact]
    public void Thresholds_ExplicitValues_WinOverDefault()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Temperature);
        SensorGadgetItemSettings.SetGradientThresholds(settings, sensor, 60f, 80f);

        Assert.True(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out float w, out float c));
        Assert.Equal((60f, 80f), (w, c));
    }

    [Fact]
    public void Thresholds_ClearOnDefaultType_SwitchesGradientOff()
    {
        // Without the "gradientOff" flag, Clear would fall back to the default
        // and look like it did nothing.
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Load);
        SensorGadgetItemSettings.SetGradientThresholds(settings, sensor, 10f, 20f);
        SensorGadgetItemSettings.ClearGradientThresholds(settings, sensor);

        Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out _, out _));
    }

    [Fact]
    public void Thresholds_SetAfterClear_TurnsGradientBackOn()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Temperature);
        SensorGadgetItemSettings.ClearGradientThresholds(settings, sensor);
        SensorGadgetItemSettings.SetGradientThresholds(settings, sensor, 40f, 70f);

        Assert.True(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out float w, out float c));
        Assert.Equal((40f, 70f), (w, c));
    }

    [Fact]
    public void Thresholds_ClearOnNonDefaultType_LeavesNoFlagBehind()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Voltage);
        SensorGadgetItemSettings.SetGradientThresholds(settings, sensor, 1f, 2f);
        SensorGadgetItemSettings.ClearGradientThresholds(settings, sensor);

        Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out _, out _));
        Assert.Empty(settings.GetAll());
    }

    [Fact]
    public void Thresholds_OnlyOneKeyPresent_IsIgnored()
    {
        // A half-written pair (e.g. hand-edited file) must not arm a gradient
        // with a 0 for the missing end.
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Voltage, "/cpu/0/voltage/1");
        settings.SetValue("/cpu/0/voltage/1/gadget.warnAt", 1.2f);

        Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(settings, sensor, out _, out _));
    }

    [Fact]
    public void Thresholds_AreStoredPerSensor()
    {
        var settings = new PersistentSettings();
        var a = new FakeSensor(SensorType.Voltage, "/cpu/0/voltage/0");
        var b = new FakeSensor(SensorType.Voltage, "/cpu/0/voltage/1");
        SensorGadgetItemSettings.SetGradientThresholds(settings, a, 1f, 2f);

        Assert.True(SensorGadgetItemSettings.TryGetGradientThresholds(settings, a, out _, out _));
        Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(settings, b, out _, out _));
    }

    [Fact]
    public void Thresholds_KeyNames_FollowGadgetConvention()
    {
        // Theme/Profile export and old settings files depend on these exact keys.
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Voltage, "/cpu/0/voltage/1");
        SensorGadgetItemSettings.SetGradientThresholds(settings, sensor, 1f, 2f);

        Assert.True(settings.Contains("/cpu/0/voltage/1/gadget.warnAt"));
        Assert.True(settings.Contains("/cpu/0/voltage/1/gadget.critAt"));
    }

    // --- Value Display ---------------------------------------------------

    [Fact]
    public void ValueDisplay_DefaultsToPercent()
    {
        Assert.Equal(ValueDisplayMode.Percent,
                     SensorGadgetItemSettings.GetValueDisplayMode(new PersistentSettings(), new FakeSensor(SensorType.Load)));
    }

    [Fact]
    public void ValueDisplay_BarIsKept()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Load);
        SensorGadgetItemSettings.SetValueDisplayMode(settings, sensor, ValueDisplayMode.Bar);
        Assert.Equal(ValueDisplayMode.Bar, SensorGadgetItemSettings.GetValueDisplayMode(settings, sensor));
    }

    [Theory]
    [InlineData("Both")]
    [InlineData("nonsense")]
    [InlineData("")]
    public void ValueDisplay_BothOrGarbage_FallsBackToPercent(string raw)
    {
        // "Both" is disabled for now (doesn't look good yet); an old or
        // hand-edited settings file must not bring it back.
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Load, "/cpu/0/load/0");
        settings.SetValue("/cpu/0/load/0/gadget.valueDisplay", raw);
        Assert.Equal(ValueDisplayMode.Percent, SensorGadgetItemSettings.GetValueDisplayMode(settings, sensor));
    }

    // --- colours and names -------------------------------------------------

    [Fact]
    public void Colors_SetGetClear_AreIndependent()
    {
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Load);
        SensorGadgetItemSettings.SetBarColor(settings, sensor, Color.FromArgb(255, 10, 20, 30));
        SensorGadgetItemSettings.SetTextColor(settings, sensor, Color.FromArgb(255, 40, 50, 60));
        SensorGadgetItemSettings.SetNameColor(settings, sensor, Color.FromArgb(255, 70, 80, 90));

        SensorGadgetItemSettings.ClearTextColor(settings, sensor);

        Assert.True(SensorGadgetItemSettings.TryGetBarColor(settings, sensor, out Color bar));
        Assert.Equal(Color.FromArgb(255, 10, 20, 30).ToArgb(), bar.ToArgb());
        Assert.False(SensorGadgetItemSettings.TryGetTextColor(settings, sensor, out _));
        Assert.True(SensorGadgetItemSettings.TryGetNameColor(settings, sensor, out Color name));
        Assert.Equal(Color.FromArgb(255, 70, 80, 90).ToArgb(), name.ToArgb());
    }

    [Fact]
    public void DisplayName_EmptyStringCountsAsSet()
    {
        // An explicitly empty display name is a choice (hide the label),
        // not "no display name".
        var settings = new PersistentSettings();
        var sensor = new FakeSensor(SensorType.Load);
        SensorGadgetItemSettings.SetDisplayName(settings, sensor, "");

        Assert.True(SensorGadgetItemSettings.TryGetDisplayName(settings, sensor, out string name));
        Assert.Equal("", name);

        SensorGadgetItemSettings.ClearDisplayName(settings, sensor);
        Assert.False(SensorGadgetItemSettings.TryGetDisplayName(settings, sensor, out _));
    }

    // --- save + load round trip -------------------------------------------

    [Fact]
    public void SettingsFile_RoundTrip_KeepsGadgetSettings_UnderDanishCulture()
    {
        // Goldfield's PC uses a comma decimal separator: thresholds must still
        // be written and read invariantly ("72.5", not "72,5").
        CultureInfo before = CultureInfo.CurrentCulture;
        string file = Path.Combine(Path.GetTempPath(), $"gadget-settings-{Guid.NewGuid():N}.config");
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("da-DK");

            var settings = new PersistentSettings();
            var temp = new FakeSensor(SensorType.Temperature, "/amdcpu/0/temperature/2");
            var load = new FakeSensor(SensorType.Load, "/amdcpu/0/load/0");
            SensorGadgetItemSettings.SetGradientThresholds(settings, temp, 72.5f, 95.25f);
            SensorGadgetItemSettings.ClearGradientThresholds(settings, load);
            SensorGadgetItemSettings.SetBarColor(settings, temp, Color.FromArgb(200, 1, 2, 3));
            SensorGadgetItemSettings.SetValueDisplayMode(settings, load, ValueDisplayMode.Bar);
            SensorGadgetItemSettings.SetDisplayName(settings, temp, "CPU \u00e6\u00f8\u00e5 Temp");
            settings.Save(file);

            var loaded = new PersistentSettings();
            loaded.Load(file);

            Assert.True(SensorGadgetItemSettings.TryGetGradientThresholds(loaded, temp, out float w, out float c));
            Assert.Equal((72.5f, 95.25f), (w, c));
            Assert.False(SensorGadgetItemSettings.TryGetGradientThresholds(loaded, load, out _, out _));
            Assert.True(SensorGadgetItemSettings.TryGetBarColor(loaded, temp, out Color bar));
            Assert.Equal(Color.FromArgb(200, 1, 2, 3).ToArgb(), bar.ToArgb());
            Assert.Equal(ValueDisplayMode.Bar, SensorGadgetItemSettings.GetValueDisplayMode(loaded, load));
            Assert.True(SensorGadgetItemSettings.TryGetDisplayName(loaded, temp, out string name));
            Assert.Equal("CPU \u00e6\u00f8\u00e5 Temp", name);
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
            File.Delete(file);
            File.Delete(file + ".backup");
        }
    }

    [Fact]
    public void SettingsFile_Corrupt_LoadsEmptyInsteadOfThrowing()
    {
        string file = Path.Combine(Path.GetTempPath(), $"gadget-settings-{Guid.NewGuid():N}.config");
        try
        {
            File.WriteAllText(file, "<configuration><appSettings><add key=");
            var loaded = new PersistentSettings();
            loaded.Load(file);
            Assert.Empty(loaded.GetAll());
        }
        finally
        {
            File.Delete(file);
            File.Delete(file + ".backup");
        }
    }
}
