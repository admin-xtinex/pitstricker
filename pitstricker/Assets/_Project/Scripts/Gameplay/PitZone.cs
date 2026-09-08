using System;
using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.Gameplay
{
    /// <summary>
    /// Represents a numbered pit in the arena (e.g. Pit 1, Pit 2, Pit 3).
    /// Detects when a marble enters and assists in settling it into the cup.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PitZone : MonoBehaviour
    {
        [Header("Pit Identity")]
        [Tooltip("Pit index according to sequential progression (1, 2, or 3).")]
        [SerializeField] private int _pitNumber = 1;

        [Header("Capture Thresholds")]
        [Tooltip("Maximum velocity allowed for a marble to count as sunk (prevents skimming).")]
        [SerializeField] private float _maxCaptureSpeed = 2.0f;

        [Tooltip("Strength of the gravitational pull drawing the marble into the center of the pit.")]
        [SerializeField] private float _pitVortexForce = 8.0f;

        public int PitNumber => _pitNumber;
        public bool IsSunk { get; private set; }

        // Events
        public static event Action<PitZone, MarbleController> OnMarbleSunk;

        private void Awake()
        {
            // Only set SphereCollider as trigger, never touch MeshCollider
            SphereCollider sphereTrigger = GetComponent<SphereCollider>();
            if (sphereTrigger != null)
            {
                sphereTrigger.isTrigger = true;
            }
        }

        private void OnTriggerStay(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble == null) return;

            Rigidbody rb = marble.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply a gentle funnel pull toward the bottom-center of the pit
                Vector3 centerTarget = transform.position;
                Vector3 pullDir = centerTarget - marble.transform.position;
                pullDir.y = -0.5f; // Pull downwards into the basin

                rb.AddForce(pullDir * _pitVortexForce, ForceMode.Acceleration);

                // Dampen horizontal velocity so the marble settles naturally inside the cup
                Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
                rb.linearVelocity -= horizontalVel * (0.05f);
            }

            // Check if marble has settled in the pit below ground elevation
            if (!IsSunk && marble.transform.position.y < 0.1f && marble.CurrentSpeed <= _maxCaptureSpeed)
            {
                IsSunk = true;
                Debug.Log($"<color=#00FFAA><b>[GOAL!]</b> Marble SUNK into Pit #{_pitNumber}!</color>");
                OnMarbleSunk?.Invoke(this, marble);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            MarbleController marble = other.GetComponent<MarbleController>();
            if (marble != null)
            {
                IsSunk = false;
            }
        }
    }
}
