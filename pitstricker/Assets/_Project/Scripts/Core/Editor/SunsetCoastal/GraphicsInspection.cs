#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitStriker.EditorTools
{
    public static class GraphicsInspection
    {
        public static void Run()
        {
            Directory.CreateDirectory("Logs/GraphicsInspection");
            ShaderUtil.allowAsyncCompilation = false;
            foreach (string name in new[] { "BeachShack_Hero_01", "BoundaryLog_Module_01", "SmallFishingBoat_01", "WoodenPost_Rope_01", "HeroCoastalRock_Set_01" })
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Models/SunsetCoastal/Heroes/" + name + ".fbx"));
                var renderers = model.GetComponentsInChildren<Renderer>();
                var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Debug.Log("MODEL AUDIT " + name + " bounds=" + bounds + " meshes=" + model.GetComponentsInChildren<MeshFilter>().Length + " vertices=" + model.GetComponentsInChildren<MeshFilter>().Sum(f => f.sharedMesh.vertexCount));
                var light = new GameObject("Key").AddComponent<Light>();
                light.type = LightType.Directional; light.intensity = 2f;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = Color.gray;
                var camera = new GameObject("InspectionCamera").AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.14f, .18f, .22f);
                camera.nearClipPlane = .01f;
                for (int side = 0; side < 2; side++)
                {
                    camera.transform.rotation = Quaternion.Euler(35, 35 + side * 180, 0);
                    camera.transform.position = bounds.center - camera.transform.forward * Mathf.Max(2f, bounds.size.magnitude * 1.6f);
                    Capture(camera, "Logs/GraphicsInspection/" + name + "_" + side + ".png");
                }
            }
            EditorSceneManager.OpenScene(SunsetCoastalSceneSetup.Map02ScenePath);
            var cam = Camera.main;
            var center = new Vector3(1.5f, 0f, 14f);
            for (int side = 0; side < 3; side++)
            {
                cam.transform.rotation = Quaternion.Euler(42, 35 + side * 90, 0);
                cam.transform.position = center - cam.transform.forward * 42f;
                Capture(cam, "Logs/GraphicsInspection/Map02_Baseline_" + side + ".png");
            }
        }

        public static void Capture(Camera camera, string path)
        {
            var target = new RenderTexture(960, 720, 24);
            camera.targetTexture = target;
            camera.aspect = 960f / 720f;
            for (int i = 0; i < 3; i++) camera.Render();
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(960, 720, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 960, 720), 0, 0); image.Apply();
            File.WriteAllBytes(path, image.EncodeToPNG());
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(image); target.Release(); Object.DestroyImmediate(target);
        }
    }
}
#endif
