namespace Mirage.NetworkProfiler
{
    /// <summary>
    /// Returns information about NetworkMessage
    /// </summary>
    public interface INetworkInfoProvider
    {
        uint? GetNetId(NetworkDiagnostics.MessageInfo info);
        NetworkIdentity GetNetworkIdentity(uint? netId);
        string GetRpcName(NetworkDiagnostics.MessageInfo info);
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

        public NetworkIdentity GetNetworkIdentity(uint? netId)
        {
            if (!netId.HasValue)
                return null;

            if (_world == null)
                return null;

            return _world.TryGetIdentity(netId.Value, out var identity)
                ? identity
                : null;
        }

        public string GetRpcName(NetworkDiagnostics.MessageInfo info)
        {
            switch (info.message)
            {
                case ServerRpcMessage msg:
                    return GetRpcName(msg.netId, msg.componentIndex, msg.functionIndex);
                case ServerRpcWithReplyMessage msg:
                    return GetRpcName(msg.netId, msg.componentIndex, msg.functionIndex);
                case RpcMessage msg:
                    return GetRpcName(msg.netId, msg.componentIndex, msg.functionIndex);
                default: return string.Empty;
            }
        }

        private string GetRpcName(uint netId, int componentIndex, int functionIndex)
        {
            var identity = GetNetworkIdentity(netId);
            if (identity == null)
                return string.Empty;

            var behaviour = identity.NetworkBehaviours[componentIndex];
            var rpc = behaviour.RemoteCallCollection.Get(functionIndex);
            return rpc.name;
        }
    }
}
