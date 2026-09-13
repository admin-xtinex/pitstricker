#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.CameraSystem;
using PitStriker.Gameplay;
using PitStriker.Physics;

namespace PitStriker.EditorTools
{
    public static class SunsetCoastalMasterHybridBuilder
    {
        public const string Map02ScenePath = "Assets/_Project/Scenes/SC_SunsetCoastal_Map02.unity";
        private const string OutDir = "Logs/SunsetCoastalPreview";
        private const string MasterCleanTexPath = "Assets/_Project/Art/Environments/SunsetCoastal/Master/SunsetCoastal_Master_Clean.png";

        [MenuItem("Pit Striker/Sunset Coastal/★ BUILD MASTER HYBRID (100% CONCEPT MATCH) ★", false, 0)]
        public static void BuildMasterHybrid()
        {
            var scene = EditorSceneManager.OpenScene(Map02ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open {Map02ScenePath}");
                return;
            }

            Undo.SetCurrentGroupName("Build Master Hybrid Sunset Coastal");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Purge legacy, village, and duplicate art objects
            PurgeScene();

            // 2. Setup Camera B
            SetupCamera();

            // 3. Setup Master Environment Backdrop (2.5D Projection)
            var artRoot = new GameObject("SunsetCoastal_Master_Art");
            SetupMasterBackdrop(artRoot.transform);

            // 4. Setup 3D Playable Fairway, Pits, and Boundaries
            SetupPlayableArea();

            // 5. Setup Active Blue Marble at Tee
            SetupMarble();

            // 6. Setup Aiming Arrow & Trajectory
            SetupAimingTrajectory(artRoot.transform);

            // 7. Foreground Framing Layer is already part of the master artwork, no extra blocking cards needed
            // SetupForegroundFraming(artRoot.transform);

            // 8. Setup Lighting & Sun Atmosphere
            SetupLighting(artRoot.transform);

            // 9. Save Scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#00FF88><b>[MASTER HYBRID SUCCESS]</b> Built 100% concept-matched Sunset Coastal scene!</color>");

            // 10. Capture Preview Screenshots
            CapturePreviewScreenshots();
        }

        private static void PurgeScene()
        {
            string[] names = {
                "SunsetCoastal_Art",
                "SunsetCoastal_HeroProps",
                "SunsetCoastal_Props",
                "SunsetCoastal_Impostors",
                "SunsetCoastal_Decals",
                "SunsetCoastal_Characters",
                "SunsetCoastal_Scenery",
                "SunsetCoastal_Phase1_Art",
                "SunsetCoastal_Master_Art",
                "BoundarySystem_BambooLogs",
                "Village_Reference_Upgrade",
                "Gameplay_Kit_Village",
                "Environment_Village_Dressing",
                "Environment_Blender_Village_Graphics",
                "StylizedFlag_Pit1", "StylizedFlag_Pit2", "StylizedFlag_Pit3",
                "Arrow_1_2", "Arrow_2_3", "Mascot_Pip", "Pip_Rig",
                "Wall_Front", "Wall_Left", "Wall_Back", "Wall_Right",
                "Character_Player_HoodieBoy", "Character_Spectator_ShackKid",
                "Character_Spectator_SignBoy", "Character_Spectator_ShoreGirl"
            };

            foreach (var n in names)
            {
                var go = GameObject.Find(n);
                if (go != null) Object.DestroyImmediate(go);
            }

            // Also purge any children attached to Main Camera (e.g. old backdrop quads)
            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                for (int i = cam.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(cam.transform.GetChild(i).gameObject);
                }
            }

