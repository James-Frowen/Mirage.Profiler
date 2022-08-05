using System.Collections.Generic;
using Mirage.NetworkProfiler.ModuleGUI.UITable;

namespace Mirage.NetworkProfiler.ModuleGUI.Messages
{
    internal sealed class Columns : IEnumerable<ColumnInfo>
    {
        private const int EXPAND_WIDTH = 25;
        private const int NAME_WIDTH = 300;
        private const int OTHER_WIDTH = 100;

        public readonly ColumnInfo Expand = new ColumnInfo("+", EXPAND_WIDTH, false);
        public readonly ColumnInfo FullName = new ColumnInfo("Message", NAME_WIDTH, true);
        public readonly ColumnInfo TotalBytes = new ColumnInfo("Total Bytes", OTHER_WIDTH, true);
        public readonly ColumnInfo Count = new ColumnInfo("Count", OTHER_WIDTH, true);
        public readonly ColumnInfo BytesPerMessage = new ColumnInfo("Bytes", OTHER_WIDTH, true);
        public readonly ColumnInfo NetId = new ColumnInfo("Net id", OTHER_WIDTH, true);

        public IEnumerator<ColumnInfo> GetEnumerator()
        {
            yield return Expand;
            yield return FullName;
            yield return TotalBytes;
            yield return Count;
            yield return BytesPerMessage;
            yield return NetId;
        }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
