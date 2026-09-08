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
        [SerializeField] private int _playerCount = 2;
        [SerializeField] private List<PlayerData> _players = new List<PlayerData>();

        [Header("Course Setup")]
        [SerializeField] private Vector3 _startCenter = new Vector3(0f, 0.3f, -6.0f);

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
        private List<PlayerData> _placementPodium = new List<PlayerData>();
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
                    marble.gameObject.SetActive(true);
                    marble.IsRetired = false;
                    Rigidbody rb = marble.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = false;
                    marble.OnMarbleLaunched += HandleMarbleLaunched;
                    marble.OnMarbleStopped += HandleMarbleStopped;
                }
            }

            // Deactivate and hide unused extra marbles so they don't sit on the fairway or collide
            for (int i = countToUse; i < foundMarbles.Length; i++)
            {
                if (foundMarbles[i] != null)
                {
                    foundMarbles[i].Halt();
                    foundMarbles[i].SetVisible(false);
                    foundMarbles[i].gameObject.SetActive(false);
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
            // If already evaluating turn or match is concluded, ignore re-entrant events!
            if (CurrentState == GameState.Evaluating || CurrentState == GameState.MatchVictory)
            {
                return;
            }

            // Detect if we are in the Toss Phase (either aiming or in-flight)
            bool isTossPhase = (_tossResults != null && _tossResults.Count < _players.Count);
            if (isTossPhase)
            {
                // In toss phase, sinking into Pit 3 is an instant bullseye!
                if (pit.PitNumber == 3)
                {
                    marble.Halt();
                    _pitSunkThisTurn = true;
                    if (_settleCoroutine != null)
                    {
                        StopCoroutine(_settleCoroutine);
                        _settleCoroutine = null;
                    }
                    if (_evaluateCoroutine == null)
                    {
                        _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
                    }
                }
                return;
            }

            if (ActivePlayer == null || ActivePlayer.isFinished) return;

            // 1. If active player sank into their targeted pit: GOAL!
            if (marble == ActivePlayer.marble && pit.PitNumber == ActivePlayer.currentPit)
            {
                _pitSunkThisTurn = true;
                _bonusStrikeEarned = true;
                marble.Halt();

                // Immediately cancel settle wait and evaluate captured pit
                if (_settleCoroutine != null)
                {
                    StopCoroutine(_settleCoroutine);
                    _settleCoroutine = null;
                }
                if (_evaluateCoroutine == null)
                {
                    _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
                }
            }
            // 2. If an opponent's marble was knocked into the pit:
            else if (marble != ActivePlayer.marble)
            {
                Debug.Log($"<color=#FF8800><b>[POCKETED OPPONENT]</b> Opponent {marble.name} was pocketed in Pit #{pit.PitNumber}! Relocating to fairway rim.</color>");
                Vector3 rimPos = pit.transform.position + new Vector3(2.5f, 0.25f, 0f);
                marble.ResetPosition(rimPos);
                pit.ResetPit();
                OnStatusMessage?.Invoke($"OPPONENT POCKETED! RELOCATED TO FAIRWAY");
            }
            // 3. Active player sank into the wrong pit (e.g. Pit 2 when aiming for Pit 1)
            else
            {
                Debug.LogWarning($"[TURN MANAGER] {ActivePlayer.name} sank Pit #{pit.PitNumber}, but active target is Pit #{ActivePlayer.currentPit}!");
                OnStatusMessage?.Invoke($"WRONG PIT! TARGET IS PIT {ActivePlayer.currentPit}");
                Vector3 rimPos = pit.transform.position + new Vector3(2.5f, 0.25f, 0f);
                marble.ResetPosition(rimPos);
                pit.ResetPit();
            }
        }

        /// <summary>
        /// Deterministically audits all pits and marbles once all physical motion has ceased.
        /// Guarantees that no marble is ever stranded inside a pit basin without triggering goal or relocation,
        /// even under multi-marble collisions or sleeping PhysX rigidbodies.
        /// </summary>
        private void AuditPitsPostSettle()
        {
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            if (allPits == null || allPits.Length == 0 || _players == null) return;

            bool isTossPhase = (_tossResults != null && _tossResults.Count < _players.Count);

            foreach (var pit in allPits)
            {
                foreach (var player in _players)
                {
                    MarbleController marble = player.marble;
                    if (marble == null || !marble.gameObject.activeInHierarchy || player.isFinished || marble.IsRetired) continue;

                    float dist = Vector2.Distance(new Vector2(marble.transform.position.x, marble.transform.position.z), new Vector2(pit.transform.position.x, pit.transform.position.z));
                    float relY = marble.transform.position.y - pit.transform.position.y;

                    if (player == ActivePlayer)
                    {
                        Debug.Log($"[PIT AUDIT] Pit #{pit.PitNumber} (target={ActivePlayer.currentPit}) vs Active {player.name}: dist={dist:F2}m, relY={relY:F2}m, isInside={pit.IsMarbleInsidePit(marble)}");
                    }

                    if (pit.IsMarbleInsidePit(marble))
                    {
                        marble.Halt();

                        if (isTossPhase)
                        {
                            if (pit.PitNumber == 3)
                            {
                                _pitSunkThisTurn = true;
                                pit.MarkSunk(marble, false);
                                Debug.Log($"<color=#00FFAA><b>[TOSS BULLSEYE]</b> {player.name} sank Pit 3!</color>");
                            }
                            else
                            {
                                // In toss phase, if a short shot landed in Pit 1 or 2, relocate to rim so pit stays clear
                                Vector3 rimPos = pit.transform.position + new Vector3(2.5f, 0.25f, 0f);
                                marble.ResetPosition(rimPos);
                                pit.ResetPit();
                            }
                        }
                        else
                        {
                            // MAIN MATCH
                            if (player == ActivePlayer)
                            {
                                if (pit.PitNumber == ActivePlayer.currentPit)
                                {
                                    // ACTIVE PLAYER SANK TARGET PIT!
                                    _pitSunkThisTurn = true;
                                    _bonusStrikeEarned = true;
                                    pit.MarkSunk(marble, false);
                                    Debug.Log($"<color=#00FFAA><b>[PIT AUDIT GOAL]</b> Active player {player.name} is settled inside target Pit #{pit.PitNumber}!</color>");
                                }
                                else
                                {
                                    // Active player in wrong pit: safely relocate to rim
                                    Debug.LogWarning($"[PIT AUDIT] {player.name} settled in Pit #{pit.PitNumber}, but target is Pit #{ActivePlayer.currentPit}! Relocating to rim.");
                                    Vector3 rimPos = pit.transform.position + new Vector3(2.5f, 0.25f, 0f);
                                    marble.ResetPosition(rimPos);
                                    pit.ResetPit();
                                }
                            }
                            else
                            {
                                // OPPONENT MARBLE IN PIT: Pocketed opponent!
                                Debug.Log($"<color=#FF8800><b>[PIT AUDIT]</b> Opponent {player.name} is inside Pit #{pit.PitNumber}! Relocating to fairway rim.</color>");
                                Vector3 rimPos = pit.transform.position + new Vector3(2.5f, 0.25f, 0f);
                                marble.ResetPosition(rimPos);
                                pit.ResetPit();
                                OnStatusMessage?.Invoke($"OPPONENT POCKETED! RELOCATED TO FAIRWAY");
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Waits for launched marbles to complete their full roll and come to a stable stop
        /// before advancing turns or displaying the next player.
        /// </summary>
        private IEnumerator WaitForMarblesToSettleRoutine()
        {
            SetState(GameState.Rolling);

            // 1. Snappy launch grace period: ensures impulse integration before checking rest
            yield return new WaitForSeconds(0.35f);

            // 2. Poll until all active marbles in the match have come to a stable stop
            float settleTimer = 0f;
            float maxWait = CurrentState == GameState.TossPhase ? 7.5f : 5.0f; // Bounded turn duration
            float timeElapsed = 0f;

            while (timeElapsed < maxWait)
            {
                timeElapsed += Time.deltaTime;

                // If a pit was captured or evaluation is active, terminate settle wait immediately
                if (_pitSunkThisTurn || _evaluateCoroutine != null || (ActivePlayer != null && ActivePlayer.isFinished))
                {
                    _settleCoroutine = null;
                    yield break;
                }

                // Check if any active marble is currently in motion
                bool anyMoving = false;
                for (int i = 0; i < _players.Count; i++)
                {
                    MarbleController m = _players[i].marble;
                    if (m != null && m.gameObject.activeInHierarchy && !m.IsRetired && m.IsMoving)
                    {
                        anyMoving = true;
                        break;
                    }
                }

                if (!anyMoving)
                {
                    settleTimer += Time.deltaTime;
                    if (settleTimer >= 0.20f)
                    {
                        // Stably rested!
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
                if (_players[i].marble != null && !_players[i].isFinished)
                {
                    _players[i].marble.Halt();
                }
            }

            if (_evaluateCoroutine != null || (ActivePlayer != null && ActivePlayer.isFinished))
            {
                _settleCoroutine = null;
                yield break;
            }

            _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
            _settleCoroutine = null;
        }

        private IEnumerator EvaluateTurnOutcomeRoutine()
        {
            SetState(GameState.Evaluating);

            // Let player savor the roll and stop
            yield return new WaitForSeconds(0.8f);

            // Deterministically audit all pits and marbles to ensure no marble is trapped or uncounted
            AuditPitsPostSettle();

            // 1. TOSS PHASE EVALUATION
            if (_tossResults != null && _tossResults.Count < _players.Count)
            {
                yield return StartCoroutine(EvaluateTossOutcomeRoutine());
                _evaluateCoroutine = null;
                yield break;
            }

            // 2. MAIN MATCH EVALUATION
            if (ActivePlayer == null || ActivePlayer.isFinished)
            {
                _evaluateCoroutine = null;
                yield break;
            }

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
                    ResetAllPits();

                    yield return new WaitForSeconds(0.4f);
                    _bonusStrikeEarned = false;
                    _hitOpponentMarbleThisTurn = false;
                    ActivateCurrentPlayer();
                    SetState(GameState.ReadyToAim);
                    OnStatusMessage?.Invoke($"★ EXTRA PLAY! AIM FOR PIT {ActivePlayer.currentPit} ★");
                }
                else
                {
                    // Player has conquered all 3 pits!
                    ActivePlayer.isFinished = true;
                    ActivePlayer.currentPit = 3;
                    OnTargetPitChanged?.Invoke(3);

                    if (!_placementPodium.Contains(ActivePlayer))
                    {
                        _placementPodium.Add(ActivePlayer);
                    }

                    int place = _placementPodium.Count;
                    string placeOrdinal = place == 1 ? "1st" : (place == 2 ? "2nd" : (place == 3 ? "3rd" : $"{place}th"));
                    Debug.Log($"<color=#FFD700><b>[PODIUM]</b> {ActivePlayer.name} takes {placeOrdinal} Place in {ActivePlayer.totalStrokes} strokes!</color>");

                    if (Audio.AudioManager.Instance != null)
                    {
                        Audio.AudioManager.Instance.PlayPitSink();
                    }

                    // Retire finished marble to the Winner's Showcase beside Pit 3:
                    // Place it safely along the right sideline (+3.5m on X) so it stands proud on the podium
                    // and cannot obstruct remaining players or trigger safety respawn!
                    if (ActivePlayer.marble != null)
                    {
                        ActivePlayer.marble.Halt();
                        Vector3 pit3Pos = new Vector3(0f, 0.25f, 31.0f);
                        PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
                        foreach (var p in allPits)
                        {
                            if (p.PitNumber == 3)
                            {
                                pit3Pos = p.transform.position;
                                break;
                            }
                        }
                        Vector3 podiumPos = pit3Pos + new Vector3(3.5f + (place - 1) * 1.1f, 0.25f, 0f);
                        ActivePlayer.marble.ResetPosition(podiumPos);
                        ActivePlayer.marble.IsRetired = true;
                        ActivePlayer.marble.SetVisible(true);
                        Rigidbody rb = ActivePlayer.marble.GetComponent<Rigidbody>();
                        if (rb != null)
                        {
                            rb.isKinematic = true;
                        }
                    }

                    // Immediately reset Pit 3 and all pits so subsequent players battling for 2nd / 3rd place can sink into Pit 3!
                    ResetAllPits();

                    // Count remaining active competitors
                    int remainingActive = 0;
                    foreach (var p in _players)
                    {
                        if (!p.isFinished) remainingActive++;
                    }

                    if (remainingActive <= 1)
                    {
                        // Tournament concluded! Award final placement to the last standing player
                        foreach (var p in _players)
                        {
                            if (!p.isFinished && !_placementPodium.Contains(p))
                            {
                                p.isFinished = true;
                                _placementPodium.Add(p);
                            }
                        }

                        OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} TAKES {placeOrdinal.ToUpper()}! TOURNAMENT COMPLETE! ★");
                        yield return new WaitForSeconds(1.2f);
                        DeclareMatchVictory();
                    }
                    else
                    {
                        int nextPlace = place + 1;
                        string nextOrdinal = nextPlace == 2 ? "2nd" : (nextPlace == 3 ? "3rd" : $"{nextPlace}th");
                        OnStatusMessage?.Invoke($"★ {ActivePlayer.name.ToUpper()} WINS {placeOrdinal.ToUpper()} PLACE! BATTLE FOR {nextOrdinal.ToUpper()}! ★");
                        Debug.Log($"<color=#00FFAA><b>[TURN ADVANCE]</b> Savoring {ActivePlayer.name}'s win; advancing turn in 1.8s...</color>");
                        yield return new WaitForSeconds(1.8f);
                        Debug.Log($"<color=#00FFAA><b>[TURN ADVANCE]</b> Calling AdvanceToNextActivePlayer() for {nextOrdinal} place...</color>");
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
                ActivateCurrentPlayer();
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
                    ActivateCurrentPlayer();
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
            bool isSunk = (pit3Zone != null && pit3Zone.IsSunk) || _pitSunkThisTurn;
            if (isSunk)
            {
                dist = 0.01f;
                // Place bullseye marble cleanly next to the rim so Pit 3 stays open for remaining tossers
                if (tossingPlayer.marble != null)
                {
                    tossingPlayer.marble.ResetPosition(pit3Pos + new Vector3(2.5f, 0.25f, 0f));
                }
                if (pit3Zone != null) pit3Zone.ResetPit();
            }
            _pitSunkThisTurn = false;

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
                        p.marble.IsRetired = false;
                        Rigidbody rb = p.marble.GetComponent<Rigidbody>();
                        if (rb != null) rb.isKinematic = false;
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

            // Ensure all pits are reset and ready for the next active player
            ResetAllPits();

            // Find next player who hasn't finished
            int attempts = 0;
            do
            {
                CurrentPlayerIndex = (CurrentPlayerIndex + 1) % _players.Count;
                attempts++;
            }
            while (ActivePlayer != null && ActivePlayer.isFinished && attempts <= _players.Count);

            if (ActivePlayer == null || ActivePlayer.isFinished)
            {
                DeclareMatchVictory();
                return;
            }

            ActivateCurrentPlayer();

            // Display battle objective for remaining places
            if (_placementPodium.Count > 0)
            {
                int targetPlace = _placementPodium.Count + 1;
                string targetOrdinal = targetPlace == 2 ? "2nd" : (targetPlace == 3 ? "3rd" : $"{targetPlace}th");
                OnStatusMessage?.Invoke($"BATTLE FOR {targetOrdinal.ToUpper()} PLACE • {ActivePlayer.name.ToUpper()}'s TURN • PIT {ActivePlayer.currentPit}");
            }

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

            // 1. Point Camera to active player's marble oriented towards active target pit
            SmoothFollowCamera cam = FindAnyObjectByType<SmoothFollowCamera>();
            if (cam != null && ActivePlayer.marble != null)
            {
                cam.SetTarget(ActivePlayer.marble.transform, GetPitPosition(ActivePlayer.currentPit));
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

            // Point Camera oriented directly towards Pit 3
            SmoothFollowCamera cam = FindAnyObjectByType<SmoothFollowCamera>();
            if (cam != null && p.marble != null)
            {
                cam.SetTarget(p.marble.transform, GetPitPosition(3));
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

            // Assemble leaderboard ordered strictly by podium finish (1st, 2nd, 3rd, 4th)
            List<PlayerData> ranked = new List<PlayerData>(_placementPodium);
            foreach (var p in _players)
            {
                if (!ranked.Contains(p)) ranked.Add(p);
            }

            PlayerData winner = ranked.Count > 0 ? ranked[0] : null;

            if (Audio.AudioManager.Instance != null)
            {
                Audio.AudioManager.Instance.PlayVictory();
            }

            OnMatchVictory?.Invoke(winner, ranked);

            string summary = "★ PODIUM: ";
            for (int i = 0; i < ranked.Count; i++)
            {
                string ord = i == 0 ? "1st" : (i == 1 ? "2nd" : (i == 2 ? "3rd" : $"{i + 1}th"));
                summary += $"{ord}: {ranked[i].name.ToUpper()}{(i < ranked.Count - 1 ? " | " : "")}";
            }
            summary += " ★";

            OnStatusMessage?.Invoke(summary);
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

        /// <summary>
        /// Retrieves the world position of the target pit. Falls back to default course layout coordinates if needed.
        /// </summary>
        public Vector3 GetPitPosition(int pitNumber)
        {
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            foreach (var p in allPits)
            {
                if (p.PitNumber == pitNumber)
                {
                    return p.transform.position;
                }
            }

            switch (pitNumber)
            {
                case 1: return new Vector3(0f, 0f, 3.0f);
                case 2: return new Vector3(0f, 0f, 16.5f);
                case 3: return new Vector3(0f, 0f, 31.0f);
                default: return new Vector3(0f, 0f, 31.0f);
            }
        }

        /// <summary>
        /// Resets the capture state of all pits across the course, opening them for the next player.
        /// </summary>
        public void ResetAllPits()
        {
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
            foreach (var p in allPits)
            {
                p.ResetPit();
            }
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
            _placementPodium.Clear();
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
