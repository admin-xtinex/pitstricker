using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PitStriker.Physics;
using PitStriker.Gameplay;
using PitStriker.Input;

namespace PitStriker.AI
{
    /// <summary>
    /// Autonomous AI Marble Controller:
    /// Drives opponent marbles with human-like strategic decision making,
    /// natural aim trajectory preview, physics-calibrated distance-to-force calculation,
    /// and tactical strike decisions.
    /// </summary>
    public class AIMarbleController : MonoBehaviour
    {
        public static AIMarbleController Instance { get; private set; }

        [Header("AI Skill & Natural Imperfections")]
        [Tooltip("Standard angular inaccuracy in degrees to keep the bot natural and beatable.")]
        [Range(0f, 5f)]
        [SerializeField] private float _aimAngleVariance = 1.8f;

        [Tooltip("Force variance percentage (e.g. 0.05 = +/- 5% error).")]
        [Range(0f, 0.15f)]
        [SerializeField] private float _forceVariance = 0.04f;

        [Tooltip("Probability of choosing tactical Strike attack over direct pit progression when an opponent is vulnerable.")]
        [Range(0f, 1f)]
        [SerializeField] private float _tacticalAggression = 0.70f;

        [Header("Timing")]
        [SerializeField] private float _thinkTimeSeconds = 1.2f;

        private Coroutine _aiTurnCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Orchestrates the AI turn: delays for camera framing, evaluates tactical targets,
        /// animates visual aiming guide, and executes the physical impulse.
        /// </summary>
        public void TakeAITurn(TurnManager.PlayerData aiPlayer)
        {
            if (aiPlayer == null || aiPlayer.marble == null) return;

            if (_aiTurnCoroutine != null)
            {
                StopCoroutine(_aiTurnCoroutine);
            }
            _aiTurnCoroutine = StartCoroutine(AITurnRoutine(aiPlayer));
        }

        private IEnumerator AITurnRoutine(TurnManager.PlayerData aiPlayer)
        {
            // 1. Brief pause to allow camera to smoothly frame the AI marble
            yield return new WaitForSeconds(0.4f);

            if (TurnManager.Instance == null) yield break;

            bool isTossPhase = (TurnManager.Instance.CurrentState == TurnManager.GameState.TossPhase);
            Vector3 marblePos = aiPlayer.marble.transform.position;
            Vector3 targetPos;
            bool isStrikeAttack = false;
            string targetDesc = "";

            if (isTossPhase)
            {
                // Toss Phase: Target Pit 3 directly
                targetPos = TurnManager.Instance.GetPitPosition(3);
                targetDesc = "Pit #3 (Opening Toss)";
            }
            else
            {
                // Main Match: Evaluate line of sight to target pit vs. tactical/defensive strike on opponent
                Vector3 pitPos = TurnManager.Instance.GetPitPosition(aiPlayer.currentPit);
                TurnManager.PlayerData bestTargetOpponent = EvaluateTacticalStrike(aiPlayer, pitPos, out bool isCriticalDenial);

                if (bestTargetOpponent != null && bestTargetOpponent.marble != null)
                {
                    targetPos = bestTargetOpponent.marble.transform.position;
                    isStrikeAttack = true;

                    if (isCriticalDenial)
                    {
                        targetDesc = $"DEFENSIVE DENIAL STRIKE on {bestTargetOpponent.name}!";
                        Debug.Log($"<color=#FF0044><b>[AI DEFENSIVE DENIAL STRIKE]</b> {aiPlayer.name} detected {bestTargetOpponent.name} threatening to win/advance! Executing decisive strike to blast them away!</color>");
                    }
                    else
                    {
                        targetDesc = $"Tactical Strike on {bestTargetOpponent.name}";
                        Debug.Log($"<color=#FFD700><b>[AI TACTICAL STRIKE]</b> {aiPlayer.name} strategically chose to strike {bestTargetOpponent.name}!</color>");
                    }

                    // Dynamically orient camera towards the targeted opponent!
                    if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null)
                    {
                        PitStriker.CameraSystem.SmoothFollowCamera.Instance.SetTarget(aiPlayer.marble.transform, targetPos);
                    }
                }
                else
                {
                    targetPos = pitPos;
                    isStrikeAttack = false;
                    targetDesc = $"Target Pit #{aiPlayer.currentPit}";
                    Debug.Log($"<color=#00FFAA><b>[AI PIT SHOT]</b> {aiPlayer.name} advancing towards Pit #{aiPlayer.currentPit}.</color>");

                    if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null)
                    {
                        PitStriker.CameraSystem.SmoothFollowCamera.Instance.SetTarget(aiPlayer.marble.transform, targetPos);
                    }
                }
            }

