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
        private const string ResultFile = "Library/BuildAPK.result";

        static BuildAPK()
        {
            EditorApplication.update += CheckBuildRequest;
        }

        private static bool _waitingForCompile = false;

        private static void CheckBuildRequest()
        {
            if (Application.isBatchMode) return; // Batch builds use an explicit executeMethod.
            if (File.Exists("Library/MenuFlowChecks.running")) return;

            if (_waitingForCompile)
            {
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                _waitingForCompile = false;

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
                return;
            }

            if (!File.Exists(RequestFile)) return;

            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            File.Delete(RequestFile);
            _waitingForCompile = true;
            AssetDatabase.Refresh();
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

            string apkPath = Path.Combine(fullOutputDir, ApkFileName);

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
            string keystorePath = Path.Combine(projectRoot, "Keystore", "pitstriker.keystore");
            if (File.Exists(keystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = "xtinex123";
                PlayerSettings.Android.keyaliasName = "pitstriker";
                PlayerSettings.Android.keyaliasPass = "xtinex123";
                Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Signing using Release Keystore: {keystorePath} (Alias: pitstriker)</color>");
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

            // 5. Execute Build Pipeline (with automatic incremental retries for transient Windows file locks)
            int maxAttempts = 15;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    Debug.Log($"<color=#00FFAA><b>[BUILD APK]</b> Executing build (attempt {attempt}/{maxAttempts})...</color>");
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
                    else if (summary.result == BuildResult.Failed)
                    {
                        if (attempt < maxAttempts)
                        {
                            Debug.LogWarning($"[BUILD APK] Attempt {attempt} encountered an error (likely transient Windows file lock). Retrying incrementally in 2s...");
                            System.GC.Collect();
                            System.GC.WaitForPendingFinalizers();
                            System.Threading.Thread.Sleep(2000);
                            continue;
                        }
                        Debug.LogError($"<color=#FF0044><b>[BUILD FAILED]</b> Android build failed with {summary.totalErrors} errors after {maxAttempts} attempts. Check Console logs for details.</color>");
                        throw new InvalidOperationException($"Android build failed with {summary.totalErrors} errors.");
                    }
                    else
                    {
                        Debug.LogWarning($"[BUILD APK] Build finished with status: {summary.result}");
                        throw new InvalidOperationException($"Android build finished with non-success status: {summary.result}");
                    }
                }
                catch (Exception ex)
                {
                    if (attempt < maxAttempts && (ex is InvalidOperationException || ex.Message.Contains("Android build failed")))
                    {
                        Debug.LogWarning($"[BUILD APK] Attempt {attempt} exception: {ex.Message}. Retrying incrementally in 2s...");
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.Threading.Thread.Sleep(2000);
                        continue;
                    }

                    Debug.LogError($"[BUILD APK ERROR] Exception occurred during build: {ex.Message}\n{ex.StackTrace}");
                    if (ex.Message.Contains("module is not installed") || ex.Message.Contains("target is not supported"))
                    {
                        Debug.LogError("<color=#FFD700><b>[ACTION REQUIRED]</b> Android Build Support module is not installed for this Unity version.</color>\n" +
                                       "Please open <b>Unity Hub > Installs > 6000.6.0f1 (Gear Icon) > Add Modules > Android Build Support</b> (with OpenJDK & Android SDK/NDK Tools) to enable APK builds.");
                    }
                    throw;
                }
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
    }
}
#endif
