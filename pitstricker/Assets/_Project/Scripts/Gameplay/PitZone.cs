using System;
using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.Gameplay
{
    /// <summary>
    /// Represents a numbered pit in the arena (e.g. Pit 1, Pit 2, Pit 3).
    /// Detects when a marble legitimately enters and settles inside the cup.
    /// Real marble pits are shallow and natural: fast marbles roll right over or lip out;
    /// only properly paced, gentle marbles that settle inside the basin are captured.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PitZone : MonoBehaviour
    {
        [Header("Pit Identity")]
        [Tooltip("Pit index according to sequential progression (1, 2, or 3).")]
        [SerializeField] private int _pitNumber = 1;

        [Header("Capture Thresholds")]
        [Tooltip("Maximum velocity allowed for a marble to count as sunk. Prevents skimming or rolling through.")]
        [SerializeField] private float _maxCaptureSpeed = 0.90f;

        [SerializeField, Min(0.1f)] private float _sizeScale = 1f;
        public int PitNumber => _pitNumber;
        public bool IsSunk { get; private set; }

        private MarbleController _capturedMarble = null;

        public void SetPitNumber(int number)
        {
            _pitNumber = number;
        }

        // Events
        public static event Action<PitZone, MarbleController> OnMarbleSunk;

        private void Awake()
        {
            // Auto-detect pit number from GameObject name if uninitialized or mismatch
            if (gameObject.name.Contains("2") || gameObject.name.Contains("02"))
            {
                _pitNumber = 2;
            }
            else if (gameObject.name.Contains("3") || gameObject.name.Contains("03"))
            {
                _pitNumber = 3;
            }
            else if (gameObject.name.Contains("1") || gameObject.name.Contains("01"))
            {
                _pitNumber = 1;
            }

            // Submerged spherical trigger covers the full basin and approach (1.50m radius)
            SphereCollider sphereTrigger = GetComponent<SphereCollider>();
            if (sphereTrigger != null)
            {
                sphereTrigger.isTrigger = true;
                sphereTrigger.radius = 1.50f * _sizeScale;
                sphereTrigger.center = Vector3.zero;
            }
        }

        private void Start()
        {
            Transform ceramicRim = transform.Find("Stylized_Ceramic_Rim");
            if (ceramicRim != null) Destroy(ceramicRim.gameObject);
            Transform badge = transform.Find("PitNumber_Badge");
            if (badge != null) Destroy(badge.gameObject);

            if (GetComponent<PitStriker.Visuals.StylizedPitFeedback>() == null)
            {
                gameObject.AddComponent<PitStriker.Visuals.StylizedPitFeedback>();
            }
        }

        /// <summary>
        /// Deterministically evaluates whether a marble is physically nestled inside this shallow earthen pit basin.
        /// Calibrated for realistic shallow pit depth (-0.08m) and 0.52m saucer rim radius.
        /// </summary>
        public bool IsMarbleInsidePit(MarbleController marble)
        {
            if (marble == null || marble.IsRetired) return false;

            Vector2 marbleXZ = new Vector2(marble.transform.position.x, marble.transform.position.z);
            Vector2 pitXZ = new Vector2(transform.position.x, transform.position.z);
            float horizontalDist = Vector2.Distance(marbleXZ, pitXZ);
            float relativeY = marble.transform.position.y - transform.position.y;

            // In shallow saucer pits (depth -0.08m, rim radius 0.52m), a resting marble center sits at relativeY ~= 0.17m.
            // When resting on the surrounding flat road, relativeY is 0.25m.
            float marbleRadius = marble.WorldRadius;
            float catchRadius = 0.50f * _sizeScale * GameDifficulty.PitCatchRadiusMultiplier - marbleRadius * 0.25f;
            return horizontalDist <= catchRadius && relativeY < marbleRadius * 0.85f;
        }

        /// <summary>
        /// Explicitly marks this pit as sunk by the given marble, firing audio, VFX, and game events.
        /// </summary>
        public void MarkSunk(MarbleController marble, bool fireEvent = true)
        {
            if (marble == null || marble.IsRetired) return;

            _capturedMarble = marble;
            IsSunk = true;
            marble.Halt();

            // Audio & VFX Juice
            if (PitStriker.Audio.AudioManager.Instance != null)
            {
                PitStriker.Audio.AudioManager.Instance.PlayPitSink();
            }
            if (PitStriker.VFX.VFXManager.Instance != null)
            {
                PitStriker.VFX.VFXManager.Instance.PlayPitCelebration(transform.position);
            }

            Debug.Log($"<color=#00FFAA><b>[GOAL!]</b> {marble.name} settled and SUNK into Pit #{_pitNumber}!</color>");
            if (fireEvent)
            {
                OnMarbleSunk?.Invoke(this, marble);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble == null || marble.IsRetired) return;

            if (IsMarbleInsidePit(marble))
            {
                // Settled speed check: Marble must have settled or slowed down inside the pit.
                float effectiveMaxSpeed = Mathf.Min(_maxCaptureSpeed, GameDifficulty.MaxCaptureSpeed);
                bool isSettled = marble.CurrentSpeed <= effectiveMaxSpeed;
                if (!isSettled) return;

                // If this marble was already captured and registered, keep it settled
                if (marble == _capturedMarble)
                {
                    marble.Halt();
                    return;
                }

                // New marble settled in pit! Register capture
                MarkSunk(marble);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble != null && marble == _capturedMarble)
            {
                IsSunk = false;
                _capturedMarble = null;
            }
        }

        /// <summary>
        /// Resets the pit capture flag and captured marble reference, opening the pit for new shots.
        /// </summary>
        public void ResetPit()
        {
            IsSunk = false;
            _capturedMarble = null;
        }
    }
}
