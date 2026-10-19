// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.

using LibreHardwareMonitor.Windows.Forms.UI;

namespace GoGoGadgetHardwareMonitor.Tests;

/// <summary>
/// Move Up/Down acts on visible rows/groups only (Fable review M1).
/// Lower-case letters are hidden (not in Mini), upper-case are visible.
/// </summary>
public class GadgetOrderingTests
{
    private static bool Visible(string s) => char.IsUpper(s[0]);
    private static bool All(string s) => true;

    private static string Order(List<string> list) => string.Join("", list);

    [Theory]
    // Full mode: plain swap.
    [InlineData("ABCD", "B", -1, "BACD")]
    [InlineData("ABCD", "B", 1, "ACBD")]
    public void FullMode_Swaps(string start, string item, int direction, string expected)
    {
        List<string> list = start.Select(c => c.ToString()).ToList();
        Assert.True(GadgetOrdering.Move(list, item, direction, All));
        Assert.Equal(expected, Order(list));
    }

    [Theory]
    // One click moves one visible row, past any hidden ones.
    [InlineData("AxyB", "B", -1, "BAxy")]
    [InlineData("AxyB", "A", 1, "xyBA")]
    [InlineData("xAyBz", "B", -1, "xBAyz")]
    [InlineData("xAyBz", "A", 1, "xyBAz")]
    public void MiniMode_JumpsPastHiddenRows(string start, string item, int direction, string expected)
    {
        List<string> list = start.Select(c => c.ToString()).ToList();
        Assert.True(GadgetOrdering.Move(list, item, direction, Visible));
        Assert.Equal(expected, Order(list));
    }

    [Theory]
    [InlineData("xAyB", "A", -1)]
    [InlineData("AxBy", "B", 1)]
    [InlineData("A", "A", 1)]
    public void TopOrBottomVisibleRow_CannotMove(string start, string item, int direction)
    {
        List<string> list = start.Select(c => c.ToString()).ToList();
        Assert.False(GadgetOrdering.CanMove(list, item, direction, Visible));
        Assert.False(GadgetOrdering.Move(list, item, direction, Visible));
        Assert.Equal(start, Order(list));
    }

    [Fact]
    public void ItemNotInList_DoesNothing()
    {
        List<string> list = new() { "A", "B" };
        Assert.False(GadgetOrdering.Move(list, "Q", 1, All));
        Assert.Equal("AB", Order(list));
    }
}
