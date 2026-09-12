#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class BuildAPK
    {
        private const string BuildOutputDirectory = "../Builds/Android";
        private const string ApkFileName = "PitStriker.apk";
        private const string RequestFile = "Library/BuildAPK.request";
        private const string PendingFile = "Library/BuildAPK.pending";
        private const string ResultFile = "Library/BuildAPK.result";

        static BuildAPK()
        {
            EditorApplication.update += CheckBuildRequest;
        }

        private static void CheckBuildRequest()
        {
            if (Application.isBatchMode) return;
            if (File.Exists("Library/MenuFlowChecks.running")) return;

            if (File.Exists(RequestFile))
            {
                if (EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying = false;
                    return;
                }
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;

                File.Delete(RequestFile);
                File.WriteAllText(PendingFile, DateTime.UtcNow.ToString("o"));
                AssetDatabase.Refresh();
                return;
            }

            if (File.Exists(PendingFile))
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                File.Delete(PendingFile);

                if (EditorUtility.scriptCompilationFailed)
                {
                    Debug.LogError("[BUILD APK] Cannot build APK: scripts have compile errors in the editor.");
                    File.WriteAllText(ResultFile, "ERROR: Cannot build APK - script compilation failed in Editor.");
                    return;
                }

                try
                {
                    BuildAndroidPlayer();
                    File.WriteAllText(ResultFile, "SUCCESS: APK build completed at " + DateTime.Now);
                }
                catch (Exception ex)
                {
                    File.WriteAllText(ResultFile, "ERROR: " + ex.Message + "\n" + ex.StackTrace);
                    Debug.LogException(ex);
                }
            }
        }

        [MenuItem("Pit Striker/Export Android APK", false, 1)]
        public static void BuildAndroidPlayer()
        {

            Debug.Log("<color=#00FFAA><b>[BUILD APK]</b> Initiating Android APK build process...</color>");

            // Ensure NetworkMatchState prefab and registration exist
            MultiplayerSmokeTest.EnsureNetworkMatchStatePrefab();

            string productionScene = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
            if (File.Exists(productionScene))
            {
                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(productionScene, UnityEditor.SceneManagement.OpenSceneMode.Single);
                Debug.Log("<color=#00FFAA><b>[BUILD APK]</b> Target production village scene: " + productionScene + "</color>");
            }

            Unity.Burst.BurstCompiler.Options.EnableBurstCompilation = false;

            // 1. Ensure output directory exists
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullOutputDir = Path.Combine(projectRoot, BuildOutputDirectory);
            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }

            string apkPath = GetCustomBuildPath();
            if (string.IsNullOrEmpty(apkPath))
            {
                apkPath = Path.Combine(fullOutputDir, ApkFileName);
            }
            string apkDir = Path.GetDirectoryName(apkPath);
            if (!string.IsNullOrEmpty(apkDir) && !Directory.Exists(apkDir))
            {
                Directory.CreateDirectory(apkDir);
            }

            // 2. Configure Player Settings for Android
            PlayerSettings.productName = "Pit Striker";
            PlayerSettings.companyName = "xtinex";
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.xtinex.pitstriker");
            PlayerSettings.bundleVersion = "1.0.0";
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // 2b. Ensure single pipeline consistency across GraphicsSettings and all Quality levels
            var rpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.RenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            if (rpAsset != null)
            {
                UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = rpAsset;
                int qualityCount = QualitySettings.names.Length;
                for (int i = 0; i < qualityCount; i++)
                {
                    QualitySettings.SetQualityLevel(i, false);
                    QualitySettings.renderPipeline = rpAsset;
                }
                AssetDatabase.SaveAssets();
                Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Verified RenderPipelineAsset: {rpAsset.name} across GraphicsSettings and all {qualityCount} quality levels.</color>");
            }

            // 3. Keystore Signing Configuration
            string keystorePath = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PATH");
            if (string.IsNullOrEmpty(keystorePath) || !File.Exists(keystorePath))
            {
                keystorePath = Path.Combine(projectRoot, "Keystore", "pitstriker.keystore");
                if (!File.Exists(keystorePath))
                {
                    keystorePath = Path.Combine(projectRoot, "..", "Keystore", "pitstriker.keystore");
                }
            }

            if (File.Exists(keystorePath))
            {
                string pass = Environment.GetEnvironmentVariable("ANDROID_KEYSTORE_PASS") ?? "xtinex123";
                string alias = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_NAME") ?? "pitstriker";
                string aliasPass = Environment.GetEnvironmentVariable("ANDROID_KEYALIAS_PASS") ?? "xtinex123";

                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = Path.GetFullPath(keystorePath);
                PlayerSettings.Android.keystorePass = pass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = aliasPass;
                Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Signing using Release Keystore: {keystorePath} (Alias: {alias})</color>");
            }
            else
            {
                Debug.LogWarning($"[BUILD APK] Custom keystore not found at {keystorePath}, using default signing.");
            }

            // 4. Assemble scene list from EditorBuildSettings (or fallback to active scene)
            string[] scenePaths = GetBuildScenes();
            if (scenePaths.Length == 0)
            {
                Debug.LogError("[BUILD APK] No enabled scenes configured in EditorBuildSettings!");
                return;
            }

            Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Building scenes ({scenePaths.Length}): {string.Join(", ", scenePaths)}</color>");
            Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Target APK Output: {apkPath}</color>");

            // 4. Build Player Options
            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = apkPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.None
            };

            // Clear any stale Bee lock file from prior runs
            try
            {
                string tundraLock = Path.Combine(projectRoot, "Library", "Bee", "tundra.lock");
                if (File.Exists(tundraLock)) File.Delete(tundraLock);
            }
            catch { }

            // 5. Execute Build Pipeline
            Debug.Log("<color=#00FFAA><b>[BUILD APK]</b> Executing build...</color>");
            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                float sizeMb = summary.totalSize / (1024f * 1024f);
                Debug.Log($"<color=#00FF88><b>[BUILD SUCCESS]</b> APK created successfully in {summary.totalTime.TotalSeconds:F1}s!</color>");
                Debug.Log($"<color=#00FF88><b>[BUILD OUTPUT]</b> {apkPath} ({sizeMb:F2} MB)</color>");
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(apkPath);
                return;
            }
            else
            {
                Debug.LogError($"<color=#FF0044><b>[BUILD FAILED]</b> Android build finished with status {summary.result} ({summary.totalErrors} errors).</color>");
                foreach (var step in report.steps)
                {
                    foreach (var msg in step.messages)
                    {
                        if (msg.type == LogType.Error || msg.type == LogType.Exception)
                        {
                            Debug.LogError($"[BUILD STEP ERROR] {step.name}: {msg.content}");
                        }
                    }
                }
                throw new InvalidOperationException($"Android build failed with status {summary.result} ({summary.totalErrors} errors).");
            }
        }

        private static string[] GetBuildScenes()
        {
            var list = new System.Collections.Generic.List<string>();
            string productionScene = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
            if (File.Exists(productionScene))
            {
                list.Add(productionScene);
            }

            var scenes = EditorBuildSettings.scenes;
            foreach (var scene in scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path) && scene.path != productionScene)
                {
                    list.Add(scene.path);
                }
            }

            return list.ToArray();
        }

        private static string GetCustomBuildPath()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length - 1; i++)
                {
                    if (string.Equals(args[i], "-customBuildPath", StringComparison.OrdinalIgnoreCase))
                    {
                        return args[i + 1];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BUILD APK] Could not parse customBuildPath argument: {ex.Message}");
            }

            return Environment.GetEnvironmentVariable("CUSTOM_BUILD_PATH");
        }

        public static void BuildAndroidPlayerBatch()
        {
            try
            {
                BuildAndroidPlayer();
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(0);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BATCH BUILD FAILED] {ex.Message}\n{ex.StackTrace}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
                throw;
            }
        }
    }
}
#endif
