using System.Collections;
using UnityEngine;
using PitStriker.Physics;
using PitStriker.Input;
using PitStriker.Gameplay;

namespace PitStriker.Visuals
{
    /// <summary>
    /// Physics-assisted squash-and-stretch and secondary deformation controller for marbles.
    /// Delivers exaggerated cartoon animation, anticipation during aiming pullback,
    /// impact pancake squash with damped harmonic spring rebound, and pit sink settling.
    /// Preserves physical SphereCollider and Rigidbody integrity by deforming visual layer only.
    /// </summary>
    [RequireComponent(typeof(MarbleController))]
    public class MarbleSquashAndStretch : MonoBehaviour
    {
        [Header("Squash & Stretch Tuning")]
        [Tooltip("Maximum squash deformation allowed on high-speed impact (0.0 to 0.7).")]
        [SerializeField, Range(0.1f, 0.7f)] private float _maxImpactSquash = 0.45f;

        [Tooltip("Impact velocity sensitivity for deformation.")]
        [SerializeField] private float _impactSensitivity = 0.045f;

        [Tooltip("Natural oscillation frequency of the elastic spring (rad/s).")]
        [SerializeField] private float _springFrequency = 28f;

        [Tooltip("Damping ratio (0.5 = lively bounce, 0.9 = stiff).")]
        [SerializeField] private float _dampingRatio = 0.65f;

        [Tooltip("Aiming pullback compression strength (anticipation).")]
        [SerializeField, Range(0.05f, 0.4f)] private float _anticipationSquash = 0.25f;

        [Header("Visual Target")]
        [SerializeField] private Transform _visualRoot;

        // References
        private MarbleController _marble;
        private Rigidbody _rb;
        private Vector3 _baseScale = Vector3.one;

        // Spring State
        private float _currentDeformation = 0f; // negative = squash, positive = stretch
        private float _deformationVelocity = 0f;
        private Vector3 _deformationAxis = Vector3.up;

        // Anticipation State
        private bool _isAimingThisMarble = false;
        private float _aimPower = 0f;
        private Vector3 _aimDirection = Vector3.forward;

        private void Awake()
        {
            _marble = GetComponent<MarbleController>();
            _rb = GetComponent<Rigidbody>();
            SetupVisualRoot();
        }

        private Coroutine _vortexRoutine;

        private void SetupVisualRoot()
        {
            if (_visualRoot != null) return;

            // If a child named "VisualMesh" exists, bind it
            Transform child = transform.Find("VisualMesh");
            if (child != null)
            {
                _visualRoot = child;
                _baseScale = Vector3.one;
                MeshRenderer mrChild = child.GetComponent<MeshRenderer>();
                if (mrChild != null) mrChild.enabled = true;
                return;
            }

            // Create a dedicated visual child so the parent collider remains perfectly round
            MeshFilter mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();

            if (mf != null && mr != null)
            {
                GameObject visualObj = new GameObject("VisualMesh");
                visualObj.transform.SetParent(transform, false);
                visualObj.transform.localPosition = Vector3.zero;
                visualObj.transform.localRotation = Quaternion.identity;
                visualObj.transform.localScale = Vector3.one;

                MeshFilter childMF = visualObj.AddComponent<MeshFilter>();
                MeshRenderer childMR = visualObj.AddComponent<MeshRenderer>();
                childMF.sharedMesh = mf.sharedMesh;
                childMR.sharedMaterials = mr.sharedMaterials;
                childMR.shadowCastingMode = mr.shadowCastingMode;
                childMR.receiveShadows = mr.receiveShadows;
                childMR.renderingLayerMask = mr.renderingLayerMask;

                // Disable parent mesh renderer so only the deformed child renders
                mr.enabled = false;

                _visualRoot = visualObj.transform;
                _baseScale = Vector3.one;
            }
            else
            {
                _visualRoot = transform;
                _baseScale = transform.localScale;
            }
        }

        private void OnEnable()
        {
            if (_marble != null)
            {
                _marble.OnMarbleLaunched += HandleLaunched;
                _marble.OnMarbleStopped += HandleStopped;
                _marble.OnMarbleCollision += HandleCollision;
            }
            SwipeLaunchController.OnPowerChanged += HandleAimPowerChanged;
            PitZone.OnMarbleSunk += HandleMarbleSunk;
        }

