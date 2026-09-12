using UnityEngine;

namespace PitStriker.CameraSystem
{
    /// <summary>
    /// Map-specific Camera A/B framing blend (Master Implementation Phase 7). Only ever
    /// added to a map's own Main Camera (e.g. Sunset Coastal) — Village's camera never
    /// gets this component, so its default SmoothFollowCamera behavior is unaffected.
    /// Drives SmoothFollowCamera's framing via its public setters, never its physics,
    /// and never blends while the player is actively aiming.
    /// </summary>
    [RequireComponent(typeof(SmoothFollowCamera))]
    public class MapCameraFramingBlender : MonoBehaviour
    {
        [Header("Camera A (near tee)")]
        [SerializeField] private float _distanceA = 4.8f;
        [SerializeField] private float _heightA = 2.6f;
        [SerializeField] private float _lookAtHeightOffsetA = 0.45f;

        [Header("Camera B (far pits)")]
        [SerializeField] private float _distanceB = 5.6f;
        [SerializeField] private float _heightB = 3.4f;
        [SerializeField] private float _lookAtHeightOffsetB = 0.65f;

        [Header("Transition Corridor (marble world Z)")]
        [SerializeField] private float _corridorStartZ = 12.0f;
        [SerializeField] private float _corridorEndZ = 20.0f;

        private SmoothFollowCamera _followCamera;
        private Transform _target;
        private float _lastBlend = -1f;

        public void ConfigureDefaults()
        {
            _distanceA = 4.8f; _heightA = 2.6f; _lookAtHeightOffsetA = 0.45f;
            _distanceB = 5.6f; _heightB = 3.4f; _lookAtHeightOffsetB = 0.65f;
            _corridorStartZ = 12.0f; _corridorEndZ = 20.0f;
        }

        private void Awake()
        {
            _followCamera = GetComponent<SmoothFollowCamera>();
            // Avoids a first-frame flash of Unity's async-shader-compile placeholder
            // (bright magenta) on materials that haven't rendered yet this session.
            Shader.WarmupAllShaders();
        }

        public void SetTrackedTarget(Transform target) => _target = target;

        private void Update()
        {
            if (_followCamera == null) return;
            if (_followCamera.IsAimLocked) return; // Never re-frame mid-aim.

            Transform target = _target;
            if (target == null) target = ResolveActiveTarget();
            if (target == null) return;

            float t = Mathf.InverseLerp(_corridorStartZ, _corridorEndZ, target.position.z);
            t = Mathf.SmoothStep(0f, 1f, t);
            if (Mathf.Approximately(t, _lastBlend)) return;
            _lastBlend = t;

            _followCamera.SetDistance(Mathf.Lerp(_distanceA, _distanceB, t));
            _followCamera.SetHeight(Mathf.Lerp(_heightA, _heightB, t));
            _followCamera.SetLookAtHeightOffset(Mathf.Lerp(_lookAtHeightOffsetA, _lookAtHeightOffsetB, t));
        }

        private Transform ResolveActiveTarget()
        {
            var marble = FindAnyObjectByType<PitStriker.Physics.MarbleController>();
            return marble != null ? marble.transform : null;
        }
    }
}
