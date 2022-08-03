namespace Mirage.NetworkProfiler.ModuleGUI.UITable
{
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
