using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    public class Names
    {
        internal const string INTERNAL_FRAME_COUNTER = "INTERNAL_FRAME_COUNTER";

        public const string PLAYER_COUNT = "Player Count";
        public const string OBJECT_COUNT = "Object Count";

        public const string SENT_COUNT = "Sent Messages";
        public const string SENT_BYTES = "Sent Bytes";
        public const string SENT_PER_SECOND = "Sent Per Second";

        public const string RECEIVED_COUNT = "Received Messages";
        public const string RECEIVED_BYTES = "Received Bytes";
        public const string RECEIVED_PER_SECOND = "Received Per Second";
    }
    internal class Counters
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Network;

        internal static readonly ProfilerCounter<int> InternalFrameCounter;

        public static readonly ProfilerCounter<int> PlayerCount;
        public static readonly ProfilerCounter<int> ObjectCount;

        public static readonly ProfilerCounter<int> SentCount;
        public static readonly ProfilerCounter<int> SentBytes;
        public static readonly ProfilerCounter<int> SentPerSecond;

        public static readonly ProfilerCounter<int> ReceiveCount;
        public static readonly ProfilerCounter<int> ReceiveBytes;
        public static readonly ProfilerCounter<int> ReceivePerSecond;

        static Counters()
        {
            ProfilerMarkerDataUnit count = ProfilerMarkerDataUnit.Count;
            ProfilerMarkerDataUnit bytes = ProfilerMarkerDataUnit.Bytes;

            InternalFrameCounter = new ProfilerCounter<int>(Category, Names.INTERNAL_FRAME_COUNTER, count);
            PlayerCount = new ProfilerCounter<int>(Category, Names.PLAYER_COUNT, count);

            SentCount = new ProfilerCounter<int>(Category, Names.SENT_COUNT, count);
            SentBytes = new ProfilerCounter<int>(Category, Names.SENT_BYTES, bytes);
            SentPerSecond = new ProfilerCounter<int>(Category, Names.SENT_PER_SECOND, bytes);

            ReceiveCount = new ProfilerCounter<int>(Category, Names.RECEIVED_COUNT, count);
            ReceiveBytes = new ProfilerCounter<int>(Category, Names.RECEIVED_BYTES, bytes);
            ReceivePerSecond = new ProfilerCounter<int>(Category, Names.RECEIVED_PER_SECOND, bytes);
        }
    }
    internal class Frame
    {
        public readonly List<NetworkDiagnostics.MessageInfo> Messages = new List<NetworkDiagnostics.MessageInfo>();
        public int Bytes;
    }
    class CountRecorder
    {
        readonly ProfilerCounter<int> profilerCount;
        readonly ProfilerCounter<int> profilerBytes;
        readonly object instance;
        internal readonly Frame[] frames;

        int count;
        int bytes;


        public CountRecorder(int bufferSize, object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes, ProfilerCounter<int> sentPerSecond)
        {
            this.instance = instance;
            this.profilerCount = profilerCount;
            this.profilerBytes = profilerBytes;
            frames = new Frame[bufferSize];
            for (int i = 0; i < frames.Length; i++)
                frames[i] = new Frame();
        }



        public void OnMessage(NetworkDiagnostics.MessageInfo obj)
        {
            // using the profiler-window branch of mirage to allow NetworkDiagnostics to say which server/client is sent the event
#if MIRAGE_DIAGNOSTIC_INSTANCE
            if (obj.instance != instance)
                return;
#endif

            // Debug.Log($"{Time.frameCount % frames.Length} {NetworkProfilerModuleViewController.CreateTextForMessageInfo(obj)}");

            count += obj.count;
            bytes += obj.bytes * obj.count;
            Frame frame = frames[Time.frameCount % frames.Length];
            frame.Messages.Add(obj);
            frame.Bytes++;
        }

        public void EndFrame()
        {
            profilerCount.Sample(count);
            profilerBytes.Sample(bytes);
            count = 0;
            bytes = 0;
            Frame frame = frames[(Time.frameCount + 1) % frames.Length];
            frame.Messages.Clear();
        }
    }
}
