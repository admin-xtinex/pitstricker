#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class BuildWindowsPlayer
    {
        private const string BuildOutputDirectory = "../Builds/Windows";
        private const string ExeFileName = "PitStriker.exe";
        private const string RequestFile = "Library/BuildWindows.request";
        private const string ResultFile = "Library/BuildWindows.result";

        static BuildWindowsPlayer()
        {
            EditorApplication.update += CheckBuildRequest;
        }

        private static void CheckBuildRequest()
        {
            if (Application.isBatchMode) return;
            if (!File.Exists(RequestFile) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
                return;
            }
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            File.Delete(RequestFile);
            try
            {
                BuildWindows();
                File.WriteAllText(ResultFile, "SUCCESS: Windows build completed at " + DateTime.Now);
            }
            catch (Exception ex)
            {
                File.WriteAllText(ResultFile, "ERROR: " + ex.Message + "\n" + ex.StackTrace);
                Debug.LogException(ex);
            }
        }

        [MenuItem("Pit Striker/Build Windows Standalone", false, 2)]
        public static void BuildWindows()
        {
            Debug.Log("<color=#00FFAA><b>[BUILD WINDOWS]</b> Initiating Windows Standalone build process...</color>");

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string fullOutputDir = Path.Combine(projectRoot, BuildOutputDirectory);
            if (!Directory.Exists(fullOutputDir))
            {
                Directory.CreateDirectory(fullOutputDir);
            }

            string exePath = Path.Combine(fullOutputDir, ExeFileName);

            PlayerSettings.productName = "Pit Striker";
            PlayerSettings.companyName = "xtinex";

            // Ensure single pipeline consistency across GraphicsSettings and all Quality levels
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
                Debug.Log($"<color=#00FFAA><b>[BUILD WINDOWS]</b> Verified RenderPipelineAsset: {rpAsset.name} across GraphicsSettings and all {qualityCount} quality levels.</color>");
            }

            string[] scenePaths = EditorBuildSettings.scenes
                .Where(s => s.enabled && !string.IsNullOrEmpty(s.path) && File.Exists(s.path))
                .Select(s => s.path)
                .ToArray();

            if (scenePaths.Length == 0)
            {
                scenePaths = new string[] { "Assets/_Project/Scenes/MainMenu.unity", "Assets/_Project/Scenes/SC_SunsetCoastal_Map02.unity" };
            }

            Debug.Log($"<color=#00FFAA><b>[BUILD WINDOWS]</b> Building scenes ({scenePaths.Length}): {string.Join(", ", scenePaths)}</color>");
            Debug.Log($"<color=#00FFAA><b>[BUILD WINDOWS]</b> Target Output: {exePath}</color>");

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = scenePaths,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                float sizeMb = summary.totalSize / (1024f * 1024f);
                Debug.Log($"<color=#00FF88><b>[BUILD SUCCESS]</b> Windows build created successfully in {summary.totalTime.TotalSeconds:F1}s! Output: {exePath} ({sizeMb:F2} MB)</color>");
            }
            else
            {
                Debug.LogError($"<color=#FF0044><b>[BUILD FAILED]</b> Windows build failed with {summary.totalErrors} errors.</color>");
                throw new InvalidOperationException($"Windows build failed with {summary.totalErrors} errors.");
            }
        }
    }
}
#endif
