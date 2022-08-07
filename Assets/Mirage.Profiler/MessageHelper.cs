using UnityEngine;

namespace Mirage.NetworkProfiler
{
    public static class MessageHelper
    {
        public static uint? GetNetId(NetworkDiagnostics.MessageInfo info)
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

        public static GameObject GetGameObject(uint? netId)
        {
            if (!netId.HasValue)
                return null;

            var world = GetNetworkWorld();
            if (world == null)
                return null;

            return world.TryGetIdentity(netId.Value, out var identity)
                ? identity.gameObject
                : null;
        }

        private static NetworkWorld GetNetworkWorld()
        {
            // first try server, then if not active, then client
            var server = GameObject.FindObjectOfType<NetworkServer>();
            if (server != null && server.Active)
            {
                return server.World;
            }

            var client = GameObject.FindObjectOfType<NetworkClient>();
            if (client != null && client.Active)
            {
                return client.World;
            }

            return null;
        }
    }
}
