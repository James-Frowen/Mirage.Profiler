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
        const ProfilerMarkerDataUnit COUNT = ProfilerMarkerDataUnit.Count;
        const ProfilerMarkerDataUnit BYTES = ProfilerMarkerDataUnit.Bytes;

        internal static readonly ProfilerCounter<int> InternalFrameCounter = new ProfilerCounter<int>(Category, Names.INTERNAL_FRAME_COUNTER, COUNT);

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
    internal class Frame
    {
        public readonly List<NetworkDiagnostics.MessageInfo> Messages = new List<NetworkDiagnostics.MessageInfo>();
        public int Bytes;
    }
    class CountRecorder
    {
        readonly ProfilerCounter<int> profilerCount;
        readonly ProfilerCounter<int> profilerBytes;
        readonly ProfilerCounter<int> profilerPerSecond;

        readonly object instance;
        internal readonly Frame[] frames;

        int count;
        int bytes;
        int perSecond;

        readonly Queue<(float time, int bytes)> PerSecondQueue = new Queue<(float time, int bytes)>();


        public CountRecorder(int bufferSize, object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes, ProfilerCounter<int> profilerPerSecond)
        {
            this.instance = instance;
            this.profilerCount = profilerCount;
            this.profilerBytes = profilerBytes;
            this.profilerPerSecond = profilerPerSecond;
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
            CaclulatePerSecond(Time.time, bytes);
            profilerCount.Sample(count);
            profilerBytes.Sample(bytes);
            count = 0;
            bytes = 0;
            Frame frame = frames[(Time.frameCount + 1) % frames.Length];
            frame.Messages.Clear();

        }

        private void CaclulatePerSecond(float now, int bytes)
        {
            // add new values to sum
            perSecond += bytes;
            PerSecondQueue.Enqueue((now, bytes));

            // remove old bytes from sum
            float removeTime = now - 1;
            while (PerSecondQueue.Peek().time < removeTime)
            {
                (float time, int bytes) removed = PerSecondQueue.Dequeue();
                perSecond -= removed.bytes;
            }

            // record sample after adding/removing value
            profilerPerSecond.Sample(count);
        }
    }
}
