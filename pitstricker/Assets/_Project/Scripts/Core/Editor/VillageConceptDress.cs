#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Drops concept-art village dressing into SC_Village_Graphics_Test:
    /// paddy water both sides, lane fences, bank grass.
    /// Does not touch pits, marbles, colliders, or HUD.
    /// </summary>
    public static class VillageConceptDress
    {
        const string RootName = "Environment_Concept_Dress";
        const string ScenePath = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";

        [MenuItem("Pit Striker/Apply Concept Village Dress (Paddy + Side Fences)", false, 22)]
        public static void Apply()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[CONCEPT DRESS] Could not open " + ScenePath);
                return;
            }

            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Concept village dress");

            var water = MakeMat("M_Concept_PaddyWater", new Color(0.12f, 0.38f, 0.32f, 1f), 0.12f);
            var paddy = MakeMat("M_Concept_PaddyField", new Color(0.16f, 0.46f, 0.10f, 1f), 0.88f);
            var wood = MakeMat("M_Concept_FenceWood", new Color(0.22f, 0.085f, 0.028f, 1f), 0.87f);

            // y must sit ABOVE the dirt slab or Game view only shows sand.
            Box("Paddy_Water_L", root.transform, new Vector3(-6.4f, 0.14f, 12f), new Vector3(3.4f, 0.12f, 34f), water);
            Box("Paddy_Water_R", root.transform, new Vector3(6.4f, 0.14f, 12f), new Vector3(3.4f, 0.12f, 34f), water);
            Box("Paddy_Field_L", root.transform, new Vector3(-10.2f, 0.08f, 12f), new Vector3(4.2f, 0.10f, 36f), paddy);
            Box("Paddy_Field_R", root.transform, new Vector3(10.2f, 0.08f, 12f), new Vector3(4.2f, 0.10f, 36f), paddy);

            const float y0 = -5.2f;
            const float y1 = 28.4f;
            float mid = (y0 + y1) * 0.5f;
            float length = y1 - y0;
            foreach (float x in new[] { -3.45f, 3.45f })
            {
                string side = x < 0 ? "L" : "R";
                for (int i = 0; i < 25; i++)
                {
                    float t = i / 24f;
                    float z = y0 + t * length;
                    Box($"LaneFence_Post_{side}_{i}", root.transform, new Vector3(x, 0.62f, z), new Vector3(0.09f, 1.24f, 0.09f), wood);
                }
                Box($"LaneFence_Rail_{side}_Lo", root.transform, new Vector3(x, 0.38f, mid), new Vector3(0.06f, 0.06f, length), wood);
                Box($"LaneFence_Rail_{side}_Hi", root.transform, new Vector3(x, 0.86f, mid), new Vector3(0.06f, 0.06f, length), wood);
            }

            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("<color=#00FF88><b>[CONCEPT DRESS]</b> Paddy water raised to y=0.14. Select Paddy_Water_L in Scene view to confirm.</color>");
        }

        [MenuItem("Pit Striker/Remove Concept Village Dress", false, 23)]
        public static void Remove()
        {
            var old = GameObject.Find(RootName);
            if (!old)
            {
                Debug.LogWarning("[CONCEPT DRESS] Nothing to remove.");
                return;
            }
            Undo.DestroyObjectImmediate(old);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        static Material MakeMat(string name, Color color, float roughness)
        {
            string path = "Assets/_Project/Art/Environments/Village/" + name + ".mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (!shader) shader = Shader.Find("Standard");
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - roughness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static void Box(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer) renderer.sharedMaterial = mat;
        }
    }
}
#endif
