using System.Collections;
using UnityEngine;
using PitStriker.Gameplay;
using PitStriker.Physics;

namespace PitStriker.Visuals
{
    /// <summary>
    /// Animated cartoon numbered flag with procedural cloth ripple,
    /// secondary wind flutter, flagpole elastic spring bounce, and 360 victory spin on pit score.
    /// Exaggerated proportions for clear readability on mobile screens.
    /// </summary>
    [ExecuteAlways]
    public class StylizedFlagAnimation : MonoBehaviour
    {
        [Header("Flag Configuration")]
        [SerializeField] private int _pitNumber = 1;
        [SerializeField] private Color _flagColor = new Color(0.92f, 0.22f, 0.22f); // Vibrant red
        [SerializeField] private Color _poleColor = new Color(0.95f, 0.82f, 0.35f); // Golden brass

        [Header("Cloth Wave Dynamics")]
        [SerializeField] private float _waveSpeed = 4.5f;
        [SerializeField] private float _waveFrequency = 3.2f;
        [SerializeField] private float _waveAmplitude = 0.08f;

        // Visual Hierarchy
        private Transform _poleTransform;
        private Transform _flagClothTransform;
        private MeshFilter _clothMeshFilter;
        private Mesh _clothMeshInstance;
        private Vector3[] _baseClothVertices;
        private Vector3[] _animatedClothVertices;

        // Flagpole Spring State
        private float _poleAngle = 0f;
        private float _poleAngleVel = 0f;
        private Vector3 _poleTiltAxis = Vector3.forward;

        private void Awake()
        {
            Transform existingPole = transform.Find("Pole");
            Transform existingCloth = transform.Find("FlagCloth");
            if (existingPole != null && existingCloth != null)
            {
                _poleTransform = existingPole;
                _flagClothTransform = existingCloth;
                _clothMeshFilter = _flagClothTransform.GetComponent<MeshFilter>();
                if (_clothMeshFilter != null && _clothMeshFilter.sharedMesh != null && _clothMeshFilter.sharedMesh.vertexCount > 0)
                {
                    _clothMeshInstance = _clothMeshFilter.sharedMesh;
                    _baseClothVertices = _clothMeshInstance.vertices;
                    _animatedClothVertices = new Vector3[_baseClothVertices.Length];
                }
                else
                {
                    CreateClothGridMesh();
                }
            }
            else
            {
                BuildFlagVisuals();
            }
        }

        public void InitInEditor()
        {
            if (transform.Find("Pole") == null)
            {
                BuildFlagVisuals();
            }
        }

        private void OnEnable()
        {
            PitZone.OnMarbleSunk += HandleMarbleSunk;
            MarbleController.OnMarbleHitMarble += HandleMarbleImpact;
        }

        private void OnDisable()
        {
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
            MarbleController.OnMarbleHitMarble -= HandleMarbleImpact;
        }

        private void BuildFlagVisuals()
        {
            Shader toonShader = Shader.Find("Pit Striker/Stylized Toon PBR");
            if (toonShader == null) toonShader = Shader.Find("Universal Render Pipeline/Lit");

            Material poleMat = new Material(toonShader);
            poleMat.color = _poleColor;
            poleMat.SetFloat("_Metallic", 0.6f);
            poleMat.SetFloat("_Smoothness", 0.85f);

            Material flagMat = new Material(toonShader);
            flagMat.color = _flagColor;
            flagMat.SetFloat("_Smoothness", 0.45f);
            flagMat.SetFloat("_Cull", 0f); // Double-sided

            // Pole
            GameObject poleObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            poleObj.name = "Pole";
            poleObj.transform.SetParent(transform, false);
            poleObj.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            poleObj.transform.localScale = new Vector3(0.08f, 1.25f, 0.08f);
            StripCollider(poleObj);
            poleObj.GetComponent<Renderer>().sharedMaterial = poleMat;
            _poleTransform = poleObj.transform;

            // Golden Finial Ball at Top
            GameObject finial = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            finial.name = "Finial";
            finial.transform.SetParent(_poleTransform, false);
            finial.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            finial.transform.localScale = new Vector3(3.2f, 0.22f, 3.2f);
            StripCollider(finial);
            finial.GetComponent<Renderer>().sharedMaterial = poleMat;

            // Cloth Flag Plane (Tessellated Grid for Wave Ripple)
            GameObject clothObj = new GameObject("FlagCloth");
            clothObj.transform.SetParent(transform, false);
            clothObj.transform.localPosition = new Vector3(0.04f, 1.95f, 0f);
            _flagClothTransform = clothObj.transform;

            _clothMeshFilter = clothObj.AddComponent<MeshFilter>();
            MeshRenderer mr = clothObj.AddComponent<MeshRenderer>();
            mr.sharedMaterial = flagMat;

            CreateClothGridMesh();
        }

        private static void StripCollider(GameObject go)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
            {
                if (Application.isPlaying) Destroy(col);
                else DestroyImmediate(col);
            }
        }

