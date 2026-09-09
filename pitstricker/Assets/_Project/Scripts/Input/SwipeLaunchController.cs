using UnityEngine;
using UnityEngine.InputSystem;
using PitStriker.Physics;
using PitStriker.Gameplay;

namespace PitStriker.Input
{
    /// <summary>
    /// Handles mobile touch and mouse drag-and-release swipe mechanics for launching marbles.
    /// Draws an aiming trajectory indicator on the ground plane.
    /// </summary>
    public class SwipeLaunchController : MonoBehaviour
    {
        [Header("Launch Physics Tuning")]
        [Tooltip("Minimum drag distance in world units required to register a stroke.")]
        [SerializeField] private float _minDragDistance = 0.2f;

        [Tooltip("Maximum drag distance in world units for 100% power.")]
        [SerializeField] private float _maxDragDistance = 3.5f;

        [Tooltip("Maximum impulse force delivered to the marble at full power.")]
        [SerializeField] private float _maxLaunchForce = 32.0f;

        [Header("Trajectory Visualizer")]
        [Tooltip("Optional LineRenderer component used to draw the aim trajectory.")]
        [SerializeField] private LineRenderer _trajectoryLine;

        [Tooltip("Length of the visual trajectory guide at maximum power.")]
        [SerializeField] private float _maxVisualTrajectoryLength = 9.0f;

        public enum AimMode
        {
            PrecisionPullBack,  // Slingshot pull-back aiming for tactical gameplay
            ForwardFlickThrow   // Fast upward flick throwing gesture for the Opening Toss Phase
        }

        [Header("Aiming Mode")]
        [SerializeField] private AimMode _aimMode = AimMode.PrecisionPullBack;
        public AimMode CurrentAimMode => _aimMode;

        // Cached References
        public static SwipeLaunchController Instance { get; private set; }
        private MarbleController _marble;
        private Camera _mainCamera;

        // Events
        public static event System.Action<float> OnPowerChanged;

        // Drag & Flick State
        private bool _isDragging = false;
        private Vector2 _dragScreenStart;
        private float _dragStartTime;
        private float _currentPower = 0f;
        private Vector3 _shootDirection = Vector3.forward;

        private void Awake()
        {
            Instance = this;
            _marble = GetComponent<MarbleController>();
            _mainCamera = Camera.main;

            // Ensure LineRenderer has clean defaults if attached
            if (_trajectoryLine == null)
            {
                _trajectoryLine = GetComponent<LineRenderer>();
            }

            if (_trajectoryLine != null)
            {
                _trajectoryLine.positionCount = 2;
                _trajectoryLine.enabled = false;
            }
        }

        public void SetAimMode(AimMode mode)
        {
            _aimMode = mode;
            CancelDrag();
        }

        public void SetActiveMarble(MarbleController newMarble)
        {
            _marble = newMarble;
            CancelDrag();

            if (_trajectoryLine != null && _marble != null)
            {
                MeshRenderer mr = _marble.GetComponent<MeshRenderer>();
                if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.HasProperty("_BaseColor"))
                {
                    Color c = mr.sharedMaterial.GetColor("_BaseColor");
                    _trajectoryLine.startColor = new Color(c.r, c.g, c.b, 0.95f);
                    _trajectoryLine.endColor = new Color(c.r, c.g, c.b, 0.25f);
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_marble == null) return;

            // Keyboard shortcut test launch for instant testing (Spacebar)
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (!TurnManager.CanAim()) return;

                _marble.Halt();
                _marble.ApplyImpulse(Vector3.forward, 22.0f);
                Debug.Log("<color=#00FFAA><b>[TEST LAUNCH]</b> Spacebar pressed! Marble launched forward with 22N force.</color>");
                return;
            }

            HandlePointerInput();
        }