            // 2. Trajectory & Ballistic Calculations
            float distance = Vector3.Distance(marblePos, targetPos);
            Vector3 baseDir = (targetPos - marblePos).normalized;

            // Physics-calibrated force
            float calibratedForce = CalculateRequiredForce(distance, isTossPhase, isStrikeAttack);

            // Natural humanized imperfection: reduce noise for critical match-saving denial strikes
            float angleVariance = isStrikeAttack ? (_aimAngleVariance * 0.5f) : _aimAngleVariance;
            float angleOffset = UnityEngine.Random.Range(-angleVariance, angleVariance);
            Vector3 finalAimDir = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

            float forceErrorFrac = UnityEngine.Random.Range(-_forceVariance, _forceVariance);
            float finalForce = Mathf.Clamp(calibratedForce * (1f + forceErrorFrac), 3.0f, 38.0f);

            // 3. Animate Aim Guide Preview (showing trajectory line and charging power bar)
            float powerFraction = Mathf.Clamp01(finalForce / 36.8f);
            float elapsed = 0f;

            TurnManager.BroadcastStatus($"★ {aiPlayer.name.ToUpper()}: AIMING FOR {targetDesc.ToUpper()}... ★");

            while (elapsed < _thinkTimeSeconds)
            {
                elapsed += Time.deltaTime;
                float currentT = Mathf.Clamp01(elapsed / _thinkTimeSeconds);

                if (SwipeLaunchController.Instance != null)
                {
                    SwipeLaunchController.Instance.ShowAimPreview(finalAimDir, powerFraction * currentT);
                }

                yield return null;
            }

            // 4. Fire impulse physically
            if (SwipeLaunchController.Instance != null)
            {
                SwipeLaunchController.Instance.HideAimPreview();
            }

            aiPlayer.marble.Halt();

            if (isTossPhase)
            {
                Vector3 tossDir = (finalAimDir + Vector3.up * 0.08f).normalized;
                aiPlayer.marble.ApplyImpulse(tossDir, finalForce);
                Debug.Log($"<color=#00FFAA><b>[AI FLICK TOSS]</b> {aiPlayer.name} executed opening toss towards Pit #3 with {finalForce:F1} N force.</color>");
            }
            else
            {
                aiPlayer.marble.ApplyImpulse(finalAimDir, finalForce);
                Debug.Log($"<color=#00FFAA><b>[AI STRIKE]</b> {aiPlayer.name} executed shot towards {targetDesc} with {finalForce:F1} N force.</color>");
            }

