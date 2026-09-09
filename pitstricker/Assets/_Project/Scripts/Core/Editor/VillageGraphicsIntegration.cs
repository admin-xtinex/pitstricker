#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class VillageGraphicsIntegration
    {
        const string Folder = "Assets/_Project/Art/Environments/Village";
        const string Source = "Assets/_Project/Scenes/SC_Sandbox_Learning.unity";
        public const string Destination = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        const string Request = "Library/VillageGraphics.request";
        [Serializable] class Palette { public Entry[] materials; }
        [Serializable] class Entry { public string name; public float[] color; public float roughness; public float metallic; }

        static VillageGraphicsIntegration() { EditorApplication.update += CheckRequest; }
        static void CheckRequest()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.isPlayingOrWillChangePlaymode) return;
            File.Delete(Request);
            try { Integrate(); }
            catch (Exception e) { File.WriteAllText("Library/VillageGraphics.result", e.ToString()); Debug.LogException(e); }
        }

        [MenuItem("Pit Striker/Create Village Graphics Test Scene")]
        public static void Integrate()
        {
            AssetDatabase.Refresh();
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "/Village_Graphics.fbx");
            if (!prefab) throw new InvalidOperationException("Village graphics FBX is not imported yet.");
            var palette = JsonUtility.FromJson<Palette>(File.ReadAllText(Folder + "/palette.json"));
            var materials = new System.Collections.Generic.Dictionary<string, Material>();
            foreach (var entry in palette.materials)
            {
                string path = Folder + "/" + entry.name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (!material)
                {
                    var shader = Shader.Find("Universal Render Pipeline/Lit");
                    if (!shader) throw new InvalidOperationException("URP Lit shader is unavailable.");
                    material = new Material(shader);
                    material.SetColor("_BaseColor", new Color(entry.color[0], entry.color[1], entry.color[2], entry.color[3]));
                    material.SetFloat("_Smoothness", 1 - entry.roughness);
                    material.SetFloat("_Metallic", entry.metallic);
                    AssetDatabase.CreateAsset(material, path);
                }
                // Existing materials contain authored texture/shader settings.
                // The Blender palette is only a default for newly imported materials.
                materials.Add(entry.name, material);
            }
            if (!File.Exists(Destination) && !AssetDatabase.CopyAsset(Source, Destination)) throw new IOException("Could not copy gameplay scene.");
            var previous = SceneManager.GetActiveScene();
            var scene = SceneManager.GetSceneByPath(Destination);
            bool wasLoaded = scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(Destination, OpenSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var oldGraphics = scene.GetRootGameObjects().FirstOrDefault(r => r.name == "Environment_Blender_Village_Graphics");
                if (oldGraphics) UnityEngine.Object.DestroyImmediate(oldGraphics);
                var roots = scene.GetRootGameObjects();
                var before = GameplaySnapshot(roots);
                // Hide only the obsolete decorative renderers. Never deactivate
                // gameplay roots, change colliders, or invoke SandboxBuilder.
                foreach (var t in roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)))
                    if (t.name == "Environment_Village_Dressing" || t.name == "VillageMap_Visuals")
                        foreach (var renderer in t.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
                var graphics = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                graphics.name = "Environment_Blender_Village_Graphics";
                graphics.transform.rotation = Quaternion.Euler(0, 180, 0);
                if (graphics.GetComponentsInChildren<Collider>(true).Length != 0 || graphics.GetComponentsInChildren<MonoBehaviour>(true).Length != 0)
                    throw new InvalidOperationException("Graphics must not include physics or gameplay scripts.");
                foreach (var renderer in graphics.GetComponentsInChildren<Renderer>(true))
                    renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m && materials.TryGetValue(m.name, out var replacement) ? replacement : m).ToArray();
                if (before != GameplaySnapshot(roots)) throw new InvalidOperationException("Gameplay data changed during graphics integration.");
                foreach (var renderer in graphics.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterials.Any(m => !m || !m.shader || !m.shader.isSupported))
                        throw new InvalidOperationException("Missing or incompatible village material: " + renderer.name);
                VillageVisualUpgrade.Apply(scene, graphics);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                var camera = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Camera>(true)).FirstOrDefault(c => c.CompareTag("MainCamera"));
                if (camera) Capture(camera);
                File.WriteAllText("Library/VillageGraphics.result", "PASS: Updated " + Destination + "\nGameplay preserved; reference scenery, materials, lighting and decorative markers applied.\nGraphics renderers: " + graphics.GetComponentsInChildren<Renderer>(true).Length);
                Debug.Log("VILLAGE_GRAPHICS_INTEGRATION_OK");
            }
            finally { SceneManager.SetActiveScene(previous); if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }

        static string GameplaySnapshot(GameObject[] roots)
        {
            return string.Join("\n", roots.SelectMany(r => r.GetComponentsInChildren<Component>(true))
                .Where(c => c is MonoBehaviour || c is Collider || c is Rigidbody || c is Transform)
                .Select(c => c.GetType().FullName + ":" + EditorJsonUtility.ToJson(c)));
        }
        internal static void Capture(Camera camera)
        {
            var otherRenderers = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include)
                .Where(r => r.gameObject.scene != camera.gameObject.scene && !r.forceRenderingOff).ToArray();
            var oldTarget = camera.targetTexture;
            var oldActive = RenderTexture.active;
            var rt = new RenderTexture(1280, 720, 24);
            var texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                foreach (var renderer in otherRenderers) renderer.forceRenderingOff = true;
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                File.WriteAllBytes("Library/VillageGraphics.png", texture.EncodeToPNG());
            }
            finally
            {
                foreach (var renderer in otherRenderers) if (renderer) renderer.forceRenderingOff = false;
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
#endif
