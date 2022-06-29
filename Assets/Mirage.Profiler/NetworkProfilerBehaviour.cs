using Mirror;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(10000)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        internal static CountRecorder _sentCounter;
        internal static CountRecorder _receivedCounter;

        private const int FRAME_COUNT = 300; // todo find a way to get real frame count

        private void Start()
        {
            _sentCounter = new CountRecorder(FRAME_COUNT, null, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            _receivedCounter = new CountRecorder(FRAME_COUNT, null, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);

            NetworkDiagnostics.InMessageEvent += _receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += _sentCounter.OnMessage;
        }

        private void OnDestroy()
        {
            if (_receivedCounter != null)
                NetworkDiagnostics.InMessageEvent -= _receivedCounter.OnMessage;
            if (_sentCounter != null)
                NetworkDiagnostics.OutMessageEvent -= _sentCounter.OnMessage;
        }

        private void LateUpdate()
        {
            if (NetworkServer.active)
            {
                Counters.PlayerCount.Sample(NetworkServer.connections.Count);
                Counters.PlayerCount.Sample(NetworkManager.singleton.numPlayers);
                Counters.ObjectCount.Sample(NetworkServer.spawned.Count);
            }

            if (NetworkServer.active || NetworkClient.active)
            {
                _sentCounter.EndFrame();
                _receivedCounter.EndFrame();
                Counters._internalFrameCounter.Sample(Time.frameCount % FRAME_COUNT);
            }
        }
    }
}
