// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibreHardwareMonitor.Windows.Forms.UI;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// Theme/Profile import checks and the clamps that keep a bad config from
/// crashing the gadget (Fable review H2).
/// </summary>
public class GadgetSettingLimitsTests
{
    private const string Sensor = "/intelcpu/0/load/0/";

    [Theory]
    [InlineData(0f, 4f)]
    [InlineData(-3f, 4f)]
    [InlineData(float.NaN, 7.5f)]
    [InlineData(float.PositiveInfinity, 7.5f)]
    [InlineData(500f, 40f)]
    [InlineData(9f, 9f)]
    public void FontSize_IsClamped(float input, float expected)
    {
        Assert.Equal(expected, GadgetSettingLimits.ClampFontSize(input));
    }

    [Theory]
    [InlineData(0f, 0.5f)]
    [InlineData(float.NaN, 1f)]
    [InlineData(50f, 4f)]
    [InlineData(1.5f, 1.5f)]
    public void ScaleMultiplier_IsClamped(float input, float expected)
    {
        Assert.Equal(expected, GadgetSettingLimits.ClampScaleMultiplier(input));
    }

    [Theory]
    [InlineData(0, 51)]
    [InlineData(300, 255)]
    [InlineData(153, 153)]
    public void Opacity_IsClamped(int input, int expected)
    {
        Assert.Equal(expected, GadgetSettingLimits.ClampOpacity(input));
    }

    [Theory]
    [InlineData("sensorGadget.FontSize", "9")]
    [InlineData("sensorGadget.FontSize", "7.5")]
    [InlineData("sensorGadget.ScaleMultiplier", "1.25")]
    [InlineData("sensorGadget.Opacity", "204")]
    [InlineData("sensorGadget.FontColor", "FFFFFFFF")]
    [InlineData("sensorGadget.BackgroundColor", "0")]
    [InlineData("sensorGadget.HardwarenamesFull", "true")]
    [InlineData("sensorGadget.gradientBarBackground", "false")]
    [InlineData("sensorGadget.RowIcons", "IconsOnly")]
    [InlineData("sensorGadget.GroupLayout", "Horizontal")]
    [InlineData(Sensor + "gadget", "true")]
    [InlineData(Sensor + "gadget.mini", "false")]
    [InlineData(Sensor + "gadget.order", "3")]
    [InlineData("/intelcpu/0/gadget.hardwareOrder", "0")]
    [InlineData(Sensor + "gadget.barColor", "FF00FF00")]
    [InlineData(Sensor + "gadget.warnAt", "72.5")]
    [InlineData(Sensor + "gadget.critAt", "-10")]
    [InlineData(Sensor + "gadget.displayName", "CPU")]
    [InlineData(Sensor + "gadget.valueDisplay", "Percent")]
    public void GoodValues_AreAccepted(string key, string value)
    {
        Assert.True(GadgetSettingLimits.IsValidImportValue(key, value));
    }

    [Theory]
    [InlineData("sensorGadget.FontSize", "0")]
    [InlineData("sensorGadget.FontSize", "-1")]
    [InlineData("sensorGadget.FontSize", "NaN")]
    [InlineData("sensorGadget.FontSize", "7,5")]
    [InlineData("sensorGadget.ScaleMultiplier", "0")]
    [InlineData("sensorGadget.ScaleMultiplier", "50")]
    [InlineData("sensorGadget.Opacity", "0")]
    [InlineData("sensorGadget.Opacity", "300")]
    [InlineData("sensorGadget.FontColor", "white")]
    [InlineData("sensorGadget.HardwarenamesFull", "yes")]
    [InlineData("sensorGadget.RowIcons", "Sideways")]
    [InlineData("sensorGadget.Width", "300")]
    [InlineData(Sensor + "gadget.warnAt", "NaN")]
    [InlineData(Sensor + "gadget.critAt", "Infinity")]
    [InlineData(Sensor + "gadget.order", "first")]
    [InlineData(Sensor + "gadget.displayName", "   ")]
    [InlineData(Sensor + "gadget.unknownThing", "1")]
    [InlineData(Sensor + "gadgetry", "true")]
    [InlineData("", "1")]
    public void BadValues_AreRejected(string key, string value)
    {
        Assert.False(GadgetSettingLimits.IsValidImportValue(key, value));
    }

    [Fact]
    public void NullValue_IsRejected()
    {
        Assert.False(GadgetSettingLimits.IsValidImportValue("sensorGadget.FontSize", null!));
    }
}
