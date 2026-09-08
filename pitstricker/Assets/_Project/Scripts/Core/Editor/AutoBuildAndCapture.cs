#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class AutoBuildAndCapture
    {
        private static bool _executed = false;

        static AutoBuildAndCapture()
        {
            EditorApplication.update += OnUpdate;
        }

        [MenuItem(""Pit Striker/Force Auto Build & Capture Snapshot"", false, 5)]
        public static void ForceRun()
        {
            _executed = false;
            ExecuteBuildAndCapture();
        }

        private static void OnUpdate()
        {
            if (_executed) return;
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

            string doneFile = Path.Combine(Application.dataPath, ""../Library/AutoBuildDone.flag"");
            if (File.Exists(doneFile)) return;

            _executed = true;
            EditorApplication.update -= OnUpdate;

            try
            {
                ExecuteBuildAndCapture();
                File.WriteAllText(doneFile, ""done"");
            }
            catch (System.Exception ex)
            {
                Debug.LogError(""[AUTO BUILD ERROR] "" + ex.Message + ""\n"" + ex.StackTrace);
            }
        }

        public static void ExecuteBuildAndCapture()
        {
            string scenePath = ""Assets/_Project/Scenes/SC_Sandbox_Learning.unity"";
            Debug.Log(""<color=#00FFAA><b>[AUTO BUILD]</b> Opening "" + scenePath + ""...</color>"");
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            Debug.Log(""<color=#00FFAA><b>[AUTO BUILD]</b> Generating Village Physics Arena...</color>"");
            SandboxBuilder.GeneratePhysicsArena();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log(""<color=#00FF88><b>[AUTO BUILD]</b> Scene saved successfully!</color>"");

            // Capture snapshot of Camera.main
            Camera cam = Camera.main;
            if (cam != null)
            {
                int width = 1280;
                int height = 720;
                RenderTexture rt = new RenderTexture(width, height, 24);
                cam.targetTexture = rt;
                Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                cam.Render();
                RenderTexture.active = rt;
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = null;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                byte[] bytes = tex.EncodeToPNG();
                Object.DestroyImmediate(tex);

                string outPath = @""C:\Users\Tisan\.gemini\antigravity\brain\abf9bbad-8ef0-408c-b0dc-d77f9cb16cc3\scratch\unity_camera_view.png"";
                File.WriteAllBytes(outPath, bytes);
                Debug.Log(""<color=#00FFAA><b>[AUTO BUILD]</b> Snapshot saved to: "" + outPath + ""</color>"");
            }
        }
    }
}
#endif
