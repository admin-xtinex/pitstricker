#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.Gameplay;
using PitStriker.UI;

namespace PitStriker.EditorTools
{
    public static class VillageSceneSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";

        // Explicit menu action only: never overwrite the scene on compilation.
        private static void AutoRunOnCompile()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool("VillageSceneSetup_AutoRun_v1", false))
                {
                    SessionState.SetBool("VillageSceneSetup_AutoRun_v1", true);
                    SetupProductionScene();
                }
            };
        }

        [MenuItem("Pit Striker/Setup Village Production Scene & Screens", false, 5)]
        public static void SetupProductionScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError($"[VILLAGE SETUP] Could not open scene: {ScenePath}");
                return;
            }

            Undo.SetCurrentGroupName("Setup Village Production Scene & Screens");
            int undoGroup = Undo.GetCurrentGroup();

            // 1. Upgrade Boundary Walls to 6m Height, 2.5m Thickness
            UpgradeBoundaryWalls();

            // 2. Add Solid Colliders to Visual Props
            AddCollidersToVisualProps();

            // 3. Build UI Screens Hierarchy (Home, Choose Players, Rules, Pause)
            BuildGameScreens();

            // 4. Update EditorBuildSettings to place Village scene at Index 0
            UpdateBuildSettings();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("<color=#00FF88><b>[VILLAGE SETUP SUCCESS]</b> Village scene upgraded with 6m boundary walls, solid prop colliders, complete Game Screens flow, and set as Scene 0!</color>");
        }

        private static void UpgradeBoundaryWalls()
        {
            PhysicsMaterial bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PhysMat_BoundaryRail.physicMaterial");
            if (bouncePhys == null)
            {
                bouncePhys = new PhysicsMaterial("PhysMat_SuperWall");
                bouncePhys.bounciness = 0.6f;
                bouncePhys.frictionCombine = PhysicsMaterialCombine.Minimum;
                bouncePhys.bounceCombine = PhysicsMaterialCombine.Maximum;
            }

            ConfigureWall("Wall_Left", new Vector3(-8.2f, 3.0f, 14.5f), new Vector3(2.5f, 6.0f, 52.0f), bouncePhys);
            ConfigureWall("Wall_Right", new Vector3(8.2f, 3.0f, 14.5f), new Vector3(2.5f, 6.0f, 52.0f), bouncePhys);
            ConfigureWall("Wall_Back", new Vector3(0f, 3.0f, -9.1f), new Vector3(20.0f, 6.0f, 2.5f), bouncePhys);
            ConfigureWall("Wall_Front", new Vector3(0f, 3.0f, 38.1f), new Vector3(20.0f, 6.0f, 2.5f), bouncePhys);
        }

        private static void ConfigureWall(string name, Vector3 pos, Vector3 scale, PhysicsMaterial physMat)
        {
            GameObject wall = GameObject.Find(name);
            if (wall == null)
            {
                GameObject arena = GameObject.Find("Arena_Sandbox");
                wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = name;
                if (arena != null) wall.transform.SetParent(arena.transform, false);
            }

            wall.transform.position = pos;
            wall.transform.localScale = scale;

            MeshRenderer mr = wall.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false; // Invisible barrier

            BoxCollider col = wall.GetComponent<BoxCollider>();
            if (col == null) col = wall.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.sharedMaterial = physMat;
        }

        private static void AddCollidersToVisualProps()
        {
            PhysicsMaterial woodPhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PhysMat_BoundaryRail.physicMaterial");

            // Props to ensure colliders exist on
            string[] propPrefixes = new string[]
            {
                "VIS_Stone_", "VIS_VillageHome", "VIS_Barricade_", "VIS_Bench_",
                "VIS_VillageBase_", "VIS_VillageStall_", "VIS_Home1", "VIS_Barr_",
                "VIS_Base", "VIS_S1", "VIS_S2", "VIS_Stone_A", "VIS_Stone_B"
            };

            var allTransforms = UnityEngine.Object.FindObjectsByType<Transform>();
            int collidersAdded = 0;

            foreach (var t in allTransforms)
            {
                if (propPrefixes.Any(p => t.name.StartsWith(p)))
                {
                    // Add BoxCollider or MeshCollider if missing
                    Collider col = t.GetComponent<Collider>();
                    if (col == null)
                    {
                        var mf = t.GetComponentInChildren<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            var mc = t.gameObject.AddComponent<MeshCollider>();
                            mc.sharedMesh = mf.sharedMesh;
                            mc.convex = true;
                            if (woodPhys != null) mc.sharedMaterial = woodPhys;
                            collidersAdded++;
                        }
                        else
                        {
                            var bc = t.gameObject.AddComponent<BoxCollider>();
                            if (woodPhys != null) bc.sharedMaterial = woodPhys;
                            collidersAdded++;
                        }
                    }
                }
            }

            Debug.Log($"[VILLAGE SETUP] Verified/added solid colliders on {collidersAdded} village props.");
        }

        private static void BuildGameScreens()
        {
            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Locate or create Canvas_GameScreens
            GameObject canvasObj = GameObject.Find("Canvas_GameScreens");
            if (canvasObj != null)
            {
                UnityEngine.Object.DestroyImmediate(canvasObj);
            }

            canvasObj = new GameObject("Canvas_GameScreens");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // Above 3D scene, below or alongside HUD

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            // Find existing HUD Canvas
            GameObject hudCanvas = GameObject.Find("HUD_Canvas");

            // MenuManager Component
            MenuManager menu = canvasObj.AddComponent<MenuManager>();
            SerializedObject menuSo = new SerializedObject(menu);

            // ==========================================
            // 1. HOME SCREEN PANEL
            // ==========================================
            GameObject homePanel = CreatePanel("Panel_HomeScreen", canvasObj.transform, new Color(0.05f, 0.07f, 0.12f, 0.94f));

            // Title
            GameObject titleObj = CreateText("Text_Title", homePanel.transform, "PIT STRIKER", 54, FontStyle.Bold, new Color(1f, 0.85f, 0.25f, 1f), TextAnchor.MiddleCenter);
            SetRect(titleObj, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.72f), new Vector2(700, 90));

            // Subtitle
            GameObject subObj = CreateText("Text_Subtitle", homePanel.transform, "TRADITIONAL KERALA GOTICHO CHAMPIONSHIP", 20, FontStyle.Bold, new Color(0.85f, 0.90f, 0.95f, 0.9f), TextAnchor.MiddleCenter);
            SetRect(subObj, new Vector2(0.5f, 0.63f), new Vector2(0.5f, 0.63f), new Vector2(700, 40));

            // Play Button
            GameObject playBtn = CreateButton("Btn_Play", homePanel.transform, "PLAY MATCH", new Color(0.12f, 0.65f, 0.35f, 1f), 24);
            SetRect(playBtn, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(320, 68));

            // How to Play / Rules Button
            GameObject rulesBtn = CreateButton("Btn_Rules", homePanel.transform, "HOW TO PLAY", new Color(0.85f, 0.55f, 0.12f, 1f), 20);
            SetRect(rulesBtn, new Vector2(0.5f, 0.36f), new Vector2(0.5f, 0.36f), new Vector2(320, 60));

            // Exit Button
            GameObject exitBtn = CreateButton("Btn_Exit", homePanel.transform, "EXIT GAME", new Color(0.35f, 0.38f, 0.45f, 1f), 18);
            SetRect(exitBtn, new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f), new Vector2(320, 52));

            // ==========================================
            // 2. CHOOSE PLAYERS SCREEN PANEL
            // ==========================================
            GameObject choosePanel = CreatePanel("Panel_ChoosePlayers", canvasObj.transform, new Color(0.06f, 0.08f, 0.14f, 0.96f));

            // Header
            GameObject chooseTitle = CreateText("Text_ChooseTitle", choosePanel.transform, "MATCH SETUP", 42, FontStyle.Bold, new Color(1f, 0.85f, 0.25f, 1f), TextAnchor.MiddleCenter);
            SetRect(chooseTitle, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(600, 60));

            // Player Count Selection Header
            GameObject countLabel = CreateText("Text_CountLabel", choosePanel.transform, "SELECT NUMBER OF PLAYERS:", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            SetRect(countLabel, new Vector2(0.5f, 0.79f), new Vector2(0.5f, 0.79f), new Vector2(500, 36));

            // 2P, 3P, 4P Tabs Row
            GameObject tab2P = CreateButton("Btn_2P", choosePanel.transform, "2 PLAYERS", new Color(0.12f, 0.55f, 0.95f, 1f), 18);
            SetRect(tab2P, new Vector2(0.32f, 0.72f), new Vector2(0.32f, 0.72f), new Vector2(170, 48));

            GameObject tab3P = CreateButton("Btn_3P", choosePanel.transform, "3 PLAYERS", new Color(0.22f, 0.22f, 0.25f, 0.85f), 18);
            SetRect(tab3P, new Vector2(0.50f, 0.72f), new Vector2(0.50f, 0.72f), new Vector2(170, 48));

            GameObject tab4P = CreateButton("Btn_4P", choosePanel.transform, "4 PLAYERS", new Color(0.22f, 0.22f, 0.25f, 0.85f), 18);
            SetRect(tab4P, new Vector2(0.68f, 0.72f), new Vector2(0.68f, 0.72f), new Vector2(170, 48));

            // 4 Player Slot Rows
            Color[] pColors = new Color[]
            {
                new Color(0f, 0.85f, 1f, 1f),    // Blue
                new Color(1f, 0.25f, 0.25f, 1f), // Red
                new Color(0.2f, 1f, 0.4f, 1f),   // Green
                new Color(1f, 0.75f, 0.1f, 1f)   // Yellow
            };

            GameObject[] slotRows = new GameObject[4];
            Text[] slotNames = new Text[4];
            Text[] slotRoles = new Text[4];
            Button[] slotToggles = new Button[4];

            float startY = 0.58f;
            float stepY = 0.10f;

            for (int i = 0; i < 4; i++)
            {
                float rowY = startY - (i * stepY);
                GameObject row = CreatePanel($"Slot_P{i + 1}", choosePanel.transform, new Color(0.12f, 0.15f, 0.22f, 0.85f));
                SetRect(row, new Vector2(0.5f, rowY), new Vector2(0.5f, rowY), new Vector2(650, 60));

                // Color Circle / Badge
                GameObject badge = CreateImage("ColorBadge", row.transform, pColors[i]);
                SetRect(badge, new Vector2(0.06f, 0.5f), new Vector2(0.06f, 0.5f), new Vector2(32, 32));

                // Name
                GameObject pName = CreateText("NameText", row.transform, i == 0 ? "Player 1" : $"Bot {i + 1}", 20, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
                SetRect(pName, new Vector2(0.25f, 0.5f), new Vector2(0.25f, 0.5f), new Vector2(160, 40));

                // Role Toggle Button
                string defaultRole = i == 0 ? "REAL PLAYER" : "AI BOT";
                Color roleColor = i == 0 ? new Color(0.25f, 0.95f, 0.55f, 1f) : new Color(1f, 0.45f, 0.25f, 1f);

                GameObject toggleBtn = CreateButton("ToggleRoleBtn", row.transform, defaultRole, new Color(0.18f, 0.22f, 0.32f, 1f), 17);
                SetRect(toggleBtn, new Vector2(0.74f, 0.5f), new Vector2(0.74f, 0.5f), new Vector2(200, 44));

                Text toggleText = toggleBtn.GetComponentInChildren<Text>();
                if (toggleText != null) toggleText.color = roleColor;

                slotRows[i] = row;
                slotNames[i] = pName.GetComponent<Text>();
                slotRoles[i] = toggleText;
                slotToggles[i] = toggleBtn.GetComponent<Button>();
            }

            // Start Match & Back Buttons
            GameObject backBtn = CreateButton("Btn_BackToHome", choosePanel.transform, "BACK", new Color(0.38f, 0.40f, 0.45f, 1f), 20);
            SetRect(backBtn, new Vector2(0.35f, 0.11f), new Vector2(0.35f, 0.11f), new Vector2(200, 58));

            GameObject startMatchBtn = CreateButton("Btn_StartMatch", choosePanel.transform, "START MATCH", new Color(0.12f, 0.75f, 0.38f, 1f), 22);
            SetRect(startMatchBtn, new Vector2(0.62f, 0.11f), new Vector2(0.62f, 0.11f), new Vector2(280, 58));

            // ==========================================
            // 3. RULES MODAL PANEL
            // ==========================================
            GameObject rulesModal = CreatePanel("Panel_Rules", canvasObj.transform, new Color(0.04f, 0.06f, 0.10f, 0.98f));

            GameObject rulesCard = CreatePanel("Card", rulesModal.transform, new Color(0.10f, 0.13f, 0.20f, 1f));
            SetRect(rulesCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(760, 580));

            GameObject rulesTitle = CreateText("Title", rulesCard.transform, "HOW TO PLAY - PIT STRIKER", 32, FontStyle.Bold, new Color(1f, 0.85f, 0.25f, 1f), TextAnchor.MiddleCenter);
            SetRect(rulesTitle, new Vector2(0.5f, 0.88f), new Vector2(0.5f, 0.88f), new Vector2(680, 50));

            string rulesContent =
                "★ 1. TOSS PHASE (ആദ്യം കുഴിയിലേക്ക് എറിയുക):\n" +
                "   Players swipe forward to flick towards Pit 3. The marble closest to Pit 3 plays first!\n\n" +
                "★ 2. THE THREE PITS (1 -> 2 -> 3):\n" +
                "   You must sink your marble into Pit 1, then Pit 2, and finally Pit 3 in sequence.\n\n" +
                "★ 3. BONUS EXTRA PLAYS (എക്സ്ട്രാ പ്ലേ):\n" +
                "   - Sinking your target pit awards an Extra Shot!\n" +
                "   - Striking an opponent's marble with yours awards a Tactical Extra Shot!\n\n" +
                "★ 4. 3-SHOT CAP PER ROUND:\n" +
                "   A player can take a maximum of 3 shots in one turn. Turn rotates on shot 3!\n\n" +
                "★ 5. VICTORY:\n" +
                "   The first player to conquer Pit 3 wins the match!";

            GameObject rulesBody = CreateText("Body", rulesCard.transform, rulesContent, 18, FontStyle.Normal, new Color(0.92f, 0.94f, 0.98f, 1f), TextAnchor.MiddleLeft);
            SetRect(rulesBody, new Vector2(0.5f, 0.48f), new Vector2(0.5f, 0.48f), new Vector2(660, 320));

            GameObject closeRulesBtn = CreateButton("Btn_CloseRules", rulesCard.transform, "CLOSE", new Color(0.85f, 0.55f, 0.12f, 1f), 20);
            SetRect(closeRulesBtn, new Vector2(0.5f, 0.10f), new Vector2(0.5f, 0.10f), new Vector2(220, 50));

            // ==========================================
            // 4. PAUSE MODAL PANEL
            // ==========================================
            GameObject pauseModal = CreatePanel("Panel_Pause", canvasObj.transform, new Color(0.04f, 0.06f, 0.10f, 0.92f));

            GameObject pauseCard = CreatePanel("Card", pauseModal.transform, new Color(0.10f, 0.13f, 0.20f, 1f));
            SetRect(pauseCard, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(500, 420));

            GameObject pauseTitle = CreateText("Title", pauseCard.transform, "GAME PAUSED", 34, FontStyle.Bold, new Color(1f, 0.85f, 0.25f, 1f), TextAnchor.MiddleCenter);
            SetRect(pauseTitle, new Vector2(0.5f, 0.82f), new Vector2(0.5f, 0.82f), new Vector2(400, 50));

            GameObject resumeBtn = CreateButton("Btn_Resume", pauseCard.transform, "RESUME MATCH", new Color(0.12f, 0.65f, 0.35f, 1f), 22);
            SetRect(resumeBtn, new Vector2(0.5f, 0.60f), new Vector2(0.5f, 0.60f), new Vector2(280, 55));

            GameObject restartBtn = CreateButton("Btn_Restart", pauseCard.transform, "RESTART MATCH", new Color(0.85f, 0.55f, 0.12f, 1f), 20);
            SetRect(restartBtn, new Vector2(0.5f, 0.42f), new Vector2(0.5f, 0.42f), new Vector2(280, 52));

            GameObject homeMenuBtn = CreateButton("Btn_HomeMenu", pauseCard.transform, "MAIN MENU", new Color(0.35f, 0.38f, 0.45f, 1f), 20);
            SetRect(homeMenuBtn, new Vector2(0.5f, 0.24f), new Vector2(0.5f, 0.24f), new Vector2(280, 52));

            // ==========================================
            // 5. IN-GAME HUD PAUSE BUTTON
            // ==========================================
            GameObject hudPauseBtn = null;
            if (hudCanvas != null)
            {
                hudPauseBtn = CreateButton("Btn_HUD_Pause", hudCanvas.transform, "II", new Color(0.15f, 0.18f, 0.25f, 0.85f), 22);
                SetRect(hudPauseBtn, new Vector2(0.95f, 0.93f), new Vector2(0.95f, 0.93f), new Vector2(56, 56));
            }

            // ==========================================
            // 6. WIRE SERIALIZED PROPERTIES ON MENUMANAGER
            // ==========================================
            menuSo.FindProperty("_homePanel").objectReferenceValue = homePanel;
            menuSo.FindProperty("_choosePlayersPanel").objectReferenceValue = choosePanel;
            menuSo.FindProperty("_rulesModal").objectReferenceValue = rulesModal;
            menuSo.FindProperty("_pauseModal").objectReferenceValue = pauseModal;
            menuSo.FindProperty("_hudRoot").objectReferenceValue = hudCanvas;

            menuSo.FindProperty("_homePlayButton").objectReferenceValue = playBtn.GetComponent<Button>();
            menuSo.FindProperty("_homeRulesButton").objectReferenceValue = rulesBtn.GetComponent<Button>();
            menuSo.FindProperty("_homeExitButton").objectReferenceValue = exitBtn.GetComponent<Button>();

            menuSo.FindProperty("_btn2Players").objectReferenceValue = tab2P.GetComponent<Button>();
            menuSo.FindProperty("_btn3Players").objectReferenceValue = tab3P.GetComponent<Button>();
            menuSo.FindProperty("_btn4Players").objectReferenceValue = tab4P.GetComponent<Button>();
            menuSo.FindProperty("_img2Players").objectReferenceValue = tab2P.GetComponent<Image>();
            menuSo.FindProperty("_img3Players").objectReferenceValue = tab3P.GetComponent<Image>();
            menuSo.FindProperty("_img4Players").objectReferenceValue = tab4P.GetComponent<Image>();

            SerializedProperty slotRowsProp = menuSo.FindProperty("_playerSlotRows");
            slotRowsProp.arraySize = 4;
            for (int i = 0; i < 4; i++) slotRowsProp.GetArrayElementAtIndex(i).objectReferenceValue = slotRows[i];

            SerializedProperty slotNamesProp = menuSo.FindProperty("_playerSlotNameTexts");
            slotNamesProp.arraySize = 4;
            for (int i = 0; i < 4; i++) slotNamesProp.GetArrayElementAtIndex(i).objectReferenceValue = slotNames[i];

            SerializedProperty slotRolesProp = menuSo.FindProperty("_playerSlotRoleTexts");
            slotRolesProp.arraySize = 4;
            for (int i = 0; i < 4; i++) slotRolesProp.GetArrayElementAtIndex(i).objectReferenceValue = slotRoles[i];

            SerializedProperty slotTogglesProp = menuSo.FindProperty("_playerSlotToggleButtons");
            slotTogglesProp.arraySize = 4;
            for (int i = 0; i < 4; i++) slotTogglesProp.GetArrayElementAtIndex(i).objectReferenceValue = slotToggles[i];

            menuSo.FindProperty("_startMatchButton").objectReferenceValue = startMatchBtn.GetComponent<Button>();
            menuSo.FindProperty("_backToHomeButton").objectReferenceValue = backBtn.GetComponent<Button>();

            menuSo.FindProperty("_closeRulesButton").objectReferenceValue = closeRulesBtn.GetComponent<Button>();

            if (hudPauseBtn != null)
                menuSo.FindProperty("_hudPauseButton").objectReferenceValue = hudPauseBtn.GetComponent<Button>();

            menuSo.FindProperty("_pauseResumeButton").objectReferenceValue = resumeBtn.GetComponent<Button>();
            menuSo.FindProperty("_pauseRestartButton").objectReferenceValue = restartBtn.GetComponent<Button>();
            menuSo.FindProperty("_pauseHomeButton").objectReferenceValue = homeMenuBtn.GetComponent<Button>();

            menuSo.ApplyModifiedProperties();

            // Set default active panel
            homePanel.SetActive(true);
            choosePanel.SetActive(false);
            rulesModal.SetActive(false);
            pauseModal.SetActive(false);
            if (hudCanvas != null) hudCanvas.SetActive(false);

            Debug.Log("[VILLAGE SETUP] UI Game Screens constructed and MenuManager properties mapped successfully.");
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;

            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static GameObject CreateImage(string name, Transform parent, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            Image img = obj.AddComponent<Image>();
            img.color = color;
            return obj;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyle style, Color color, TextAnchor align)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();

            Text t = obj.AddComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = style;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return obj;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, Color bgColor, int fontSize)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            btnObj.AddComponent<RectTransform>();

            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = bgColor;
            cb.highlightedColor = Color.Lerp(bgColor, Color.white, 0.2f);
            cb.pressedColor = Color.Lerp(bgColor, Color.black, 0.2f);
            btn.colors = cb;

            GameObject textObj = CreateText("Text", btnObj.transform, label, fontSize, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            return btnObj;
        }

        private static void SetRect(GameObject obj, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
        {
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect == null) rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void UpdateBuildSettings()
        {
            EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log($"[VILLAGE SETUP] EditorBuildSettings set Scene 0 to {ScenePath}");
        }
    }
}
#endif
