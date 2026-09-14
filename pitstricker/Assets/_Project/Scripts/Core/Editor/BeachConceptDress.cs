#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using PitStriker.Core;
using PitStriker.Gameplay;
using PitStriker.Physics;

namespace PitStriker.EditorTools
{
    public static class BeachConceptDress
    {
        public const string RootName = "Environment_Concept_Beach_Dress";
        public const string SolidGroundName = "Beach_SolidGround";
        const string VillageScene = "Assets/_Project/Scenes/SC_Village_Graphics_Test.unity";
        public const string BeachScene = "Assets/_Project/Scenes/SC_Beach_Graphics_Test.unity";
        const string MatFolder = "Assets/_Project/Art/Environments/Beach/";

        static readonly string[] HideRoots =
        {
            "Environment_Blender_Village_Graphics",
            "Environment_Concept_Dress",
            "Environment_Village_Dressing",
            "VillageMap_Visuals",
            "VillageMap_Visuals_Runtime",
            "Village_Foliage",
            "Village_Scenery",
            "Village_Reference_Polish"
        };

        static readonly string[] HideNameBits =
        {
            "village", "paddy", "foliage", "laterite", "hedge", "reed",
            "vis_home", "vis_stone", "vis_barr", "vis_base", "vis_s1", "vis_s2",
            "ground_bank", "home1", "house", "cottage", "verand", "thatch_roof",
            "coconut", "plant_clump", "tuft", "bush"
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
                throw new System.InvalidOperationException("[BEACH DRESS] Village scene missing: " + VillageScene);

            if (!System.IO.File.Exists(BeachScene))
            {
                if (!AssetDatabase.CopyAsset(VillageScene, BeachScene))
                    throw new System.InvalidOperationException("[BEACH DRESS] Could not copy village scene to beach scene.");
                AssetDatabase.Refresh();
            }

            var scene = EditorSceneManager.OpenScene(BeachScene, OpenSceneMode.Single);
            Apply(scene, save: true);
            EnsureBuildSettings();
            Debug.Log("<color=#00DDFF><b>[BEACH DRESS]</b> Scene ready: " + BeachScene + "</color>");
        }

