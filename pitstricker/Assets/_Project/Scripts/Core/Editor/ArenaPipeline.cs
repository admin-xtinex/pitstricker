#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    public static class ArenaPipeline
    {
        const string ResultFile = "Library/ArenaPipeline.result";

        [MenuItem("Pit Striker/Run Arena Pipeline (Align + Village Dress)", false, 9)]
        public static void RunVillageAlign()
        {
            Run("village", () =>
            {
                ArenaAligner.Align();
                VillageConceptDress.Apply();
            });
        }

        [MenuItem("Pit Striker/Run Arena Pipeline (Create Beach Scene + Align + Beach Dress)", false, 10)]
        public static void RunBeachAlign()
        {
            Run("beach", () =>
            {
                BeachConceptDress.CreateBeachScene();
                ArenaAligner.Align();
            });
        }

        public static void RunVillageAlignAndBuildApk()
        {
            Run("village-apk", () =>
            {
                ArenaAligner.Align();
                VillageConceptDress.Apply();
                BuildAPK.BuildAndroidPlayer();
            });
        }

        static void Run(string label, Action body)
        {
            try
            {
                Directory.CreateDirectory("Library");
                body();
                File.WriteAllText(ResultFile, "PASS " + label + " " + DateTime.UtcNow.ToString("o"));
                Debug.Log("<color=#00FF88><b>[ARENA PIPELINE]</b> " + label + " finished.</color>");
                if (Application.isBatchMode)
                    EditorApplication.Exit(0);
            }
            catch (Exception ex)
            {
                try { File.WriteAllText(ResultFile, "FAIL " + label + "\n" + ex); } catch { }
                Debug.LogError("[ARENA PIPELINE] " + label + " failed: " + ex);
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
                throw;
            }
        }
    }
}
#endif
