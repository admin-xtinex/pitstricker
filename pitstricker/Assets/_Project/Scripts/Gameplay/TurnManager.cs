using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PitStriker.Physics;
using PitStriker.Input;
using PitStriker.CameraSystem;

namespace PitStriker.Gameplay
{
    /// <summary>
    /// Phase 5 Match & Turn Orchestrator:
    /// Coordinates local pass-and-play multiplayer (1 to 4 players),
    /// turn order rotation, active marble camera framing, bonus strikes on pit capture,
    /// stroke tracking per player, and leaderboard victory.
    /// </summary>
    public class TurnManager : MonoBehaviour
    {
        public enum GameState
        {
            ReadyToAim,
            Rolling,
            Evaluating,
            MatchVictory
        }

        [System.Serializable]
        public class PlayerData
        {
            public int id;
            public string name;
            public Color color;
            public MarbleController marble;
            public int totalStrokes = 0;
            public int currentPit = 1;
            public bool isFinished = false;

            public PlayerData(int id, string name, Color color, MarbleController marble)
            {
                this.id = id;
                this.name = name;
                this.color = color;
                this.marble = marble;
                this.totalStrokes = 0;
                this.currentPit = 1;
                this.isFinished = false;
            }
        }

        public static TurnManager Instance { get; private set; }

        [Header("Multiplayer Configuration")]
        [Range(1, 4)]
        [SerializeField] private int _playerCount = 4;
        [SerializeField] private List<PlayerData> _players = new List<PlayerData>();

        [Header("Course Setup")]
        [SerializeField] private Vector3 _startCenter = new Vector3(0f, 0.3f, -5.5f);
        [SerializeField] private float _startSpacing = 0.45f;

        [Header("Par Configuration")]
        [SerializeField] private int[] _pitPars = new int[] { 2, 3, 3 }; // Pit 1: Par 2, Pit 2: Par 3, Pit 3: Par 3

        // State Tracking
        public GameState CurrentState { get; private set; } = GameState.ReadyToAim;
        public int CurrentPlayerIndex { get; private set; } = 0;
        public PlayerData ActivePlayer => (_players != null && _players.Count > CurrentPlayerIndex) ? _players[CurrentPlayerIndex] : null;
        public int CoursePar => _pitPars[0] + _pitPars[1] + _pitPars[2];
        public int CurrentTargetPit => ActivePlayer != null ? ActivePlayer.currentPit : 1;
        public int CurrentPitPar => (_pitPars != null && CurrentTargetPit >= 1 && CurrentTargetPit <= _pitPars.Length) ? _pitPars[CurrentTargetPit - 1] : 3;
        public int TotalStrokes => ActivePlayer != null ? ActivePlayer.totalStrokes : 0;
        public int PlayerCount => _players.Count;
        public IReadOnlyList<PlayerData> Players => _players;

        private bool _pitSunkThisTurn = false;
        private Coroutine _evaluateCoroutine;

        // Events
        public static event Action<GameState> OnStateChanged;
        public static event Action<PlayerData> OnActivePlayerChanged;
        public static event Action<int, int> OnStrokeCountChanged; // (activePlayerStrokes, totalCoursePar)
        public static event Action<int> OnTargetPitChanged;         // (targetPit: 1, 2, 3)
        public static event Action<PlayerData, List<PlayerData>> OnMatchVictory; // (winner, rankedLeaderboard)
        public static event Action<string> OnStatusMessage;

        public static bool CanAim()
        {
            return Instance == null || Instance.CurrentState == GameState.ReadyToAim;
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }
        }

        private void OnEnable()
        {
            PitZone.OnMarbleSunk += HandleMarbleSunk;
        }

        private void OnDisable()
        {
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
            UnbindAllMarbles();
        }

        private void Start()
        {
            InitializePlayers();
            RestartMatch();
        }

