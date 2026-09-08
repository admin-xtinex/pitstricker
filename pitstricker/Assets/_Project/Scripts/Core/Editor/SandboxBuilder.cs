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

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Studio Editor Automation:
    /// One-click generation of the complete Pit Striker Physics Arena
    /// with Round Pits, Chalk Launch Reticle, and Full Concept HUD (Power Meter + Stage Beads).
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

            GameObject oldHUD = GameObject.Find("HUD_Canvas");
            if (oldHUD != null) Undo.DestroyObjectImmediate(oldHUD);

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

            // 5. Create Ground Chalk Launch Ring (Concept Image 1 & 2 reference)
            CreateChalkRing("Chalk_Launch_Ring", arenaRoot.transform, new Vector3(0f, 0.015f, -5.5f), 1.0f);

            // 6. Create Player Striker Marble
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

            // 7. Main Camera Setup
            Camera cam = Camera.main;
            if (cam != null)
            {
                cam.transform.position = new Vector3(0f, 2.8f, -10.0f);
                cam.transform.rotation = Quaternion.Euler(22f, 0f, 0f);

                SmoothFollowCamera follow = cam.GetComponent<SmoothFollowCamera>();
                if (follow == null)
                {
                    follow = cam.gameObject.AddComponent<SmoothFollowCamera>();
                }
                follow.SetTarget(marble.transform);
            }

            // 8. Create Concept HUD Canvas (Power Meter + Stage Beads 1 -> 2 -> 3)
            CreateHUDCanvas();

            Undo.CollapseUndoOperations(group);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Arena rebuilt with 100% ROUND PITS, Numbered Flags, Chalk Ring, and Concept HUD (Power Meter & Strike)!</color>");
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

            // Numbered Flag Marker next to the pit
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
            UnityEngine.EventSystems.EventSystem es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
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
            so.ApplyModifiedProperties();
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
    }
}
#endif
