using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler.ModuleGUI
{
    class Table
    {
        public readonly VisualElement VisualElement;

        public readonly Row Header;
        public readonly List<Row> Rows = new List<Row>();

        public Table(IEnumerable<ColumnInfo> columns)
        {
            VisualElement = new VisualElement();
            Header = AddRow();

            // add headers
            foreach (ColumnInfo c in columns)
            {
                Header.AddElement(c, c.Header);
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
            foreach (Row row in Rows)
            {
                if (row == Header)
                    continue;

                row.VisualElement.RemoveFromHierarchy();
            }
        }
    }

    class Row
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
            Label label = AddElement(column);
            label.text = text;
            return label;
        }
        public Label AddElement(ColumnInfo column)
        {
            var label = new Label();
            VisualElement.Add(label);
            label.style.width = column.Width;
            return label;
        }
    }

    class ColumnInfo
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
