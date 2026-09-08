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
        [SerializeField] private float _stopThreshold = 0.12f;

        // Cached Components
        private Rigidbody _rigidbody;
        private SphereCollider _collider;

        // State Tracking
        public bool _wasMoving = false;
        private float _launchGraceTimer = 0f;
        private int _consecutiveRestFrames = 0;

        // Events
        public event Action OnMarbleLaunched;
        public event Action OnMarbleStopped;
        public event Action<Collision> OnMarbleCollision;
        public static event Action<MarbleController, MarbleController> OnMarbleHitMarble; // (striker, hitTarget)

        public bool IsMoving => _rigidbody != null && (_rigidbody.linearVelocity.sqrMagnitude > (_stopThreshold * _stopThreshold) || _rigidbody.angularVelocity.sqrMagnitude > 0.6f);
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
        /// <param name="direction">Normalized ground direction vector.</param>
        /// <param name="forceMagnitude">Impulse strength to apply.</param>
        public void ApplyImpulse(Vector3 direction, float forceMagnitude)
        {
            if (_rigidbody == null) return;

            // Allow slight upward pitch if provided (for flick throw lob), clamped to prevent sky launches
            direction.y = Mathf.Clamp(direction.y, 0f, 0.12f);
            Vector3 normalizedDir = direction.normalized;

            // Apply physical impulse
            _rigidbody.AddForce(normalizedDir * forceMagnitude, ForceMode.Impulse);
            _wasMoving = true;
            _launchGraceTimer = 0.4f; // Guarantee physics integration time
            _consecutiveRestFrames = 0;

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

            if (_launchGraceTimer > 0f)
            {
                _launchGraceTimer -= Time.fixedDeltaTime;
                _wasMoving = true;
                _consecutiveRestFrames = 0;
                return;
            }

            // Snappy tail-end braking to eliminate endless micro-crawls on the course
            Vector3 horizVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            float horizSpeed = horizVel.magnitude;
            if (horizSpeed < 0.75f && horizSpeed > 0f)
            {
                float brake = Time.fixedDeltaTime * 2.2f;
                _rigidbody.linearVelocity = Vector3.MoveTowards(_rigidbody.linearVelocity, Vector3.zero, brake);
                _rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, Vector3.zero, brake * 1.5f);
            }

            bool currentlyMoving = IsMoving;

            if (currentlyMoving)
            {
                _wasMoving = true;
                _consecutiveRestFrames = 0;
            }
            else if (_wasMoving)
            {
                _consecutiveRestFrames++;
                // Must be under stop threshold continuously for 8 physics steps (~0.16s)
                if (_consecutiveRestFrames >= 8)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                    _wasMoving = false;
                    _consecutiveRestFrames = 0;

                    OnMarbleStopped?.Invoke();
                    Debug.Log("[MARBLE] Marble has come to a stable complete stop.");
                }
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

                // Carrom / 8-Ball Pool Elastic Momentum Transfer:
                // Strike target marble with amplified impulse along the contact normal
                if (collision.contactCount > 0)
                {
                    Vector3 normal = collision.contacts[0].normal;
                    Vector3 pushDir = -normal;
                    pushDir.y = 0f;
                    if (pushDir.sqrMagnitude > 0.001f)
                    {
                        pushDir.Normalize();
                        float relSpeed = collision.relativeVelocity.magnitude;
                        if (relSpeed > 0.12f)
                        {
                            // Kinetic transfer impulse directly along contact normal
                            float transferImpulse = relSpeed * _mass * 1.55f;

                            Rigidbody otherRb = otherMarble.GetComponent<Rigidbody>();
                            if (otherRb != null)
                            {
                                otherRb.AddForce(pushDir * transferImpulse, ForceMode.Impulse);
                                otherMarble._wasMoving = true;
                            }

                            // Deflect and slow down striker like a carrom striker hitting a coin
                            if (_rigidbody != null)
                            {
                                _rigidbody.AddForce(-pushDir * (transferImpulse * 0.45f), ForceMode.Impulse);
                            }
                        }
                    }
                }
            }

            float speed = collision.relativeVelocity.magnitude;
            if (speed > 0.25f)
            {
                if (PitStriker.Audio.AudioManager.Instance != null)
                {
                    PitStriker.Audio.AudioManager.Instance.PlayCollision(isMarble ? speed * 1.4f : speed, isMarble);
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
            _launchGraceTimer = 0f;
            _consecutiveRestFrames = 0;
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
            _launchGraceTimer = 0f;
            _consecutiveRestFrames = 0;
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
