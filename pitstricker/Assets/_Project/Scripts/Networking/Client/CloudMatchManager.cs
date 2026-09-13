using System;
using UnityEngine;
using PitStriker.Networking.Shared;
using PitStriker.Physics;
using PitStriker.Gameplay;

namespace PitStriker.Networking.Client
{
    public class CloudMatchManager : MonoBehaviour
    {
        public static CloudMatchManager Instance { get; private set; }

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

        private bool _eventsBound;
        private MarbleController _marble0;
        private MarbleController _marble1;
        public CompactPlayerData Player0Data { get; private set; }
        public CompactPlayerData Player1Data { get; private set; }

        public static event Action<int> OnActivePlayerChangedEvent;
        public static event Action<float> OnTimerTickEvent;
        public static event Action<CloudMatchPhase> OnPhaseChangedEvent;
        public static event Action<int, int, int> OnPlayerStatsChangedEvent;
        public static event Action<int> OnMatchCompletedEvent;
        public static event Action OnRematchReadyEvent;
        public static event Action<int, Vector3, float> OnNetworkShotExecutedEvent;

        private void Awake()
        {
            if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
            else if (Instance != this) Destroy(gameObject);
        }

        private void Start()
        {
            FindSceneMarbles();
            RegisterClientEvents();
        }

        private void Update()
        {
            if (!_eventsBound)
                RegisterClientEvents();

            if (!IsOnlineMatchActive || PredictionAndInterpolationController.Instance == null) return;
            int localIdx = CloudNetworkClient.Instance != null ? CloudNetworkClient.Instance.ResolvedLocalPlayerIndex() : 0;
            if (localIdx < 0) localIdx = 0;
            MarbleController remoteMarble = GetMarble(1 - localIdx);
            if (remoteMarble != null)
                PredictionAndInterpolationController.Instance.UpdateRemoteMarble(remoteMarble, 1 - localIdx);
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
            if (found.Length >= 2) { _marble0 = found[0]; _marble1 = found[1]; }
            else if (found.Length == 1) _marble0 = found[0];
        }

        public MarbleController GetMarble(int playerIndex) => playerIndex == 0 ? _marble0 : _marble1;

        public bool IsMyTurn()
        {
            if (!IsOnlineMatchActive) return true;
            if (CloudNetworkClient.Instance == null) return false;
            int local = CloudNetworkClient.Instance.ResolvedLocalPlayerIndex();
            if (local < 0) return false;
            if (_currentPhase == CloudMatchPhase.Rolling || _currentPhase == CloudMatchPhase.Evaluating
                || _currentPhase == CloudMatchPhase.MatchCompleted || _currentPhase == CloudMatchPhase.Abandoned)
                return false;
            return local == _activePlayerIndex;
        }

        public void SubmitLocalShot(Vector3 direction, float force)
        {
            if (!IsMyTurn())
            {
                Debug.LogWarning("[CLOUD MATCH] Cannot shoot: Not your authoritative turn!");
                return;
            }
            int localIdx = CloudNetworkClient.Instance.ResolvedLocalPlayerIndex();
            MarbleController localMarble = GetMarble(localIdx);
            if (localMarble != null) localMarble.ApplyImpulse(direction, force);
            CloudNetworkClient.Instance.SubmitShot(direction, force);
        }

        private void RegisterClientEvents()
        {
            if (_eventsBound || CloudNetworkClient.Instance == null) return;
            CloudNetworkClient.Instance.OnMatchStarted += HandleMatchStarted;
            CloudNetworkClient.Instance.OnSnapshotReceived += HandleSnapshotReceived;
            CloudNetworkClient.Instance.OnShotBroadcastReceived += HandleShotBroadcast;
            CloudNetworkClient.Instance.OnMatchCompleted += HandleMatchCompleted;
            CloudNetworkClient.Instance.OnRematchConfirmed += HandleRematchConfirmed;
            _eventsBound = true;
            Debug.Log("[CLOUD MATCH] Bound to CloudNetworkClient events.");
        }