        public void InitializePlayers()
        {
            UnbindAllMarbles();

            // Find all marbles in the scene
            MarbleController[] foundMarbles = FindObjectsByType<MarbleController>(FindObjectsInactive.Exclude);
            Array.Sort(foundMarbles, (a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            _players.Clear();

            Color[] themeColors = new Color[]
            {
                new Color(0f, 0.85f, 1f, 1f),    // P1: Cyan / Blue Swirl
                new Color(1f, 0.25f, 0.25f, 1f), // P2: Crimson Red
                new Color(0.2f, 1f, 0.4f, 1f),   // P3: Emerald Green
                new Color(1f, 0.75f, 0.1f, 1f)   // P4: Amber Gold
            };

            int countToUse = Mathf.Min(_playerCount, Mathf.Max(1, foundMarbles.Length));

            for (int i = 0; i < countToUse; i++)
            {
                MarbleController marble = i < foundMarbles.Length ? foundMarbles[i] : null;
                PlayerData player = new PlayerData(i + 1, $"Player {i + 1}", themeColors[i % themeColors.Length], marble);
                _players.Add(player);

                if (marble != null)
                {
                    marble.OnMarbleLaunched += HandleMarbleLaunched;
                    marble.OnMarbleStopped += HandleMarbleStopped;
                }
            }

            Debug.Log($"<color=#00FFAA><b>[TURN MANAGER]</b> Initialized {_players.Count} players in roster.</color>");
        }

        private void UnbindAllMarbles()
        {
            if (_players == null) return;
            foreach (var p in _players)
            {
                if (p.marble != null)
                {
                    p.marble.OnMarbleLaunched -= HandleMarbleLaunched;
                    p.marble.OnMarbleStopped -= HandleMarbleStopped;
                }
            }
        }

        private void HandleMarbleLaunched()
        {
            if (CurrentState == GameState.MatchVictory || ActivePlayer == null) return;

            ActivePlayer.totalStrokes++;
            _pitSunkThisTurn = false;

            SetState(GameState.Rolling);
            OnStrokeCountChanged?.Invoke(ActivePlayer.totalStrokes, CoursePar);
            OnStatusMessage?.Invoke($"{ActivePlayer.name.ToUpper()}: STROKE #{ActivePlayer.totalStrokes}");
        }

        private void HandleMarbleStopped()
        {
            if (CurrentState != GameState.Rolling) return;

            if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
            _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (ActivePlayer == null || marble != ActivePlayer.marble) return;

            if (pit.PitNumber == ActivePlayer.currentPit)
            {
                _pitSunkThisTurn = true;
                marble.Halt();

                if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
            }
            else
            {
                Debug.LogWarning($"[TURN MANAGER] {ActivePlayer.name} sank Pit #{pit.PitNumber}, but active target is Pit #{ActivePlayer.currentPit}!");
                OnStatusMessage?.Invoke($"WRONG PIT! TARGET IS PIT {ActivePlayer.currentPit}");
            }
        }

        private IEnumerator EvaluateTurnOutcomeRoutine()
        {
            SetState(GameState.Evaluating);

            // Let player savor the sink visual
            yield return new WaitForSeconds(0.8f);

            if (ActivePlayer == null) yield break;

            if (_pitSunkThisTurn)
            {
                _pitSunkThisTurn = false;
                Debug.Log($"<color=#00FF88><b>[GOAL]</b> {ActivePlayer.name} conquered Pit #{ActivePlayer.currentPit} in {ActivePlayer.totalStrokes} total strokes!</color>");

                if (ActivePlayer.currentPit < 3)
                {
                    ActivePlayer.currentPit++;
                    OnTargetPitChanged?.Invoke(ActivePlayer.currentPit);
                    OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} CONQUERED PIT! BONUS STRIKE! ★");

                    // Relocate to next tee
                    RelocateToNextTee(ActivePlayer.marble, ActivePlayer.currentPit);

                    yield return new WaitForSeconds(0.4f);
                    // Sinking grants a BONUS STRIKE (keeps turn)
                    SetState(GameState.ReadyToAim);
                }
                else
                {
                    // Player has completed all 3 pits!
                    ActivePlayer.isFinished = true;
                    ActivePlayer.currentPit = 3;
                    OnTargetPitChanged?.Invoke(3);
                    Debug.Log($"<color=#FFD700><b>[COURSE COMPLETE]</b> {ActivePlayer.name} finished in {ActivePlayer.totalStrokes} strokes!</color>");

                    if (AreAllPlayersFinished())
                    {
                        DeclareMatchVictory();
                    }
                    else
                    {
                        OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} FINISHED! ADVANCING TO NEXT PLAYER ★");
                        yield return new WaitForSeconds(1.0f);
                        AdvanceToNextActivePlayer();
                    }
                }
            }
            else
            {
                // Fairway shot: marble remains where it came to rest
                if (_players.Count > 1)
                {
                    AdvanceToNextActivePlayer();
                }
                else
                {
                    SetState(GameState.ReadyToAim);
                    OnStatusMessage?.Invoke($"AIM FOR PIT {ActivePlayer.currentPit} • STROKE {ActivePlayer.totalStrokes + 1}");
                }
            }

            _evaluateCoroutine = null;
        }

