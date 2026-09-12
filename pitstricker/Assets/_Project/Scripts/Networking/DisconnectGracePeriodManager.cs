using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace PitStriker.Networking
{
    /// <summary>
    /// Phase 6 Disconnect Grace Period Manager:
    ///
    /// When an opponent disconnects during an active match, this component:
    /// 1. Starts a 30-second countdown before declaring permanent leave.
    /// 2. Pauses the authoritative turn timer so gameplay does not time-out
    ///    during the wait.
    /// 3. Detects if the same client reconnects within the window via
    ///    NGO's OnClientConnectedCallback and cancels the countdown.
    /// 4. If the window expires without reconnect → fires OnGracePeriodExpired.
    ///
    /// Attach to the same persistent GameObject as NetworkSessionManager.
    /// </summary>
    public class DisconnectGracePeriodManager : MonoBehaviour
    {
        public static DisconnectGracePeriodManager Instance { get; private set; }

        public const float GraceDuration = 30f;

        // ---- Events (subscribe anywhere, fired on main thread) ----
        public static event Action<float>  OnGracePeriodStarted;   // total seconds
        public static event Action<float>  OnGracePeriodTick;      // seconds remaining
        public static event Action         OnGracePeriodExpired;   // permanent leave
        public static event Action         OnOpponentReturned;     // reconnected in time

        // ---- State ----
        private bool   _graceActive          = false;
        private ulong  _disconnectedClientId = ulong.MaxValue;
        private Coroutine _graceCoroutine    = null;

        public bool IsGraceActive => _graceActive;

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
            CancelGracePeriod();
            UnregisterNetworkCallbacks();
        }

        private void OnEnable()  => RegisterNetworkCallbacks();
        private void OnDisable() => UnregisterNetworkCallbacks();

        private void RegisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback    += HandleClientReconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback   += HandleClientDisconnectedRaw;
            }
        }

        private void UnregisterNetworkCallbacks()
        {
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnClientConnectedCallback    -= HandleClientReconnected;
                NetworkManager.Singleton.OnClientDisconnectCallback   -= HandleClientDisconnectedRaw;
            }
        }

        // =========================================================================
        // Public API
        // =========================================================================

        /// <summary>
        /// Begin the grace window for a disconnected opponent.
        /// Called by NetworkSessionManager when an opponent leaves during a match.
        /// </summary>
        public void StartGracePeriod(ulong disconnectedClientId)
        {
            if (_graceActive)
            {
                Debug.LogWarning("[GRACE] Grace period already running — resetting for new disconnect.");
                CancelGracePeriod();
            }

            _disconnectedClientId = disconnectedClientId;
            _graceActive = true;

            // Pause authoritative timer so the active player doesn't lose their turn
            if (NetworkMatchState.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkMatchState.Instance.ServerPauseTimer();
            }

            Debug.Log($"<color=#FFAA00><b>[GRACE]</b> Starting {GraceDuration}s grace period for client {disconnectedClientId}.</color>");
            OnGracePeriodStarted?.Invoke(GraceDuration);
            _graceCoroutine = StartCoroutine(GraceCountdownRoutine());
        }

        /// <summary>
        /// Cancel any active grace period (e.g. session fully shut down, or session cleanup).
        /// </summary>
        public void CancelGracePeriod()
        {
            if (_graceCoroutine != null)
            {
                StopCoroutine(_graceCoroutine);
                _graceCoroutine = null;
            }
            _graceActive = false;
            _disconnectedClientId = ulong.MaxValue;
        }

        /// <summary>
        /// Test / Admin hook to force-expire the current grace period immediately without waiting 30s.
        /// </summary>
        public void ForceExpireGracePeriodForTesting()
        {
            if (!_graceActive) return;
            CancelGracePeriod();
            Debug.LogWarning("<color=#FF5500><b>[GRACE]</b> Grace period expired (forced). Declaring match abandoned.</color>");
            OnGracePeriodExpired?.Invoke();
        }

        // =========================================================================
        // Internal
        // =========================================================================

        private IEnumerator GraceCountdownRoutine()
        {
            float remaining = GraceDuration;

            while (remaining > 0f)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                remaining -= 0.5f;
                OnGracePeriodTick?.Invoke(Mathf.Max(0f, remaining));
            }

            // Grace expired — declare permanent leave
            _graceActive = false;
            _graceCoroutine = null;
            _disconnectedClientId = ulong.MaxValue;

            Debug.LogWarning("<color=#FF5500><b>[GRACE]</b> Grace period expired. Opponent did not return. Declaring match abandoned.</color>");
            OnGracePeriodExpired?.Invoke();
        }

        private void HandleClientReconnected(ulong clientId)
        {
            if (!_graceActive) return;
            if (clientId != _disconnectedClientId) return;

            // Same client reconnected within the window!
            Debug.Log($"<color=#00FF88><b>[GRACE]</b> Opponent {clientId} reconnected within grace window!</color>");
            CancelGracePeriod();

            // Resume authoritative timer
            if (NetworkMatchState.Instance != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
            {
                NetworkMatchState.Instance.ServerResumeTimer();
            }

            OnOpponentReturned?.Invoke();
        }

        private void HandleClientDisconnectedRaw(ulong clientId)
        {
            // Only respond to opponent disconnects during an active match.
            // NetworkSessionManager.HandleOnlinePlayerLeft handles routing logic;
            // this manager only reacts if NetworkSessionManager triggers StartGracePeriod.
        }
    }
}