        [MenuItem("Pit Striker/Remove Concept Beach Dress", false, 26)]
        public static void Remove()
        {
            var old = GameObject.Find(RootName);
            if (old) Undo.DestroyObjectImmediate(old);
            var ground = GameObject.Find(SolidGroundName);
            if (ground) Undo.DestroyObjectImmediate(ground);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        public static void Apply(Scene scene, bool save)
        {
            if (!scene.IsValid())
                throw new System.InvalidOperationException("[BEACH DRESS] Invalid scene.");

            EnsureMatFolder();
            HideVillageDress(scene);
            BuildSolidPlayfield();
            RepairPits();

            var old = GameObject.Find(RootName);
            if (old) Object.DestroyImmediate(old);

            var root = new GameObject(RootName);
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

            Box("Sand_PlayLane", root.transform, new Vector3(0f, -0.03f, ArenaFrame.MidZ), new Vector3(ArenaFrame.PlayLaneWidth, 0.06f, ArenaFrame.ArenaLength), sand);
            Box("Sand_Verge_L", root.transform, new Vector3(-8.2f, 0.02f, 12f), new Vector3(4.0f, 0.05f, 33f), sand);
            Box("Sand_Verge_R", root.transform, new Vector3(8.2f, 0.02f, 12f), new Vector3(4.0f, 0.05f, 33f), sand);
            Box("Dune_Left", root.transform, new Vector3(-9.2f, 0.18f, 12f), new Vector3(3.2f, 0.36f, 36f), dune);
            Box("WetSand_Right", root.transform, new Vector3(8.6f, 0.01f, 12f), new Vector3(2.2f, 0.04f, 34f), wet);
            Box("Ocean_Right", root.transform, new Vector3(11.4f, -0.10f, 12f), new Vector3(5.6f, 0.12f, 38f), water);
            Box("Foam_Right", root.transform, new Vector3(9.4f, 0.02f, 12f), new Vector3(0.38f, 0.03f, 34f), foam);
            Box("Ocean_Far", root.transform, new Vector3(2.2f, -0.12f, 36.5f), new Vector3(24f, 0.14f, 12f), water);

            float railX = ArenaFrame.LaneHalf;
            const float y0 = -5.2f;
            const float y1 = 28.4f;
            float mid = (y0 + y1) * 0.5f;
            float length = y1 - y0;
            foreach (float x in new[] { -railX, railX })
            {
                string side = x < 0 ? "L" : "R";
                for (int i = 0; i < 18; i++)
                {
                    float t = i / 17f;
                    Box($"Driftwood_Post_{side}_{i}", root.transform, new Vector3(x, 0.28f, y0 + t * length), new Vector3(0.22f, 0.22f, 0.55f), wood);
                }
                Box($"Driftwood_Rail_{side}", root.transform, new Vector3(x, 0.42f, mid), new Vector3(0.18f, 0.18f, length), wood);
            }

            Box("Beach_Hut_Body", root.transform, new Vector3(9.2f, 1.15f, 8.2f), new Vector3(3.2f, 2.3f, 4.2f), plaster);
            Box("Beach_Hut_Roof", root.transform, new Vector3(9.2f, 2.55f, 8.2f), new Vector3(3.8f, 0.55f, 4.8f), thatch);
            Box("Fishing_Boat_Hull", root.transform, new Vector3(10.2f, 0.35f, 22.0f), new Vector3(1.1f, 0.55f, 3.6f), wood);
            Cyl("Lighthouse_Shaft", root.transform, new Vector3(11.2f, 3.4f, 33.5f), new Vector3(1.1f, 6.4f, 1.1f), white);
            Cyl("Lighthouse_Stripe", root.transform, new Vector3(11.2f, 3.6f, 33.5f), new Vector3(1.16f, 1.1f, 1.16f), red);

            float[][] palms =
            {
                new[] { -8.4f, -2.0f, 4.8f }, new[] { -8.6f, 5.4f, 5.2f },
                new[] { -8.8f, 18.5f, 5.0f }, new[] { -8.2f, 27.2f, 4.6f },
                new[] { 8.4f, -1.2f, 5.0f }, new[] { 8.8f, 15.0f, 5.4f },
                new[] { 8.4f, 26.6f, 4.8f }, new[] { 7.6f, 30.4f, 4.4f }
            };
            for (int i = 0; i < palms.Length; i++)
                Palm(root.transform, new Vector3(palms[i][0], 0f, palms[i][1]), palms[i][2], trunk, leaf, i);

            TintPlayGround(sand);
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
                Object.DestroyImmediate(col);

            if (save)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static void HideVillageDress(Scene scene)
        {
            foreach (var sceneRoot in scene.GetRootGameObjects())
            {
                string n = sceneRoot.name.ToLowerInvariant();
                if (n.Contains("blender_village") || n.Contains("villagemap") ||
                    n == "environment_concept_dress" || n.Contains("village_dress") ||
                    n.Contains("village_foliage") || n.Contains("village_scenery"))
                {
                    sceneRoot.SetActive(false);
                    continue;
                }
            }

            foreach (var name in HideRoots)
            {
                var go = GameObject.Find(name);
                if (go) MuteVisualTree(go, deactivate: true);
            }

            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (!t) continue;
                if (IsProtectedGameplay(t.gameObject)) continue;
                if (t.gameObject.name == RootName || t.gameObject.name == SolidGroundName) continue;
                if (t.gameObject.name.StartsWith("Beach_")) continue;

                string n = t.gameObject.name.ToLowerInvariant();
                bool hide = false;
                for (int i = 0; i < HideNameBits.Length; i++)
                    if (n.Contains(HideNameBits[i])) { hide = true; break; }
                if (n.Contains("grass") && !n.Contains("beach")) hide = true;
                if ((n.Contains("palm") || n.Contains("tree")) && !n.Contains("beach")) hide = true;
                if (n.Contains("fence") || n.Contains("stone_wall")) hide = true;
                if (!hide && t.position.x < -5.4f && !IsProtectedGameplay(t.gameObject))
                {
                    var rend = t.GetComponent<Renderer>();
                    if (rend && t.GetComponent<Camera>() == null && t.GetComponent<Light>() == null)
                        hide = true;
                }
                if (hide) MuteVisualTree(t.gameObject, deactivate: true);
            }
        }

        static void MuteVisualTree(GameObject go, bool deactivate)
        {
            if (!go || go.name == RootName || go.name == SolidGroundName) return;
            foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            foreach (var col in go.GetComponentsInChildren<Collider>(true))
            {
                if (IsProtectedGameplay(col.gameObject)) continue;
                col.enabled = false;
            }
            if (deactivate && !IsProtectedGameplay(go))
                go.SetActive(false);
        }

        static void BuildSolidPlayfield()
        {
            string[] holeOwners =
            {
                "Ground_Center_Fairway", "Fairway_Road_With_Pits", "Unified_Arena_Ground",
                "Ground_Center_Start", "Ground_Center_Bridge_1_2", "Ground_Center_Bridge_2_3", "Ground_Center_End"
            };
            foreach (var name in holeOwners)
            {
                var go = GameObject.Find(name);
                if (!go) continue;
                foreach (var col in go.GetComponents<Collider>())
                    col.enabled = false;
            }

            var existing = GameObject.Find(SolidGroundName);
            if (existing) Object.DestroyImmediate(existing);

            var ground = new GameObject(SolidGroundName);
            ground.transform.position = new Vector3(0f, 0f, ArenaFrame.MidZ);
            var box = ground.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, -0.12f, 0f);
            box.size = new Vector3(ArenaFrame.PlayLaneWidth, 0.24f, ArenaFrame.ArenaLength + 10f);
            box.enabled = true;
        }

