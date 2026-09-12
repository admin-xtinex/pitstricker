using System.Collections;
using UnityEngine;
using PitStriker.Physics;
using PitStriker.Gameplay;

namespace PitStriker.VFX
{
    /// <summary>
    /// Cartoon VFX juice manager for Pit Striker.
    /// Provides comic impact stars, dynamic speed trails, launch puffs, and golden victory bursts.
    /// Operates with pure decorative meshes (no physics colliders or DestroyImmediate in physics steps).
    /// </summary>
    public class StylizedVFX : MonoBehaviour
    {
        public static StylizedVFX Instance { get; private set; }

        private Material _starMaterial;
        private Material _trailMaterial;
        private static Mesh _starMesh;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMaterials();
                CleanOrphanedStars();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            MarbleController.OnMarbleHitMarble += HandleMarbleCollision;
            PitZone.OnMarbleSunk += HandleMarbleSunk;
            CleanOrphanedStars();
        }

        private void OnDisable()
        {
            MarbleController.OnMarbleHitMarble -= HandleMarbleCollision;
            PitZone.OnMarbleSunk -= HandleMarbleSunk;
        }

        public void CleanOrphanedStars()
        {
            var stars = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in stars)
            {
                if (s != null && s.name.StartsWith("Comic_Star"))
                {
                    if (Application.isPlaying) Destroy(s);
                    else DestroyImmediate(s);
                }
            }
        }

        private static Mesh GetStarMesh()
        {
            if (_starMesh == null)
            {
                _starMesh = new Mesh();
                _starMesh.name = "Comic_Star_Octahedron";
                _starMesh.vertices = new Vector3[]
                {
                    new Vector3(0, 1.3f, 0),    // 0 Top
                    new Vector3(0, -1.3f, 0),   // 1 Bottom
                    new Vector3(0.55f, 0, 0),   // 2 Right
                    new Vector3(-0.55f, 0, 0),  // 3 Left
                    new Vector3(0, 0, 0.55f),   // 4 Forward
                    new Vector3(0, 0, -0.55f)   // 5 Back
                };
                _starMesh.triangles = new int[]
                {
                    0, 2, 4,
                    0, 4, 3,
                    0, 3, 5,
                    0, 5, 2,
                    1, 4, 2,
                    1, 3, 4,
                    1, 5, 3,
                    1, 2, 5
                };
                _starMesh.RecalculateNormals();
                _starMesh.RecalculateBounds();
            }
            return _starMesh;
        }

        private void InitializeMaterials()
        {
            Shader toonShader = Shader.Find("Pit Striker/Stylized Toon PBR");
            if (toonShader == null) toonShader = Shader.Find("Universal Render Pipeline/Lit");
            if (toonShader == null) toonShader = Shader.Find("Standard");

            _starMaterial = new Material(toonShader);
            _starMaterial.name = "M_ComicStar_Gold";
            Color starGold = new Color(1f, 0.88f, 0.15f, 1f);
            _starMaterial.SetColor("_BaseColor", starGold);
            _starMaterial.SetColor("_Color", starGold);
            _starMaterial.SetFloat("_Smoothness", 0.85f);
            _starMaterial.EnableKeyword("_EMISSION");
            _starMaterial.SetColor("_EmissionColor", starGold * 0.4f);
            if (_starMaterial.HasProperty("_RimColor")) _starMaterial.SetColor("_RimColor", Color.white);

            Shader trailShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (trailShader != null)
            {
                _trailMaterial = new Material(trailShader);
                _trailMaterial.color = new Color(0.2f, 0.85f, 1f, 0.75f);
            }
        }

        public void SpawnComicImpactStars(Vector3 point, float intensity)
        {
            int count = Mathf.Clamp(Mathf.RoundToInt(intensity * 4f), 3, 6);
            for (int i = 0; i < count; i++)
            {
                Vector3 launchDir = (Random.onUnitSphere + Vector3.up * 1.2f).normalized;
                StartCoroutine(SingleStarRoutine(point, launchDir, Random.Range(1.8f, 3.5f) * Mathf.Max(0.5f, intensity)));
            }
        }

        private IEnumerator SingleStarRoutine(Vector3 origin, Vector3 direction, float speed)
        {
            if (_starMaterial == null) InitializeMaterials();

            GameObject star = new GameObject("Comic_Star");
            star.transform.position = origin;
            star.transform.localScale = Vector3.zero;

            MeshFilter mf = star.AddComponent<MeshFilter>();
            mf.sharedMesh = GetStarMesh();

            MeshRenderer mr = star.AddComponent<MeshRenderer>();
            mr.sharedMaterial = _starMaterial;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            float elapsed = 0f;
            const float lifetime = 0.42f;
            Vector3 rotAxis = Random.insideUnitSphere.normalized;

            while (elapsed < lifetime)
            {
                if (star == null) yield break;

                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;

                // Arc movement with gravity
                star.transform.position += direction * speed * Time.deltaTime + Vector3.down * (4.0f * elapsed * Time.deltaTime);
                star.transform.Rotate(rotAxis, 450f * Time.deltaTime);

                // Squash-out scale: expand quickly, then shrink to zero
                float scale = Mathf.Sin(t * Mathf.PI) * 0.16f;
                star.transform.localScale = Vector3.one * scale;

                yield return null;
            }

            if (star != null)
            {
                Destroy(star);
            }
        }

        private void HandleMarbleCollision(MarbleController m1, MarbleController m2)
        {
            if (m1 == null || m2 == null) return;
            Vector3 midPoint = (m1.transform.position + m2.transform.position) * 0.5f;
            float relativeSpeed = (m1.Velocity - m2.Velocity).magnitude;
            if (relativeSpeed > 0.4f)
            {
                SpawnComicImpactStars(midPoint + Vector3.up * 0.15f, Mathf.Clamp01(relativeSpeed / 8f));
            }
        }

        private void HandleMarbleSunk(PitZone pit, MarbleController marble)
        {
            // Pit celebration is handled by StylizedPitFeedback (expanding ground ripple shockwave & lip bounce)
            // and StylizedFlagAnimation (joyful victory spin).
        }
    }
}
