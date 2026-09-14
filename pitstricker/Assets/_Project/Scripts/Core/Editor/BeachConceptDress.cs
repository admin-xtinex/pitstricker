#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Beach concept dress at the same gameplay footprint as the village map:
    /// 20x34 arena, pits 0/12/24, r=0.18, left bank reserved for camera.
    /// Visual only. Does not touch pits, marbles, colliders, or HUD.
    /// </summary>
    public static class BeachConceptDress
    {
        public const string RootName = "Environment_Concept_Beach_Dress";
        const string VillageScene = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        public const string BeachScene = "Assets/_Project/Scenes/SC_Beach_Graphics_Test.unity";
        const string MatFolder = "Assets/_Project/Art/Environments/Beach/";

        static readonly string[] HideRoots =
        {
            "Environment_Blender_Village_Graphics",
            "Environment_Concept_Dress",
            "Environment_Village_Dressing",
            "VillageMap_Visuals"
        };

        [MenuItem("Pit Striker/Apply Concept Beach Dress (Ocean + Driftwood)", false, 24)]
        public static void ApplyToOpenOrVillageScene()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
                scene = EditorSceneManager.OpenScene(VillageScene, OpenSceneMode.Single);
            Apply(scene, save: true);
        }

        [MenuItem("Pit Striker/Create Beach Graphics Test Scene", false, 25)]
        public static void CreateBeachScene()
        {
            if (!System.IO.File.Exists(VillageScene))
            {
                Debug.LogError("[BEACH DRESS] Village scene missing: " + VillageScene);
                return;
            }

            if (!System.IO.File.Exists(BeachScene))
            {
                if (!AssetDatabase.CopyAsset(VillageScene, BeachScene))
                {
                    Debug.LogError("[BEACH DRESS] Could not copy village scene to beach scene.");
                    return;
                }
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
            Apply(scene, save: true);
            Debug.Log("<color=#00DDFF><b>[BEACH DRESS]</b> Scene ready: " + BeachScene + ". Same pits/marbles. Village props hidden.</color>");
        }

        [MenuItem("Pit Striker/Remove Concept Beach Dress", false, 26)]
        public static void Remove()
        {
            var old = GameObject.Find(RootName);
            if (!old)
            {
                Debug.LogWarning("[BEACH DRESS] Nothing to remove.");
                return;
            }
            Undo.DestroyObjectImmediate(old);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Apply(Scene scene, bool save)
        {
            if (!scene.IsValid())
            {
                Debug.LogError("[BEACH DRESS] Invalid scene.");
                return;
            }

            EnsureMatFolder();

            foreach (var name in HideRoots)
            {
                var go = GameObject.Find(name);
                if (!go) continue;
                foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                    renderer.enabled = false;
            }

            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);

            var root = new GameObject(RootName);
            Undo.RegisterCreatedObjectUndo(root, "Concept beach dress");

            var sand = MakeMat("M_Beach_Sand", new Color(0.78f, 0.58f, 0.32f, 1f), 0.86f);
            var wet = MakeMat("M_Beach_WetSand", new Color(0.52f, 0.38f, 0.22f, 1f), 0.42f);
            var water = MakeMat("M_Beach_Ocean", new Color(0.04f, 0.38f, 0.55f, 1f), 0.12f);
            var foam = MakeMat("M_Beach_Foam", new Color(0.92f, 0.95f, 0.96f, 1f), 0.35f);
            var dune = MakeMat("M_Beach_Dune", new Color(0.70f, 0.52f, 0.28f, 1f), 0.92f);
            var wood = MakeMat("M_Beach_Driftwood", new Color(0.28f, 0.16f, 0.08f, 1f), 0.90f);
            var thatch = MakeMat("M_Beach_Thatch", new Color(0.42f, 0.28f, 0.10f, 1f), 0.88f);
            var plaster = MakeMat("M_Beach_HutPlaster", new Color(0.82f, 0.72f, 0.52f, 1f), 0.80f);
            var trunk = MakeMat("M_Beach_PalmTrunk", new Color(0.28f, 0.14f, 0.05f, 1f), 0.94f);
            var leaf = MakeMat("M_Beach_PalmLeaf", new Color(0.10f, 0.38f, 0.08f, 1f), 0.78f);
            var white = MakeMat("M_Beach_Lighthouse", new Color(0.92f, 0.90f, 0.84f, 1f), 0.55f);
            var red = MakeMat("M_Beach_LighthouseStripe", new Color(0.62f, 0.12f, 0.08f, 1f), 0.55f);
            var rock = MakeMat("M_Beach_Rock", new Color(0.32f, 0.30f, 0.26f, 1f), 0.95f);

            Box("Sand_Verge_L", root.transform, new Vector3(-5.9f, 0.02f, 12f), new Vector3(4.4f, 0.05f, 33f), sand);
            Box("Sand_Verge_R", root.transform, new Vector3(5.4f, 0.02f, 12f), new Vector3(3.2f, 0.05f, 33f), sand);
            Box("Dune_Left", root.transform, new Vector3(-7.6f, 0.18f, 12f), new Vector3(4.4f, 0.36f, 36f), dune);
            Box("WetSand_Right", root.transform, new Vector3(4.55f, 0.01f, 12f), new Vector3(1.6f, 0.04f, 34f), wet);
            Box("Ocean_Right", root.transform, new Vector3(7.6f, -0.10f, 12f), new Vector3(5.6f, 0.12f, 38f), water);
            Box("Foam_Right", root.transform, new Vector3(5.40f, 0.02f, 12f), new Vector3(0.38f, 0.03f, 34f), foam);
            Box("Ocean_Far", root.transform, new Vector3(2.2f, -0.12f, 36.5f), new Vector3(24f, 0.14f, 12f), water);
            Box("Foam_Far", root.transform, new Vector3(1.0f, 0.02f, 30.6f), new Vector3(18f, 0.03f, 0.55f), foam);
            Box("Background_Sand", root.transform, new Vector3(0f, 0.03f, 29.8f), new Vector3(16f, 0.06f, 5.0f), sand);

            const float y0 = -5.2f;
            const float y1 = 28.4f;
            float mid = (y0 + y1) * 0.5f;
            float length = y1 - y0;
            foreach (float x in new[] { -3.55f, 3.55f })
            {
                string side = x < 0 ? "L" : "R";
                for (int i = 0; i < 18; i++)
                {
                    float t = i / 17f;
                    float z = y0 + t * length;
                    Box($"Driftwood_Post_{side}_{i}", root.transform, new Vector3(x, 0.28f, z), new Vector3(0.22f, 0.22f, 0.55f), wood);
                }
                Box($"Driftwood_Rail_{side}", root.transform, new Vector3(x, 0.42f, mid), new Vector3(0.18f, 0.18f, length), wood);
            }

            Box("Beach_Hut_Body", root.transform, new Vector3(7.5f, 1.15f, 8.2f), new Vector3(3.2f, 2.3f, 4.2f), plaster);
            Box("Beach_Hut_Roof", root.transform, new Vector3(7.5f, 2.55f, 8.2f), new Vector3(3.8f, 0.55f, 4.8f), thatch);
            Box("Beach_Hut_Deck", root.transform, new Vector3(6.2f, 0.22f, 8.2f), new Vector3(1.2f, 0.12f, 4.0f), wood);
            Box("Beach_Hut_Post_A", root.transform, new Vector3(5.75f, 0.85f, 6.6f), new Vector3(0.12f, 1.5f, 0.12f), wood);
            Box("Beach_Hut_Post_B", root.transform, new Vector3(5.75f, 0.85f, 9.8f), new Vector3(0.12f, 1.5f, 0.12f), wood);

            Box("Fishing_Boat_Hull", root.transform, new Vector3(8.4f, 0.35f, 22.0f), new Vector3(1.1f, 0.55f, 3.6f), wood);
            Box("Fishing_Boat_Cabin", root.transform, new Vector3(8.4f, 0.85f, 21.4f), new Vector3(0.7f, 0.55f, 1.1f), plaster);

            Box("Lighthouse_Island", root.transform, new Vector3(9.6f, 0.35f, 33.5f), new Vector3(3.4f, 0.7f, 3.4f), rock);
            Cyl("Lighthouse_Shaft", root.transform, new Vector3(9.6f, 3.4f, 33.5f), new Vector3(1.1f, 6.4f, 1.1f), white);
            Cyl("Lighthouse_Stripe", root.transform, new Vector3(9.6f, 3.6f, 33.5f), new Vector3(1.16f, 1.1f, 1.16f), red);
            Box("Lighthouse_Lamp", root.transform, new Vector3(9.6f, 6.8f, 33.5f), new Vector3(1.3f, 0.7f, 1.3f), foam);

            float[][] palms =
            {
                new[] { -7.2f, -2.0f, 4.8f },
                new[] { -7.4f, 5.4f, 5.2f },
                new[] { -7.8f, 18.5f, 5.0f },
                new[] { -6.6f, 27.2f, 4.6f },
                new[] { 6.6f, -1.2f, 5.0f },
                new[] { 7.4f, 15.0f, 5.4f },
                new[] { 6.8f, 26.6f, 4.8f },
                new[] { 4.6f, 30.4f, 4.4f }
            };
            for (int i = 0; i < palms.Length; i++)
                Palm(root.transform, new Vector3(palms[i][0], 0f, palms[i][1]), palms[i][2], trunk, leaf, i);

            Vector3[] rocks =
            {
                new Vector3(-6.4f, 0.18f, 2.2f),
                new Vector3(-6.8f, 0.22f, 11.5f),
                new Vector3(-5.9f, 0.16f, 21.0f),
                new Vector3(5.2f, 0.14f, 1.8f),
                new Vector3(5.6f, 0.18f, 16.4f),
                new Vector3(6.1f, 0.20f, 27.8f)
            };
            for (int i = 0; i < rocks.Length; i++)
                Box($"Beach_Rock_{i}", root.transform, rocks[i], new Vector3(0.55f, 0.32f, 0.48f), rock);

            TintPlayGround(sand);

            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            if (save)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("<color=#00DDFF><b>[BEACH DRESS]</b> Ocean, dunes, hut, boat, lighthouse, palms. Pits/marbles/HUD unchanged.</color>");
        }

        static void TintPlayGround(Material sand)
        {
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer) continue;
                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("ground") || n.Contains("playfield") || n.Contains("arena_floor") || n.Contains("soil"))
                {
                    renderer.sharedMaterial = sand;
                }
            }
        }

        static void EnsureMatFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Environments/Beach"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Environments"))
                    AssetDatabase.CreateFolder("Assets/_Project/Art", "Environments");
                AssetDatabase.CreateFolder("Assets/_Project/Art/Environments", "Beach");
            }
        }

        static Material MakeMat(string name, Color color, float roughness)
        {
            string path = MatFolder + name + ".mat";
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

        static void Cyl(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = pos;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer) renderer.sharedMaterial = mat;
        }

        static void Palm(Transform parent, Vector3 basePos, float height, Material trunkMat, Material leafMat, int index)
        {
            var go = new GameObject("Beach_Palm_" + index);
            go.transform.SetParent(parent, true);
            go.transform.position = basePos;
            Cyl("Trunk", go.transform, basePos + Vector3.up * (height * 0.5f), new Vector3(0.28f, height, 0.28f), trunkMat);
            for (int i = 0; i < 8; i++)
            {
                float ang = i * 45f;
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "Frond_" + i;
                leaf.transform.SetParent(go.transform, true);
                leaf.transform.position = basePos + Vector3.up * height;
                leaf.transform.localScale = new Vector3(1.55f, 0.05f, 0.28f);
                leaf.transform.rotation = Quaternion.Euler(18f, ang, 12f);
                var renderer = leaf.GetComponent<MeshRenderer>();
                if (renderer) renderer.sharedMaterial = leafMat;
            }
        }
    }
}
#endif
