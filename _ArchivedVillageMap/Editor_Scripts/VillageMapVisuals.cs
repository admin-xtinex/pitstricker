using UnityEditor;
using UnityEngine;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Dresses the working gameplay arena with simple village / rough-soil visuals.
    /// Does NOT modify gameplay components (Rigidbody, PitZone, TurnManager, etc.).
    /// </summary>
    public static class VillageMapVisuals
    {
        const string VisualRootName = "VillageMap_Visuals";

        const string HomePath = "Assets/ThirdParty/CrashBash/Fbx/Home1.fbx";
        const string StonePath = "Assets/ThirdParty/CrashBash/Fbx/Stone.fbx";
        const string BarrPath = "Assets/ThirdParty/CrashBash/Fbx/Barr.fbx";
        const string BasePath = "Assets/ThirdParty/CrashBash/Fbx/Base.fbx";
        const string S1Path = "Assets/ThirdParty/CrashBash/Fbx/S1.fbx";
        const string S2Path = "Assets/ThirdParty/CrashBash/Fbx/s2.fbx";


        [MenuItem("Pit Striker/Apply Village Map Visuals (Rough Soil)", false, 20)]
        public static void Apply()
        {
            Undo.SetCurrentGroupName("Apply Village Map Visuals");
            int undo = Undo.GetCurrentGroup();

            // Remove previous visual dress
            var old = GameObject.Find(VisualRootName);
            if (old != null)
                Undo.DestroyObjectImmediate(old);

            var root = new GameObject(VisualRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create VillageMap_Visuals");

            Place(HomePath, root.transform, new Vector3(-9f, 0f, 10f), new Vector3(0f, 25f, 0f), Vector3.one * 1.2f, "VIS_Home1");
            Place(StonePath, root.transform, new Vector3(7.5f, 0f, 4f), Vector3.zero, Vector3.one * 1.5f, "VIS_Stone_A");
            Place(StonePath, root.transform, new Vector3(-7f, 0f, 22f), new Vector3(0f, 40f, 0f), Vector3.one * 1.2f, "VIS_Stone_B");
            Place(BarrPath, root.transform, new Vector3(8f, 0f, 16f), new Vector3(0f, -15f, 0f), Vector3.one, "VIS_Barr_A");
            Place(BarrPath, root.transform, new Vector3(-8.5f, 0f, 28f), new Vector3(0f, 180f, 0f), Vector3.one, "VIS_Barr_B");
            Place(BasePath, root.transform, new Vector3(9f, 0f, 30f), Vector3.zero, Vector3.one * 0.8f, "VIS_Base");
            Place(S1Path, root.transform, new Vector3(-6.5f, 0f, 6f), new Vector3(0f, 90f, 0f), Vector3.one, "VIS_S1");
            Place(S2Path, root.transform, new Vector3(6.8f, 0f, 12f), new Vector3(0f, -90f, 0f), Vector3.one, "VIS_S2");

            TintFairwayRoughSoil();
            StripCollidersUnder(root.transform);

            Undo.CollapseUndoOperations(undo);
            Selection.activeGameObject = root;
            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Village / Rough Soil visuals applied. Gameplay objects untouched.</color>");
        }

        [MenuItem("Pit Striker/Remove Village Map Visuals", false, 21)]
        public static void Remove()
        {
            var old = GameObject.Find(VisualRootName);
            if (old == null)
            {
                Debug.LogWarning("[PIT STRIKER] No VillageMap_Visuals found.");
                return;
            }
            Undo.DestroyObjectImmediate(old);
            Debug.Log("<color=#00FF88><b>[PIT STRIKER]</b> Village map visuals removed.</color>");
        }

        static void Place(string assetPath, Transform parent, Vector3 pos, Vector3 euler, Vector3 scale, string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[PIT STRIKER] Missing asset: {assetPath}");
                return;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null)
                instance = Object.Instantiate(prefab);

            Undo.RegisterCreatedObjectUndo(instance, "Place " + name);
            instance.name = name;
            instance.transform.SetParent(parent, true);
            instance.transform.position = pos;
            instance.transform.rotation = Quaternion.Euler(euler);
            instance.transform.localScale = scale;
        }

        /// <summary>
        /// Visual-only: darken existing sand ground renderers toward rough soil.
        /// Does not change PhysicsMaterials / colliders.
        /// </summary>
        static void TintFairwayRoughSoil()
        {
            var sandMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/M_Ground_Sand.mat");
            if (sandMat == null) return;

            // Soft instance so we don't permanently dirty the shared sand mat for Beach later.
            var soil = new Material(sandMat);
            soil.name = "M_Ground_RoughSoil_Runtime";
            if (soil.HasProperty("_BaseColor"))
                soil.SetColor("_BaseColor", new Color(0.32f, 0.18f, 0.08f, 1f));
            else if (soil.HasProperty("_Color"))
                soil.SetColor("_Color", new Color(0.32f, 0.18f, 0.08f, 1f));

            var arena = GameObject.Find("Arena_Sandbox");
            if (arena == null) return;

            foreach (var rend in arena.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (rend == null) continue;
                if (!rend.name.StartsWith("Ground_")) continue;
                Undo.RecordObject(rend, "Tint rough soil");
                rend.sharedMaterial = soil;
            }
        }

        /// <summary>
        /// Keep dress-up meshes from fighting gameplay physics.
        /// </summary>
        static void StripCollidersUnder(Transform root)
        {
            foreach (var col in root.GetComponentsInChildren<Collider>(true))
            {
                Undo.DestroyObjectImmediate(col);
            }
            foreach (var rb in root.GetComponentsInChildren<Rigidbody>(true))
            {
                Undo.DestroyObjectImmediate(rb);
            }
        }
    }
}