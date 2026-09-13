#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Builds the rich 3D + 2.5D Boundary System for Sunset Coastal:
    /// - Physical BoxColliders along X = +/-2.3m for accurate marble bounce response.
    /// - High-fidelity painted coastal boundary fence cards (driftwood logs, rope windings, shells).
    /// - Continuous lush tropical foliage border (banana leaves, monstera, bright red hibiscus, birds of paradise, coastal rocks).
    /// - Slender authentic bamboo tiki torches with flickering flames placed safely outside the fairway.
    /// </summary>
    public static class SunsetCoastalBambooBoundary
    {
        public const float RailLeftX = -2.3f;
        public const float RailRightX = 2.3f;
        public const float StartZ = -8.5f;
        public const float EndZ = 35.5f;

        private const string Pack02 = "Pack02_Core_Environment_Assets";
        private const string Pack03 = "Pack03_Props_And_Scenery";

        public static GameObject BuildBoundarySystem(Transform parent)
        {
            var root = new GameObject("BoundarySystem_BambooLogs");
            root.transform.SetParent(parent, false);

            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");

            // 1. Physics Colliders along Left and Right rails
            CreateBoundaryCollider(root.transform, "Collider_Left", RailLeftX, StartZ, EndZ, bouncePhys);
            CreateBoundaryCollider(root.transform, "Collider_Right", RailRightX, StartZ, EndZ, bouncePhys);
            CreateEndCollider(root.transform, "Collider_Back", StartZ, RailLeftX, RailRightX, bouncePhys);
            CreateEndCollider(root.transform, "Collider_Front", EndZ, RailLeftX, RailRightX, bouncePhys);

            // 2. High-res painted Boundary Fence Modules
            BuildFenceCards(root.transform);

            // 3. Continuous Lush Tropical Foliage & Flower Borders along BOTH rails
            BuildContinuousLushBorders(root.transform);

            // 4. Slender Bamboo Tiki Torches
            BuildBoundaryTikiTorches(root.transform);

            return root;
        }

        private static void CreateBoundaryCollider(Transform parent, string name, float x, float startZ, float endZ, PhysicsMaterial physMat)
        {
            var colGo = new GameObject(name);
            colGo.transform.SetParent(parent, false);
            float length = endZ - startZ;
            colGo.transform.position = new Vector3(x, 0.40f, (startZ + endZ) * 0.5f);
            var col = colGo.AddComponent<BoxCollider>();
            col.size = new Vector3(0.35f, 0.90f, length + 0.4f);
            col.material = physMat;
        }

        private static void CreateEndCollider(Transform parent, string name, float z, float minX, float maxX, PhysicsMaterial physMat)
        {
            var colGo = new GameObject(name);
            colGo.transform.SetParent(parent, false);
            float length = maxX - minX;
            colGo.transform.position = new Vector3((minX + maxX) * 0.5f, 0.40f, z);
            var col = colGo.AddComponent<BoxCollider>();
            col.size = new Vector3(length + 0.4f, 0.90f, 0.35f);
            col.material = physMat;
        }

        private static void BuildFenceCards(Transform parent)
        {
            var fenceRoot = new GameObject("Boundary_Fence_Cards");
            fenceRoot.transform.SetParent(parent, false);

            Texture2D fenceTex = LoadSourceTexture(Pack03, "Crate_Set_01");
            if (fenceTex == null) fenceTex = LoadSourceTexture(Pack02, "BoundaryLog_Module_01");
            if (fenceTex == null) return;

            var matL = SunsetCoastalMaterialBuilder.GetCardMaterial("BoundaryFence_Left", fenceTex, cutoff: 0.25f);
            var matR = SunsetCoastalMaterialBuilder.GetCardMaterial("BoundaryFence_Right", fenceTex, cutoff: 0.25f);

            float cardHeight = 1.65f;
            float step = 2.6f;

            // Left Rail Fence Modules
            for (float z = -6.5f; z <= 34.0f; z += step)
            {
                SunsetCoastalBillboardUtil.SpawnCard($"Fence_L_{z:F1}", fenceRoot.transform, fenceTex, matL,
                    new Vector3(RailLeftX - 0.12f, 0f, z), cardHeight, yRotation: -8f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            }

            // Right Rail Fence Modules
            for (float z = -6.5f; z <= 34.0f; z += step)
            {
                SunsetCoastalBillboardUtil.SpawnCard($"Fence_R_{z:F1}", fenceRoot.transform, fenceTex, matR,
                    new Vector3(RailRightX + 0.12f, 0f, z), cardHeight, yRotation: 8f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            }

            // Back Rail (behind tee)
            SunsetCoastalBillboardUtil.SpawnCard("Fence_Back_1", fenceRoot.transform, fenceTex, matL,
                new Vector3(-1.2f, 0f, StartZ), cardHeight, yRotation: 0f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            SunsetCoastalBillboardUtil.SpawnCard("Fence_Back_2", fenceRoot.transform, fenceTex, matR,
                new Vector3(1.2f, 0f, StartZ), cardHeight, yRotation: 0f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);

            // Front Rail (behind pit 3)
            SunsetCoastalBillboardUtil.SpawnCard("Fence_Front_1", fenceRoot.transform, fenceTex, matL,
                new Vector3(-1.2f, 0f, EndZ), cardHeight, yRotation: 0f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            SunsetCoastalBillboardUtil.SpawnCard("Fence_Front_2", fenceRoot.transform, fenceTex, matR,
                new Vector3(1.2f, 0f, EndZ), cardHeight, yRotation: 0f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
        }

        private static void BuildContinuousLushBorders(Transform parent)
        {
            var borderRoot = new GameObject("Boundary_Lush_Borders");
            borderRoot.transform.SetParent(parent, false);

            Texture2D foliageTex = LoadSourceTexture(Pack03, "DockModule_01");
            Texture2D rockTex = LoadSourceTexture(Pack02, "HeroCoastalRock_Set_01");
            if (foliageTex == null) return;

            var foliageMat = SunsetCoastalMaterialBuilder.GetCardMaterial("FoliageBorder_Cluster", foliageTex, cutoff: 0.20f, windStrength: 0.20f);
            Material rockMat = rockTex != null ? SunsetCoastalMaterialBuilder.GetCardMaterial("RockBorder_Cluster", rockTex, cutoff: 0.25f) : null;

            float step = 2.4f;

            // 1. Left Rail Hedge
            int idx = 0;
            for (float z = -5.5f; z <= 34.0f; z += step)
            {
                float heightVar = 2.0f + ((idx % 3) * 0.25f);
                float xOffset = RailLeftX - 0.65f - ((idx % 2) * 0.20f);
                float rotVar = -12f + ((idx % 4) * 6f);

                SunsetCoastalBillboardUtil.SpawnCard($"Hedge_L_{idx:00}", borderRoot.transform, foliageTex, foliageMat,
                    new Vector3(xOffset, 0f, z), heightVar, yRotation: rotVar, castShadows: true, isStatic: false, backwardLeanDegrees: 20f);

                if (rockMat != null && idx % 2 == 1)
                {
                    SunsetCoastalBillboardUtil.SpawnCard($"Rock_L_{idx:00}", borderRoot.transform, rockTex, rockMat,
                        new Vector3(xOffset - 0.4f, 0f, z + 0.8f), 1.2f, yRotation: rotVar * 2f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
                }

                idx++;
            }

            // 2. Right Rail Hedge
            idx = 0;
            for (float z = -5.5f; z <= 34.0f; z += step)
            {
                // Leave a clear viewing gap at Z ~ 1.2m for the signpost
                if (z >= 0.5f && z <= 2.2f)
                {
                    idx++;
                    continue;
                }

                float heightVar = 2.0f + (((idx + 1) % 3) * 0.25f);
                float xOffset = RailRightX + 0.65f + ((idx % 2) * 0.20f);
                float rotVar = 12f - ((idx % 4) * 6f);

                SunsetCoastalBillboardUtil.SpawnCard($"Hedge_R_{idx:00}", borderRoot.transform, foliageTex, foliageMat,
                    new Vector3(xOffset, 0f, z), heightVar, yRotation: rotVar, castShadows: true, isStatic: false, backwardLeanDegrees: 20f);

                if (rockMat != null && idx % 2 == 0)
                {
                    SunsetCoastalBillboardUtil.SpawnCard($"Rock_R_{idx:00}", borderRoot.transform, rockTex, rockMat,
                        new Vector3(xOffset + 0.4f, 0f, z + 0.8f), 1.2f, yRotation: rotVar * 2f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
                }

                idx++;
            }
        }

        private static void BuildBoundaryTikiTorches(Transform parent)
        {
            var torchRoot = new GameObject("Boundary_TikiTorches");
            torchRoot.transform.SetParent(parent, false);

            const string torchPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/TikiTorch_Slender.png";
            var torchTex = AssetDatabase.LoadAssetAtPath<Texture2D>(torchPath);
            if (torchTex == null) return;

            var torchMat = SunsetCoastalMaterialBuilder.GetCardMaterial("TikiTorch_Slender", torchTex, cutoff: 0.15f);

            // Left side torches (safely behind the fence posts, not in fairway)
            CreateSlenderTorch(torchRoot.transform, "Torch_L_Near", new Vector3(RailLeftX - 0.55f, 0f, 0.5f), torchTex, torchMat, -12f);
            CreateSlenderTorch(torchRoot.transform, "Torch_L_Far", new Vector3(RailLeftX - 0.55f, 0f, 15.5f), torchTex, torchMat, -12f);

            // Right side torches (behind right fence posts)
            CreateSlenderTorch(torchRoot.transform, "Torch_R_Near", new Vector3(RailRightX + 0.55f, 0f, 2.5f), torchTex, torchMat, 12f);
            CreateSlenderTorch(torchRoot.transform, "Torch_R_Far", new Vector3(RailRightX + 0.55f, 0f, 15.5f), torchTex, torchMat, 12f);
        }

        private static void CreateSlenderTorch(Transform parent, string name, Vector3 pos, Texture2D tex, Material mat, float yRot)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = pos;

            SunsetCoastalBillboardUtil.SpawnCard("TorchVisual", root.transform, tex, mat,
                Vector3.zero, 2.4f, yRotation: yRot, castShadows: true, isStatic: false, backwardLeanDegrees: 20f);

            var lightGo = new GameObject("TorchLight");
            lightGo.transform.SetParent(root.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.8f, 0f);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.65f, 0.25f);
            light.intensity = 2.4f;
            light.range = 5.0f;
            light.shadows = LightShadows.None;
        }

        private static Texture2D LoadSourceTexture(string pack, string assetName, string subFolder = "Assets")
        {
            string path = $"{SunsetCoastalAssetImporter.SourceRoot}/{pack}/{subFolder}/{assetName}.png";
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (tex == null)
            {
                string altPath = $"{SunsetCoastalAssetImporter.SourceRoot}/{pack}/Props_2D/{assetName}.png";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(altPath);
            }
            if (tex == null)
            {
                string altPath2 = $"{SunsetCoastalAssetImporter.SourceRoot}/{pack}/Impostors/{assetName}.png";
                tex = AssetDatabase.LoadAssetAtPath<Texture2D>(altPath2);
            }
            return tex;
        }
    }
}
#endif
