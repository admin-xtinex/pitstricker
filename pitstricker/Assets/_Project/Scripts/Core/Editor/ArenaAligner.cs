#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.Core;
using PitStriker.Gameplay;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Snaps live pit objects, graphics roots, and launch fallback to ArenaFrame.
    /// </summary>
    public static class ArenaAligner
    {
        static readonly string[] PitNames = { "Pit_01_Round", "Pit_02_Round", "Pit_03_Round" };
        static readonly string[] GraphicsRoots =
        {
            "Environment_Blender_Village_Graphics",
            "Environment_Blender_Beach_Graphics",
            "Environment_Concept_Dress",
            "Environment_Concept_Beach_Dress"
        };

        [MenuItem("Pit Striker/Align Arena To Canonical Frame (0 / 12 / 24)", false, 8)]
        public static void Align()
        {
            string scenePath = VillageSceneRename.ActiveVillageScenePath();
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            int moved = 0;
            for (int i = 0; i < PitNames.Length; i++)
            {
                var pit = GameObject.Find(PitNames[i]);
                if (!pit)
                {
                    var zones = Object.FindObjectsByType<PitZone>(FindObjectsInactive.Exclude);
                    foreach (var z in zones)
                    {
                        if (z.PitNumber == i + 1) { pit = z.gameObject; break; }
                    }
                }
                if (!pit) continue;

                pit.transform.position = ArenaFrame.PitPosition(i + 1);
                pit.transform.rotation = Quaternion.identity;
                var zone = pit.GetComponent<PitZone>();
                if (zone != null) zone.SetPitNumber(i + 1);
                EditorUtility.SetDirty(pit);
                moved++;
            }

            foreach (var name in GraphicsRoots)
            {
                var go = GameObject.Find(name);
                if (!go) continue;
                if (name.StartsWith("Environment_Blender_"))
                {
                    go.transform.position = Vector3.zero;
                    go.transform.rotation = Quaternion.Euler(0f, ArenaFrame.GraphicsRootEulerY, 0f);
                }
                EditorUtility.SetDirty(go);
            }

            var turns = Object.FindObjectsByType<TurnManager>(FindObjectsInactive.Exclude);
            if (turns != null && turns.Length > 0)
            {
                var so = new SerializedObject(turns[0]);
                var start = so.FindProperty("_startCenter");
                if (start != null)
                {
                    start.vector3Value = ArenaFrame.LaunchPosition(0.3f);
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(turns[0]);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("<color=#00FF88><b>[ARENA ALIGN]</b> Snapped " + moved + " pits to Z=0/12/24. Launch Z=" + ArenaFrame.LaunchZ + ". Rebuild pits if ground holes still sit on 3/16.5/31.</color>");
        }
    }
}
#endif