        private void HandlePointerInput()
        {
            if (!TurnManager.CanAim())
            {
                if (_isDragging) CancelDrag();
                return;
            }

            Vector2 screenPos = Vector2.zero;
            bool isPressed = false;
            bool justPressed = false;
            bool justReleased = false;

            // 1. Prioritize Touchscreen if active
            if (Touchscreen.current != null && (Touchscreen.current.primaryTouch.press.isPressed || Touchscreen.current.primaryTouch.press.wasPressedThisFrame || Touchscreen.current.primaryTouch.press.wasReleasedThisFrame))
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                isPressed = Touchscreen.current.primaryTouch.press.isPressed;
                justPressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
                justReleased = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
            }
            // 2. Mouse / Trackpad
            else if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                justPressed = Mouse.current.leftButton.wasPressedThisFrame;
                justReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            // Pointer Down: Start Drag anywhere on screen (unless tapping over a UI element)
            if (justPressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null)
                {
                    if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    {
                        int touchId = Touchscreen.current.primaryTouch.touchId.ReadValue();
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId)) return;
                    }
                    else if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        return;
                    }
                }

                if (_marble.CurrentSpeed < 2.5f)
                {
                    _marble.Halt();
                }

                _isDragging = true;
                _dragScreenStart = screenPos;
                _dragStartTime = Time.time;
                _currentPower = 0f;
            }

            // Pointer Dragging / Flicking
            if (_isDragging && isPressed)
            {
                Vector2 screenDelta = screenPos - _dragScreenStart;

                if (_aimMode == AimMode.ForwardFlickThrow)
                {
                    // Forward Flick Mode (Toss Phase): Upward swipe on screen means forward throw
                    float dt = Mathf.Max(0.001f, Time.time - _dragStartTime);
                    float flickSpeed = screenDelta.magnitude / dt;

                    if (screenDelta.y > 15f)
                    {
                        _currentPower = Mathf.Clamp01(flickSpeed / (Screen.height * 1.5f));
                        OnPowerChanged?.Invoke(_currentPower);

                        Vector3 camFwd = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
                        Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
                        Vector2 aimDir = screenDelta.normalized;
                        _shootDirection = (camRight * aimDir.x + camFwd * aimDir.y).normalized;

                        if (_trajectoryLine != null)
                        {
                            _trajectoryLine.enabled = true;
                            Vector3 marblePos = _marble != null ? _marble.transform.position : transform.position;
                            Vector3 startPos = marblePos + (Vector3.up * 0.05f);
                            Vector3 endPos = startPos + (_shootDirection * (Mathf.Max(0.3f, _currentPower) * _maxVisualTrajectoryLength * GameDifficulty.TrajectoryLengthMultiplier));

                            _trajectoryLine.SetPosition(0, startPos);
                            _trajectoryLine.SetPosition(1, endPos);
                        }
                    }
                    else
                    {
                        _currentPower = 0f;
                        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
                        OnPowerChanged?.Invoke(0f);
                    }
                }
                else
                {
                    // Precision Slingshot Mode (Main Match): Pull backward to shoot forward
                    float dragPixels = screenDelta.magnitude;
                    float minPixels = Mathf.Max(10f, _minDragDistance * 60f);
                    float maxPixels = Mathf.Clamp(_maxDragDistance * 80f, 160f, Screen.height * 0.45f);

                    if (dragPixels < minPixels)
                    {
                        _currentPower = 0f;
                        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
                        OnPowerChanged?.Invoke(0f);
                    }
                    else
                    {
                        _currentPower = Mathf.Clamp01((dragPixels - minPixels) / (maxPixels - minPixels));
                        OnPowerChanged?.Invoke(_currentPower);

                        // Intuitive aim:
                        // If pulled backward (screenDelta.y < 0), invert to shoot forward (slingshot pull).
                        // If swiped forward (screenDelta.y > 0), follow forward drag to shoot forward (direct flick).
                        Vector2 aimScreenDir;
                        if (screenDelta.y < 0f)
                        {
                            // Pull-back slingshot: pull down on screen shoots forward
                            aimScreenDir = -screenDelta.normalized;
                        }
                        else
                        {
                            // Forward swipe/flick: push up on screen shoots forward
                            aimScreenDir = screenDelta.normalized;
                        }

                        Vector3 camFwd = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
                        Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
                        _shootDirection = (camRight * aimScreenDir.x + camFwd * aimScreenDir.y).normalized;

                        if (_trajectoryLine != null)
                        {
                            _trajectoryLine.enabled = true;
                            Vector3 marblePos = _marble != null ? _marble.transform.position : transform.position;
                            Vector3 startPos = marblePos + (Vector3.up * 0.05f);
                            Vector3 endPos = startPos + (_shootDirection * (_currentPower * _maxVisualTrajectoryLength * GameDifficulty.TrajectoryLengthMultiplier));

                            _trajectoryLine.SetPosition(0, startPos);
                            _trajectoryLine.SetPosition(1, endPos);
                        }
                    }
                }
            }

            // Pointer Released: Execute Launch or Flick
            if (_isDragging && justReleased)
            {
                ExecuteLaunch();
            }
        }

        private void ExecuteLaunch()
        {
            if (_aimMode == AimMode.ForwardFlickThrow)
            {
                // Forward Flick Throw Execution
                if (_currentPower > 0.05f)
                {
                    Vector3 dir = _shootDirection != Vector3.zero ? _shootDirection : Vector3.forward;
                    Vector3 throwDir = (dir + Vector3.up * 0.08f).normalized; // Natural upward lob
                    float force = _currentPower * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier;

                    _marble.Halt();
                    _marble.ApplyImpulse(throwDir, force);
                    Debug.Log($"<color=#00FFAA><b>[FLICK THROW]</b> Forward swipe tossed marble with {_currentPower * 100:F0}% power ({force:F1} N)!</color>");
                }
            }
            else
            {
                // Precision Slingshot Execution
                if (_currentPower > 0.03f)
                {
                    float finalForce = _currentPower * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier;
                    _marble.ApplyImpulse(_shootDirection, finalForce);
                    Debug.Log($"<color=#00FFAA><b>[PRECISION STRIKE]</b> Launched with {_currentPower * 100:F0}% power ({finalForce:F1} N)!</color>");
                }
            }

            CancelDrag();
        }

        /// <summary>
        /// Allows UI buttons (like the STRIKE button) to trigger a launch towards the aimed or forward direction.
        /// </summary>
        public void LaunchStrike(float powerFraction = -1f)
        {
            if (!TurnManager.CanAim()) return;

            float power = powerFraction >= 0f ? powerFraction : (_currentPower > 0.05f ? _currentPower : 0.65f);
            Vector3 dir = _shootDirection != Vector3.zero ? _shootDirection : Vector3.forward;
            Vector3 finalDir = _aimMode == AimMode.ForwardFlickThrow ? (dir + Vector3.up * 0.08f).normalized : dir;

            _marble.Halt();
            _marble.ApplyImpulse(finalDir, power * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier);
            Debug.Log($"<color=#00FFAA><b>[STRIKE BUTTON]</b> Executed launch with {power * 100:F0}% power towards {finalDir}!</color>");
            CancelDrag();
        }

        /// <summary>
        /// Renders the visual trajectory aim guide and power bar during autonomous AI turns or guided tutorials.
        /// </summary>
        public void ShowAimPreview(Vector3 direction, float power01)
        {
            _shootDirection = direction;
            _currentPower = Mathf.Clamp01(power01);

            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = true;
                Vector3 marblePos = _marble != null ? _marble.transform.position : transform.position;
                Vector3 startPos = marblePos + (Vector3.up * 0.05f);
                Vector3 endPos = startPos + (_shootDirection * (Mathf.Max(0.3f, _currentPower) * _maxVisualTrajectoryLength * GameDifficulty.TrajectoryLengthMultiplier));

                _trajectoryLine.SetPosition(0, startPos);
                _trajectoryLine.SetPosition(1, endPos);
            }

            OnPowerChanged?.Invoke(_currentPower);
        }

        /// <summary>
        /// Hides the visual aim line and zeroes the power meter.
        /// </summary>
        public void HideAimPreview()
        {
            CancelDrag();
        }

        private void CancelDrag()
        {
            _isDragging = false;
            _currentPower = 0f;
            OnPowerChanged?.Invoke(0f);
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
        }
    }
}
