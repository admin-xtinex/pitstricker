#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitStriker.EditorTools
{
    public static class VillageSceneRename
    {
        public const string OldPath = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        public const string NewPath = "Assets/_Project/Scenes/SC_Village.unity";

        public static string ActiveVillageScenePath()
        {
            if (File.Exists(NewPath)) return NewPath;
            if (File.Exists(OldPath)) return OldPath;
            return NewPath;
        }

        [MenuItem("Pit Striker/Rename Village Scene to SC_Village", false, 4)]
        public static void Rename()
        {
            if (File.Exists(NewPath) && !File.Exists(OldPath))
            {
                Debug.Log("[VILLAGE RENAME] Already using SC_Village.unity");
                EditorSceneManager.OpenScene(NewPath, OpenSceneMode.Single);
                return;
            }

            if (!File.Exists(OldPath))
            {
                Debug.LogError("[VILLAGE RENAME] Neither old nor new scene found.");
                return;
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogError("[VILLAGE RENAME] Stop Play Mode first.");
                return;
            }

            var active = EditorSceneManager.GetActiveScene();
            if (active.path == OldPath)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            string error = AssetDatabase.MoveAsset(OldPath, NewPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError("[VILLAGE RENAME] MoveAsset failed: " + error);
                return;
            }

            var scenes = EditorBuildSettings.scenes;
            bool found = false;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].path == OldPath)
                {
                    scenes[i].path = NewPath;
                    scenes[i].enabled = true;
                    found = true;
                }
            }
            if (!found)
            {
                var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
                {
                    new EditorBuildSettingsScene(NewPath, true)
                };
                scenes = list.ToArray();
            }
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(NewPath, OpenSceneMode.Single);
            Debug.Log("<color=#00FF88><b>[VILLAGE RENAME]</b> Scene is now SC_Village.unity. GUID preserved.</color>");
        }
    }
}
#endif
