using Mirage.Logging;
using UnityEngine;

namespace Mirage.NetworkProfiler
{
    [DefaultExecutionOrder(int.MaxValue)] // last
    public class NetworkProfilerRecorder : MonoBehaviour
    {
        private static readonly ILogger logger = LogFactory.GetLogger<NetworkProfilerRecorder>();

        // singleton because unity only has 1 profiler
        public static NetworkProfilerRecorder Instance { get; private set; }

        public NetworkServer Server;
        public NetworkServer Client;

        /// <summary>
        /// instance being used for profiler
        /// </summary>
        private static object instance;

        internal static CountRecorder _sentCounter;
        internal static CountRecorder _receivedCounter;
        internal const int FRAME_COUNT = 300; // todo find a way to get real frame count

        public delegate void FrameUpdate(int tick);
        public static event FrameUpdate AfterSample;

        private void Start()
        {
#if !UNITY_EDITOR
            Debug.LogWarning("NetworkProfilerBehaviour only works in editor");
            return;
#endif

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


#if UNITY_EDITOR
        private void LateUpdate()
        {
            Debug.Log($"Sample: [LateUpdate, first {UnityEditorInternal.ProfilerDriver.firstFrameIndex}, last {UnityEditorInternal.ProfilerDriver.lastFrameIndex}]");
            Sample(UnityEditorInternal.ProfilerDriver.lastFrameIndex);
        }
#endif
        private void Sample(int frame)
        {

            if (instance == null)
                return;

            if (instance == (object)Server)
            {
                Counters.PlayerCount.Sample(Server.Players.Count);
                Counters.PlayerCount.Sample(Server.NumberOfPlayers);
                Counters.ObjectCount.Sample(Server.World.SpawnedIdentities.Count);
            }

            _sentCounter.EndFrame(frame);
            _receivedCounter.EndFrame(frame);
            AfterSample?.Invoke(frame);
        }
    }
}
