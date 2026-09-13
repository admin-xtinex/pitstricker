#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Builds the rich, photorealistic Sunset Sky & Coastal Ocean Vista:
    /// - Sunset Sky Backdrop: True vertical gradient (golden yellow -> amber -> peach -> violet -> deep purple)
    ///   precisely framed within Camera B's vertical FOV so the full gradient is visible.
    /// - Distant Mountain & Cloud Layers: Impostor cards for misty mountains and golden sunset clouds.
    /// - Turquoise Ocean: Water plane with realistic wave ripples normal map, high specular smoothness
    ///   reflecting the setting sun, and wet sand transition.
    /// </summary>
    public static class SunsetCoastalSkyWaterBuilder
    {
        private const string GradientTexPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/SunsetSky_Gradient.png";
        private const string OceanNormalPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/Ocean_Normal.png";
        private const string Pack05 = "Pack05_Expanded_Impostors";

        public static void Build(Transform parent)
        {
            BuildSkyBackdrop(parent);
            BuildDistantMountainsAndClouds(parent);
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

            // Camera is at (0, 5.6, -11) looking down 24.5 deg.
            // At Z = 52m, the visible sky spans from Y ~ 0.5m (horizon) to Y ~ 4.5m (screen top).
            // Placing a quad centered at Y = 3.2m with height 8.0m ensures the full rich gradient is visible!
            go.transform.position = new Vector3(0f, -0.8f, 52f);
            go.transform.localScale = new Vector3(180f, 8.5f, 1f);

            var mf = go.AddComponent<MeshFilter>();
            mf.sharedMesh = SunsetCoastalBillboardUtil.GetQuadMesh();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        private static void BuildDistantMountainsAndClouds(Transform parent)
        {
            var vistaRoot = new GameObject("Sky_Mountains_And_Clouds");
            vistaRoot.transform.SetParent(parent, false);

            // 1. Distant Mountain Silhouette Layer
            Texture2D mtnTex = LoadSourceTexture(Pack05, "MountainLayer_Far_01", "Impostors");
            if (mtnTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("MountainLayer_Far_01", mtnTex);
                SunsetCoastalBillboardUtil.SpawnCard("DistantMountains", vistaRoot.transform, mtnTex, mat,
                    new Vector3(-4.0f, 0.2f, 48.0f), 4.2f, yRotation: 0f, castShadows: false, isStatic: true);
            }

            // 2. Sunset Cloud Layer
            Texture2D cloudTex = LoadSourceTexture(Pack05, "SunsetCloud_Layer_01", "Impostors");
            if (cloudTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("SunsetCloud_Layer_01", cloudTex);
                SunsetCoastalBillboardUtil.SpawnCard("SunsetClouds", vistaRoot.transform, cloudTex, mat,
                    new Vector3(2.0f, 2.2f, 49.0f), 4.8f, yRotation: 0f, castShadows: false, isStatic: true);
            }

            // 3. Atmospheric Haze Layer
            Texture2D hazeTex = LoadSourceTexture(Pack05, "AtmosphericHaze_Layer_01", "Impostors");
            if (hazeTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("AtmosphericHaze_Layer_01", hazeTex);
                SunsetCoastalBillboardUtil.SpawnCard("AtmosphericHaze", vistaRoot.transform, hazeTex, mat,
                    new Vector3(0.0f, 0.4f, 45.0f), 3.8f, yRotation: 0f, castShadows: false, isStatic: true);
            }
        }

        private static Texture2D GetOrCreateGradientTexture()
        {
            const int height = 256;
            var tex = new Texture2D(4, height, TextureFormat.RGBA32, false);

            // Gorgeous Sunset Sky Gradient matching Phase 4 reference artwork:
            // 0.00: Brilliant golden horizon glow (#FFE28A)
            // 0.20: Warm amber sunset orange (#FFA350)
            // 0.45: Vibrant coral pink / peach (#EB646E)
            // 0.70: Soft lavender / mauve twilight (#8E4B7E)
            // 1.00: Deep majestic twilight purple (#3A2450)
            var stops = new[]
            {
                (0.00f, new Color(1.00f, 0.88f, 0.54f)),
                (0.20f, new Color(1.00f, 0.64f, 0.31f)),
                (0.45f, new Color(0.92f, 0.39f, 0.43f)),
                (0.70f, new Color(0.56f, 0.29f, 0.49f)),
                (1.00f, new Color(0.23f, 0.14f, 0.31f)),
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

            // Vibrant tropical turquoise ocean with sparkling specular
            mat.SetColor("_BaseColor", new Color(0.14f, 0.52f, 0.65f, 1f));
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0.08f);

            var normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(OceanNormalPath);
            if (normalTex != null)
            {
                mat.SetTexture("_BumpMap", normalTex);
                mat.SetTextureScale("_BumpMap", new Vector2(12f, 24f));
                mat.SetFloat("_BumpScale", 0.8f);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", new Color(0.06f, 0.22f, 0.28f));
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            EditorUtility.SetDirty(mat);

            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SunsetCoastal_Ocean";
            go.transform.SetParent(parent, false);

            // Plane is 10m x 10m. Scale (6.0, 1, 9.0) = 60m wide, 90m long.
            // X center at 27m spans X from -3m to +57m; starts outside right beach at X = 4.2m.
            go.transform.position = new Vector3(32.0f, -0.04f, 24.0f);
            go.transform.localScale = new Vector3(6.0f, 1f, 9.0f);
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);

            BuildWetSandStrip(parent);
        }

        private static void BuildWetSandStrip(Transform parent)
        {
            Material wetSand = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Wet", 4f);
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SunsetCoastal_WetSandStrip";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(4.8f, -0.035f, 20.0f);
            go.transform.localScale = new Vector3(0.24f, 1f, 8.0f); // 2.4m wide strip along shoreline
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = wetSand;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        private static Texture2D LoadSourceTexture(string pack, string assetName, string subFolder = "Assets")
        {
            string path = $"{SunsetCoastalAssetImporter.SourceRoot}/{pack}/{subFolder}/{assetName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                string altPath = $"{SunsetCoastalAssetImporter.SourceRoot}/{pack}/Impostors/{assetName}.png";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(altPath);
            }
            return tex;
        }
    }
}
#endif
