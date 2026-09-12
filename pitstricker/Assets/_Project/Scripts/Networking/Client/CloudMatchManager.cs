using System;
using UnityEngine;
using PitStriker.Networking.Shared;
using PitStriker.Physics;
using PitStriker.Gameplay;

namespace PitStriker.Networking.Client
{
    /// <summary>
    /// Phase 4 Authoritative Client Match Synchronizer:
    /// Coordinates the in-game scene marbles, turns, timers, and scores based on authoritative snapshots
    /// streamed from the Google Cloud dedicated server.
    /// </summary>
    public class CloudMatchManager : MonoBehaviour
    {
        public static CloudMatchManager Instance { get; private set; }

        [Header("State Tracking")]
        [SerializeField] private CloudMatchPhase _currentPhase = CloudMatchPhase.WaitingForPlayers;
        [SerializeField] private int _activePlayerIndex = 0;
        [SerializeField] private float _turnTimerRemaining = NetworkProtocol.DefaultTurnDuration;

        public CloudMatchPhase CurrentPhase => _currentPhase;
        public int ActivePlayerIndex => _activePlayerIndex;
        public float TurnTimerRemaining => _turnTimerRemaining;
        public bool IsOnlineMatchActive => CloudNetworkClient.Instance != null &&
                                          CloudNetworkClient.Instance.IsConnected &&
                                          !string.IsNullOrEmpty(CloudNetworkClient.Instance.ActiveRoomCode) &&
                                          _currentPhase != CloudMatchPhase.WaitingForPlayers &&
                                          _currentPhase != CloudMatchPhase.Abandoned;

        // Marbles in scene
        private MarbleController _marble0;
        private MarbleController _marble1;

        // Latest player statistics
        public CompactPlayerData Player0Data { get; private set; }
        public CompactPlayerData Player1Data { get; private set; }