        private void AdvanceToNextActivePlayer()
        {
            if (AreAllPlayersFinished())
            {
                DeclareMatchVictory();
                return;
            }

            // Find next player who hasn't finished
            int attempts = 0;
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
                attempts++;
            }
            while (ActivePlayer != null && ActivePlayer.isFinished && attempts <= _players.Count);

            ActivateCurrentPlayer();
            SetState(GameState.ReadyToAim);
        }

        private void ActivateCurrentPlayer()
        {
            if (ActivePlayer == null) return;

            // 1. Point Camera to active player's marble
            SmoothFollowCamera cam = FindAnyObjectByType<SmoothFollowCamera>();
            if (cam != null && ActivePlayer.marble != null)
            {
                cam.SetTarget(ActivePlayer.marble.transform);
            }

            // 2. Rebind Swipe Controller
            if (SwipeLaunchController.Instance != null && ActivePlayer.marble != null)
            {
                SwipeLaunchController.Instance.SetActiveMarble(ActivePlayer.marble);
            }

            OnActivePlayerChanged?.Invoke(ActivePlayer);
            OnTargetPitChanged?.Invoke(ActivePlayer.currentPit);
            OnStrokeCountChanged?.Invoke(ActivePlayer.totalStrokes, CoursePar);
            OnStatusMessage?.Invoke($"{ActivePlayer.name.ToUpper()}'s TURN • TARGET: PIT {ActivePlayer.currentPit}");
        }

        private bool AreAllPlayersFinished()
        {
            if (_players == null || _players.Count == 0) return true;
            foreach (var p in _players)
            {
                if (!p.isFinished) return false;
            }
            return true;
        }

        private void DeclareMatchVictory()
        {
            SetState(GameState.MatchVictory);

            // Sort leaderboard by fewest strokes
            List<PlayerData> ranked = new List<PlayerData>(_players);
            ranked.Sort((a, b) => a.totalStrokes.CompareTo(b.totalStrokes));

            PlayerData winner = ranked[0];

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayVictory();
            }

            OnMatchVictory?.Invoke(winner, ranked);
            OnStatusMessage?.Invoke($"★ {winner.name.ToUpper()} WINS WITH {winner.totalStrokes} STROKES! ★");
        }

        private void RelocateToNextTee(MarbleController marble, int nextPit)
        {
            if (marble == null) return;

            Vector3 nextTeePos;
            if (nextPit == 2)
            {
                nextTeePos = new Vector3(0f, 0.3f, 3.5f);
            }
            else if (nextPit == 3)
            {
                nextTeePos = new Vector3(0f, 0.3f, 14.5f);
            }
            else
            {
                nextTeePos = _startCenter;
            }

            marble.ResetPosition(nextTeePos);
        }

        public void RestartMatch()
        {
            if (_evaluateCoroutine != null)
            {
                StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = null;
            }

            CurrentPlayerIndex = 0;

            // Reset Pits
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            foreach (PitZone p in allPits)
            {
                p.ResetPit();
            }

            // Position marbles side by side on the opening chalk baseline
            float startX = -((_players.Count - 1) * _startSpacing * 0.5f);
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerData p = _players[i];
                p.totalStrokes = 0;
                p.currentPit = 1;
                p.isFinished = false;

                if (p.marble != null)
                {
                    Vector3 startPos = _startCenter + new Vector3(startX + (i * _startSpacing), 0f, 0f);
                    p.marble.ResetPosition(startPos);
                }
            }

            ActivateCurrentPlayer();
            SetState(GameState.ReadyToAim);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
