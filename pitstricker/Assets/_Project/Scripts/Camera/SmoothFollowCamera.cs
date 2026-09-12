using UnityEngine;
using UnityEngine.InputSystem;
using PitStriker.Physics;

namespace PitStriker.CameraSystem
{
    /// <summary>
    /// Smoothly follows the active marble and dynamically aligns the camera view behind the marble.
    /// Supports a TWO-ZONE input system:
    /// - Left side: Camera orbit (yaw and pitch) with smooth interpolation, damping/inertia, limits, and dead zone.
    /// - Right side: Aiming & shooting (locks camera orientation during aim).
    /// </summary>
    public class SmoothFollowCamera : MonoBehaviour
    {
        public static SmoothFollowCamera Instance { get; private set; }

        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (typically the active marble).")]
        [SerializeField] private Transform _target;

        [Header("Framing Distance & Elevation")]
        [Tooltip("Distance behind the marble along the aim vector.")]
        [SerializeField] private float _distance = 4.8f;

        [Tooltip("Base height of the camera above the ground plane.")]
        [SerializeField] private float _height = 2.6f;

        [Tooltip("Look-at height offset above target pivot.")]
        [SerializeField] private float _lookAtHeightOffset = 0.45f;

        [Header("Tracking Smoothing")]
        [Tooltip("Smooth time for position tracking damping (lower is snappier, higher is smoother).")]
        [SerializeField] private float _smoothTime = 0.22f;

        [Tooltip("Smooth time for camera tracking behind the marble during a rolling shot.")]
        [SerializeField] private float _shotFollowSmoothing = 0.065f;

        [Header("Two-Zone Screen Layout")]
        [Range(0.2f, 0.8f)]
        [Tooltip("Fraction of screen width (0.0 to 1.0) on the left dedicated to camera rotation. Touches at or right of this line are aiming.")]
        [SerializeField] private float _cameraZoneSplitRatio = 0.5f;

        [Tooltip("Show visual boundary line and zone labels in the Game view for testing (disable in production).")]
        [SerializeField] private bool _showDebugZones = false;

        [Header("Manual Orbit Controls & Sensitivity")]
        [Tooltip("Horizontal drag sensitivity for yaw rotation (degrees per screen pixel).")]
        [SerializeField] private float _yawSensitivity = 0.22f;

        [Tooltip("Vertical drag sensitivity for pitch adjustment (degrees per screen pixel).")]
        [SerializeField] private float _pitchSensitivity = 0.16f;

        [Tooltip("Invert vertical drag direction for pitch adjustment.")]
        [SerializeField] private bool _invertPitch = false;

        [Tooltip("Keyboard orbit speed in degrees per second.")]
        [SerializeField] private float _keyOrbitSpeed = 65.0f;

        [Tooltip("Minimum drag distance in pixels before registering camera rotation (filters accidental micro-jitters).")]
        [SerializeField] private float _dragDeadZone = 1.5f;

        [Header("Rotation Limits")]
        [Tooltip("Enable horizontal rotation limits. If false, camera has free 360-degree rotation.")]
        [SerializeField] private bool _limitHorizontalRotation = false;

        [Tooltip("Minimum horizontal yaw angle (degrees relative to target alignment).")]
        [SerializeField] private float _minYawAngle = -90f;

        [Tooltip("Maximum horizontal yaw angle (degrees relative to target alignment).")]
        [SerializeField] private float _maxYawAngle = 90f;

        [Tooltip("Minimum pitch angle (degrees, lower camera angle). Clamped to prevent flipping.")]
        [SerializeField] private float _minPitchAngle = -12f;

        [Tooltip("Maximum pitch angle (degrees, higher bird's-eye camera angle).")]
        [SerializeField] private float _maxPitchAngle = 38f;

        [Header("Smoothing & Inertia")]
        [Tooltip("Smooth time for camera rotation interpolation (prevents twitchiness).")]
        [SerializeField] private float _rotationSmoothTime = 0.08f;

