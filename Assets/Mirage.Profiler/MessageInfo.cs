using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [System.Serializable]
    public class MessageInfo
    {
        /// <summary>
        /// Order message was sent/received in frame
        /// </summary>
        [SerializeField] private int _order;
        [SerializeField] private int _bytes;
        [SerializeField] private int _count;
        [SerializeField] private string _messageName;
        // unity can't serialize nullable so store as 2 fields
        [SerializeField] private bool _hasNetId;
        [SerializeField] private uint _netId;

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
