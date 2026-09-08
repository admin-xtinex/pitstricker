using UnityEngine;
using UnityEngine.InputSystem;
using PitStriker.Physics;

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
        [SerializeField] private float _maxDragDistance = 2.5f;

        [Tooltip("Maximum impulse force delivered to the marble at full power.")]
        [SerializeField] private float _maxLaunchForce = 18.0f;

        [Tooltip("If true, pull backward to shoot forward (Slingshot style). If false, push forward (Cue stick style).")]
        [SerializeField] private bool _invertPullToShoot = true;

        [Header("Trajectory Visualizer")]
        [Tooltip("Optional LineRenderer component used to draw the aim trajectory.")]
        [SerializeField] private LineRenderer _trajectoryLine;

        [Tooltip("Length of the visual trajectory guide at maximum power.")]
        [SerializeField] private float _maxVisualTrajectoryLength = 5.0f;

        // Cached References
        private MarbleController _marble;
        private Camera _mainCamera;

        // Drag State
        private bool _isDragging = false;
        private Vector3 _dragWorldStart;
        private Vector3 _currentDragWorldPoint;

        private void Awake()
        {
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

        private void Update()
        {
            // Do not allow aiming while the marble is currently rolling
            if (_marble.IsMoving)
            {
                if (_isDragging)
                {
                    CancelDrag();
                }
                return;
            }

            HandlePointerInput();
        }

        private void HandlePointerInput()
        {
            Vector2 screenPos = Vector2.zero;
            bool isPressed = false;
            bool justPressed = false;
            bool justReleased = false;

            // Query Input using New Input System with Touch & Mouse support
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                isPressed = true;
                justPressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
            {
                screenPos = Touchscreen.current.primaryTouch.position.ReadValue();
                justReleased = true;
            }
            else if (Mouse.current != null)
            {
                screenPos = Mouse.current.position.ReadValue();
                isPressed = Mouse.current.leftButton.isPressed;
                justPressed = Mouse.current.leftButton.wasPressedThisFrame;
                justReleased = Mouse.current.leftButton.wasReleasedThisFrame;
            }

            // 1. Pointer Down: Start Drag if touching near marble
            if (justPressed)
            {
                if (TryGetGroundPoint(screenPos, out Vector3 groundPoint))
                {
                    float distToMarble = Vector3.Distance(new Vector3(groundPoint.x, 0, groundPoint.z),
                                                          new Vector3(transform.position.x, 0, transform.position.z));

                    // Allow touch within 1.5 units of marble to start drag
                    if (distToMarble <= 1.5f)
                    {
                        _isDragging = true;
                        _dragWorldStart = groundPoint;
                        _currentDragWorldPoint = groundPoint;

                        if (_trajectoryLine != null)
                        {
                            _trajectoryLine.enabled = true;
                        }
                    }
                }
            }

            // 2. Pointer Dragging: Update aim trajectory
            if (_isDragging && isPressed)
            {
                if (TryGetGroundPoint(screenPos, out Vector3 groundPoint))
                {
                    _currentDragWorldPoint = groundPoint;
                    UpdateTrajectoryVisuals();
                }
            }

            // 3. Pointer Released: Execute launch
            if (_isDragging && justReleased)
            {
                ExecuteLaunch();
            }
        }

        private void UpdateTrajectoryVisuals()
        {
            if (_trajectoryLine == null) return;

            Vector3 pullVector = _dragWorldStart - _currentDragWorldPoint;
            pullVector.y = 0f;

            float pullDist = pullVector.magnitude;
            if (pullDist < _minDragDistance)
            {
                _trajectoryLine.enabled = false;
                return;
            }

            _trajectoryLine.enabled = true;

            // Direction to shoot
            Vector3 shootDir = _invertPullToShoot ? pullVector.normalized : -pullVector.normalized;
            float powerFraction = Mathf.Clamp01(pullDist / _maxDragDistance);
            float visualLength = powerFraction * _maxVisualTrajectoryLength;

            Vector3 startPos = transform.position;
            startPos.y += 0.05f; // Elevate slightly above ground to prevent z-fighting

            Vector3 endPos = startPos + (shootDir * visualLength);

            _trajectoryLine.SetPosition(0, startPos);
            _trajectoryLine.SetPosition(1, endPos);
        }

        private void ExecuteLaunch()
        {
            Vector3 pullVector = _dragWorldStart - _currentDragWorldPoint;
            pullVector.y = 0f;

            float pullDist = pullVector.magnitude;

            // If pulled less than deadzone threshold, treat as safe cancellation
            if (pullDist >= _minDragDistance)
            {
                Vector3 shootDir = _invertPullToShoot ? pullVector.normalized : -pullVector.normalized;
                float powerFraction = Mathf.Clamp01(pullDist / _maxDragDistance);
                float finalForce = powerFraction * _maxLaunchForce;

                _marble.ApplyImpulse(shootDir, finalForce);
            }

            CancelDrag();
        }

        private void CancelDrag()
        {
            _isDragging = false;
            if (_trajectoryLine != null)
            {
                _trajectoryLine.enabled = false;
            }
        }

        private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 worldGroundPoint)
        {
            worldGroundPoint = Vector3.zero;
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return false;
            }

            Ray ray = _mainCamera.ScreenPointToRay(screenPosition);
            // Create a mathematical plane at the marble's ground elevation
            UnityEngine.Plane groundPlane = new UnityEngine.Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

            if (groundPlane.Raycast(ray, out float enterDistance))
            {
                worldGroundPoint = ray.GetPoint(enterDistance);
                return true;
            }

            return false;
        }
    }
}
