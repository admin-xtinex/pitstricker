#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace PitStriker.EditorTools
{
    public static class VillageReferencePolish
    {
        const string Folder = "Assets/_Project/Art/Environments/Village/";
        static Material Mat(string name, string shader, Color color, float smoothness=.2f)
        {
            if(shader=="Universal Render Pipeline/Lit") shader="Pit Striker/Village Surface";
            string path=Folder+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m=new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m,path); }
            m.shader=Shader.Find(shader); m.SetColor("_BaseColor",color); m.SetFloat("_Smoothness",smoothness);
            EditorUtility.SetDirty(m); return m;
        }
        static void Texture(Material m, string slot, string path)
        {
            var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(t) m.SetTexture(slot,t);
        }
        static GameObject Cube(string name, Transform parent, Vector3 pos, Vector3 scale, Material m)
        {
            var o=GameObject.CreatePrimitive(PrimitiveType.Cube); o.name=name;
            o.transform.SetParent(parent); o.transform.position=pos; o.transform.rotation=Quaternion.identity; o.transform.localScale=scale;
            UnityEngine.Object.DestroyImmediate(o.GetComponent<Collider>()); o.GetComponent<Renderer>().sharedMaterial=m; return o;
        }
        static void Label(string name, string value, Transform parent, Vector3 pos, float size, Color? color=null)
        {
            var o=new GameObject(name); o.transform.SetParent(parent); o.transform.position=pos; o.transform.rotation=Quaternion.identity;
            var text=o.AddComponent<TextMesh>(); text.text=value; text.fontSize=64; text.characterSize=size*.15f;
            text.anchor=TextAnchor.MiddleCenter; text.alignment=TextAlignment.Center; text.color=color ?? new Color(1,.92f,.72f); text.fontStyle=FontStyle.Bold;
        }
        public static void Apply(Scene scene, GameObject graphics)
        {
            System.IO.File.WriteAllText("Logs/PolishDebug.txt", "1. Polish started\n");
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            // All legacy chalk meshes belong to the decorative gameplay kit.
            // Keep trajectory renderers driven by input; remove only static rings.
            foreach(var t in all)
                if(t.name=="Gameplay_Kit_Village" || t.name.Contains("Chalk") || t.name.StartsWith("LaunchRing"))
                    foreach(var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            RenderSettings.ambientSkyColor=new Color(.58f,.70f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.52f,.62f,.42f);
            RenderSettings.ambientGroundColor=new Color(.35f,.28f,.18f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.72f,.82f,.88f); RenderSettings.fogDensity=.0038f;
            var sky=Mat("Reference_Sky","Pit Striker/Village Sky",Color.white);
            RenderSettings.skybox=sky;
            foreach(var light in all.Select(t=>t.GetComponent<Light>()).Where(l=>l&&l.type==LightType.Directional))
            {
                light.color=new Color(1,.94f,.80f); light.intensity=2.5f; light.shadows=LightShadows.Soft;
                light.shadowStrength=.85f; light.shadowBias=.02f; light.shadowNormalBias=.12f;
                light.transform.rotation=Quaternion.Euler(33,-48,0); RenderSettings.sun=light;
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"Reference_Grade.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Folder+"Reference_Grade.asset");}
            if(!profile.TryGet<Tonemapping>(out var tone)) {tone=profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var grade)) {grade=profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(grade,profile);}
            grade.postExposure.Override(.35f); grade.contrast.Override(12); grade.saturation.Override(10);
            if(!profile.TryGet<Bloom>(out var bloom)) {bloom=profile.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.28f); bloom.threshold.Override(1.15f); bloom.scatter.Override(.7f);
            if(!profile.TryGet<Vignette>(out var vig)) {vig=profile.Add<Vignette>(); AssetDatabase.AddObjectToAsset(vig,profile);}
            vig.intensity.Override(.18f); vig.smoothness.Override(.42f);
            EditorUtility.SetDirty(profile);
            var volume = graphics.GetComponent<Volume>();
            if (!volume) volume = graphics.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 10; volume.sharedProfile = profile;

            // 1. Setup Stylized Character Mascot ("Pip" the Striker Caddy)
            var mascotObj = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Mascot_Pip");
            if (!mascotObj)
            {
                mascotObj = new GameObject("Mascot_Pip");
                if (mascotObj.scene != scene) SceneManager.MoveGameObjectToScene(mascotObj, scene);
            }
            var mascot = mascotObj.GetComponent<PitStriker.Visuals.StylizedCharacterMascot>();
            if (!mascot) mascot = mascotObj.AddComponent<PitStriker.Visuals.StylizedCharacterMascot>();
            mascot.InitInEditor();
            mascotObj.transform.position = new Vector3(3.6f, 0.05f, -3.8f);
            mascotObj.transform.rotation = Quaternion.Euler(0, -75f, 0);
            System.IO.File.AppendAllText("Logs/PolishDebug.txt", "2. Mascot placed at " + mascotObj.transform.position + "\n");

            // Clean any obsolete Comic_Star objects from the scene
            foreach (var s in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray())
            {
                if (s != null && s.name.StartsWith("Comic_Star"))
                {
                    UnityEngine.Object.DestroyImmediate(s.gameObject);
                }
            }

            // 1. Unconditionally destroy all obsolete standalone StylizedPitRim objects in the scene
            System.IO.File.AppendAllText("Logs/PolishDebug.txt", "Roots count: " + scene.GetRootGameObjects().Length + "\n");
            foreach (var rootObj in scene.GetRootGameObjects().ToArray())
            {
                if (rootObj != null && rootObj.name.StartsWith("StylizedPitRim"))
                {
                    System.IO.File.AppendAllText("Logs/PolishDebug.txt", "Destroying rootObj: " + rootObj.name + "\n");
                    UnityEngine.Object.DestroyImmediate(rootObj);
                }
            }

            // 2. Locate the 3 Pits by name or PitZone
            GameObject[] pitObjs = new GameObject[]
            {
                GameObject.Find("Pit_01_Round"),
                GameObject.Find("Pit_02_Round"),
                GameObject.Find("Pit_03_Round")
            };

            Vector3[] fallbackPitPositions = new Vector3[]
            {
                new Vector3(0f, 0f, 3.0f),
                new Vector3(0f, 0f, 16.5f),
                new Vector3(0f, 0f, 31.0f)
            };

            for (int i = 0; i < 3; i++)
            {
                int pitNum = i + 1;
                GameObject pitObj = pitObjs[i];
                Vector3 pitPos = pitObj != null ? pitObj.transform.position : fallbackPitPositions[i];

                // Setup Animated Numbered Flag
                string flagName = "StylizedFlag_Pit" + pitNum;
                var flagObj = scene.GetRootGameObjects().FirstOrDefault(g => g.name == flagName);
                if (!flagObj)
                {
                    flagObj = new GameObject(flagName);
                    if (flagObj.scene != scene) SceneManager.MoveGameObjectToScene(flagObj, scene);
                }
                var anim = flagObj.GetComponent<PitStriker.Visuals.StylizedFlagAnimation>();
                if (!anim) anim = flagObj.AddComponent<PitStriker.Visuals.StylizedFlagAnimation>();
                var so = new SerializedObject(anim);
                so.FindProperty("_pitNumber").intValue = pitNum;
                Color pitFlagColor = pitNum == 1 ? new Color(0.95f, 0.22f, 0.22f) :
                                    (pitNum == 2 ? new Color(0.20f, 0.85f, 0.40f) : new Color(0.98f, 0.75f, 0.15f));
                so.FindProperty("_flagColor").colorValue = pitFlagColor;
                so.ApplyModifiedPropertiesWithoutUndo();
                anim.InitInEditor();
                flagObj.transform.position = pitPos + (pitNum % 2 == 1 ? new Vector3(-0.85f, 0f, 0f) : new Vector3(0.85f, 0f, 0f));

                if (pitObj != null)
                {
                    // Clean any solid puck or badge children from the pit
                    Transform solidRim = pitObj.transform.Find("Stylized_Ceramic_Rim");
                    if (solidRim != null) UnityEngine.Object.DestroyImmediate(solidRim.gameObject);
                    Transform badge = pitObj.transform.Find("PitNumber_Badge");
                    if (badge != null) UnityEngine.Object.DestroyImmediate(badge.gameObject);

                    // Ensure StylizedPitFeedback is on the pit for capture pulse
                    var feedback = pitObj.GetComponent<PitStriker.Visuals.StylizedPitFeedback>();
                    if (!feedback) feedback = pitObj.AddComponent<PitStriker.Visuals.StylizedPitFeedback>();
                    feedback.PitNumber = pitNum;
                    feedback.InitInEditor();

                    // Style the open 3D BrightRed_PitLip ring mesh with vibrant cartoon ceramic material
                    Transform lipTrans = pitObj.transform.Find("BrightRed_PitLip");
                    if (lipTrans != null)
                    {
                        MeshRenderer lipMr = lipTrans.GetComponent<MeshRenderer>();
                        if (lipMr != null)
                        {
                            Shader toonShader = Shader.Find("Universal Render Pipeline/Lit");
                            if (toonShader == null) toonShader = Shader.Find("Standard");
                            Material lipMat = new Material(toonShader);
                            lipMat.name = "M_PitLip_Stylized_" + pitNum;
                            Color lipColor = pitNum == 1 ? new Color(0.92f, 0.22f, 0.20f) :
                                            (pitNum == 2 ? new Color(0.20f, 0.85f, 0.38f) : new Color(0.98f, 0.72f, 0.15f));
                            lipMat.SetColor("_BaseColor", lipColor);
                            lipMat.SetColor("_Color", lipColor);
                            lipMat.SetFloat("_Smoothness", 0.75f);
                            lipMat.EnableKeyword("_EMISSION");
                            lipMat.SetColor("_EmissionColor", lipColor * 0.25f);
                            lipMr.sharedMaterial = lipMat;
                        }
                    }
                }
            }

            // 3. Ensure Stylized VFX Manager is Present
            if (UnityEngine.Object.FindAnyObjectByType<PitStriker.VFX.StylizedVFX>() == null)
            {
                var vfxObj = new GameObject("Stylized_VFX_Manager");
                if (vfxObj.scene != scene) SceneManager.MoveGameObjectToScene(vfxObj, scene);
                vfxObj.AddComponent<PitStriker.VFX.StylizedVFX>();
            }

            var cam = Camera.main ?? UnityEngine.Object.FindAnyObjectByType<Camera>();
            var marbles = UnityEngine.Object.FindObjectsByType<PitStriker.Physics.MarbleController>(FindObjectsSortMode.None);
            if (cam)
            {
                cam.GetUniversalAdditionalCameraData().renderPostProcessing = true; cam.allowHDR = true;
                cam.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                // Match the existing runtime follow framing for an honest editor preview.
                var follow = cam.GetComponent<PitStriker.CameraSystem.SmoothFollowCamera>();
                if (follow && marbles.Length > 0)
                {
                    var so = new SerializedObject(follow); var p = marbles[0].transform.position;
                    cam.transform.position = p + new Vector3(0, so.FindProperty("_height").floatValue, -so.FindProperty("_distance").floatValue);
                    cam.transform.LookAt(p + new Vector3(0, so.FindProperty("_lookAtHeightOffset").floatValue, 1.5f));
                }
            }
            DynamicGI.UpdateEnvironment();
            System.IO.File.AppendAllText("Logs/PolishDebug.txt", "3. Polish complete!\n");
        }
    }
}
#endif
