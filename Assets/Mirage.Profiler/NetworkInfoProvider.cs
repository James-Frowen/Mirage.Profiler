using UnityEngine;

namespace Mirage.NetworkProfiler
{
    /// <summary>
    /// Returns information about NetworkMessage
    /// </summary>
    public interface INetworkInfoProvider
    {
        GameObject GetGameObject(uint? netId);
        uint? GetNetId(NetworkDiagnostics.MessageInfo info);
    }

    public class NetworkInfoProvider : INetworkInfoProvider
    {
        private readonly NetworkWorld _world;

        public NetworkInfoProvider(NetworkWorld world)
        {
            _world = world;
        }

        public uint? GetNetId(NetworkDiagnostics.MessageInfo info)
        {
            switch (info.message)
            {
                case ServerRpcMessage msg: return msg.netId;
                case ServerRpcWithReplyMessage msg: return msg.netId;
                case RpcMessage msg: return msg.netId;
                case SpawnMessage msg: return msg.netId;
                case RemoveAuthorityMessage msg: return msg.netId;
                case ObjectDestroyMessage msg: return msg.netId;
                case ObjectHideMessage msg: return msg.netId;
                case UpdateVarsMessage msg: return msg.netId;
                default: return default;
            }
        }

        public GameObject GetGameObject(uint? netId)
        {
            if (!netId.HasValue)
                return null;

            if (_world == null)
                return null;

            return _world.TryGetIdentity(netId.Value, out var identity)
                ? identity.gameObject
                : null;
        }
    }
}
