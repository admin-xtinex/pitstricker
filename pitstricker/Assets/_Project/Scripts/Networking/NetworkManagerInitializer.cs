using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 1: Auto-initializes and configures NetworkManager and NetworkSessionManager
    /// at application launch, ensuring online capabilities are available without requiring
    /// manual GameObject placement in every scene.
    /// </summary>
    public static class NetworkManagerInitializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeNetworking()
        {
            if (NetworkManager.Singleton != null) return;

            GameObject networkObj = new GameObject("NetworkManager_Persistent");
            Object.DontDestroyOnLoad(networkObj);

            NetworkManager nm = networkObj.AddComponent<NetworkManager>();
            nm.SetSingleton();
            UnityTransport transport = networkObj.AddComponent<UnityTransport>();

            nm.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                ProtocolVersion = 1,
                ConnectionApproval = false,
                EnableSceneManagement = true
            };

            GameObject matchPrefab = Resources.Load<GameObject>("NetworkMatchState");
            if (matchPrefab != null)
            {
                nm.NetworkConfig.Prefabs.Add(new NetworkPrefab { Prefab = matchPrefab });
            }

            networkObj.AddComponent<NetworkSessionManager>();
            networkObj.AddComponent<DisconnectGracePeriodManager>();
            networkObj.AddComponent<QuickMatchManager>();

            Debug.Log("<color=#00FFAA><b>[NETWORKING INITIALIZER]</b> Initialized persistent NetworkManager, NetworkSessionManager, DisconnectGracePeriodManager &amp; QuickMatchManager.</color>");
        }
    }
}
