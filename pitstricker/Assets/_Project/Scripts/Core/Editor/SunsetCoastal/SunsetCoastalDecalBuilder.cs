#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 4 decals: Footprints, Shells/Starfish, Sand Scratches, Pit Edge Wear, Moss,
    /// Shoreline Foam — laid flat on the sand as ground quads. Kept subtle per the doc
    /// ("keep decals subtle"): small scale, sparse count, no shadows/collision.
    /// </summary>
    public static class SunsetCoastalDecalBuilder
    {
        private const string Pack04Decals = "Pack04_Materials_And_Decals/Decals";
        private const float LeftRailX = -4.5f;
        private const float RightRailX = 7.2f;

        public static GameObject BuildDecalLayer(Transform parent)
        {
            var root = new GameObject("SunsetCoastal_Decals");
            root.transform.SetParent(parent, false);

            // Footprints trailing down the lane, as if the last player walked it.
            SpawnDecal(root.transform, "Footprints_Decal", -0.6f, -4.0f, 0.9f, 8f);
            SpawnDecal(root.transform, "Footprints_Decal", 0.4f, 8.0f, 0.9f, -12f);
            SpawnDecal(root.transform, "Footprints_Decal", -0.3f, 20.0f, 0.9f, 5f);

            // Pit edge wear around each of the 3 pits (z = 3.0, 16.5, 31.0).
            SpawnDecal(root.transform, "PitEdge_Wear_Decal", 0f, 3.0f, 1.6f, 0f);
            SpawnDecal(root.transform, "PitEdge_Wear_Decal", 0f, 16.5f, 1.6f, 40f);
            SpawnDecal(root.transform, "PitEdge_Wear_Decal", 0f, 31.0f, 1.6f, -20f);

            // Shells / starfish scattered near the shoreline (right/end side).
            SpawnDecal(root.transform, "Shells_Starfish_Decal", RightRailX - 1.0f, 27.0f, 0.8f, 15f);
            SpawnDecal(root.transform, "Shells_Starfish_Decal", RightRailX - 1.6f, 33.0f, 0.7f, -30f);

            // Sand scratches / drag marks, a couple mid-lane for ground texture variety.
            SpawnDecal(root.transform, "Sand_Scratch_Decal", 1.0f, 10.0f, 1.3f, 60f);
            SpawnDecal(root.transform, "Sand_Scratch_Decal", -0.8f, 24.5f, 1.1f, -25f);

            // Moss patches near the shaded shack/foliage side.
            SpawnDecal(root.transform, "Moss_Patch_Decal", LeftRailX + 0.6f, LaneStartOffset(), 1.0f, 0f);
            SpawnDecal(root.transform, "Moss_Patch_Decal", LeftRailX + 0.9f, 13.0f, 0.9f, 45f);

            // Shoreline foam wash lines near the boat/lighthouse end of the lane.
            SpawnDecal(root.transform, "Shoreline_Foam_Decal", RightRailX + 0.5f, 30.0f, 2.4f, 10f);
            SpawnDecal(root.transform, "Shoreline_Foam_Decal", RightRailX + 1.2f, 34.5f, 2.0f, -15f);

            return root;
        }

        private static float LaneStartOffset() => -3.0f;

        private static void SpawnDecal(Transform parent, string textureName, float x, float z, float worldSize, float yRotation)
        {
            string path = $"{SunsetCoastalAssetImporter.SourceRoot}/{Pack04Decals}/{textureName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                Debug.LogWarning($"[SUNSET COASTAL DECALS] Missing decal texture: {path}");
                return;
            }

            var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial(textureName, tex);
            SunsetCoastalBillboardUtil.SpawnGroundDecal($"Decal_{textureName}_{x:00}_{z:00}", parent, tex, mat,
                new Vector3(x, 0.015f, z), worldSize, yRotation);
        }
    }
}
#endif
