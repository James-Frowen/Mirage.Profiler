namespace Mirage.NetworkProfiler
{
    [System.Serializable]
    internal class MessageInfo
    {
        /// <summary>
        /// Order message was sent/received in frame
        /// </summary>
        private int _order;
        private int _bytes;
        private int _count;
        private string _messageName;
        // unity can't serialize nullable so store as 2 fields
        private bool _hasNetId;
        private uint _netId;

        public int Order => _order;
        public string Name => _messageName;
        public int Bytes => _bytes;
        public int Count => _count;
        public int TotalBytes => Bytes * Count;
        public uint? NetId => _hasNetId ? _netId : default;

        public MessageInfo(NetworkDiagnostics.MessageInfo msg, int order)
        {
            _order = order;
            _bytes = msg.bytes;
            _count = msg.count;
            _messageName = msg.message.GetType().FullName;
            var id = msg.GetNetId();
            _hasNetId = id.HasValue;
            _netId = id.GetValueOrDefault();
        }
    }
}
