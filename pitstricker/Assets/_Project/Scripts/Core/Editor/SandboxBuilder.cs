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
    /// matching the concept art layouts.
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
            PhysicsMaterial sandPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Sand_Friction.physicMaterial");
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PM_Marble_Bouncy.physicMaterial");

            // 1. Root Arena Container
            GameObject arenaRoot = GameObject.Find("Arena_Sandbox");
            if (arenaRoot != null)
            {
                Undo.DestroyObjectImmediate(arenaRoot);
            }
            arenaRoot = new GameObject("Arena_Sandbox");
            Undo.RegisterCreatedObjectUndo(arenaRoot, "Create Arena Root");

            // 2. Solid Ground Box (Cube prevents any tunneling)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Arena_Ground";
            ground.transform.SetParent(arenaRoot.transform);
            ground.transform.position = new Vector3(0f, -0.25f, 5f);
            ground.transform.localScale = new Vector3(8f, 0.5f, 22f);

            if (sandMat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = sandMat;
            BoxCollider groundCollider = ground.GetComponent<BoxCollider>();
            if (sandPhys != null) groundCollider.sharedMaterial = sandPhys;

            // 3. Boundary Rails (Left, Right, Back, Front)
            CreateBoundaryWall("Wall_Left", arenaRoot.transform, new Vector3(-4.1f, 0.3f, 5f), new Vector3(0.3f, 0.7f, 22f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Right", arenaRoot.transform, new Vector3(4.1f, 0.3f, 5f), new Vector3(0.3f, 0.7f, 22f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Back", arenaRoot.transform, new Vector3(0f, 0.3f, -6.1f), new Vector3(8.5f, 0.7f, 0.3f), woodMat, bouncePhys);
            CreateBoundaryWall("Wall_Front", arenaRoot.transform, new Vector3(0f, 0.3f, 16.1f), new Vector3(8.5f, 0.7f, 0.3f), woodMat, bouncePhys);

            // 4. Create 3 Numbered Pits (Pit 1, Pit 2, Pit 3 along the track)
            CreatePit("Pit_01", arenaRoot.transform, new Vector3(0f, 0.01f, 1f), 1);
            CreatePit("Pit_02", arenaRoot.transform, new Vector3(0f, 0.01f, 6f), 2);
            CreatePit("Pit_03", arenaRoot.transform, new Vector3(0f, 0.01f, 11f), 3);

            // 5. Create Player Striker Marble
            GameObject marble = GameObject.Find("PlayerMarble_Blue");
            if (marble != null)
            {
                Undo.DestroyObjectImmediate(marble);
            }
            marble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marble.name = "PlayerMarble_Blue";
            marble.transform.position = new Vector3(0f, 0.25f, -4f);
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

            // Core Scripts
            MarbleController marbleController = marble.AddComponent<MarbleController>();

            // Trajectory Line
            LineRenderer line = marble.AddComponent<LineRenderer>();
            line.startWidth = 0.08f;
            line.endWidth = 0.15f;
            line.startColor = new Color(0.1f, 0.8f, 1f, 0.9f);
            line.endColor = new Color(0.1f, 0.9f, 1f, 0.3f);
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            line.material.color = new Color(0f, 0.85f, 1f, 1f);
            line.enabled = false;

            SwipeLaunchController launcher = marble.AddComponent<SwipeLaunchController>();

            // 6. Main Camera Setup
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.5f, -8.5f);
                cam.transform.rotation = Quaternion.Euler(20f, 0f, 0f);

                SmoothFollowCamera follow = cam.GetComponent<SmoothFollowCamera>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<SmoothFollowCamera>();
                }
                follow.SetTarget(marble.transform);
            }

            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = marble;

            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Arena successfully generated with 3 Pits, Boundary Rails, and Blue Striker Marble!</color>");
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

        private static void CreatePit(string name, Transform parent, Vector3 position, int pitNumber)
        {
            GameObject pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pit.name = name;
            pit.transform.SetParent(parent);
            pit.transform.position = position;
            pit.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);

            // Dark rim material for the pit
            Material pitMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            pitMat.color = new Color(0.12f, 0.09f, 0.06f, 1f);
            pit.GetComponent<MeshRenderer>().sharedMaterial = pitMat;

            // Remove default solid collider and add trigger zone
            Object.DestroyImmediate(pit.GetComponent<Collider>());
            SphereCollider trigger = pit.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.radius = 0.7f;
            trigger.center = new Vector3(0f, 0.1f, 0f);

            PitZone zone = pit.AddComponent<PitZone>();
        }
    }
}
#endif
