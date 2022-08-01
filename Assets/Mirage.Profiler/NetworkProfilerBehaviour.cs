using Mirage.Logging;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(int.MaxValue)] // last
    public class NetworkProfilerBehaviour : MonoBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkProfilerBehaviour>();

        // singleton because unity only has 1 profiler
        public static NetworkProfilerBehaviour Instance { get; private set; }

        public NetworkServer Server;
        public NetworkServer Client;

        /// <summary>
        /// instance being used for profiler
        /// </summary>
        private static object instance;

        internal static CountRecorder _sentCounter;
        internal static CountRecorder _receivedCounter;
        private const int FRAME_COUNT = 300; // todo find a way to get real frame count

        private void Start()
        {
            Debug.Assert(Instance == null);
            Instance = this;
            DontDestroyOnLoad(this);

            if (Server != null)
            {
                Server.Started.AddListener(ServerStarted);
                Server.Stopped.AddListener(ServerStopped);
            }

            if (Client != null)
            {
                Client.Started.AddListener(ClientStarted);
                Client.Disconnected.AddListener(ClientStopped);
            }
        }

        private void OnDestroy()
        {
            if (_receivedCounter != null)
                NetworkDiagnostics.InMessageEvent -= _receivedCounter.OnMessage;
            if (_sentCounter != null)
                NetworkDiagnostics.OutMessageEvent -= _sentCounter.OnMessage;

            Debug.Assert(Instance == this);
            Instance = null;
        }

        private void ServerStarted()
        {
            if (instance != null)
            {
                logger.LogWarning($"Already started profiler for different Instance:{instance}");
                return;
            }
            instance = Server;

            _sentCounter = new CountRecorder(FRAME_COUNT, Server, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            _receivedCounter = new CountRecorder(FRAME_COUNT, Server, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
            NetworkDiagnostics.InMessageEvent += _receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += _sentCounter.OnMessage;
        }

        private void ClientStarted()
        {
            if (instance != null)
            {
                logger.LogWarning($"Already started profiler for different Instance:{instance}");
                return;
            }
            instance = Client;

            _sentCounter = new CountRecorder(FRAME_COUNT, Client, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            _receivedCounter = new CountRecorder(FRAME_COUNT, Client, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
            NetworkDiagnostics.InMessageEvent += _receivedCounter.OnMessage;
            NetworkDiagnostics.OutMessageEvent += _sentCounter.OnMessage;
        }

        private void ServerStopped()
        {
            if (instance == (object)Server)
                instance = null;
        }

        private void ClientStopped(INetworkPlayer arg0)
        {
            if (instance == (object)Client)
                instance = null;

            _receivedCounter = null;
            _sentCounter = null;
        }

        private void LateUpdate()
        {
            if (instance == null)
                return;

            if (instance == (object)Server)
            {
                Counters.PlayerCount.Sample(Server.Players.Count);
                Counters.PlayerCount.Sample(Server.NumberOfPlayers);
                Counters.ObjectCount.Sample(Server.World.SpawnedIdentities.Count);
            }

            _sentCounter.EndFrame();
            _receivedCounter.EndFrame();
            Counters._internalFrameCounter.Sample(Time.frameCount % FRAME_COUNT);
        }
    }
}
