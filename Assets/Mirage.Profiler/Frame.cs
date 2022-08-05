using System.Collections.Generic;

namespace Mirage.NetworkProfiler
{
    [System.Serializable]
    internal class Frame
    {
        public List<MessageInfo> Messages = new List<MessageInfo>();
        public int Bytes;
    }
}