        [Range(0.5f, 0.98f)]
        [Tooltip("Inertia damping factor when releasing drag (higher = longer gentle spin).")]
        [SerializeField] private float _inertiaDamping = 0.90f;

        [Tooltip("Preserve the player's last intentional camera viewing angle across turns.")]
        [SerializeField] private bool _preserveViewingAngleAcrossTurns = false;

        // Alignment & Orbit State
        private Vector3 _baseAimDirection = Vector3.forward;
        private float _manualOrbitAngle = 0f;
        private float _targetOrbitAngle = 0f;
        private float _orbitSmoothVelocity = 0f;
        private float _yawInertiaVelocity = 0f;

        private float _pitchAngle = 0f;
        private float _targetPitchAngle = 0f;
        private float _pitchSmoothVelocity = 0f;
        private float _pitchInertiaVelocity = 0f;

        private Vector3 _currentVelocity;
        private bool _isAimLocked = false;
        private bool _isDraggingCamera = false;

        // Input Tracking State
        private int _cameraTouchId = -1;
        private Vector2 _lastTouchPos;
        private bool _isMouseOrbitDragging = false;
        private Vector2 _lastMousePos;
        private bool _isRightMouseDragging = false;
        private Vector2 _lastRightMousePos;

        // Camera Impact Shake
        private float _shakeTimer = 0f;
        private float _shakeIntensity = 0f;
        private Coroutine _impactTrackCoroutine;

        // Public Properties
        public float ManualOrbitAngle => _manualOrbitAngle;
        public float PitchAngle => _pitchAngle;
        public bool IsAimLocked => _isAimLocked;
        public float CameraZoneSplitRatio => _cameraZoneSplitRatio;

        /// <summary>
        /// Per-map framing overrides (e.g. MapCameraFramingBlender's Camera A/B blend).
        /// Unused by default, so maps that never call these keep today's exact behavior.
        /// </summary>
        public void SetDistance(float distance) => _distance = distance;
        public void SetHeight(float height) => _height = height;
        public void SetLookAtHeightOffset(float offset) => _lookAtHeightOffset = offset;

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
        /// Locks or unlocks camera orientation. When locked (e.g. during aiming/power pull),
        /// camera rotation input is completely ignored and orientation stays rock-solid.
        /// </summary>
        public void SetAimLocked(bool locked)
        {
            _isAimLocked = locked;
            if (locked)
            {
                _isDraggingCamera = false;
                _isMouseOrbitDragging = false;
                _isRightMouseDragging = false;
                _cameraTouchId = -1;
                _yawInertiaVelocity = 0f;
                _pitchInertiaVelocity = 0f;

                // Lock targets to current angles so no interpolation drift occurs while aiming
                _targetOrbitAngle = _manualOrbitAngle;
                _targetPitchAngle = _pitchAngle;
                _orbitSmoothVelocity = 0f;
                _pitchSmoothVelocity = 0f;
            }
        }

        /// <summary>
        /// Public entry point to feed camera rotation delta from external controllers if needed.
        /// </summary>
        public void ProcessCameraDrag(Vector2 delta)
        {
            if (_isAimLocked) return;
            ProcessCameraDragInternal(delta);
        }

        /// <summary>
        /// Public entry point to signal drag release.
        /// </summary>
        public void EndCameraDrag()
        {
            EndCameraDragInternal();
        }

