using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    internal class Table
    {
        public readonly VisualElement VisualElement;

        public readonly Row Header;
        public readonly List<Row> Rows = new List<Row>();

        public Table(IEnumerable<ColumnInfo> columns)
        {
            VisualElement = new VisualElement();
            Header = AddRow();

            // add headers
            foreach (var c in columns)
            {
                var ele = Header.AddElement(c, c.Header);
                var eleStyle = ele.style;
                // make header element thicker
                eleStyle.unityFontStyleAndWeight = FontStyle.Bold;
                eleStyle.borderBottomWidth = 3;
                eleStyle.borderRightWidth = 3;
            }
        }

        public Row AddRow()
        {
            var row = new Row(this);
            Rows.Add(row);
            return row;
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

    internal class Row
    {
        public readonly Table Table;
        public readonly VisualElement VisualElement;

        public Row(Table table)
        {
            Table = table;

            VisualElement = new VisualElement();
            VisualElement.style.flexDirection = FlexDirection.Row;
            table.VisualElement.Add(VisualElement);
        }

        public Label AddElement(ColumnInfo column, object obj)
        {
            return AddElement(column, obj.ToString());
        }
        public Label AddElement(ColumnInfo column, string text)
        {
            var label = AddElement(column);
            label.text = text;
            return label;
        }
        public Label AddElement(ColumnInfo column)
        {
            var label = new Label();
            VisualElement.Add(label);
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
            return label;
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
