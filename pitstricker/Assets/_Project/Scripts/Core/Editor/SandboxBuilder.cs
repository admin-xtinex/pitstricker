#if UNITY_EDITOR
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
    /// with REAL PHYSICAL HOLES and recessed pit basins.
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

            // 3. Segmented Ground Structure (Leaving actual physical holes at Z: 1, 6, 11)
            // Left & Right Bank Slabs
            CreateGroundSlab("Ground_Bank_Left", arenaRoot.transform, new Vector3(-2.7f, -0.25f, 5f), new Vector3(2.6f, 0.5f, 22f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Bank_Right", arenaRoot.transform, new Vector3(2.7f, -0.25f, 5f), new Vector3(2.6f, 0.5f, 22f), sandMat, sandPhys);

            // Center Track Slabs between the pits
            CreateGroundSlab("Ground_Center_Start", arenaRoot.transform, new Vector3(0f, -0.25f, -3.1f), new Vector3(2.8f, 0.5f, 5.8f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_1_2", arenaRoot.transform, new Vector3(0f, -0.25f, 3.5f), new Vector3(2.8f, 0.5f, 2.6f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_Bridge_2_3", arenaRoot.transform, new Vector3(0f, -0.25f, 8.5f), new Vector3(2.8f, 0.5f, 2.6f), sandMat, sandPhys);
            CreateGroundSlab("Ground_Center_End", arenaRoot.transform, new Vector3(0f, -0.25f, 14.1f), new Vector3(2.8f, 0.5f, 3.8f), sandMat, sandPhys);

            // 4. Boundary Rails (Left, Right, Back, Front)
            CreateBoundaryWall("Wall_Left", arenaRoot.transform, new Vector3(-4.1f, 0.35f, 5f), new Vector3(0.3f, 0.8f, 22f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Right", arenaRoot.transform, new Vector3(4.1f, 0.35f, 5f), new Vector3(0.3f, 0.8f, 22f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Back", arenaRoot.transform, new Vector3(0f, 0.35f, -6.1f), new Vector3(8.5f, 0.8f, 0.3f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Front", arenaRoot.transform, new Vector3(0f, 0.35f, 16.1f), new Vector3(8.5f, 0.8f, 0.3f), woodMat, bouncePhys);

            // 5. Create 3 Real Recessed Sunken Pits
            CreateSunkenPit("Pit_01", arenaRoot.transform, new Vector3(0f, 0f, 1f), 1, pitMat, woodMat);
            CreateSunkenPit("Pit_02", arenaRoot.transform, new Vector3(0f, 0f, 6f), 2, pitMat, woodMat);
            CreateSunkenPit("Pit_03", arenaRoot.transform, new Vector3(0f, 0f, 11f), 3, pitMat, woodMat);

            // 6. Create Player Striker Marble
            GameObject marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marble.name = "PlayerMarble_Blue";
            marble.transform.position = new Vector3(0f, 0.3f, -4.5f);
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

            // 7. Main Camera Setup
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

            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Arena rebuilt with REAL SUNKEN PITS! Marbles will now fall directly into the pit cups.</color>");
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

        private static void CreateSunkenPit(string name, Transform parent, Vector3 position, int pitNumber, Material pitMat, Material rimMat)
        {
            GameObject pitRoot = new GameObject(name);
            pitRoot.transform.SetParent(parent);
            pitRoot.transform.position = position;

            // 1. Sunken Basin Floor (Solid surface at depth Y: -0.38)
            GameObject basinFloor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            basinFloor.name = "Basin_Floor";
            basinFloor.transform.SetParent(pitRoot.transform);
            basinFloor.transform.localPosition = new Vector3(0f, -0.38f, 0f);
            basinFloor.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
            if (pitMat != null) basinFloor.GetComponent<MeshRenderer>().sharedMaterial = pitMat;

            // 2. Beveled Funnel Rim (Sloped cylinder funneling marbles down)
            GameObject rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            rim.name = "Basin_Rim";
            rim.transform.SetParent(pitRoot.transform);
            rim.transform.localPosition = new Vector3(0f, -0.2f, 0f);
            rim.transform.localScale = new Vector3(2.1f, 0.15f, 2.1f);
            if (pitMat != null) rim.GetComponent<MeshRenderer>().sharedMaterial = pitMat;

            // Remove solid collider from rim so marble drops through freely
            Object.DestroyImmediate(rim.GetComponent<Collider>());

            // 3. Trigger Zone inside the cup
            SphereCollider trigger = pitRoot.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 1.0f;
            trigger.center = new Vector3(0f, -0.15f, 0f);

            PitZone zone = pitRoot.AddComponent<PitZone>();
        }
    }
}
#endif
