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
        [SerializeField] private float _linearDrag = 0.18f;

        [Tooltip("Angular drag preventing infinite ice-rink rolling.")]
        [SerializeField] private float _angularDrag = 0.5f;

        [Tooltip("Speed below which the marble is considered completely stopped.")]
        [SerializeField] private float _stopThreshold = 0.10f;

        // Cached Components
        private Rigidbody _rigidbody;
        private SphereCollider _collider;
        private PhysicsMaterial _contactMaterial;
        private Vector3 _preStepVelocity;
        private static int _nextCollisionOrder;
        private int _collisionOrder;
        public float WorldRadius => _collider != null
            ? _collider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y), Mathf.Abs(transform.lossyScale.z)) : 0.16f;

        // State Tracking
        public bool _wasMoving = false;
        private float _launchGraceTimer = 0f;
        private int _consecutiveRestFrames = 0;

        // Events
        public event Action OnMarbleLaunched;
        public event Action OnMarbleStopped;
        public event Action<Collision> OnMarbleCollision;
        public static event Action<MarbleController, MarbleController> OnMarbleHitMarble; // (striker, hitTarget)

        public Rigidbody Rigidbody => _rigidbody;
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

            _collisionOrder = ++_nextCollisionOrder;
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
            _rigidbody.solverIterations = 12;
            _rigidbody.solverVelocityIterations = 6;
            _rigidbody.maxAngularVelocity = 100f;
            _contactMaterial = new PhysicsMaterial("Marble contact")
            {
                dynamicFriction = 0.12f, staticFriction = 0.12f,
                bounciness = 0.85f,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            _collider.sharedMaterial = _contactMaterial;
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

            _preStepVelocity = _rigidbody.linearVelocity;

            if (_launchGraceTimer > 0f)
            {
                _launchGraceTimer -= Time.fixedDeltaTime;
                _wasMoving = true;
                _consecutiveRestFrames = 0;
                return;
            }

            // Snappy tail-end braking on the flat fairway to eliminate endless micro-crawls.
            // When dipping inside the shallow pit saucer (y < 0.22f), natural 3D physics runs uninhibited so marbles can settle and lip out!
            Vector3 horizVel = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            float horizSpeed = horizVel.magnitude;
            if (horizSpeed < 0.15f && horizSpeed > 0f && transform.position.y >= WorldRadius * 0.88f)
            {
                float brake = Time.fixedDeltaTime * 0.6f;
                Vector3 targetHoriz = Vector3.MoveTowards(horizVel, Vector3.zero, brake);
                _rigidbody.linearVelocity = new Vector3(targetHoriz.x, _rigidbody.linearVelocity.y, targetHoriz.z);
                _rigidbody.angularVelocity = Vector3.MoveTowards(_rigidbody.angularVelocity, Vector3.zero, brake * 1.0f);
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

            // Active Boundary Containment & Elastic Wall Reflection:
            // Aligned with the visual stone wall borders (widening from 5.1m at baseline to 6.6m at Pit 3)
            // Mathematically guarantees marbles NEVER pass through or jump over the stone walls under any velocity
            Vector3 pos = transform.position;
            Vector3 vel = _rigidbody.linearVelocity;
            bool boundaryHit = false;

            float t = Mathf.Clamp01((pos.z - (-8.5f)) / 46.5f);
            float currentMaxX = Mathf.Lerp(5.15f, 6.65f, t);
            float currentMinX = -currentMaxX;

            if (pos.x < currentMinX)
            {
                pos.x = currentMinX;
                vel.x = Mathf.Abs(vel.x) * 0.65f;
                boundaryHit = true;
            }
            else if (pos.x > currentMaxX)
            {
                pos.x = currentMaxX;
                vel.x = -Mathf.Abs(vel.x) * 0.65f;
                boundaryHit = true;
            }

            const float minZ = -8.5f;
            const float maxZ = 37.5f;
            if (pos.z < minZ)
            {
                pos.z = minZ;
                vel.z = Mathf.Abs(vel.z) * 0.65f;
                boundaryHit = true;
            }
            else if (pos.z > maxZ)
            {
                pos.z = maxZ;
                vel.z = -Mathf.Abs(vel.z) * 0.65f;
                boundaryHit = true;
            }

            // Altitude ceiling clamp to prevent vaulting over walls
            if (pos.y > 1.8f)
            {
                pos.y = 1.8f;
                if (vel.y > 0f) vel.y = -1.0f;
                boundaryHit = true;
            }

            if (boundaryHit)
            {
                transform.position = pos;
                _rigidbody.linearVelocity = vel;
            }

            // Safety catch: If marble ever drops into the void below the track, recover it
            if (transform.position.y < -2.0f)
            {
                ResetPosition(new Vector3(0f, 0.3f, -5.5f));
                Debug.LogWarning("<color=#FFAA00><b>[SAFETY RESPAWN]</b> Marble recovered from void and placed safely at launch baseline.</color>");
            }
        }

        private void OnDestroy()
        {
            if (_contactMaterial != null) Destroy(_contactMaterial);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (IsRetired) return;

            OnMarbleCollision?.Invoke(collision);

            MarbleController otherMarble = collision.collider.GetComponent<MarbleController>();
            bool isMarble = otherMarble != null && !otherMarble.IsRetired;
            if (isMarble)
            {
                // PhysX has already resolved this contact. Do not overwrite its
                // post-solver velocities or apply a second energy-adding impulse.
                _wasMoving = otherMarble._wasMoving = true;
                _consecutiveRestFrames = otherMarble._consecutiveRestFrames = 0;
                _launchGraceTimer = Mathf.Max(_launchGraceTimer, 0.08f);
                otherMarble._launchGraceTimer = Mathf.Max(otherMarble._launchGraceTimer, 0.08f);

                Vector3 n = (otherMarble.transform.position - transform.position).normalized;
                float mine = Vector3.Dot(_preStepVelocity, n);
                float theirs = Vector3.Dot(otherMarble._preStepVelocity, -n);
                bool primary = mine > theirs + 0.0001f ||
                    (Mathf.Abs(mine - theirs) <= 0.0001f && _collisionOrder < otherMarble._collisionOrder);
                if (!primary) return; // One scoring/audio/VFX notification per pair.
                OnMarbleHitMarble?.Invoke(this, otherMarble);
                float impactSpeed = collision.relativeVelocity.magnitude;
                if (PitStriker.CameraSystem.SmoothFollowCamera.Instance != null && impactSpeed > 1.2f)
                    PitStriker.CameraSystem.SmoothFollowCamera.Instance.TriggerImpactShake(
                        Mathf.Clamp(impactSpeed * 0.018f, 0.035f, 0.16f), 0.12f);
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
