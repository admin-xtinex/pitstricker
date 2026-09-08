using UnityEngine;

namespace PitStriker.CameraSystem
{
    /// <summary>
    /// Smoothly follows the active marble using damping to create cinematic, low-angle camera framing
    /// matching the Pit Striker concept art.
    /// </summary>
    public class SmoothFollowCamera : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Target transform to follow (typically the active marble).")]
        [SerializeField] private Transform _target;

        [Header("Framing & Offsets")]
        [Tooltip("Camera offset relative to the target in world coordinates.")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 2.8f, -4.8f);

        [Tooltip("Look-at height offset above target pivot.")]
        [SerializeField] private float _lookAtHeightOffset = 0.4f;

        [Header("Smoothing")]
        [Tooltip("Smooth time for position damping (lower is snappier, higher is smoother).")]
        [SerializeField] private float _smoothTime = 0.25f;

        private Vector3 _currentVelocity;

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            // Compute target position with offset
            Vector3 desiredPosition = _target.position + _offset;

            // Smoothly interpolate camera position
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _smoothTime);

            // Look at target point slightly elevated above ground
            Vector3 lookTarget = _target.position + (Vector3.up * _lookAtHeightOffset);
            transform.LookAt(lookTarget);
        }
    }
}