        private void ProcessCameraDragInternal(Vector2 delta)
        {
            if (_isAimLocked) return;

            if (delta.sqrMagnitude < _dragDeadZone * _dragDeadZone)
            {
                return;
            }

            _isDraggingCamera = true;

            // Horizontal / Yaw
            float yawDelta = delta.x * _yawSensitivity;
            _targetOrbitAngle += yawDelta;
            if (_limitHorizontalRotation)
            {
                _targetOrbitAngle = Mathf.Clamp(_targetOrbitAngle, _minYawAngle, _maxYawAngle);
            }

            // Vertical / Pitch (dragging up tilts view / elevates camera)
            float pitchDir = _invertPitch ? 1f : -1f;
            float pitchDelta = delta.y * _pitchSensitivity * pitchDir;
            _targetPitchAngle = Mathf.Clamp(_targetPitchAngle + pitchDelta, _minPitchAngle, _maxPitchAngle);

            // Compute release inertia velocity (degrees per second)
            float dt = Mathf.Max(0.001f, Time.deltaTime);
            _yawInertiaVelocity = yawDelta / dt;
            _pitchInertiaVelocity = pitchDelta / dt;
        }

        private void EndCameraDragInternal()
        {
            _isDraggingCamera = false;

            // Cap max inertia velocity to prevent excessive spinning on sudden flicks
            _yawInertiaVelocity = Mathf.Clamp(_yawInertiaVelocity, -400f, 400f);
            _pitchInertiaVelocity = Mathf.Clamp(_pitchInertiaVelocity, -200f, 200f);
        }

        /// <summary>
        /// Triggers camera screen shake for high-energy direct strikes and heavy impacts.
        /// </summary>
        public void TriggerImpactShake(float intensity = 0.25f, float duration = 0.2f)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;
        }

        /// <summary>
        /// Temporarily shifts camera tracking to the blasted opponent marble during a strike impact.
        /// </summary>
        public void TrackImpactedTarget(Transform struckTarget, float duration = 1.6f)
        {
            if (struckTarget == null) return;
            if (_impactTrackCoroutine != null) StopCoroutine(_impactTrackCoroutine);
            _impactTrackCoroutine = StartCoroutine(TrackImpactRoutine(struckTarget, duration));
        }

        private System.Collections.IEnumerator TrackImpactRoutine(Transform struckTarget, float duration)
        {
            Transform originalTarget = _target;
            _target = struckTarget;
            yield return new WaitForSeconds(duration);
            if (_target == struckTarget && originalTarget != null)
            {
                _target = originalTarget;
            }
            _impactTrackCoroutine = null;
        }

        public void SetTarget(Transform newTarget, Vector3? objectivePoint = null, bool resetViewingAngle = false)
        {
            if (_impactTrackCoroutine != null)
            {
                StopCoroutine(_impactTrackCoroutine);
                _impactTrackCoroutine = null;
            }

            bool isDifferentTarget = (_target != newTarget);
            _target = newTarget;

            if (objectivePoint.HasValue && _target != null)
            {
                Vector3 toObj = objectivePoint.Value - _target.position;
                toObj.y = 0f;
                if (toObj.sqrMagnitude > 0.04f)
                {
                    _baseAimDirection = toObj.normalized;
                }
                else
                {
                    _baseAimDirection = Vector3.forward;
                }
            }
            else if (_baseAimDirection == Vector3.zero)
            {
                _baseAimDirection = Vector3.forward;
            }

            // Only reset orbit viewing angle if explicitly requested or target changed to another player without preservation
            if (resetViewingAngle || (isDifferentTarget && !_preserveViewingAngleAcrossTurns))
            {
                ResetOrbitToObjective();
            }
        }

        public void SetObjectiveTarget(Vector3 objectivePoint)
        {
            if (_target != null)
            {
                Vector3 toObj = objectivePoint - _target.position;
                toObj.y = 0f;
                if (toObj.sqrMagnitude > 0.04f)
                {
                    _baseAimDirection = toObj.normalized;
                }
            }
        }

        public void RotateOrbit(float deltaDegrees)
        {
            if (_isAimLocked) return;
            _targetOrbitAngle += deltaDegrees;
            if (_limitHorizontalRotation)
            {
                _targetOrbitAngle = Mathf.Clamp(_targetOrbitAngle, _minYawAngle, _maxYawAngle);
            }
        }

