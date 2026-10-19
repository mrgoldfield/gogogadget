// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
// If a copy of the MPL was not distributed with this file, You can obtain one at http://mozilla.org/MPL/2.0/.
// Copyright (C) LibreHardwareMonitor and Contributors.
// Partial Copyright (C) Michael Möller <mmoeller@openhardwaremonitor.org> and Contributors.
// All Rights Reserved.

using System;
using System.Collections.Generic;

namespace LibreHardwareMonitor.Windows.Forms.UI;

// Fork addition (Fable review M1): Move Up/Down for sensor rows and
// hardware groups, acting on what is visible. In Mini mode the lists also
// hold rows/groups that aren't drawn; swapping with those made a click
// do nothing on screen. Here an item jumps past hidden neighbours to the
// next visible one, and hidden items keep their order relative to each
// other. With everything visible (Full mode) it is a plain swap.
// WinForms-free so the test project can link it.
public static class GadgetOrdering
{
    /// <summary>Index of the nearest visible item from <paramref name="item"/> in
    /// <paramref name="direction"/> (-1 up, +1 down), or -1 if there is none.</summary>
    public static int FindVisibleNeighbour<T>(IList<T> list, T item, int direction, Func<T, bool> isVisible)
    {
        int index = list.IndexOf(item);
        if (index < 0 || direction == 0)
            return -1;

        int step = Math.Sign(direction);
        for (int i = index + step; i >= 0 && i < list.Count; i += step)
        {
            if (isVisible(list[i]))
                return i;
        }

        return -1;
    }

    public static bool CanMove<T>(IList<T> list, T item, int direction, Func<T, bool> isVisible) =>
        FindVisibleNeighbour(list, item, direction, isVisible) >= 0;

    /// <summary>Moves <paramref name="item"/> just past its nearest visible
    /// neighbour in <paramref name="direction"/>. Returns false (list
    /// untouched) when there is no such neighbour.</summary>
    public static bool Move<T>(IList<T> list, T item, int direction, Func<T, bool> isVisible)
    {
        int neighbour = FindVisibleNeighbour(list, item, direction, isVisible);
        if (neighbour < 0)
            return false;

        // Up: the neighbour's index is unchanged by the removal, so the item
        // lands right before it. Down: the neighbour shifts one left, so
        // inserting at its old index puts the item right after it.
        list.Remove(item);
        list.Insert(neighbour, item);
        return true;
    }
}
