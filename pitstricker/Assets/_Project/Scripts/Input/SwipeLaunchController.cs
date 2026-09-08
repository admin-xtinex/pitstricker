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
    [RequireComponent(typeof(MarbleController))]
    public class SwipeLaunchController : MonoBehaviour
    {
        [Header("Launch Physics Tuning")]
        [Tooltip("Minimum drag distance in world units required to register a stroke.")]
        [SerializeField] private float _minDragDistance = 0.2f;

        [Tooltip("Maximum drag distance in world units for 100% power.")]
        [SerializeField] private float _maxDragDistance = 3.5f;

        [Tooltip("Maximum impulse force delivered to the marble at full power.")]
        [SerializeField] private float _maxLaunchForce = 32.0f;

        [Tooltip("If true, pull backward to shoot forward (Slingshot style). If false, push forward (Cue stick style).")]
        [SerializeField] private bool _invertPullToShoot = true;

        [Header("Trajectory Visualizer")]
        [Tooltip("Optional LineRenderer component used to draw the aim trajectory.")]
        [SerializeField] private LineRenderer _trajectoryLine;

        [Tooltip("Length of the visual trajectory guide at maximum power.")]
        [SerializeField] private float _maxVisualTrajectoryLength = 9.0f;

        // Cached References
        public static SwipeLaunchController Instance { get; private set; }
        private MarbleController _marble;
        private Camera _mainCamera;

        // Events
        public static event System.Action<float> OnPowerChanged;

        // Drag State
        private bool _isDragging = false;
        private Vector2 _dragScreenStart;
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

            // Pointer Down: Start Drag anywhere on screen
            if (justPressed)
            {
                // If marble has residual drift, halt it so the player can aim cleanly
                if (_marble.CurrentSpeed < 2.5f)
                {
                    _marble.Halt();
                }

                _isDragging = true;
                _dragScreenStart = screenPos;
                _currentPower = 0f;
            }

            // Pointer Dragging: Calculate power and aim trajectory
            if (_isDragging && isPressed)
            {
                Vector2 screenDelta = screenPos - _dragScreenStart;
                float dragPixels = screenDelta.magnitude;

                float minPixels = Mathf.Max(10f, _minDragDistance * 60f); // Deadzone threshold to prevent accidental launches
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

                    // Direction to shoot
                    Vector2 aimScreenDir = _invertPullToShoot ? -screenDelta.normalized : screenDelta.normalized;

                    Vector3 camFwd = Vector3.ProjectOnPlane(_mainCamera.transform.forward, Vector3.up).normalized;
                    Vector3 camRight = Vector3.ProjectOnPlane(_mainCamera.transform.right, Vector3.up).normalized;
                    _shootDirection = (camRight * aimScreenDir.x + camFwd * aimScreenDir.y).normalized;

                    if (_trajectoryLine != null)
                    {
                        _trajectoryLine.enabled = true;
                        Vector3 marblePos = _marble != null ? _marble.transform.position : transform.position;
                        Vector3 startPos = marblePos + (Vector3.up * 0.05f);
                        Vector3 endPos = startPos + (_shootDirection * (_currentPower * _maxVisualTrajectoryLength));

                        _trajectoryLine.SetPosition(0, startPos);
                        _trajectoryLine.SetPosition(1, endPos);
                    }
                }
            }

            // Pointer Released: Execute launch
            if (_isDragging && justReleased)
            {
                ExecuteLaunch();
            }
        }

        private void ExecuteLaunch()
        {
            if (_currentPower > 0.03f)
            {
                float finalForce = _currentPower * _maxLaunchForce;
                _marble.ApplyImpulse(_shootDirection, finalForce);
                Debug.Log($"<color=#00FFAA><b>[SWIPE LAUNCH]</b> Launched with {_currentPower * 100:F0}% power ({finalForce:F1} N)!</color>");
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

            _marble.Halt();
            _marble.ApplyImpulse(dir, power * _maxLaunchForce);
            Debug.Log($"<color=#00FFAA><b>[STRIKE BUTTON]</b> Launched with {power * 100:F0}% power ({power * _maxLaunchForce:F1} N) towards {dir}!</color>");
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
