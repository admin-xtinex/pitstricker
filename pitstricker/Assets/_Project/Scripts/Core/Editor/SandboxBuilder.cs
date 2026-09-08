#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.Physics;
using PitStriker.Input;
using PitStriker.CameraSystem;
using PitStriker.Gameplay;
using PitStriker.UI;
using PitStriker.Audio;
using PitStriker.VFX;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Studio Editor Automation:
    /// One-click generation of the complete Pit Striker Physics Arena
    /// with Round Pits, Chalk Launch Reticle, 4-Player Pass-and-Play Marbles,
    /// Procedural Audio/VFX Juice, and Full Concept HUD (Power Meter, Stage Beads, Player Badges, Podium Modal).
    /// </summary>
    public static class SandboxBuilder
    {
        [MenuItem("Pit Striker/Generate Physics Arena", false, 10)]
        public static void GeneratePhysicsArena()
        {
            Undo.SetCurrentGroupName("Generate Pit Striker Arena");
            int group = Undo.GetCurrentGroup();

            AssetDatabase.Refresh();

            // 0. Setup Village Dirt Ground & User Marble Materials
            Material sandMat = GetOrCreateVillageDirtMaterial();
            Material woodMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Wood_Rustic.mat", new Color(0.28f, 0.16f, 0.08f, 1f), 0.20f);
            Material chalkMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Chalk_White.mat", new Color(0.96f, 0.96f, 0.94f, 1f), 0.08f);
            Material flagRedMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Flag_Red.mat", new Color(0.88f, 0.05f, 0.05f, 1f), 0.35f);
            Material signTextMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Sign_White.mat", new Color(0.98f, 0.98f, 0.96f, 1f), 0.10f);
            Material pebbleMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Stone_Pebble.mat", new Color(0.48f, 0.44f, 0.38f, 1f), 0.15f);
            Material pitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Pit_Dark.mat");
            if (pitMat == null) pitMat = GetOrCreateMaterial("Assets/_Project/Art/Materials/M_Pit_Dark.mat", new Color(0.18f, 0.12f, 0.08f, 1f), 0.1f);
            Material lineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Trajectory_Cyan.mat");

            // User-created custom glass marble colors (P1..P4)
            Material[] marbleMats = new Material[]
            {
                GetOrCreateMarbleMaterial("Assets/_Project/Art/Materials/M_Marble_Blue.mat", new Color(0.025f, 0.18f, 0.95f, 1.0f)),
                GetOrCreateMarbleMaterial("Assets/_Project/Art/Materials/M_Marble_Red.mat", new Color(0.80f, 0.025f, 0.02f, 1.0f)),
                GetOrCreateMarbleMaterial("Assets/_Project/Art/Materials/M_Marble_Green.mat", new Color(0.02f, 0.62f, 0.09f, 1.0f)),
                GetOrCreateMarbleMaterial("Assets/_Project/Art/Materials/M_Marble_Amber.mat", new Color(0.95f, 0.58f, 0.02f, 1.0f))
            };

            PhysicsMaterial sandPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Sand_Friction.physicMaterial");
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");

            // 1. Clean up old objects
            GameObject oldGround = GameObject.Find("Ground");
            if (oldGround != null) Undo.DestroyObjectImmediate(oldGround);

            GameObject oldTestMarble = GameObject.Find("TestMarble");
            if (oldTestMarble != null) Undo.DestroyObjectImmediate(oldTestMarble);

            GameObject oldArena = GameObject.Find("Arena_Sandbox");
            if (oldArena != null) Undo.DestroyObjectImmediate(oldArena);

            GameObject oldVillage = GameObject.Find("VillageMap_Visuals");
            if (oldVillage != null) Undo.DestroyObjectImmediate(oldVillage);

            // Clean up any existing marble controllers in the scene
            foreach (var m in Object.FindObjectsByType<MarbleController>(FindObjectsInactive.Exclude))
            {
                Undo.DestroyObjectImmediate(m.gameObject);
            }

            GameObject oldHUD = GameObject.Find("HUD_Canvas");
            if (oldHUD != null) Undo.DestroyObjectImmediate(oldHUD);

            // 2. Root Arena Container
            GameObject arenaRoot = new GameObject("Arena_Sandbox");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // 2.1 Village Gameplay Kit (Chalk Rings, Red Numbered Flags, Trajectory Arrows, PIT STRIKER Wooden Sign, Pebbles)
            GameObject villageKitFbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/VillageKit/Village_Gameplay_Kit.fbx");
            if (villageKitFbx != null)
            {
                GameObject kitObj = (GameObject)PrefabUtility.InstantiatePrefab(villageKitFbx, arenaRoot.transform);
                kitObj.name = "Gameplay_Kit_Village";
                kitObj.transform.localPosition = Vector3.zero;
                kitObj.transform.localRotation = Quaternion.identity;
                kitObj.transform.localScale = Vector3.one;

                // Strip any colliders from kit props so marbles roll uninhibited
                foreach (var col in kitObj.GetComponentsInChildren<Collider>(true))
                {
                    Undo.DestroyObjectImmediate(col);
                }

                // Apply dedicated materials by child name
                foreach (var mr in kitObj.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string oName = mr.gameObject.name;
                    if (oName.Contains("Chalk") || oName.Contains("Arrow"))
                    {
                        mr.sharedMaterial = chalkMat;
                    }
                    else if (oName.Contains("Flag"))
                    {
                        // Flag mesh has 3 submeshes: pole (wood), cloth (red), number (white)
                        mr.sharedMaterials = new Material[] { woodMat, flagRedMat, signTextMat };
                    }
                    else if (oName.Contains("Signboard"))
                    {
                        // Signboard has 2 submeshes: wood board & posts, white text
                        mr.sharedMaterials = new Material[] { woodMat, signTextMat };
                    }
                    else if (oName.Contains("Pebble"))
                    {
                        mr.sharedMaterial = pebbleMat;
                    }
                }
            }

            // 2.2 Village Architecture Dressing (House, Stone Walls, Benches/Barricades, Outbuildings - Strictly WITHOUT Plants)
            GameObject dressingRoot = new GameObject("Environment_Village_Dressing");
            dressingRoot.transform.SetParent(arenaRoot.transform);
            dressingRoot.transform.localPosition = Vector3.zero;

            // Village House on left flank
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Home1.fbx", dressingRoot.transform, new Vector3(-11.5f, 0f, 7.5f), new Vector3(0f, 28f, 0f), Vector3.one * 1.35f, "VIS_VillageHome");

            // Stone walls / rock fences framing the track
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Stone.fbx", dressingRoot.transform, new Vector3(-5.6f, 0f, -3.2f), new Vector3(0f, 45f, 0f), Vector3.one * 1.3f, "VIS_Stone_Dugout");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Stone.fbx", dressingRoot.transform, new Vector3(6.5f, 0f, 2.0f), new Vector3(0f, -15f, 0f), Vector3.one * 1.4f, "VIS_Stone_Right_Front");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Stone.fbx", dressingRoot.transform, new Vector3(-6.8f, 0f, 18.0f), new Vector3(0f, 35f, 0f), Vector3.one * 1.4f, "VIS_Stone_Left_Mid");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Stone.fbx", dressingRoot.transform, new Vector3(7.0f, 0f, 24.0f), new Vector3(0f, -40f, 0f), Vector3.one * 1.5f, "VIS_Stone_Right_Mid");

            // Rustic wooden benches / barricades
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Barr.fbx", dressingRoot.transform, new Vector3(-4.4f, 0f, -5.4f), new Vector3(0f, 15f, 0f), Vector3.one * 1.0f, "VIS_Bench_Dugout");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Barr.fbx", dressingRoot.transform, new Vector3(6.8f, 0f, 12.0f), new Vector3(0f, -10f, 0f), Vector3.one * 1.0f, "VIS_Barricade_Right");

            // Village outbuilding / stalls in background
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/Base.fbx", dressingRoot.transform, new Vector3(9.8f, 0f, 32.0f), new Vector3(0f, -25f, 0f), Vector3.one * 0.9f, "VIS_VillageBase_Back");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/S1.fbx", dressingRoot.transform, new Vector3(-10.5f, 0f, 25.0f), new Vector3(0f, 90f, 0f), Vector3.one * 1.1f, "VIS_VillageStall_Left");
            PlaceVillageProp("Assets/ThirdParty/CrashBash/Fbx/s2.fbx", dressingRoot.transform, new Vector3(9.2f, 0f, 18.0f), new Vector3(0f, -90f, 0f), Vector3.one * 1.0f, "VIS_VillageStall_Right");

            // Master safety subfloor underneath the entire arena (48-meter fairway, 2x width 22m)
            CreateGroundSlab("Ground_Safety_Subfloor", arenaRoot.transform, new Vector3(0f, -1.2f, 15f), new Vector3(22f, 0.5f, 52f), sandMat, sandPhys);

            // Left & Right Bank Slabs (2x wide, covering x: -8.2m to +8.2m)
            CreateGroundSlab("Ground_Bank_Left", arenaRoot.transform, new Vector3(-5.2f, -0.25f, 15f), new Vector3(7.6f, 0.5f, 48f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Bank_Right", arenaRoot.transform, new Vector3(5.2f, -0.25f, 15f), new Vector3(7.6f, 0.5f, 48f), sandMat, sandPhys);

            // Center Track Slabs connecting seamlessly with round pit tiles (Z: -8.5m to Z: 38.0m)
            CreateGroundSlab("Ground_Center_Start", arenaRoot.transform, new Vector3(0f, -0.25f, -3.4f), new Vector3(2.8f, 0.5f, 10.2f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_1_2", arenaRoot.transform, new Vector3(0f, -0.25f, 9.75f), new Vector3(2.8f, 0.5f, 10.9f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_2_3", arenaRoot.transform, new Vector3(0f, -0.25f, 23.75f), new Vector3(2.8f, 0.5f, 11.9f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_End", arenaRoot.transform, new Vector3(0f, -0.25f, 35.15f), new Vector3(2.8f, 0.5f, 5.7f), sandMat, sandPhys);

            // 3. Boundary Rails (Left, Right, Back, Front) - 2x wide layout (bounds: x = -8.2m to +8.2m)
            // Invisible physics barriers ensure marbles never clip out while scenic village environment remains fully visible
            CreateBoundaryWall("Wall_Left", arenaRoot.transform, new Vector3(-8.2f, 0.35f, 14.5f), new Vector3(0.5f, 0.7f, 47.5f), woodMat, bouncePhys, true);
            CreateBoundaryWall("Wall_Right", arenaRoot.transform, new Vector3(8.2f, 0.35f, 14.5f), new Vector3(0.5f, 0.7f, 47.5f), woodMat, bouncePhys, true);
            CreateBoundaryWall("Wall_Back", arenaRoot.transform, new Vector3(0f, 0.35f, -9.1f), new Vector3(16.8f, 0.7f, 0.5f), woodMat, bouncePhys, true);
            CreateBoundaryWall("Wall_Front", arenaRoot.transform, new Vector3(0f, 0.35f, 38.1f), new Vector3(16.8f, 0.7f, 0.5f), woodMat, bouncePhys, true);

            // 4. Create 3 TRUE ROUND PITS with 2x marble size (Diameter = 1.0m, R = 0.50m)
            CreateRoundPitTile("Pit_01_Round", arenaRoot.transform, new Vector3(0f, 0f, 3.0f), 1, sandMat, pitMat, woodMat, sandPhys);
            CreateRoundPitTile("Pit_02_Round", arenaRoot.transform, new Vector3(0f, 0f, 16.5f), 2, sandMat, pitMat, woodMat, sandPhys);
            CreateRoundPitTile("Pit_03_Round", arenaRoot.transform, new Vector3(0f, 0f, 31.0f), 3, sandMat, pitMat, woodMat, sandPhys);

            // 5. Create 4 Player Striker Marbles (P1..P4) using User-Created Custom Models
            string[] marbleNames = new string[] { "PlayerMarble_1_Blue", "PlayerMarble_2_Red", "PlayerMarble_3_Green", "PlayerMarble_4_Amber" };
            string[] customFbxPaths = new string[]
            {
                "Assets/_Project/Art/Models/Marbles/Marble_P1.fbx",
                "Assets/_Project/Art/Models/Marbles/Marble_P2.fbx",
                "Assets/_Project/Art/Models/Marbles/Marble_P3.fbx",
                "Assets/_Project/Art/Models/Marbles/Marble_P4.fbx"
            };
            MarbleController p1Marble = null;

            for (int i = 0; i < 4; i++)
            {
                GameObject customFbx = AssetDatabase.LoadAssetAtPath<GameObject>(customFbxPaths[i]);
                GameObject marbleObj = null;

                if (customFbx != null)
                {
                    marbleObj = (GameObject)PrefabUtility.InstantiatePrefab(customFbx);
                    PrefabUtility.UnpackPrefabInstance(marbleObj, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    marbleObj.name = marbleNames[i];
                    marbleObj.transform.localScale = Vector3.one;
                }
                else
                {
                    marbleObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    marbleObj.name = marbleNames[i];
                    marbleObj.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }

                marbleObj.transform.SetParent(arenaRoot.transform);
                // Position active marble in chalk launch circle, and waiting marbles in the sideline dugout by the wooden sign
                if (i == 0)
                {
                    marbleObj.transform.position = new Vector3(0f, 0.25f, -6.0f);
                }
                else
                {
                    // Sideline waiting area beside the PIT STRIKER wooden sign and rustic bench (Concept_Village_Path.png)
                    marbleObj.transform.position = new Vector3(-2.2f - (i - 1) * 0.55f, 0.25f, -4.5f - (i - 1) * 0.35f);
                }
                Undo.RegisterCreatedObjectUndo(marbleObj, "Create Marble " + marbleNames[i]);

                MeshRenderer mr = marbleObj.GetComponentInChildren<MeshRenderer>();
                if (mr == null) mr = marbleObj.AddComponent<MeshRenderer>();
                if (marbleMats[i] != null) mr.sharedMaterial = marbleMats[i];

                Rigidbody rb = marbleObj.GetComponent<Rigidbody>();
                if (rb == null) rb = marbleObj.AddComponent<Rigidbody>();
                rb.mass = 1.0f;
                rb.linearDamping = 0.3f;
                rb.angularDamping = 0.8f;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.interpolation = RigidbodyInterpolation.Interpolate;

                SphereCollider sc = marbleObj.GetComponent<SphereCollider>();
                if (sc == null) sc = marbleObj.AddComponent<SphereCollider>();
                sc.center = Vector3.zero;
                sc.radius = 0.25f; // User's marble geometry radius is 0.25m (0.50m diameter)
                if (bouncePhys != null) sc.sharedMaterial = bouncePhys;

                MarbleController mc = marbleObj.GetComponent<MarbleController>();
                if (mc == null) mc = marbleObj.AddComponent<MarbleController>();

                if (i == 0)
                {
                    p1Marble = mc;

                    // Trajectory Line
                    LineRenderer line = marbleObj.GetComponent<LineRenderer>();
                    if (line == null) line = marbleObj.AddComponent<LineRenderer>();
                    line.startWidth = 0.08f;
                    line.endWidth = 0.16f;
                    line.startColor = new Color(0f, 0.85f, 1f, 0.95f);
                    line.endColor = new Color(0f, 0.9f, 1f, 0.25f);
                    line.useWorldSpace = true;
                    if (lineMat != null) line.sharedMaterial = lineMat;
                    line.enabled = false;

                    if (marbleObj.GetComponent<SwipeLaunchController>() == null)
                    {
                        marbleObj.AddComponent<SwipeLaunchController>();
                    }
                }
                else
                {
                    // Marbles P2..P4 sit ready on the sideline waiting area (Concept_Village_Path.png)
                    mc.SetVisible(true);
                }
            }

            // 6. Main Camera Setup (Low-angle perspective matching Concept_Village_Path.png)
            Camera cam = Camera.main;
            if (cam != null && p1Marble != null)
            {
                cam.transform.position = new Vector3(0f, 1.45f, -9.6f);
                cam.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

                SmoothFollowCamera follow = cam.GetComponent<SmoothFollowCamera>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<SmoothFollowCamera>();
                }
                follow.SetTarget(p1Marble.transform);

                SerializedObject camSo = new SerializedObject(follow);
                var distProp = camSo.FindProperty("_distance");
                if (distProp != null) distProp.floatValue = 3.6f;
                var heightProp = camSo.FindProperty("_height");
                if (heightProp != null) heightProp.floatValue = 1.45f;
                var lookProp = camSo.FindProperty("_lookAtHeightOffset");
                if (lookProp != null) lookProp.floatValue = 0.45f;
                camSo.ApplyModifiedProperties();
            }

            // Directional Sun Light (Warm Golden Hour matching concept art)
            Light sun = Object.FindAnyObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
            {
                sun.transform.rotation = Quaternion.Euler(36f, 35f, 0f);
                sun.color = new Color(1f, 0.95f, 0.88f, 1f);
                sun.intensity = 1.35f;
            }

            // 8. Turn & Match Orchestrator (Phase 5 Multiplayer)
            TurnManager tm = Object.FindAnyObjectByType<TurnManager>();
            if (tm == null)
            {
                GameObject tmObj = new GameObject("TurnManager");
                tm = tmObj.AddComponent<TurnManager>();
            }
            SerializedObject tmSo = new SerializedObject(tm);
            tmSo.FindProperty("_playerCount").intValue = 2;
            tmSo.ApplyModifiedProperties();

            // 9. Audio & Visual Juice Managers (Phase 7)
            AudioManager audioMgr = Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.AddComponent<AudioManager>();
            }

            VFXManager vfxMgr = Object.FindAnyObjectByType<VFXManager>();
            if (vfxMgr == null)
            {
                GameObject vfxObj = new GameObject("VFXManager");
                vfxObj.AddComponent<VFXManager>();
            }

            // 10. Create Concept HUD Canvas (Power Meter + Stage Beads + Scoreboard + Player Badges + Victory Modal)
            CreateHUDCanvas();

            Undo.CollapseUndoOperations(group);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> 4-Player Arena rebuilt with Pass-and-Play, Audio/VFX Juice, Badges, and Podium Modal!</color>");
        }

        private static void CreateGroundSlab(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, PhysicsMaterial physMat)
        {
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = name;
            slab.transform.SetParent(parent);
            slab.transform.position = position;
            slab.transform.localScale = scale;

            if (mat != null) slab.GetComponent<MeshRenderer>().sharedMaterial = mat;
            BoxCollider col = slab.GetComponent<BoxCollider>();
            if (physMat != null) col.sharedMaterial = physMat;
        }

        private static void CreateBoundaryWall(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, PhysicsMaterial physMat, bool hideRenderer = false)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;

            MeshRenderer mr = wall.GetComponent<MeshRenderer>();
            if (mat != null && mr != null) mr.sharedMaterial = mat;
            if (hideRenderer && mr != null) mr.enabled = false;

            BoxCollider col = wall.GetComponent<BoxCollider>();
            if (physMat != null && col != null) col.sharedMaterial = physMat;
        }

        private static void CreateRoundPitTile(string name, Transform parent, Vector3 position, int pitNumber, Material sandMat, Material pitMat, Material woodMat, PhysicsMaterial physMat)
        {
            GameObject pitRoot = new GameObject(name);
            pitRoot.transform.SetParent(parent);
            pitRoot.transform.position = position;

            MeshFilter mf = pitRoot.AddComponent<MeshFilter>();
            MeshRenderer mr = pitRoot.AddComponent<MeshRenderer>();

            float width = 2.8f;
            float length = 2.6f;
            float rimRadius = 0.50f;     // Exactly 2x marble radius (Diameter = 1.0m, marble diameter = 0.50m)
            float floorRadius = 0.38f;   // Basin floor radius
            float depth = 0.28f;         // Steep vertical earthen pit depth
            int segments = 28;

            Mesh mesh = new Mesh();
            mesh.name = name + "_RoundMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> sandTriangles = new List<int>();
            List<int> pitTriangles = new List<int>();

            // 1. Top Sand Surface (Outer rectangle to inner rim circle)
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float tx = Mathf.Abs(cos) > 0.0001f ? (width * 0.5f) / Mathf.Abs(cos) : float.MaxValue;
                float tz = Mathf.Abs(sin) > 0.0001f ? (length * 0.5f) / Mathf.Abs(sin) : float.MaxValue;
                float t = Mathf.Min(tx, tz);

                Vector3 outerPt = new Vector3(t * cos, 0f, t * sin);
                Vector3 innerRimPt = new Vector3(rimRadius * cos, 0f, rimRadius * sin);

                vertices.Add(outerPt);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(outerPt.x, outerPt.z));

                vertices.Add(innerRimPt);
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(innerRimPt.x, innerRimPt.z));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int outerCurr = i * 2;
                int innerCurr = i * 2 + 1;
                int outerNext = next * 2;
                int innerNext = next * 2 + 1;

                sandTriangles.Add(outerCurr);
                sandTriangles.Add(innerCurr);
                sandTriangles.Add(innerNext);

                sandTriangles.Add(outerCurr);
                sandTriangles.Add(innerNext);
                sandTriangles.Add(outerNext);
            }

            // 2. Smooth Sloped Bowl Wall of the Pit Cup (connecting rim to basin floor)
            int wallStartIndex = vertices.Count;
            float deltaR = rimRadius - floorRadius;
            Vector2 slopeNormal2D = new Vector2(-depth, deltaR).normalized;

            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 topPt = new Vector3(rimRadius * cos, 0f, rimRadius * sin);
                Vector3 bottomPt = new Vector3(floorRadius * cos, -depth, floorRadius * sin);
                Vector3 slopedNormal = new Vector3(slopeNormal2D.x * cos, slopeNormal2D.y, slopeNormal2D.x * sin);

                vertices.Add(topPt);
                normals.Add(slopedNormal);
                uvs.Add(new Vector2((float)i / segments, 0f));

                vertices.Add(bottomPt);
                normals.Add(slopedNormal);
                uvs.Add(new Vector2((float)i / segments, 1f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int topCurr = wallStartIndex + i * 2;
                int botCurr = wallStartIndex + i * 2 + 1;
                int topNext = wallStartIndex + next * 2;
                int botNext = wallStartIndex + next * 2 + 1;

                pitTriangles.Add(topCurr);
                pitTriangles.Add(topNext);
                pitTriangles.Add(botNext);

                pitTriangles.Add(topCurr);
                pitTriangles.Add(botNext);
                pitTriangles.Add(botCurr);
            }

            // 3. Flat Circular Basin Floor at the bottom of the cup
            int floorCenterIndex = vertices.Count;
            vertices.Add(new Vector3(0f, -depth, 0f));
            normals.Add(Vector3.up);
            uvs.Add(new Vector2(0.5f, 0.5f));

            int floorRimStartIndex = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                vertices.Add(new Vector3(floorRadius * cos, -depth, floorRadius * sin));
                normals.Add(Vector3.up);
                uvs.Add(new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
            }

            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                pitTriangles.Add(floorCenterIndex);
                pitTriangles.Add(floorRimStartIndex + i);
                pitTriangles.Add(floorRimStartIndex + next);
            }

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);

            mesh.subMeshCount = 2;
            mesh.SetTriangles(sandTriangles, 0);
            mesh.SetTriangles(pitTriangles, 1);

            mf.sharedMesh = mesh;
            mr.sharedMaterials = new Material[] { sandMat, pitMat };

            // Physical Mesh Collider for 100% round physics
            MeshCollider mc = pitRoot.AddComponent<MeshCollider>();
            mc.sharedMesh = mesh;
            if (physMat != null) mc.sharedMaterial = physMat;

            // Trigger Zone strictly inside the bottom cup
            SphereCollider trigger = pitRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.55f;
            trigger.center = new Vector3(0f, -0.14f, 0f);

            PitZone zone = pitRoot.AddComponent<PitZone>();
            zone.SetPitNumber(pitNumber);
            SerializedObject zoneSo = new SerializedObject(zone);
            zoneSo.FindProperty("_pitNumber").intValue = pitNumber;
            zoneSo.ApplyModifiedProperties();

            // Red Numbered Flags (1, 2, 3) are provided cleanly by Gameplay_Kit_Village (SM_PitFlag_01..03)
            // CreateMarkerPole("MarkerPole_" + pitNumber, pitRoot.transform, new Vector3(rimRadius + 0.35f, 0f, 0f), pitNumber, woodMat);
        }

        private static void CreateMarkerPole(string name, Transform parent, Vector3 localPos, int number, Material woodMat)
        {
            GameObject poleObj = new GameObject(name);
            poleObj.transform.SetParent(parent);
            poleObj.transform.localPosition = localPos;

            // 1. Turned wooden milestone post
            GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "Milestone_Post";
            post.transform.SetParent(poleObj.transform);
            post.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            post.transform.localScale = new Vector3(0.08f, 0.35f, 0.08f);
            if (woodMat != null) post.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(post.GetComponent<Collider>());

            // 2. Brass/Bronze Milestone Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.name = "Milestone_Head";
            head.transform.SetParent(poleObj.transform);
            head.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            head.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            Material brassMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            brassMat.color = new Color(0.85f, 0.65f, 0.25f, 1f);
            brassMat.SetFloat("_Smoothness", 0.7f);
            head.GetComponent<MeshRenderer>().sharedMaterial = brassMat;
            Object.DestroyImmediate(head.GetComponent<Collider>());

            // 3. Milestone Number Badge Rings (1 ring for Pit 1, 2 for Pit 2, 3 for Pit 3)
            for (int r = 0; r < number; r++)
            {
                GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "Ring_" + (r + 1);
                ring.transform.SetParent(poleObj.transform);
                ring.transform.localPosition = new Vector3(0f, 0.45f + (r * 0.08f), 0f);
                ring.transform.localScale = new Vector3(0.10f, 0.02f, 0.10f);
                ring.GetComponent<MeshRenderer>().sharedMaterial = brassMat;
                Object.DestroyImmediate(ring.GetComponent<Collider>());
            }
        }

        private static void CreateChalkRing(string name, Transform parent, Vector3 position, float radius)
        {
            GameObject ring = new GameObject(name);
            ring.transform.SetParent(parent);
            ring.transform.position = position;

            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = true;
            line.startWidth = 0.04f;
            line.endWidth = 0.04f;

            int segments = 32;
            line.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }

            Material chalkMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            chalkMat.color = new Color(1f, 1f, 1f, 0.5f);
            line.sharedMaterial = chalkMat;
        }

        /// <summary>
        /// Generates the complete Concept Art HUD UI Canvas (Power Meter + Sequential Tracker + Strike Button).
        /// </summary>
        private static void CreateHUDCanvas()
        {
            // 1. Root Canvas
            GameObject canvasObj = new GameObject("HUD_Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObj.AddComponent<GraphicRaycaster>();

            HUDManager hud = canvasObj.AddComponent<HUDManager>();

            // Ensure EventSystem exists and uses InputSystemUIInputModule (Unity 6 Input System)
            UnityEngine.EventSystems.EventSystem es = Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            if (es == null)
            {
                GameObject esObj = new GameObject("EventSystem");
                es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            }

            // Remove legacy StandaloneInputModule if present
            UnityEngine.EventSystems.StandaloneInputModule standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (standalone != null)
            {
                Object.DestroyImmediate(standalone);
            }

            // Ensure InputSystemUIInputModule is present
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            // 2. Power Meter Panel (Bottom-Left)
            GameObject powerPanel = new GameObject("Power_Meter_Panel");
            powerPanel.transform.SetParent(canvasObj.transform, false);
            RectTransform powerRect = powerPanel.AddComponent<RectTransform>();
            powerRect.anchorMin = new Vector2(0f, 0f);
            powerRect.anchorMax = new Vector2(0f, 0f);
            powerRect.pivot = new Vector2(0f, 0f);
            powerRect.anchoredPosition = new Vector2(50f, 45f);
            powerRect.sizeDelta = new Vector2(360f, 65f);

            Image panelBg = powerPanel.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            panelBg.raycastTarget = false;

            // Power Slider
            GameObject sliderObj = new GameObject("Power_Slider");
            sliderObj.transform.SetParent(powerPanel.transform, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.35f, 0.2f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.8f);
            sliderRect.sizeDelta = Vector2.zero;

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // Fill Area
            GameObject fillArea = new GameObject("Fill_Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.sizeDelta = Vector2.zero;

            // Fill Image
            GameObject fillImgObj = new GameObject("Fill");
            fillImgObj.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fillImgObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            Image fillImage = fillImgObj.AddComponent<Image>();
            fillImage.color = new Color(0f, 0.85f, 1f, 1f);
            fillImage.raycastTarget = false;
            slider.fillRect = fillRect;

            // Power Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(powerPanel.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.03f, 0f);
            labelRect.anchorMax = new Vector2(0.34f, 1f);
            labelRect.sizeDelta = Vector2.zero;

            Text powerLabel = labelObj.AddComponent<Text>();
            powerLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            powerLabel.fontSize = 17;
            powerLabel.fontStyle = FontStyle.Bold;
            powerLabel.alignment = TextAnchor.MiddleCenter;
            powerLabel.color = Color.white;
            powerLabel.raycastTarget = false;
            powerLabel.text = "POWER: 0%";

            // 3. STRIKE Button (Bottom-Right, Concept Art layout)
            GameObject strikeObj = new GameObject("Strike_Button");
            strikeObj.transform.SetParent(canvasObj.transform, false);
            RectTransform strikeRect = strikeObj.AddComponent<RectTransform>();
            strikeRect.anchorMin = new Vector2(1f, 0f);
            strikeRect.anchorMax = new Vector2(1f, 0f);
            strikeRect.pivot = new Vector2(1f, 0f);
            strikeRect.anchoredPosition = new Vector2(-50f, 40f);
            strikeRect.sizeDelta = new Vector2(110f, 110f);

            Image strikeImg = strikeObj.AddComponent<Image>();
            strikeImg.color = new Color(1f, 0.45f, 0.05f, 0.95f);

            Button strikeBtn = strikeObj.AddComponent<Button>();
            ColorBlock cb = strikeBtn.colors;
            cb.normalColor = new Color(1f, 0.45f, 0.05f, 0.95f);
            cb.highlightedColor = new Color(1f, 0.65f, 0.2f, 1f);
            cb.pressedColor = new Color(0.85f, 0.35f, 0f, 1f);
            strikeBtn.colors = cb;

            GameObject strikeLabelObj = new GameObject("Text");
            strikeLabelObj.transform.SetParent(strikeObj.transform, false);
            RectTransform strikeLabelRect = strikeLabelObj.AddComponent<RectTransform>();
            strikeLabelRect.anchorMin = Vector2.zero;
            strikeLabelRect.anchorMax = Vector2.one;
            strikeLabelRect.sizeDelta = Vector2.zero;

            Text strikeText = strikeLabelObj.AddComponent<Text>();
            strikeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            strikeText.fontSize = 20;
            strikeText.fontStyle = FontStyle.Bold;
            strikeText.alignment = TextAnchor.MiddleCenter;
            strikeText.color = Color.white;
            strikeText.raycastTarget = false;
            strikeText.text = "STRIKE";

            // 4. Sequential Stage Tracker (Top-Center: 1 -> 2 -> 3)
            GameObject stagePanel = new GameObject("Stage_Tracker_Panel");
            stagePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform stageRect = stagePanel.AddComponent<RectTransform>();
            stageRect.anchorMin = new Vector2(0.5f, 1f);
            stageRect.anchorMax = new Vector2(0.5f, 1f);
            stageRect.pivot = new Vector2(0.5f, 1f);
            stageRect.anchoredPosition = new Vector2(0f, -30f);
            stageRect.sizeDelta = new Vector2(400f, 85f);

            Image stageBg = stagePanel.AddComponent<Image>();
            stageBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            stageBg.raycastTarget = false;

            // Banner Text
            GameObject bannerObj = new GameObject("Banner_Text");
            bannerObj.transform.SetParent(stagePanel.transform, false);
            RectTransform bannerRect = bannerObj.AddComponent<RectTransform>();
            bannerRect.anchorMin = new Vector2(0f, 0.55f);
            bannerRect.anchorMax = new Vector2(1f, 1f);
            bannerRect.sizeDelta = Vector2.zero;

            Text bannerText = bannerObj.AddComponent<Text>();
            bannerText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bannerText.fontSize = 16;
            bannerText.fontStyle = FontStyle.Bold;
            bannerText.alignment = TextAnchor.MiddleCenter;
            bannerText.color = new Color(0f, 0.85f, 1f, 1f);
            bannerText.raycastTarget = false;
            bannerText.text = "YOUR TURN  •  TARGET: PIT 1";

            // Beads Container
            Image bead1 = CreateBead("Bead_1", stagePanel.transform, new Vector2(-70f, -22f), "1");
            CreateArrowText("Arrow_1_2", stagePanel.transform, new Vector2(-23f, -22f));
            Image bead2 = CreateBead("Bead_2", stagePanel.transform, new Vector2(23f, -22f), "2");
            CreateArrowText("Arrow_2_3", stagePanel.transform, new Vector2(70f, -22f));
            Image bead3 = CreateBead("Bead_3", stagePanel.transform, new Vector2(117f, -22f), "3");

            // 5. Stroke & Par Scoreboard (Top-Left)
            GameObject scorePanel = new GameObject("Score_Panel");
            scorePanel.transform.SetParent(canvasObj.transform, false);
            RectTransform scoreRect = scorePanel.AddComponent<RectTransform>();
            scoreRect.anchorMin = new Vector2(0f, 1f);
            scoreRect.anchorMax = new Vector2(0f, 1f);
            scoreRect.pivot = new Vector2(0f, 1f);
            scoreRect.anchoredPosition = new Vector2(40f, -30f);
            scoreRect.sizeDelta = new Vector2(280f, 80f);

            Image scoreBg = scorePanel.AddComponent<Image>();
            scoreBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            scoreBg.raycastTarget = false;

            // Stroke Text
            GameObject strokeTextObj = new GameObject("Stroke_Text");
            strokeTextObj.transform.SetParent(scorePanel.transform, false);
            RectTransform strokeRectTransform = strokeTextObj.AddComponent<RectTransform>();
            strokeRectTransform.anchorMin = new Vector2(0.06f, 0.5f);
            strokeRectTransform.anchorMax = new Vector2(0.94f, 0.95f);
            strokeRectTransform.sizeDelta = Vector2.zero;

            Text strokeText = strokeTextObj.AddComponent<Text>();
            strokeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            strokeText.fontSize = 20;
            strokeText.fontStyle = FontStyle.Bold;
            strokeText.alignment = TextAnchor.MiddleLeft;
            strokeText.color = Color.white;
            strokeText.raycastTarget = false;
            strokeText.text = "STROKES: 0";

            // Par Text
            GameObject parTextObj = new GameObject("Par_Text");
            parTextObj.transform.SetParent(scorePanel.transform, false);
            RectTransform parRectTransform = parTextObj.AddComponent<RectTransform>();
            parRectTransform.anchorMin = new Vector2(0.06f, 0.05f);
            parRectTransform.anchorMax = new Vector2(0.94f, 0.5f);
            parRectTransform.sizeDelta = Vector2.zero;

            Text parText = parTextObj.AddComponent<Text>();
            parText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            parText.fontSize = 14;
            parText.fontStyle = FontStyle.Bold;
            parText.alignment = TextAnchor.MiddleLeft;
            parText.color = new Color(0.5f, 0.85f, 1f, 0.9f);
            parText.raycastTarget = false;
            parText.text = "COURSE PAR: 8";

            // 6. Player Badges Panel (Top-Right, Concept Art layout: P1..P4)
            CreatePlayerBadgesPanel(canvasObj.transform, out Image[] badgeImages, out Text[] badgeTexts, out Image[] badgeGlows);

            // 7. Victory Modal (Center Overlay, initially inactive)
            GameObject modalObj = new GameObject("Victory_Modal");
            modalObj.transform.SetParent(canvasObj.transform, false);
            RectTransform modalRect = modalObj.AddComponent<RectTransform>();
            modalRect.anchorMin = new Vector2(0.5f, 0.5f);
            modalRect.anchorMax = new Vector2(0.5f, 0.5f);
            modalRect.pivot = new Vector2(0.5f, 0.5f);
            modalRect.anchoredPosition = Vector2.zero;
            modalRect.sizeDelta = new Vector2(540f, 440f);

            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.04f, 0.06f, 0.1f, 0.96f);

            // Header
            GameObject vHeaderObj = new GameObject("Header");
            vHeaderObj.transform.SetParent(modalObj.transform, false);
            RectTransform vHeaderRect = vHeaderObj.AddComponent<RectTransform>();
            vHeaderRect.anchorMin = new Vector2(0f, 0.82f);
            vHeaderRect.anchorMax = new Vector2(1f, 0.97f);
            vHeaderRect.sizeDelta = Vector2.zero;
            Text vHeader = vHeaderObj.AddComponent<Text>();
            vHeader.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vHeader.fontSize = 28;
            vHeader.fontStyle = FontStyle.Bold;
            vHeader.alignment = TextAnchor.MiddleCenter;
            vHeader.color = new Color(1f, 0.85f, 0.2f, 1f);
            vHeader.text = "★ MATCH FINISHED! ★";

            // Strokes / Winner Text
            GameObject vStrokesObj = new GameObject("Strokes");
            vStrokesObj.transform.SetParent(modalObj.transform, false);
            RectTransform vStrokesRect = vStrokesObj.AddComponent<RectTransform>();
            vStrokesRect.anchorMin = new Vector2(0f, 0.66f);
            vStrokesRect.anchorMax = new Vector2(1f, 0.82f);
            vStrokesRect.sizeDelta = Vector2.zero;
            Text vStrokes = vStrokesObj.AddComponent<Text>();
            vStrokes.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vStrokes.fontSize = 22;
            vStrokes.fontStyle = FontStyle.Bold;
            vStrokes.alignment = TextAnchor.MiddleCenter;
            vStrokes.color = Color.white;
            vStrokes.text = "★ WINNER ★";

            // Course Par Text
            GameObject vParObj = new GameObject("CoursePar");
            vParObj.transform.SetParent(modalObj.transform, false);
            RectTransform vParRect = vParObj.AddComponent<RectTransform>();
            vParRect.anchorMin = new Vector2(0f, 0.56f);
            vParRect.anchorMax = new Vector2(1f, 0.66f);
            vParRect.sizeDelta = Vector2.zero;
            Text vPar = vParObj.AddComponent<Text>();
            vPar.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vPar.fontSize = 16;
            vPar.alignment = TextAnchor.MiddleCenter;
            vPar.color = new Color(0.7f, 0.85f, 1f, 1f);
            vPar.text = "COURSE PAR: 8";

            // Rating / Ranked Podium Text
            GameObject vRatingObj = new GameObject("Rating");
            vRatingObj.transform.SetParent(modalObj.transform, false);
            RectTransform vRatingRect = vRatingObj.AddComponent<RectTransform>();
            vRatingRect.anchorMin = new Vector2(0.08f, 0.16f);
            vRatingRect.anchorMax = new Vector2(0.92f, 0.55f);
            vRatingRect.sizeDelta = Vector2.zero;
            Text vRating = vRatingObj.AddComponent<Text>();
            vRating.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vRating.fontSize = 18;
            vRating.fontStyle = FontStyle.Bold;
            vRating.lineSpacing = 1.25f;
            vRating.alignment = TextAnchor.MiddleCenter;
            vRating.color = new Color(0.2f, 1f, 0.5f, 1f);
            vRating.text = "PODIUM STANDINGS";

            // Play Again Button
            GameObject againBtnObj = new GameObject("Play_Again_Button");
            againBtnObj.transform.SetParent(modalObj.transform, false);
            RectTransform againBtnRect = againBtnObj.AddComponent<RectTransform>();
            againBtnRect.anchorMin = new Vector2(0.25f, 0.035f);
            againBtnRect.anchorMax = new Vector2(0.75f, 0.14f);
            againBtnRect.sizeDelta = Vector2.zero;

            Image againImg = againBtnObj.AddComponent<Image>();
            againImg.color = new Color(0f, 0.8f, 0.4f, 1f);

            Button againBtn = againBtnObj.AddComponent<Button>();

            GameObject againTextObj = new GameObject("Text");
            againTextObj.transform.SetParent(againBtnObj.transform, false);
            RectTransform againTextRect = againTextObj.AddComponent<RectTransform>();
            againTextRect.anchorMin = Vector2.zero;
            againTextRect.anchorMax = Vector2.one;
            againTextRect.sizeDelta = Vector2.zero;

            Text againText = againTextObj.AddComponent<Text>();
            againText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            againText.fontSize = 18;
            againText.fontStyle = FontStyle.Bold;
            againText.alignment = TextAnchor.MiddleCenter;
            againText.color = Color.white;
            againText.text = "PLAY AGAIN";

            modalObj.SetActive(false);

            // Wire to HUDManager via SerializedObject
            SerializedObject so = new SerializedObject(hud);
            so.FindProperty("_powerSlider").objectReferenceValue = slider;
            so.FindProperty("_powerFillImage").objectReferenceValue = fillImage;
            so.FindProperty("_powerLabel").objectReferenceValue = powerLabel;
            so.FindProperty("_strikeButton").objectReferenceValue = strikeBtn;
            so.FindProperty("_bead1Image").objectReferenceValue = bead1;
            so.FindProperty("_bead2Image").objectReferenceValue = bead2;
            so.FindProperty("_bead3Image").objectReferenceValue = bead3;
            so.FindProperty("_statusBanner").objectReferenceValue = bannerText;
            so.FindProperty("_strokeCounterText").objectReferenceValue = strokeText;
            so.FindProperty("_parText").objectReferenceValue = parText;
            so.FindProperty("_victoryModal").objectReferenceValue = modalObj;
            so.FindProperty("_victoryStrokesText").objectReferenceValue = vStrokes;
            so.FindProperty("_victoryParText").objectReferenceValue = vPar;
            so.FindProperty("_victoryRatingText").objectReferenceValue = vRating;
            so.FindProperty("_playAgainButton").objectReferenceValue = againBtn;

            // Wire Player Badges arrays
            SerializedProperty bImgsProp = so.FindProperty("_playerBadgeImages");
            bImgsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) bImgsProp.GetArrayElementAtIndex(i).objectReferenceValue = badgeImages[i];

            SerializedProperty bTextsProp = so.FindProperty("_playerBadgeTexts");
            bTextsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) bTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = badgeTexts[i];

            SerializedProperty bGlowsProp = so.FindProperty("_playerBadgeGlows");
            bGlowsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) bGlowsProp.GetArrayElementAtIndex(i).objectReferenceValue = badgeGlows[i];

            so.ApplyModifiedProperties();
        }

        private static void CreatePlayerBadgesPanel(Transform parent, out Image[] badgeImages, out Text[] badgeTexts, out Image[] badgeGlows)
        {
            badgeImages = new Image[4];
            badgeTexts = new Text[4];
            badgeGlows = new Image[4];

            GameObject panelObj = new GameObject("Player_Badges_Panel");
            panelObj.transform.SetParent(parent, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-40f, -30f);
            panelRect.sizeDelta = new Vector2(340f, 80f);

            Image panelBg = panelObj.AddComponent<Image>();
            panelBg.color = new Color(0.05f, 0.08f, 0.12f, 0.85f);
            panelBg.raycastTarget = false;

            Color[] playerColors = new Color[]
            {
                new Color(0f, 0.85f, 1f, 1f),    // P1: Cyan
                new Color(1f, 0.25f, 0.25f, 1f), // P2: Crimson Red
                new Color(0.2f, 1f, 0.4f, 1f),   // P3: Emerald Green
                new Color(1f, 0.75f, 0.1f, 1f)   // P4: Amber Gold
            };

            float startX = 45f;
            float stepX = 82f;

            for (int i = 0; i < 4; i++)
            {
                float posX = startX + (i * stepX);

                // Badge Container
                GameObject container = new GameObject($"Badge_P{i + 1}");
                container.transform.SetParent(panelObj.transform, false);
                RectTransform cRect = container.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(0f, 0.5f);
                cRect.anchorMax = new Vector2(0f, 0.5f);
                cRect.pivot = new Vector2(0.5f, 0.5f);
                cRect.anchoredPosition = new Vector2(posX, 0f);
                cRect.sizeDelta = new Vector2(56f, 56f);

                // Outer Glow / Ring (Active Turn Indicator)
                GameObject glowObj = new GameObject("GlowRing");
                glowObj.transform.SetParent(container.transform, false);
                RectTransform gRect = glowObj.AddComponent<RectTransform>();
                gRect.anchorMin = Vector2.zero;
                gRect.anchorMax = Vector2.one;
                gRect.sizeDelta = new Vector2(8f, 8f);
                Image glowImg = glowObj.AddComponent<Image>();
                glowImg.color = new Color(1f, 0.9f, 0.25f, 1f); // Vibrant Gold Glow
                glowImg.raycastTarget = false;
                glowImg.enabled = (i == 0); // P1 starts active
                badgeGlows[i] = glowImg;

                // Inner Disc
                GameObject discObj = new GameObject("Disc");
                discObj.transform.SetParent(container.transform, false);
                RectTransform dRect = discObj.AddComponent<RectTransform>();
                dRect.anchorMin = Vector2.zero;
                dRect.anchorMax = Vector2.one;
                dRect.sizeDelta = Vector2.zero;
                Image discImg = discObj.AddComponent<Image>();
                Color c = playerColors[i];
                c.a = (i == 0) ? 1.0f : 0.45f;
                discImg.color = c;
                discImg.raycastTarget = false;
                badgeImages[i] = discImg;

                // Label Text
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(container.transform, false);
                RectTransform lRect = labelObj.AddComponent<RectTransform>();
                lRect.anchorMin = Vector2.zero;
                lRect.anchorMax = Vector2.one;
                lRect.sizeDelta = Vector2.zero;
                Text labelText = labelObj.AddComponent<Text>();
                labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                labelText.fontSize = 20;
                labelText.fontStyle = FontStyle.Bold;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.color = Color.white;
                labelText.raycastTarget = false;
                labelText.text = $"P{i + 1}";
                badgeTexts[i] = labelText;
            }
        }

        private static Image CreateBead(string name, Transform parent, Vector2 pos, string number)
        {
            GameObject beadObj = new GameObject(name);
            beadObj.transform.SetParent(parent, false);
            RectTransform rect = beadObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(34f, 34f);

            Image img = beadObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.25f, 0.3f, 1f);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(beadObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text t = textObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.text = number;

            return img;
        }

        private static void CreateArrowText(string name, Transform parent, Vector2 pos)
        {
            GameObject arrowObj = new GameObject(name);
            arrowObj.transform.SetParent(parent, false);
            RectTransform rect = arrowObj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(25f, 30f);

            Text t = arrowObj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 16;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.6f, 0.6f, 0.7f, 0.8f);
            t.text = "→";
        }

        private static Material GetOrCreateMarbleMaterial(string path, Color color)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader uLit = Shader.Find("Universal Render Pipeline/Lit");
                if (uLit == null) uLit = Shader.Find("Standard");
                mat = new Material(uLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", 0.92f);
            mat.SetFloat("_Metallic", 0.05f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void PlaceVillageProp(string assetPath, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null) return;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            if (instance == null) instance = Object.Instantiate(prefab, parent);

            instance.name = name;
            instance.transform.localPosition = pos;
            instance.transform.localRotation = Quaternion.Euler(euler);
            instance.transform.localScale = scale;

            // Strip any colliders and rigidbodies so props never interfere with gameplay physics
            foreach (var col in instance.GetComponentsInChildren<Collider>(true))
            {
                Undo.DestroyObjectImmediate(col);
            }
            foreach (var rb in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                Undo.DestroyObjectImmediate(rb);
            }
        }

        private static Material GetOrCreateMaterial(string path, Color color, float smoothness = 0.5f, float metallic = 0.0f)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader uLit = Shader.Find("Universal Render Pipeline/Lit");
                if (uLit == null) uLit = Shader.Find("Standard");
                mat = new Material(uLit);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateVillageDirtMaterial()
        {
            string path = "Assets/_Project/Art/Materials/M_Ground_Sand.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader uLit = Shader.Find("Universal Render Pipeline/Lit");
                if (uLit == null) uLit = Shader.Find("Standard");
                mat = new Material(uLit);
                AssetDatabase.CreateAsset(mat, path);
            }

            // High-res village soil textures from CrashBash
            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/CrashBash/Fbx/1.jpg");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ThirdParty/CrashBash/Fbx/1NM.jpg");

            // Fallback to canyon ground if CrashBash 1.jpg is not found
            if (albedo == null)
                albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/CanyonDesert/canyon_ground_01_albedo.jpg");
            if (normal == null)
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/CanyonDesert/canyon_ground_01_normal.jpg");

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_MainTex", albedo);
                mat.SetTextureScale("_BaseMap", new Vector2(4f, 16f));
            }
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.2f);
                mat.EnableKeyword("_NORMALMAP");
            }

            mat.SetColor("_BaseColor", new Color(0.92f, 0.72f, 0.54f, 1.0f));
            mat.SetFloat("_Smoothness", 0.12f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreateCanyonMaterial()
        {
            string path = "Assets/_Project/Art/Materials/M_Canyon_Desert.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                Shader uLit = Shader.Find("Universal Render Pipeline/Lit");
                if (uLit == null) uLit = Shader.Find("Standard");
                mat = new Material(uLit);
                AssetDatabase.CreateAsset(mat, path);
            }

            Texture2D albedo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/CanyonDesert/canyon_cliff_02_albedo.jpg");
            Texture2D normal = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/Textures/CanyonDesert/canyon_cliff_02_normal.jpg");

            if (albedo != null)
            {
                mat.SetTexture("_BaseMap", albedo);
                mat.SetTexture("_MainTex", albedo);
                mat.SetTextureScale("_BaseMap", new Vector2(4f, 4f));
            }
            if (normal != null)
            {
                mat.SetTexture("_BumpMap", normal);
                mat.SetFloat("_BumpScale", 1.2f);
                mat.EnableKeyword("_NORMALMAP");
            }
            mat.SetColor("_BaseColor", new Color(0.85f, 0.62f, 0.44f, 1.0f));
            mat.SetFloat("_Smoothness", 0.18f);
            mat.SetFloat("_Metallic", 0.0f);
            EditorUtility.SetDirty(mat);
            return mat;
        }
    }
}
#endif
