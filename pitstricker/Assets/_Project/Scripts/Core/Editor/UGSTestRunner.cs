using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using PitStriker.Networking;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class UGSTestRunner
    {
        static UGSTestRunner()
        {
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (!File.Exists("Library/UGSTest.request") || EditorApplication.isCompiling) return;
            File.Delete("Library/UGSTest.request");
            TestAuth();
        }

        [MenuItem("Pit Striker/Test UGS Auth")]
        public static async void TestAuth()
        {
            try
            {
                Debug.Log("<color=#00FFAA>Testing UGS Auth & Relay...</color>");
                bool ok = await NetworkBootstrap.InitializeAndSignInAsync();
                Debug.Log($"<color=#00FFAA>UGS Auth Result: {ok}, PlayerId={NetworkBootstrap.PlayerId}</color>");
                if (ok)
                {
                    string code = await NetworkSessionManager.Instance.CreateRelayHostSessionAsync();
                    Debug.Log($"<color=#00FF88>Relay Join Code: {code}</color>");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UGS TEST EXCEPTION] {ex}");
            }
        }
    }
}
