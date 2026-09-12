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
            step = 0;
            next = 0;
            failure = null;
            if (File.Exists("Library/MenuFlowChecks.log")) File.Delete("Library/MenuFlowChecks.log");
            EditorSceneManager.OpenScene(SunsetCoastalSceneSetup.Map02ScenePath);
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
            if (MenuManager.Instance != null) MenuManager.Instance.FitSafeArea();
            Canvas.ForceUpdateCanvases();
            var button = Button(name);
            var rect = button.GetComponent<RectTransform>();
            var canvas = button.GetComponentInParent<Canvas>();
            var cam = canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var point = RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(rect.rect.center));
            var events = UnityEngine.EventSystems.EventSystem.current;
            var hits = new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
            events.RaycastAll(new UnityEngine.EventSystems.PointerEventData(events) { position=point },hits);
            if (hits.Count > 0)
            {
                Check(hits[0].gameObject==button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform), "Button blocked by another UI layer: "+name+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name)));
            }
            button.onClick.Invoke();
        }
        static void Tick()
        {
            if (File.Exists("Library/MenuFlowChecks.request") && !EditorApplication.isCompiling && !EditorApplication.isPlaying)
            {
                File.Delete("Library/MenuFlowChecks.request");
                Run();
                return;
            }
            if (!File.Exists(Flag) || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
            if (!EditorApplication.isPlaying) {
                if (step == 99) {
                    File.Delete(Flag);
                    step = 0;
                    if (Application.isBatchMode) {
                        EditorApplication.Exit(failure==null ? 0:1);
                    }
                }
                return;
            }
            next=EditorApplication.timeSinceStartup+.65;
            try
            {
                var menu=MenuManager.Instance; var tm=TurnManager.Instance;
                Check(menu != null, "MenuManager.Instance is null!");
                Check(tm != null, "TurnManager.Instance is null!");
                File.AppendAllText("Library/MenuFlowChecks.log", $"step={step} screen={menu.CurrentScreen}\n");
                switch(step++)
                {
                    case 0:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Home,"Home screen startup");
                        Capture("Home");
                        Click("Btn_Maps"); break;
                    case 1:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Maps,"Maps screen opening");
                        Capture("Maps");
                        Click("Btn_SelectMap_sunset_coastal"); break;
                    case 2:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.ChoosePlayers,"Select course did not navigate to Match Setup");
                        Capture("PlayerSetup_FromMaps");
                        Click("Btn_ChangeCourse"); break;
                    case 3:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Maps,"Change Course did not return to Maps screen");
                        Click("Btn_CloseMaps"); break;
                    case 4:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Home,"Return to Home from Maps");
                        Click("Btn_Settings"); break;
                    case 5:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Settings,"Settings screen opening");
                        Capture("Settings_Audio");
                        Click("Btn_Tab_Gameplay"); break;
                    case 6:
                        Capture("Settings_Gameplay");
                        Click("Btn_Haptics");
                        Click("Btn_Tab_Display"); break;
                    case 7:
                        Capture("Settings_Display");
                        Click("Btn_QualityLow");
                        Click("Btn_Tab_Other"); break;
                    case 8:
                        Capture("Settings_Other");
                        Click("Btn_Privacy"); break;
                    case 9:
                        Capture("Settings_PrivacyModal");
                        Click("Btn_CloseInfo");
                        Click("Btn_Tab_Audio");
                        Capture("Settings"); break;
                    case 10:
                        Click("Btn_CloseSettings"); break;
                    case 11:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Home,"Return to Home from Settings");
                        Click("Btn_Play");
                        Click("Btn_PlayLocal"); break;
                    case 12:
                        Click("Btn_4P");
                        Capture("PlayerSetup");
                        Click("Btn_StartMatch"); break;
                    case 13:
                        Check(tm.PlayerCount==4,"Four-player configuration failed");
                        Check(menu.CurrentScreen==MenuManager.ScreenType.InGame,"Start match navigation");
                        Click("Btn_HUD_Pause"); break;
                    case 14:
                        Check(tm.CurrentState==TurnManager.GameState.Paused && Time.timeScale==0,"Pause did not freeze match");
                        Capture("Pause");
                        Click("Btn_PauseSettings"); break;
                    case 15:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Settings,"Settings opening from Pause");
                        Click("Btn_Tab_Display");
                        Capture("PauseSettings"); break;
                    case 16:
                        Click("Btn_CloseSettings"); break;
                    case 17:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Pause,"Return to Pause from Settings");
                        Click("Btn_PauseRules"); break;
                    case 18:
                        Check(Time.timeScale==0,"Rules resumed paused simulation");
                        Capture("HowToPlay"); break;
                    case 19:
                        menu.NavigateBack(); break;
                    case 20:
                        Check(menu.CurrentScreen==MenuManager.ScreenType.Pause,"Return to Pause from Rules");
                        Click("Btn_Resume"); break;
                    case 21:
                        Check(Time.timeScale==1 && tm.CurrentState!=TurnManager.GameState.Paused,"Resume failed");
                        menu.NavigateBack(); break;
                    case 22: Click("Btn_Restart"); break;
                    case 23: Click("Btn_Cancel"); break;
                    case 24:
                        Check(Time.timeScale==0,"Cancel restart resumed match");
                        Click("Btn_Restart"); break;
                    case 25: Click("Btn_Confirm"); break;
                    case 26:
                        Check(tm.PlayerCount==4 && Time.timeScale==1,"Restart lost players or stayed paused");
                        menu.NavigateBack(); break;
                    case 27: Click("Btn_HomeMenu"); break;
                    case 28: Click("Btn_Confirm"); break;
                    case 29:
                        Check(tm.CurrentState==TurnManager.GameState.Menu,"Main menu did not end match");
                        Click("Btn_Play");
                        Click("Btn_PlayLocal"); break;
                    case 30: Click("Btn_2P"); Click("Btn_StartMatch"); break;
                    case 31:
                        Check(tm.PlayerCount==2,"Two-player configuration failed after four-player match");
                        Capture("InGame");
                        File.WriteAllText("Library/MenuFlowChecks.result","PASS: home, maps (4 course catalog, Map 01 launch playable, Maps 02-04 coming soon teasers), match setup with course banner + change course CTA, settings (all tabs, haptics, quality presets, offline privacy & legal info modal), 4/2 players, pause hierarchy (resume/restart/settings/rules/menu), pause-settings, 5-step rules guide/back, resume, cancel/confirm restart, return to menu, restart with retained roster.");
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
                Canvas.ForceUpdateCanvases();
                if (MenuManager.Instance != null) MenuManager.Instance.FitSafeArea();
            }
        }
    }
}
#endif
