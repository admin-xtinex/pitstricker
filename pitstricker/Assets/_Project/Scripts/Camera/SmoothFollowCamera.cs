using UnityEngine;
using UnityEngine.InputSystem;

namespace PitStriker.CameraSystem
{
    public class SmoothFollowCamera : MonoBehaviour
    {
        public static SmoothFollowCamera Instance { get; private set; }

        [SerializeField] private Transform _target;
        [SerializeField] private float _distance = 4.8f;
        [SerializeField] private float _height = 2.6f;
        [SerializeField] private float _lookAtHeightOffset = 0.45f;
        [SerializeField] private float _smoothTime = 0.22f;
        [SerializeField] private float _orbitSensitivity = 0.25f;
        [SerializeField] private float _keyOrbitSpeed = 65.0f;

        private Vector3 _baseAimDirection = Vector3.forward;
        private float _manualOrbitAngle = 0f;
        private Vector3 _currentVelocity;
        private Vector2 _lastPointerPos;
        private bool _isOrbitDragging = false;
        private float _shakeTimer = 0f;
        private float _shakeIntensity = 0f;
        private Coroutine _scorePunchRoutine;
        private Coroutine _impactTrackCoroutine;

        public float ManualOrbitAngle => _manualOrbitAngle;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(gameObject);
        }

        public void TriggerImpactShake(float intensity = 0.25f, float duration = 0.2f)
        {
            _shakeIntensity = intensity;
            _shakeTimer = duration;
        }

        public void TriggerScorePunch()
        {
            TriggerImpactShake(0.10f, 0.28f);
            if (_scorePunchRoutine != null) StopCoroutine(_scorePunchRoutine);
            _scorePunchRoutine = StartCoroutine(ScorePunchRoutine());
        }

        private System.Collections.IEnumerator ScorePunchRoutine()
        {
            float startDist = _distance;
            float startHeight = _height;
            float t = 0f;
            while (t < 0.42f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 0.42f) * Mathf.PI);
                _distance = Mathf.Lerp(startDist, startDist * 0.84f, k);
                _height = Mathf.Lerp(startHeight, startHeight * 0.9f, k);
                yield return null;
            }
            _distance = startDist;
            _height = startHeight;
            _scorePunchRoutine = null;
        }

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
                _target = originalTarget;
            _impactTrackCoroutine = null;
        }

        public void SetTarget(Transform newTarget, Vector3? objectivePoint = null)
        {
            if (_impactTrackCoroutine != null)
            {
                StopCoroutine(_impactTrackCoroutine);
                _impactTrackCoroutine = null;
            }
            _target = newTarget;
            if (objectivePoint.HasValue && _target != null)
            {
                Vector3 toObj = objectivePoint.Value - _target.position;
                toObj.y = 0f;
                _baseAimDirection = toObj.sqrMagnitude > 0.04f ? toObj.normalized : Vector3.forward;
            }
            else _baseAimDirection = Vector3.forward;
            _manualOrbitAngle = 0f;
        }

        public void SetObjectiveTarget(Vector3 objectivePoint)
        {
            if (_target != null)
            {
                Vector3 toObj = objectivePoint - _target.position;
                toObj.y = 0f;
                if (toObj.sqrMagnitude > 0.04f) _baseAimDirection = toObj.normalized;
            }
            _manualOrbitAngle = 0f;
        }

        public void RotateOrbit(float deltaDegrees) { _manualOrbitAngle += deltaDegrees; }
        public void ResetOrbitToObjective() { _manualOrbitAngle = 0f; }

        private void Update() { HandleManualOrbitInput(); }

        private void HandleManualOrbitInput()
        {
            float keyInput = 0f;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.qKey.isPressed) keyInput -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed || Keyboard.current.eKey.isPressed) keyInput += 1f;
            }
            if (Mathf.Abs(keyInput) > 0.01f)
                _manualOrbitAngle += keyInput * _keyOrbitSpeed * Time.deltaTime;

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
                    _manualOrbitAngle += (currentPos.x - _lastPointerPos.x) * _orbitSensitivity;
                    _lastPointerPos = currentPos;
                }
                else if (Mouse.current.rightButton.wasReleasedThisFrame)
                    _isOrbitDragging = false;
            }

            if (Touchscreen.current != null && Touchscreen.current.touches.Count >= 2)
            {
                var touch1 = Touchscreen.current.touches[0];
                var touch2 = Touchscreen.current.touches[1];
                if (touch1.press.isPressed && touch2.press.isPressed)
                {
                    float avgDeltaX = (touch1.delta.ReadValue().x + touch2.delta.ReadValue().x) * 0.5f;
                    _manualOrbitAngle += avgDeltaX * _orbitSensitivity * 0.6f;
                }
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;
            Quaternion orbitRotation = Quaternion.Euler(0f, _manualOrbitAngle, 0f);
            Vector3 currentAimDir = orbitRotation * _baseAimDirection;
            currentAimDir.y = 0f;
            if (currentAimDir.sqrMagnitude < 0.001f) currentAimDir = Vector3.forward;
            currentAimDir.Normalize();
            Vector3 desiredPosition = _target.position - (currentAimDir * _distance) + (Vector3.up * _height);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref _currentVelocity, _smoothTime);
            if (_shakeTimer > 0f)
            {
                _shakeTimer -= Time.deltaTime;
                Vector3 shakeOffset = UnityEngine.Random.insideUnitSphere * _shakeIntensity;
                shakeOffset.y *= 0.6f;
                transform.position += shakeOffset;
            }
            Vector3 lookTarget = _target.position + (currentAimDir * 1.5f) + (Vector3.up * _lookAtHeightOffset);
            transform.LookAt(lookTarget);
        }
    }
}
