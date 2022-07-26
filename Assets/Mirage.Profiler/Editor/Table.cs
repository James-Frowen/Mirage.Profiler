using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    internal class Table
    {
        public readonly ScrollView VisualElement;

        public readonly Row Header;
        public readonly List<Row> Rows = new List<Row>();
        public readonly IReadOnlyList<ColumnInfo> HeaderInfo;

        public Table(IEnumerable<ColumnInfo> columns)
        {
            VisualElement = new ScrollView(ScrollViewMode.VerticalAndHorizontal);

            // create readonly list from given Enumerable
            HeaderInfo = new List<ColumnInfo>(columns);

            // header will initialize labels, but we need to set text
            Header = AddRow();

            // add headers
            foreach (var c in columns)
            {
                var ele = Header.GetLabel(c);
                ele.text = c.Header;

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
            foreach (var row in Rows)
            {
                if (row == Header)
                    continue;

                row.VisualElement.RemoveFromHierarchy();
            }
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

            if (previous != null)
            {
                var index = table.VisualElement.IndexOf(previous.VisualElement);
                // insert after previous
                table.VisualElement.Insert(index + 1, VisualElement);
            }
            else
            {
                // just add at end
                table.VisualElement.Add(VisualElement);
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
                var label = CreateLabel(header.Width);
                VisualElement.Add(label);
                _elements[header] = label;
            }
        }

        public static Label CreateLabel(int width)
        {
            var label = new Label();
            var style = label.style;
            style.width = width;

            style.paddingLeft = 5;
            style.paddingRight = 5;
            style.paddingTop = 5;
            style.paddingBottom = 5;
            style.borderRightColor = Color.white * .4f;
            style.borderBottomColor = Color.white * .4f;
            style.borderBottomWidth = 1;
            style.borderRightWidth = 2;
            return label;
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

    internal class ColumnInfo
    {
        public string Header;
        public int Width;

        public ColumnInfo(string header, int width)
        {
            Header = header;
            Width = width;
        }
    }
}
