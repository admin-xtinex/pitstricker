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
        [SerializeField] private float _maxCaptureSpeed = 0.65f;

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

            // Realistic trigger zone: covers the full cup interior below ground level.
            // Center is submerged so it NEVER touches marbles rolling on the flat surface outside the pit rim!
            SphereCollider sphereTrigger = GetComponent<SphereCollider>();
            if (sphereTrigger != null)
            {
                sphereTrigger.isTrigger = true;
                sphereTrigger.radius = 0.65f;
                sphereTrigger.center = new Vector3(0f, -0.15f, 0f);
            }
        }

        private void OnTriggerStay(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble == null) return;

            // If this marble was already captured and sunk in this pit, keep it settled
            if (IsSunk && marble == _capturedMarble)
            {
                marble.Halt();
                return;
            }

            // 1. Elevation check: Marble center must be down inside the pit depression below the flat fairway
            // On flat ground, marble center is at y ~ 0.25m. In the pit cup, center is <= 0.16m.
            float relativeY = marble.transform.position.y - transform.position.y;
            bool isDownInPit = relativeY < 0.18f;

            // 2. Horizontal containment: must be within the pit rim radius
            Vector2 marbleXZ = new Vector2(marble.transform.position.x, marble.transform.position.z);
            Vector2 pitXZ = new Vector2(transform.position.x, transform.position.z);
            float horizontalDist = Vector2.Distance(marbleXZ, pitXZ);
            bool isInsidePitHole = horizontalDist < 0.65f;

            // 3. Settled speed check: Marble must have settled or slowed down inside the pit.
            // Fast skimming marbles (> 0.65 m/s) will naturally roll through or lip out.
            bool isSettled = marble.CurrentSpeed <= _maxCaptureSpeed;

            if (!IsSunk && isDownInPit && isInsidePitHole && isSettled)
            {
                IsSunk = true;
                _capturedMarble = marble;
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
                OnMarbleSunk?.Invoke(this, marble);
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
