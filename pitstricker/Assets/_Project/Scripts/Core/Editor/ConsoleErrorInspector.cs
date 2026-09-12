#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class ConsoleErrorInspector
    {
        private const string RequestFile = "Library/GetConsoleErrors.request";
        private const string OutputFile = "Library/ConsoleDump.log";
        private static readonly List<string> _recentErrors = new List<string>();
        private static bool _isDumping = false;

        static ConsoleErrorInspector()
        {
            Application.logMessageReceived += HandleLog;
            EditorApplication.update += CheckRequest;
        }

        private static void HandleLog(string condition, string stackTrace, LogType type)
        {
            if (_isDumping) return;
            if (condition != null && condition.Contains("ConsoleDump.log")) return;

            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                _recentErrors.Add($"[{DateTime.Now:HH:mm:ss}] [{type}] {condition}\n{stackTrace}\n-------------------");
                if (_recentErrors.Count > 50) _recentErrors.RemoveAt(0);
            }
        }

        private const string FixGraphicsFile = "Library/FixGraphicsAPI.request";
        private const string BuildApkFile = "Library/BuildAPK.request";
        private const string BuildWindowsFile = "Library/BuildWindows.request";

        private static void CheckRequest()
        {
            if (File.Exists(BuildWindowsFile) && !EditorApplication.isCompiling)
            {
                if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    EditorApplication.isPlaying = false;
                    return;
                }
                File.Delete(BuildWindowsFile);
                Debug.Log("<color=#00FFAA><b>[BUILD WINDOWS]</b> Triggering BuildWindows...</color>");
                BuildWindowsPlayer.BuildWindows();
                return;
            }



            if (File.Exists(FixGraphicsFile) && !EditorApplication.isCompiling)
            {
                File.Delete(FixGraphicsFile);
                FixGraphicsAPIMismatch();
                DumpAllLogsToFile();
            }

            if (!File.Exists(RequestFile) || EditorApplication.isCompiling) return;
            File.Delete(RequestFile);
            DumpAllLogsToFile();
        }

        [MenuItem("Pit Striker/Fix Graphics API Mismatch")]
        public static void FixGraphicsAPIMismatch()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, true);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, true);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=#00FF88>[FIX] Default Graphics APIs enabled for Windows and Android.</color>");
        }

        [MenuItem("Pit Striker/Dump Console Errors")]
        public static void DumpAllLogsToFile()
        {
            var lines = new List<string>();
            lines.Add($"=== UNITY CONSOLE DUMP AT {DateTime.Now} ===");
            lines.Add($"Editor State: isPlaying={EditorApplication.isPlaying}, isCompiling={EditorApplication.isCompiling}, isUpdating={EditorApplication.isUpdating}");

            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries, UnityEditor.dll");
                if (logEntriesType != null)
                {
                    var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    var getEntryInternalMethod = logEntriesType.GetMethod("GetEntryInternal", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    var logEntryType = Type.GetType("UnityEditor.LogEntry, UnityEditor.dll");

                    if (getCountMethod != null && getEntryInternalMethod != null && logEntryType != null)
                    {
                        int count = (int)getCountMethod.Invoke(null, null);
                        lines.Add($"Total Entries in Console: {count}");

                        object entryInstance = Activator.CreateInstance(logEntryType);
                        FieldInfo msgField = logEntryType.GetField("message", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        FieldInfo modeField = logEntryType.GetField("mode", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        int start = Math.Max(0, count - 40);
                        for (int i = start; i < count; i++)
                        {
                            getEntryInternalMethod.Invoke(null, new object[] { i, entryInstance });
                            string msg = msgField?.GetValue(entryInstance) as string;
                            int mode = modeField != null ? (int)modeField.GetValue(entryInstance) : 0;
                            lines.Add($"[Entry {i}] (Mode {mode}): {msg}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lines.Add($"Reflection extraction error: {ex.Message}");
            }

            if (_recentErrors.Count > 0)
            {
                lines.Add("\n=== RECENTLY CAPTURED RUNTIME ERRORS ===");
                lines.AddRange(_recentErrors);
            }

            File.WriteAllLines(OutputFile, lines);
        }
    }
}
#endif
