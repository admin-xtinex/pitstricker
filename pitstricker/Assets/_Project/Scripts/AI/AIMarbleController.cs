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
        [Tooltip("Standard angular inaccuracy in degrees to keep the bot natural and beatable (calibrated with reduced intelligence).")]
        [Range(0f, 8f)]
        [SerializeField] private float _aimAngleVariance = 2.34f; // Calibrated for natural organic aim spread

        [Tooltip("Force variance percentage (e.g. 0.10 = +/- 10% error).")]
        [Range(0f, 0.25f)]
        [SerializeField] private float _forceVariance = 0.10f; // Shot weight variation

        [Tooltip("Probability of choosing tactical Strike attack over direct pit progression when an opponent is vulnerable.")]
        [Range(0f, 1f)]
        [SerializeField] private float _tacticalAggression = 0.55f; // Increased strike aggression (+20% boost)

        [Tooltip("Awareness rate for detecting critical match-point denial threats.")]
        [Range(0f, 1f)]
        [SerializeField] private float _threatAwarenessRate = 0.56f; // Calibrated threat awareness (20% reduction)

        [Header("Timing")]
        [SerializeField] private float _thinkTimeSeconds = 1.0f;

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
        /// Resets active AI coroutines and hides aim preview immediately.
        /// </summary>
        public void ResetAI()
        {
            if (_aiTurnCoroutine != null)
            {
                StopCoroutine(_aiTurnCoroutine);
                _aiTurnCoroutine = null;
            }

            if (SwipeLaunchController.Instance != null)
            {
                SwipeLaunchController.Instance.HideAimPreview();
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

            if (TurnManager.Instance == null || TurnManager.Instance.CurrentState == TurnManager.GameState.MatchVictory || TurnManager.Instance.CurrentState == TurnManager.GameState.Menu)
            {
                _aiTurnCoroutine = null;
                yield break;
            }

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

                    float distToTargetPit = Vector3.Distance(marblePos, pitPos);
                    bool justConqueredPit = TurnManager.BotsThatConqueredPitLastTurn.Contains(aiPlayer.id);

                    // If bot just conquered a pit in the previous round, or it's a long shot (> 5.5m),
                    // enforce an approach layup to disable back-to-back 1-shot sinks.
                    if (justConqueredPit || distToTargetPit > 5.5f || aiPlayer.currentPit == 3)
                    {
                        if (distToTargetPit > 3.5f)
                        {
                            Vector3 approachOffset = (marblePos - pitPos).normalized * UnityEngine.Random.Range(1.8f, 2.8f);
                            targetPos = pitPos + approachOffset;
                            targetDesc = $"Pit #{aiPlayer.currentPit} (Approach Layup)";
                            Debug.Log($"<color=#FFD700><b>[AI APPROACH LAYUP]</b> {aiPlayer.name} playing strategic approach layup to Pit #{aiPlayer.currentPit} (dist: {distToTargetPit:F1}m, consecutive pit prevented).</color>");
                        }
                        else
                        {
                            targetDesc = $"Pit #{aiPlayer.currentPit} (Cup Sink Attempt)";
                            Debug.Log($"<color=#00FFAA><b>[AI CUP SINK]</b> {aiPlayer.name} within tap-in range ({distToTargetPit:F1}m) attempting cup sink!</color>");
                        }

                        if (justConqueredPit)
                        {
                            TurnManager.BotsThatConqueredPitLastTurn.Remove(aiPlayer.id);
                        }
                    }
                    else
                    {
                        targetDesc = $"Target Pit #{aiPlayer.currentPit}";
                        Debug.Log($"<color=#00FFAA><b>[AI PIT SHOT]</b> {aiPlayer.name} advancing towards Pit #{aiPlayer.currentPit}.</color>");
                    }

                    if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null)
                    {
                        PitStriker.CameraSystem.SmoothFollowCamera.Instance.SetTarget(aiPlayer.marble.transform, targetPos);
                    }
                }
            }

            // 2. Trajectory & Ballistic Calculations
            // Natural distance perception variance (+/- 7.5%, calibrated with 20% reduced intelligence)
            float perceivedDistNoise = 1f + UnityEngine.Random.Range(-0.075f, 0.075f);
            float distance = Vector3.Distance(marblePos, targetPos) * perceivedDistNoise;
            Vector3 baseDir = (targetPos - marblePos).normalized;

            // Physics-calibrated force
            float calibratedForce = CalculateRequiredForce(distance, isTossPhase, isStrikeAttack);

            // Natural organic aim variance based on distance and pit:
            float dynamicAngleVariance;
            if (isTossPhase)
            {
                dynamicAngleVariance = UnityEngine.Random.Range(3.0f, 5.4f);
            }
            else if (aiPlayer.currentPit == 3)
            {
                // Third pit championship range
                if (distance > 7.0f)
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(4.5f, 7.0f);
                }
                else if (distance > 3.5f)
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(3.4f, 5.0f);
                }
                else
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(2.2f, 3.4f);
                }
            }
            else
            {
                // Pits 1 and 2
                if (distance > 7.0f)
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(3.4f, 5.0f);
                }
                else if (distance > 3.5f)
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(2.0f, 3.4f);
                }
                else
                {
                    dynamicAngleVariance = UnityEngine.Random.Range(1.0f, 2.0f);
                }
            }

            // Apply +50% difficulty variance multiplier for bot on pit shots, while keeping attack strikes focused
            if (isStrikeAttack)
            {
                dynamicAngleVariance *= 0.55f;
            }
            else
            {
                dynamicAngleVariance *= GameDifficulty.BotVarianceMultiplier;
            }

            float angleOffset = UnityEngine.Random.Range(-dynamicAngleVariance, dynamicAngleVariance);
            Vector3 finalAimDir = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

            // Realistic shot weight variation (+50% bot spread on pit shots per difficulty config)
            float forceSpread = (distance > 7.0f ? 0.15f : 0.065f) * (isStrikeAttack ? 0.6f : GameDifficulty.BotVarianceMultiplier);
            float forceErrorFrac = UnityEngine.Random.Range(-forceSpread, forceSpread);
            float maxForceClamp = isTossPhase ? 26.0f : (isStrikeAttack ? 36.0f : 24.0f);
            float finalForce = Mathf.Clamp(calibratedForce * (1f + forceErrorFrac), 1.0f, maxForceClamp);

            // 3. Animate Aim Guide Preview (showing trajectory line and charging power bar)
            float powerFraction = Mathf.Clamp01(finalForce / (isStrikeAttack ? 36.0f : 24.0f));
            float elapsed = 0f;

            TurnManager.BroadcastStatus($"★ {aiPlayer.name.ToUpper()}: AIMING FOR {targetDesc.ToUpper()}... ★");

            while (elapsed < _thinkTimeSeconds)
            {
                if (TurnManager.Instance == null || TurnManager.Instance.CurrentState == TurnManager.GameState.MatchVictory || TurnManager.Instance.CurrentState == TurnManager.GameState.Menu)
                {
                    if (SwipeLaunchController.Instance != null) SwipeLaunchController.Instance.HideAimPreview();
                    _aiTurnCoroutine = null;
                    yield break;
                }

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

            if (TurnManager.Instance == null || TurnManager.Instance.CurrentState == TurnManager.GameState.MatchVictory || TurnManager.Instance.CurrentState == TurnManager.GameState.Menu)
            {
                _aiTurnCoroutine = null;
                yield break;
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
        /// Calibrated with 30% reduced intelligence to make bot gameplay beatable and natural.
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
                bool isHumanPlayer = !opponent.isAI || opponent.id == 0;
                float playerAggressionMultiplier = isHumanPlayer ? 1.50f : 1.0f;

                // =========================================================================
                // ADAPTIVE BEHAVIOR BASED ON PLAYER STATUS:
                // If human player is closer to winning, bot adapts by aggressively attacking the player!
                // =========================================================================
                if (isHumanPlayer)
                {
                    bool isPlayerCloserToWin =
                        (opponent.currentPit == 3) ||                                          // Player on match point pit
                        (opponent.currentPit > aiPlayer.currentPit) ||                         // Player leading in pits
                        (opponent.currentPit == aiPlayer.currentPit && oppDistToPit < aiDistToPit) || // Same pit, but player closer to hole
                        (oppDistToPit <= 5.5f);                                                // Player within direct scoring range

                    if (isPlayerCloserToWin && distToOpponent <= 32.0f)
                    {
                        // 92% adaptive response: prioritize attacking the player to blast them away and deny win
                        if (UnityEngine.Random.value < 0.92f)
                        {
                            isCriticalDenial = true;
                            Debug.Log($"<color=#FF0044><b>[AI ADAPTIVE ATTACK]</b> Player {opponent.name} is closer to winning (Pit {opponent.currentPit}, dist {oppDistToPit:F1}m vs AI Pit {aiPlayer.currentPit}, dist {aiDistToPit:F1}m)! AI adapting tactics to aggressively attack player marble!</color>");
                            return opponent;
                        }
                    }
                }

                // =========================================================================
                // CRITICAL CONDITION 1: OPPONENT IS ON MATCH POINT (PIT 3) & IN SCORING RANGE
                // =========================================================================
                if (opponent.currentPit == 3 && oppDistToPit <= 6.5f && distToOpponent <= 18.0f)
                {
                    if (UnityEngine.Random.value < (_threatAwarenessRate * playerAggressionMultiplier))
                    {
                        isCriticalDenial = true;
                        return opponent;
                    }
                }

                // =========================================================================
                // CRITICAL CONDITION 2: OPPONENT IS LEADING AND ABOUT TO CONQUER THEIR PIT
                // =========================================================================
                if (opponent.currentPit > aiPlayer.currentPit && oppDistToPit <= 4.0f && distToOpponent <= 14.0f)
                {
                    if (UnityEngine.Random.value < (_threatAwarenessRate * playerAggressionMultiplier))
                    {
                        isCriticalDenial = true;
                        return opponent;
                    }
                }

                // =========================================================================
                // CRITICAL CONDITION 3: OPPONENT IS IN IMMEDIATE TAP-IN RANGE (<= 2.5m)
                // =========================================================================
                if (oppDistToPit <= 2.5f && aiDistToPit > 4.0f && distToOpponent <= 12.0f)
                {
                    if (UnityEngine.Random.value < (_threatAwarenessRate * playerAggressionMultiplier))
                    {
                        isCriticalDenial = true;
                        return opponent;
                    }
                }

                // =========================================================================
                // GENERAL TACTICAL EVALUATION (Aggressive Strike targeting on player marble)
                // =========================================================================
                // Threat score based on opponent proximity to their pit
                float threatScore = Mathf.Clamp(6.0f - oppDistToPit, 0f, 6.0f) * 1.5f;

                // Path blocking score: opponent is directly between AI and AI's target pit
                Vector3 toPit = (aiPitPos - aiPos).normalized;
                Vector3 toOpp = (oppPos - aiPos).normalized;
                float angle = Vector3.Angle(toPit, toOpp);
                float blockScore = (angle < 22f && distToOpponent < aiDistToPit) ? 7.0f : 0f;

                // Strike viability based on distance (closer opponents are easier to strike cleanly)
                float proximityScore = Mathf.Clamp(16.0f - distToOpponent, 0f, 16.0f) * 0.35f;

                // Extra aggression bonus targeting human player marble (+20% strike priority)
                float playerTargetBonus = isHumanPlayer ? 3.5f : 0f;

                float totalScore = threatScore + blockScore + proximityScore + playerTargetBonus;

                if (totalScore > highestTacticalScore)
                {
                    highestTacticalScore = totalScore;
                    bestTacticalTarget = opponent;
                }
            }

            // Tactical strike threshold with boosted aggression
            if (bestTacticalTarget != null && highestTacticalScore >= 6.5f && UnityEngine.Random.value < _tacticalAggression)
            {
                return bestTacticalTarget;
            }

            return null;
        }

        /// <summary>
        /// Calculates the exact physical impulse required to roll across a given distance
        /// and settle inside the target cup, based on the marble's linear damping and stopping friction.
        /// Calibrated for 1.0kg marble with PM_Sand_Friction (mu = 0.425) and 0.3 linear drag.
        /// </summary>
        public static float CalculateRequiredForce(float distance, bool isToss, bool isAttack)
        {
            if (distance <= 0.4f) return 1.0f;

            if (isToss)
            {
                // Opening Toss upward lob to Pit 3 across the 37m fairway
                return Mathf.Clamp(distance * 0.58f + 1.6f, 22.5f, 24.2f);
            }

            if (isAttack)
            {
                // Tactical strike applies doubled physical impulse for explosive kinetic displacement
                return Mathf.Clamp((distance * 0.48f + 3.0f) * 2.70f, 10.0f, 36.0f);
            }

            // Direct Pit shot: Calibrated formula F = distance * 0.43f + 1.95f
            // Ensures marble reaches pit basin with enough speed to climb bevel and settle in cup
            return Mathf.Clamp(distance * 0.43f + 1.95f, 1.5f, 22.0f);
        }
    }
}
