using Mirage.Logging;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(10000)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        static readonly ILogger logger = LogFactory.GetLogger<NetworkProfilerBehaviour>();

        public NetworkServer Server;
        public NetworkServer Client;

        /// <summary>
        /// instance being used for profiler
        /// </summary>
        static object _instance;

        internal static CountRecorder sentCounter;
        internal static CountRecorder receivedCounter;

        const int frameCount = 300; // todo find a way to get real frame count

        private void Awake()
        {
            Server.Started.AddListener(ServerStarted);
            Server.Stopped.AddListener(ServerStopped);
            Client.Started.AddListener(ClientStarted);
            Client.Disconnected.AddListener(ClientStopped);

            NetworkDiagnostics.InMessageEvent += receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += sentCounter.OnMessage;
        }

        private void ServerStarted()
        {
            if (_instance != null)
            {
                logger.LogWarning($"Already started profiler for different Instance:{_instance}");
                return;
            }
            _instance = Server;

            sentCounter = new CountRecorder(frameCount, Server, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            receivedCounter = new CountRecorder(frameCount, Server, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
        }

        private void ClientStarted()
        {
            if (_instance != null)
            {
                logger.LogWarning($"Already started profiler for different Instance:{_instance}");
                return;
            }
            _instance = Client;

            sentCounter = new CountRecorder(frameCount, Client, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            receivedCounter = new CountRecorder(frameCount, Client, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
        }

        private void ServerStopped()
        {
            if (_instance == (object)Server)
                _instance = null;
        }

        private void ClientStopped(INetworkPlayer arg0)
        {
            if (_instance == (object)Client)
                _instance = null;

            receivedCounter = null;
            sentCounter = null;
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

            if (_instance == (object)Server)
            {
                Counters.PlayerCount.Sample(Server.Players.Count);
                Counters.ObjectCount.Sample(Server.World.SpawnedIdentities.Count);
            }

            sentCounter.EndFrame();
            receivedCounter.EndFrame();
            Counters.InternalFrameCounter.Sample(Time.frameCount % frameCount);
        }
    }
}
