using System.Collections;
using UnityEngine;
using PitStriker.Gameplay;
using PitStriker.Physics;

namespace PitStriker.Visuals
{
    /// <summary>
    /// Stylized pit feedback and celebratory squash-and-stretch pulse.
    /// Operates directly on the existing hollow pit lip ring (BrightRed_PitLip)
    /// without obstructing the open 3D earthen saucer basin or marble entrance.
    /// </summary>
    [ExecuteAlways]
    public class StylizedPitFeedback : MonoBehaviour
    {
        [Header("Stylized Rim Appearance")]
        [SerializeField] private int _pitNumber = 1;

        public int PitNumber
        {
            get => _pitZone != null ? _pitZone.PitNumber : _pitNumber;
            set => _pitNumber = value;
        }

        private PitZone _pitZone;
        private Transform _rimTransform;
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            CleanObsoleteBlockers();
            SetupRimReference();
        }

        private void OnEnable()
        {
            CleanObsoleteBlockers();
            SetupRimReference();
            PitZone.OnMarbleSunk += HandleMarbleSunk;
        }

        private void OnDisable()
        {
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
        }

        public void InitInEditor()
        {
            CleanObsoleteBlockers();
            SetupRimReference();
        }

        /// <summary>
        /// Cleans up any legacy or solid primitive blockers (e.g. solid cylinder discs)
        /// to ensure the 3D pit saucer cavity remains 100% open and visible.
        /// </summary>
        public void CleanObsoleteBlockers()
        {
            Transform ceramicRim = transform.Find("Stylized_Ceramic_Rim");
            if (ceramicRim != null)
            {
                if (Application.isPlaying) Destroy(ceramicRim.gameObject);
                else DestroyImmediate(ceramicRim.gameObject);
            }

            Transform badge = transform.Find("PitNumber_Badge");
            if (badge != null)
            {
                if (Application.isPlaying) Destroy(badge.gameObject);
                else DestroyImmediate(badge.gameObject);
            }
        }

        private void SetupRimReference()
        {
            _pitZone = GetComponent<PitZone>();
            if (_pitZone != null) _pitNumber = _pitZone.PitNumber;

            // Target the open circular lip ring mesh generated on the pit
            _rimTransform = transform.Find("BrightRed_PitLip");
            if (_rimTransform != null)
            {
                _baseScale = _rimTransform.localScale;
            }
            else
            {
                _baseScale = Vector3.one;
            }
        }

        private static Mesh _rippleMesh;
        private static Material _rippleMaterial;

        private static Mesh GetRippleRingMesh()
        {
            if (_rippleMesh == null)
            {
                _rippleMesh = new Mesh();
                _rippleMesh.name = "Pit_Celebration_Ripple_Mesh";
                const int segments = 36;
                Vector3[] verts = new Vector3[segments * 2];
                int[] tris = new int[segments * 6];

                float rIn = 0.48f;
                float rOut = 0.62f;

                for (int i = 0; i < segments; i++)
                {
                    float angle = (i / (float)segments) * Mathf.PI * 2f;
                    float c = Mathf.Cos(angle);
                    float s = Mathf.Sin(angle);

                    verts[i * 2] = new Vector3(c * rIn, 0.015f, s * rIn);
                    verts[i * 2 + 1] = new Vector3(c * rOut, 0.015f, s * rOut);

                    int next = (i + 1) % segments;
                    int i0 = i * 2;
                    int i1 = i * 2 + 1;
                    int i2 = next * 2;
                    int i3 = next * 2 + 1;

                    tris[i * 6] = i0;
                    tris[i * 6 + 1] = i1;
                    tris[i * 6 + 2] = i2;

                    tris[i * 6 + 3] = i2;
                    tris[i * 6 + 4] = i1;
                    tris[i * 6 + 5] = i3;
                }

                _rippleMesh.vertices = verts;
                _rippleMesh.triangles = tris;
                _rippleMesh.RecalculateNormals();
                _rippleMesh.RecalculateBounds();
            }
            return _rippleMesh;
        }

        private static Material GetRippleMaterial()
        {
            if (_rippleMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Standard");

                _rippleMaterial = new Material(shader);
                _rippleMaterial.name = "M_PitRipple_Gold";
                if (_rippleMaterial.HasProperty("_Surface")) _rippleMaterial.SetFloat("_Surface", 1); // Transparent
                if (_rippleMaterial.HasProperty("_Blend")) _rippleMaterial.SetFloat("_Blend", 0); // Alpha blend
                _rippleMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                _rippleMaterial.renderQueue = 3000;
            }
            return _rippleMaterial;
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (pit == _pitZone || (pit != null && pit.PitNumber == PitNumber))
            {
                StopAllCoroutines();
                StartCoroutine(CapturePulseRoutine());
                StartCoroutine(GroundRippleRoutine());
            }
        }

        private IEnumerator GroundRippleRoutine()
        {
            GameObject rippleObj = new GameObject("VFX_PitGroundRipple");
            rippleObj.transform.position = transform.position;
            rippleObj.transform.rotation = Quaternion.identity;

            MeshFilter mf = rippleObj.AddComponent<MeshFilter>();
            mf.sharedMesh = GetRippleRingMesh();

            MeshRenderer mr = rippleObj.AddComponent<MeshRenderer>();
            Material instMat = new Material(GetRippleMaterial());
            mr.sharedMaterial = instMat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            float elapsed = 0f;
            const float duration = 0.65f;
            Color goldStart = new Color(1f, 0.88f, 0.25f, 0.85f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Expand smoothly from 1.0 to 2.4 scale
                float scale = Mathf.Lerp(1.0f, 2.45f, Mathf.Sqrt(t));
                rippleObj.transform.localScale = new Vector3(scale, 1.0f, scale);

                // Fade out smoothly
                float alpha = (1f - t) * (1f - t);
                Color c = goldStart;
                c.a = alpha * 0.85f;
                if (instMat.HasProperty("_BaseColor")) instMat.SetColor("_BaseColor", c);
                if (instMat.HasProperty("_Color")) instMat.SetColor("_Color", c);

                yield return null;
            }

            Destroy(rippleObj);
            Destroy(instMat);
        }

        private IEnumerator CapturePulseRoutine()
        {
            if (_rimTransform == null)
            {
                _rimTransform = transform.Find("BrightRed_PitLip");
                if (_rimTransform == null) yield break;
                _baseScale = _rimTransform.localScale;
            }

            float elapsed = 0f;
            const float duration = 0.55f;

            // Elastic cartoon scale pulse on the lip ring: 1.0 -> 1.30 -> 0.90 -> 1.08 -> 1.0
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 3f) * 0.30f * (1f - t);
                float squashY = 1f / Mathf.Sqrt(Mathf.Max(0.2f, pulse));

                _rimTransform.localScale = new Vector3(_baseScale.x * pulse, _baseScale.y * squashY, _baseScale.z * pulse);
                yield return null;
            }

            _rimTransform.localScale = _baseScale;
        }
    }
}
