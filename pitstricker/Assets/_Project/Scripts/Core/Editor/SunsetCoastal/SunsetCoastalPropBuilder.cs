#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 3: 2.5D billboard cards from Pack01/Pack03 for peripheral scenic dressing
    /// (fence variant, surfboards, dock, crates, signage, lanterns, foliage, barrels,
    /// driftwood). Kept off the gameplay strip per the doc's "do not clutter the lane" rule.
    /// </summary>
    public static class SunsetCoastalPropBuilder
    {
        private const float LeftRailX = -4.5f;
        private const float RightRailX = 7.2f;

        public static GameObject BuildPropLayer(Transform parent)
        {
            var root = new GameObject("SunsetCoastal_Props");
            root.transform.SetParent(parent, false);

            // Near the shack (SunsetCoastalMeshBuilder.BuildShack, x=LeftRailX-1.4, z=9.0)
            // matching the reference, but offset clear of its bounds — two billboard
            // cards placed too close at slightly different angles visibly cross/intersect
            // each other rather than reading as "leaning against the wall."
            SpawnProp(root.transform, "Surfboard_Set_01", LeftRailX - 3.6f, 8.6f, 1.4f, 5f);
            SpawnProp(root.transform, "BeachSignage_Set_01", RightRailX + 1.2f, 30.5f, 1.3f, -18f);
            SpawnProp(root.transform, "DockModule_01", RightRailX + 2.8f, 33.0f, 1.6f, -40f);
            SpawnProp(root.transform, "Crate_Set_01", LeftRailX - 1.6f, 1.0f, 0.8f, 10f);
            SpawnProp(root.transform, "Crate_Set_01", LeftRailX - 1.9f, 1.6f, 0.7f, -12f);
            SpawnProp(root.transform, "Lantern_Set_02", LeftRailX - 0.9f, 6.0f, 0.9f, 0f);
            SpawnProp(root.transform, "Lantern_Set_02", RightRailX + 0.9f, 10.0f, 0.9f, 0f);
            SpawnProp(root.transform, "Lantern_Set_02", LeftRailX - 0.9f, 20.0f, 0.9f, 0f);
            SpawnProp(root.transform, "Lantern_Set_02", RightRailX + 0.9f, 26.0f, 0.9f, 0f);
            SpawnProp(root.transform, "BoundaryFence_Variant_01", LeftRailX - 1.0f, 12.0f, 1.1f, 0f);
            SpawnProp(root.transform, "TropicalFoliage_Cluster_02", LeftRailX - 1.4f, 8.0f, 1.6f, 0f, foliage: true);
            SpawnProp(root.transform, "TropicalFoliage_Cluster_02", RightRailX + 1.5f, 13.5f, 1.5f, 0f, foliage: true);
            SpawnProp(root.transform, "ForegroundFoliage_Frame_02", LeftRailX - 2.6f, 0.0f, 2.4f, 0f, foliage: true);
            SpawnProp(root.transform, "ForegroundFoliage_01", RightRailX + 2.8f, -2.0f, 2.6f, 0f, foliage: true);
            SpawnProp(root.transform, "ClayPot_01", LeftRailX - 1.2f, 24.0f, 0.6f, 0f);
            SpawnProp(root.transform, "WoodenBarrel_01", LeftRailX - 1.5f, 25.0f, 0.7f, -8f);
            SpawnProp(root.transform, "DriftwoodObstacle_01", RightRailX + 1.7f, 28.5f, 0.6f, 20f);
            SpawnProp(root.transform, "CoastalRockCluster_01", RightRailX + 2.1f, 3.5f, 1.1f, 0f);

            return root;
        }

        private static void SpawnProp(Transform parent, string textureName, float x, float z, float worldHeight, float yRotation, bool foliage = false)
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
                Debug.LogWarning($"[SUNSET COASTAL PROPS] Texture not found: {textureName}");
                return;
            }

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial(textureName, tex, cutoff: 0.35f, windStrength: foliage ? 0.35f : 0f);
            SunsetCoastalBillboardUtil.SpawnCard($"Prop_{textureName}", parent, tex, mat,
                new Vector3(x, 0f, z), worldHeight, yRotation, castShadows: false, isStatic: !foliage);
        }
    }
}
#endif
