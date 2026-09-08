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
            TossPhase,     // Opening Lag Phase: Players flick-throw towards Pit 3 to decide turn order
            ReadyToAim,    // Precision aim for active player
            Rolling,       // Marble in motion
            Evaluating,    // Outcome evaluation
            MatchVictory   // Match finished, podium display
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
            public bool hasTakenFirstShot = false;

            public PlayerData(int id, string name, Color color, MarbleController marble)
            {
                this.id = id;
                this.name = name;
                this.color = color;
                this.marble = marble;
                this.totalStrokes = 0;
                this.currentPit = 1;
                this.isFinished = false;
                this.hasTakenFirstShot = false;
            }
        }

        [System.Serializable]
        public class TossResult
        {
            public PlayerData player;
            public float distanceToPit3;
            public bool sunkInPit3;
        }

        public static TurnManager Instance { get; private set; }

        [Header("Multiplayer Configuration")]
        [Range(1, 4)]
        [SerializeField] private int _playerCount = 4;
        [SerializeField] private List<PlayerData> _players = new List<PlayerData>();

        [Header("Course Setup")]
        [SerializeField] private Vector3 _startCenter = new Vector3(0f, 0.3f, -6.0f);
        [SerializeField] private float _startSpacing = 0.45f;

        [Header("Par Configuration")]
        [SerializeField] private int[] _pitPars = new int[] { 2, 3, 3 }; // Pit 1: Par 2, Pit 2: Par 3, Pit 3: Par 3

        // State Tracking
        public GameState CurrentState { get; private set; } = GameState.TossPhase;
        public int CurrentPlayerIndex { get; private set; } = 0;
        public PlayerData ActivePlayer => (_players != null && _players.Count > CurrentPlayerIndex) ? _players[CurrentPlayerIndex] : null;
        public int CoursePar => _pitPars[0] + _pitPars[1] + _pitPars[2];
        public int CurrentTargetPit => ActivePlayer != null ? ActivePlayer.currentPit : 1;
        public int CurrentPitPar => (_pitPars != null && CurrentTargetPit >= 1 && CurrentTargetPit <= _pitPars.Length) ? _pitPars[CurrentTargetPit - 1] : 3;
        public int TotalStrokes => ActivePlayer != null ? ActivePlayer.totalStrokes : 0;
        public int PlayerCount => _players.Count;
        public IReadOnlyList<PlayerData> Players => _players;

        // Toss & Bonus Play Tracking
        private List<TossResult> _tossResults = new List<TossResult>();
        private int _tossPlayerIndex = 0;
        private bool _pitSunkThisTurn = false;
        private bool _hitOpponentMarbleThisTurn = false;
        private bool _bonusStrikeEarned = false;
        private Coroutine _evaluateCoroutine;
        private Coroutine _settleCoroutine;

        // Events
        public static event Action<GameState> OnStateChanged;
        public static event Action<PlayerData> OnActivePlayerChanged;
        public static event Action<int, int> OnStrokeCountChanged; // (activePlayerStrokes, totalCoursePar)
        public static event Action<int> OnTargetPitChanged;         // (targetPit: 1, 2, 3)
        public static event Action<PlayerData, List<PlayerData>> OnMatchVictory; // (winner, rankedLeaderboard)
        public static event Action<string> OnStatusMessage;

        public static bool CanAim()
        {
            return Instance == null || Instance.CurrentState == GameState.ReadyToAim || Instance.CurrentState == GameState.TossPhase;
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
            MarbleController.OnMarbleHitMarble += HandleMarbleHitMarble;
        }

        private void OnDisable()
        {
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
            MarbleController.OnMarbleHitMarble -= HandleMarbleHitMarble;
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

            // Find all marbles in the scene (including staged/inactive ones)
            MarbleController[] foundMarbles = FindObjectsByType<MarbleController>(FindObjectsInactive.Include);
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

        private void HandleMarbleHitMarble(MarbleController striker, MarbleController hitTarget)
        {
            if (CurrentState == GameState.TossPhase) return;
            if (ActivePlayer == null || striker != ActivePlayer.marble) return;

            if (!_hitOpponentMarbleThisTurn)
            {
                _hitOpponentMarbleThisTurn = true;
                _bonusStrikeEarned = true;
                Debug.Log($"<color=#FFD700><b>[COMBO HIT]</b> {ActivePlayer.name} hit {hitTarget.name}! EXTRA PLAY AWARDED!</color>");
                OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} HIT OPPONENT! EXTRA PLAY AWARDED! ★");
            }
        }

        private void HandleMarbleLaunched()
        {
            if (CurrentState == GameState.MatchVictory) return;

            if (CurrentState == GameState.TossPhase)
            {
                SetState(GameState.Rolling);
                if (_tossPlayerIndex < _players.Count)
                {
                    OnStatusMessage?.Invoke($"★ {_players[_tossPlayerIndex].name.ToUpper()} TOSS IN FLIGHT! ★");
                }
                if (_settleCoroutine != null) StopCoroutine(_settleCoroutine);
                _settleCoroutine = StartCoroutine(WaitForMarblesToSettleRoutine());
                return;
            }

            if (ActivePlayer == null) return;

            ActivePlayer.totalStrokes++;
            ActivePlayer.hasTakenFirstShot = true;
            _pitSunkThisTurn = false;
            _hitOpponentMarbleThisTurn = false;
            _bonusStrikeEarned = false;

            SetState(GameState.Rolling);
            OnStrokeCountChanged?.Invoke(ActivePlayer.totalStrokes, CoursePar);
            OnStatusMessage?.Invoke($"{ActivePlayer.name.ToUpper()}: STROKE #{ActivePlayer.totalStrokes}");

            if (_settleCoroutine != null) StopCoroutine(_settleCoroutine);
            _settleCoroutine = StartCoroutine(WaitForMarblesToSettleRoutine());
        }

        private void HandleMarbleStopped()
        {
            // Settle lifecycle is actively polled and orchestrated in WaitForMarblesToSettleRoutine
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (CurrentState == GameState.TossPhase)
            {
                // In toss phase, sinking into Pit 3 is an instant bullseye!
                marble.Halt();
                if (_settleCoroutine != null) StopCoroutine(_settleCoroutine);
                if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
                return;
            }

            if (ActivePlayer == null || marble != ActivePlayer.marble) return;

            if (pit.PitNumber == ActivePlayer.currentPit)
            {
                _pitSunkThisTurn = true;
                _bonusStrikeEarned = true;
                marble.Halt();

                // Immediately cancel settle wait and evaluate captured pit
                if (_settleCoroutine != null) StopCoroutine(_settleCoroutine);
                if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
            }
            else
            {
                Debug.LogWarning($"[TURN MANAGER] {ActivePlayer.name} sank Pit #{pit.PitNumber}, but active target is Pit #{ActivePlayer.currentPit}!");
                OnStatusMessage?.Invoke($"WRONG PIT! TARGET IS PIT {ActivePlayer.currentPit}");
            }
        }

        /// <summary>
        /// Waits for launched marbles to complete their full roll and come to a stable stop
        /// before advancing turns or displaying the next player.
        /// </summary>
        private IEnumerator WaitForMarblesToSettleRoutine()
        {
            SetState(GameState.Rolling);

            // 1. Mandatory launch grace period: ensures marble has time to accelerate and roll
            yield return new WaitForSeconds(0.6f);

            // 2. Poll until all active marbles in the match have come to a stable stop
            float settleTimer = 0f;
            float maxWait = 14.0f; // Safety timeout in case of microscopic drift
            float timeElapsed = 0f;

            while (timeElapsed < maxWait)
            {
                timeElapsed += Time.deltaTime;

                // If a pit was captured, HandleMarbleSunk will have immediately triggered evaluation
                if (_pitSunkThisTurn)
                {
                    yield break;
                }

                // Check if any active marble is currently in motion
                bool anyMoving = false;
                for (int i = 0; i < _players.Count; i++)
                {
                    MarbleController m = _players[i].marble;
                    if (m != null && m.gameObject.activeInHierarchy && m.IsMoving)
                    {
                        anyMoving = true;
                        break;
                    }
                }

                if (!anyMoving)
                {
                    settleTimer += Time.deltaTime;
                    if (settleTimer >= 0.4f)
                    {
                        // Stably rested for 0.4 consecutive seconds!
                        break;
                    }
                }
                else
                {
                    settleTimer = 0f;
                }

                yield return null;
            }

            // Halt all active marbles to eliminate microscopic physics drift
            for (int i = 0; i < _players.Count; i++)
            {
                if (_players[i].marble != null)
                {
                    _players[i].marble.Halt();
                }
            }

            if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
            _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
            _settleCoroutine = null;
        }

        private IEnumerator EvaluateTurnOutcomeRoutine()
        {
            SetState(GameState.Evaluating);

            // Let player savor the roll and stop
            yield return new WaitForSeconds(0.8f);

            // 1. TOSS PHASE EVALUATION
            if (_tossResults != null && _tossResults.Count < _players.Count)
            {
                yield return StartCoroutine(EvaluateTossOutcomeRoutine());
                _evaluateCoroutine = null;
                yield break;
            }

            // 2. MAIN MATCH EVALUATION
            if (ActivePlayer == null) yield break;

            if (_pitSunkThisTurn)
            {
                _pitSunkThisTurn = false;
                Debug.Log($"<color=#00FF88><b>[GOAL]</b> {ActivePlayer.name} conquered Pit #{ActivePlayer.currentPit} in {ActivePlayer.totalStrokes} total strokes!</color>");

                if (ActivePlayer.currentPit < 3)
                {
                    ActivePlayer.currentPit++;
                    OnTargetPitChanged?.Invoke(ActivePlayer.currentPit);
                    OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} CONQUERED PIT! EXTRA PLAY! ★");

                    // Relocate to next tee ahead of the conquered pit
                    RelocateToNextTee(ActivePlayer.marble, ActivePlayer.currentPit);

                    yield return new WaitForSeconds(0.4f);
                    _bonusStrikeEarned = false;
                    _hitOpponentMarbleThisTurn = false;
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
            else if (_bonusStrikeEarned)
            {
                // Extra play awarded from hitting another player's marble!
                _bonusStrikeEarned = false;
                _hitOpponentMarbleThisTurn = false;
                OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} EARNED AN EXTRA PLAY! ★");
                yield return new WaitForSeconds(0.5f);
                SetState(GameState.ReadyToAim);
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

        private IEnumerator EvaluateTossOutcomeRoutine()
        {
            // Find Pit 3 position
            Vector3 pit3Pos = new Vector3(0f, 0f, 31.0f);
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            PitZone pit3Zone = null;
            foreach (var p in allPits)
            {
                if (p.PitNumber == 3)
                {
                    pit3Pos = p.transform.position;
                    pit3Zone = p;
                    break;
                }
            }

            PlayerData tossingPlayer = _players[_tossPlayerIndex];
            float dist = Vector3.Distance(tossingPlayer.marble.transform.position, pit3Pos);
            bool isSunk = pit3Zone != null && pit3Zone.IsSunk;
            if (isSunk)
            {
                dist = 0.01f;
                pit3Zone.ResetPit(); // Free pit for next throws
            }

            _tossResults.Add(new TossResult
            {
                player = tossingPlayer,
                distanceToPit3 = dist,
                sunkInPit3 = isSunk
            });

            OnStatusMessage?.Invoke($"{tossingPlayer.name.ToUpper()}: {dist:F2}m FROM PIT 3");
            yield return new WaitForSeconds(1.2f);

            _tossPlayerIndex++;

            if (_tossPlayerIndex < _players.Count)
            {
                // Next player's turn to toss
                ActivateTossPlayer(_tossPlayerIndex);
                SetState(GameState.TossPhase);
            }
            else
            {
                // All players have thrown their toss!
                // Sort by shortest distance to Pit 3
                _tossResults.Sort((a, b) => a.distanceToPit3.CompareTo(b.distanceToPit3));

                _players.Clear();
                foreach (var tr in _tossResults)
                {
                    _players.Add(tr.player);
                }

                PlayerData tossWinner = _players[0];
                OnStatusMessage?.Invoke($"★ {tossWinner.name.ToUpper()} WON THE TOSS ({_tossResults[0].distanceToPit3:F2}m)! PLAYS FIRST! ★");
                Debug.Log($"<color=#FFD700><b>[TOSS WINNER]</b> {tossWinner.name} is closest to Pit 3 ({_tossResults[0].distanceToPit3:F2}m)! Wins 1st turn!</color>");

                yield return new WaitForSeconds(2.0f);

                // Reset all pits
                foreach (var p in allPits)
                {
                    p.ResetPit();
                }

                // Traditional rule: All marbles are picked up and brought back to the starting line!
                // Initially hide all marbles so only the active player appears at the start line.
                for (int i = 0; i < _players.Count; i++)
                {
                    PlayerData p = _players[i];
                    p.hasTakenFirstShot = false;
                    p.totalStrokes = 0;
                    p.currentPit = 1;
                    p.isFinished = false;

                    if (p.marble != null)
                    {
                        p.marble.Halt();
                        p.marble.ResetPosition(_startCenter);
                        p.marble.SetVisible(false);
                    }
                }

                // Switch SwipeLaunchController back to Precision Pull-back Aiming for the main game
                if (SwipeLaunchController.Instance != null)
                {
                    SwipeLaunchController.Instance.SetAimMode(SwipeLaunchController.AimMode.PrecisionPullBack);
                }

                CurrentPlayerIndex = 0;
                ActivateCurrentPlayer();
                SetState(GameState.ReadyToAim);
            }
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

            // If active player has not yet taken their first move, stage them at the start line and make visible
            if (!ActivePlayer.hasTakenFirstShot && ActivePlayer.marble != null)
            {
                ActivePlayer.marble.ResetPosition(_startCenter);
                ActivePlayer.marble.SetVisible(true);
            }

            // 1. Point Camera to active player's marble
            SmoothFollowCamera cam = FindAnyObjectByType<SmoothFollowCamera>();
            if (cam != null && ActivePlayer.marble != null)
            {
                cam.SetTarget(ActivePlayer.marble.transform);
            }

            // 2. Rebind Swipe Controller in Precision Mode
            if (SwipeLaunchController.Instance != null && ActivePlayer.marble != null)
            {
                SwipeLaunchController.Instance.SetAimMode(SwipeLaunchController.AimMode.PrecisionPullBack);
                SwipeLaunchController.Instance.SetActiveMarble(ActivePlayer.marble);
            }

            OnActivePlayerChanged?.Invoke(ActivePlayer);
            OnTargetPitChanged?.Invoke(ActivePlayer.currentPit);
            OnStrokeCountChanged?.Invoke(ActivePlayer.totalStrokes, CoursePar);
            OnStatusMessage?.Invoke($"{ActivePlayer.name.ToUpper()}'s TURN • TARGET: PIT {ActivePlayer.currentPit}");
        }

        private void ActivateTossPlayer(int index)
        {
            if (index >= _players.Count) return;

            PlayerData p = _players[index];

            // Staging: Position this tossing player at the start line and make visible
            if (p.marble != null)
            {
                p.marble.ResetPosition(_startCenter);
                p.marble.SetVisible(true);
            }

            // Point Camera
            SmoothFollowCamera cam = FindAnyObjectByType<SmoothFollowCamera>();
            if (cam != null && p.marble != null)
            {
                cam.SetTarget(p.marble.transform);
            }

            // Bind SwipeLaunchController in Forward Flick Throw Mode
            if (SwipeLaunchController.Instance != null && p.marble != null)
            {
                SwipeLaunchController.Instance.SetAimMode(SwipeLaunchController.AimMode.ForwardFlickThrow);
                SwipeLaunchController.Instance.SetActiveMarble(p.marble);
            }

            OnActivePlayerChanged?.Invoke(p);
            OnStatusMessage?.Invoke($"TOSS: {p.name.ToUpper()} • SWIPE FORWARD TO PIT 3!");
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
                nextTeePos = new Vector3(0f, 0.3f, 4.5f);
            }
            else if (nextPit == 3)
            {
                nextTeePos = new Vector3(0f, 0.3f, 18.0f);
            }
            else
            {
                nextTeePos = _startCenter;
            }

            marble.ResetPosition(nextTeePos);
        }

        public void RestartMatch()
        {
            if (_settleCoroutine != null)
            {
                StopCoroutine(_settleCoroutine);
                _settleCoroutine = null;
            }

            if (_evaluateCoroutine != null)
            {
                StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = null;
            }

            CurrentPlayerIndex = 0;
            _tossPlayerIndex = 0;
            _tossResults.Clear();
            _bonusStrikeEarned = false;
            _hitOpponentMarbleThisTurn = false;

            // Reset Pits
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            foreach (PitZone p in allPits)
            {
                p.ResetPit();
            }

            // Hide all marbles initially; each player will appear at the start line one-by-one on their turn
            for (int i = 0; i < _players.Count; i++)
            {
                PlayerData p = _players[i];
                p.totalStrokes = 0;
                p.currentPit = 1;
                p.isFinished = false;
                p.hasTakenFirstShot = false;

                if (p.marble != null)
                {
                    p.marble.Halt();
                    p.marble.ResetPosition(_startCenter);
                    p.marble.SetVisible(false);
                }
            }

            // Start in Toss Phase: Only Player 1 is visible at the starting point!
            SetState(GameState.TossPhase);
            ActivateTossPlayer(0);
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }
    }
}