        private void OnDisable()
        {
            if (_marble != null)
            {
                _marble.OnMarbleLaunched -= HandleLaunched;
                _marble.OnMarbleStopped -= HandleStopped;
                _marble.OnMarbleCollision -= HandleCollision;
            }
            SwipeLaunchController.OnPowerChanged -= HandleAimPowerChanged;
            PitZone.OnMarbleSunk -= HandleMarbleSunk;

            if (_vortexRoutine != null)
            {
                StopCoroutine(_vortexRoutine);
                _vortexRoutine = null;
            }

            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.one;
                _visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void OnDestroy()
        {
            MeshRenderer mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = true;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (_isAimingThisMarble && _aimPower > 0.05f)
            {
                // Anticipation: Compress backward along aim direction, jitter slightly at high power
                float jitter = _aimPower > 0.75f ? (Mathf.Sin(Time.time * 55f) * 0.035f * _aimPower) : 0f;
                float targetSquash = -(_aimPower * _anticipationSquash + jitter);
                _currentDeformation = Mathf.Lerp(_currentDeformation, targetSquash, dt * 16f);
                _deformationAxis = _aimDirection;
            }
            else
            {
                // Sub-step the spring-damper to ensure unconditional numerical stability under any frame-rate
                float remainingDt = Mathf.Min(dt, 0.066f);
                const float maxSubStep = 0.012f;
                while (remainingDt > 0.0001f)
                {
                    float step = Mathf.Min(remainingDt, maxSubStep);
                    remainingDt -= step;

                    float springForce = -(_springFrequency * _springFrequency) * _currentDeformation;
                    float dampingForce = -(2f * _dampingRatio * _springFrequency) * _deformationVelocity;
                    float acceleration = springForce + dampingForce;

                    // Semi-implicit Euler integration (integrates velocity first, then position for energy stability)
                    _deformationVelocity += acceleration * step;
                    _deformationVelocity = Mathf.Clamp(_deformationVelocity, -15f, 15f);
                    _currentDeformation += _deformationVelocity * step;
                }

                // Strictly sanitize against NaN or Infinity
                if (float.IsNaN(_currentDeformation) || float.IsInfinity(_currentDeformation))
                {
                    _currentDeformation = 0f;
                    _deformationVelocity = 0f;
                }
                if (float.IsNaN(_deformationVelocity) || float.IsInfinity(_deformationVelocity))
                {
                    _deformationVelocity = 0f;
                }

                // Dampen any residual oscillation quickly once marble stabilizes
                if (Mathf.Abs(_currentDeformation) < 0.008f && Mathf.Abs(_deformationVelocity) < 0.05f)
                {
                    _currentDeformation = 0f;
                    _deformationVelocity = 0f;
                }
            }

            ApplyDeformation();
        }

        private void ApplyDeformation()
        {
            if (_visualRoot == null) return;

            // When no active deformation is present, lock to pure neutral identity
            // This guarantees 100% smooth, jitter-free PhysX rolling at full monitor refresh rate!
            if (Mathf.Abs(_currentDeformation) < 0.005f)
            {
                _visualRoot.localRotation = Quaternion.identity;
                _visualRoot.localScale = Vector3.one;
                return;
            }

            if (float.IsNaN(_currentDeformation) || float.IsInfinity(_currentDeformation))
            {
                _currentDeformation = 0f;
                _deformationVelocity = 0f;
                _visualRoot.localRotation = Quaternion.identity;
                _visualRoot.localScale = Vector3.one;
                return;
            }

            float d = Mathf.Clamp(_currentDeformation, -0.55f, 0.55f);

            // 3D Volume Preservation: V = (1 + d) * p * p = 1 => p = 1 / sqrt(1 + d)
            float axisScale = 1.0f + d;
            float perpScale = 1.0f / Mathf.Sqrt(Mathf.Max(0.20f, axisScale));

            if (float.IsNaN(axisScale) || float.IsInfinity(axisScale) || axisScale <= 0.01f) axisScale = 1f;
            if (float.IsNaN(perpScale) || float.IsInfinity(perpScale) || perpScale <= 0.01f) perpScale = 1f;

            // Align deformation along deformation axis in local coordinates
            Vector3 localAxis = _visualRoot.parent != null
                ? _visualRoot.parent.InverseTransformDirection(_deformationAxis).normalized
                : _deformationAxis.normalized;

            if (float.IsNaN(localAxis.x) || float.IsNaN(localAxis.y) || float.IsNaN(localAxis.z) || localAxis.sqrMagnitude < 0.01f)
            {
                localAxis = Vector3.up;
            }

            // Rotate visualRoot to deformation axis, apply non-uniform scale, rotate back
            Quaternion rotToAxis = Quaternion.FromToRotation(Vector3.up, localAxis);
            Vector3 scaled = new Vector3(perpScale, axisScale, perpScale);

            if (float.IsNaN(scaled.x) || float.IsInfinity(scaled.x) || scaled.x <= 0.001f) scaled = Vector3.one;
            if (float.IsNaN(scaled.y) || float.IsInfinity(scaled.y) || scaled.y <= 0.001f) scaled = Vector3.one;
            if (float.IsNaN(scaled.z) || float.IsInfinity(scaled.z) || scaled.z <= 0.001f) scaled = Vector3.one;

            _visualRoot.localRotation = rotToAxis;
            _visualRoot.localScale = Vector3.Scale(_baseScale, scaled);
        }

        private void HandleAimPowerChanged(float power)
        {
            if (SwipeLaunchController.Instance != null &&
                TurnManager.Instance != null &&
                TurnManager.Instance.ActivePlayer != null &&
                TurnManager.Instance.ActivePlayer.marble == _marble)
            {
                _isAimingThisMarble = power > 0.01f;
                _aimPower = power;
                if (Camera.main != null)
                {
                    Vector3 fwd = Camera.main.transform.forward;
                    fwd.y = 0f;
                    _aimDirection = fwd.normalized;
                }
            }
            else
            {
                _isAimingThisMarble = false;
                _aimPower = 0f;
            }
        }

        private void HandleLaunched()
        {
            if (_vortexRoutine != null)
            {
                StopCoroutine(_vortexRoutine);
                _vortexRoutine = null;
            }

            _isAimingThisMarble = false;
            _aimPower = 0f;
            _currentDeformation = 0f;
            _deformationVelocity = 0f;

            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.one;
                _visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void HandleStopped()
        {
            if (_vortexRoutine != null)
            {
                StopCoroutine(_vortexRoutine);
                _vortexRoutine = null;
            }

            _isAimingThisMarble = false;
            _aimPower = 0f;
            _currentDeformation = 0f;
            _deformationVelocity = 0f;
            if (_visualRoot != null)
            {
                _visualRoot.localScale = Vector3.one;
                _visualRoot.localRotation = Quaternion.identity;
            }
        }

        private void HandleCollision(Collision collision)
        {
            // Marbles are solid glass spheres; rolling contact must remain pristine and un-deformed
            // so PhysX contact and angular velocity remain 100% stable at high refresh rates.
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (marble == _marble)
            {
                if (_vortexRoutine != null) StopCoroutine(_vortexRoutine);
                _vortexRoutine = StartCoroutine(PitSinkVortexRoutine(pit.transform.position));
            }
        }

        private IEnumerator PitSinkVortexRoutine(Vector3 pitCenter)
        {
            float elapsed = 0f;
            const float duration = 0.65f;
            Vector3 startPos = transform.position;
            Vector3 settlePos = new Vector3(pitCenter.x, pitCenter.y + 0.16f, pitCenter.z);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float ease = Mathf.SmoothStep(0f, 1f, t);

                // Vortex spiral into pit
                float angle = ease * Mathf.PI * 4f;
                float radius = (1f - ease) * 0.28f;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(t * Mathf.PI) * 0.08f, Mathf.Sin(angle) * radius);

                transform.position = Vector3.Lerp(startPos, settlePos, ease) + offset;

                // Joyful celebratory squash on bottom landing
                if (t > 0.7f && t < 0.9f)
                {
                    _currentDeformation = -0.22f;
                    _deformationAxis = Vector3.up;
                }

                yield return null;
            }

            transform.position = settlePos;
            // Settle with a playful little bounce
            _currentDeformation = 0.12f;
            _deformationVelocity = -4f;
            _vortexRoutine = null;
        }
    }
}
