namespace Mirage.NetworkProfiler
{
    internal static class FramesExtensions
    {
        public static Frame GetFrame(this Frame[] frames, int frameIndex)
        {
            return frames[frameIndex % frames.Length];
        }
    }
}