        public void ResetOrbitToObjective()
        {
            _targetOrbitAngle = 0f;
            _targetPitchAngle = 0f;
            _manualOrbitAngle = 0f;
            _pitchAngle = 0f;
            _yawInertiaVelocity = 0f;
            _pitchInertiaVelocity = 0f;
            _orbitSmoothVelocity = 0f;
            _pitchSmoothVelocity = 0f;
        }

        private void Update()
        {
            HandleTwoZoneCameraInput();
            UpdateOrbitPhysics();
        }

        private void HandleTwoZoneCameraInput()
        {
            if (_isAimLocked)
            {
                _isCameraTouchDragging = false;
                _isMouseOrbitDragging = false;
                _isRightMouseDragging = false;
                _cameraTouchId = -1;
                return;
            }

            float splitX = Screen.width * _cameraZoneSplitRatio;

            // Check if any touch is active on screen
            bool hasTouchInput = false;
            if (Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    if (touches[i].press.isPressed || touches[i].press.wasPressedThisFrame || touches[i].press.wasReleasedThisFrame)
                    {
                        hasTouchInput = true;
                        break;
                    }
                }
            }

            // 1. Mobile Touch Handling (prioritized when touches exist)
            if (hasTouchInput && Touchscreen.current != null)
            {
                var touches = Touchscreen.current.touches;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    int touchId = touch.touchId.ReadValue();
                    bool wasPressed = touch.press.wasPressedThisFrame;
                    bool isPressed = touch.press.isPressed;
                    bool wasReleased = touch.press.wasReleasedThisFrame;
                    Vector2 pos = touch.position.ReadValue();

                    if (wasPressed)
                    {
                        // Initial touch MUST be in Camera Zone (< splitX)
                        if (pos.x < splitX)
                        {
                            // Check UI blocking
                            if (UnityEngine.EventSystems.EventSystem.current == null ||
                                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId))
                            {
                                if (_cameraTouchId == -1)
                                {
                                    _cameraTouchId = touchId;
                                    _lastTouchPos = pos;
                                    _isCameraTouchDragging = true;
                                    _yawInertiaVelocity = 0f;
                                    _pitchInertiaVelocity = 0f;
                                }
                            }
                        }
                    }
                    else if (isPressed && touchId == _cameraTouchId)
                    {
                        // Continuous drag: tracks this touch regardless of crossing screen boundary
                        Vector2 delta = pos - _lastTouchPos;
                        _lastTouchPos = pos;
                        ProcessCameraDragInternal(delta);
                    }
                    else if (wasReleased && touchId == _cameraTouchId)
                    {
                        _cameraTouchId = -1;
                        _isCameraTouchDragging = false;
                        EndCameraDragInternal();
                    }
                }
            }
            // 2. Mouse / Trackpad Handling (when no active touch interaction)
            else if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();

                // Left-Click Drag: must start in Camera Zone (< splitX)
                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    if (mousePos.x < splitX)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current == null ||
                            !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                        {
                            _isMouseOrbitDragging = true;
                            _lastMousePos = mousePos;
                            _yawInertiaVelocity = 0f;
                            _pitchInertiaVelocity = 0f;
                        }
                    }
                }
                else if (Mouse.current.leftButton.isPressed && _isMouseOrbitDragging)
                {
                    Vector2 delta = mousePos - _lastMousePos;
                    _lastMousePos = mousePos;
                    ProcessCameraDragInternal(delta);
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame && _isMouseOrbitDragging)
                {
                    _isMouseOrbitDragging = false;
                    EndCameraDragInternal();
                }

                // Right-Click Drag: Editor convenience to orbit anywhere when not aiming
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    if (UnityEngine.EventSystems.EventSystem.current == null ||
                        !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        _isRightMouseDragging = true;
                        _lastRightMousePos = mousePos;
                        _yawInertiaVelocity = 0f;
                        _pitchInertiaVelocity = 0f;
                    }
                }
                else if (Mouse.current.rightButton.isPressed && _isRightMouseDragging)
                {
                    Vector2 delta = mousePos - _lastRightMousePos;
                    _lastRightMousePos = mousePos;
                    ProcessCameraDragInternal(delta);
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame && _isRightMouseDragging)
                {
                    _isRightMouseDragging = false;
                    EndCameraDragInternal();
                }
            }

            // 3. Keyboard A/D / Arrows / Q/E orbit
            float keyInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.qKey.isPressed)
                {
                    keyInput -= 1f;
                }
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed || Keyboard.current.eKey.isPressed)
                {
                    keyInput += 1f;
                }
            }

            if (Mathf.Abs(keyInput) > 0.01f)
            {
                _targetOrbitAngle += keyInput * _keyOrbitSpeed * Time.deltaTime;
                if (_limitHorizontalRotation)
                {
                    _targetOrbitAngle = Mathf.Clamp(_targetOrbitAngle, _minYawAngle, _maxYawAngle);
                }
                _yawInertiaVelocity = 0f;
            }
        }

        private bool _isCameraTouchDragging = false;

        private void UpdateOrbitPhysics()
        {
            if (_isAimLocked) return;

            bool isAnyDragActive = _isDraggingCamera || _isCameraTouchDragging || _isMouseOrbitDragging || _isRightMouseDragging;

            // Apply inertia decay when player releases touch / mouse
            if (!isAnyDragActive)
            {
                if (Mathf.Abs(_yawInertiaVelocity) > 0.1f)
                {
                    _targetOrbitAngle += _yawInertiaVelocity * Time.deltaTime;
                    if (_limitHorizontalRotation)
                    {
                        _targetOrbitAngle = Mathf.Clamp(_targetOrbitAngle, _minYawAngle, _maxYawAngle);
                        if (_targetOrbitAngle <= _minYawAngle || _targetOrbitAngle >= _maxYawAngle)
                        {
                            _yawInertiaVelocity = 0f;
                        }
                    }
                    _yawInertiaVelocity *= Mathf.Pow(_inertiaDamping, Time.deltaTime * 60f);
                }
                else
                {
                    _yawInertiaVelocity = 0f;
                }

                if (Mathf.Abs(_pitchInertiaVelocity) > 0.1f)
                {
                    _targetPitchAngle += _pitchInertiaVelocity * Time.deltaTime;
                    _targetPitchAngle = Mathf.Clamp(_targetPitchAngle, _minPitchAngle, _maxPitchAngle);
                    if (_targetPitchAngle <= _minPitchAngle || _targetPitchAngle >= _maxPitchAngle)
                    {
                        _pitchInertiaVelocity = 0f;
                    }
                    _pitchInertiaVelocity *= Mathf.Pow(_inertiaDamping, Time.deltaTime * 60f);
                }
                else
                {
                    _pitchInertiaVelocity = 0f;
                }
            }

            // Smooth interpolation to target angles (eliminates twitchiness)
            if (_rotationSmoothTime > 0.001f)
            {
                _manualOrbitAngle = Mathf.SmoothDamp(_manualOrbitAngle, _targetOrbitAngle, ref _orbitSmoothVelocity, _rotationSmoothTime);
                _pitchAngle = Mathf.SmoothDamp(_pitchAngle, _targetPitchAngle, ref _pitchSmoothVelocity, _rotationSmoothTime);
            }
            else
            {
                _manualOrbitAngle = _targetOrbitAngle;
                _pitchAngle = _targetPitchAngle;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // 1. Calculate horizontal aim direction based on base objective direction + manual yaw orbit angle
            Quaternion orbitRotation = Quaternion.Euler(0f, _manualOrbitAngle, 0f);
            Vector3 currentAimDir = orbitRotation * _baseAimDirection;
            currentAimDir.y = 0f;
            if (currentAimDir.sqrMagnitude < 0.001f) currentAimDir = Vector3.forward;
            currentAimDir.Normalize();

            // 2. Pitch elevation calculation (spherical elevation adjustment)
            float pitchRad = _pitchAngle * Mathf.Deg2Rad;
            float effectiveHeight = Mathf.Max(1.0f, _height + Mathf.Sin(pitchRad) * _distance * 0.8f);
            float effectiveDist = Mathf.Max(1.5f, _distance * Mathf.Cos(pitchRad));

            // 3. Dynamic smooth time: Snappy and responsive during active rolling, gentle when resting
            float targetSpeed = 0f;
            MarbleController targetMarble = _target.GetComponent<MarbleController>();
            if (targetMarble != null) targetSpeed = targetMarble.CurrentSpeed;
            float dynamicSmooth = Mathf.Lerp(Mathf.Max(0.12f, _smoothTime), _shotFollowSmoothing, Mathf.Clamp01(targetSpeed / 6.0f));

            // 4. Camera sits behind the marble along the aim vector with pitch height and distance
            Vector3 desiredPosition = _target.position - (currentAimDir * effectiveDist) + (Vector3.up * effectiveHeight);

            // Prevent camera from clipping inside the house or behind the left stone wall
            if (desiredPosition.z >= 2.0f && desiredPosition.z <= 16.5f)
            {
                desiredPosition.x = Mathf.Max(desiredPosition.x, -3.25f);
            }
            desiredPosition.y = Mathf.Max(desiredPosition.y, 1.2f);

            // 5. Smoothly interpolate camera position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, dynamicSmooth);

            // 6. Apply impact shake displacement if active
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * _shakeIntensity;
                shakeOffset.y *= 0.6f;
                transform.position += shakeOffset;
            }

            // 7. Look directly through the marble toward the fairway for stable, jitter-free framing
            Vector3 lookTarget = _target.position + (currentAimDir * 0.9f) + (Vector3.up * _lookAtHeightOffset);
            transform.LookAt(lookTarget);
        }

        private void OnGUI()
        {
            if (!_showDebugZones) return;

            float splitX = Screen.width * _cameraZoneSplitRatio;
            Texture2D tex = Texture2D.whiteTexture;
            Color prevColor = UnityEngine.GUI.color;

            // Left Camera Zone tint (translucent cyan)
            UnityEngine.GUI.color = new Color(0.1f, 0.6f, 1f, 0.12f);
            UnityEngine.GUI.DrawTexture(new Rect(0, 0, splitX, Screen.height), tex);

            // Right Aiming Zone tint (translucent amber)
            UnityEngine.GUI.color = new Color(1f, 0.6f, 0.1f, 0.12f);
            UnityEngine.GUI.DrawTexture(new Rect(splitX, 0, Screen.width - splitX, Screen.height), tex);

            // Boundary Divider Line
            UnityEngine.GUI.color = new Color(1f, 1f, 1f, 0.85f);
            UnityEngine.GUI.DrawTexture(new Rect(splitX - 1.5f, 0, 3f, Screen.height), tex);

            // Labels
            GUIStyle leftStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 18,
                fontStyle = FontStyle.Bold
            };
            leftStyle.normal.textColor = new Color(0.3f, 0.85f, 1f, 0.95f);

            GUIStyle rightStyle = new GUIStyle(leftStyle);
            rightStyle.normal.textColor = new Color(1f, 0.85f, 0.3f, 0.95f);

            UnityEngine.GUI.color = Color.white;
            UnityEngine.GUI.Label(new Rect(0, 30, splitX, 48), $"CAMERA CONTROL ZONE\n(Drag to Rotate | Yaw: {_manualOrbitAngle:F0}° Pitch: {_pitchAngle:F0}°)", leftStyle);
            string aimStateText = _isAimLocked ? "AIMING ACTIVE (CAMERA LOCKED)" : "DRAG TO AIM & SHOOT";
            UnityEngine.GUI.Label(new Rect(splitX, 30, Screen.width - splitX, 48), $"AIMING ZONE\n({aimStateText})", rightStyle);

            UnityEngine.GUI.color = prevColor;
        }
    }
}
