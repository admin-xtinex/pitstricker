#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using PitStriker.Physics;
using PitStriker.Input;
using PitStriker.CameraSystem;
using PitStriker.Gameplay;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Studio Editor Automation:
    /// One-click generation of the complete Pit Striker Physics Arena
    /// featuring mathematically smooth, perfectly ROUND pits matching concept art.
    /// </summary>
    public static class SandboxBuilder
    {
        [MenuItem("Pit Striker/Generate Physics Arena", false, 10)]
        public static void GeneratePhysicsArena()
        {
            Undo.SetCurrentGroupName("Generate Pit Striker Arena");
            int group = Undo.GetCurrentGroup();

            // Load Materials
            Material sandMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Ground_Sand.mat");
            Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Boundary_Wood.mat");
            Material marbleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Marble_Blue.mat");
            Material pitMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Pit_Dark.mat");
            Material lineMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Trajectory_Cyan.mat");

            PhysicsMaterial sandPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Sand_Friction.physicMaterial");
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");

            // 1. Clean up old objects
            GameObject oldGround = GameObject.Find("Ground");
            if (oldGround != null) Undo.DestroyObjectImmediate(oldGround);

            GameObject oldTestMarble = GameObject.Find("TestMarble");
            if (oldTestMarble != null) Undo.DestroyObjectImmediate(oldTestMarble);

            GameObject oldArena = GameObject.Find("Arena_Sandbox");
            if (oldArena != null) Undo.DestroyObjectImmediate(oldArena);

            GameObject oldMarble = GameObject.Find("PlayerMarble_Blue");
            if (oldMarble != null) Undo.DestroyObjectImmediate(oldMarble);

            // 2. Root Arena Container
            GameObject arenaRoot = new GameObject("Arena_Sandbox");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // Master safety subfloor underneath the entire arena (40-meter fairway)
            CreateGroundSlab("Ground_Safety_Subfloor", arenaRoot.transform, new Vector3(0f, -1.2f, 12f), new Vector3(12f, 0.5f, 44f), sandMat, sandPhys);

            // Left & Right Bank Slabs
            CreateGroundSlab("Ground_Bank_Left", arenaRoot.transform, new Vector3(-2.7f, -0.25f, 12f), new Vector3(2.6f, 0.5f, 40f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Bank_Right", arenaRoot.transform, new Vector3(2.7f, -0.25f, 12f), new Vector3(2.6f, 0.5f, 40f), sandMat, sandPhys);

            // Center Track Slabs connecting seamlessly with round pit tiles
            // Track runs from Z: -8.0 to Z: 32.0 (40m total distance)
            CreateGroundSlab("Ground_Center_Start", arenaRoot.transform, new Vector3(0f, -0.25f, -3.75f), new Vector3(2.8f, 0.5f, 8.5f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_1_2", arenaRoot.transform, new Vector3(0f, -0.25f, 7.5f), new Vector3(2.8f, 0.5f, 8.0f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_2_3", arenaRoot.transform, new Vector3(0f, -0.25f, 19.0f), new Vector3(2.8f, 0.5f, 9.0f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_End", arenaRoot.transform, new Vector3(0f, -0.25f, 29.25f), new Vector3(2.8f, 0.5f, 5.5f), sandMat, sandPhys);

            // 3. Boundary Rails (Left, Right, Back, Front)
            CreateBoundaryWall("Wall_Left", arenaRoot.transform, new Vector3(-4.1f, 0.35f, 12f), new Vector3(0.3f, 0.8f, 40f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Right", arenaRoot.transform, new Vector3(4.1f, 0.35f, 12f), new Vector3(0.3f, 0.8f, 40f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Back", arenaRoot.transform, new Vector3(0f, 0.35f, -8.1f), new Vector3(8.5f, 0.8f, 0.3f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Front", arenaRoot.transform, new Vector3(0f, 0.35f, 32.1f), new Vector3(8.5f, 0.8f, 0.3f), woodMat, bouncePhys);

            // 4. Create 3 TRUE ROUND PITS with generous spacing (11-12m apart)
            CreateRoundPitTile("Pit_01_Round", arenaRoot.transform, new Vector3(0f, 0f, 2.0f), 1, sandMat, pitMat, woodMat, sandPhys);
            CreateRoundPitTile("Pit_02_Round", arenaRoot.transform, new Vector3(0f, 0f, 13.0f), 2, sandMat, pitMat, woodMat, sandPhys);
            CreateRoundPitTile("Pit_03_Round", arenaRoot.transform, new Vector3(0f, 0f, 25.0f), 3, sandMat, pitMat, woodMat, sandPhys);

            // 5. Create Player Striker Marble
            GameObject marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marble.name = "PlayerMarble_Blue";
            marble.transform.position = new Vector3(0f, 0.3f, -5.5f);
            marble.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            if (marbleMat != null) marble.GetComponent<MeshRenderer>().sharedMaterial = marbleMat;

            // Rigidbody
            Rigidbody rb = marble.AddComponent<Rigidbody>();
            rb.mass = 1.0f;
            rb.linearDamping = 0.3f;
            rb.angularDamping = 0.8f;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            // SphereCollider
            SphereCollider sc = marble.GetComponent<SphereCollider>();
            sc.center = Vector3.zero;
            sc.radius = 0.5f;
            if (bouncePhys != null) sc.sharedMaterial = bouncePhys;

            // Core Marble Controller
            MarbleController marbleController = marble.AddComponent<MarbleController>();

            // Trajectory Line
            LineRenderer line = marble.AddComponent<LineRenderer>();
            line.startWidth = 0.08f;
            line.endWidth = 0.16f;
            line.startColor = new Color(0.1f, 0.85f, 1f, 0.95f);
            line.endColor = new Color(0.1f, 0.9f, 1f, 0.3f);
            line.useWorldSpace = true;
            if (lineMat != null) line.sharedMaterial = lineMat;
            line.enabled = false;

            SwipeLaunchController launcher = marble.AddComponent<SwipeLaunchController>();

            // 6. Main Camera Setup
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.8f, -9.0f);
                cam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

                SmoothFollowCamera follow = cam.GetComponent<SmoothFollowCamera>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<SmoothFollowCamera>();
                }
                follow.SetTarget(marble.transform);
            }

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = marble;

            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Arena rebuilt with 100% ROUND CIRCULAR PITS and numbered flags!</color>");
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

        private static void CreateBoundaryWall(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, PhysicsMaterial physMat)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;

            if (mat != null) wall.GetComponent<MeshRenderer>().sharedMaterial = mat;
            BoxCollider col = wall.GetComponent<BoxCollider>();
            if (physMat != null) col.sharedMaterial = physMat;
        }

        /// <summary>
        /// Procedurally generates a seamless ground tile containing a perfectly ROUND 3D hole and recessed cup.
        /// </summary>
        private static void CreateRoundPitTile(string name, Transform parent, Vector3 position, int pitNumber, Material sandMat, Material pitMat, Material woodMat, PhysicsMaterial physMat)
        {
            GameObject pitRoot = new GameObject(name);
            pitRoot.transform.SetParent(parent);
            pitRoot.transform.position = position;

            MeshFilter mf = pitRoot.AddComponent<MeshFilter>();
            MeshRenderer mr = pitRoot.AddComponent<MeshRenderer>();

            float width = 2.8f;
            float length = 2.6f;
            float radius = 0.75f;
            float depth = 0.38f;
            int segments = 24;

            Mesh mesh = new Mesh();
            mesh.name = name + "_RoundMesh";

            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();

            List<int> sandTriangles = new List<int>();
            List<int> pitTriangles = new List<int>();

            // 1. Top Sand Surface (Outer rectangle to inner circle)
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                float tx = Mathf.Abs(cos) > 0.0001f ? (width * 0.5f) / Mathf.Abs(cos) : float.MaxValue;
                float tz = Mathf.Abs(sin) > 0.0001f ? (length * 0.5f) / Mathf.Abs(sin) : float.MaxValue;
                float t = Mathf.Min(tx, tz);

                Vector3 outerPt = new Vector3(t * cos, 0f, t * sin);
                Vector3 innerRimPt = new Vector3(radius * cos, 0f, radius * sin);

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

            // 2. Vertical Cylindrical Inner Wall of the Pit Cup
            int wallStartIndex = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector3 topPt = new Vector3(radius * cos, 0f, radius * sin);
                Vector3 bottomPt = new Vector3(radius * cos, -depth, radius * sin);
                Vector3 inNormal = new Vector3(-cos, 0f, -sin);

                vertices.Add(topPt);
                normals.Add(inNormal);
                uvs.Add(new Vector2((float)i / segments, 0f));

                vertices.Add(bottomPt);
                normals.Add(inNormal);
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

                vertices.Add(new Vector3(radius * cos, -depth, radius * sin));
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

            // Trigger Zone inside the cup
            SphereCollider trigger = pitRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = radius * 1.1f;
            trigger.center = new Vector3(0f, -depth * 0.5f, 0f);

            PitZone zone = pitRoot.AddComponent<PitZone>();

            // 4. Numbered Flag Marker next to the pit (like concept art!)
            CreateFlagPole("Flag_" + pitNumber, pitRoot.transform, new Vector3(radius + 0.35f, 0f, 0f), pitNumber, woodMat);
        }

        private static void CreateFlagPole(string name, Transform parent, Vector3 localPos, int number, Material woodMat)
        {
            GameObject flagObj = new GameObject(name);
            flagObj.transform.SetParent(parent);
            flagObj.transform.localPosition = localPos;

            // Pole
            GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pole.name = "Pole";
            pole.transform.SetParent(flagObj.transform);
            pole.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            pole.transform.localScale = new Vector3(0.04f, 0.45f, 0.04f);
            if (woodMat != null) pole.GetComponent<MeshRenderer>().sharedMaterial = woodMat;
            Object.DestroyImmediate(pole.GetComponent<Collider>());

            // Red Flag Banner
            GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
            banner.name = "Banner";
            banner.transform.SetParent(flagObj.transform);
            banner.transform.localPosition = new Vector3(0.18f, 0.75f, 0f);
            banner.transform.localScale = new Vector3(0.32f, 0.22f, 0.02f);

            Material redMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            redMat.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            banner.GetComponent<MeshRenderer>().sharedMaterial = redMat;
            Object.DestroyImmediate(banner.GetComponent<Collider>());
        }
    }
}
#endif
