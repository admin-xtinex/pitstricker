using UnityEngine;
using PitStriker.Physics;

namespace PitStriker.VFX
{
    [DefaultExecutionOrder(40)]
    public class MarbleMotionJuice : MonoBehaviour
    {
        private MarbleController _marble;
        private TrailRenderer _trail;
        private float _dustTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            foreach (var marble in Object.FindObjectsByType<MarbleController>(FindObjectsSortMode.None))
            {
                if (marble.GetComponent<MarbleMotionJuice>() == null)
                    marble.gameObject.AddComponent<MarbleMotionJuice>();
            }
        }

        private void Awake()
        {
            _marble = GetComponent<MarbleController>();
            _trail = GetComponent<TrailRenderer>();
            if (_trail == null) _trail = gameObject.AddComponent<TrailRenderer>();
            _trail.time = 0.32f;
            _trail.minVertexDistance = 0.04f;
            _trail.startWidth = 0.07f;
            _trail.endWidth = 0.01f;
            _trail.numCapVertices = 4;
            _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _trail.receiveShadows = false;
            _trail.emitting = false;
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color");
            if (shader != null)
            {
                var mat = new Material(shader) { color = new Color(1f, 0.92f, 0.55f, 0.55f) };
                _trail.material = mat;
            }
            _trail.startColor = new Color(1f, 0.95f, 0.7f, 0.55f);
            _trail.endColor = new Color(1f, 0.8f, 0.3f, 0f);
        }

        private void LateUpdate()
        {
            if (_marble == null || _trail == null) return;
            float speed = _marble.CurrentSpeed;
            bool show = !_marble.IsRetired && speed > 0.55f;
            _trail.emitting = show;
            _trail.startWidth = Mathf.Lerp(0.03f, 0.10f, Mathf.Clamp01(speed / 8f));
            _dustTimer -= Time.deltaTime;
            if (show && speed > 1.8f && _dustTimer <= 0f && VFXManager.Instance != null)
            {
                _dustTimer = 0.12f;
                VFXManager.Instance.PlayRollDust(transform.position, _marble.Velocity.normalized, Mathf.Clamp01(speed / 10f));
            }
        }
    }
}
