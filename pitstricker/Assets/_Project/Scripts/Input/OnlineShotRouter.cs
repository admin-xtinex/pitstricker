using UnityEngine;
using Unity.Netcode;
using PitStriker.Networking;
using PitStriker.Networking.Client;
using PitStriker.Physics;
using PitStriker.Gameplay;

namespace PitStriker.Input
{
    public static class OnlineShotRouter
    {
        public static bool IsCloudMatchLive()
        {
            var cloud = CloudMatchManager.Instance;
            var client = CloudNetworkClient.Instance;
            return cloud != null && cloud.IsOnlineMatchActive && client != null && client.IsConnected;
        }

        public static bool IsRelayMatchLive()
        {
            if (IsCloudMatchLive()) return false;
            return NetworkSessionManager.Instance != null &&
                   NetworkSessionManager.Instance.ActiveNetworkMode != NetworkSessionManager.NetworkMode.None &&
                   NetworkSessionManager.Instance.IsConnected;
        }

        public static bool TryDispatch(Vector3 direction, float force, MarbleController marble)
        {
            if (IsCloudMatchLive() && CloudMatchManager.Instance != null)
            {
                CloudMatchManager.Instance.SubmitLocalShot(direction, force);
                return true;
            }

            if (IsRelayMatchLive() && NetworkMatchState.Instance != null)
            {
                if (marble != null) marble.Halt();
                NetworkMatchState.Instance.SubmitLocalShot(direction, force);
                if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsServer && marble != null)
                    marble.ApplyImpulse(direction, force);
                return true;
            }

            return false;
        }
    }
}
