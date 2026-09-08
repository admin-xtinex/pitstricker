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

        /// <summary>
        /// True when this marble has finished the course and taken its place on the podium.
        /// When retired, physics forces, collisions, and safety respawns are suspended.
        /// </summary>
        public bool IsRetired { get; set; } = false;

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
            if (IsRetired || _rigidbody == null) return;

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
            if (IsRetired || _rigidbody == null) return;

            if (_launchGraceTimer > 0f)
            {
                _launchGraceTimer -= Time.fixedDeltaTime;
                _wasMoving = true;
                _consecutiveRestFrames = 0;
                return;
            }

            // Snappy tail-end braking on the flat fairway to eliminate endless micro-crawls.
            // When dipping inside the pit depression (y < 0.18f), natural 3D physics runs uninhibited so marbles can climb and lip out!
            Vector3 horizVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            float horizSpeed = horizVel.magnitude;
            if (horizSpeed < 0.75f && horizSpeed > 0f && transform.position.y >= 0.18f)
            {
                float brake = Time.fixedDeltaTime * 2.0f;
                Vector3 targetHoriz = Vector3.MoveTowards(horizVel, Vector3.zero, brake);
                _rigidbody.linearVelocity = new Vector3(targetHoriz.x, _rigidbody.linearVelocity.y, targetHoriz.z);
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
            if (IsRetired) return;

            OnMarbleCollision?.Invoke(collision);

            MarbleController otherMarble = collision.collider.GetComponent<MarbleController>();
            bool isMarble = otherMarble != null;
            if (isMarble)
            {
                OnMarbleHitMarble?.Invoke(this, otherMarble);

                // Authentic Carrom & 8-Ball Pool Equal-Mass Elastic Collision:
                // Only process from the striker side (marble with higher speed) to prevent reciprocal double-impulse inversion!
                if (collision.contactCount > 0 && _rigidbody != null)
                {
                    Rigidbody otherRb = otherMarble.GetComponent<Rigidbody>();
                    if (otherRb != null)
                    {
                        Vector3 vStriker = _rigidbody.linearVelocity;
                        Vector3 vTarget = otherRb.linearVelocity;

                        // Only the faster incoming marble acts as striker
                        if (vStriker.sqrMagnitude > vTarget.sqrMagnitude)
                        {
                            // Contact normal pointing from striker to target
                            Vector3 normal = (otherMarble.transform.position - transform.position);
                            normal.y = 0f;

                            if (normal.sqrMagnitude > 0.001f)
                            {
                                normal.Normalize();

                                // Striker incoming speed along contact normal
                                Vector3 relVel = vStriker - vTarget;
                                float vn = Vector3.Dot(relVel, normal);

                                if (vn > 0.05f) // Valid closing velocity
                                {
                                    // 1. Target Marble receives massive kinetic blast forward
                                    float blastSpeed = Mathf.Max(vn * 2.6f, 5.5f);
                                    Vector3 blastVelocity = normal * blastSpeed;

                                    // If target marble is sitting down inside a pit depression, pop it up and out over the rim!
                                    if (otherMarble.transform.position.y < 0.20f)
                                    {
                                        blastVelocity += Vector3.up * 3.2f;
                                    }

                                    otherRb.linearVelocity = blastVelocity;
                                    otherMarble._wasMoving = true;
                                    otherMarble._consecutiveRestFrames = 0;

                                    // 2. Striker dumps almost all forward momentum (dead stop / gentle settle)
                                    _rigidbody.linearVelocity = _rigidbody.linearVelocity * 0.15f;
                                    _rigidbody.angularVelocity = _rigidbody.angularVelocity * 0.15f;
                                    _wasMoving = true;

                                    // Audio, VFX, Camera Shake & Haptics Juice for powerful tactical direct strike
                                    if (PitStriker.VFX.VFXManager.Instance != null && collision.contactCount > 0)
                                    {
                                        PitStriker.VFX.VFXManager.Instance.PlayCollisionSparks(collision.contacts[0].point, blastSpeed);
                                    }

                                    if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null)
                                    {
                                        PitStriker.CameraSystem.SmoothFollowCamera.Instance.TriggerImpactShake(Mathf.Clamp(blastSpeed * 0.04f, 0.15f, 0.45f), 0.22f);
                                    }
#if UNITY_ANDROID || UNITY_IOS
                                    Handheld.Vibrate();
#endif

                                    Debug.Log($"<color=#00FFAA><b>[DIRECT STRIKE BLAST]</b> Striker halted. Target {otherMarble.name} blasted away with {blastSpeed:F1} m/s speed!</color>");
                                }
                            }
                        }
                    }
                }
            }

            float speed = collision.relativeVelocity.magnitude;
            if (speed > 0.2f)
            {
                if (PitStriker.Audio.AudioManager.Instance != null)
                {
                    PitStriker.Audio.AudioManager.Instance.PlayCollision(isMarble ? speed * 1.6f : speed, isMarble);
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
            if (_rigidbody != null && !_rigidbody.isKinematic)
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
                if (!_rigidbody.isKinematic)
                {
                    _rigidbody.linearVelocity = Vector3.zero;
                    _rigidbody.angularVelocity = Vector3.zero;
                }
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
