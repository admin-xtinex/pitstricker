#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.CameraSystem;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Orchestrates Map 02 "Sunset Coastal": duplicates the Village scene (so every
    /// shared gameplay/UI/networking system carries over untouched — see
    /// PIT_STRIKER_MAP02_SUNSET_COASTAL_MASTER_IMPLEMENTATION_ALL_PACKS.md), hides
    /// Village's decorative art, reskins the reused gameplay-critical objects, and
    /// builds the new Sunset Coastal art layer in their place.
    /// </summary>
    public static class SunsetCoastalSceneSetup
    {
        public const string Map02ScenePath = "Assets/_Project/Scenes/SC_SunsetCoastal_Map02.unity";

        [MenuItem("Pit Striker/Sunset Coastal/0. Setup Sunset Coastal Map 02 Scene (Full Pipeline)", false, 0)]
        public static void SetupFullPipeline()
        {
            AssetDatabase.Refresh();
            SunsetCoastalAssetImporter.ConfigureImportSettings();
            SunsetCoastalMaterialBuilder.BuildAllSurfaceMaterials();
            BuildScene();
        }

        [MenuItem("Pit Striker/Sunset Coastal/3. Build Sunset Coastal Scene", false, 12)]
        public static void BuildScene()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            {
                Debug.LogError("[SUNSET COASTAL SETUP] Scenes folder missing.");
                return;
            }

            if (!System.IO.File.Exists(Map02ScenePath))
            {
                Debug.LogError($"[SUNSET COASTAL SETUP] {Map02ScenePath} does not exist. The Village scene it was originally cloned from has been archived and removed, so this pipeline can no longer bootstrap the scene from scratch — restore Map02 from version control if it was deleted.");
                return;
            }

            var scene = EditorSceneManager.OpenScene(Map02ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[SUNSET COASTAL SETUP] Could not open {Map02ScenePath}");
                return;
            }

            Undo.SetCurrentGroupName("Setup Sunset Coastal Map 02");
            int undoGroup = Undo.GetCurrentGroup();

            RemoveVillageArt();
            RemoveStrayVillagePrefabInstances();
            var arenaRoot = ReskinArena();
            var artRoot = BuildNewArtLayer();
            SunsetCoastalLighting.Configure(artRoot);
            ConfigureCameraFraming();
            RemovePitFlagMarkers();

            SunsetCoastalBuildSettings.AddSceneAdditive(Map02ScenePath);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#00FF88><b>[SUNSET COASTAL SETUP SUCCESS]</b> Map 02 scene built: Village dressing removed, gameplay lane reskinned, new coastal art/lighting/camera in place.</color>");
        }

        /// <summary>
        /// Village's decorative dressing is gone for good now (Village_Reference_Upgrade
        /// referenced materials/meshes that no longer exist on disk), so it's deleted
        /// outright rather than left deactivated — an inactive-but-present subtree would
        /// otherwise carry ~120 dangling missing-asset references inside the saved scene.
        /// </summary>
        private static void RemoveVillageArt()
        {
            var villageArt = GameObject.Find("Village_Reference_Upgrade");
            if (villageArt == null)
            {
                var scene = EditorSceneManager.GetActiveScene();
                foreach (var root in scene.GetRootGameObjects())
                {
                    if (root.name == "Village_Reference_Upgrade") { villageArt = root; break; }
                }
            }
            if (villageArt != null)
            {
                Object.DestroyImmediate(villageArt);
            }
            else
            {
                Debug.LogWarning("[SUNSET COASTAL SETUP] 'Village_Reference_Upgrade' not found — already removed.");
            }
        }

        /// <summary>
        /// The Village scene carried a separate PrefabInstance ("Environment_Blender_
        /// Village_Graphics") nested outside Village_Reference_Upgrade, whose source
        /// prefab lived in the now-archived Village art folder. A fully-broken prefab
        /// instance doesn't expose a normal queryable GameObject.name via root iteration,
        /// so this finds it by prefab status instead and removes the whole instance —
        /// its source is intentionally gone for good, not something to repair.
        /// </summary>
        private static void RemoveStrayVillagePrefabInstances()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var removedRoots = new System.Collections.Generic.HashSet<GameObject>();

            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                var go = t.gameObject;
                if (go.scene != scene) continue;
                if (!PrefabUtility.IsPartOfPrefabInstance(go)) continue;
                if (PrefabUtility.GetPrefabInstanceStatus(go) != PrefabInstanceStatus.MissingAsset) continue;

                var instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                if (removedRoots.Add(instanceRoot))
                {
                    Debug.Log($"<color=#00FF88>[SUNSET COASTAL SETUP]</color> Removing broken prefab instance '{instanceRoot.name}' (missing source asset).");
                    Object.DestroyImmediate(instanceRoot);
                }
            }
        }

        private static GameObject ReskinArena()
        {
            var arena = GameObject.Find("Arena_Sandbox");
            if (arena == null)
            {
                Debug.LogError("[SUNSET COASTAL SETUP] 'Arena_Sandbox' not found — gameplay lane missing!");
                return null;
            }

            Material sand = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Packed", 8f);
            Material wood = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Wood_Weathered", 2f);
            Material pitWell = GetPitWellMaterial();

            SetRendererMaterial("Ground_Center_Fairway", arena.transform, sand);

            // Two entire Village decoration subtrees were nested inside Arena_Sandbox
            // itself, as siblings of the walls/pits/ground — easy to miss since a first
            // pass only touched the objects it already knew by name. "Gameplay_Kit_
            // Village" is chalk-drawn ground rings, a wooden pit-flag set, and a
            // signboard; "Environment_Village_Dressing" is a full village house, 8
            // stone rocks, a bench, a barricade, and market stalls. Both are purely
            // decorative Village dressing, unrelated to gameplay.
            foreach (var villageChildName in new[] { "Gameplay_Kit_Village", "Environment_Village_Dressing" })
            {
                var child = arena.transform.Find(villageChildName);
                if (child != null) Object.DestroyImmediate(child.gameObject);
            }

            foreach (var wallName in new[] { "Wall_Front", "Wall_Left", "Wall_Back", "Wall_Right" })
            {
                var wall = arena.transform.Find(wallName);
                if (wall == null) continue;
                var mr = wall.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false; // Physics-only: visual boundary is now the new post & rope fence.
            }

            foreach (var pitName in new[] { "Pit_01_Round", "Pit_02_Round", "Pit_03_Round" })
            {
                var pit = arena.transform.Find(pitName);
                if (pit == null) continue;
                SetRendererMaterial(pitName, arena.transform, pitWell);
                var lip = pit.Find("BrightRed_PitLip");
                if (lip != null)
                {
                    var lipRenderer = lip.GetComponent<MeshRenderer>();
                    if (lipRenderer != null) lipRenderer.sharedMaterial = wood;
                }
            }

            return arena;
        }

        private static Material GetPitWellMaterial()
        {
            const string path = "Assets/_Project/Art/Environments/SunsetCoastal/Materials/M_SC_PitWell.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Pit Striker/Village Surface");
                mat = new Material(shader) { name = "M_SC_PitWell" };
                mat.SetColor("_BaseColor", new Color(0.16f, 0.11f, 0.09f));
                mat.SetFloat("_Smoothness", 0.1f);
                AssetDatabase.CreateAsset(mat, path);
            }
            return mat;
        }

        private static void SetRendererMaterial(string childName, Transform parent, Material mat)
        {
            var t = parent.Find(childName);
            if (t == null) return;
            var mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = mat;
        }

        private static GameObject BuildNewArtLayer()
        {
            var existing = GameObject.Find("SunsetCoastal_Art");
            if (existing != null) Object.DestroyImmediate(existing);

            var artRoot = new GameObject("SunsetCoastal_Art");
            BuildGroundPlane(artRoot.transform);
            SunsetCoastalSkyWaterBuilder.Build(artRoot.transform);
            SunsetCoastalMeshBuilder.BuildHeroLayer(artRoot.transform);
            SunsetCoastalPropBuilder.BuildPropLayer(artRoot.transform);
            SunsetCoastalImpostorBuilder.BuildImpostorLayer(artRoot.transform);
            SunsetCoastalDecalBuilder.BuildDecalLayer(artRoot.transform);
            return artRoot;
        }

        private static void BuildGroundPlane(Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
            go.name = "SunsetCoastal_GroundSkirt";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(1.5f, -0.015f, 14.0f);
            go.transform.localScale = new Vector3(4.0f, 1f, 6.0f); // Unity plane primitive is 10x10 at scale 1.
            Object.DestroyImmediate(go.GetComponent<MeshCollider>());
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Dry", 12f);
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        private static void ConfigureCameraFraming()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null)
            {
                Debug.LogWarning("[SUNSET COASTAL SETUP] 'Main Camera' not found — skipping Camera A/B setup.");
                return;
            }
            var blender = camGo.GetComponent<MapCameraFramingBlender>();
            if (blender == null) blender = camGo.AddComponent<MapCameraFramingBlender>();
            blender.ConfigureDefaults();
        }

        /// <summary>
        /// Village marked each pit with a pole-and-ball flag marker. The approved
        /// reference shows plain pit holes with no markers, and PitZone's detection
        /// relies only on its own SphereCollider/world position — these flags are purely
        /// decorative Village dressing, not gameplay-relevant, so they're removed outright
        /// rather than reskinned.
        /// </summary>
        private static void RemovePitFlagMarkers()
        {
            foreach (var flagName in new[] { "StylizedFlag_Pit1", "StylizedFlag_Pit2", "StylizedFlag_Pit3" })
            {
                var flag = GameObject.Find(flagName);
                if (flag != null) Object.DestroyImmediate(flag);
            }
        }
    }
}
#endif
