using Mirror;
using Mirror.RemoteCalls;
using UnityEngine.Assertions;

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
        public uint? GetNetId(NetworkDiagnostics.MessageInfo info)
        {
            switch (info.message)
            {
                case CommandMessage msg: return msg.netId;
                case RpcMessage msg: return msg.netId;
                case SpawnMessage msg: return msg.netId;
                case ChangeOwnerMessage msg: return msg.netId;
                case ObjectDestroyMessage msg: return msg.netId;
                case ObjectHideMessage msg: return msg.netId;
                case EntityStateMessage msg: return msg.netId;
                default: return default;
            }
        }

        public NetworkIdentity GetNetworkIdentity(uint? netId)
        {
            if (!netId.HasValue)
                return null;

            if (NetworkServer.active)
            {
                NetworkServer.spawned.TryGetValue(netId.Value, out var identity);
                return identity;
            }

            if (NetworkClient.active)
            {
                NetworkClient.spawned.TryGetValue(netId.Value, out var identity);
                return identity;
            }

            return null;
        }

        public string GetRpcName(NetworkDiagnostics.MessageInfo info)
        {
            switch (info.message)
            {
                case CommandMessage msg:
                    return GetRpcName(msg.netId, msg.componentIndex, msg.functionHash);
                case RpcMessage msg:
                    return GetRpcName(msg.netId, msg.componentIndex, msg.functionHash);
                default: return string.Empty;
            }
        }

        private string GetRpcName(uint netId, int componentIndex, ushort functionIndex)
        {
            ushort hash = functionIndex;
            var remoteCallDelegate = RemoteProcedureCalls.GetDelegate(hash);
            if (remoteCallDelegate != null)
            {
                return remoteCallDelegate.Method.Name;
            }

            // todo some error maybe
            return string.Empty;
        }
    }
}
