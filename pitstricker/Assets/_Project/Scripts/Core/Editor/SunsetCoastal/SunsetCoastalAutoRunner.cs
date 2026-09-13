#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class SunsetCoastalAutoRunner
    {
        private const string RunFlagKey = "SunsetCoastal_NeedsRun_AlignGameplay_v1";

        static SunsetCoastalAutoRunner()
        {
            EditorApplication.delayCall += CheckAndRun;
        }

        [MenuItem("Pit Striker/Sunset Coastal/Force Align Gameplay Now", false, 1)]
        public static void ForceRun()
        {
            SessionState.SetBool(RunFlagKey, true);
            CheckAndRun();
        }

        private static void CheckAndRun()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!SessionState.GetBool(RunFlagKey, true)) return;

            SessionState.SetBool(RunFlagKey, false);
            Debug.Log("<color=#00FFFF>[AUTO RUNNER] Automatically running SunsetCoastalPhase1Builder.BuildPhase1() to align gameplay with environment...</color>");
            SunsetCoastalPhase1Builder.BuildPhase1();
        }
    }
}
#endif
