#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class VillageGraphicsSmokeCheck
    {
        static double started;
        static SceneSetup[] previous;
        static string errors = "";
        static int phase;
        static bool batch;
        public static void PolishAndRunBatch()
        {
            VillageGraphicsIntegration.Integrate();
            RunBatch();
        }
        public static void RunBatch()
        {
            batch = true;
            File.WriteAllText("Library/VillageSmoke.request", "batch");
        }
        static VillageGraphicsSmokeCheck() { EditorApplication.update += Tick; }
        static void Log(string message, string trace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors += message + "\n";
        }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (phase == 0)
            {
                if (!File.Exists("Library/VillageSmoke.request") || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete("Library/VillageSmoke.request");
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).isDirty) { File.WriteAllText("Library/VillageSmoke.result", "Cannot run: open scene has unsaved changes."); return; }
                previous = EditorSceneManager.GetSceneManagerSetup();
                EditorSceneManager.OpenScene(VillageGraphicsIntegration.Destination);
                errors = "";
                Application.logMessageReceived += Log;
                phase = 1;
                EditorApplication.isPlaying = true;
            }
            else if (phase == 1 && EditorApplication.isPlaying)
            {
                started = EditorApplication.timeSinceStartup;
                phase = 2;
            }
            else if (phase == 2 && EditorApplication.timeSinceStartup - started > 8)
            {
                var marbles = UnityEngine.Object.FindObjectsByType<PitStriker.Physics.MarbleController>(FindObjectsInactive.Include);
                var pits = UnityEngine.Object.FindObjectsByType<PitStriker.Gameplay.PitZone>(FindObjectsInactive.Include);
                if (marbles.Length != 4 || pits.Length != 3) errors += "Expected four marbles and three pits.\n";
                if (marbles.Any(m => m.transform.position.y < -2)) errors += "Marble fell below arena.\n";
                var pitMeshes = string.Join("\n", pits.Select(p => p.name + " mesh=" +
                    (p.GetComponent<MeshFilter>().sharedMesh ? p.GetComponent<MeshFilter>().sharedMesh.vertexCount.ToString() : "MISSING") +
                    " collider=" + (p.GetComponent<MeshCollider>().sharedMesh ? "present" : "MISSING")));
                foreach (var name in new[] { "Pit Striker/Village Ground", "Pit Striker/Swirl Marble", "Pit Striker/Village Foliage", "Pit Striker/Village Sky", "Pit Striker/Village Surface" })
                {
                    var shader = Shader.Find(name);
                    if (!shader || !shader.isSupported) errors += "Unsupported shader: " + name + "\n";
                    else foreach (var message in ShaderUtil.GetShaderMessages(shader))
                        if (message.severity.ToString() == "Error") errors += message.message + "\n";
                }
                try { VillageVisualUpgrade.Validate(SceneManager.GetActiveScene()); }
                catch (Exception e) { errors += e.Message + "\n"; }
                if (Camera.main) VillageGraphicsIntegration.Capture(Camera.main);
                try { File.WriteAllText("Library/MarbleCollisionChecks.result", MarbleCollisionChecks.Run()); }
                catch(Exception e) { errors+=e.Message+"\n"; File.WriteAllText("Library/MarbleCollisionChecks.result",e.ToString()); }
                File.WriteAllText("Library/VillageSmoke.result", (errors.Length == 0 ? "PASS" : "FAIL\n" + errors) + "\n8 second Play Mode smoke check. Marbles=" + marbles.Length + "; pits=" + pits.Length + "\n" + pitMeshes);
                Application.logMessageReceived -= Log;
                phase = 3;
                EditorApplication.isPlaying = false;
            }
            else if (phase == 3 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                phase = 0;
                if (batch) EditorApplication.Exit(errors.Length == 0 ? 0 : 1);
                else EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }
    }
}
#endif
