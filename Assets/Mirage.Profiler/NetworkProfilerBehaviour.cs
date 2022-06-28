using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(10000)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        public NetworkServer Server;

        internal static CountRecorder sentCounter;
        internal static CountRecorder receivedCounter;

        
        const int frameCount = 300; // todo find a way to get real frame count
        private void Start()
        {

            sentCounter = new CountRecorder(frameCount, Server, Counters.SentMessagesCount, Counters.SentMessagesBytes);
            receivedCounter = new CountRecorder(frameCount, Server, Counters.ReceiveMessagesCount, Counters.ReceiveMessagesBytes);

            NetworkDiagnostics.InMessageEvent += receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += sentCounter.OnMessage;
        }
        private void OnDestroy()
        {
            if (receivedCounter != null)
                NetworkDiagnostics.InMessageEvent -= receivedCounter.OnMessage;
            if (sentCounter != null)
                NetworkDiagnostics.OutMessageEvent -= sentCounter.OnMessage;
        }

        private void LateUpdate()
        {
            if (Server == null || !Server.Active)
                return;

            Counters.PlayerCount.Sample(Server.Players.Count);
            sentCounter.EndFrame();
            receivedCounter.EndFrame();
            Counters.InternalFrameCounter.Sample(Time.frameCount % frameCount);
        }
    }
}