        private void UnregisterClientEvents()
        {
            if (CloudNetworkClient.Instance == null) return;
            CloudNetworkClient.Instance.OnMatchStarted -= HandleMatchStarted;
            CloudNetworkClient.Instance.OnSnapshotReceived -= HandleSnapshotReceived;
            CloudNetworkClient.Instance.OnShotBroadcastReceived -= HandleShotBroadcast;
            CloudNetworkClient.Instance.OnMatchCompleted -= HandleMatchCompleted;
            CloudNetworkClient.Instance.OnRematchConfirmed -= HandleRematchConfirmed;
            _eventsBound = false;
        }

        private void HandleMatchStarted(string roomCode, string p1, string p2)
        {
            FindSceneMarbles();
            _currentPhase = CloudMatchPhase.ReadyToAim;
            _activePlayerIndex = 0;
            _turnTimerRemaining = NetworkProtocol.DefaultTurnDuration;
            if (_marble0 != null) { _marble0.ResetPosition(new Vector3(-0.4f, 0.25f, -6.0f)); _marble0.Halt(); _marble0.IsRetired = false; }
            if (_marble1 != null) { _marble1.ResetPosition(new Vector3(0.4f, 0.25f, -6.0f)); _marble1.Halt(); _marble1.IsRetired = false; }
            if (TurnManager.Instance != null)
            {
                TurnManager.Instance.ConfigureAndStartMatch(2, new bool[] { false, false }, new string[] { p1, p2 });
                TurnManager.Instance.CurrentPlayerIndex = 0;
            }
            OnPhaseChangedEvent?.Invoke(_currentPhase);
            OnActivePlayerChangedEvent?.Invoke(_activePlayerIndex);
            if (UI.MenuManager.Instance != null) UI.MenuManager.Instance.ShowScreen(UI.MenuManager.ScreenType.InGame);
        }

        private void HandleSnapshotReceived(WorldSnapshotData snapshot)
        {
            if (_currentPhase != snapshot.Phase) { _currentPhase = snapshot.Phase; OnPhaseChangedEvent?.Invoke(_currentPhase); }
            if (_activePlayerIndex != snapshot.ActivePlayerIndex) { _activePlayerIndex = snapshot.ActivePlayerIndex; OnActivePlayerChangedEvent?.Invoke(_activePlayerIndex); }
            _turnTimerRemaining = snapshot.TurnTimerRemaining;
            OnTimerTickEvent?.Invoke(_turnTimerRemaining);
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
            if (PredictionAndInterpolationController.Instance != null)
                PredictionAndInterpolationController.Instance.PushSnapshot(snapshot);
            int localIdx = CloudNetworkClient.Instance != null ? CloudNetworkClient.Instance.ResolvedLocalPlayerIndex() : 0;
            if (localIdx < 0) localIdx = 0;
            CompactMarbleState localState = localIdx == 0 ? snapshot.Marble0 : snapshot.Marble1;
            if (PredictionAndInterpolationController.Instance != null)
                PredictionAndInterpolationController.Instance.ReconcileLocalMarble(GetMarble(localIdx), localState);
        }

        private void HandleShotBroadcast(int playerIndex, ShotIntentData intent)
        {
            int localIdx = CloudNetworkClient.Instance != null ? CloudNetworkClient.Instance.ResolvedLocalPlayerIndex() : 0;
            MarbleController shotMarble = GetMarble(playerIndex);
            if (playerIndex != localIdx && shotMarble != null)
                shotMarble.ApplyImpulse(intent.Direction, intent.Force);
            var cam = UnityEngine.Object.FindAnyObjectByType<PitStriker.CameraSystem.SmoothFollowCamera>();
            if (cam != null && shotMarble != null) cam.SetTarget(shotMarble.transform);
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
