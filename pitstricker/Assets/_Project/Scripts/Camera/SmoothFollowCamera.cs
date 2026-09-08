using UnityEngine;
using UnityEngine.InputSystem;

namespace PitStriker.CameraSystem
{
    /// <summary>
    /// Smoothly follows the active marble and dynamically aligns the camera view
    /// behind the marble facing directly towards the target pit objective.
    /// Also supports manual camera orbit adjustment via mouse right-drag, touch swipe, or A/D keys.
    /// </summary>
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (typically the active marble).")]
        [SerializeField] private Transform _target;

        [Header("Framing Distance & Elevation")]
        [Tooltip("Distance behind the marble along the aim vector.")]
        [SerializeField] private float _distance = 4.8f;

        [Tooltip("Height of the camera above the ground plane.")]
        [SerializeField] private float _height = 2.6f;

        [Tooltip("Look-at height offset above target pivot.")]
        [SerializeField] private float _lookAtHeightOffset = 0.45f;

        [Header("Smoothing")]
        [Tooltip("Smooth time for position damping (lower is snappier, higher is smoother).")]
        [SerializeField] private float _smoothTime = 0.22f;

        [Header("Manual Orbit Controls")]
        [SerializeField] private float _orbitSensitivity = 0.25f;
        [SerializeField] private float _keyOrbitSpeed = 65.0f;

        // Alignment & Orbit State
        private Vector3 _baseAimDirection = Vector3.forward;
        private float _manualOrbitAngle = 0f;
        private Vector3 _currentVelocity;
        private Vector2 _lastPointerPos;
        private bool _isOrbitDragging = false;

        public float ManualOrbitAngle => _manualOrbitAngle;

        public void SetTarget(Transform newTarget, Vector3? objectivePoint = null)
        {
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
            else
            {
                _baseAimDirection = Vector3.forward;
            }

            _manualOrbitAngle = 0f; // Automatically align straight to the objective pit!
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
            _manualOrbitAngle = 0f;
        }

        public void RotateOrbit(float deltaDegrees)
        {
            _manualOrbitAngle += deltaDegrees;
        }

        public void ResetOrbitToObjective()
        {
            _manualOrbitAngle = 0f;
        }

        private void Update()
        {
            HandleManualOrbitInput();
        }

        private void HandleManualOrbitInput()
        {
            // Keyboard A/D or Left/Right Arrow camera orbit
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
                _manualOrbitAngle += keyInput * _keyOrbitSpeed * Time.deltaTime;
            }

            // Mouse Right-Click drag to orbit camera around the marble
            if (Mouse.current != null)
            {
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    _isOrbitDragging = true;
                    _lastPointerPos = Mouse.current.position.ReadValue();
                }
                else if (Mouse.current.rightButton.isPressed && _isOrbitDragging)
                {
                    Vector2 currentPos = Mouse.current.position.ReadValue();
                    float deltaX = currentPos.x - _lastPointerPos.x;
                    _manualOrbitAngle += deltaX * _orbitSensitivity;
                    _lastPointerPos = currentPos;
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                {
                    _isOrbitDragging = false;
                }
            }

            // Mobile Touch: 2-finger horizontal drag to orbit camera
            if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
            {
                var touch1 = Touchscreen.current.touches[0];
                var touch2 = Touchscreen.current.touches[1];

                if (touch1.press.isPressed && touch2.press.isPressed)
                {
                    Vector2 delta1 = touch1.delta.ReadValue();
                    Vector2 delta2 = touch2.delta.ReadValue();
                    float avgDeltaX = (delta1.x + delta2.x) * 0.5f;
                    _manualOrbitAngle += avgDeltaX * _orbitSensitivity * 0.6f;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Calculate current aim vector based on objective direction + manual orbit angle
            Quaternion orbitRotation = Quaternion.Euler(0f, _manualOrbitAngle, 0f);
            Vector3 currentAimDir = orbitRotation * _baseAimDirection;
            currentAimDir.y = 0f;
            if (currentAimDir.sqrMagnitude < 0.001f) currentAimDir = Vector3.forward;
            currentAimDir.Normalize();

            // Camera sits behind the marble along the aim vector
            Vector3 desiredPosition = _target.position - (currentAimDir * _distance) + (Vector3.up * _height);

            // Smoothly interpolate camera position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _smoothTime);

            // Look at point ahead through the marble toward the target
            Vector3 lookTarget = _target.position + (currentAimDir * 1.5f) + (Vector3.up * _lookAtHeightOffset);
            transform.LookAt(lookTarget);
        }
    }
}
