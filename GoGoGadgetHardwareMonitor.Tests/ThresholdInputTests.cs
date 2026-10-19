// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibreHardwareMonitor.Windows.Forms.UI;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// What the Gradient Threshold dialog accepts as a number.
/// </summary>
public class ThresholdInputTests
{
    [Theory]
    [InlineData("72.5", 72.5f)]
    [InlineData("90", 90f)]
    [InlineData("-10", -10f)]
    [InlineData(" 50 ", 50f)]
    public void Numbers_Parse(string text, float expected)
    {
        Assert.True(ThresholdInput.TryParse(text, out float value));
        Assert.Equal(expected, value);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    [InlineData("1e40")]
    [InlineData("")]
    [InlineData("abc")]
    public void NonFiniteOrGarbage_IsRejected(string text)
    {
        Assert.False(ThresholdInput.TryParse(text, out _));
    }
}
