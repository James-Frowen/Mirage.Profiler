using UnityEngine;

namespace Mirage.NetworkProfiler.Example
{
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(NetworkTransform))]
    public class ExamplePlayer : NetworkBehaviour
    {
        [SerializeField] float _moveSpeed = 10;
        [SerializeField] float _rotateSpeed = 10;

        Vector2Int _clientInputs;
        Vector2Int _serverInputs;
        int _serverCounter;

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

                float xRand = Random.value;
                float yRand = Random.value;

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

            SendInuts(_clientInputs);
        }

        //[ServerRpc]
        private void SendInuts(Vector2Int clientInputs)
        {
            _serverInputs = clientInputs;
        }

        //[ClientRpc]
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
