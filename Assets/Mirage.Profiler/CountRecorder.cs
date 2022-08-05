using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
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
        private int _frameIndex = -1;


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

        public void EndFrame(int frameIndex)
        {
            CaclulatePerSecond(Time.time, _bytes);
            _profilerCount.Sample(_count);
            _profilerBytes.Sample(_bytes);
            _count = 0;
            _bytes = 0;

            _frameIndex = frameIndex;
            var frame = _frames.GetFrame(_frameIndex);
            frame.Messages.Clear();
            frame.Bytes = 0;
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
