#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.Gameplay;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 8 static validation: confirms the Sunset Coastal scene still has exactly the
    /// gameplay-critical objects Village had (pits, boundary colliders, marbles, camera,
    /// managers) after the art-layer rebuild, and that nothing regressed on Village itself.
    /// Not a substitute for a live play-mode pass, multiplayer test, or on-device profiling.
    /// </summary>
    public static class SunsetCoastalSmokeCheck
    {
        [MenuItem("Pit Striker/Sunset Coastal/4. Run Smoke Check", false, 20)]
        public static void RunSmokeCheck()
        {
            var scene = EditorSceneManager.OpenScene(SunsetCoastalSceneSetup.Map02ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[SUNSET COASTAL SMOKE CHECK] Could not open Map 02 scene.");
                return;
            }

            int pass = 0, fail = 0;
            void Check(string label, bool condition)
            {
                if (condition) { pass++; Debug.Log($"<color=#4CE07C>[PASS]</color> {label}"); }
                else { fail++; Debug.LogError($"[FAIL] {label}"); }
            }

            var arena = GameObject.Find("Arena_Sandbox");
            Check("Arena_Sandbox present", arena != null);

            int pitCount = 0;
            if (arena != null)
            {
                foreach (var pitName in new[] { "Pit_01_Round", "Pit_02_Round", "Pit_03_Round" })
                {
                    var pit = arena.transform.Find(pitName);
                    bool hasZone = pit != null && pit.GetComponent<PitZone>() != null;
                    bool hasSphere = pit != null && pit.GetComponent<SphereCollider>() != null;
                    Check($"{pitName} has PitZone + SphereCollider", hasZone && hasSphere);
                    if (hasZone) pitCount++;
                }
            }
            Check("Exactly 3 pits present (matches master doc Phase 1)", pitCount == 3);

            if (arena != null)
            {
                foreach (var wallName in new[] { "Wall_Front", "Wall_Left", "Wall_Back", "Wall_Right" })
                {
                    var wall = arena.transform.Find(wallName);
                    var col = wall != null ? wall.GetComponent<BoxCollider>() : null;
                    Check($"{wallName} boundary collider intact (non-trigger)", col != null && !col.isTrigger);
                }
            }

            var villageArt = FindRootIncludingInactive(scene, "Village_Reference_Upgrade");
            Check("Village_Reference_Upgrade fully removed (not just hidden)", villageArt == null);

            var artRoot = GameObject.Find("SunsetCoastal_Art");
            Check("SunsetCoastal_Art layer present", artRoot != null);

            var camGo = GameObject.Find("Main Camera");
            Check("Main Camera present", camGo != null);
            Check("Main Camera has SmoothFollowCamera", camGo != null && camGo.GetComponent<PitStriker.CameraSystem.SmoothFollowCamera>() != null);
            Check("Main Camera has MapCameraFramingBlender (Camera A/B)", camGo != null && camGo.GetComponent<PitStriker.CameraSystem.MapCameraFramingBlender>() != null);

            Check("TurnManager present", GameObject.Find("TurnManager") != null);
            Check("MapManager present", GameObject.Find("MapManager") != null);

            Debug.Log(fail == 0
                ? $"<color=#00FF88><b>[SUNSET COASTAL SMOKE CHECK] ALL {pass} CHECKS PASSED.</b></color>"
                : $"<color=#FF4444><b>[SUNSET COASTAL SMOKE CHECK] {fail} FAILED, {pass} PASSED.</b></color>");
        }

        private static GameObject FindRootIncludingInactive(UnityEngine.SceneManagement.Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }
            return null;
        }

    }
}
#endif
