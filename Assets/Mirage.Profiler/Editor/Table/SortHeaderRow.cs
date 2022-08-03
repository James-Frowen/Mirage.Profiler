using System;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI.UITable
{
    internal class SortHeaderRow : LabelRow
    {
        private readonly ITableSorter _sorter;
        private SortHeader _currentSort;

        public SortHeaderRow(Table table, ITableSorter sorter) : base(table, null)
        {
            _sorter = sorter ?? throw new ArgumentNullException(nameof(sorter));
        }

        public override Label CreateLabel(ColumnInfo column)
        {
            var label = column.AllowSort
                ? new SortHeader(this)
                : new Label();

            SetLabelStyle(column, label);
            return label;
        }


        internal void UpdateSort(SortHeader sortHeader)
        {
            // not null or current
            if (_currentSort != null && _currentSort != sortHeader)
            {
                _currentSort.SortMode = SortMode.None;
                _currentSort.UpdateName();
            }

            _currentSort = sortHeader;
            _currentSort.UpdateName();

            _sorter.Sort(Table, sortHeader);
        }
    }
}
