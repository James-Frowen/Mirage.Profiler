using System;
using System.Collections.Generic;
using Mirage.NetworkProfiler.ModuleGUI.UITable;

namespace Mirage.NetworkProfiler.ModuleGUI.Messages
{
    internal struct GroupSorter
    {
        private readonly Columns _columns;
        private readonly Dictionary<string, Group> _grouped;
        private readonly ColumnInfo _sortHeader;
        private readonly SortMode _sortMode;

        public GroupSorter(Columns columns, Dictionary<string, Group> grouped, ColumnInfo sortHeader, SortMode sortMode)
        {
            _columns = columns;
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
            var sort = CompareGroup(x, y);
            return CheckSortMode(sort);
        }

        private int CompareDrawnSortMode(DrawnMessage x, DrawnMessage y)
        {
            var sort = CompareDrawn(x, y);
            return CheckSortMode(sort);
        }

        private int CheckSortMode(int sort)
        {
            // flip order if Descending
            if (_sortMode == SortMode.Descending)
                return -sort;

            return sort;
        }

        private int CompareGroup(Group x, Group y)
        {
            // find matching coloum and sort using it
            if (IsHeader(_columns.FullName))
                return Compare(x, y, m => m.Name);

            if (IsHeader(_columns.TotalBytes))
                return Compare(x, y, m => m.TotalBytes);

            if (IsHeader(_columns.Count))
                return Compare(x, y, m => m.TotalCount);

            // else just use order
            // for example if someone is sorting by netid
            return x.Order.CompareTo(y.Order);
        }

        private int CompareDrawn(DrawnMessage x, DrawnMessage y)
        {
            return CompareMessage(x.Info, y.Info);
        }
        private int CompareMessage(MessageInfo x, MessageInfo y)
        {
            // find matching coloum and sort using it
            if (IsHeader(_columns.FullName))
                return Compare(x, y, m => m.Name);

            if (IsHeader(_columns.TotalBytes))
                return Compare(x, y, m => m.TotalBytes);

            if (IsHeader(_columns.Count))
                return Compare(x, y, m => m.Count);

            if (IsHeader(_columns.BytesPerMessage))
                return Compare(x, y, m => m.Bytes);

            if (IsHeader(_columns.NetId))
                return Compare(x, y, m => m.NetId.GetValueOrDefault());

            // else, header not found just use order
            return x.Order.CompareTo(y.Order);
        }

        private bool IsHeader(ColumnInfo info)
        {
            return _sortHeader != null && _sortHeader == info;
        }

        private int Compare<T>(Group x, Group y, Func<Group, T> func) where T : IComparable<T>
        {
            var xValue = func.Invoke(x);
            var yValue = func.Invoke(y);
            return xValue.CompareTo(yValue);
        }
        private int Compare<T>(MessageInfo x, MessageInfo y, Func<MessageInfo, T> func) where T : IComparable<T>
        {
            var xValue = func.Invoke(x);
            var yValue = func.Invoke(y);
            return xValue.CompareTo(yValue);
        }
    }
}
