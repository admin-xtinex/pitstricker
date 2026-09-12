#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using PitStriker.Networking;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class PlayModeTestRunner
    {
        private const string RequestFile = "Library/PlayModeTest.request";
        private const string ActiveFile = "Library/PlayModeTest.active";
        private const string ResultFile = "Library/PlayModeTest.result";

        static PlayModeTestRunner()
        {
            EditorApplication.update += CheckRequest;
        }

        private static void CheckRequest()
        {
            if (!File.Exists(RequestFile) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            File.Delete(RequestFile);
            File.WriteAllText(ActiveFile, "ACTIVE");
            File.WriteAllText(ResultFile, "STARTING_PLAY_MODE");
            Debug.Log("<color=#00FFAA><b>[PLAY MODE TEST]</b> Entering Play Mode to test UGS & Relay...</color>");
            EditorApplication.isPlaying = true;
        }
    }

    public class RuntimeTestHook : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            string activePath = "Library/PlayModeTest.active";
            if (!File.Exists(activePath)) return;
            File.Delete(activePath);

            GameObject go = new GameObject("__RuntimeTestHook");
            go.AddComponent<RuntimeTestHook>();
        }

        private async void Start()
        {
            string resultPath = "Library/PlayModeTest.result";
            try
            {
                Debug.Log("<color=#00FFAA><b>[RUNTIME TEST]</b> Starting UGS & Relay runtime diagnostics...</color>");
                bool authed = await NetworkBootstrap.InitializeAndSignInAsync();
                string status = $"AUTH_RESULT: {authed}, PlayerId: {NetworkBootstrap.PlayerId}";
                Debug.Log($"<color=#00FFAA><b>[RUNTIME TEST]</b> {status}</color>");

                string joinCode = null;
                if (authed)
                {
                    joinCode = await NetworkSessionManager.Instance.CreateRelayHostSessionAsync();
                    Debug.Log($"<color=#00FF88><b>[RUNTIME TEST]</b> RELAY_JOIN_CODE: {joinCode}</color>");
                }

                File.WriteAllText(resultPath, $"SUCCESS: authed={authed}, joinCode={joinCode}, playerId={NetworkBootstrap.PlayerId}");
            }
            catch (Exception ex)
            {
                File.WriteAllText(resultPath, $"EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                Debug.LogError($"[RUNTIME TEST EXCEPTION] {ex}");
            }
            finally
            {
                await Task.Delay(2000);
                EditorApplication.isPlaying = false;
            }
        }
    }
}
#endif