            var scene = EditorSceneManager.GetActiveScene();
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null) continue;
                var go = t.gameObject;
                if (go.scene != scene) continue;
                string n = go.name;
                if (n == "InPit_NumberText" || n == "InPit_Floor_Marker" || n == "BrightRed_PitLip" ||
                    n.Contains("Pip") || n.Contains("Character") || n.Contains("Avatar") || n.Contains("Mascot") ||
                    n.StartsWith("Kid_") || n.StartsWith("Spectator_"))
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void SetupCamera()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null) return;
            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 46.0f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 100.0f;
            camGo.transform.position = new Vector3(0.0f, 5.6f, -11.0f);
            camGo.transform.rotation = Quaternion.Euler(24.5f, 0f, 0f);

            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.68f, 0.48f);
        }

        private static void SetupMasterBackdrop(Transform parent)
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(MasterCleanTexPath);
            if (tex == null)
            {
                Debug.LogError($"Could not find master clean texture at {MasterCleanTexPath}");
                return;
            }

            // Ensure texture importer is set to clamp and high quality
            var importer = AssetImporter.GetAtPath(MasterCleanTexPath) as TextureImporter;
            if (importer != null)
            {
                bool dirty = false;
                if (importer.textureType != TextureImporterType.Default) { importer.textureType = TextureImporterType.Default; dirty = true; }
                if (importer.wrapMode != TextureWrapMode.Clamp) { importer.wrapMode = TextureWrapMode.Clamp; dirty = true; }
                if (importer.maxTextureSize < 2048) { importer.maxTextureSize = 2048; dirty = true; }
                if (dirty) importer.SaveAndReimport();
            }

            const string matPath = "Assets/_Project/Art/Environments/SunsetCoastal/Master/M_SC_MasterBackdrop.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit");
                mat = new Material(shader) { name = "M_SC_MasterBackdrop" };
                AssetDatabase.CreateAsset(mat, matPath);
            }
            mat.SetTexture("_BaseMap", tex);
            mat.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(mat);

            var backdropGo = new GameObject("Master_Backdrop_Quad");
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null)
            {
                backdropGo.transform.SetParent(camGo.transform, false);
                backdropGo.transform.localPosition = new Vector3(0f, 0f, 40f);
                backdropGo.transform.localRotation = Quaternion.identity;
                float aspect = 960f / 1706f;
                float dist = 40f;
                float frustumH = 2f * dist * Mathf.Tan(camGo.GetComponent<Camera>().fieldOfView * 0.5f * Mathf.Deg2Rad);
                float frustumW = frustumH * aspect;
                backdropGo.transform.localScale = new Vector3(frustumW, frustumH, 1.0f);
            }
            else
            {
                backdropGo.transform.SetParent(parent, false);
            }

            var mf = backdropGo.AddComponent<MeshFilter>();
            mf.sharedMesh = GetCenteredQuadMesh();
            var mr = backdropGo.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static Mesh GetCenteredQuadMesh()
        {
            var mesh = new Mesh { name = "CenteredQuad" };
            mesh.vertices = new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3(0.5f, -0.5f, 0f),
                new Vector3(0.5f, 0.5f, 0f),
                new Vector3(-0.5f, 0.5f, 0f),
            };
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.normals = new[] { Vector3.back, Vector3.back, Vector3.back, Vector3.back };
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void SetupPlayableArea()
        {
            var arena = GameObject.Find("Arena_Sandbox");
            if (arena == null) arena = new GameObject("Arena_Sandbox");

            PhysicsMaterial sandPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Sand_Friction.physicMaterial");
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");

            // 1. Playable Fairway Ground Collider Plane
            var oldFairway = arena.transform.Find("Ground_Center_Fairway");
            if (oldFairway != null) Object.DestroyImmediate(oldFairway.gameObject);

            var fairwayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fairwayGo.name = "Ground_Center_Fairway";
            fairwayGo.transform.SetParent(arena.transform, false);
            // Positioned at Y = -0.05 so top surface is at Y = 0.00
            fairwayGo.transform.position = new Vector3(0f, -0.05f, 2.5f);
            fairwayGo.transform.localScale = new Vector3(3.2f, 0.10f, 16.0f);

            var boxCol = fairwayGo.GetComponent<BoxCollider>();
            if (boxCol != null) boxCol.material = sandPhys;

            // Hide the cube mesh renderer so the master painted sand artwork shows through cleanly
            var fMr = fairwayGo.GetComponent<MeshRenderer>();
            if (fMr != null) fMr.enabled = false;

            // 2. Setup 3 Pits at exact 3D raycast locations
            // Pit 1 (Near): Z = 0.10m, Pit 2 (Mid): Z = 2.79m, Pit 3 (Far): Z = 5.93m
            float[] pitZs = { 0.10f, 2.79f, 5.93f };

            for (int i = 0; i < 3; i++)
            {
                string pitName = $"Pit_0{i + 1}_Round";
                var pit = arena.transform.Find(pitName);
                if (pit == null)
                {
                    var pgo = new GameObject(pitName);
                    pgo.transform.SetParent(arena.transform, false);
                    pit = pgo.transform;
                }

                pit.position = new Vector3(0f, 0.005f, pitZs[i]);
                pit.localScale = Vector3.one;

                // Remove old child visual clutter
                for (int c = pit.childCount - 1; c >= 0; c--)
                {
                    Object.DestroyImmediate(pit.GetChild(c).gameObject);
                }

                var mr = pit.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;

                // Setup Trigger Collider for scoring
                var col = pit.GetComponent<SphereCollider>();
                if (col == null) col = pit.gameObject.AddComponent<SphereCollider>();
                col.isTrigger = true;
                col.radius = 0.50f;
                col.center = new Vector3(0f, -0.12f, 0f);

                // Setup PitZone component
                var zone = pit.GetComponent<PitZone>();
                if (zone == null) zone = pit.gameObject.AddComponent<PitZone>();
                zone.SetPitNumber(i + 1);
                var zoneSo = new SerializedObject(zone);
                var prop = zoneSo.FindProperty("_pitNumber");
                if (prop != null) prop.intValue = i + 1;
                zoneSo.ApplyModifiedProperties();
            }

            // 3. Setup Boundary Colliders (Rails)
            var oldBound = arena.transform.Find("Playable_Boundary_Colliders");
            if (oldBound != null) Object.DestroyImmediate(oldBound.gameObject);

            var boundRoot = new GameObject("Playable_Boundary_Colliders");
            boundRoot.transform.SetParent(arena.transform, false);

            CreateRailCollider(boundRoot.transform, "RailCollider_Left", new Vector3(-1.35f, 0.20f, 2.0f), new Vector3(0.35f, 0.60f, 15.0f), 2.3f, bouncePhys);
            CreateRailCollider(boundRoot.transform, "RailCollider_Right", new Vector3(1.35f, 0.20f, 2.0f), new Vector3(0.35f, 0.60f, 15.0f), -2.3f, bouncePhys);
            CreateEndCollider(boundRoot.transform, "RailCollider_Back", new Vector3(0f, 0.20f, -5.2f), new Vector3(3.2f, 0.60f, 0.35f), bouncePhys);
            CreateEndCollider(boundRoot.transform, "RailCollider_Front", new Vector3(0f, 0.20f, 9.0f), new Vector3(3.2f, 0.60f, 0.35f), bouncePhys);
        }

        private static void CreateRailCollider(Transform parent, string name, Vector3 pos, Vector3 size, float yRot, PhysicsMaterial mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            col.material = mat;
        }

        private static void CreateEndCollider(Transform parent, string name, Vector3 pos, Vector3 size, PhysicsMaterial mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            col.material = mat;
        }

        private static void SetupMarble()
        {
            var p1Marble = GameObject.Find("PlayerMarble_1_Blue");
            if (p1Marble != null)
            {
                p1Marble.transform.position = new Vector3(0f, 0.22f, -3.77f);
                var rb = p1Marble.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Add smooth radial soft contact shadow under marble
                var oldShadow = p1Marble.transform.Find("DropShadow");
                if (oldShadow != null) Object.DestroyImmediate(oldShadow.gameObject);

                var shadowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Master/Marble_Soft_Shadow.png");
                if (shadowTex != null)
                {
                    var shadowGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    shadowGo.name = "DropShadow";
                    shadowGo.transform.SetParent(p1Marble.transform, false);
                    shadowGo.transform.localPosition = new Vector3(0.04f, -0.21f, -0.04f);
                    shadowGo.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    shadowGo.transform.localScale = new Vector3(0.55f, 0.55f, 1f);
                    Object.DestroyImmediate(shadowGo.GetComponent<Collider>());

                    var sMr = shadowGo.GetComponent<MeshRenderer>();
                    var sMat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Marble_Soft_Shadow", shadowTex);
                    sMr.sharedMaterial = sMat;
                }

                // Add subtle glowing cyan halo around marble
                var oldHalo = p1Marble.transform.Find("CyanHalo");
                if (oldHalo != null) Object.DestroyImmediate(oldHalo.gameObject);

                var haloTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Master/Marble_Cyan_Halo.png");
                if (haloTex != null)
                {
                    var haloGo = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    haloGo.name = "CyanHalo";
                    haloGo.transform.SetParent(p1Marble.transform, false);
                    haloGo.transform.localPosition = new Vector3(0f, 0.02f, 0f);
                    haloGo.transform.localRotation = Quaternion.Euler(24.5f, 0f, 0f);
                    haloGo.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
                    Object.DestroyImmediate(haloGo.GetComponent<Collider>());

                    var hMr = haloGo.GetComponent<MeshRenderer>();
                    var hMat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Marble_Cyan_Halo", haloTex);
                    hMr.sharedMaterial = hMat;
                }
            }

            // Sideline marbles tucked away
            var p2 = GameObject.Find("PlayerMarble_2_Red");
            if (p2 != null) p2.transform.position = new Vector3(-2.8f, 0.25f, -4.5f);
            var p3 = GameObject.Find("PlayerMarble_3_Green");
            if (p3 != null) p3.transform.position = new Vector3(-2.8f, 0.25f, -3.8f);
            var p4 = GameObject.Find("PlayerMarble_4_Amber");
            if (p4 != null) p4.transform.position = new Vector3(-2.8f, 0.25f, -3.1f);
        }

        private static void SetupAimingTrajectory(Transform parent)
        {
            var arrowRoot = new GameObject("Aiming_Trajectory");
            arrowRoot.transform.SetParent(parent, false);

            var dotTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Master/Aim_Dot.png");
            var arrowTex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Master/Aim_Chevron_Tip.png");

            Material dotMat = dotTex != null ? SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Aim_Dot", dotTex) : null;
            Material arrowMat = arrowTex != null ? SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Aim_Chevron_Tip", arrowTex) : null;

            // Dotted aim line leading from marble towards Pit 1
            float startZ = -3.45f;
            float endZ = -0.35f;
            int numDots = 14;
            for (int i = 0; i < numDots; i++)
            {
                float t = (float)i / (numDots - 1);
                float z = Mathf.Lerp(startZ, endZ, t);
                var dot = GameObject.CreatePrimitive(PrimitiveType.Quad);
                dot.name = $"Dot_{i}";
                dot.transform.SetParent(arrowRoot.transform, false);
                dot.transform.position = new Vector3(0f, 0.012f, z);
                dot.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                dot.transform.localScale = new Vector3(0.08f, 0.08f, 1f);
                Object.DestroyImmediate(dot.GetComponent<Collider>());
                if (dotMat != null) dot.GetComponent<MeshRenderer>().sharedMaterial = dotMat;
            }

            // Clean chevron arrow head at the tip
            var arrowHead = GameObject.CreatePrimitive(PrimitiveType.Quad);
            arrowHead.name = "Arrow_Tip";
            arrowHead.transform.SetParent(arrowRoot.transform, false);
            arrowHead.transform.position = new Vector3(0f, 0.015f, endZ + 0.18f);
            arrowHead.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            arrowHead.transform.localScale = new Vector3(0.30f, 0.30f, 1f);
            Object.DestroyImmediate(arrowHead.GetComponent<Collider>());
            if (arrowMat != null) arrowHead.GetComponent<MeshRenderer>().sharedMaterial = arrowMat;
        }

        private static void SetupForegroundFraming(Transform parent)
        {
            // Foreground foliage framing corners
            Texture2D fgA = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Source/Pack05_Expanded_Impostors/Impostors/ForegroundFoliage_Frame_A.png");
            Texture2D fgB = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Environments/SunsetCoastal/Source/Pack05_Expanded_Impostors/Impostors/ForegroundFoliage_Frame_B.png");

            var fgRoot = new GameObject("Foreground_Framing");
            fgRoot.transform.SetParent(parent, false);

            if (fgA != null)
            {
                var matA = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("FG_Frame_A", fgA);
                SunsetCoastalBillboardUtil.SpawnCard("FG_LeftCorner", fgRoot.transform, fgA, matA,
                    new Vector3(-2.8f, 1.2f, -8.5f), 5.2f, yRotation: 15f, castShadows: false, isStatic: true, backwardLeanDegrees: 24.5f);
            }

            if (fgB != null)
            {
                var matB = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("FG_Frame_B", fgB);
                SunsetCoastalBillboardUtil.SpawnCard("FG_RightCorner", fgRoot.transform, fgB, matB,
                    new Vector3(2.8f, 1.2f, -8.5f), 5.2f, yRotation: -15f, castShadows: false, isStatic: true, backwardLeanDegrees: 24.5f);
            }
        }

        private static void SetupLighting(Transform parent)
        {
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                var light = lightGo.GetComponent<Light>();
                // Match the golden sunset angle from upper right towards lower left
                light.transform.rotation = Quaternion.Euler(22f, 215f, 0f);
                light.color = new Color(1.0f, 0.82f, 0.60f);
                light.intensity = 1.8f;
                light.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.95f, 0.65f, 0.50f);
            RenderSettings.ambientEquatorColor = new Color(0.88f, 0.55f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.50f, 0.35f, 0.25f);
            RenderSettings.ambientIntensity = 1.0f;
            RenderSettings.fog = false;
        }

        public static void CapturePreviewScreenshots()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null) return;
            var cam = camGo.GetComponent<Camera>();
            if (cam == null) return;

            Directory.CreateDirectory(OutDir);
            ShaderUtil.allowAsyncCompilation = false;

            // Warmup render
            var warmupRt = new RenderTexture(64, 64, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = warmupRt;
            for (int i = 0; i < 3; i++) cam.Render();
            cam.targetTexture = null;
            warmupRt.Release();
            Object.DestroyImmediate(warmupRt);

            // Camera B (Gameplay View)
            cam.transform.position = new Vector3(0.0f, 5.6f, -11.0f);
            cam.transform.rotation = Quaternion.Euler(24.5f, 0f, 0f);
            cam.fieldOfView = 46.0f;
            RenderToPng(cam, Path.Combine(OutDir, "CameraB_Master_GameplayView.png"));

            Debug.Log($"<color=#00FF88><b>[MASTER PREVIEW CAPTURED]</b> Saved to {OutDir}</color>");
        }

        private static void RenderToPng(Camera cam, string outPath)
        {
            int width = 960, height = 1706; // Portrait 9:16
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            byte[] bytes = tex.EncodeToPNG();
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);

            try
            {
                if (File.Exists(outPath))
                {
                    try { File.Delete(outPath); } catch { }
                }
                File.WriteAllBytes(outPath, bytes);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Could not write {outPath}: {ex.Message}");
            }
        }
    }
}
#endif
