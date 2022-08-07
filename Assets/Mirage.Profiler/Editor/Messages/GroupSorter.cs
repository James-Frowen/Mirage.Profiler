using System;
using System.Collections.Generic;
using Mirage.NetworkProfiler.ModuleGUI.UITable;

namespace Mirage.NetworkProfiler.ModuleGUI.Messages
{
    internal struct GroupSorter
    {
        private readonly Dictionary<string, Group> _grouped;
        private readonly ColumnInfo _sortHeader;
        private readonly SortMode _sortMode;

        public GroupSorter(Dictionary<string, Group> grouped, ColumnInfo sortHeader, SortMode sortMode)
        {
            _grouped = grouped;
            _sortHeader = sortHeader;
            _sortMode = sortMode;
        }

        public void Sort()
        {
            var groups = new List<Group>(_grouped.Values);

            // sort all groups and their messages
            groups.Sort(CompareGroupSortMode);
            foreach (var group in groups)
            {
                group.Messages.Sort(CompareDrawnSortMode);
            }

            // apply sort to table
            foreach (var group in groups)
            {
                // use BringToFront so that each new element is placed after the last one, bring them all to their correct position

                // head might be null if messages are ungrouped
                group.Head?.VisualElement.BringToFront();
                foreach (var msg in group.Messages)
                {
                    // row might be null before it is drawn for first time
                    msg.Row?.VisualElement.BringToFront();
                }
            }
        }

        private int CompareGroupSortMode(Group x, Group y)
        {
            var sort = _sortHeader.SortGroup.Invoke(x, y);
            return CheckSortMode(sort);
        }

        private int CompareDrawnSortMode(DrawnMessage x, DrawnMessage y)
        {
            var sort = _sortHeader.SortMessages.Invoke(x.Info, y.Info);
            return CheckSortMode(sort);
        }

        private int CheckSortMode(int sort)
        {
            // flip order if Descending
            if (_sortMode == SortMode.Descending)
                return -sort;

            return sort;
        }

        public static int Compare<T>(Group x, Group y, Func<Group, T> func) where T : IComparable<T>
        {
            var xValue = func.Invoke(x);
            var yValue = func.Invoke(y);
            return xValue.CompareTo(yValue);
        }
        public static int Compare<T>(MessageInfo x, MessageInfo y, Func<MessageInfo, T> func) where T : IComparable<T>
        {
            var xValue = func.Invoke(x);
            var yValue = func.Invoke(y);
            return xValue.CompareTo(yValue);
        }

        public static int DefaultGroupSort(Group x, Group y)
        {
            return x.Order.CompareTo(y.Order);
        }
        public static int DefaultMessageSort(MessageInfo x, MessageInfo y)
        {
            return x.Order.CompareTo(y.Order);
        }
    }
}
