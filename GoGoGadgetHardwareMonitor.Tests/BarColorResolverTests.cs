// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System.Drawing;
using LibreHardwareMonitor.Windows.Forms.UI;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// The green -> yellow -> red gradient between a sensor's threshold (warnAt)
/// and max (critAt).
/// </summary>
public class BarColorResolverTests
{
    private static readonly Color Green = Color.FromArgb(255, 0, 255, 0);
    private static readonly Color Yellow = Color.FromArgb(255, 255, 255, 0);
    private static readonly Color Red = Color.FromArgb(255, 255, 0, 0);

    [Theory]
    [InlineData(0f)]
    [InlineData(30f)]
    [InlineData(50f)]
    [InlineData(-40f)]
    public void AtOrBelowThreshold_IsGreen(float value)
    {
        Assert.Equal(Green, BarColorResolver.ResolveGradientColor(value, 50f, 90f));
    }

    [Theory]
    [InlineData(90f)]
    [InlineData(95f)]
    [InlineData(1000f)]
    public void AtOrAboveMax_IsRed(float value)
    {
        Assert.Equal(Red, BarColorResolver.ResolveGradientColor(value, 50f, 90f));
    }

    [Fact]
    public void Midpoint_IsYellowish()
    {
        // 32 buckets: the exact midpoint lands between buckets 15 and 16,
        // so allow a small step either side of pure yellow.
        Color c = BarColorResolver.ResolveGradientColor(70f, 50f, 90f);
        Assert.Equal(255, c.A);
        Assert.InRange(c.R, 240, 255);
        Assert.InRange(c.G, 240, 255);
        Assert.Equal(0, c.B);
    }

    [Fact]
    public void Colors_AreAlwaysOpaque_WithNoBlue()
    {
        for (float v = 40f; v <= 100f; v += 0.25f)
        {
            Color c = BarColorResolver.ResolveGradientColor(v, 50f, 90f);
            Assert.Equal(255, c.A);
            Assert.Equal(0, c.B);
        }
    }

    [Fact]
    public void RisingValue_NeverGetsGreener()
    {
        Color previous = BarColorResolver.ResolveGradientColor(50f, 50f, 90f);
        for (float v = 50f; v <= 90f; v += 0.1f)
        {
            Color c = BarColorResolver.ResolveGradientColor(v, 50f, 90f);
            Assert.True(c.R >= previous.R, $"red dropped at {v}");
            Assert.True(c.G <= previous.G, $"green rose at {v}");
            previous = c;
        }
    }

    [Fact]
    public void ColorSet_StaysBounded_ForTintCache()
    {
        // SensorGadget caches one tinted bar image per colour: a sensor that
        // ticks through thousands of values must only ever yield a fixed set.
        var seen = new HashSet<Color>();
        for (int i = 0; i <= 100_000; i++)
            seen.Add(BarColorResolver.ResolveGradientColor(i / 1000f, 0f, 100f));

        Assert.True(seen.Count <= BarColorResolver.GradientBuckets,
                    $"{seen.Count} colours, expected at most {BarColorResolver.GradientBuckets}");
        Assert.True(seen.Count >= BarColorResolver.GradientBuckets - 1);
    }

    [Theory]
    [InlineData(90f, 50f)]
    [InlineData(70f, 70f)]
    public void InvertedOrEmptyRange_IsRed(float warnAt, float critAt)
    {
        // Treated as "already at max" rather than dividing by zero.
        Assert.Equal(Red, BarColorResolver.ResolveGradientColor(0f, warnAt, critAt));
        Assert.Equal(Red, BarColorResolver.ResolveGradientColor(100f, warnAt, critAt));
    }

    [Fact]
    public void WorksForNegativeRanges()
    {
        // e.g. a temperature range below zero
        Assert.Equal(Green, BarColorResolver.ResolveGradientColor(-30f, -20f, -10f));
        Assert.Equal(Red, BarColorResolver.ResolveGradientColor(-5f, -20f, -10f));
    }

    [Fact]
    public void GreenAndRedEnds_MatchExpected()
    {
        Assert.Equal(Green, BarColorResolver.ResolveGradientColor(0f, 0f, 100f));
        Assert.Equal(Red, BarColorResolver.ResolveGradientColor(100f, 0f, 100f));
        Assert.NotEqual(Yellow, BarColorResolver.ResolveGradientColor(10f, 0f, 100f));
    }
}
