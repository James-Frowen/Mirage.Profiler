
namespace Mirage.NetworkProfiler.ModuleGUI
{
    internal static class HumanReadableByteFormatter
    {
        public static string Format(long bytes)
        {
            if (bytes >= 1024 * 1024)
                return $"{bytes / 1024f / 1024f:0.##} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024f:0.##} KB";
            return $"{bytes} B";
        }
    }
}
