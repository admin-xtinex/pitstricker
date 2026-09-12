#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 2 hero dressing — rendered as rich billboard cards using the actual Pack02
    /// illustrated reference art (HeroCoastalRock_Set_01, BoundaryLog_Module_01,
    /// WoodenPost_Rope_01, BeachShack_Hero_01, SmallFishingBoat_01), not the earlier
    /// Blender-modeled grey-PBR stand-ins. The map's camera never rotates freely (Camera
    /// A/B only, per the master doc), so a card facing the fixed viewing corridor reads
    /// far closer to the approved artwork than a generic tiling material ever could — and
    /// visual fidelity to the approved art is the doc's explicitly highest priority.
    /// Grounded cards (shack, boat) lean back slightly so they don't read as paper-flat
    /// under the map's steep downward camera tilt.
    /// </summary>
    public static class SunsetCoastalMeshBuilder
    {
        private const string Pack02 = "Pack02_Core_Environment_Assets";
        private const float LeftRailX = -4.5f;
        private const float RightRailX = 7.2f;
        private const float LaneStartZ = -6.5f;
        private const float LaneEndZ = 35.5f;

        public static GameObject BuildHeroLayer(Transform parent)
        {
            var root = new GameObject("SunsetCoastal_HeroProps");
            root.transform.SetParent(parent, false);

            BuildPostRopeFence(root.transform);
            BuildLogAccents(root.transform);
            BuildRockClusters(root.transform);
            BuildShack(root.transform);
            BuildBoat(root.transform);
            BuildHeroPalms(root.transform);
            BuildLanternTorches(root.transform);
            BuildShorelineRocks3D(root.transform);

            return root;
        }

        private static Texture2D LoadHeroTexture(string fileName)
        {
            string path = $"{SunsetCoastalAssetImporter.SourceRoot}/{Pack02}/Assets/{fileName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null) Debug.LogError($"[SUNSET COASTAL MESH] Missing hero texture: {path}");
            return tex;
        }

        private static void BuildPostRopeFence(Transform parent)
        {
            var fence = new GameObject("BoundaryFence_PostAndRope");
            fence.transform.SetParent(parent, false);
            var tex = LoadHeroTexture("WoodenPost_Rope_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("WoodenPost_Rope_01", tex, cutoff: 0.3f);

            // The source art is one square image showing a full post-to-post rope span,
            // so cards must tile at (roughly) their own on-screen width or the rail reads
            // as disconnected ticks with visible gaps between modules.
            const float cardHeight = 0.62f;
            const float spacing = cardHeight * 0.92f;
            foreach (float railX in new[] { LeftRailX, RightRailX })
            {
                bool facingLeft = railX < 0f;
                float yRot = facingLeft ? 90f : -90f;
                int count = Mathf.FloorToInt((LaneEndZ - LaneStartZ) / spacing);
                for (int i = 0; i < count; i++)
                {
                    float z = LaneStartZ + i * spacing;
                    SunsetCoastalBillboardUtil.SpawnCard($"PostRope_{(facingLeft ? "L" : "R")}_{i:00}", fence.transform,
                        tex, mat, new Vector3(railX, 0f, z), cardHeight, yRot);
                }
            }
        }

        /// <summary>
        /// Previously 6 sparse, tiny (0.35m) accents scattered across a 42m lane — the
        /// illustrated log art was effectively invisible at gameplay distance. Now a
        /// continuous curb of larger modules running the length of both rails, close to
        /// the fence base where the camera actually reads them.
        /// </summary>
        private static void BuildLogAccents(Transform parent)
        {
            var group = new GameObject("BoundaryLog_Accents");
            group.transform.SetParent(parent, false);
            var tex = LoadHeroTexture("BoundaryLog_Module_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("BoundaryLog_Module_01", tex, cutoff: 0.3f);

            const float worldHeight = 1.0f;
            const float spacing = 4.0f;
            foreach (float railX in new[] { LeftRailX, RightRailX })
            {
                bool left = railX < 0f;
                float x = left ? railX - 0.35f : railX + 0.35f;
                float yRot = left ? 90f : -90f;
                int count = Mathf.FloorToInt((LaneEndZ - LaneStartZ) / spacing);
                for (int i = 0; i < count; i++)
                {
                    float z = LaneStartZ + 1.0f + i * spacing;
                    SunsetCoastalBillboardUtil.SpawnCard($"LogAccent_{(left ? "L" : "R")}_{i:00}", group.transform,
                        tex, mat, new Vector3(x, 0f, z), worldHeight, yRot);
                }
            }
        }

        /// <summary>
        /// Previously placed 1.3-2.0m past the rail at small scale — easy to lose behind
        /// the fence and other dressing. Pulled closer to the rail and scaled up so the
        /// illustrated rock art (some of the richest detail in the whole pack) actually
        /// reads at gameplay distance. Lean reduced — 25° made the largest clusters look
        /// like they were tipping over.
        /// </summary>
        private static void BuildRockClusters(Transform parent)
        {
            var group = new GameObject("HeroRockClusters");
            group.transform.SetParent(parent, false);
            var tex = LoadHeroTexture("HeroCoastalRock_Set_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("HeroCoastalRock_Set_01", tex, cutoff: 0.3f);

            (float x, float z, float scale, float yRot)[] placements =
            {
                (LeftRailX - 0.9f, 2.0f, 2.1f, 20f),
                (RightRailX + 1.0f, 5.5f, 1.9f, -25f),
                (LeftRailX - 1.1f, 16.5f, 2.4f, 15f),
                (RightRailX + 0.9f, 22.0f, 2.0f, -10f),
                (LeftRailX - 0.9f, 31.0f, 2.0f, 30f),
                (RightRailX + 1.2f, 33.5f, 2.5f, -20f),
            };

            for (int i = 0; i < placements.Length; i++)
            {
                var (x, z, scale, yRot) = placements[i];
                SunsetCoastalBillboardUtil.SpawnCard($"RockCluster_{i:00}", group.transform, tex, mat,
                    new Vector3(x, 0f, z), scale, yRot, backwardLeanDegrees: 12f);
            }
        }

        private static void BuildShack(Transform parent)
        {
            var tex = LoadHeroTexture("BeachShack_Hero_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("BeachShack_Hero_01", tex, cutoff: 0.3f);
            // In the reference, the shack sits in the midground scenery band (roughly
            // where the fence/rock-and-foliage dressing begins), not at the tee — it was
            // previously placed only ~6m from the Camera A start, which read as a huge,
            // dominant shape crowding the very front of the shot instead of a modest
            // midground structure. The giant arching palms are the actual close-foreground
            // framing elements; the shack is not. No backward lean here — that treatment
            // suits rocks viewed from above but makes a building's straight vertical
            // walls/roofline look like it's toppling over.
            SunsetCoastalBillboardUtil.SpawnCard("BeachShack_Hero_01", parent, tex, mat,
                new Vector3(LeftRailX - 1.4f, 0f, 9.0f), 2.6f, 18f);
        }

        private static void BuildBoat(Transform parent)
        {
            var tex = LoadHeroTexture("SmallFishingBoat_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("SmallFishingBoat_01", tex, cutoff: 0.3f);
            SunsetCoastalBillboardUtil.SpawnCard("SmallFishingBoat_01", parent, tex, mat,
                new Vector3(RightRailX + 2.6f, 0f, LaneEndZ - 3.0f), 1.5f, -35f, backwardLeanDegrees: 30f);
        }

        /// <summary>
        /// Large near-camera palms arching into frame from both sides at the tee, matching
        /// the approved reference's foreground palm-frond framing. Positioned tall and close
        /// so their canopies read at the top of the shot from Camera A.
        /// </summary>
        private static void BuildHeroPalms(Transform parent)
        {
            var group = new GameObject("HeroPalms");
            group.transform.SetParent(parent, false);

            var tex1 = LoadHeroTexture("HeroPalm_01");
            var mat1 = SunsetCoastalMaterialBuilder.GetCardMaterial("HeroPalm_01", tex1, cutoff: 0.3f, windStrength: 0.4f);
            SunsetCoastalBillboardUtil.SpawnCard("HeroPalm_01", group.transform, tex1, mat1,
                new Vector3(LeftRailX - 1.6f, 0f, LaneStartZ + 1.0f), 6.5f, 35f, backwardLeanDegrees: 8f);

            var tex2 = LoadHeroTexture("HeroPalm_02");
            var mat2 = SunsetCoastalMaterialBuilder.GetCardMaterial("HeroPalm_02", tex2, cutoff: 0.3f, windStrength: 0.4f);
            SunsetCoastalBillboardUtil.SpawnCard("HeroPalm_02", group.transform, tex2, mat2,
                new Vector3(RightRailX + 1.8f, 0f, LaneStartZ + 2.5f), 5.8f, -30f, backwardLeanDegrees: 8f);
        }

        /// <summary>
        /// Lit torch/lantern posts lining the rail — the reference's warm glow accents.
        /// </summary>
        private static void BuildLanternTorches(Transform parent)
        {
            var group = new GameObject("LanternTorches");
            group.transform.SetParent(parent, false);
            var tex = LoadHeroTexture("LanternTorch_Set_01");
            var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("LanternTorch_Set_01", tex, cutoff: 0.3f);

            (float x, float z)[] placements =
            {
                (LeftRailX - 0.7f, 4.0f), (RightRailX + 0.7f, 8.0f),
                (LeftRailX - 0.7f, 19.5f), (RightRailX + 0.7f, 25.0f),
                (LeftRailX - 0.7f, 29.0f),
            };

            for (int i = 0; i < placements.Length; i++)
            {
                var (x, z) = placements[i];
                float yRot = x < 0 ? 90f : -90f;
                SunsetCoastalBillboardUtil.SpawnCard($"LanternTorch_{i:00}", group.transform, tex, mat,
                    new Vector3(x, 0f, z), 0.9f, yRot);
            }
        }

        /// <summary>
        /// The Blender-modeled rock mesh from the earlier 3D pass (Assets/_Project/Art/
        /// Models/SunsetCoastal/Heroes/HeroCoastalRock_Set_01.fbx) was left unused once
        /// the hero rocks moved to billboard cards for visual fidelity, which in turn left
        /// the Rock_Coastal PBR material (built from Pack04) with nowhere to actually
        /// apply. Real 3D rock outcrops at the water's edge use both: matches the doc's
        /// own "selected rocks" lightweight-3D guidance and reads well since real 3D holds
        /// up better than a flat card right at the shoreline seam the camera sits close to.
        /// </summary>
        private static void BuildShorelineRocks3D(Transform parent)
        {
            const string modelPath = "Assets/_Project/Art/Models/SunsetCoastal/Heroes/HeroCoastalRock_Set_01.fbx";
            var prefabSource = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefabSource == null)
            {
                Debug.LogWarning($"[SUNSET COASTAL MESH] Missing shoreline rock model: {modelPath}");
                return;
            }

            Material rockCoastal = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Rock_Coastal", 1.2f);
            var group = new GameObject("ShorelineRocks3D");
            group.transform.SetParent(parent, false);

            (float x, float z, float scale, float yRot)[] placements =
            {
                (8.6f, 27.0f, 1.1f, 15f),
                (9.4f, 34.0f, 0.9f, -20f),
                (7.8f, 40.0f, 1.3f, 40f),
            };

            for (int i = 0; i < placements.Length; i++)
            {
                var (x, z, scale, yRot) = placements[i];
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, group.transform);
                instance.name = $"ShorelineRock_{i:00}";
                instance.transform.localPosition = new Vector3(x, 0f, z);
                instance.transform.localRotation = Quaternion.Euler(0f, yRot, 0f);
                instance.transform.localScale = Vector3.one * scale;
                foreach (var mr in instance.GetComponentsInChildren<MeshRenderer>())
                {
                    mr.sharedMaterial = rockCoastal;
                    mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    GameObjectUtility.SetStaticEditorFlags(mr.gameObject, StaticEditorFlags.BatchingStatic);
                }
            }
        }
    }
}
#endif