        static void RepairPits()
        {
            var zones = Object.FindObjectsByType<PitZone>(FindObjectsInactive.Include);
            foreach (var zone in zones)
            {
                if (!zone) continue;
                int n = zone.PitNumber;
                if (n < 1 || n > 3) continue;
                zone.transform.position = ArenaFrame.PitPosition(n);
                zone.transform.rotation = Quaternion.identity;
                foreach (var meshCol in zone.GetComponentsInChildren<MeshCollider>(true))
                    meshCol.enabled = false;
                var sphere = zone.GetComponent<SphereCollider>();
                if (!sphere) sphere = zone.gameObject.AddComponent<SphereCollider>();
                sphere.isTrigger = true;
                sphere.center = new Vector3(0f, 0.08f, 0f);
                sphere.radius = 0.42f;
                sphere.enabled = true;
                zone.SetPitNumber(n);
                EditorUtility.SetDirty(zone);
            }
        }

        static bool IsProtectedGameplay(GameObject go)
        {
            if (!go) return false;
            if (go.GetComponent<PitZone>()) return true;
            if (go.GetComponent<MarbleController>()) return true;
            if (go.GetComponent<Camera>()) return true;
            if (go.GetComponent<Light>()) return true;
            if (go.GetComponent<TurnManager>()) return true;
            string n = go.name;
            if (n == SolidGroundName || n == RootName) return true;
            if (n.StartsWith("Pit_") || n.StartsWith("Beach_")) return true;
            if (n.StartsWith("Marble") || n.StartsWith("Player")) return true;
            if (n.Contains("HUD") || n.Contains("Menu")) return true;
            return false;
        }

        static void EnsureBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
                if (scenes[i].path == BeachScene) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(BeachScene, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void TintPlayGround(Material sand)
        {
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude))
            {
                if (!renderer) continue;
                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("ground") || n.Contains("playfield") || n.Contains("fairway") || n.Contains("soil"))
                    renderer.sharedMaterial = sand;
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
                var leaf = GameObject.CreatePrimitive(PrimitiveType.Cube);
                leaf.name = "Frond_" + i;
                leaf.transform.SetParent(go.transform, true);
                leaf.transform.position = basePos + Vector3.up * height;
                leaf.transform.localScale = new Vector3(1.55f, 0.05f, 0.28f);
                leaf.transform.rotation = Quaternion.Euler(18f, i * 45f, 12f);
                var renderer = leaf.GetComponent<MeshRenderer>();
                if (renderer) renderer.sharedMaterial = leafMat;
            }
        }
    }
}
#endif
