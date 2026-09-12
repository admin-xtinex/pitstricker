#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Adds the Sunset Coastal scene to EditorBuildSettings.scenes without disturbing any
    /// existing entries. Deliberately does NOT reuse VillageSceneSetup.UpdateBuildSettings(),
    /// which wholesale-replaces the scenes array with just the Village scene.
    /// </summary>
    public static class SunsetCoastalBuildSettings
    {
        public static void AddSceneAdditive(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.Any(s => s.path == scenePath))
            {
                var existing = scenes.First(s => s.path == scenePath);
                existing.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
