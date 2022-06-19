using Cysharp.Threading.Tasks;
using Mirage.Profiler.Example;
using Mirage.Sockets.Udp;
using UnityEngine;

namespace Mirage.Profiler
{
    public class AutoStart : MonoBehaviour
    {
        public int ClientCount = 4;
        private NetworkServer server;
        private NetworkClient[] clients;


        void Start()
        {
            StartAsync().Forget();
        }


        async UniTaskVoid StartAsync()
        {
            var prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prefab.name = "player";
            prefab.transform.parent = transform;
            prefab.SetActive(false);
            NetworkIdentity prefabIdentity = prefab.AddComponent<NetworkIdentity>();
            prefab.AddComponent<NetworkTransform>();
            prefab.AddComponent<ProfilerPlayer>();
            const int PREFAB_HASH = 1;

            var serverGO = new GameObject("server");
            serverGO.transform.parent = transform;
            server = serverGO.AddComponent<NetworkServer>();
            ServerObjectManager serverObjectManager = serverGO.AddComponent<ServerObjectManager>();
            server.SocketFactory = serverGO.AddComponent<UdpSocketFactory>();
            serverObjectManager.Server = server;
            server.Connected.AddListener((player) =>
            {
                NetworkIdentity clone = Instantiate(prefabIdentity);
                clone.gameObject.SetActive(true);
                serverObjectManager.AddCharacter(player, clone, PREFAB_HASH);
                clone.name = $"Player {clone.NetId}";
                clone.transform.parent = serverGO.transform;
            });
            SetupProfiler(server);

            server.StartServer();

            await UniTask.Delay(100);

            clients = new NetworkClient[ClientCount];
            for (int i = 0; i < ClientCount; i++)
            {
                var clientGO = new GameObject($"client {i}");
                clientGO.transform.parent = transform;
                NetworkClient client = clientGO.AddComponent<NetworkClient>();
                clients[i] = client;
                ClientObjectManager clientObjectManager = clientGO.AddComponent<ClientObjectManager>();
                clientObjectManager.Client = client;
                clientObjectManager.RegisterPrefab(prefabIdentity, PREFAB_HASH);

                client.SocketFactory = clientGO.AddComponent<UdpSocketFactory>();
                await UniTask.Delay(100);
                client.Connect();
            }
        }

        private void SetupProfiler(NetworkServer server)
        {
            //
        }
    }
}
