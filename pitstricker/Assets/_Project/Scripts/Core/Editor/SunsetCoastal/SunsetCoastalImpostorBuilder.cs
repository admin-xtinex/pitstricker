#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 5: distant scenic impostor cards from Pack01/Pack05 forming the coastal
    /// backdrop (lighthouse island, palm forests, mountains, huts, boats, clouds, haze,
    /// cliffs, island vegetation). No colliders, no rigidbodies, shadows off, static-batched.
    /// </summary>
    public static class SunsetCoastalImpostorBuilder
    {
        public static GameObject BuildImpostorLayer(Transform parent)
        {
            var root = new GameObject("SunsetCoastal_Impostors");
            root.transform.SetParent(parent, false);

            // Lighthouse island sits beyond the shoreline on the right, matching the
            // approved reference framing (sun setting behind/near the lighthouse). A/B
            // pairs are layered at slightly different depths for a subtle parallax read.
            SpawnImpostor(root.transform, "LighthouseIsland_Expanded_A", 13f, 3f, 48f, 9f, 0f);
            SpawnImpostor(root.transform, "LighthouseIsland_Expanded_B", 13.6f, 2.8f, 47.5f, 8.5f, -6f);
            SpawnImpostor(root.transform, "LighthouseIsland_A", 13f, 3f, 48.5f, 8.5f, 0f);
            SpawnImpostor(root.transform, "LighthouseIsland_B", 12.4f, 2.9f, 49f, 8f, 5f);

            // Source art is a flat, desaturated grey-green — reads as an out-of-place
            // "leftover" cutout against the rest of the warm sunset palette unless tinted
            // to match (real distant mountains skew toward hazy dusk purple/pink anyway).
            // The card shader multiplies texture*tint, and the source is already dark, so
            // the tint needs values above 1.0 to actually brighten/shift hue rather than
            // darken it further.
            Color mountainTint = new Color(2.6f, 1.75f, 2.05f);
            SpawnImpostor(root.transform, "MountainLayer_Far_01", -4f, 6f, 62f, 26f, 0f, mountainTint);
            SpawnImpostor(root.transform, "MountainLayer_Far_02", 10f, 6.5f, 66f, 24f, 0f, mountainTint);
            SpawnImpostor(root.transform, "DistantCliffs_Layer_01", 18f, 4f, 55f, 14f, -20f, mountainTint);

            SpawnImpostor(root.transform, "PalmForest_Far_A", -9f, 3.5f, 20f, 12f, 15f);
            SpawnImpostor(root.transform, "PalmForest_Far_B", 15f, 3.5f, 22f, 12f, -15f);
            SpawnImpostor(root.transform, "PalmCluster_Far_01", -8f, 2.6f, 6f, 8f, 10f);
            SpawnImpostor(root.transform, "PalmCluster_Far_02", 14f, 2.6f, 9f, 8f, -10f);

            // Deliberately crude/flat source art (plain silhouette boxes, unlike the
            // painted hero assets) — kept small and pushed well back so its simplicity
            // blends into the distance instead of reading as an obvious flat cutout.
            SpawnImpostor(root.transform, "CoastalHuts_Far_Strip", -14f, 1.6f, 52f, 5f, 8f);
            SpawnImpostor(root.transform, "FarBoats_Silhouette_Set", 12f, 1.4f, 40f, 6f, 0f);
            SpawnImpostor(root.transform, "IslandVegetation_Mass_01", 12.5f, 2.0f, 46f, 6.5f, 0f);

            SpawnImpostor(root.transform, "ForegroundFoliage_Frame_A", -6.5f, 4.2f, -6f, 8.5f, 22f);
            SpawnImpostor(root.transform, "ForegroundFoliage_Frame_B", 9.5f, 4.2f, -6f, 8.5f, -22f);
            SpawnImpostor(root.transform, "ForegroundFoliage_02", -6.8f, 3.6f, -3.5f, 7f, 18f);

            SpawnImpostor(root.transform, "SunsetCloud_Layer_01", 2f, 20f, 55f, 40f, 0f);
            SpawnImpostor(root.transform, "AtmosphericHaze_Layer_01", 2f, 8f, 50f, 46f, 0f);

            return root;
        }

        // Soft full-gradient art: a cutout/clip shader can only show these as hard
        // on/off, which reads as a solid wall. They need true alpha blending instead.
        private static readonly System.Collections.Generic.HashSet<string> SoftBlendLayers = new()
        {
            "AtmosphericHaze_Layer_01", "SunsetCloud_Layer_01"
        };

        private static void SpawnImpostor(Transform parent, string textureName, float x, float baseY, float z, float worldHeight, float yRotation, Color? tint = null)
        {
            string[] guids = AssetDatabase.FindAssets(textureName, new[] { SunsetCoastalAssetImporter.SourceRoot });
            string path = null;
            foreach (var guid in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(p) == textureName) { path = p; break; }
            }
            if (path == null)
            {
                Debug.LogWarning($"[SUNSET COASTAL IMPOSTORS] Texture not found: {textureName}");
                return;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var mat = SoftBlendLayers.Contains(textureName)
                ? SunsetCoastalMaterialBuilder.GetTransparentCardMaterial(textureName, tex)
                : SunsetCoastalMaterialBuilder.GetCardMaterial(textureName, tex, cutoff: 0.08f, windStrength: 0f, tint: tint);
            var card = SunsetCoastalBillboardUtil.SpawnCard($"Impostor_{textureName}", parent, tex, mat,
                new Vector3(x, baseY, z), worldHeight, yRotation, castShadows: false, isStatic: true);

            // Distant scenery never needs collision or light-probe sampling.
            var mr = card.GetComponent<MeshRenderer>();
            mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        }
    }
}
#endif
