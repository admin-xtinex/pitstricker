using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 1 Session Orchestrator:
    /// Coordinates online match sessions using Unity Relay and Netcode for GameObjects.
    /// Supports:
    /// - 2-Player Private Matches via 6-character Join Codes.
    /// - Local LAN / Loopback testing without internet.
    /// - Automatic reconnection grace periods and clean session teardown.
    /// </summary>
    public class NetworkSessionManager : MonoBehaviour
    {
        public static NetworkSessionManager Instance { get; private set; }

        public enum SessionState
        {
            Offline,
            Authenticating,
            Ready,
            CreatingHost,
            InLobbyWaitingForOpponent,
            Joining,
            ConnectedInMatch,
            Reconnecting,
            Disconnecting,
            Error
        }

        public enum NetworkMode
        {
            None,
            RelayOnline,
            DirectLocal
        }

        [Header("Session State")]
        [SerializeField] private SessionState _currentState = SessionState.Offline;
        public SessionState CurrentState => _currentState;

        public NetworkMode ActiveNetworkMode { get; private set; } = NetworkMode.None;
        public string ActiveJoinCode { get; private set; } = string.Empty;
        public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        public bool IsClient => NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;
        public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        public int ConnectedPlayerCount => NetworkManager.Singleton != null ? NetworkManager.Singleton.ConnectedClients.Count : 0;

        // Events
        public event Action<SessionState> OnStateChanged;
        public event Action<string> OnJoinCodeReady;
        public event Action<ulong> OnPlayerJoined;
        public event Action<ulong> OnPlayerLeft;
        public event Action<string> OnSessionError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            RegisterNetworkCallbacks();
        }

        private void OnDestroy()
        {
            UnregisterNetworkCallbacks();
            if (Instance == this) Instance = null;
        }

        private void RegisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnected;
                NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            }
        }

        private void UnregisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnected;
                NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            }
        }

        /// <summary>
        /// Creates a 2-player private online room using Unity Relay.
        /// Generates a join code to share with the opponent.
        /// </summary>
        public async Task<string> CreateRelayHostSessionAsync()
        {
            SetState(SessionState.Authenticating);

            bool authed = await NetworkBootstrap.InitializeAndSignInAsync();
            if (!authed)
            {
                SetState(SessionState.Error);
                OnSessionError?.Invoke("Authentication failed. Please check your internet connection.");
                return null;
            }

            SetState(SessionState.CreatingHost);
            EnsureNetworkManagerReady();

            try
            {
                Debug.Log("<color=#00FFAA><b>[RELAY]</b> Requesting Relay allocation for 2 players (1 host + 1 client)...</color>");
                // maxConnections = 1 (1 additional remote client connects to this host)
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                ActiveJoinCode = joinCode;
                ActiveNetworkMode = NetworkMode.RelayOnline;

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetRelayServerData(
                        allocation.RelayServer.IpV4,
                        (ushort)allocation.RelayServer.Port,
                        allocation.AllocationIdBytes,
                        allocation.Key,
                        allocation.ConnectionData,
                        allocation.ConnectionData, // In Unity Relay, HostConnectionData is identical to ConnectionData on the host
                        true // Secure DTLS
                    );
                }

                bool started = NetworkManager.Singleton.StartHost();
                if (started)
                {
                    Debug.Log($"<color=#00FF88><b>[RELAY HOST]</b> Host started! JOIN CODE: {joinCode}</color>");
                    SetState(SessionState.InLobbyWaitingForOpponent);
                    OnJoinCodeReady?.Invoke(joinCode);
                    return joinCode;
                }
                else
                {
                    throw new Exception("NetworkManager failed to start host.");
                }
            }
            catch (Exception ex)
            {
                string error = $"Failed to create Relay host: {ex.Message}";
                Debug.LogError($"[RELAY HOST ERROR] {error}");
                SetState(SessionState.Error);
                OnSessionError?.Invoke(error);
                return null;
            }
        }

        /// <summary>
        /// Joins an existing private online room using a 6-character Join Code.
        /// </summary>
        public async Task<bool> JoinRelaySessionAsync(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                OnSessionError?.Invoke("Invalid join code.");
                return false;
            }

            string cleanCode = joinCode.Trim().ToUpperInvariant();

            SetState(SessionState.Authenticating);

            bool authed = await NetworkBootstrap.InitializeAndSignInAsync();
            if (!authed)
            {
                SetState(SessionState.Error);
                OnSessionError?.Invoke("Authentication failed. Please check your internet connection.");
                return false;
            }

            SetState(SessionState.Joining);
            EnsureNetworkManagerReady();

            try
            {
                Debug.Log($"<color=#00FFAA><b>[RELAY]</b> Joining Relay allocation with code: {cleanCode}...</color>");
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(cleanCode);

                ActiveJoinCode = cleanCode;
                ActiveNetworkMode = NetworkMode.RelayOnline;

                UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport != null)
                {
                    transport.SetRelayServerData(
                        joinAllocation.RelayServer.IpV4,
                        (ushort)joinAllocation.RelayServer.Port,
                        joinAllocation.AllocationIdBytes,
                        joinAllocation.Key,
                        joinAllocation.ConnectionData,
                        joinAllocation.HostConnectionData,
                        true // Secure DTLS
                    );
                }

                bool started = NetworkManager.Singleton.StartClient();
                if (started)
                {
                    Debug.Log("<color=#00FF88><b>[RELAY CLIENT]</b> Client connecting to host...</color>");
                    return true;
                }
                else
                {
                    throw new Exception("NetworkManager failed to start client.");
                }
            }
            catch (Exception ex)
            {
                string error = $"Failed to join room: {ex.Message}";
                Debug.LogError($"[RELAY JOIN ERROR] {error}");
                SetState(SessionState.Error);
                OnSessionError?.Invoke(error);
                return false;
            }
        }

        /// <summary>
        /// Starts a direct local host (Loopback / LAN) for development testing without internet.
        /// </summary>
        public bool StartLocalHost(ushort port = 7777)
        {
            EnsureNetworkManagerReady();
            ActiveNetworkMode = NetworkMode.DirectLocal;
            ActiveJoinCode = "LOCAL";

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData("127.0.0.1", port);
            }

            bool started = NetworkManager.Singleton.StartHost();
            if (started)
            {
                Debug.Log($"<color=#00FF88><b>[LOCAL HOST]</b> Started local host on port {port}.</color>");
                SetState(SessionState.InLobbyWaitingForOpponent);
                OnJoinCodeReady?.Invoke("LOCAL");
            }
            else
            {
                SetState(SessionState.Error);
                OnSessionError?.Invoke("Failed to start local host.");
            }
            return started;
        }

        /// <summary>
        /// Connects directly to a local host IP for development testing.
        /// </summary>
        public bool StartLocalClient(string ip = "127.0.0.1", ushort port = 7777)
        {
            EnsureNetworkManagerReady();
            ActiveNetworkMode = NetworkMode.DirectLocal;
            ActiveJoinCode = "LOCAL";

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.SetConnectionData(ip, port);
            }

            bool started = NetworkManager.Singleton.StartClient();
            if (started)
            {
                Debug.Log($"<color=#00FF88><b>[LOCAL CLIENT]</b> Connecting to {ip}:{port}...</color>");
                SetState(SessionState.Joining);
            }
            else
            {
                SetState(SessionState.Error);
                OnSessionError?.Invoke("Failed to start local client.");
            }
            return started;
        }

        /// <summary>
        /// Gracefully shuts down active network connections and returns to offline state.
        /// </summary>
        public void ShutdownSession()
        {
            if (_currentState == SessionState.Offline && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening))
            {
                return;
            }

            SetState(SessionState.Disconnecting);

            // Cancel any active grace period
            if (DisconnectGracePeriodManager.Instance != null)
            {
                DisconnectGracePeriodManager.Instance.CancelGracePeriod();
            }

            if (NetworkMatchState.Instance != null && NetworkMatchState.Instance.gameObject != null)
            {
                if (NetworkMatchState.Instance.IsSpawned && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
                {
                    NetworkMatchState.Instance.NetworkObject.Despawn();
                }
                Destroy(NetworkMatchState.Instance.gameObject);
            }

            if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
            {
                NetworkManager.Singleton.Shutdown();
            }

            if (_reconnectCoroutine != null)
            {
                StopCoroutine(_reconnectCoroutine);
                _reconnectCoroutine = null;
            }

            ActiveJoinCode = string.Empty;
            ActiveNetworkMode = NetworkMode.None;
            SetState(SessionState.Offline);

            Debug.Log("[NETWORK SESSION] Session cleanly shut down.");
        }

        /// <summary>
        /// Binds an externally created Relay host (e.g. from Quick Match / Multiplayer Services ISession)
        /// to this NetworkSessionManager so state, join code, and match objects stay consistent.
        /// </summary>
        public void BindExternalRelayHost(string joinCode)
        {
            EnsureNetworkManagerReady();
            ActiveJoinCode = joinCode;
            ActiveNetworkMode = NetworkMode.RelayOnline;
            SetState(SessionState.InLobbyWaitingForOpponent);
            OnJoinCodeReady?.Invoke(joinCode);
            EnsureMatchStateSpawned();
            Debug.Log($"<color=#00FF88><b>[EXTERNAL RELAY]</b> Bound external Relay host with code: {joinCode}</color>");
        }

        /// <summary>
        /// Binds an externally joined Relay client (e.g. from Quick Match / Multiplayer Services ISession)
        /// to this NetworkSessionManager.
        /// </summary>
        public void BindExternalRelayClient(string joinCode)
        {
            EnsureNetworkManagerReady();
            ActiveJoinCode = joinCode;
            ActiveNetworkMode = NetworkMode.RelayOnline;
            SetState(SessionState.Joining);
            Debug.Log($"<color=#00FF88><b>[EXTERNAL RELAY]</b> Bound external Relay client for code: {joinCode}</color>");
        }

        public void EnsureMatchStateSpawned()
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

            if (NetworkMatchState.Instance == null)
            {
                GameObject prefab = Resources.Load<GameObject>("NetworkMatchState");
                GameObject stateObj = prefab != null ? Instantiate(prefab) : new GameObject("NetworkMatchState");
                if (stateObj.GetComponent<NetworkMatchState>() == null)
                {
                    stateObj.AddComponent<NetworkMatchState>();
                }
                NetworkObject netObj = stateObj.GetComponent<NetworkObject>();
                if (netObj == null)
                {
                    netObj = stateObj.AddComponent<NetworkObject>();
                }
                if (!netObj.IsSpawned)
                {
                    netObj.Spawn();
                }
                if (Application.isPlaying)
                {
                    DontDestroyOnLoad(stateObj);
                }
                Debug.Log("<color=#00FFAA><b>[NETWORK SESSION]</b> Authoritative NetworkMatchState spawned successfully.</color>");
            }
        }

        private void HandleServerStarted()
        {
            Debug.Log("<color=#00FFAA><b>[NETWORK]</b> Server socket initialized.</color>");
            EnsureMatchStateSpawned();
        }

        private void HandleClientConnected(ulong clientId)
        {
            Debug.Log($"<color=#00FFAA><b>[NETWORK]</b> Client connected: {clientId} (Local? {clientId == NetworkManager.Singleton.LocalClientId})</color>");

            OnPlayerJoined?.Invoke(clientId);

            if (IsHost && ConnectedPlayerCount >= 2)
            {
                // Both players connected! Transition to Match Ready!
                Debug.Log("<color=#00FF88><b>[NETWORK MATCH]</b> Both players connected! Ready to configure match!</color>");
                SetState(SessionState.ConnectedInMatch);
            }
            else if (IsClient && clientId == NetworkManager.Singleton.LocalClientId)
            {
                SetState(SessionState.ConnectedInMatch);
            }
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            Debug.Log($"<color=#FFAA00><b>[NETWORK]</b> Client disconnected: {clientId}</color>");

            bool isLocalDisconnect = (NetworkManager.Singleton != null && clientId == NetworkManager.Singleton.LocalClientId);
            bool wasIntentionalDisconnect = (_currentState == SessionState.Disconnecting || _currentState == SessionState.Offline);

            // Do not fire player-left for the local player when shutting down intentionally
            if (!isLocalDisconnect && !wasIntentionalDisconnect)
            {
                OnPlayerLeft?.Invoke(clientId);
            }

            if (isLocalDisconnect)
            {
                // If local client disconnected during an active match (and not host), attempt reconnect
                if (!IsHost && _currentState == SessionState.ConnectedInMatch && !string.IsNullOrEmpty(ActiveJoinCode))
                {
                    Debug.LogWarning("[NETWORK SESSION] Local client disconnected during active match! Starting reconnect retry loop...");
                    if (_reconnectCoroutine != null)
                    {
                        StopCoroutine(_reconnectCoroutine);
                    }
                    _reconnectCoroutine = StartCoroutine(AttemptAutoReconnectRoutine(ActiveJoinCode));
                    return;
                }

                SetState(SessionState.Offline);

                // Only report error if disconnection was unexpected
                if (!wasIntentionalDisconnect)
                {
                    OnSessionError?.Invoke("Disconnected from game session.");
                }
            }
            else if (IsHost && _currentState == SessionState.ConnectedInMatch && !wasIntentionalDisconnect)
            {
                // Opponent disconnected during active match — start grace period
                Debug.LogWarning($"[NETWORK SESSION] Opponent {clientId} disconnected during active match. Starting grace period.");
                if (DisconnectGracePeriodManager.Instance != null)
                {
                    DisconnectGracePeriodManager.Instance.StartGracePeriod(clientId);
                }
                else
                {
                    // Fallback: no grace manager, treat as permanent leave
                    OnSessionError?.Invoke("Opponent disconnected from the match.");
                }
            }
            else if (IsHost && !wasIntentionalDisconnect)
            {
                OnSessionError?.Invoke("Opponent disconnected.");
            }
        }

        private Coroutine _reconnectCoroutine;

        private System.Collections.IEnumerator AttemptAutoReconnectRoutine(string codeToReconnect)
        {
            SetState(SessionState.Reconnecting);
            const int maxAttempts = 3;
            const float retryDelay = 4f;

            Debug.Log($"<color=#FFAA00><b>[RECONNECT]</b> Initiating client reconnect loop for code: {codeToReconnect} (max {maxAttempts} attempts)...</color>");

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                Debug.Log($"<color=#FFAA00><b>[RECONNECT]</b> Attempt {attempt}/{maxAttempts}...</color>");

                if (NetworkManager.Singleton != null && (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost))
                {
                    NetworkManager.Singleton.Shutdown();
                }

                yield return new WaitForSecondsRealtime(1.0f);

                Task<bool> joinTask = JoinRelaySessionAsync(codeToReconnect);
                while (!joinTask.IsCompleted)
                {
                    yield return null;
                }

                if (joinTask.Result)
                {
                    Debug.Log("<color=#00FF88><b>[RECONNECT]</b> Successfully reconnected to host session!</color>");
                    _reconnectCoroutine = null;
                    yield break;
                }

                if (attempt < maxAttempts)
                {
                    Debug.LogWarning($"[RECONNECT] Attempt {attempt} failed. Waiting {retryDelay}s before retry...");
                    yield return new WaitForSecondsRealtime(retryDelay);
                }
            }

            Debug.LogError("[RECONNECT] All reconnection attempts failed. Returning to offline state.");
            _reconnectCoroutine = null;
            ShutdownSession();
            OnSessionError?.Invoke("Reconnection failed. Match session could not be recovered.");
        }

        // =========================================================================
        // App Lifecycle — Android Backgrounding (Task 6)
        // =========================================================================

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                HandleAppBackgrounded();
            }
            else
            {
                HandleAppResumed();
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) HandleAppBackgrounded();
            else          HandleAppResumed();
        }

        private bool _wasConnectedBeforeSuspend = false;
        private float _suspendTimestamp = 0f;
        private const float MaxSuspendSeconds = 20f;

        private void HandleAppBackgrounded()
        {
            if (ActiveNetworkMode == NetworkMode.None) return; // not in an online session
            _wasConnectedBeforeSuspend = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
            _suspendTimestamp = Time.realtimeSinceStartup;
            Debug.Log("[NETWORK SESSION] App backgrounded during online match.");
        }

        private void HandleAppResumed()
        {
            if (!_wasConnectedBeforeSuspend) return;
            _wasConnectedBeforeSuspend = false;

            float suspendDuration = Time.realtimeSinceStartup - _suspendTimestamp;
            bool stillConnected = NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;

            Debug.Log($"[NETWORK SESSION] App resumed after {suspendDuration:F1}s. Connected={stillConnected}");

            if (!stillConnected)
            {
                Debug.LogWarning("[NETWORK SESSION] Connection lost during background suspend. Returning to menu.");
                OnSessionError?.Invoke("Connection lost while the app was in the background.");
                ShutdownSession();
            }
            else if (suspendDuration > MaxSuspendSeconds)
            {
                // Connection may technically still be alive but Relay socket health is uncertain.
                // For V1, we treat prolonged suspend as a potential disconnect risk and notify the player.
                Debug.LogWarning($"[NETWORK SESSION] App was backgrounded for {suspendDuration:F1}s (>{MaxSuspendSeconds}s). Connection may be stale.");
                OnSessionError?.Invoke("Game was backgrounded for a long time. Connection may have dropped.");
            }
        }

        public void EnsureNetworkManagerReady()
        {
            if (NetworkManager.Singleton == null)
            {
                var existing = FindAnyObjectByType<NetworkManager>();
                if (existing == null)
                {
                    GameObject nmObj = new GameObject("NetworkManager_Persistent");
                    var nm = nmObj.AddComponent<NetworkManager>();
                    nm.SetSingleton();
                    var transport = nmObj.AddComponent<UnityTransport>();
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
                    if (Application.isPlaying)
                    {
                        DontDestroyOnLoad(nmObj);
                    }
                }
            }

            RegisterNetworkCallbacks();
        }

        private void SetState(SessionState newState)
        {
            _currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
