using System.Collections.Generic;
using Mirage.NetworkProfiler.ModuleGUI.UITable;

namespace Mirage.NetworkProfiler.ModuleGUI.Messages
{
    internal sealed class Columns : IEnumerable<ColumnInfo>
    {
        private const int EXPAND_WIDTH = 25;
        private const int NAME_WIDTH = 300;
        private const int OTHER_WIDTH = 100;

        public readonly ColumnInfo Expand;
        public readonly ColumnInfo FullName;
        public readonly ColumnInfo TotalBytes;
        public readonly ColumnInfo Count;
        public readonly ColumnInfo BytesPerMessage;
        public readonly ColumnInfo NetId;
        public readonly ColumnInfo ObjectName;

        public Columns()
        {
            Expand = new ColumnInfo("+", EXPAND_WIDTH);

            FullName = new ColumnInfo("Message", NAME_WIDTH);
            FullName.AddSort(m => m.Name, m => m.Name);

            TotalBytes = new ColumnInfo("Total Bytes", OTHER_WIDTH);
            TotalBytes.AddSort(m => m.TotalBytes, m => m.TotalBytes);

            Count = new ColumnInfo("Count", OTHER_WIDTH);
            Count.AddSort(m => m.TotalCount, m => m.Count);

            BytesPerMessage = new ColumnInfo("Bytes", OTHER_WIDTH);
            BytesPerMessage.AddSort(null, m => m.Bytes);

            NetId = new ColumnInfo("Net id", OTHER_WIDTH);
            NetId.AddSort(null, m => m.NetId.GetValueOrDefault());

            ObjectName = new ColumnInfo("GameObject Name", NAME_WIDTH);
            ObjectName.AddSort(null, m => m.ObjectName);
        }


        public IEnumerator<ColumnInfo> GetEnumerator()
        {
            yield return Expand;
            yield return FullName;
            yield return TotalBytes;
            yield return Count;
            yield return BytesPerMessage;
            yield return NetId;
            yield return ObjectName;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