        // Events for HUD and UI
        public static event Action<int> OnActivePlayerChangedEvent;
        public static event Action<float> OnTimerTickEvent;
        public static event Action<CloudMatchPhase> OnPhaseChangedEvent;
        public static event Action<int, int, int> OnPlayerStatsChangedEvent; // playerIdx, strokes, pit
        public static event Action<int> OnMatchCompletedEvent; // winnerIdx
        public static event Action OnRematchReadyEvent;
        public static event Action<int, Vector3, float> OnNetworkShotExecutedEvent;

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
            }
        }

        private void Start()
        {
            FindSceneMarbles();
            RegisterClientEvents();
        }

        private void OnDestroy()
        {
            UnregisterClientEvents();
            if (Instance == this) Instance = null;
        }

        public void FindSceneMarbles()
        {
            MarbleController[] found = FindObjectsByType<MarbleController>(FindObjectsInactive.Include);
            Array.Sort(found, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            if (found.Length >= 2)
            {
                _marble0 = found[0];
                _marble1 = found[1];
            }
            else if (found.Length == 1)
            {
                _marble0 = found[0];
            }
        }

        public MarbleController GetMarble(int playerIndex)
        {
            return playerIndex == 0 ? _marble0 : _marble1;
        }

        public bool IsMyTurn()
        {
            if (!IsOnlineMatchActive) return true;
            if (CloudNetworkClient.Instance == null) return false;
            return CloudNetworkClient.Instance.LocalPlayerIndex == _activePlayerIndex && _currentPhase == CloudMatchPhase.ReadyToAim;
        }

        public void SubmitLocalShot(Vector3 direction, float force)
        {
            if (!IsMyTurn())
            {
                Debug.LogWarning("[CLOUD MATCH] Cannot shoot: Not your authoritative turn!");
                return;
            }

            // Client-Side Prediction (Phase 5): Apply impulse locally immediately so player feels 0ms latency
            int localIdx = CloudNetworkClient.Instance.LocalPlayerIndex;
            MarbleController localMarble = GetMarble(localIdx);
            if (localMarble != null)
            {
                localMarble.ApplyImpulse(direction, force);
            }

            // Dispatch authoritative intent to cloud server
            CloudNetworkClient.Instance.SubmitShot(direction, force);
        }

        private void RegisterClientEvents()
        {
            if (CloudNetworkClient.Instance != null)
            {
                CloudNetworkClient.Instance.OnMatchStarted += HandleMatchStarted;
                CloudNetworkClient.Instance.OnSnapshotReceived += HandleSnapshotReceived;
                CloudNetworkClient.Instance.OnShotBroadcastReceived += HandleShotBroadcast;
                CloudNetworkClient.Instance.OnMatchCompleted += HandleMatchCompleted;
                CloudNetworkClient.Instance.OnRematchConfirmed += HandleRematchConfirmed;
            }
        }

        private void UnregisterClientEvents()
        {
            if (CloudNetworkClient.Instance != null)
            {
                CloudNetworkClient.Instance.OnMatchStarted -= HandleMatchStarted;
                CloudNetworkClient.Instance.OnSnapshotReceived -= HandleSnapshotReceived;
                CloudNetworkClient.Instance.OnShotBroadcastReceived -= HandleShotBroadcast;
                CloudNetworkClient.Instance.OnMatchCompleted -= HandleMatchCompleted;
                CloudNetworkClient.Instance.OnRematchConfirmed -= HandleRematchConfirmed;
            }
        }

        private void HandleMatchStarted(string roomCode, string p1, string p2)
        {
            FindSceneMarbles();

            _currentPhase = CloudMatchPhase.ReadyToAim;
            _activePlayerIndex = 0;
            _turnTimerRemaining = NetworkProtocol.DefaultTurnDuration;

            // Reset marble positions
            if (_marble0 != null)
            {
                _marble0.ResetPosition(new Vector3(-0.4f, 0.25f, -6.0f));
                _marble0.Halt();
                _marble0.IsRetired = false;
            }
            if (_marble1 != null)
            {
                _marble1.ResetPosition(new Vector3(0.4f, 0.25f, -6.0f));
                _marble1.Halt();
                _marble1.IsRetired = false;
            }

            // Sync with TurnManager so cameras, marbles, and HUD are properly configured
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.ConfigureAndStartMatch(2, new bool[] { false, false }, new string[] { p1, p2 });
                TurnManager.Instance.CurrentPlayerIndex = 0;
            }

            OnPhaseChangedEvent?.Invoke(_currentPhase);
            OnActivePlayerChangedEvent?.Invoke(_activePlayerIndex);

            // Switch to in-game screen if MenuManager exists
            if (UI.MenuManager.Instance != null)
            {
                UI.MenuManager.Instance.ShowScreen(UI.MenuManager.ScreenType.InGame);
            }
        }

        private void HandleSnapshotReceived(WorldSnapshotData snapshot)
        {
            if (_currentPhase != snapshot.Phase)
            {
                _currentPhase = snapshot.Phase;
                OnPhaseChangedEvent?.Invoke(_currentPhase);
            }

            if (_activePlayerIndex != snapshot.ActivePlayerIndex)
            {
                _activePlayerIndex = snapshot.ActivePlayerIndex;
                OnActivePlayerChangedEvent?.Invoke(_activePlayerIndex);
            }

            _turnTimerRemaining = snapshot.TurnTimerRemaining;
            OnTimerTickEvent?.Invoke(_turnTimerRemaining);

            // Check player scores
            if (Player0Data.TotalStrokes != snapshot.Player0.TotalStrokes || Player0Data.CurrentPit != snapshot.Player0.CurrentPit)
            {
                Player0Data = snapshot.Player0;
                OnPlayerStatsChangedEvent?.Invoke(0, snapshot.Player0.TotalStrokes, snapshot.Player0.CurrentPit);
            }
            if (Player1Data.TotalStrokes != snapshot.Player1.TotalStrokes || Player1Data.CurrentPit != snapshot.Player1.CurrentPit)
            {
                Player1Data = snapshot.Player1;
                OnPlayerStatsChangedEvent?.Invoke(1, snapshot.Player1.TotalStrokes, snapshot.Player1.CurrentPit);
            }

            // Push snapshot to prediction and interpolation controller
            if (PredictionAndInterpolationController.Instance != null)
            {
                PredictionAndInterpolationController.Instance.PushSnapshot(snapshot);
            }

            int localIdx = CloudNetworkClient.Instance?.LocalPlayerIndex ?? 0;
            CompactMarbleState localState = localIdx == 0 ? snapshot.Marble0 : snapshot.Marble1;
            MarbleController localMarble = GetMarble(localIdx);
            if (PredictionAndInterpolationController.Instance != null)
            {
                PredictionAndInterpolationController.Instance.ReconcileLocalMarble(localMarble, localState);
            }
        }

        private void Update()
        {
            if (IsOnlineMatchActive && PredictionAndInterpolationController.Instance != null)
            {
                int localIdx = CloudNetworkClient.Instance?.LocalPlayerIndex ?? 0;
                int remoteIdx = 1 - localIdx;
                MarbleController remoteMarble = GetMarble(remoteIdx);
                if (remoteMarble != null)
                {
                    PredictionAndInterpolationController.Instance.UpdateRemoteMarble(remoteMarble, remoteIdx);
                }
            }
        }

        private void HandleShotBroadcast(int playerIndex, ShotIntentData intent)
        {
            int localIdx = CloudNetworkClient.Instance?.LocalPlayerIndex ?? 0;
            MarbleController shotMarble = GetMarble(playerIndex);

            if (playerIndex != localIdx)
            {
                // Remote shot: play juice and launch remote marble
                if (shotMarble != null)
                {
                    shotMarble.ApplyImpulse(intent.Direction, intent.Force);
                }
            }

            // Ensure camera tracks the active shooting marble for both players
            var cam = UnityEngine.Object.FindAnyObjectByType<PitStriker.CameraSystem.SmoothFollowCamera>();
            if (cam != null && shotMarble != null)
            {
                cam.SetTarget(shotMarble.transform);
            }

            OnNetworkShotExecutedEvent?.Invoke(playerIndex, intent.Direction, intent.Force);
        }

        private void HandleMatchCompleted(int winnerIndex)
        {
            _currentPhase = CloudMatchPhase.MatchCompleted;
            OnMatchCompletedEvent?.Invoke(winnerIndex);
        }

        private void HandleRematchConfirmed()
        {
            OnRematchReadyEvent?.Invoke();
            HandleMatchStarted(CloudNetworkClient.Instance?.ActiveRoomCode ?? "", "P1", "P2");
        }
    }
}
