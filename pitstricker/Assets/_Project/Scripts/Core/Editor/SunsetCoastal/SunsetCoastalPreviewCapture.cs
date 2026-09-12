#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Batch-mode preview screenshots of the finished Sunset Coastal scene from
    /// approximate Camera A (near tee) and Camera B (far pits) framings, for visual
    /// review outside the Editor. Never saves the scene — purely a read/render step.
    /// </summary>
    public static class SunsetCoastalPreviewCapture
    {
        private const string OutDir = "Logs/SunsetCoastalPreview";

        [MenuItem("Pit Striker/Sunset Coastal/6. Capture Preview Screenshots", false, 22)]
        public static void CapturePreviews()
        {
            var scene = EditorSceneManager.OpenScene(SunsetCoastalSceneSetup.Map02ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[SUNSET COASTAL PREVIEW] Could not open Map 02 scene.");
                return;
            }

            var camGo = GameObject.Find("Main Camera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null)
            {
                Debug.LogError("[SUNSET COASTAL PREVIEW] No Main Camera/Camera component found.");
                return;
            }

            Directory.CreateDirectory(OutDir);

            // A brand-new batch-mode process hasn't warmed up shader variants yet, so the
            // very first Camera.Render() call can show Unity's async-compile placeholder
            // (bright magenta) on materials that haven't rendered before — a capture-only
            // artifact, not a real material/shader bug. Force synchronous compilation and
            // render a few throwaway warm-up passes first.
            ShaderUtil.allowAsyncCompilation = false;
            var warmupRt = new RenderTexture(64, 64, 16, RenderTextureFormat.ARGB32);
            cam.targetTexture = warmupRt;
            for (int i = 0; i < 3; i++) cam.Render();
            cam.targetTexture = null;
            warmupRt.Release();
            Object.DestroyImmediate(warmupRt);

            Vector3 originalPos = cam.transform.position;
            Quaternion originalRot = cam.transform.rotation;

            RenderLookAt(cam, new Vector3(1.3f, 5.2f, -10.0f), new Vector3(1.3f, 0.3f, 9.0f), Path.Combine(OutDir, "CameraA_NearTee.png"));
            RenderLookAt(cam, new Vector3(1.7f, 6.4f, 17.0f), new Vector3(2.0f, 0.5f, 34.0f), Path.Combine(OutDir, "CameraB_FarPits.png"));

            cam.transform.position = originalPos;
            cam.transform.rotation = originalRot;

            Debug.Log($"<color=#00FF88><b>[SUNSET COASTAL PREVIEW]</b> Screenshots written to {Path.GetFullPath(OutDir)}</color>");
        }

        private static void RenderLookAt(Camera cam, Vector3 position, Vector3 lookAtPoint, string outPath)
        {
            Quaternion rot = Quaternion.LookRotation((lookAtPoint - position).normalized, Vector3.up);
            RenderFrom(cam, position, rot, outPath);
        }

        private static void RenderFrom(Camera cam, Vector3 position, Quaternion rotation, string outPath)
        {
            cam.transform.position = position;
            cam.transform.rotation = rotation;

            int width = 960, height = 1706; // Portrait mobile framing.
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            File.WriteAllBytes(outPath, tex.EncodeToPNG());

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(rt);
        }
    }
}
#endif
