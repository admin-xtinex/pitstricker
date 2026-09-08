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
    /// and tactical Vettu (attack) decisions.
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

        [Tooltip("Probability of choosing tactical Vettu attack over direct pit progression when an opponent is vulnerable.")]
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
            bool isVettuAttack = false;
            string targetDesc = "";

            if (isTossPhase)
            {
                // Toss Phase: Target Pit 3 directly
                targetPos = TurnManager.Instance.GetPitPosition(3);
                targetDesc = "Pit #3 (Opening Toss)";
            }
            else
            {
                // Main Match: Evaluate line of sight to target pit vs. tactical Vettu attack on opponent
                Vector3 pitPos = TurnManager.Instance.GetPitPosition(aiPlayer.currentPit);
                TurnManager.PlayerData bestTargetOpponent = EvaluateTacticalVettu(aiPlayer, pitPos);

                if (bestTargetOpponent != null && bestTargetOpponent.marble != null && UnityEngine.Random.value < _tacticalAggression)
                {
                    targetPos = bestTargetOpponent.marble.transform.position;
                    isVettuAttack = true;
                    targetDesc = $"Vettu Attack on {bestTargetOpponent.name}";
                    Debug.Log($"<color=#FFD700><b>[AI TACTICAL VETTU]</b> {aiPlayer.name} strategically chose to strike {bestTargetOpponent.name}!</color>");
                }
                else
                {
                    targetPos = pitPos;
                    isVettuAttack = false;
                    targetDesc = $"Target Pit #{aiPlayer.currentPit}";
                    Debug.Log($"<color=#00FFAA><b>[AI PIT SHOT]</b> {aiPlayer.name} advancing towards Pit #{aiPlayer.currentPit}.</color>");
                }
            }

            // 2. Trajectory & Ballistic Calculations
            Vector3 flatToTarget = Vector3.ProjectOnPlane(targetPos - marblePos, Vector3.up);
            float distance = flatToTarget.magnitude;
            Vector3 baseDir = distance > 0.05f ? flatToTarget.normalized : Vector3.forward;

            // Physics-calibrated force
            float calibratedForce = CalculateRequiredForce(distance, isTossPhase, isVettuAttack);

            // Natural humanized imperfection: small gaussian-like angular and force noise
            float angleOffset = UnityEngine.Random.Range(-_aimAngleVariance, _aimAngleVariance);
            Vector3 finalAimDir = Quaternion.Euler(0f, angleOffset, 0f) * baseDir;

            float forceErrorFrac = UnityEngine.Random.Range(-_forceVariance, _forceVariance);
            float finalForce = Mathf.Clamp(calibratedForce * (1f + forceErrorFrac), 3.0f, 38.0f);

            // 3. Animate Aim Guide Preview (showing trajectory line and charging power bar)
            float powerFraction = Mathf.Clamp01(finalForce / 36.8f);
            float elapsed = 0f;

            TurnManager.OnStatusMessage?.Invoke($"★ {aiPlayer.name.ToUpper()}: AIMING FOR {targetDesc.ToUpper()}... ★");

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

            // 4. Release & Fire Physical Impulse
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
        /// Evaluates all active opponents to find the highest-value tactical Vettu opportunity.
        /// An opponent is high-value if they are threatening a pit win or blocking the fairway lane.
        /// </summary>
        private TurnManager.PlayerData EvaluateTacticalVettu(TurnManager.PlayerData aiPlayer, Vector3 pitPos)
        {
            if (TurnManager.Instance == null || TurnManager.Instance.Players == null) return null;

            Vector3 aiPos = aiPlayer.marble.transform.position;
            TurnManager.PlayerData bestTarget = null;
            float highestScore = 0f;

            foreach (var opponent in TurnManager.Instance.Players)
            {
                if (opponent == aiPlayer || opponent.isFinished || opponent.marble == null || !opponent.marble.gameObject.activeInHierarchy || opponent.marble.IsRetired)
                {
                    continue;
                }

                Vector3 oppPos = opponent.marble.transform.position;
                float distToOpponent = Vector3.Distance(aiPos, oppPos);

                // Ignore opponents out of practical striking range
                if (distToOpponent < 1.0f || distToOpponent > 18.0f) continue;

                // Value 1: Opponent is close to their own target pit (threat to win or advance!)
                Vector3 oppTargetPit = TurnManager.Instance.GetPitPosition(opponent.currentPit);
                float oppDistToPit = Vector3.Distance(oppPos, oppTargetPit);
                float threatValue = Mathf.Clamp(6.0f - oppDistToPit, 0f, 6.0f) * 2.0f;

                // Value 2: Opponent is positioned directly along AI's path to its target pit (blocking shot)
                Vector3 toPit = (pitPos - aiPos).normalized;
                Vector3 toOpp = (oppPos - aiPos).normalized;
                float angleDegrees = Vector3.Angle(toPit, toOpp);
                float blockValue = (angleDegrees < 25f && distToOpponent < Vector3.Distance(aiPos, pitPos)) ? 6.0f : 0f;

                // Value 3: Closer opponents are easier to strike cleanly
                float proximityScore = Mathf.Clamp(18.0f - distToOpponent, 0f, 18.0f) * 0.4f;

                float totalScore = threatValue + blockValue + proximityScore;

                if (totalScore > highestScore && totalScore >= 7.0f)
                {
                    highestScore = totalScore;
                    bestTarget = opponent;
                }
            }

            return bestTarget;
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
                // Vettu attack applies +18% force to punch through target marble and maximize knockback
                baseForce *= 1.18f;
            }

            return Mathf.Clamp(baseForce, 3.0f, 38.0f);
        }
    }
}
