using Cysharp.Threading.Tasks;
using Mirage.Sockets.Udp;
using UnityEngine;

namespace Mirage.NetworkProfiler.Example
{
    public class AutoStart : MonoBehaviour
    {
        public int ClientCount = 4;
        private NetworkServer server;
        private NetworkClient[] clients;

        private void Start()
        {
            StartAsync().Forget();
        }

        private async UniTaskVoid StartAsync()
        {
            var (prefabIdentity, prefabCharacter) = CreatePlayerPrefab();
            var prefabBullet = CreateBulletPrefab();
            prefabCharacter.BulletPrefabs = new NetworkIdentity[1] { prefabBullet };

            const int PREFAB_HASH = 1;
            const int PREFAB_HASH_BULLET = 2;

            var serverGO = new GameObject("server");
            serverGO.transform.parent = transform;
            server = serverGO.AddComponent<NetworkServer>();
            var serverObjectManager = serverGO.AddComponent<ServerObjectManager>();
            server.SocketFactory = serverGO.AddComponent<UdpSocketFactory>();
            serverObjectManager.Server = server;
            server.Connected.AddListener((player) =>
            {
                var clone = Instantiate(prefabIdentity);
                clone.gameObject.SetActive(true);
                serverObjectManager.AddCharacter(player, clone, PREFAB_HASH);
                clone.name = $"Player {clone.NetId}";
                clone.transform.parent = serverGO.transform;
            });
            SetupProfiler(server);

            server.StartServer();

            await UniTask.Delay(100);

            clients = new NetworkClient[ClientCount];
            for (var i = 0; i < ClientCount; i++)
            {
                var clientGO = new GameObject($"client {i}");
                clientGO.transform.parent = transform;
                var client = clientGO.AddComponent<NetworkClient>();
                clients[i] = client;
                var clientObjectManager = clientGO.AddComponent<ClientObjectManager>();
                clientObjectManager.Client = client;
                clientObjectManager.RegisterPrefab(prefabIdentity, PREFAB_HASH);
                clientObjectManager.RegisterPrefab(prefabBullet, PREFAB_HASH_BULLET);

                client.SocketFactory = clientGO.AddComponent<UdpSocketFactory>();
                await UniTask.Delay(100);
                client.Connect();
            }
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
            var profiler = server.gameObject.AddComponent<NetworkProfilerBehaviour>();
            profiler.Server = server;
        }
    }
}
