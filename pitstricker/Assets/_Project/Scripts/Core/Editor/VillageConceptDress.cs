#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using PitStriker.Core;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Concept village dressing aligned to ArenaFrame.
    /// Dirt lane = 14 m play lane. Paddy and fences sit outside plus/minus 7 m.
    /// Does not touch pits, marbles, colliders, or HUD.
    /// </summary>
    public static class VillageConceptDress
    {
        const string RootName = "Environment_Concept_Dress";

        [MenuItem("Pit Striker/Apply Concept Village Dress (Paddy + Side Fences)", false, 22)]
        public static void Apply()
        {
            string scenePath = VillageSceneRename.ActiveVillageScenePath();
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[CONCEPT DRESS] Could not open " + scenePath);
                return;
            }

            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Concept village dress");

            var dirt = MakeMat("M_Concept_DirtLane", new Color(0.31f, 0.135f, 0.052f, 1f), 0.96f);
            var water = MakeMat("M_Concept_PaddyWater", new Color(0.12f, 0.38f, 0.32f, 1f), 0.12f);
            var paddy = MakeMat("M_Concept_PaddyField", new Color(0.16f, 0.46f, 0.10f, 1f), 0.88f);
            var wood = MakeMat("M_Concept_FenceWood", new Color(0.22f, 0.085f, 0.028f, 1f), 0.87f);

            float mid = ArenaFrame.MidZ;
            float length = ArenaFrame.ArenaLength;
            float lane = ArenaFrame.PlayLaneWidth;
            float half = ArenaFrame.LaneHalf;
            float y0 = ArenaFrame.DressMinZ;
            float y1 = ArenaFrame.DressMaxZ;
            float fenceLen = y1 - y0;
            float fenceMid = (y0 + y1) * 0.5f;

            Box("Dirt_Lane", root.transform, new Vector3(0f, 0.025f, mid), new Vector3(lane, 0.05f, length), dirt);
            Box("Paddy_Water_L", root.transform, new Vector3(-(half + 1.7f), 0.14f, mid), new Vector3(3.4f, 0.12f, length), water);
            Box("Paddy_Water_R", root.transform, new Vector3(half + 1.7f, 0.14f, mid), new Vector3(3.4f, 0.12f, length), water);
            Box("Paddy_Field_L", root.transform, new Vector3(-(half + 5.2f), 0.08f, mid), new Vector3(4.2f, 0.10f, length + 2f), paddy);
            Box("Paddy_Field_R", root.transform, new Vector3(half + 5.2f, 0.08f, mid), new Vector3(4.2f, 0.10f, length + 2f), paddy);

            foreach (float x in new[] { -half, half })
            {
                string side = x < 0 ? "L" : "R";
                for (int i = 0; i < 25; i++)
                {
                    float t = i / 24f;
                    float z = y0 + t * fenceLen;
                    Box($"LaneFence_Post_{side}_{i}", root.transform, new Vector3(x, 0.62f, z), new Vector3(0.09f, 1.24f, 0.09f), wood);
                }
                Box($"LaneFence_Rail_{side}_Lo", root.transform, new Vector3(x, 0.38f, fenceMid), new Vector3(0.06f, 0.06f, fenceLen), wood);
                Box($"LaneFence_Rail_{side}_Hi", root.transform, new Vector3(x, 0.86f, fenceMid), new Vector3(0.06f, 0.06f, fenceLen), wood);
            }

            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = root;
            Debug.Log("<color=#00FF88><b>[CONCEPT DRESS]</b> Applied on " + scenePath + " using ArenaFrame 0/12/24, lane +/- " + half + "</color>");
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