        private void CreateClothGridMesh()
        {
            const int resX = 10;
            const int resY = 6;
            const float width = 0.85f;
            const float height = 0.55f;

            _clothMeshInstance = new Mesh();
            _clothMeshInstance.name = "FlagCloth_Instance";

            Vector3[] verts = new Vector3[(resX + 1) * (resY + 1)];
            Vector2[] uvs = new Vector2[verts.Length];
            int[] tris = new int[resX * resY * 12];

            int vIdx = 0;
            for (int y = 0; y <= resY; y++)
            {
                for (int x = 0; x <= resX; x++)
                {
                    float u = (float)x / resX;
                    float v = (float)y / resY;
                    verts[vIdx] = new Vector3(u * width, -v * height, 0f);
                    uvs[vIdx] = new Vector2(u, v);
                    vIdx++;
                }
            }

            int tIdx = 0;
            for (int y = 0; y < resY; y++)
            {
                for (int x = 0; x < resX; x++)
                {
                    int row1 = y * (resX + 1);
                    int row2 = (y + 1) * (resX + 1);

                    // Front Face (Clockwise from front)
                    tris[tIdx++] = row1 + x;
                    tris[tIdx++] = row1 + x + 1;
                    tris[tIdx++] = row2 + x;

                    tris[tIdx++] = row1 + x + 1;
                    tris[tIdx++] = row2 + x + 1;
                    tris[tIdx++] = row2 + x;

                    // Back Face (Counter-clockwise from front / Clockwise from back)
                    tris[tIdx++] = row1 + x;
                    tris[tIdx++] = row2 + x;
                    tris[tIdx++] = row1 + x + 1;

                    tris[tIdx++] = row1 + x + 1;
                    tris[tIdx++] = row2 + x;
                    tris[tIdx++] = row2 + x + 1;
                }
            }

            _clothMeshInstance.vertices = verts;
            _clothMeshInstance.uv = uvs;
            _clothMeshInstance.triangles = tris;
            _clothMeshInstance.RecalculateNormals();

            _baseClothVertices = verts;
            _animatedClothVertices = new Vector3[verts.Length];

            _clothMeshFilter.sharedMesh = _clothMeshInstance;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            // 1. Cloth Wave Ripple Animation
            AnimateClothWave();

            // 2. Elastic Spring Physics for Pole Tilt
            float springK = 35f;
            float damping = 5f;
            _poleAngleVel += (-springK * _poleAngle - damping * _poleAngleVel) * dt;
            _poleAngle += _poleAngleVel * dt;

            if (_poleTransform != null)
            {
                _poleTransform.localRotation = Quaternion.AngleAxis(_poleAngle, _poleTiltAxis);
                if (_flagClothTransform != null)
                {
                    _flagClothTransform.localRotation = _poleTransform.localRotation;
                }
            }
        }

        private void AnimateClothWave()
        {
            if (_clothMeshInstance == null || _baseClothVertices == null) return;

            float t = Time.time * _waveSpeed;

            for (int i = 0; i < _baseClothVertices.Length; i++)
            {
                Vector3 baseV = _baseClothVertices[i];
                // Wave weight grows towards free tip of flag (baseV.x)
                float weight = Mathf.Clamp01(baseV.x / 0.85f);
                float wave1 = Mathf.Sin(t + baseV.x * _waveFrequency) * _waveAmplitude * weight;
                float wave2 = Mathf.Cos(t * 1.6f + baseV.y * 4f) * (_waveAmplitude * 0.4f) * weight;

                _animatedClothVertices[i] = new Vector3(baseV.x, baseV.y + wave2 * 0.2f, wave1 + wave2);
            }

            _clothMeshInstance.vertices = _animatedClothVertices;
            _clothMeshInstance.RecalculateNormals();
        }

        public void TriggerSpringBounce(Vector3 impactDir, float strength)
        {
            _poleTiltAxis = Vector3.Cross(Vector3.up, impactDir).normalized;
            if (_poleTiltAxis.sqrMagnitude < 0.01f) _poleTiltAxis = Vector3.forward;
            _poleAngleVel = Mathf.Clamp(strength * 22f, 10f, 65f);
        }

        private void HandleMarbleImpact(MarbleController m1, MarbleController m2)
        {
            // Nearby impacts rattle the flag
            float dist = Vector3.Distance(transform.position, m1.transform.position);
            if (dist < 4.5f)
            {
                TriggerSpringBounce(Random.insideUnitSphere, (4.5f - dist) * 0.35f);
            }
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            if (pit != null && pit.PitNumber == _pitNumber)
            {
                StartCoroutine(VictoryCelebrationRoutine());
            }
        }

        private IEnumerator VictoryCelebrationRoutine()
        {
            TriggerSpringBounce(Vector3.forward, 2.5f);

            // Excited 360-degree celebratory spin!
            float elapsed = 0f;
            const float duration = 0.8f;
            Quaternion startRot = transform.rotation;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float angle = Mathf.SmoothStep(0f, 360f, t);
                transform.rotation = startRot * Quaternion.Euler(0f, angle, 0f);
                yield return null;
            }

            transform.rotation = startRot;
        }
    }
}
