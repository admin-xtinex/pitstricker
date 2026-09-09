#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using PitStriker.UI;
using PitStriker.Gameplay;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class MenuFlowChecks
    {
        const string Flag = "Library/MenuFlowChecks.running";
        static int step;
        static double next;
        static string failure;
        static MenuFlowChecks() { EditorApplication.update += Tick; }
        public static void Run()
        {
            EditorSceneManager.OpenScene(VillageGraphicsIntegration.Destination);
            File.WriteAllText(Flag,"run");
            EditorApplication.isPlaying = true;
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static Button Button(string name)
        {
            var button = UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).FirstOrDefault(b=>b.name==name);
            Check(button && button.interactable, "Missing active button: "+name);
            return button;
        }
        static void Click(string name)
        {
            Canvas.ForceUpdateCanvases();
            var button = Button(name);
            var rect = button.GetComponent<RectTransform>();
            var point = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
            var events = UnityEngine.EventSystems.EventSystem.current;
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position=point },hits);
            Check(hits.Count>0 && (hits[0].gameObject==button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform)), "Button blocked by another UI layer: "+name+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
            button.onClick.Invoke();
        }
        static void Tick()
        {
            if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            if (!EditorApplication.isPlaying) {
                if (step == 99) { File.Delete(Flag); EditorApplication.Exit(failure==null ? 0:1); }
                return;
            }
            next=EditorApplication.timeSinceStartup+.65;
            try
            {
                var menu=MenuManager.Instance; var tm=TurnManager.Instance;
                if (!menu || !tm) return;
                switch(step++)
                {
                    case 0:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Home,"Home screen startup");
                        Capture("Home"); Click("Btn_Play"); break;
                    case 1:
                        Click("Btn_4P"); Capture("PlayerSetup"); Click("Btn_StartMatch"); break;
                    case 2:
                        Check(tm.PlayerCount==4,"Four-player configuration failed");
                        Check(menu.CurrentScreen==MenuManager.ScreenType.InGame,"Start match navigation");
                        Click("Btn_HUD_Pause"); break;
                    case 3:
                        Check(tm.CurrentState==TurnManager.GameState.Paused && Time.timeScale==0,"Pause did not freeze match");
                        Capture("Pause"); Click("Btn_PauseRules"); break;
                    case 4:
                        Check(Time.timeScale==0,"Rules resumed paused simulation");
                        menu.NavigateBack(); break;
                    case 5: Click("Btn_Resume"); break;
                    case 6:
                        Check(Time.timeScale==1 && tm.CurrentState!=TurnManager.GameState.Paused,"Resume failed");
                        menu.NavigateBack(); break;
                    case 7: Click("Btn_Restart"); break;
                    case 8: Click("Btn_Cancel"); break;
                    case 9:
                        Check(Time.timeScale==0,"Cancel restart resumed match");
                        Click("Btn_Restart"); break;
                    case 10: Click("Btn_Confirm"); break;
                    case 11:
                        Check(tm.PlayerCount==4 && Time.timeScale==1,"Restart lost players or stayed paused");
                        menu.NavigateBack(); break;
                    case 12: Click("Btn_HomeMenu"); break;
                    case 13: Click("Btn_Confirm"); break;
                    case 14:
                        Check(tm.CurrentState==TurnManager.GameState.Menu,"Main menu did not end match");
                        Click("Btn_Play"); break;
                    case 15: Click("Btn_2P"); Click("Btn_StartMatch"); break;
                    case 16:
                        Check(tm.PlayerCount==2,"Two-player configuration failed after four-player match");
                        Capture("InGame");
                        File.WriteAllText("Library/MenuFlowChecks.result","PASS: home, 4/2 players, pause, rules/back, resume, cancel/confirm restart, return to menu, restart with retained roster.");
                        step=99; EditorApplication.isPlaying=false; break;
                }
            }
            catch(Exception e)
            {
                failure=e.ToString(); File.WriteAllText("Library/MenuFlowChecks.result",failure);
                Debug.LogException(e); step=99; EditorApplication.isPlaying=false;
            }
        }
        static void Capture(string name)
        {
            var camera=Camera.main;
            var canvases=UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude).Where(c=>c.isRootCanvas).ToArray();
            var modes=canvases.Select(c=>c.renderMode).ToArray();
            var cameras=canvases.Select(c=>c.worldCamera).ToArray();
            var distances=canvases.Select(c=>c.planeDistance).ToArray();
            var oldTarget=camera.targetTexture; var oldActive=RenderTexture.active;
            var rt=new RenderTexture(1280,720,24); var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                camera.targetTexture=rt;
                foreach(var c in canvases) { c.renderMode=RenderMode.ScreenSpaceCamera; c.worldCamera=camera; c.planeDistance=1; }
                Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active=rt;
                texture.ReadPixels(new Rect(0,0,1280,720),0,0); texture.Apply();
                Directory.CreateDirectory("Library/UIPreviews"); File.WriteAllBytes("Library/UIPreviews/"+name+".png",texture.EncodeToPNG());
            }
            finally
            {
                for(int i=0;i<canvases.Length;i++) { canvases[i].renderMode=modes[i]; canvases[i].worldCamera=cameras[i]; canvases[i].planeDistance=distances[i]; }
                camera.targetTexture=oldTarget; RenderTexture.active=oldActive;
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
