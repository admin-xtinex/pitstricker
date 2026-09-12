#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Builds Sunset Coastal materials by reusing the project's existing shared shaders
    /// ("Pit Striker/Village Surface" for opaque PBR sand/wood/rock/rope surfaces,
    /// "Pit Striker/Village Foliage" for alpha-cutout billboard cards, props, impostors
    /// and decals) rather than authoring new shaders. Neither shared shader is modified.
    /// </summary>
    public static class SunsetCoastalMaterialBuilder
    {
        public const string MaterialRoot = "Assets/_Project/Art/Environments/SunsetCoastal/Materials";
        private const string SurfaceShaderName = "Pit Striker/Village Surface";
        private const string CardShaderName = "Pit Striker/Village Foliage";

        private static readonly Dictionary<string, Material> _cache = new Dictionary<string, Material>();

        public static Material GetSurfaceMaterial(string materialSet, float tiling = 3f, float smoothnessBias = 0f)
        {
            string path = $"{MaterialRoot}/M_SC_{materialSet}.mat";
            if (_cache.TryGetValue(path, out var cached) && cached != null) return cached;

            Directory.CreateDirectory(MaterialRoot);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find(SurfaceShaderName);
                mat = new Material(shader) { name = $"M_SC_{materialSet}" };
                AssetDatabase.CreateAsset(mat, path);
            }

            string albedoPath = SunsetCoastalAssetImporter.FindTexturePath(materialSet, "_Albedo.png");
            string normalPath = SunsetCoastalAssetImporter.FindTexturePath(materialSet, "_Normal.png");
            string maskPath = SunsetCoastalAssetImporter.GetMaskPath(materialSet);

            var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(albedoPath);
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);

            if (albedo != null) mat.SetTexture("_BaseMap", albedo);
            if (normal != null) mat.SetTexture("_BumpMap", normal);
            if (mask != null)
            {
                mat.SetTexture("_MaskMap", mask);
                mat.SetFloat("_UseMask", 1f);
            }
            mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            mat.SetFloat("_Smoothness", Mathf.Clamp01(0.18f + smoothnessBias));
            mat.SetFloat("_Metallic", 0f);
            mat.SetFloat("_BumpScale", 0.6f);
            mat.SetFloat("_AmbientFill", 0.22f);

            EditorUtility.SetDirty(mat);
            _cache[path] = mat;
            return mat;
        }

        public static Material GetCardMaterial(string cardName, Texture2D texture, float cutoff = 0.35f, float windStrength = 0f, Color? tint = null)
        {
            string path = $"{MaterialRoot}/Cards/M_SC_Card_{cardName}.mat";
            if (_cache.TryGetValue(path, out var cached) && cached != null) return cached;

            Directory.CreateDirectory(MaterialRoot + "/Cards");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find(CardShaderName);
                mat = new Material(shader) { name = $"M_SC_Card_{cardName}" };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", tint ?? Color.white);
            mat.SetFloat("_Cutoff", cutoff);
            mat.SetFloat("_WindSpeed", windStrength > 0f ? 1.4f : 0f);
            mat.SetFloat("_WindStrength", windStrength);
            mat.SetFloat("_Transmission", windStrength > 0f ? 0.35f : 0.1f);
            mat.SetFloat("_AmbientFill", 0.30f);

            EditorUtility.SetDirty(mat);
            _cache[path] = mat;
            return mat;
        }

        /// <summary>
        /// True alpha-blended card for soft-gradient art (haze, clouds) that a cutout
        /// shader can only show as hard on/off. Uses URP's stock Unlit shader configured
        /// for the Transparent surface type — still URP-pipeline-tagged, unlike the
        /// legacy Built-in "Skybox/Procedural" shader that renders invalid in this project.
        /// </summary>
        public static Material GetTransparentCardMaterial(string cardName, Texture2D texture)
        {
            string path = $"{MaterialRoot}/Cards/M_SC_Blend_{cardName}.mat";
            if (_cache.TryGetValue(path, out var cached) && cached != null) return cached;

            Directory.CreateDirectory(MaterialRoot + "/Cards");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader) { name = $"M_SC_Blend_{cardName}" };
                AssetDatabase.CreateAsset(mat, path);
            }

            mat.SetTexture("_BaseMap", texture);
            mat.SetColor("_BaseColor", Color.white);
            mat.SetFloat("_Surface", 1f); // Transparent
            mat.SetFloat("_Blend", 0f);   // Alpha
            mat.SetFloat("_Cull", 0f);    // Off (double-sided billboard)
            mat.SetFloat("_ZWrite", 0f);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

            EditorUtility.SetDirty(mat);
            _cache[path] = mat;
            return mat;
        }

        [MenuItem("Pit Striker/Sunset Coastal/2. Build Pack04 Materials", false, 11)]
        public static void BuildAllSurfaceMaterials()
        {
            foreach (var set in SunsetCoastalAssetImporter.MaterialSets)
            {
                float tiling = set.StartsWith("Sand") ? 6f : set == "Rope_Coastal" ? 2f : 3f;
                GetSurfaceMaterial(set, tiling);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#35BFF3><b>[SUNSET COASTAL MATERIALS]</b> Pack04 surface materials built.</color>");
        }
    }
}
#endif
