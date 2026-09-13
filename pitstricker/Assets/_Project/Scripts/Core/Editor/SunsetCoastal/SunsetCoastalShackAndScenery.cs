#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Builds the rich, photorealistic Sunset Coastal environment matching Phase 4:
    /// - Left: Rustic beach shack with thatched roof, glowing yellow lanterns, surfboards, barrels,
    ///         tiki torches, and towering coconut palms arching overhead across the upper screen.
    /// - Center: Golden sand fairway, footprints, aiming trajectory arrow.
    /// - Right: Sloping beach sand, rustic 3-plank signpost nestled in red hibiscus flowers,
    ///          small wooden fishing boat on the shoreline, sparkling turquoise ocean,
    ///          distant rocky island with red-and-white lighthouse, and glowing golden setting sun.
    /// - Foreground: Lush tropical monstera leaves, red hibiscus blossoms, driftwood and rocks
    ///   framing the bottom-left (ForegroundFoliage_01) and bottom-right (ForegroundFoliage_02) corners.
    /// NO human avatars or mascots per user requirement.
    /// </summary>
    public static class SunsetCoastalShackAndScenery
    {
        private const string Pack01 = "Pack01_Starter_Impostors_And_Props";
        private const string Pack02 = "Pack02_Core_Environment_Assets";
        private const string Pack03 = "Pack03_Props_And_Scenery";
        private const string Pack04 = "Pack04_Materials_And_Decals";
        private const string Pack05 = "Pack05_Expanded_Impostors";

        public static GameObject BuildScenery(Transform parent)
        {
            var root = new GameObject("SunsetCoastal_Scenery");
            root.transform.SetParent(parent, false);

            Material woodMat = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Wood_Weathered", 2f);
            Material sandDry = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Dry", 6f);
            Material sandWet = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Wet", 4f);

            // 1. Terrain outside the fairway
            BuildGroundTerrain(root.transform, sandDry, sandWet);

            // 2. Left Flank: Beach Shack, Deck, Lanterns, Surfboards, Barrels, Arching Palms
            BuildBeachShack(root.transform, woodMat);
            BuildSurfboardsAndBarrels(root.transform);
            BuildOverheadArchingPalms(root.transform);

            // 3. Right Flank: Signpost with Hibiscus, Fishing Boat, Shoreline, Lighthouse & Sun
            BuildWoodenSignpost(root.transform);
            BuildFishingBoat(root.transform);
            BuildLighthouseAndSun(root.transform);

            // 4. Sky Backdrop & Ocean (handled by SunsetCoastalSkyWaterBuilder)
            SunsetCoastalSkyWaterBuilder.Build(root.transform);

            // 5. Sand Decals (Footprints, Shoreline Foam, Shells)
            BuildDecals(root.transform);

            // 6. Aiming Trajectory is dynamically rendered by SwipeLaunchController during aiming
            // BuildAimingTrajectoryArrow(root.transform);

            // 7. Foreground Framing Corners (Monstera, Hibiscus, Rocks)
            BuildForegroundFraming(root.transform);

            return root;
        }

        private static void BuildGroundTerrain(Transform parent, Material sandDry, Material sandWet)
        {
            var groundRoot = new GameObject("Terrain_Surrounds");
            groundRoot.transform.SetParent(parent, false);

            // Left side dry beach sand (covers outside left rail X = -2.3 to X = -14.0)
            var leftGround = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftGround.name = "Ground_LeftDrySand";
            leftGround.transform.SetParent(groundRoot.transform, false);
            leftGround.transform.position = new Vector3(-8.0f, -0.06f, 13.5f);
            leftGround.transform.localScale = new Vector3(11.4f, 0.10f, 48.0f);
            Object.DestroyImmediate(leftGround.GetComponent<Collider>());
            leftGround.GetComponent<MeshRenderer>().sharedMaterial = sandDry;

            // Right side beach sand (slopes gently from rail X = 2.3 to shoreline X = 4.8)
            var rightBeach = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightBeach.name = "Ground_RightBeachSand";
            rightBeach.transform.SetParent(groundRoot.transform, false);
            rightBeach.transform.position = new Vector3(3.55f, -0.06f, 13.5f);
            rightBeach.transform.localScale = new Vector3(2.5f, 0.10f, 48.0f);
            Object.DestroyImmediate(rightBeach.GetComponent<Collider>());
            rightBeach.GetComponent<MeshRenderer>().sharedMaterial = sandDry;
        }

        private static void BuildBeachShack(Transform parent, Material woodMat)
        {
            var shackRoot = new GameObject("BeachShack_Hero");
            shackRoot.transform.SetParent(parent, false);
            shackRoot.transform.position = new Vector3(-3.85f, 0f, 8.5f);

            // Wooden Deck Foundation
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.name = "Deck_Base";
            deck.transform.SetParent(shackRoot.transform, false);
            deck.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            deck.transform.localRotation = Quaternion.Euler(0f, 12f, 0f);
            deck.transform.localScale = new Vector3(3.8f, 0.18f, 5.2f);
            Object.DestroyImmediate(deck.GetComponent<Collider>());
            deck.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

            // High-resolution painted Beach Shack Hero Card
            Texture2D shackTex = LoadSourceTexture(Pack02, "BeachShack_Hero_01");
            if (shackTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("BeachShack_Hero_01", shackTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("ShackArtFacade", shackRoot.transform, shackTex, mat,
                    new Vector3(0.2f, 0.0f, 0f), 5.6f, yRotation: 12f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            }

            // Glowing Yellow Lanterns under the porch eaves
            BuildLantern(shackRoot.transform, "Lantern_Porch_Front", new Vector3(0.7f, 2.2f, -1.0f));
            BuildLantern(shackRoot.transform, "Lantern_Porch_Mid", new Vector3(1.0f, 2.3f, 0.8f));
            BuildLantern(shackRoot.transform, "Lantern_Porch_Back", new Vector3(1.3f, 2.4f, 2.2f));
        }

        private static void BuildLantern(Transform parent, string name, Vector3 pos)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = pos;

            var litShader = Shader.Find("Universal Render Pipeline/Unlit");
            var glowMat = new Material(litShader);
            glowMat.color = new Color(1.0f, 0.86f, 0.45f);

            var lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = "LampHousing";
            lamp.transform.SetParent(root.transform, false);
            lamp.transform.localScale = new Vector3(0.22f, 0.30f, 0.22f);
            Object.DestroyImmediate(lamp.GetComponent<Collider>());
            lamp.GetComponent<MeshRenderer>().sharedMaterial = glowMat;

            var light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1.0f, 0.72f, 0.35f);
            light.intensity = 2.6f;
            light.range = 5.5f;
            light.shadows = LightShadows.None;
        }

        private static void BuildSurfboardsAndBarrels(Transform parent)
        {
            var group = new GameObject("SurfboardsAndBarrels");
            group.transform.SetParent(parent, false);

            // Leaning Surfboard near shack porch
            Texture2D surfTex = LoadSourceTexture(Pack03, "Surfboard_Set_01");
            if (surfTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("Surfboard_Set_01", surfTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("Surfboard_Hero", group.transform, surfTex, mat,
                    new Vector3(-3.1f, 0.0f, 4.8f), 2.3f, yRotation: 15f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            }

            // Wooden Rum Barrels beside left boundary fence
            Texture2D barrelTex = LoadSourceTexture(Pack01, "WoodenBarrel_01", "Props_2D");
            if (barrelTex != null)
            {
                var barrelMat = SunsetCoastalMaterialBuilder.GetCardMaterial("WoodenBarrel_01", barrelTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("Barrel_01", group.transform, barrelTex, barrelMat,
                    new Vector3(-3.0f, 0.0f, 0.2f), 1.6f, yRotation: 18f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
                SunsetCoastalBillboardUtil.SpawnCard("Barrel_02", group.transform, barrelTex, barrelMat,
                    new Vector3(-3.3f, 0.0f, 1.0f), 1.4f, yRotation: -10f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
            }

            // Crates and fishing nets beside shack
            Texture2D crateTex = LoadSourceTexture(Pack03, "BoundaryFence_Variant_01");
            if (crateTex != null)
            {
                var crateMat = SunsetCoastalMaterialBuilder.GetCardMaterial("CrateNetSet", crateTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("Crates_Shack", group.transform, crateTex, crateMat,
                    new Vector3(-3.2f, 0.0f, 11.5f), 1.8f, yRotation: -15f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
            }
        }

        private static void BuildOverheadArchingPalms(Transform parent)
        {
            var palmRoot = new GameObject("OverheadArchingPalms");
            palmRoot.transform.SetParent(parent, false);

            Texture2D palm1Tex = LoadSourceTexture(Pack02, "HeroPalm_01");
            Texture2D palm2Tex = LoadSourceTexture(Pack02, "HeroPalm_02");

            if (palm1Tex != null)
            {
                var mat1 = SunsetCoastalMaterialBuilder.GetCardMaterial("HeroPalm_01", palm1Tex, cutoff: 0.20f, windStrength: 0.30f);
                // Palm 1: Near tee, arches dramatically into top-left and top-center of screen
                SunsetCoastalBillboardUtil.SpawnCard("Palm_Tee_Arch", palmRoot.transform, palm1Tex, mat1,
                    new Vector3(-3.6f, 0f, 0.2f), 9.2f, yRotation: 10f, castShadows: true, isStatic: false, backwardLeanDegrees: 12f);

                // Palm 3: Mid-flank behind shack
                SunsetCoastalBillboardUtil.SpawnCard("Palm_Mid_Arch", palmRoot.transform, palm1Tex, mat1,
                    new Vector3(-5.2f, 0f, 16.0f), 10.5f, yRotation: -5f, castShadows: true, isStatic: false, backwardLeanDegrees: 12f);
            }

            if (palm2Tex != null)
            {
                var mat2 = SunsetCoastalMaterialBuilder.GetCardMaterial("HeroPalm_02", palm2Tex, cutoff: 0.20f, windStrength: 0.25f);
                // Palm 2: Beside shack, arches over shack roof
                SunsetCoastalBillboardUtil.SpawnCard("Palm_Shack_Arch", palmRoot.transform, palm2Tex, mat2,
                    new Vector3(-4.8f, 0f, 8.0f), 10.8f, yRotation: 15f, castShadows: true, isStatic: false, backwardLeanDegrees: 12f);

                // Palm 4: Distant jungle tree
                SunsetCoastalBillboardUtil.SpawnCard("Palm_Far_Backdrop", palmRoot.transform, palm2Tex, mat2,
                    new Vector3(-6.0f, 0f, 26.0f), 10.0f, yRotation: 10f, castShadows: true, isStatic: false, backwardLeanDegrees: 12f);
            }
        }

        private static void BuildWoodenSignpost(Transform parent)
        {
            var signRoot = new GameObject("Signpost_GoodGamesBetterFriends");
            signRoot.transform.SetParent(parent, false);

            // Rustic 3-plank signpost ("GOOD GAMES / BETTER FRIENDS / PIT STRIKER")
            const string signTexPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/BeachSignpost_Planks_01.png";
            var signTex = AssetDatabase.LoadAssetAtPath<Texture2D>(signTexPath);
            if (signTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("BeachSignpost_Planks_01", signTex, cutoff: 0.20f);
                SunsetCoastalBillboardUtil.SpawnCard("Signpost_Planks", signRoot.transform, signTex, mat,
                    new Vector3(2.85f, 0f, 1.8f), 2.1f, yRotation: -12f, castShadows: true, isStatic: true, backwardLeanDegrees: 18f);
            }

            // Bright red hibiscus blossoms and foliage clustered right around the sign base
            Texture2D flowersTex = LoadSourceTexture(Pack03, "DockModule_01");
            if (flowersTex != null)
            {
                var fMat = SunsetCoastalMaterialBuilder.GetCardMaterial("Signpost_Hibiscus", flowersTex, cutoff: 0.20f);
                SunsetCoastalBillboardUtil.SpawnCard("Signpost_Flowers", signRoot.transform, flowersTex, fMat,
                    new Vector3(2.75f, 0f, 1.5f), 1.8f, yRotation: 15f, castShadows: true, isStatic: false, backwardLeanDegrees: 18f);
            }
        }

        private static void BuildFishingBoat(Transform parent)
        {
            var boatRoot = new GameObject("SmallFishingBoat");
            boatRoot.transform.SetParent(parent, false);

            // Painted wooden fishing boat resting on wet sand shoreline
            Texture2D boatTex = LoadSourceTexture(Pack02, "SmallFishingBoat_01");
            if (boatTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("SmallFishingBoat_01", boatTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("Boat_Hero", boatRoot.transform, boatTex, mat,
                    new Vector3(3.95f, 0.0f, 6.8f), 3.3f, yRotation: -28f, castShadows: true, isStatic: true, backwardLeanDegrees: 16f);
            }

            // Coastal rocks nestled beside boat
            Texture2D rockTex = LoadSourceTexture(Pack02, "HeroCoastalRock_Set_01");
            if (rockTex != null)
            {
                var rMat = SunsetCoastalMaterialBuilder.GetCardMaterial("Boat_Rocks", rockTex, cutoff: 0.25f);
                SunsetCoastalBillboardUtil.SpawnCard("Boat_Rock_Cluster", boatRoot.transform, rockTex, rMat,
                    new Vector3(4.4f, 0.0f, 8.2f), 1.8f, yRotation: 20f, castShadows: true, isStatic: true, backwardLeanDegrees: 15f);
            }
        }

        private static void BuildLighthouseAndSun(Transform parent)
        {
            var vistaRoot = new GameObject("DistantVistas_LighthouseAndSun");
            vistaRoot.transform.SetParent(parent, false);

            // 1. Rocky Island with Red-and-White Lighthouse in open ocean
            Texture2D islandTex = LoadSourceTexture(Pack05, "LighthouseIsland_Expanded_A", "Impostors");
            if (islandTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetCardMaterial("LighthouseIsland_Expanded_A", islandTex, cutoff: 0.15f);
                SunsetCoastalBillboardUtil.SpawnCard("LighthouseIsland", vistaRoot.transform, islandTex, mat,
                    new Vector3(9.8f, 0.6f, 38.0f), 5.2f, yRotation: 0f, castShadows: false, isStatic: true);
            }

            // 2. Warm glowing beacon light on the lighthouse tower
            var beaconGo = new GameObject("Lighthouse_BeaconLight");
            beaconGo.transform.SetParent(vistaRoot.transform, false);
            beaconGo.transform.position = new Vector3(10.6f, 4.6f, 37.8f);
            var beaconLight = beaconGo.AddComponent<Light>();
            beaconLight.type = LightType.Point;
            beaconLight.color = new Color(1.0f, 0.90f, 0.50f);
            beaconLight.intensity = 4.0f;
            beaconLight.range = 15.0f;
            beaconLight.shadows = LightShadows.None;

            // 3. Glowing Setting Sun near horizon
            var sunGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunGo.name = "Sunset_Sun_Disc";
            sunGo.transform.SetParent(vistaRoot.transform, false);
            sunGo.transform.position = new Vector3(11.4f, 3.4f, 41.0f);
            sunGo.transform.localScale = Vector3.one * 2.8f;
            Object.DestroyImmediate(sunGo.GetComponent<Collider>());
            var sunMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            sunMat.color = new Color(1.0f, 0.95f, 0.75f, 1f);
            sunGo.GetComponent<MeshRenderer>().sharedMaterial = sunMat;

            // Sun corona halo
            var sunHalo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sunHalo.name = "Sunset_Sun_Corona";
            sunHalo.transform.SetParent(vistaRoot.transform, false);
            sunHalo.transform.position = new Vector3(11.4f, 3.4f, 41.2f);
            sunHalo.transform.localScale = Vector3.one * 5.2f;
            Object.DestroyImmediate(sunHalo.GetComponent<Collider>());
            var haloMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            haloMat.color = new Color(1.0f, 0.80f, 0.40f, 0.5f);
            sunHalo.GetComponent<MeshRenderer>().sharedMaterial = haloMat;
        }

        private static void BuildDecals(Transform parent)
        {
            var decalRoot = new GameObject("Sand_Decals");
            decalRoot.transform.SetParent(parent, false);

            // Footprints along the sand fairway
            Texture2D footTex = LoadSourceTexture(Pack04, "Footprints_Decal", "Decals");
            if (footTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Footprints_Decal", footTex);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Footprints_01", decalRoot.transform, footTex, mat,
                    new Vector3(0.15f, 0.008f, -3.8f), 1.2f, 6f);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Footprints_02", decalRoot.transform, footTex, mat,
                    new Vector3(-0.25f, 0.008f, 7.5f), 1.2f, -10f);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Footprints_03", decalRoot.transform, footTex, mat,
                    new Vector3(0.10f, 0.008f, 19.5f), 1.2f, 4f);
            }

            // Shoreline foam wash lines along the water's edge
            Texture2D foamTex = LoadSourceTexture(Pack04, "Shoreline_Foam_Decal", "Decals");
            if (foamTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Shoreline_Foam_Decal", foamTex);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Foam_01", decalRoot.transform, foamTex, mat,
                    new Vector3(4.8f, -0.02f, 5.0f), 3.2f, 8f);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Foam_02", decalRoot.transform, foamTex, mat,
                    new Vector3(4.9f, -0.02f, 13.0f), 3.4f, -12f);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Foam_03", decalRoot.transform, foamTex, mat,
                    new Vector3(5.0f, -0.02f, 22.0f), 3.4f, 5f);
            }

            // Shells & Starfish on sand
            Texture2D shellTex = LoadSourceTexture(Pack04, "Shells_Starfish_Decal", "Decals");
            if (shellTex != null)
            {
                var mat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Shells_Starfish_Decal", shellTex);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Shells_01", decalRoot.transform, shellTex, mat,
                    new Vector3(1.6f, 0.008f, -4.5f), 0.9f, 25f);
                SunsetCoastalBillboardUtil.SpawnGroundDecal("Shells_02", decalRoot.transform, shellTex, mat,
                    new Vector3(3.2f, 0.008f, 3.5f), 1.0f, -40f);
            }
        }

        private static void BuildAimingTrajectoryArrow(Transform parent)
        {
            var arrowRoot = new GameObject("AimingTrajectory_DottedArrow");
            arrowRoot.transform.SetParent(parent, false);

            var lineMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMat.color = Color.white;

            // Dotted white trajectory line leading from blue marble toward Pit 1
            float startZ = -5.0f;
            float endZ = -1.2f;
            int dotCount = 8;
            for (int i = 0; i < dotCount; i++)
            {
                float t = (float)i / (dotCount - 1);
                float z = Mathf.Lerp(startZ, endZ, t);

                var dot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                dot.name = $"Dot_{i}";
                dot.transform.SetParent(arrowRoot.transform, false);
                dot.transform.localPosition = new Vector3(0f, 0.015f, z);
                dot.transform.localScale = new Vector3(0.09f, 0.008f, 0.20f);
                Object.DestroyImmediate(dot.GetComponent<Collider>());
                dot.GetComponent<MeshRenderer>().sharedMaterial = lineMat;
            }

            // Arrow head pointing towards Pit 1
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "ArrowHead";
            head.transform.SetParent(arrowRoot.transform, false);
            head.transform.localPosition = new Vector3(0f, 0.015f, -0.92f);
            head.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            head.transform.localScale = new Vector3(0.26f, 0.008f, 0.26f);
            Object.DestroyImmediate(head.GetComponent<Collider>());
            head.GetComponent<MeshRenderer>().sharedMaterial = lineMat;
        }

        private static void BuildForegroundFraming(Transform parent)
        {
            var fgRoot = new GameObject("ForegroundFraming_Foliage");
            fgRoot.transform.SetParent(parent, false);

            Texture2D fg1Tex = LoadSourceTexture(Pack01, "ForegroundFoliage_01", "Impostors");
            Texture2D fg2Tex = LoadSourceTexture(Pack01, "ForegroundFoliage_02", "Impostors");

            // Bottom-Left Framing: Lush Monstera, Red Hibiscus, Plumeria, Driftwood & Rocks
            if (fg1Tex != null)
            {
                var mat1 = SunsetCoastalMaterialBuilder.GetCardMaterial("ForegroundFoliage_01", fg1Tex, cutoff: 0.20f, windStrength: 0.15f);
                SunsetCoastalBillboardUtil.SpawnCard("FG_BottomLeft_Cluster", fgRoot.transform, fg1Tex, mat1,
                    new Vector3(-2.85f, 0.85f, -8.2f), 3.6f, yRotation: 12f, castShadows: false, isStatic: false, backwardLeanDegrees: 24.5f);
            }

            // Bottom-Right Framing: Lush Banana Leaves, Monstera, Red Hibiscus, Driftwood & Rocks
            if (fg2Tex != null)
            {
                var mat2 = SunsetCoastalMaterialBuilder.GetCardMaterial("ForegroundFoliage_02", fg2Tex, cutoff: 0.20f, windStrength: 0.15f);
                SunsetCoastalBillboardUtil.SpawnCard("FG_BottomRight_Cluster", fgRoot.transform, fg2Tex, mat2,
                    new Vector3(2.85f, 0.85f, -8.2f), 3.6f, yRotation: -12f, castShadows: false, isStatic: false, backwardLeanDegrees: 24.5f);
            }
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
