using System;
using System.Collections;
using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.Gameplay
{
    /// <summary>
    /// Phase 4 Turn & Match Orchestrator:
    /// Coordinates game state (Aiming -> Rolling -> Evaluating -> Victory),
    /// tracks strokes per pit & course total, evaluates sequential pit conquers,
    /// and manages match restarts.
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

        public static TurnManager Instance { get; private set; }

        [Header("Course Setup")]
        [Tooltip("Active marble being controlled.")]
        [SerializeField] private MarbleController _activeMarble;

        [Tooltip("Starting tee position for Pit 1 (Chalk Circle).")]
        [SerializeField] private Vector3 _startPosition = new Vector3(0f, 0.3f, -5.5f);

        [Header("Par Configuration")]
        [SerializeField] private int[] _pitPars = new int[] { 2, 3, 3 }; // Pit 1: Par 2, Pit 2: Par 3, Pit 3: Par 3

        // Game State
        public GameState CurrentState { get; private set; } = GameState.ReadyToAim;
        public int CurrentTargetPit { get; private set; } = 1;
        public int TotalStrokes { get; private set; } = 0;
        public int CurrentPitStrokes { get; private set; } = 0;
        public int CoursePar => _pitPars[0] + _pitPars[1] + _pitPars[2];
        public int CurrentPitPar => (_pitPars != null && CurrentTargetPit >= 1 && CurrentTargetPit <= _pitPars.Length) ? _pitPars[CurrentTargetPit - 1] : 3;

        // Pit Sunk Tracking
        private bool _currentPitSunkThisTurn = false;
        private Coroutine _evaluateCoroutine;

        // Events
        public static event Action<GameState> OnStateChanged;
        public static event Action<int, int> OnStrokeCountChanged; // (pitStrokes, totalStrokes)
        public static event Action<int> OnTargetPitChanged;         // (targetPit: 1, 2, 3)
        public static event Action<int, int, string> OnMatchWon;   // (totalStrokes, coursePar, scoreRating)
        public static event Action<string> OnStatusMessage;        // Dynamic banner notification

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

            if (_activeMarble == null)
            {
                _activeMarble = FindFirstObjectByType<MarbleController>();
            }
        }

        private void OnEnable()
        {
            if (_activeMarble != null)
            {
                _activeMarble.OnMarbleLaunched += HandleMarbleLaunched;
                _activeMarble.OnMarbleStopped += HandleMarbleStopped;
            }

            PitZone.OnMarbleSunk += HandleMarbleSunk;
        }

        private void OnDisable()
        {
            if (_activeMarble != null)
            {
                _activeMarble.OnMarbleLaunched -= HandleMarbleLaunched;
                _activeMarble.OnMarbleStopped -= HandleMarbleStopped;
            }

            PitZone.OnMarbleSunk -= HandleMarbleSunk;
        }

        private void Start()
        {
            if (_activeMarble == null)
            {
                _activeMarble = FindFirstObjectByType<MarbleController>();
                if (_activeMarble != null)
                {
                    _activeMarble.OnMarbleLaunched += HandleMarbleLaunched;
                    _activeMarble.OnMarbleStopped += HandleMarbleStopped;
                }
            }

            RestartMatch();
        }

        /// <summary>
        /// Called when the marble receives launch impulse.
        /// </summary>
        private void HandleMarbleLaunched()
        {
            if (CurrentState == GameState.MatchVictory) return;

            TotalStrokes++;
            CurrentPitStrokes++;
            _currentPitSunkThisTurn = false;

            SetState(GameState.Rolling);
            OnStrokeCountChanged?.Invoke(CurrentPitStrokes, TotalStrokes);
            OnStatusMessage?.Invoke($"STROKE {TotalStrokes} IN PLAY");
        }

        /// <summary>
        /// Called when the marble settles to rest.
        /// </summary>
        private void HandleMarbleStopped()
        {
            if (CurrentState != GameState.Rolling) return;

            if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
            _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
        }

        private IEnumerator EvaluateTurnOutcomeRoutine()
        {
            SetState(GameState.Evaluating);

            // Wait a brief moment for physics triggers & vortices to settle completely
            yield return new WaitForSeconds(0.4f);

            // Check if active target pit was conquered
            if (_currentPitSunkThisTurn)
            {
                Debug.Log($"<color=#00FF88><b>[TURN MANAGER]</b> Pit #{CurrentTargetPit} conquered in {CurrentPitStrokes} strokes!</color>");

                if (CurrentTargetPit < 3)
                {
                    CurrentTargetPit++;
                    CurrentPitStrokes = 0;
                    _currentPitSunkThisTurn = false;

                    OnTargetPitChanged?.Invoke(CurrentTargetPit);
                    OnStrokeCountChanged?.Invoke(CurrentPitStrokes, TotalStrokes);
                    OnStatusMessage?.Invoke($"★ PIT CONQUERED! PROCEED TO PIT {CurrentTargetPit}! ★");

                    // Reposition marble slightly ahead of the conquered pit for a clean tee shot to the next pit
                    yield return new WaitForSeconds(0.6f);
                    RelocateToNextTee(CurrentTargetPit);

                    SetState(GameState.ReadyToAim);
                }
                else
                {
                    // Match Victory! All 3 pits conquered!
                    SetState(GameState.MatchVictory);
                    string rating = CalculatePerformanceRating(TotalStrokes, CoursePar);
                    Debug.Log($"<color=#FFD700><b>[MATCH VICTORY]</b> Course Complete! Strokes: {TotalStrokes}, Par: {CoursePar}, Rating: {rating}</color>");
                    OnMatchWon?.Invoke(TotalStrokes, CoursePar, rating);
                    OnStatusMessage?.Invoke("★ VICTORY! ALL PITS CONQUERED! ★");
                }
            }
            else
            {
                // Marble came to rest on the fairway
                SetState(GameState.ReadyToAim);
                OnStatusMessage?.Invoke($"AIM FOR PIT {CurrentTargetPit} • STROKE {TotalStrokes + 1}");
            }

            _evaluateCoroutine = null;
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (marble != _activeMarble) return;

            if (pit.PitNumber == CurrentTargetPit)
            {
                _currentPitSunkThisTurn = true;
                marble.Halt();

                // Immediately trigger evaluation so player isn't stuck waiting
                if (_evaluateCoroutine != null) StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = StartCoroutine(EvaluateTurnOutcomeRoutine());
            }
            else
            {
                Debug.LogWarning($"[TURN MANAGER] Marble entered Pit #{pit.PitNumber}, but target is Pit #{CurrentTargetPit}!");
                OnStatusMessage?.Invoke($"WRONG PIT! TARGET IS PIT {CurrentTargetPit}");
            }
        }

        /// <summary>
        /// Relocates marble safely out of the completed pit onto the forward fairway.
        /// </summary>
        private void RelocateToNextTee(int nextPit)
        {
            if (_activeMarble == null) return;

            Vector3 nextTeePos;
            if (nextPit == 2)
            {
                // Ahead of Pit 1 (Pit 1 is at Z = 2.0m)
                nextTeePos = new Vector3(0f, 0.3f, 3.5f);
            }
            else if (nextPit == 3)
            {
                // Ahead of Pit 2 (Pit 2 is at Z = 13.0m)
                nextTeePos = new Vector3(0f, 0.3f, 14.5f);
            }
            else
            {
                nextTeePos = _startPosition;
            }

            _activeMarble.ResetPosition(nextTeePos);
            Debug.Log($"<color=#00FFAA><b>[TEE PLACEMENT]</b> Marble placed at {nextTeePos} ready to strike for Pit {nextPit}!</color>");
        }

        /// <summary>
        /// Resets the full match to initial conditions (Chalk Circle, Pit 1, 0 Strokes).
        /// </summary>
        public void RestartMatch()
        {
            if (_evaluateCoroutine != null)
            {
                StopCoroutine(_evaluateCoroutine);
                _evaluateCoroutine = null;
            }

            TotalStrokes = 0;
            CurrentPitStrokes = 0;
            CurrentTargetPit = 1;
            _currentPitSunkThisTurn = false;

            // Reset all pit capture states
            PitZone[] allPits = FindObjectsByType<PitZone>(FindObjectsSortMode.None);
            foreach (PitZone p in allPits)
            {
                p.ResetPit();
            }

            if (_activeMarble != null)
            {
                _activeMarble.ResetPosition(_startPosition);
            }

            SetState(GameState.ReadyToAim);

            OnTargetPitChanged?.Invoke(CurrentTargetPit);
            OnStrokeCountChanged?.Invoke(CurrentPitStrokes, TotalStrokes);
            OnStatusMessage?.Invoke("YOUR TURN • TARGET: PIT 1");

            Debug.Log("<color=#00FFAA><b>[MATCH RESTART]</b> Arena reset to opening break at Pit 1.</color>");
        }

        private void SetState(GameState newState)
        {
            CurrentState = newState;
            OnStateChanged?.Invoke(newState);
        }

        public static string CalculatePerformanceRating(int strokes, int par)
        {
            int diff = strokes - par;
            if (diff <= -3) return "ALBATROSS (-3)";
            if (diff == -2) return "EAGLE (-2)";
            if (diff == -1) return "BIRDIE (-1)";
            if (diff == 0) return "PAR (EVEN)";
            if (diff == 1) return "BOGEY (+1)";
            if (diff == 2) return "DOUBLE BOGEY (+2)";
            return $"+{diff} OVER PAR";
        }
    }
}
