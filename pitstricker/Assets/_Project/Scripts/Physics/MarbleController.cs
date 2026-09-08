using System;
using UnityEngine;

namespace PitStriker.Physics
{
    /// <summary>
    /// Controls the physical properties, movement, and velocity states of a marble.
    /// Follows the Pit Striker Physics Design Specification.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SphereCollider))]
    public class MarbleController : MonoBehaviour
    {
        [Header("Physical Constraints")]
        [Tooltip("Standard mass for the marble in kilograms.")]
        [SerializeField] private float _mass = 1.0f;

        [Tooltip("Linear drag simulating ground and rolling resistance.")]
        [SerializeField] private float _linearDrag = 0.3f;

        [Tooltip("Angular drag preventing infinite ice-rink rolling.")]
        [SerializeField] private float _angularDrag = 0.8f;

        [Tooltip("Speed below which the marble is considered completely stopped.")]
        [SerializeField] private float _stopThreshold = 0.05f;

        // Cached Components
        private Rigidbody _rigidbody;
        private SphereCollider _collider;

        // State Tracking
        private bool _wasMoving = false;

        // Events
        public event Action OnMarbleLaunched;
        public event Action OnMarbleStopped;
        public event Action<Collision> OnMarbleCollision;
        public static event Action<MarbleController, MarbleController> OnMarbleHitMarble; // (striker, hitTarget)

        public bool IsMoving => _rigidbody != null && _rigidbody.linearVelocity.sqrMagnitude > (_stopThreshold * _stopThreshold);
        public Vector3 Velocity => _rigidbody != null ? _rigidbody.linearVelocity : Vector3.zero;
        public float CurrentSpeed => _rigidbody != null ? _rigidbody.linearVelocity.magnitude : 0f;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<SphereCollider>();

            ConfigurePhysicsDefaults();
        }

        /// <summary>
        /// Applies studio physics standards to ensure smooth rolling and prevent tunneling.
        /// </summary>
        private void ConfigurePhysicsDefaults()
        {
            _rigidbody.mass = _mass;
            _rigidbody.linearDamping = _linearDrag;
            _rigidbody.angularDamping = _angularDrag;
            _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        /// <summary>
        /// Launches the marble with a single physical impulse along a specified vector.
        /// Must only be called when authorized by input or turn controllers.
        /// </summary>
        /// <param name="direction">Normalized ground direction vector (Y should be 0).</param>
        /// <param name="forceMagnitude">Impulse strength to apply.</param>
        public void ApplyImpulse(Vector3 direction, float forceMagnitude)
        {
            if (_rigidbody == null) return;

            // Flatten direction to ground plane to prevent launching vertically
            direction.y = 0f;
            Vector3 normalizedDir = direction.normalized;

            // Apply physical impulse
            _rigidbody.AddForce(normalizedDir * forceMagnitude, ForceMode.Impulse);
            _wasMoving = true;

            OnMarbleLaunched?.Invoke();

            // Audio & VFX Juice
            if (PitStriker.Audio.AudioManager.Instance != null)
            {
                PitStriker.Audio.AudioManager.Instance.PlayLaunch(Mathf.Clamp01(forceMagnitude / 32f));
            }
            if (PitStriker.VFX.VFXManager.Instance != null)
            {
                PitStriker.VFX.VFXManager.Instance.PlayLaunchDust(transform.position, normalizedDir, Mathf.Clamp01(forceMagnitude / 32f));
            }

            Debug.Log($"[MARBLE] Launched along {normalizedDir} with force {forceMagnitude:F1} N.");
        }

        private void FixedUpdate()
        {
            if (_rigidbody == null) return;

            bool currentlyMoving = IsMoving;

            // Detect when marble transitions from moving to completely at rest
            if (_wasMoving && !currentlyMoving)
            {
                // Force velocity cleanly to zero once under threshold to prevent micro-drifting
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _wasMoving = false;

                OnMarbleStopped?.Invoke();
                Debug.Log("[MARBLE] Marble has come to a complete stop.");
            }
            else if (currentlyMoving)
            {
                _wasMoving = true;
            }

            // Safety catch: If marble ever drops into the void below the track, recover it
            if (transform.position.y < -2.0f)
            {
                ResetPosition(new Vector3(0f, 0.3f, -5.5f));
                Debug.LogWarning("<color=#FFAA00><b>[SAFETY RESPAWN]</b> Marble recovered from void and placed safely at launch baseline.</color>");
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            OnMarbleCollision?.Invoke(collision);

            MarbleController otherMarble = collision.collider.GetComponent<MarbleController>();
            bool isMarble = otherMarble != null;
            if (isMarble)
            {
                OnMarbleHitMarble?.Invoke(this, otherMarble);
            }

            float speed = collision.relativeVelocity.magnitude;
            if (speed > 0.4f)
            {
                if (PitStriker.Audio.AudioManager.Instance != null)
                {
                    PitStriker.Audio.AudioManager.Instance.PlayCollision(speed, isMarble);
                }

                if (PitStriker.VFX.VFXManager.Instance != null && collision.contactCount > 0)
                {
                    PitStriker.VFX.VFXManager.Instance.PlayCollisionSparks(collision.contacts[0].point, speed);
                }
            }
        }

        /// <summary>
        /// Immediately halts all physics velocity.
        /// </summary>
        public void Halt()
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            _wasMoving = false;
        }

        /// <summary>
        /// Resets the marble position and halts all physics motion immediately.
        /// </summary>
        public void ResetPosition(Vector3 newPosition)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.position = newPosition;
            }
            transform.position = newPosition;
            _wasMoving = false;
        }

        /// <summary>
        /// Enables or disables marble visual rendering and physical interaction on the track.
        /// When hidden, collisions, gravity, and velocity are suspended.
        /// </summary>
        public void SetVisible(bool visible)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                if (r is LineRenderer) continue; // Keep aiming guide intact
                r.enabled = visible;
            }

            if (_collider == null) _collider = GetComponent<SphereCollider>();
            if (_collider != null) _collider.enabled = visible;

            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_rigidbody != null)
            {
                if (!visible)
                {
                    Halt();
                    _rigidbody.isKinematic = true;
                    _rigidbody.detectCollisions = false;
                }
                else
                {
                    _rigidbody.isKinematic = false;
                    _rigidbody.detectCollisions = true;
                }
            }
        }
    }
}
