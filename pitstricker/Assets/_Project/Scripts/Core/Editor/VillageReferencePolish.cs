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
            var all=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
            // All legacy chalk meshes belong to the decorative gameplay kit.
            // Keep trajectory renderers driven by input; remove only static rings.
            foreach(var t in all)
                if(t.name=="Gameplay_Kit_Village" || t.name.Contains("Chalk") || t.name.StartsWith("LaunchRing"))
                    foreach(var r in t.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            RenderSettings.ambientSkyColor=new Color(.62f,.72f,.83f);
            RenderSettings.ambientEquatorColor=new Color(.55f,.57f,.39f);
            RenderSettings.ambientGroundColor=new Color(.32f,.25f,.15f);
            RenderSettings.ambientIntensity=1;
            RenderSettings.fog=true; RenderSettings.fogMode=FogMode.ExponentialSquared;
            RenderSettings.fogColor=new Color(.65f,.73f,.76f); RenderSettings.fogDensity=.011f;
            var sky=Mat("Reference_Sky","Pit Striker/Village Sky",Color.white);
            RenderSettings.skybox=sky;
            foreach(var light in all.Select(t=>t.GetComponent<Light>()).Where(l=>l&&l.type==LightType.Directional))
            {
                light.color=new Color(1,.91f,.74f); light.intensity=2.5f; light.shadows=LightShadows.Soft;
                light.shadowStrength=.85f; light.shadowBias=.025f; light.shadowNormalBias=.15f;
                light.transform.rotation=Quaternion.Euler(33,-48,0); RenderSettings.sun=light;
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(Folder+"Reference_Grade.asset");
            if(!profile){profile=ScriptableObject.CreateInstance<VolumeProfile>(); AssetDatabase.CreateAsset(profile,Folder+"Reference_Grade.asset");}
            if(!profile.TryGet<Tonemapping>(out var tone)) {tone=profile.Add<Tonemapping>(); AssetDatabase.AddObjectToAsset(tone,profile);}
            tone.mode.Override(TonemappingMode.ACES);
            if(!profile.TryGet<ColorAdjustments>(out var grade)) {grade=profile.Add<ColorAdjustments>(); AssetDatabase.AddObjectToAsset(grade,profile);}
            grade.postExposure.Override(.45f); grade.contrast.Override(6); grade.saturation.Override(4);
            if(!profile.TryGet<Bloom>(out var bloom)) {bloom=profile.Add<Bloom>(); AssetDatabase.AddObjectToAsset(bloom,profile);}
            bloom.intensity.Override(.12f); bloom.threshold.Override(1.3f);
            EditorUtility.SetDirty(profile);
            var volume = graphics.GetComponent<Volume>();
            if (!volume) volume = graphics.AddComponent<Volume>();
            volume.isGlobal = true; volume.priority = 10; volume.sharedProfile = profile;
            var cam = all.Select(t => t.GetComponent<Camera>()).FirstOrDefault(c => c && c.CompareTag("MainCamera"));
            var marbles = all.Select(t => t.GetComponent<PitStriker.Physics.MarbleController>()).Where(m => m != null).ToArray();
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
        }
    }
}
#endif
