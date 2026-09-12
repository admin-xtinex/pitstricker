#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Neither a sky nor an ocean exists anywhere in Packs 01-05 — comparing against the
    /// approved reference image, their absence (flat solid-color background, sand to the
    /// horizon) is the single biggest gap from reading as "coastal" at all. Built here
    /// from scratch: a generated vertical-gradient sunset sky backdrop and a shiny blue
    /// water plane along the shoreline (right side of the lane), both using URP's stock
    /// shaders so there's no custom-shader/pipeline-compatibility risk.
    /// </summary>
    public static class SunsetCoastalSkyWaterBuilder
    {
        private const string GradientTexPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/SunsetSky_Gradient.png";

        public static void Build(Transform parent)
        {
            BuildSkyBackdrop(parent);
            BuildOcean(parent);
        }

        private static void BuildSkyBackdrop(Transform parent)
        {
            var tex = GetOrCreateGradientTexture();

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            const string matPath = "Assets/_Project/Art/Environments/SunsetCoastal/SunsetCoastal_SkyBackdrop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "SunsetCoastal_SkyBackdrop" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Cull", 0f);
            EditorUtility.SetDirty(mat);

            var go = new GameObject("SunsetCoastal_SkyBackdrop");
            go.transform.SetParent(parent, false);
            // Kept short so the dramatic pink/purple upper gradient sits within the
            // camera's actual pitch range instead of mostly above the frustum, where only
            // the (near-identical-to-the-old-flat-color) horizon band would ever be seen.
            go.transform.position = new Vector3(2f, 0f, 95f);
            go.transform.localScale = new Vector3(220f, 55f, 1f);
            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = SunsetCoastalBillboardUtil.GetQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        private static Texture2D GetOrCreateGradientTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientTexPath);
            if (existing != null) return existing;

            const int height = 256;
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false);

            // Bottom (horizon) to top, matching the approved reference's sunset gradient.
            var stops = new[]
            {
                (0.00f, new Color(1.00f, 0.80f, 0.50f)),
                (0.12f, new Color(1.00f, 0.66f, 0.42f)),
                (0.28f, new Color(0.95f, 0.50f, 0.46f)),
                (0.48f, new Color(0.75f, 0.38f, 0.48f)),
                (0.70f, new Color(0.45f, 0.28f, 0.44f)),
                (1.00f, new Color(0.24f, 0.20f, 0.38f)),
            };

            var colors = new Color[4 * height];
            for (int y = 0; y < height; y++)
            {
                float t = y / (float)(height - 1);
                Color c = stops[0].Item2;
                for (int i = 0; i < stops.Length - 1; i++)
                {
                    if (t >= stops[i].Item1 && t <= stops[i + 1].Item1)
                    {
                        float localT = Mathf.InverseLerp(stops[i].Item1, stops[i + 1].Item1, t);
                        c = Color.Lerp(stops[i].Item2, stops[i + 1].Item2, localT);
                        break;
                    }
                }
                for (int x = 0; x < 4; x++) colors[y * 4 + x] = c;
            }
            tex.SetPixels(colors);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(GradientTexPath));
            File.WriteAllBytes(GradientTexPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(GradientTexPath);

            var importer = AssetImporter.GetAtPath(GradientTexPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = false;
                importer.maxTextureSize = 256;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(GradientTexPath);
        }

        /// <summary>
        /// A shiny, sunset-tinted water plane along the shoreline (right side of the lane,
        /// wrapping to the horizon near the lighthouse) — flat but with high smoothness so
        /// it catches a warm specular highlight rather than reading as inert grey-blue.
        /// </summary>
        private static void BuildOcean(Transform parent)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            const string matPath = "Assets/_Project/Art/Environments/SunsetCoastal/SunsetCoastal_Ocean.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                mat = new Material(shader) { name = "SunsetCoastal_Ocean" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetColor("_BaseColor", new Color(0.10f, 0.34f, 0.42f, 1f));
            mat.SetFloat("_Smoothness", 0.88f);
            mat.SetFloat("_Metallic", 0.15f);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.25f, 0.12f, 0.05f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);

            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SunsetCoastal_Ocean";
            go.transform.SetParent(parent, false);
            // Right rail sits at x=7.2 (see SunsetCoastalMeshBuilder.RightRailX) — start the
            // water just past it so nothing overlaps the sand lane itself.
            go.transform.position = new Vector3(29f, 0.005f, 22f);
            go.transform.localScale = new Vector3(4.2f, 1f, 7.5f); // 10x10 base plane -> 42 x 75.
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

            BuildWetSandStrip(parent);
        }

        /// <summary>
        /// A darker wet-sand strip right where the dry sand meets the water — softens
        /// what would otherwise be a hard, unnatural-looking straight edge between the
        /// two planes, and gives the Sand_Wet material (built but never applied anywhere
        /// else) an actual home.
        /// </summary>
        private static void BuildWetSandStrip(Transform parent)
        {
            Material wetSand = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Wet", 6f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SunsetCoastal_WetSandStrip";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(6.4f, 0.006f, 22f);
            go.transform.localScale = new Vector3(0.32f, 1f, 7.5f); // ~3.2m wide, right up to the water at x=8.
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = wetSand;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }
    }
}
#endif
