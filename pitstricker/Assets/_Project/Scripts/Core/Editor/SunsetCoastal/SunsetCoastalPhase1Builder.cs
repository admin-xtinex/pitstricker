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
    /// <summary>
    /// Master orchestrator for Phase 1 — Core Gameplay Blockout:
    /// Clean, fresh rebuild of Map 02 Sunset Coastal matching the Phase 1 reference diagram:
    /// - Playable 3D lane: clean golden sand fairway, 3 pits with wooden lips (NO village numbers/arrows)
    /// - 3D Boundary system: dual-stacked bamboo logs with rope bindings and vertical posts
    /// - Environment layout: beach shack, surfboards, torches & palms on left; beach shoreline, boat, signpost, ocean & lighthouse on right
    /// - 4 stylized kid characters positioned around the lane
    /// - Camera B (Gameplay View) & Camera A (Cinematic View)
    /// Completely removes any leftover Village assets/numbers/arrows/mascots.
    /// </summary>
    public static class SunsetCoastalPhase1Builder
    {
        public const string Map02ScenePath = "Assets/_Project/Scenes/SC_SunsetCoastal_Map02.unity";
        private const string OutDir = "Logs/SunsetCoastalPreview";

        [MenuItem("Pit Striker/Sunset Coastal/★ BUILD PHASE 1 (FRESH REBUILD) ★", false, 0)]
        public static void BuildPhase1()
        {
            var scene = EditorSceneManager.OpenScene(Map02ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"Could not open {Map02ScenePath}");
                return;
            }

            Undo.SetCurrentGroupName("Build Phase 1 Sunset Coastal");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Purge all legacy village assets, master art, and camera-attached quads
            PurgeAllLegacyAndVillageElements();

            // 2. Build 3D Playable Fairway (Ground subfloor, packed sand fairway, 3 round saucer pits with triggers & PitZones, 4 player marbles)
            BuildPlayableFairway(scene);

            // 3. Art root
            var artRoot = new GameObject("SunsetCoastal_Art");

            // 4. World-space Boundary System (Physics colliders, fence cards, tropical hedges, tiki torches)
            SunsetCoastalBambooBoundary.BuildBoundarySystem(artRoot.transform);

            // 5. World-space Scenery (Beach shack, deck, surfboards, barrels, overhead palms, signpost, fishing boat, ocean, lighthouse & sun, sky backdrop at Z=52m)
            SunsetCoastalShackAndScenery.BuildScenery(artRoot.transform);

            // 6. Lighting & Atmosphere
            ConfigureLighting(artRoot);

            // 7. Camera (Clean Main Camera, configure SmoothFollowCamera targeting PlayerMarble_1_Blue)
            ConfigureCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#00FF88><b>[SUNSET COASTAL SUCCESS]</b> Successfully aligned gameplay with coastal environment!</color>");

            // 8. Capture preview screenshots
            CapturePreviewScreenshots();
        }

        private static void PurgeAllLegacyAndVillageElements()
        {
            // Destroy all Village-specific decorative dressing, numbers, floor markers, arrows, old walls, and master quads
            string[] exactNamesToDestroy =
            {
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
                "Playable_Boundary_Colliders",
                "Aiming_Trajectory",
                "AimingTrajectory_DottedArrow",
                "Village_Reference_Upgrade",
                "Gameplay_Kit_Village",
                "Environment_Village_Dressing",
                "Environment_Blender_Village_Graphics",
                "StylizedFlag_Pit1",
                "StylizedFlag_Pit2",
                "StylizedFlag_Pit3",
                "Arrow_1_2",
                "Arrow_2_3",
                "Mascot_Pip",
                "Pip_Rig",
                "Wall_Front",
                "Wall_Left",
                "Wall_Back",
                "Wall_Right",
                "Character_Player_HoodieBoy",
                "Character_Spectator_ShackKid",
                "Character_Spectator_SignBoy",
                "Character_Spectator_ShoreGirl"
            };

            foreach (var name in exactNamesToDestroy)
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }

            // Remove any camera-attached quads or backdrops (prevent sticking to lens during gameplay)
            var cam = GameObject.Find("Main Camera");
            if (cam != null)
            {
                for (int i = cam.transform.childCount - 1; i >= 0; i--)
                {
                    Object.DestroyImmediate(cam.transform.GetChild(i).gameObject);
                }
            }

            // Remove any child objects named InPit_NumberText, InPit_Floor_Marker, or any avatars/mascots/characters across all objects
            var scene = EditorSceneManager.GetActiveScene();
            foreach (var t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t == null) continue;
                var go = t.gameObject;
                if (go.scene != scene) continue;

                string n = go.name;
                if (n == "InPit_NumberText" || n == "InPit_Floor_Marker" || n == "BrightRed_PitLip" ||
                    n == "Master_Backdrop_Quad" ||
                    n.Contains("Pip") || n.Contains("Character") || n.Contains("Avatar") || n.Contains("Mascot") ||
                    n.StartsWith("Kid_") || n.StartsWith("Spectator_"))
                {
                    Object.DestroyImmediate(go);
                }
                else if (PrefabUtility.IsPartOfPrefabInstance(go) &&
                         PrefabUtility.GetPrefabInstanceStatus(go) == PrefabInstanceStatus.MissingAsset)
                {
                    var root = PrefabUtility.GetOutermostPrefabInstanceRoot(go) ?? go;
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void BuildPlayableFairway(UnityEngine.SceneManagement.Scene scene)
        {
            var arena = GameObject.Find("Arena_Sandbox");
            if (arena == null) arena = new GameObject("Arena_Sandbox");

            Material sandPacked = SunsetCoastalMaterialBuilder.GetSurfaceMaterial("Sand_Packed", 6f);
            sandPacked.SetColor("_BaseColor", new Color(1.06f, 0.94f, 0.78f)); // Warm golden sunset sand
            sandPacked.SetFloat("_BumpScale", 1.0f);
            sandPacked.SetFloat("_Smoothness", 0.18f);

            PhysicsMaterial sandPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Sand_Friction.physicMaterial");

            // 1. Safety Subfloor (underneath entire arena so marbles never fall out of world)
            var oldSubfloor = arena.transform.Find("Ground_Safety_Subfloor");
            if (oldSubfloor != null) Object.DestroyImmediate(oldSubfloor.gameObject);

            var subfloorGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            subfloorGo.name = "Ground_Safety_Subfloor";
            subfloorGo.transform.SetParent(arena.transform, false);
            subfloorGo.transform.position = new Vector3(0f, -1.2f, 13.5f);
            subfloorGo.transform.localScale = new Vector3(26f, 0.5f, 54f);
            subfloorGo.GetComponent<MeshRenderer>().sharedMaterial = sandPacked;
            var subCol = subfloorGo.GetComponent<BoxCollider>();
            if (subCol != null) subCol.material = sandPhys;

            // 2. Center Track Slab: continuous seamless packed sand fairway (Z: -8.5m to Z: 35.5m)
            var oldFairway = arena.transform.Find("Ground_Center_Fairway");
            if (oldFairway != null) Object.DestroyImmediate(oldFairway.gameObject);

            var fairwayGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fairwayGo.name = "Ground_Center_Fairway";
            fairwayGo.transform.SetParent(arena.transform, false);
            fairwayGo.transform.position = new Vector3(0f, -0.05f, 13.5f);
            fairwayGo.transform.localScale = new Vector3(4.6f, 0.10f, 44.0f);
            fairwayGo.GetComponent<MeshRenderer>().sharedMaterial = sandPacked;
            fairwayGo.GetComponent<MeshRenderer>().enabled = true;
            var boxCol = fairwayGo.GetComponent<BoxCollider>();
            if (boxCol != null) boxCol.material = sandPhys;

            // 3. Pits at Z = 3.0m, 16.5m, 31.0m matching TurnManager calibration
            float[] pitZs = { 3.0f, 16.5f, 31.0f };

            const string pitTexPath = "Assets/_Project/Art/Environments/SunsetCoastal/Generated/Pit_Dark_Hole.png";
            var pitTex = AssetDatabase.LoadAssetAtPath<Texture2D>(pitTexPath);
            Material pitMat = null;
            if (pitTex != null)
            {
                pitMat = SunsetCoastalMaterialBuilder.GetTransparentCardMaterial("Pit_Dark_Hole", pitTex);
            }

            Material pitWellMat = GetPitWellMaterial();

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

                // Clean old visual children
                for (int c = pit.childCount - 1; c >= 0; c--)
                {
                    Object.DestroyImmediate(pit.GetChild(c).gameObject);
                }

                // Earthen saucer dish mesh
                var mf = pit.GetComponent<MeshFilter>();
                if (mf == null) mf = pit.gameObject.AddComponent<MeshFilter>();
                mf.sharedMesh = CreateSaucerMesh(pitName, 0.52f, 0.085f);

                var mr = pit.GetComponent<MeshRenderer>();
                if (mr == null) mr = pit.gameObject.AddComponent<MeshRenderer>();
                mr.enabled = true;
                mr.sharedMaterial = pitWellMat;

                // Physical collider for saucer basin
                var mc = pit.GetComponent<MeshCollider>();
                if (mc == null) mc = pit.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                if (sandPhys != null) mc.sharedMaterial = sandPhys;

                // Trigger SphereCollider strictly inside the saucer cup for sinking
                var trigger = pit.GetComponent<SphereCollider>();
                if (trigger == null) trigger = pit.gameObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = 0.55f;
                trigger.center = new Vector3(0f, -0.04f, 0f);

                // PitZone scoring component
                var zone = pit.GetComponent<PitZone>();
                if (zone == null) zone = pit.gameObject.AddComponent<PitZone>();
                zone.SetPitNumber(i + 1);
                var zoneSo = new SerializedObject(zone);
                var numProp = zoneSo.FindProperty("_pitNumber");
                if (numProp != null) numProp.intValue = i + 1;
                zoneSo.ApplyModifiedProperties();

                // Place photorealistic Pit_Dark_Hole decal on top of the sand
                if (pitTex != null && pitMat != null)
                {
                    SunsetCoastalBillboardUtil.SpawnGroundDecal($"Pit_Visual_{i + 1}", pit, pitTex, pitMat,
                        new Vector3(0f, 0.006f, 0f), 1.62f, 0f);
                }
            }

            // 4. Player Marbles setup
            var p1Marble = GameObject.Find("PlayerMarble_1_Blue");
            if (p1Marble != null)
            {
                p1Marble.transform.position = new Vector3(0f, 0.25f, -6.0f);
                var rb = p1Marble.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            // Waiting sideline marbles in the sideline dugout
            var p2 = GameObject.Find("PlayerMarble_2_Red");
            if (p2 != null) p2.transform.position = new Vector3(-2.2f, 0.25f, -4.6f);
            var p3 = GameObject.Find("PlayerMarble_3_Green");
            if (p3 != null) p3.transform.position = new Vector3(-1.7f, 0.25f, -4.3f);
            var p4 = GameObject.Find("PlayerMarble_4_Amber");
            if (p4 != null) p4.transform.position = new Vector3(-1.2f, 0.25f, -4.0f);

            // 5. Outer boundary containment walls (Left, Right, Back, Front)
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");
            CreateBoundaryWall("Wall_Left", arena.transform, new Vector3(-8.5f, 0.35f, 14.5f), new Vector3(0.5f, 0.7f, 48.0f), bouncePhys);
            CreateBoundaryWall("Wall_Right", arena.transform, new Vector3(8.5f, 0.35f, 14.5f), new Vector3(0.5f, 0.7f, 48.0f), bouncePhys);
            CreateBoundaryWall("Wall_Back", arena.transform, new Vector3(0f, 0.35f, -9.1f), new Vector3(17.5f, 0.7f, 0.5f), bouncePhys);
            CreateBoundaryWall("Wall_Front", arena.transform, new Vector3(0f, 0.35f, 38.1f), new Vector3(17.5f, 0.7f, 0.5f), bouncePhys);
        }

        private static void CreateBoundaryWall(string name, Transform parent, Vector3 pos, Vector3 scale, PhysicsMaterial physMat)
        {
            var old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent, false);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            var mr = wall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
            var col = wall.GetComponent<BoxCollider>();
            if (col != null && physMat != null) col.sharedMaterial = physMat;
        }

        private static Mesh CreateSaucerMesh(string name, float rimRadius, float maxDepth)
        {
            var mesh = new Mesh { name = name + "_Saucer" };
            int rings = 5;
            int segments = 24;

            var vertices = new System.Collections.Generic.List<Vector3>();
            var normals = new System.Collections.Generic.List<Vector3>();
            var uvs = new System.Collections.Generic.List<Vector2>();
            var triangles = new System.Collections.Generic.List<int>();

            int centerIdx = vertices.Count;
            vertices.Add(new Vector3(0f, -maxDepth, 0f));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int[][] ringIndices = new int[rings][];
            for (int r = 1; r <= rings; r++)
            {
                ringIndices[r - 1] = new int[segments];
                float frac = (float)r / rings;
                float curRadius = frac * rimRadius;
                float curY = -maxDepth * Mathf.Pow(Mathf.Cos(frac * Mathf.PI * 0.5f), 2f);

                for (int s = 0; s < segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / segments;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);

                    ringIndices[r - 1][s] = vertices.Count;
                    vertices.Add(new Vector3(curRadius * cos, curY, curRadius * sin));
                    normals.Add(Vector3.up);
                    uvs.Add(new Vector2(cos * frac * 0.5f + 0.5f, sin * frac * 0.5f + 0.5f));
                }
            }

            for (int s = 0; s < segments; s++)
            {
                int next = (s + 1) % segments;
                triangles.Add(centerIdx);
                triangles.Add(ringIndices[0][next]);
                triangles.Add(ringIndices[0][s]);
            }

            for (int r = 0; r < rings - 1; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int next = (s + 1) % segments;
                    int innerCurr = ringIndices[r][s];
                    int innerNext = ringIndices[r][next];
                    int outerCurr = ringIndices[r + 1][s];
                    int outerNext = ringIndices[r + 1][next];

                    triangles.Add(innerCurr);
                    triangles.Add(outerNext);
                    triangles.Add(outerCurr);

                    triangles.Add(innerCurr);
                    triangles.Add(innerNext);
                    triangles.Add(outerNext);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }


        private static Material GetPitWellMaterial()
        {
            const string path = "Assets/_Project/Art/Environments/SunsetCoastal/Materials/M_SC_PitWell.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader) { name = "M_SC_PitWell" };
                AssetDatabase.CreateAsset(mat, path);
            }
            // Deep, dark shadow well color
            mat.SetColor("_BaseColor", new Color(0.08f, 0.05f, 0.03f));
            mat.SetFloat("_Smoothness", 0.08f);
            mat.SetFloat("_Metallic", 0.0f);
            return mat;
        }

        private static void ConfigureLighting(GameObject artRoot)
        {
            // Golden Sunset Sun Directional Light
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo != null)
            {
                var light = lightGo.GetComponent<Light>();
                // Sun angled from upper-left casting diagonal palm shadows across fairway towards lower-right
                light.transform.rotation = Quaternion.Euler(22f, 122f, 0f);
                light.color = new Color(1.0f, 0.78f, 0.52f); // Warm sunset golden amber
                light.intensity = 2.1f;
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.85f;
                light.shadowBias = 0.04f;
                light.shadowNormalBias = 0.2f;
            }

            // Warm Sunset Ambient Trilight
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.92f, 0.60f, 0.48f); // Warm peach/amber sky
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.52f, 0.38f); // Golden dusk horizon
            RenderSettings.ambientGroundColor = new Color(0.48f, 0.32f, 0.22f); // Warm sand bounce
            RenderSettings.ambientIntensity = 1.15f;

            // Soft sunset haze fog
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.96f, 0.72f, 0.52f);
            RenderSettings.fogDensity = 0.0028f;

            // Configure Post-Processing Volume
            SunsetCoastalLighting.Configure(artRoot);

            // Camera background sunset sky
            var camGo = GameObject.Find("Main Camera");
            if (camGo != null)
            {
                var cam = camGo.GetComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.94f, 0.65f, 0.45f);
            }
        }

        private static void ConfigureCamera()
        {
            var camGo = GameObject.Find("Main Camera");
            if (camGo == null) return;

            // Remove any camera-attached quads or backdrops (prevent sticking to lens during gameplay)
            for (int i = camGo.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(camGo.transform.GetChild(i).gameObject);
            }

            var cam = camGo.GetComponent<Camera>();
            cam.fieldOfView = 46.0f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 120.0f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.68f, 0.48f);

            // Camera B (Gameplay View) matching the reference perspective
            camGo.transform.position = new Vector3(0.0f, 5.6f, -11.0f);
            camGo.transform.rotation = Quaternion.Euler(24.5f, 0f, 0f);

            var follow = camGo.GetComponent<SmoothFollowCamera>();
            if (follow != null)
            {
                var p1 = GameObject.Find("PlayerMarble_1_Blue");
                if (p1 != null)
                {
                    follow.SetTarget(p1.transform);
                    var so = new SerializedObject(follow);
                    var targetProp = so.FindProperty("_target");
                    if (targetProp != null) targetProp.objectReferenceValue = p1.transform;
                    so.ApplyModifiedProperties();
                }
                follow.SetDistance(5.2f);
                follow.SetHeight(2.8f);
                follow.SetLookAtHeightOffset(0.45f);
                follow.enabled = true;
            }
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

            // 1. Camera B (Gameplay View - Exact match to Phase 4 / Final reference)
            cam.transform.position = new Vector3(0.0f, 5.6f, -11.0f);
            cam.transform.rotation = Quaternion.Euler(24.5f, 0f, 0f);
            cam.fieldOfView = 46.0f;
            RenderToPng(cam, Path.Combine(OutDir, "CameraB_Phase1_GameplayView.png"));

            // 2. Camera A (Cinematic / Offset View)
            cam.transform.position = new Vector3(-2.8f, 4.2f, -9.8f);
            cam.transform.rotation = Quaternion.Euler(20.0f, 22.0f, 0f);
            cam.fieldOfView = 48.0f;
            RenderToPng(cam, Path.Combine(OutDir, "CameraA_Phase1_CinematicView.png"));

            // Restore Camera B as default
            cam.transform.position = new Vector3(0.0f, 5.6f, -11.0f);
            cam.transform.rotation = Quaternion.Euler(24.5f, 0f, 0f);
            cam.fieldOfView = 46.0f;

            Debug.Log($"<color=#00FF88><b>[PREVIEW CAPTURED]</b> Screenshots saved to {OutDir}</color>");
        }

        private static void RenderToPng(Camera cam, string outPath)
        {
            int width = 960, height = 1706; // Portrait 9:16 mobile framing
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
                Debug.LogWarning($"[SCREENSHOT] Could not write {outPath}: {ex.Message}. Writing to backup path.");
                string backup = Path.Combine(Path.GetDirectoryName(outPath), Path.GetFileNameWithoutExtension(outPath) + "_new.png");
                try { File.WriteAllBytes(backup, bytes); } catch { }
            }
        }
    }
}
#endif