            _aiTurnCoroutine = null;
        }

        /// <summary>
        /// Evaluates all active opponents to find critical victory-denial threats or high-value tactical opportunities.
        /// Prioritizes stopping opponents who are on match point (Pit 3), leading in pit count, or within sinking range.
        /// </summary>
        private TurnManager.PlayerData EvaluateTacticalStrike(TurnManager.PlayerData aiPlayer, Vector3 aiPitPos, out bool isCriticalDenial)
        {
            isCriticalDenial = false;
            if (TurnManager.Instance == null || TurnManager.Instance.Players == null) return null;

            Vector3 aiPos = aiPlayer.marble.transform.position;
            float aiDistToPit = Vector3.Distance(aiPos, aiPitPos);

            TurnManager.PlayerData bestTacticalTarget = null;
            float highestTacticalScore = 0f;

            foreach (var opponent in TurnManager.Instance.Players)
            {
                if (opponent == aiPlayer || opponent.isFinished || opponent.marble == null || !opponent.marble.gameObject.activeInHierarchy || opponent.marble.IsRetired)
                {
                    continue;
                }

                Vector3 oppPos = opponent.marble.transform.position;
                float distToOpponent = Vector3.Distance(aiPos, oppPos);

                // Range check: allow shots up to 34m across the arena fairway
                if (distToOpponent < 0.6f || distToOpponent > 34.0f) continue;

                Vector3 oppTargetPit = TurnManager.Instance.GetPitPosition(opponent.currentPit);
                float oppDistToPit = Vector3.Distance(oppPos, oppTargetPit);

                // =========================================================================
                // CRITICAL CONDITION 1: OPPONENT IS ON MATCH POINT (PIT 3) & IN SCORING RANGE
                // =========================================================================
                // If opponent is at Pit 3 and within 14 meters of sinking, they can win next turn!
                // AI MUST prioritize striking them away, regardless of AI's own progression.
                if (opponent.currentPit == 3 && oppDistToPit <= 14.0f)
                {
                    isCriticalDenial = true;
                    return opponent; // Immediate mandatory denial strike!
                }

                // =========================================================================
                // CRITICAL CONDITION 2: OPPONENT IS LEADING AND ABOUT TO CONQUER THEIR PIT
                // =========================================================================
                // Opponent has cleared more pits (e.g. on Pit 2 while AI is on Pit 1) and is near their pit
                if (opponent.currentPit > aiPlayer.currentPit && oppDistToPit <= 7.0f)
                {
                    isCriticalDenial = true;
                    return opponent; // Immediate mandatory denial strike!
                }

                // =========================================================================
                // CRITICAL CONDITION 3: OPPONENT IS IN EASY TAP-IN RANGE (< 4.5m)
                // =========================================================================
                // E.g. Opponent is 2m from pit, AI is 8m from pit. Sinking takes priority for opponent.
                if (oppDistToPit <= 4.5f && aiDistToPit > 3.0f)
                {
                    isCriticalDenial = true;
                    return opponent;
                }

                // =========================================================================
                // GENERAL TACTICAL EVALUATION (Secondary)
                // =========================================================================
                // Threat score based on opponent proximity to their pit
                float threatScore = Mathf.Clamp(10.0f - oppDistToPit, 0f, 10.0f) * 1.5f;

                // Path blocking score
                Vector3 toPit = (aiPitPos - aiPos).normalized;
                Vector3 toOpp = (oppPos - aiPos).normalized;
                float angle = Vector3.Angle(toPit, toOpp);
                float blockScore = (angle < 28f && distToOpponent < aiDistToPit) ? 8.0f : 0f;

                // Strike viability based on distance (closer opponents are easier to strike cleanly)
                float proximityScore = Mathf.Clamp(24.0f - distToOpponent, 0f, 24.0f) * 0.4f;

                float totalScore = threatScore + blockScore + proximityScore;

                if (totalScore > highestTacticalScore)
                {
                    highestTacticalScore = totalScore;
                    bestTacticalTarget = opponent;
                }
            }

            // If a tactical target scored sufficiently high and AI aggression roll succeeds:
            if (bestTacticalTarget != null && highestTacticalScore >= 5.0f && UnityEngine.Random.value < _tacticalAggression)
            {
                return bestTacticalTarget;
            }

            return null;
        }

        /// <summary>
        /// Calculates the exact physical impulse required to roll across a given distance
        /// and settle inside the target cup, based on the marble's linear damping and stopping friction.
        /// </summary>
        public static float CalculateRequiredForce(float distance, bool isToss, bool isAttack)
        {
            if (distance <= 0.5f) return 2.5f;

            // Physics regression model calibrated to 0.05kg mass and 0.12 linear damping
            float baseForce = Mathf.Sqrt(distance) * 5.65f + 1.2f;

            if (isToss)
            {
                // Toss phase lob to Pit 3 benefits from slightly higher speed for the upward flick
                baseForce = Mathf.Max(baseForce, 35.0f);
            }
            else if (isAttack)
            {
                // Strike attack applies +25% force to punch through target marble and maximize knockback displacement
                baseForce = Mathf.Clamp(baseForce * 1.25f, 12.0f, 38.0f);
            }

            return Mathf.Clamp(baseForce, 3.0f, 38.0f);
        }
    }
}
