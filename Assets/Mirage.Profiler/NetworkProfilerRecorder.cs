using Mirage.Logging;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditorInternal;
#endif

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

        private int _lastProcessedFrame = -1;

        private void Start()
        {
#if !UNITY_EDITOR
            Debug.LogWarning("NetworkProfilerBehaviour only works in editor");
            return;
#endif

#if UNITY_EDITOR
            _lastProcessedFrame = ProfilerDriver.lastFrameIndex;
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

            var provider = new NetworkInfoProvider(Server.World);
            _sentCounter = new CountRecorder(Server, provider, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            _receivedCounter = new CountRecorder(Server, provider, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
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

            var provider = new NetworkInfoProvider(Client.World);
            _sentCounter = new CountRecorder(Client, provider, Counters.SentCount, Counters.SentBytes, Counters.SentPerSecond);
            _receivedCounter = new CountRecorder(Client, provider, Counters.ReceiveCount, Counters.ReceiveBytes, Counters.ReceivePerSecond);
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
            if (!ProfilerDriver.enabled)
                return;

            // once a frame, ever frame, no matter what lastFrameIndex is
            SampleCounts();

            // unity sometimes skips a profiler frame, because unity
            // so we have to check if that happens and then sample the missing frame
            while (_lastProcessedFrame < ProfilerDriver.lastFrameIndex)
            {
                _lastProcessedFrame++;

                //Debug.Log($"Sample: [LateUpdate, enabled { ProfilerDriver.enabled}, first {ProfilerDriver.firstFrameIndex}, last {ProfilerDriver.lastFrameIndex}, lastProcessed {lastProcessedFrame}]");

                var lastFrame = _lastProcessedFrame;
                // not sure why frame is offset, but +2 fixes it
                SampleMessages(lastFrame + 2);
            }
        }
#endif

        /// <summary>
        /// call this every frame to sample number of players and objects
        /// </summary>
        private void SampleCounts()
        {
            if (instance == (object)Server)
            {
                Counters.PlayerCount.Sample(Server.Players.Count);
                Counters.PlayerCount.Sample(Server.NumberOfPlayers);
                Counters.ObjectCount.Sample(Server.World.SpawnedIdentities.Count);
            }
        }

        /// <summary>
        /// call this when ProfilerDriver shows it is next frame
        /// </summary>
        /// <param name="frame"></param>
        private void SampleMessages(int frame)
        {
            if (instance == null)
                return;

            _sentCounter.EndFrame(frame);
            _receivedCounter.EndFrame(frame);
            AfterSample?.Invoke(frame);
        }
    }
}
