using System.Collections.Generic;
using Unity.Profiling;
using Unity.Profiling.Editor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mirage.NetworkProfiler
{

    public class Names
    {
        internal const string INTERNAL_FRAME_COUNTER = "INTERNAL_FRAME_COUNTER";

        public const string PLAYER_COUNT = "Player Count";
        public const string MESSAGES_SENT_COUNT = "Sent Messages";
        public const string MESSAGES_SENT_BYTES = "Sent Bytes";
        public const string MESSAGES_RECEIVED_COUNT = "Received Messages";
        public const string MESSAGES_RECEIVED_BYTES = "Received Bytes";
    }
    internal class Counters
    {
        public static readonly ProfilerCategory Category = ProfilerCategory.Network;

        internal static readonly ProfilerCounter<int> InternalFrameCounter;
        public static readonly ProfilerCounter<int> PlayerCount;
        public static readonly ProfilerCounter<int> SentMessagesCount;
        public static readonly ProfilerCounter<int> SentMessagesBytes;
        public static readonly ProfilerCounter<int> ReceiveMessagesCount;
        public static readonly ProfilerCounter<int> ReceiveMessagesBytes;

        static Counters()
        {
            ProfilerMarkerDataUnit count = ProfilerMarkerDataUnit.Count;
            ProfilerMarkerDataUnit bytes = ProfilerMarkerDataUnit.Bytes;

            InternalFrameCounter = new ProfilerCounter<int>(Category, Names.INTERNAL_FRAME_COUNTER, count);
            PlayerCount = new ProfilerCounter<int>(Category, Names.PLAYER_COUNT, count);
            SentMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_COUNT, count);
            SentMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_SENT_BYTES, bytes);
            ReceiveMessagesCount = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_COUNT, count);
            ReceiveMessagesBytes = new ProfilerCounter<int>(Category, Names.MESSAGES_RECEIVED_BYTES, bytes);
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


        public CountRecorder(int bufferSize, object instance, ProfilerCounter<int> profilerCount, ProfilerCounter<int> profilerBytes)
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