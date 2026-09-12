using UnityEngine;
using UnityEngine.InputSystem;
using PitStriker.Physics;
using PitStriker.Gameplay;
using PitStriker.Networking;
using PitStriker.CameraSystem;

namespace PitStriker.Input
{
    /// <summary>
    /// Handles mobile touch and mouse drag-and-release swipe mechanics for launching marbles.
    /// Operates on the RIGHT ZONE of the screen in the TWO-ZONE layout:
    /// - Touching/dragging in the right zone controls marble aim direction and power.
    /// - Locks camera rotation during aiming.
    /// - Touches originating in the left zone are ignored here (handled by SmoothFollowCamera).
    /// - A gesture that begins in the right zone remains an aiming gesture even if it crosses to the left.
    /// </summary>
    public class SwipeLaunchController : MonoBehaviour
    {
        [Header("Two-Zone Screen Layout")]
        [Range(0.2f, 0.8f)]
        [Tooltip("Fraction of screen width (0.0 to 1.0) on the left dedicated to camera control. Right portion is for aiming.")]
        [SerializeField] private float _cameraZoneSplitRatio = 0.5f;

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

        // Drag & Aim State
        private bool _isDragging = false;
        private Vector2 _dragScreenStart;
        private float _dragStartTime;
        private float _currentPower = 0f;
        private Vector3 _shootDirection = Vector3.forward;

        // Two-Zone Touch & Mouse Tracking
        private int _aimTouchId = -1;
        private bool _isMouseAimDragging = false;

        public float CameraZoneSplitRatio => SmoothFollowCamera.Instance != null 
            ? SmoothFollowCamera.Instance.CameraZoneSplitRatio 
            : _cameraZoneSplitRatio;

        public bool IsAiming => _isDragging;

        private void Awake()
        {
            Instance = this;
            _marble = GetComponent<MarbleController>();
            _mainCamera = Camera.main;

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

            float splitX = Screen.width * CameraZoneSplitRatio;

            // Check if any touchscreen touch is actively in progress
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

            // 1. Prioritize Mobile Touchscreen
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
                        // Touch MUST start in the Aiming Zone (>= splitX)
                        if (pos.x >= splitX)
                        {
                            if (UnityEngine.EventSystems.EventSystem.current == null ||
                                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(touchId))
                            {
                                if (_aimTouchId == -1)
                                {
                                    _aimTouchId = touchId;
                                    StartAim(pos);
                                }
                            }
                        }
                    }
                    else if (isPressed && touchId == _aimTouchId)
                    {
                        // Continuous Aim Drag: Finger started in aim zone, continues aiming even if dragged left!
                        UpdateAim(pos);
                    }
                    else if (wasReleased && touchId == _aimTouchId)
                    {
                        _aimTouchId = -1;
                        ExecuteLaunch();
                    }
                }
            }
            // 2. Mouse / Trackpad (active when no touch is being processed)
            else if (Mouse.current != null)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();

                if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    // Mouse MUST start in the Aiming Zone (>= splitX)
                    if (mousePos.x >= splitX)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current == null ||
                            !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                        {
                            _isMouseAimDragging = true;
                            StartAim(mousePos);
                        }
                    }
                }
                else if (Mouse.current.leftButton.isPressed && _isMouseAimDragging)
                {
                    UpdateAim(mousePos);
                }
                else if (Mouse.current.leftButton.wasReleasedThisFrame && _isMouseAimDragging)
                {
                    _isMouseAimDragging = false;
                    ExecuteLaunch();
                }
            }
        }

        private void StartAim(Vector2 screenPos)
        {
            if (_marble != null && _marble.CurrentSpeed < 2.5f)
            {
                _marble.Halt();
            }

            _isDragging = true;
            _dragScreenStart = screenPos;
            _dragStartTime = Time.time;
            _currentPower = 0f;

            // Lock camera orientation immediately when player begins aiming
            if (SmoothFollowCamera.Instance != null)
            {
                SmoothFollowCamera.Instance.SetAimLocked(true);
            }
        }

        private void UpdateAim(Vector2 currentScreenPos)
        {
            if (!_isDragging) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector2 screenDelta = currentScreenPos - _dragScreenStart;

            if (_aimMode == AimMode.ForwardFlickThrow)
            {
                // Forward Flick Mode (Toss Phase): Upward swipe on screen throws forward
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

                    UpdateTrajectoryVisuals();
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

                    Vector2 aimScreenDir;
                    if (screenDelta.y < 0f)
                    {
                        // Slingshot pull: pulling down on screen shoots forward
                        aimScreenDir = -screenDelta.normalized;
                    }
                    else
                    {
                        // Forward flick/drag: pushing up shoots forward
                        aimScreenDir = screenDelta.normalized;
                    }

                    Vector3 camFwd = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
                    Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
                    _shootDirection = (camRight * aimScreenDir.x + camFwd * aimScreenDir.y).normalized;

                    UpdateTrajectoryVisuals();
                }
            }
        }

        private void UpdateTrajectoryVisuals()
        {
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

        private void DispatchLaunch(Vector3 direction, float force)
        {
            if (PitStriker.Networking.Client.CloudMatchManager.Instance != null &&
                PitStriker.Networking.Client.CloudMatchManager.Instance.IsOnlineMatchActive)
            {
                PitStriker.Networking.Client.CloudMatchManager.Instance.SubmitLocalShot(direction, force);
            }
            else if (NetworkSessionManager.Instance != null &&
                NetworkSessionManager.Instance.ActiveNetworkMode != NetworkSessionManager.NetworkMode.None &&
                NetworkMatchState.Instance != null)
            {
                NetworkMatchState.Instance.SubmitLocalShot(direction, force);
            }
            else
            {
                if (_marble != null)
                {
                    _marble.Halt();
                    _marble.ApplyImpulse(direction, force);
                }
            }
        }

        private void ExecuteLaunch()
        {
            if (_aimMode == AimMode.ForwardFlickThrow)
            {
                if (_currentPower > 0.05f)
                {
                    Vector3 dir = _shootDirection != Vector3.zero ? _shootDirection : Vector3.forward;
                    Vector3 throwDir = (dir + Vector3.up * 0.08f).normalized;
                    float force = _currentPower * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier;

                    DispatchLaunch(throwDir, force);
                    Debug.Log($"<color=#00FFAA><b>[FLICK THROW]</b> Forward swipe tossed marble with {_currentPower * 100:F0}% power ({force:F1} N)!</color>");
                }
            }
            else
            {
                if (_currentPower > 0.03f)
                {
                    float finalForce = _currentPower * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier;
                    DispatchLaunch(_shootDirection, finalForce);
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
            float strikeForce = power * _maxLaunchForce * GameDifficulty.LaunchForceMultiplier;

            DispatchLaunch(finalDir, strikeForce);
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
            UpdateTrajectoryVisuals();
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
            _aimTouchId = -1;
            _isMouseAimDragging = false;
            _currentPower = 0f;
            OnPowerChanged?.Invoke(0f);
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }

            // Return camera orientation control to free look
            if (SmoothFollowCamera.Instance != null)
            {
                SmoothFollowCamera.Instance.SetAimLocked(false);
            }
        }
    }
}
