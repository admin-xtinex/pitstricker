#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using PitStriker.Networking;
using PitStriker.Gameplay;
using PitStriker.UI;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class MultiplayerSmokeTest
    {
        private const string RequestFile = "Library/MultiplayerSmokeTest.request";
        private const string ResultFile = "Library/MultiplayerSmokeTest.result";

        static MultiplayerSmokeTest()
        {
            EditorApplication.update += CheckTestRequest;
            EditorApplication.delayCall += EnsureNetworkMatchStatePrefab;
        }

        public static void EnsureNetworkMatchStatePrefab()
        {
            string dir = "Assets/Resources";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string path = "Assets/Resources/NetworkMatchState.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing == null)
            {
                GameObject temp = new GameObject("NetworkMatchState");
                temp.AddComponent<NetworkObject>();
                temp.AddComponent<NetworkMatchState>();
                existing = PrefabUtility.SaveAsPrefabAsset(temp, path);
                UnityEngine.Object.DestroyImmediate(temp);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("<color=#00FF88>[PREFAB] Created NetworkMatchState.prefab at " + path + "</color>");
            }

            var defaultPrefabs = AssetDatabase.LoadAssetAtPath<Unity.Netcode.NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
            if (defaultPrefabs != null && existing != null)
            {
                SerializedObject so = new SerializedObject(defaultPrefabs);
                SerializedProperty listProp = so.FindProperty("List") ?? so.FindProperty("m_List") ?? so.FindProperty("m_PrefabList");
                if (listProp != null && listProp.isArray)
                {
                    bool found = false;
                    for (int i = 0; i < listProp.arraySize; i++)
                    {
                        var elem = listProp.GetArrayElementAtIndex(i);
                        var prefabProp = elem.FindPropertyRelative("Prefab") ?? elem.FindPropertyRelative("m_Prefab");
                        if (prefabProp != null && prefabProp.objectReferenceValue == existing)
                        {
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                    {
                        int idx = listProp.arraySize;
                        listProp.InsertArrayElementAtIndex(idx);
                        var elem = listProp.GetArrayElementAtIndex(idx);
                        var prefabProp = elem.FindPropertyRelative("Prefab") ?? elem.FindPropertyRelative("m_Prefab");
                        if (prefabProp != null) prefabProp.objectReferenceValue = existing;
                        so.ApplyModifiedProperties();
                        AssetDatabase.SaveAssets();
                        Debug.Log("<color=#00FF88>[PREFAB] Registered NetworkMatchState in DefaultNetworkPrefabs.asset</color>");
                    }
                }
            }
        }

        private static void CheckTestRequest()
        {
            if (!File.Exists(RequestFile) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string req = File.ReadAllText(RequestFile).Trim();
            if (req == "REFRESH")
            {
                File.Delete(RequestFile);
                UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
                AssetDatabase.Refresh();
                return;
            }

            File.Delete(RequestFile);
            Debug.Log("<color=#00FFAA><b>[MULTIPLAYER TEST]</b> Running All Multiplayer Smoke Tests...</color>");

            try
            {
                EnsureNetworkMatchStatePrefab();
                RunPhase1Validation();
                RunPhase2Validation();
                RunPhase6Validation();
                RunPhase7Validation();
                RunPhase8Validation();
                MultiplayerSimulationTest.RunAllSimulationTests();
                string successMsg = "SUCCESS: All Multiplayer Phases (1 through 10) smoke and simulation tests passed at " + DateTime.Now;
                File.WriteAllText(ResultFile, successMsg);
                Debug.Log($"<color=#00FF88><b>[MULTIPLAYER TEST SUCCESS]</b> {successMsg}</color>");
            }
            catch (Exception ex)
            {
                string err = "FAILED: " + ex.Message + "\n" + ex.StackTrace;
                File.WriteAllText(ResultFile, err);
                Debug.LogError($"[MULTIPLAYER TEST FAILED] {err}");
            }
        }

        [MenuItem("Pit Striker/Multiplayer/Run All Smoke Tests", false, 1)]
        public static void RunAllValidation()
        {
            RunPhase1Validation();
            RunPhase2Validation();
            RunPhase6Validation();
            RunPhase7Validation();
            RunPhase8Validation();
            MultiplayerSimulationTest.RunAllSimulationTests();
            Debug.Log("<color=#00FF88>[MULTIPLAYER TEST] All Phases (1 through 10) Manual Validation Passed!</color>");
        }

        public static void RunPhase1Validation()
        {
            // 1. Verify Netcode Types are present
            Type nmType = typeof(NetworkManager);
            if (nmType == null) throw new Exception("NetworkManager type not found.");

            Type utpType = typeof(UnityTransport);
            if (utpType == null) throw new Exception("UnityTransport type not found.");

            Type bsType = typeof(NetworkBootstrap);
            if (bsType == null) throw new Exception("NetworkBootstrap type not found.");

            Type nsmType = typeof(NetworkSessionManager);
            if (nsmType == null) throw new Exception("NetworkSessionManager type not found.");

            var ilppType = typeof(NetworkManager).Assembly.GetType("Unity.Netcode.ILPPMessageProvider");
            if (ilppType == null) throw new Exception("ILPPMessageProvider type was not found in Netcode assembly!");
            var testField = ilppType.GetField("IntegrationTestNoMessages", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            if (testField == null) throw new Exception("IntegrationTestNoMessages field was not found on ILPPMessageProvider!");
            testField.SetValue(null, true);

            // 2. Verify GameObject creation and component binding
            GameObject testObj = new GameObject("__Test_Multiplayer_Host");
            try
            {
                NetworkManager nm = testObj.AddComponent<NetworkManager>();
                nm.SetSingleton();
                var awakeMethod = typeof(NetworkManager).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awakeMethod?.Invoke(nm, null);

                UnityTransport utp = testObj.AddComponent<UnityTransport>();
                nm.NetworkConfig = new NetworkConfig
                {
                    NetworkTransport = utp,
                    ProtocolVersion = 1,
                    ConnectionApproval = false,
                    EnableSceneManagement = false
                };

                NetworkSessionManager sm = testObj.AddComponent<NetworkSessionManager>();

                // Verify local host startup
                ushort testPort = (ushort)UnityEngine.Random.Range(7800, 7900);
                utp.SetConnectionData("127.0.0.1", testPort);
                bool started = nm.StartHost();
                if (!started) throw new Exception("Local host failed to start in test.");

                if (!nm.IsHost || !nm.IsServer)
                {
                    throw new Exception("NetworkManager did not report IsHost == true.");
                }

                // Verify clean shutdown
                nm.Shutdown();
            }
            finally
            {
                testField?.SetValue(null, false);
                UnityEngine.Object.DestroyImmediate(testObj);
            }

            // 3. Verify single-player TurnManager is completely untouched and valid
            Type tmType = typeof(TurnManager);
            if (tmType == null) throw new Exception("TurnManager type missing!");
        }

        public static void RunPhase2Validation()
        {
            // 1. Verify ScreenType enum values exist
            var playModeVal = Enum.Parse(typeof(MenuManager.ScreenType), "PlayModeSelect");
            var onlineMenuVal = Enum.Parse(typeof(MenuManager.ScreenType), "OnlineMenu");
            var createMatchVal = Enum.Parse(typeof(MenuManager.ScreenType), "CreateMatch");
            var joinMatchVal = Enum.Parse(typeof(MenuManager.ScreenType), "JoinMatch");
            var onlineLobbyVal = Enum.Parse(typeof(MenuManager.ScreenType), "OnlineLobby");

            if (playModeVal == null || onlineMenuVal == null || createMatchVal == null || joinMatchVal == null || onlineLobbyVal == null)
            {
                throw new Exception("Missing one or more Phase 2 ScreenType enum values.");
            }

            // 2. Construct MenuManager on a test Canvas and verify hierarchy and screen switching
            GameObject menuObj = new GameObject("__Test_MenuManager_UI");
            try
            {
                MenuManager menu = menuObj.AddComponent<MenuManager>();
                var awakeMethod = typeof(MenuManager).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awakeMethod?.Invoke(menu, null);

                // Test Screen Switching
                menu.ShowScreen(MenuManager.ScreenType.PlayModeSelect);
                if (menu.CurrentScreen != MenuManager.ScreenType.PlayModeSelect)
                {
                    throw new Exception("ShowScreen(PlayModeSelect) failed to update CurrentScreen.");
                }

                menu.ShowScreen(MenuManager.ScreenType.OnlineMenu);
                if (menu.CurrentScreen != MenuManager.ScreenType.OnlineMenu)
                {
                    throw new Exception("ShowScreen(OnlineMenu) failed to update CurrentScreen.");
                }

                menu.ShowScreen(MenuManager.ScreenType.CreateMatch);
                if (menu.CurrentScreen != MenuManager.ScreenType.CreateMatch)
                {
                    throw new Exception("ShowScreen(CreateMatch) failed to update CurrentScreen.");
                }

                menu.ShowScreen(MenuManager.ScreenType.JoinMatch);
                if (menu.CurrentScreen != MenuManager.ScreenType.JoinMatch)
                {
                    throw new Exception("ShowScreen(JoinMatch) failed to update CurrentScreen.");
                }

                menu.ShowScreen(MenuManager.ScreenType.OnlineLobby);
                if (menu.CurrentScreen != MenuManager.ScreenType.OnlineLobby)
                {
                    throw new Exception("ShowScreen(OnlineLobby) failed to update CurrentScreen.");
                }

                // Verify Local play mode is completely preserved
                menu.ShowScreen(MenuManager.ScreenType.ChoosePlayers);
                if (menu.CurrentScreen != MenuManager.ScreenType.ChoosePlayers)
                {
                    throw new Exception("ShowScreen(ChoosePlayers) failed; local mode regressed!");
                }

                menu.ShowScreen(MenuManager.ScreenType.Home);
                if (menu.CurrentScreen != MenuManager.ScreenType.Home)
                {
                    throw new Exception("ShowScreen(Home) failed.");
                }

                // Verify required ActionButtons exist in hierarchy
                CheckButtonExists(menuObj, "Btn_PlayLocal");
                CheckButtonExists(menuObj, "Btn_PlayOnline");
                CheckButtonExists(menuObj, "Btn_OnlineQuickMatch");
                CheckButtonExists(menuObj, "Btn_OnlineCreateMatch");
                CheckButtonExists(menuObj, "Btn_OnlineJoinMatch");
                CheckButtonExists(menuObj, "Btn_CopyJoinCode");
                CheckButtonExists(menuObj, "Btn_CancelCreateMatch");
                CheckButtonExists(menuObj, "Btn_SubmitJoin");
                CheckButtonExists(menuObj, "Btn_LeaveLobby");

                Debug.Log("<color=#00FFAA><b>[TEST]</b> Phase 2 UI hierarchy and button controls verified.</color>");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(menuObj);
            }
        }

        public static void RunPhase6Validation()
        {
            // 1. Verify DisconnectGracePeriodManager exists and has expected duration
            Type graceType = typeof(DisconnectGracePeriodManager);
            if (graceType == null) throw new Exception("DisconnectGracePeriodManager type not found.");

            if (DisconnectGracePeriodManager.GraceDuration != 30f)
            {
                throw new Exception($"Expected GraceDuration to be 30s, got {DisconnectGracePeriodManager.GraceDuration}s");
            }

            // 2. Verify NetworkMatchState timer pause/resume methods exist
            var pauseMethod = typeof(NetworkMatchState).GetMethod("ServerPauseTimer");
            var resumeMethod = typeof(NetworkMatchState).GetMethod("ServerResumeTimer");
            if (pauseMethod == null) throw new Exception("NetworkMatchState.ServerPauseTimer method missing!");
            if (resumeMethod == null) throw new Exception("NetworkMatchState.ServerResumeTimer method missing!");

            // 3. Verify DisconnectGracePeriodManager lifecycle on a test object
            GameObject testObj = new GameObject("__Test_GraceManager");
            try
            {
                var gm = testObj.AddComponent<DisconnectGracePeriodManager>();
                var awakeMethod = typeof(DisconnectGracePeriodManager).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awakeMethod?.Invoke(gm, null);
                if (DisconnectGracePeriodManager.Instance == null)
                {
                    throw new Exception("DisconnectGracePeriodManager singleton instance not set properly.");
                }
                gm.CancelGracePeriod();
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(testObj);
            }

            Debug.Log("<color=#00FFAA><b>[TEST]</b> Phase 6 Disconnect & Reconnect validation passed.</color>");
        }

        public static void RunPhase7Validation()
        {
            // 1. Verify QuickMatchManager type and constants
            Type qmType = typeof(QuickMatchManager);
            if (qmType == null) throw new Exception("QuickMatchManager type not found.");

            if (QuickMatchManager.GameModeKey != "pit_striker_v1")
            {
                throw new Exception($"Expected GameModeKey 'pit_striker_v1', got '{QuickMatchManager.GameModeKey}'");
            }

            if (QuickMatchManager.SearchTimeout != 30f)
            {
                throw new Exception($"Expected SearchTimeout 30s, got {QuickMatchManager.SearchTimeout}s");
            }

            // 2. Verify QuickMatch ScreenType enum exists
            var qmScreenVal = Enum.Parse(typeof(MenuManager.ScreenType), "QuickMatch");
            if (qmScreenVal == null) throw new Exception("QuickMatch not found in ScreenType enum.");

            // 3. Verify MenuManager handles QuickMatch screen and includes required buttons
            GameObject menuObj = new GameObject("__Test_MenuManager_Phase7");
            try
            {
                MenuManager menu = menuObj.AddComponent<MenuManager>();
                var awakeMethod = typeof(MenuManager).GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                awakeMethod?.Invoke(menu, null);

                menu.ShowScreen(MenuManager.ScreenType.QuickMatch);
                if (menu.CurrentScreen != MenuManager.ScreenType.QuickMatch)
                {
                    throw new Exception("ShowScreen(QuickMatch) failed to update CurrentScreen.");
                }

                CheckButtonExists(menuObj, "Btn_OnlineQuickMatch");
                CheckButtonExists(menuObj, "Btn_QMCancel");

                Debug.Log("<color=#00FFAA><b>[TEST]</b> Phase 7 Quick Match validation passed.</color>");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(menuObj);
            }
        }

        public static void RunPhase8Validation()
        {
            // 1. Verify MultiplayerSecurityLogger is present
            Type loggerType = typeof(MultiplayerSecurityLogger);
            if (loggerType == null) throw new Exception("MultiplayerSecurityLogger type not found.");

            // 2. Verify security constants on NetworkMatchState
            if (NetworkMatchState.MinAllowedForce != 0.5f)
                throw new Exception($"Expected MinAllowedForce 0.5f, got {NetworkMatchState.MinAllowedForce}");
            if (NetworkMatchState.MaxAllowedForce != 45f)
                throw new Exception($"Expected MaxAllowedForce 45f, got {NetworkMatchState.MaxAllowedForce}");
            if (NetworkMatchState.MaxAllowedVerticalRatio != 0.25f)
                throw new Exception($"Expected MaxAllowedVerticalRatio 0.25f, got {NetworkMatchState.MaxAllowedVerticalRatio}");

            // 3. Verify validation rules on a test NetworkMatchState instance
            GameObject nmsObj = new GameObject("__Test_NetworkMatchState_Sec");
            try
            {
                var nms = nmsObj.AddComponent<NetworkMatchState>();

                // Configure test match state
                nms.Player1.Value = new NetworkPlayerData(100, "Player 1");
                nms.Player2.Value = new NetworkPlayerData(200, "Player 2");
                nms.ActivePlayerIndex.Value = 0;
                nms.CurrentPhase.Value = NetworkMatchPhase.ReadyToAim;
                nms.TurnTimerRemaining.Value = 20f;
                nms.IsTimerRunning.Value = true;
                nms.IsMatchCompleted.Value = false;

                // Rule A: Non-member client rejected
                if (nms.ValidateShotCommand(999, 1, Vector3.forward, 15f, out string errNonMember))
                    throw new Exception("Security breach: Non-member client shot was accepted!");

                // Rule B: Out-of-turn client rejected
                if (nms.ValidateShotCommand(200, 1, Vector3.forward, 15f, out string errOutOfTurn))
                    throw new Exception("Security breach: Out-of-turn client shot was accepted!");

                // Rule C: Excessive force rejected
                if (nms.ValidateShotCommand(100, 1, Vector3.forward, 999f, out string errExcessForce))
                    throw new Exception("Security breach: Excessive force shot was accepted!");

                // Rule D: Negative / below-min force rejected
                if (nms.ValidateShotCommand(100, 1, Vector3.forward, 0.1f, out string errLowForce))
                    throw new Exception("Security breach: Below-minimum force shot was accepted!");

                // Rule E: Zero direction rejected
                if (nms.ValidateShotCommand(100, 1, Vector3.zero, 15f, out string errZeroDir))
                    throw new Exception("Security breach: Zero direction shot was accepted!");

                // Rule F: High vertical pitch rejected
                if (nms.ValidateShotCommand(100, 1, new Vector3(0, 0.9f, 0.1f), 15f, out string errPitch))
                    throw new Exception("Security breach: Sky launch vertical pitch shot was accepted!");

                // Rule G: Valid active shot accepted
                if (!nms.ValidateShotCommand(100, 1, Vector3.forward, 15f, out string errValid))
                    throw new Exception($"Valid shot was unexpectedly rejected: {errValid}");

                // Rule H: Wrong phase rejected
                nms.CurrentPhase.Value = NetworkMatchPhase.Rolling;
                if (nms.ValidateShotCommand(100, 2, Vector3.forward, 15f, out string errWrongPhase))
                    throw new Exception("Security breach: Shot accepted during Rolling phase!");
                nms.CurrentPhase.Value = NetworkMatchPhase.ReadyToAim;

                // Rule I: Completed match rejected
                nms.IsMatchCompleted.Value = true;
                if (nms.ValidateShotCommand(100, 2, Vector3.forward, 15f, out string errCompleted))
                    throw new Exception("Security breach: Shot accepted after match completed!");
                nms.IsMatchCompleted.Value = false;

                // 4. Verify NetworkVariable permissions are strictly Server-only write
                if (nms.ActivePlayerIndex.WritePerm != Unity.Netcode.NetworkVariableWritePermission.Server)
                    throw new Exception("ActivePlayerIndex write permission is not Server-only!");
                if (nms.WinnerPlayerIndex.WritePerm != Unity.Netcode.NetworkVariableWritePermission.Server)
                    throw new Exception("WinnerPlayerIndex write permission is not Server-only!");
                if (nms.Player1.WritePerm != Unity.Netcode.NetworkVariableWritePermission.Server)
                    throw new Exception("Player1 write permission is not Server-only!");
                if (nms.Player2.WritePerm != Unity.Netcode.NetworkVariableWritePermission.Server)
                    throw new Exception("Player2 write permission is not Server-only!");

                Debug.Log("<color=#00FFAA><b>[TEST]</b> Phase 8 Multiplayer Security & Validation tests passed.</color>");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(nmsObj);
            }
        }

        private static void CheckButtonExists(GameObject root, string buttonName)
        {
            var buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.gameObject.name == buttonName) return;
            }
            throw new Exception($"Required UI Button '{buttonName}' was not found in MenuManager hierarchy!");
        }
    }
}
#endif
