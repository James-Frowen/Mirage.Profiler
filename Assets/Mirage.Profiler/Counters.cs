using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    public class Names
    {
        internal const string INTERNAL_FRAME_COUNTER = "INTERNAL_FRAME_COUNTER";

        public const string PLAYER_COUNT = "Player Count";
        public const string PLAYER_COUNT_TOOLTIP = "Number of players connected to the server";
        public const string CHARACTER_COUNT = "Character Count";
        public const string CHARACTER_COUNT_TOOLTIP = "Number of players with spawned GameObjects";

        public const string OBJECT_COUNT = "Object Count";
        public const string OBJECT_COUNT_TOOLTIP = "Number of NetworkIdentities spawned on the server";

        public const string SENT_COUNT = "Sent Messages";
        public const string SENT_BYTES = "Sent Bytes";
        public const string SENT_PER_SECOND = "Sent Per Second";

        public const string RECEIVED_COUNT = "Received Messages";
        public const string RECEIVED_BYTES = "Received Bytes";
        public const string RECEIVED_PER_SECOND = "Received Per Second";

        public const string PER_SECOND_TOOLTIP = "Sum of Bytes over the previous second";
    }

    internal class Counters
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Network;
        private const ProfilerMarkerDataUnit COUNT = ProfilerMarkerDataUnit.Count;
        private const ProfilerMarkerDataUnit BYTES = ProfilerMarkerDataUnit.Bytes;

        internal static readonly ProfilerCounter<int> _internalFrameCounter = new ProfilerCounter<int>(Category, Names.INTERNAL_FRAME_COUNTER, COUNT);

        public static readonly ProfilerCounter<int> PlayerCount = new ProfilerCounter<int>(Category, Names.PLAYER_COUNT, COUNT);
        public static readonly ProfilerCounter<int> CharCount = new ProfilerCounter<int>(Category, Names.CHARACTER_COUNT, COUNT);
        public static readonly ProfilerCounter<int> ObjectCount = new ProfilerCounter<int>(Category, Names.OBJECT_COUNT, COUNT);

        public static readonly ProfilerCounter<int> SentCount = new ProfilerCounter<int>(Category, Names.SENT_COUNT, COUNT);
        public static readonly ProfilerCounter<int> SentBytes = new ProfilerCounter<int>(Category, Names.SENT_BYTES, BYTES);
        public static readonly ProfilerCounter<int> SentPerSecond = new ProfilerCounter<int>(Category, Names.SENT_PER_SECOND, BYTES);

        public static readonly ProfilerCounter<int> ReceiveCount = new ProfilerCounter<int>(Category, Names.RECEIVED_COUNT, COUNT);
        public static readonly ProfilerCounter<int> ReceiveBytes = new ProfilerCounter<int>(Category, Names.RECEIVED_BYTES, BYTES);
        public static readonly ProfilerCounter<int> ReceivePerSecond = new ProfilerCounter<int>(Category, Names.RECEIVED_PER_SECOND, BYTES);
    }

    [System.Serializable]
    internal class Frame
    {
        public List<MessageInfo> Messages = new List<MessageInfo>();
        public int Bytes;
    }

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

    public static class MessageHelper
    {
        public static uint? GetNetId(this NetworkDiagnostics.MessageInfo info)
        {
            switch (info.message)
            {
                case ServerRpcMessage msg: return msg.netId;
                case ServerRpcWithReplyMessage msg: return msg.netId;
                case RpcMessage msg: return msg.netId;
                case SpawnMessage msg: return msg.netId;
                case RemoveAuthorityMessage msg: return msg.netId;
                case ObjectDestroyMessage msg: return msg.netId;
                case ObjectHideMessage msg: return msg.netId;
                case UpdateVarsMessage msg: return msg.netId;
                default: return default;
            }
        }
    }

    internal class CountRecorder
    {
        private readonly ProfilerCounter<int> _profilerCount;
        private readonly ProfilerCounter<int> _profilerBytes;
        private readonly ProfilerCounter<int> _profilerPerSecond;
        private readonly object _instance;
        internal readonly Frame[] _frames;
        private int _count;
        private int _bytes;
        private int _perSecond;
        private readonly Queue<(float time, int bytes)> _perSecondQueue = new Queue<(float time, int bytes)>();


        public CountRecorder(int bufferSize, object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes, ProfilerCounter<int> profilerPerSecond)
        {
            _instance = instance;
            _profilerCount = profilerCount;
            _profilerBytes = profilerBytes;
            _profilerPerSecond = profilerPerSecond;
            _frames = new Frame[bufferSize];
            for (var i = 0; i < _frames.Length; i++)
                _frames[i] = new Frame();
        }



        public void OnMessage(NetworkDiagnostics.MessageInfo obj)
        {
            // using the profiler-window branch of mirage to allow NetworkDiagnostics to say which server/client is sent the event
#if MIRAGE_DIAGNOSTIC_INSTANCE
            if (obj.instance != _instance)
                return;
#endif

            // Debug.Log($"{Time.frameCount % frames.Length} {NetworkProfilerModuleViewController.CreateTextForMessageInfo(obj)}");

            _count += obj.count;
            _bytes += obj.bytes * obj.count;
            var frame = _frames[Time.frameCount % _frames.Length];
            frame.Messages.Add(new MessageInfo(obj, frame.Messages.Count));
            frame.Bytes++;
        }

        public void EndFrame()
        {
            CaclulatePerSecond(Time.time, _bytes);
            _profilerCount.Sample(_count);
            _profilerBytes.Sample(_bytes);
            _count = 0;
            _bytes = 0;
            var frame = _frames[(Time.frameCount + 1) % _frames.Length];
            frame.Messages.Clear();

        }

        private void CaclulatePerSecond(float now, int bytes)
        {
            // add new values to sum
            _perSecond += bytes;
            _perSecondQueue.Enqueue((now, bytes));

            // remove old bytes from sum
            var removeTime = now - 1;
            while (_perSecondQueue.Peek().time < removeTime)
            {
                var removed = _perSecondQueue.Dequeue();
                _perSecond -= removed.bytes;
            }

            // record sample after adding/removing value
            _profilerPerSecond.Sample(_perSecond);
        }
    }
}
