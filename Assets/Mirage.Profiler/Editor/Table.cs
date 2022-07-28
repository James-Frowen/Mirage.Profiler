using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    internal class Table
    {
        public readonly VisualElement VisualElement;
        public readonly ScrollView ScrollView;

        public readonly SortHeaderRow Header;
        public readonly List<Row> Rows = new List<Row>();
        public readonly IReadOnlyList<ColumnInfo> HeaderInfo;

        public bool ContainsEmptyRows { get; private set; }

        public Table(IEnumerable<ColumnInfo> columns, ITableSorter sorter)
        {
            // create readonly list from given Enumerable
            HeaderInfo = new List<ColumnInfo>(columns);

            // create table root and scroll view
            VisualElement = new VisualElement();
            ScrollView = new ScrollView(ScrollViewMode.VerticalAndHorizontal);

            // add header to table root
            // header will initialize labels, but we need to set text
            Header = new SortHeaderRow(this, sorter);

            // add header and scroll to root
            VisualElement.Add(Header.VisualElement);
            VisualElement.Add(ScrollView);

            // add headers
            foreach (var c in columns)
            {
                var ele = Header.GetLabel(c);
                ele.text = c.Header;

                if (c.AllowSort)
                {
                    var sortHeader = (SortHeader)ele;
                    sortHeader.Info = c;
                }

                // make header element thicker
                var eleStyle = ele.style;
                eleStyle.unityFontStyleAndWeight = FontStyle.Bold;
                eleStyle.borderBottomWidth = 3;
                eleStyle.borderRightWidth = 3;
            }
        }

        public Row AddRow(Row previous = null)
        {
            var row = new LabelRow(this, previous);
            Rows.Add(row);
            return row;
        }

        public Row AddEmptyRow(Row previous = null)
        {
            var row = new EmptyRow(this, previous);
            ContainsEmptyRows = true;
            Rows.Add(row);
            return row;
        }

        public void ChangeWidth(ColumnInfo column, int newWidth, bool setVisibility)
        {
            foreach (var row in Rows)
            {
                if (row is EmptyRow)
                    continue;

                var label = row.GetLabel(column);
                var style = label.style;
                style.width = newWidth;

                if (setVisibility)
                    style.display = newWidth > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>
        /// Removes all rows expect header
        /// </summary>
        public void Clear()
        {
            ScrollView.Clear();
            Rows.Clear();
            Rows.Add(Header);
            ContainsEmptyRows = false;
        }
    }

    internal abstract class Row
    {
        public Table Table { get; }
        public VisualElement VisualElement { get; }

        public Row(Table table, Row previous = null)
        {
            Table = table;

            VisualElement = new VisualElement();
            VisualElement.style.flexDirection = FlexDirection.Row;

            var parent = table.ScrollView;
            if (previous != null)
            {
                var index = parent.IndexOf(previous.VisualElement);
                // insert after previous
                parent.Insert(index + 1, VisualElement);
            }
            else
            {
                // just add at end
                parent.Add(VisualElement);
            }
        }

        public abstract Label GetLabel(ColumnInfo column);
        public abstract IEnumerable<VisualElement> GetChildren();

        public void SetText(ColumnInfo column, object obj)
        {
            SetText(column, obj.ToString());
        }
        public void SetText(ColumnInfo column, string text)
        {
            var label = GetLabel(column);
            label.text = text;
        }
    }

    internal class EmptyRow : Row
    {
        public EmptyRow(Table table, Row previous = null) : base(table, previous) { }

        public override Label GetLabel(ColumnInfo column)
        {
            throw new NotSupportedException("Empty row does not have any columns");
        }

        public override IEnumerable<VisualElement> GetChildren()
        {
            return Enumerable.Empty<VisualElement>();
        }
    }

    internal class LabelRow : Row
    {
        private readonly Dictionary<ColumnInfo, Label> _elements = new Dictionary<ColumnInfo, Label>();

        public LabelRow(Table table, Row previous = null) : base(table, previous)
        {
            foreach (var header in table.HeaderInfo)
            {
                var label = CreateLabel(header);
                VisualElement.Add(label);
                _elements[header] = label;
            }
        }

        public virtual Label CreateLabel(ColumnInfo column)
        {
            var label = new Label();
            SetLabelStyle(column, label);
            return label;
        }

        protected static void SetLabelStyle(ColumnInfo column, Label label)
        {
            var style = label.style;
            style.width = column.Width;

            style.paddingLeft = 5;
            style.paddingRight = 5;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.borderRightColor = Color.white * .4f;
            style.borderBottomColor = Color.white * .4f;
            style.borderBottomWidth = 1;
            style.borderRightWidth = 2;
        }

        public override Label GetLabel(ColumnInfo column)

        {
            return _elements[column];
        }

        public override IEnumerable<VisualElement> GetChildren()
        {
            return _elements.Values;
        }
    }

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

    internal class SortHeader : Label
    {
        public const string ARROW_UP = "\u2191";
        public const string ARROW_DOWN = "\u2193";

        public SortMode SortMode;

        private readonly SortHeaderRow _row;

        private string _nameWithoutSort;
        public string NameWithoutSort
        {
            get
            {
                // this get should be called before any modifications are made
                // so lazy property should be safe here
                if (_nameWithoutSort == null)
                    _nameWithoutSort = text;

                return _nameWithoutSort;
            }
        }

        public ColumnInfo Info { get; internal set; }

        public SortHeader(SortHeaderRow row) : base()
        {
            _row = row;
            this.AddManipulator(new Clickable(OnClick));
        }

        private void OnClick(EventBase evt)
        {
            var sortIndex = (int)SortMode;
            sortIndex = (sortIndex + 1) % 3;
            SortMode = (SortMode)sortIndex;
            _row.UpdateSort(this);
        }

        public void UpdateName()
        {
            switch (SortMode)
            {
                case SortMode.None:
                    text = NameWithoutSort;
                    break;
                case SortMode.Accending:
                    text = ARROW_UP + NameWithoutSort;
                    break;
                case SortMode.Descending:
                    text = ARROW_DOWN + NameWithoutSort;
                    break;
            }
        }
    }
    internal enum SortMode
    {
        None,
        Accending,
        Descending,
    }
    internal interface ITableSorter
    {
        void Sort(Table table, SortHeader header);
    }

    internal class ColumnInfo
    {
        public string Header;
        public int Width;
        public bool AllowSort;

        public ColumnInfo(string header, int width, bool allowSort)
        {
            Header = header;
            Width = width;
            AllowSort = allowSort;
        }
    }
}
