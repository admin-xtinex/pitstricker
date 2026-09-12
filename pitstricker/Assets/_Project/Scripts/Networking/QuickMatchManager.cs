using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Netcode;
using Unity.Services.Multiplayer;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 7 — Quick Match Manager.
    ///
    /// Uses Unity Multiplayer Services 2.3+ MatchmakeSessionAsync to find or
    /// create a 2-player Relay-based session automatically.
    ///
    /// Match criteria applied via QuickJoinOptions filters:
    ///   - AvailableSlots >= 1 (needs room for this player)
    ///   - StringIndex1 == GameMode constant (game mode identity)
    ///   - StringIndex2 == Application.version (version compatibility)
    ///   - CreateSession = true  (becomes host if no open room found)
    ///
    /// Lifecycle:
    ///   StartQuickMatch() → OnSearching → OnMatchFound / OnTimeout / OnCancelled / OnError
    ///   CancelSearch()   → cancels the outstanding MatchmakeSessionAsync call
    /// </summary>
    public class QuickMatchManager : MonoBehaviour
    {
        public static QuickMatchManager Instance { get; private set; }

        // ---- Constants ----
        public const string GameModeKey   = "pit_striker_v1";
        public const float  SearchTimeout = 30f; // seconds

        // ---- Events ----
        public static event Action        OnSearching;
        public static event Action        OnMatchFound;
        public static event Action        OnTimeout;
        public static event Action        OnCancelled;
        public static event Action<string> OnError;

        // ---- State ----
        private CancellationTokenSource _cts;
        private ISession                _session;
        private bool                    _isSearching = false;

        public bool IsSearching => _isSearching;

        // =========================================================================
        // Lifecycle
        // =========================================================================

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(this); return; }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            CancelSearch();
        }

        // =========================================================================
        // Public API
        // =========================================================================

        /// <summary>
        /// Begins matchmaking. Fires OnSearching immediately, then one of
        /// OnMatchFound / OnTimeout / OnCancelled / OnError when complete.
        /// </summary>
        public async void StartQuickMatch()
        {
            if (_isSearching)
            {
                Debug.LogWarning("[QUICK MATCH] StartQuickMatch called while already searching.");
                return;
            }

            // Ensure UGS is ready
            bool authenticated = await NetworkBootstrap.InitializeAndSignInAsync();
            if (!authenticated)
            {
                OnError?.Invoke("Unable to sign into Unity Gaming Services. Check your connection.");
                return;
            }

            _isSearching = true;
            _cts = new CancellationTokenSource();

            Debug.Log("<color=#00FFAA><b>[QUICK MATCH]</b> Starting matchmaking search...</color>");
            OnSearching?.Invoke();

            try
            {
                // Define filters: game mode + app version compatibility
                var filters = new List<FilterOption>
                {
                    new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual),
                    new FilterOption(FilterField.StringIndex1, GameModeKey, FilterOperation.Equal),
                    new FilterOption(FilterField.StringIndex2, Application.version, FilterOperation.Equal)
                };

                var quickJoinOptions = new QuickJoinOptions
                {
                    Filters       = filters,
                    Timeout       = TimeSpan.FromSeconds(SearchTimeout),
                    CreateSession = true   // become host if no open session found
                };

                if (NetworkSessionManager.Instance != null)
                {
                    NetworkSessionManager.Instance.EnsureNetworkManagerReady();
                }

                var sessionOptions = new SessionOptions { MaxPlayers = 2 }
                    .WithRelayNetwork();

                // Attach game mode + version as properties so created sessions are filterable
                sessionOptions.SessionProperties = new Dictionary<string, SessionProperty>
                {
                    { "string_index_1", new SessionProperty(GameModeKey, VisibilityPropertyOptions.Public) },
                    { "string_index_2", new SessionProperty(Application.version, VisibilityPropertyOptions.Public) }
                };

                var matchmakeTask = MultiplayerService.Instance.MatchmakeSessionAsync(quickJoinOptions, sessionOptions);
                var cancelTcs = new TaskCompletionSource<bool>();
                using (_cts.Token.Register(() => cancelTcs.TrySetResult(true)))
                {
                    var completedTask = await Task.WhenAny(matchmakeTask, cancelTcs.Task);
                    if (completedTask == cancelTcs.Task)
                    {
                        throw new OperationCanceledException();
                    }
                    _session = await matchmakeTask;
                }

                if (_session == null)
                {
                    throw new InvalidOperationException("Matchmaking returned null session.");
                }

                Debug.Log($"<color=#00FF88><b>[QUICK MATCH]</b> Session found/created! Id={_session.Id}</color>");
                _isSearching = false;

                // Bridge the ISession to NGO's NetworkManager via NetworkSessionManager
                await BridgeSessionToNetworkManagerAsync(_session);

                OnMatchFound?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[QUICK MATCH] Search cancelled by user.");
                _isSearching = false;
                OnCancelled?.Invoke();
            }
            catch (TimeoutException)
            {
                Debug.LogWarning("[QUICK MATCH] Matchmaking timed out — no opponent found.");
                _isSearching = false;
                OnTimeout?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[QUICK MATCH] Matchmaking failed: {ex.Message}");
                _isSearching = false;
                OnError?.Invoke($"Matchmaking failed: {ex.Message}");
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// Cancels an active matchmaking search.
        /// </summary>
        public async void CancelSearch()
        {
            if (!_isSearching && _session == null) return;
            Debug.Log("[QUICK MATCH] Cancelling search and cleaning session...");
            _isSearching = false;
            _cts?.Cancel();

            if (_session != null)
            {
                try
                {
                    await _session.LeaveAsync();
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[QUICK MATCH] Error leaving session: {ex.Message}");
                }
                _session = null;
            }

            if (NetworkSessionManager.Instance != null)
            {
                NetworkSessionManager.Instance.ShutdownSession();
            }

            OnCancelled?.Invoke();
        }

        // =========================================================================
        // ISession → NetworkManager bridge
        // =========================================================================

        /// <summary>
        /// After MatchmakeSessionAsync returns a session, extract the Relay join code
        /// and connect the NGO NetworkManager as host or client depending on role.
        /// </summary>
        private async Task BridgeSessionToNetworkManagerAsync(ISession session)
        {
            if (NetworkSessionManager.Instance == null)
            {
                throw new InvalidOperationException("NetworkSessionManager not available.");
            }

            NetworkSessionManager.Instance.EnsureNetworkManagerReady();

            bool isHost = session.IsHost;
            Debug.Log($"[QUICK MATCH] Session role: {(isHost ? "HOST" : "CLIENT")}");

            string sessionCode = session.Code ?? session.Id;

            if (isHost)
            {
                // The ISession was configured with .WithRelayNetwork()
                // Start the Relay network on the session handler if not already started
                if (session.AsHost().Network.State != NetworkState.Started)
                {
                    Debug.Log($"[QUICK MATCH] Starting Session Relay Network for host...");
                    await session.AsHost().Network.StartRelayNetworkAsync(new RelayNetworkOptions());
                }

                NetworkSessionManager.Instance.BindExternalRelayHost(sessionCode);
                Debug.Log($"[QUICK MATCH] Host successfully bound to NetworkManager with code: {sessionCode}");
            }
            else
            {
                // We joined an existing session
                NetworkSessionManager.Instance.BindExternalRelayClient(sessionCode);
                Debug.Log($"[QUICK MATCH] Client bound to session code: {sessionCode}. Awaiting NetworkManager connection...");

                // Wait for NetworkManager client to connect via the Multiplayer Services handler
                float timeout = 15f;
                float start = Time.realtimeSinceStartup;
                while ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient) 
                       && Time.realtimeSinceStartup - start < timeout)
                {
                    await Task.Delay(200);
                }

                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
                {
                    // Fallback: If automatic join via session network didn't connect within timeout,
                    // attempt direct join using session code if available.
                    if (!string.IsNullOrEmpty(sessionCode))
                    {
                        Debug.LogWarning($"[QUICK MATCH] Session handler did not connect client in time. Attempting fallback JoinRelaySessionAsync with {sessionCode}...");
                        bool joined = await NetworkSessionManager.Instance.JoinRelaySessionAsync(sessionCode);
                        if (!joined)
                        {
                            throw new TimeoutException("Failed to connect client to host Relay session.");
                        }
                    }
                    else
                    {
                        throw new TimeoutException("Timed out waiting for client to connect to host Relay session.");
                    }
                }
            }
        }
    }
}
