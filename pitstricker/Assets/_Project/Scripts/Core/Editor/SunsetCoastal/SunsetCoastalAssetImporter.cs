#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Configures import settings for the Sunset Coastal source PNGs copied in from
    /// PitStriker_Map02_SunsetCoastal_AllPacks_v1.zip, and synthesizes the combined
    /// AO(R)/Roughness(G) mask textures that "Pit Striker/Village Surface" expects,
    /// since Pack04 ships Roughness as its own separate grayscale map.
    /// </summary>
    public static class SunsetCoastalAssetImporter
    {
        public const string SourceRoot = "Assets/_Project/Art/Environments/SunsetCoastal/Source";
        public const string GeneratedRoot = "Assets/_Project/Art/Environments/SunsetCoastal/Generated";
        public const string MaskRoot = GeneratedRoot + "/Masks";

        public static readonly string[] MaterialSets =
        {
            "Sand_Dry", "Sand_Packed", "Sand_Wet", "Wood_Weathered", "Rock_Coastal", "Rope_Coastal"
        };

        [MenuItem("Pit Striker/Sunset Coastal/1. Configure Imported Source Textures", false, 10)]
        public static void ConfigureImportSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { SourceRoot });
            int configured = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool isNormal = path.EndsWith("_Normal.png");
                bool isMaterialMap = path.Contains("/Pack04_Materials_And_Decals/Materials/");

                importer.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                importer.alphaIsTransparency = !isNormal;
                importer.mipmapEnabled = true;
                importer.wrapMode = isMaterialMap ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                importer.maxTextureSize = 1024;
                importer.sRGBTexture = !isNormal && !path.EndsWith("_Roughness.png") && !path.EndsWith("_Height.png");

                var settings = importer.GetDefaultPlatformTextureSettings();
                settings.maxTextureSize = 1024;
                settings.format = TextureImporterFormat.Automatic;
                importer.SetPlatformTextureSettings(settings);

                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
                configured++;
            }

            Debug.Log($"<color=#35BFF3><b>[SUNSET COASTAL IMPORT]</b> Configured {configured} source textures.</color>");
            BuildMaskMaps();
        }

        private static void BuildMaskMaps()
        {
            Directory.CreateDirectory(MaskRoot);
            foreach (var set in MaterialSets)
            {
                string roughnessPath = FindTexturePath(set, "_Roughness.png");
                if (roughnessPath == null)
                {
                    Debug.LogWarning($"[SUNSET COASTAL IMPORT] No roughness map found for '{set}', skipping mask.");
                    continue;
                }

                byte[] bytes = File.ReadAllBytes(roughnessPath);
                var roughnessTex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                roughnessTex.LoadImage(bytes);

                var mask = new Texture2D(roughnessTex.width, roughnessTex.height, TextureFormat.RGBA32, false);
                Color[] src = roughnessTex.GetPixels();
                Color[] dst = new Color[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    float roughness = src[i].grayscale;
                    dst[i] = new Color(1f, roughness, 0f, 1f);
                }
                mask.SetPixels(dst);
                mask.Apply();

                string outPath = $"{MaskRoot}/{set}_Mask.png";
                File.WriteAllBytes(outPath, mask.EncodeToPNG());
                Object.DestroyImmediate(roughnessTex);
                Object.DestroyImmediate(mask);
                AssetDatabase.ImportAsset(outPath);

                var maskImporter = AssetImporter.GetAtPath(outPath) as TextureImporter;
                if (maskImporter != null)
                {
                    maskImporter.sRGBTexture = false;
                    maskImporter.alphaIsTransparency = false;
                    maskImporter.wrapMode = TextureWrapMode.Repeat;
                    maskImporter.maxTextureSize = 1024;
                    maskImporter.SaveAndReimport();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("<color=#35BFF3><b>[SUNSET COASTAL IMPORT]</b> Mask maps generated.</color>");
        }

        public static string FindTexturePath(string materialSet, string suffix)
        {
            string[] guids = AssetDatabase.FindAssets(materialSet.Replace("_", " "), new[] { SourceRoot + "/Pack04_Materials_And_Decals" });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains(materialSet) && path.EndsWith(suffix)) return path;
            }
            // Fallback: direct path probe (manifest-known layout).
            string direct = $"{SourceRoot}/Pack04_Materials_And_Decals/Materials/{materialSet}/{materialSet}{suffix}";
            return File.Exists(direct) ? direct : null;
        }

        public static string GetMaskPath(string materialSet) => $"{MaskRoot}/{materialSet}_Mask.png";
    }
}
#endif
