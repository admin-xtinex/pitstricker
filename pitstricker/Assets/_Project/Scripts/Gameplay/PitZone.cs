using System;
using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.Gameplay
{
    /// <summary>
    /// Represents a numbered pit in the arena (e.g. Pit 1, Pit 2, Pit 3).
    /// Detects when a marble enters and validates capture conditions.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PitZone : MonoBehaviour
    {
        [Header("Pit Identity")]
        [Tooltip("Pit index according to sequential progression (1, 2, or 3).")]
        [SerializeField] private int _pitNumber = 1;

        [Header("Capture Thresholds")]
        [Tooltip("Maximum velocity allowed for a marble to count as sunk (prevents skimming).")]
        [SerializeField] private float _maxCaptureSpeed = 1.2f;

        public int PitNumber => _pitNumber;

        // Events
        public static event Action<PitZone, MarbleController> OnMarbleSunk;

        private void Awake()
        {
            // Ensure collider is set to trigger
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble == null) return;

            // Check if marble is moving slow enough to drop into pit cup
            if (marble.CurrentSpeed <= _maxCaptureSpeed)
            {
                Debug.Log($"[PIT] Marble successfully SUNK into Pit #{_pitNumber}!");
                OnMarbleSunk?.Invoke(this, marble);
            }
        }
    }
}
