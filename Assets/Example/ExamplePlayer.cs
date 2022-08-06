using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Mirage.NetworkProfiler.Example
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ExamplePlayer : NetworkBehaviour
    {
        [SerializeField] private float _moveSpeed = 10;
        [SerializeField] private float _rotateSpeed = 10;

        [SerializeField] private float _bulletImpulse = 10;
        public NetworkIdentity[] BulletPrefabs;

        private Vector2Int _clientInputs;
        private Vector2Int _serverInputs;
        private int _serverCounter;

        private void Awake()
        {
            Identity.OnStartClient.AddListener(OnStartClient);
        }

        private void OnStartClient()
        {
            name = $"Player {NetId}";
            transform.parent = (Client as MonoBehaviour)?.transform;
        }

        private void Update()
        {
            if (IsServer)
                ServerUpdate();
            if (IsClient && HasAuthority)
                ClientUpdate();
        }

        private void ClientUpdate()
        {
            // 95% chance to stay the same
            if (Random.value > 0.95f)
            {
                _clientInputs = default;

                var xRand = Random.value;
                var yRand = Random.value;

                // turn left bias (keeps objects near origin)
                if (xRand < 0.6f)
                    _clientInputs.x = -1;
                else if (xRand < 0.7f)
                    _clientInputs.x = 0;
                else
                    _clientInputs.x = 1;

                if (yRand < 0.2f)
                    _clientInputs.y = -1;
                else if (yRand < 0.3f)
                    _clientInputs.y = 0;
                else
                    _clientInputs.y = 1;
            }

            RpcSendInuts(_clientInputs);

            if (Input.GetKey(KeyCode.Space))
            {
                RpcShoot(0, transform.position, transform.rotation, 10, Random.value);
            }
        }

        [ServerRpc]
        public void RpcShoot(int prefabIndex, Vector3 position, Quaternion rotation, float impluse, float lifeTime)
        {
            var prefab = BulletPrefabs[prefabIndex];
            var clone = Instantiate(prefab, position, rotation);
            if (clone.TryGetComponent(out Rigidbody rb))
            {
                rb.AddForce(transform.forward * impluse, ForceMode.Impulse);
            }
            clone.gameObject.SetActive(true);
            ServerObjectManager.Spawn(clone, prefab.name.GetStableHashCode());

            Despawn(clone, lifeTime).Forget();
        }

        private async UniTaskVoid Despawn(NetworkIdentity clone, float time)
        {
            await UniTask.Delay((int)(time * 1000));
            ServerObjectManager.Destroy(clone, true);
        }


        [ServerRpc]
        private void RpcSendInuts(Vector2Int clientInputs)
        {
            _serverInputs = clientInputs;
        }

        [ClientRpc]
        private void RpcCounter(int counter)
        {
            // empty
        }

        private void ServerUpdate()
        {
            RpcCounter(_serverCounter++);

            transform.Translate((_serverInputs.y * _moveSpeed * Time.deltaTime) * transform.forward);
            transform.Rotate((_serverInputs.x * _rotateSpeed * Time.deltaTime) * Vector3.up);
        }
    }
}
