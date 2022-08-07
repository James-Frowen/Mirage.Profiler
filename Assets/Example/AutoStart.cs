using Cysharp.Threading.Tasks;
using Mirage.Sockets.Udp;
using UnityEngine;

namespace Mirage.NetworkProfiler.Example
{
    public class AutoStart : MonoBehaviour
    {
        [System.Serializable]
        public struct RunSettings
        {
            public bool StartServer;
            public int ClientCount;
        }
        public RunSettings EditorRun;
        public RunSettings PlayerRun;

        private void Start()
        {
#if UNITY_EDITOR
            var settings = EditorRun;
#else
            var settings = PlayerRun;
#endif

            StartAsync(settings).Forget();
        }

        private async UniTaskVoid StartAsync(RunSettings runSettings)
        {
            var (prefabIdentity, prefabCharacter) = CreatePlayerPrefab();
            var prefabBullet = CreateBulletPrefab();
            prefabCharacter.BulletPrefabs = new NetworkIdentity[1] { prefabBullet };

            if (runSettings.StartServer)
                CreateServer(prefabIdentity);

            await UniTask.Delay(100);

            var clients = new NetworkClient[runSettings.ClientCount];
            for (var i = 0; i < runSettings.ClientCount; i++)
            {
                clients[i] = await CreateClient(prefabIdentity, prefabBullet, i);
            }
        }

        private void CreateServer(NetworkIdentity prefabIdentity)
        {
            var serverGO = new GameObject("server");
            serverGO.transform.parent = transform;
            var server = serverGO.AddComponent<NetworkServer>();
            server.MaxConnections = 1000;
            var serverObjectManager = serverGO.AddComponent<ServerObjectManager>();
            server.SocketFactory = serverGO.AddComponent<UdpSocketFactory>();
            serverObjectManager.Server = server;
            server.Connected.AddListener((player) =>
            {
                var clone = Instantiate(prefabIdentity);
                clone.gameObject.SetActive(true);
                serverObjectManager.AddCharacter(player, clone, prefabIdentity.name.GetStableHashCode());
                clone.name = $"Player {clone.NetId}";
                clone.transform.parent = serverGO.transform;
            });
            SetupProfiler(server);

            server.StartServer();
        }

        private async UniTask<NetworkClient> CreateClient(NetworkIdentity prefabIdentity, NetworkIdentity prefabBullet, int i)
        {
            var clientGO = new GameObject($"client {i}");
            clientGO.transform.parent = transform;
            var client = clientGO.AddComponent<NetworkClient>();
            var clientObjectManager = clientGO.AddComponent<ClientObjectManager>();
            clientObjectManager.Client = client;

            client.SocketFactory = clientGO.AddComponent<UdpSocketFactory>();
            await UniTask.Delay(100);

            client.Connected.AddListener(_ =>
            {
                clientObjectManager.RegisterPrefab(prefabIdentity, prefabIdentity.name.GetStableHashCode());
                clientObjectManager.RegisterPrefab(prefabBullet, prefabBullet.name.GetStableHashCode());
            });
            client.Disconnected.AddListener(reason =>
            {
                Debug.Log($"Disconnected[{i}] {reason}");
                // if closed locally, do nothing
                if (reason == ClientStoppedReason.LocalConnectionClosed || reason == ClientStoppedReason.ConnectingCancel)
                    return;

                // else just connect again
                // must be in delay so client can fully shutdown
                UniTask.Void(async () =>
                {
                    await UniTask.Delay(100);
                    client.Connect();
                });
            });
            client.Connect();

            return client;
        }

        private (NetworkIdentity, ExamplePlayer) CreatePlayerPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "player";
            prefab.transform.parent = transform;
            prefab.SetActive(false);
            var prefabIdentity = prefab.AddComponent<NetworkIdentity>();
            prefab.AddComponent<NetworkTransform>();
            var character = prefab.AddComponent<ExamplePlayer>();

            return (prefabIdentity, character);
        }

        private NetworkIdentity CreateBulletPrefab()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            prefab.transform.localScale = Vector3.one * .25f;
            prefab.name = "bullet";
            prefab.transform.parent = transform;
            prefab.SetActive(false);
            var prefabIdentity = prefab.AddComponent<NetworkIdentity>();
            prefab.AddComponent<NetworkTransform>();
            prefab.AddComponent<Rigidbody>();

            return prefabIdentity;
        }


        private void SetupProfiler(NetworkServer server)
        {
            var profiler = server.gameObject.AddComponent<NetworkProfilerRecorder>();
            profiler.Server = server;
        }
    }
}
