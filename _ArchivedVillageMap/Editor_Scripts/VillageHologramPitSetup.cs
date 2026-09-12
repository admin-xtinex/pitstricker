#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.Visuals;
using PitStriker.Gameplay;

namespace PitStriker.EditorTools
{
    public static class VillageHologramPitSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        private const string OutDir = @"C:\Users\Tisan\.gemini\antigravity\brain\abf9bbad-8ef0-408c-b0dc-d77f9cb16cc3";

        [MenuItem("Pit Striker/Apply Deep Red Pits & In-Pit Numbers", false, 14)]
        public static void ApplyHologramsAndRedPits()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[PIT SETUP] Could not open scene: " + ScenePath);
                return;
            }

            // Rebuild seamless pits and fairway
            VillagePitMeshGenerator.RebuildVillagePits();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#00FFAA><b>[PIT SETUP SUCCESS]</b> Deep red circular pits with in-pit numerals configured!</color>");

            // Capture and Export Image of Pit 1
            CapturePitRender();
        }

        private static void CapturePitRender()
        {
            GameObject camObj = new GameObject("Temp_PitHolo_Capture_Cam");
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.Skybox;
            cam.fieldOfView = 48f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;

            // Framed 3/4 perspective looking directly at the circular red pit with the in-pit floor numeral
            camObj.transform.position = new Vector3(0f, 2.2f, 0.4f);
            camObj.transform.LookAt(new Vector3(0f, -0.05f, 3.0f));

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
            Object.DestroyImmediate(camObj);

            byte[] bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            string outPath1 = Path.Combine(OutDir, "scratch", "current_pit_hologram.png");
            string outPath2 = Path.Combine(OutDir, "current_pit_hologram.png");
            File.WriteAllBytes(outPath1, bytes);
            File.WriteAllBytes(outPath2, bytes);

            Debug.Log("<color=#FFD700><b>[PIT SNAPSHOT EXPORTED]</b> Image saved to: " + outPath2 + "</color>");
        }
    }
}
#endif