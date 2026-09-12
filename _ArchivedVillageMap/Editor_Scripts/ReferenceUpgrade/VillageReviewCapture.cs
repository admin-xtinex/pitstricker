#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
namespace PitStriker.EditorTools
{
    // Local file mailbox for repeatable editor validation; no network or runtime component.
    [InitializeOnLoad]
    public static class VillageReviewCapture
    {
        const string Request = "Library/VillageReview.request";
        const string Result = "Logs/VillageReview.txt";
        static VillageReviewCapture() { EditorApplication.update += Poll; }
        static void Poll()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Request)) return;
            string command = File.ReadAllText(Request).Trim(); File.Delete(Request);
            if (string.IsNullOrWhiteSpace(command)) return;
            try
            {
                var scene = SceneManager.GetActiveScene();
                if (scene.path != VillageGraphicsIntegration.Destination) throw new Exception("Open the village graphics scene.");
                if (command == "inspect")
                {
                    var roots = string.Join("\nROOT: ", scene.GetRootGameObjects().Select(g => g.name + " (children=" + g.transform.childCount + ", components=" + string.Join(",", g.GetComponents<Component>().Select(c => c ? c.GetType().Name : "null")) + ")"));
                    var rends = string.Join("\n", scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
                        .Where(r => r.enabled).Select(r => r.name + " | " + r.bounds + " | " + string.Join(",",r.sharedMaterials.Select(m=>m ? m.name : "NULL"))));
                    File.WriteAllText(Result, "Playing=" + EditorApplication.isPlaying + "\nDirty=" + scene.isDirty + "\nROOT: " + roots + "\n---\n" + rends);
                    return;
                }
                if (command == "inspect-stone")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== STONE DETAILED MESH AUDIT ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        if (r.name.StartsWith("Detail_Stone_") && r.enabled)
                        {
                            var mf = r.GetComponent<MeshFilter>();
                            int vCount = mf && mf.sharedMesh ? mf.sharedMesh.vertexCount : 0;
                            sb.AppendLine($"{r.name} | center={r.bounds.center} | size={r.bounds.size} | min={r.bounds.min} | max={r.bounds.max} | verts={vCount}");
                        }
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "disabled-audit")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== ALL RENDERERS (including disabled) ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        sb.AppendLine($"{(r.enabled ? "ON " : "OFF")} | {r.name} | root={r.transform.root.name} | mat={r.sharedMaterial?.name} | pos={r.transform.position}");
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "find-green-wall")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== FENCE VERTICES DETAILED AUDIT ===");
                    var fenceObj = GameObject.Find("Detail_Wood_0_3");
                    if (fenceObj != null)
                    {
                        var mf = fenceObj.GetComponent<MeshFilter>();
                        var mr = fenceObj.GetComponent<MeshRenderer>();
                        var mesh = mf.sharedMesh;
                        Vector3 minW = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                        Vector3 maxW = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                        foreach (var v in mesh.vertices)
                        {
                            Vector3 w = fenceObj.transform.TransformPoint(v);
                            minW = Vector3.Min(minW, w);
                            maxW = Vector3.Max(maxW, w);
                        }
                        sb.AppendLine($"World AABB: min={minW} max={maxW}");
                        sb.AppendLine($"Renderer.bounds: min={mr.bounds.min} max={mr.bounds.max}");
                        sb.AppendLine($"mr.enabled={mr.enabled}, isVisible={mr.isVisible}");
                        sb.AppendLine($"Materials count: {mr.sharedMaterials.Length}");
                        for (int m = 0; m < mr.sharedMaterials.Length; m++)
                        {
                            var mat = mr.sharedMaterials[m];
                            sb.AppendLine($"  mat[{m}]: {(mat ? mat.name : "NULL")}, shader={(mat ? mat.shader.name : "NULL")}, color={(mat && mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor").ToString() : "N/A")}");
                        }
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "inspect-missing")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== LEFT OBJECTS ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        if (r.enabled && r.bounds.center.x < -15f && r.bounds.center.x > -35f && r.bounds.center.z < 25f && r.bounds.center.z > -10f)
                        {
                            sb.AppendLine($"{r.name} | root={r.transform.root.name} | center={r.bounds.center} | ext={r.bounds.extents}");
                        }
                    }
                    sb.AppendLine("\n=== BLENDER ALL CHILDREN ===");
                    var graphics = GameObject.Find("Environment_Blender_Village_Graphics");
                    if (graphics != null)
                    {
                        for (int i = 0; i < graphics.transform.childCount; i++)
                        {
                            var c = graphics.transform.GetChild(i);
                            var mr = c.GetComponent<Renderer>();
                            sb.AppendLine($"{c.name} | active={c.gameObject.activeSelf} | mr_enabled={(mr ? mr.enabled.ToString() : "no_mr")} | pos={c.position} | scale={c.localScale}");
                        }
                    }
                    sb.AppendLine("\n=== PIT STRIKER SIGNS ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        if (r.name.Contains("Sign") || r.name.Contains("Sticker") || r.name.Contains("Text"))
                        {
                            sb.AppendLine($"SIGN: {r.name} | enabled={r.enabled} | root={r.transform.root.name} | pos={r.transform.position} | bounds={r.bounds}");
                        }
                    }
                    sb.AppendLine("\n=== AWNING / TENT OBJECTS ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        if (r.name.ToLower().Contains("awning") || r.name.ToLower().Contains("tent") || r.name.ToLower().Contains("canopy") || r.name.ToLower().Contains("stall"))
                        {
                            sb.AppendLine($"AWNING: {r.name} | enabled={r.enabled} | root={r.transform.root.name} | pos={r.transform.position} | bounds={r.bounds}");
                        }
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "inspect-fence")
                {
                    var sb = new System.Text.StringBuilder();
                    sb.AppendLine("=== FENCE AND REAR OBJECTS AUDIT ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        var b = r.bounds;
                        if (r.name.Contains("Wood") || r.name.Contains("Fence") || (b.center.z >= 28f && b.center.z <= 50f))
                        {
                            var mf = r.GetComponent<MeshFilter>();
                            int vCount = mf && mf.sharedMesh ? mf.sharedMesh.vertexCount : -1;
                            sb.AppendLine($"{(r.enabled ? "ON " : "OFF")} | {r.gameObject.name} | root={r.transform.root.name} | pos={r.transform.position} | scale={r.transform.localScale} | center={b.center} | ext={b.extents} | mat={r.sharedMaterial?.name} | verts={vCount}");
                        }
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "inspect-house")
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        if (!r.enabled) continue;
                        var b = r.bounds;
                        if (b.center.x <= -1f && b.center.x >= -25f && b.center.z >= -2f && b.center.z <= 25f)
                        {
                            sb.AppendLine($"{r.gameObject.name} (root: {r.transform.root.name}, parent: {r.transform.parent?.name}) pos={r.transform.position} localPos={r.transform.localPosition} center={b.center} ext={b.extents} mat={r.sharedMaterial?.name}");
                        }
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                if (command == "backup")
                {
                    if (EditorApplication.isPlaying) throw new Exception("Stop play before backing up.");
                    string backup = "Assets/_Project/Scenes/SC_Village_Backup_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".unity";
                    if (!EditorSceneManager.SaveScene(scene, backup, true)) throw new Exception("Backup failed");
                    File.WriteAllText(Result, "Backed up open scene: " + backup); return;
                }
                if (command == "stop_play")
                {
                    EditorApplication.isPlaying = false;
                    File.WriteAllText(Result, "Stopped play mode.");
                    return;
                }
                if (command == "refresh")
                {
                    AssetDatabase.Refresh();
                    File.WriteAllText(Result, "Refreshed AssetDatabase.");
                    return;
                }
                if (command == "stop-play")
                {
                    EditorApplication.isPlaying = false;
                    File.WriteAllText(Result, "Stopped play mode.");
                    return;
                }
                if (command == "check-menu")
                {
                    if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                    MenuFlowChecks.Run();
                    File.WriteAllText(Result, "Started MenuFlowChecks...");
                    return;
                }
                if (command == "widen_arena")
                {
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                        File.WriteAllText(Result, "Stopping play mode. Please re-trigger widen_arena.");
                        return;
                    }

                    // 1. Upgrade Boundary Walls to 75% wider positions (Wall_Left: -14.35m, Wall_Right: +14.35m, Back/Front width: 35.0m)
                    VillageSceneSetup.UpgradeBoundaryWalls();

                    // 2. Generate Unified Full Arena Mesh (X in [-17.5, 17.5])
                    var soilMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Environments/Village/Soil.mat");
                    var bouncePhys = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>("Assets/_Project/Physics/PhysMat_BoundaryRail.physicMaterial");
                    VillagePitMeshGenerator.ConfigureUnifiedArenaGround(soilMat, bouncePhys);

                    // 3. Shift roadside Blender props (house, bike, cow, fences, benches) outward along X
                    var graphics = GameObject.Find("Environment_Blender_Village_Graphics");
                    if (graphics != null)
                    {
                        for (int i = 0; i < graphics.transform.childCount; i++)
                        {
                            Transform child = graphics.transform.GetChild(i);
                            string n = child.name;

                            // House group (rigidly at target offset -5.40m along X)
                            if (n.StartsWith("Detail_Plaster_-1") || n.StartsWith("Detail_Terracotta") ||
                                n.StartsWith("Detail_Shadow_-1") || n == "Detail_Wood_-1_0" || n == "Detail_Wood_-1_1")
                            {
                                child.position = new Vector3(-5.40f, child.position.y, child.position.z);
                            }
                            // Bicycle group (rigidly at target offset +4.61m along X)
                            else if (n.StartsWith("Bicycle_") || n.StartsWith("Bike_"))
                            {
                                child.position = new Vector3(4.61f, child.position.y, child.position.z);
                            }
                            // Cow group (rigidly at target offset +4.35m along X)
                            else if (n.StartsWith("Cow_"))
                            {
                                child.position = new Vector3(4.35f, child.position.y, child.position.z);
                            }
                            // Back Fence main span: scale along X by 1.75
                            else if (n == "Detail_Wood_0_3")
                            {
                                child.localScale = new Vector3(1.75f, child.localScale.y, child.localScale.z);
                            }
                            // Stone ground patches along road borders (transforms at origin with baked vertices)
                            else if (n.StartsWith("Detail_Stone_"))
                            {
                                if (n.Contains("_-1_"))
                                {
                                    child.position = new Vector3(-3.60f, child.position.y, child.position.z);
                                }
                                else if (n.Contains("_0_"))
                                {
                                    child.position = new Vector3(3.75f, child.position.y, child.position.z);
                                }
                            }
                            // Roadside wooden benches (transforms at origin with baked vertices)
                            else if (n == "Detail_Wood_0_-1")
                            {
                                child.position = new Vector3(3.45f, child.position.y, child.position.z);
                            }
                            else if (n == "Detail_Wood_0_1")
                            {
                                child.position = new Vector3(4.58f, child.position.y, child.position.z);
                            }
                        }
                    }

                    // Destroy any remaining Reference_Signs and HouseWall_Sticker
                    var signs = GameObject.Find("Reference_Signs");
                    if (signs != null)
                    {
                        UnityEngine.Object.DestroyImmediate(signs);
                    }
                    var sticker = GameObject.Find("HouseWall_Sticker");
                    if (sticker != null)
                    {
                        UnityEngine.Object.DestroyImmediate(sticker);
                    }

                    // 4. Reposition waiting sideline player marbles
                    var p2 = GameObject.Find("PlayerMarble_2_Red");
                    if (p2 != null) p2.transform.position = new Vector3(-4.2f, 0.25f, -4.6f);
                    var p3 = GameObject.Find("PlayerMarble_3_Green");
                    if (p3 != null) p3.transform.position = new Vector3(-3.6f, 0.25f, -4.3f);
                    var p4 = GameObject.Find("PlayerMarble_4_Amber");
                    if (p4 != null) p4.transform.position = new Vector3(-3.0f, 0.25f, -4.0f);

                    // 5. Apply full visual upgrade (regenerates meadow, scenery details, lighting, validations)
                    VillageVisualUpgrade.ApplyToOpenScene();
                    VillageVisualUpgrade.ValidateOpenScene();

                    // 6. Save scene cleanly
                    if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed");
                    AssetDatabase.SaveAssets();

                    File.WriteAllText(Result, "Widen arena (+75%) completed, applied, validated and saved successfully.");
                    return;
                }
                if (command == "apply" || command == "apply_and_save")
                {
                    if (EditorApplication.isPlaying)
                    {
                        EditorApplication.isPlaying = false;
                        File.WriteAllText(Result, "Stopping play mode. Please re-trigger apply.");
                        return;
                    }
                    VillageVisualUpgrade.ApplyToOpenScene();
                    if (command == "apply_and_save")
                    {
                        VillageVisualUpgrade.ValidateOpenScene();
                        if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed");
                        AssetDatabase.SaveAssets();
                        File.WriteAllText(Result, "Applied, validated and saved scene successfully.");
                    }
                    else
                    {
                        File.WriteAllText(Result, "Applied and validated; scene left unsaved for review.");
                    }
                    return;
                }
                if (command == "save")
                {
                    VillageVisualUpgrade.ValidateOpenScene();
                    if (!EditorSceneManager.SaveScene(scene)) throw new Exception("Save failed");
                    AssetDatabase.SaveAssets(); File.WriteAllText(Result, "Validated and saved."); return;
                }
                if (command.StartsWith("capture"))
                {
                    Capture(command); File.WriteAllText(Result, "Captured " + command); return;
                }
                if (command == "inspect-all")
                {
                    var sb = new System.Text.StringBuilder();
                    // All renderers in entire scene — both ON and OFF
                    sb.AppendLine("=== ALL RENDERERS (ON and OFF) ===");
                    foreach (var r in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true)))
                    {
                        sb.AppendLine($"{(r.enabled ? "ON " : "OFF")} | {r.name} | root={r.transform.root.name} | mat={r.sharedMaterial?.name} | pos={r.transform.position}");
                    }
                    File.WriteAllText(Result, sb.ToString());
                    return;
                }
                throw new Exception("Unknown review command: " + command);
            }
            catch (Exception e) { File.WriteAllText(Result, e.ToString()); Debug.LogException(e); }
        }
        static void Capture(string name)
        {
            var source = Camera.main;
            if (!source) throw new Exception("Main camera missing");
            var obj = new GameObject("Village_Review_Camera");
            var camera = obj.AddComponent<Camera>(); camera.CopyFrom(source);
            camera.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            camera.aspect = 16f / 9;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var rt = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            var old = RenderTexture.active;
            try
            {
                if (name == "capture-overview")
                { camera.transform.position = new Vector3(25, 16, -16); camera.transform.LookAt(new Vector3(0,0,18)); }
                if (name == "capture-gameplay")
                { camera.transform.position = new Vector3(0, 2.6f, -10.8f); camera.transform.LookAt(new Vector3(0, 0.5f, 3.0f)); }
                if (name == "capture-reverse")
                { camera.transform.position = new Vector3(0,2,38); camera.transform.LookAt(new Vector3(0,0,0)); }
                if (name == "capture-pit3")
                { camera.transform.position = new Vector3(0.5f, 1.25f, 29.5f); camera.transform.LookAt(new Vector3(-5f, 1.5f, 55f)); }
                if (name == "capture-fence")
                { camera.transform.position = new Vector3(0, 1.0f, 34f); camera.transform.LookAt(new Vector3(0, 1.0f, 40f)); }
                if (name == "capture-leftflank")
                { camera.transform.position = new Vector3(6f, 3.5f, 8f); camera.transform.LookAt(new Vector3(-35f, 2.5f, 12f)); }
                camera.targetTexture = rt;
                var request = new UniversalRenderPipeline.SingleCameraRequest { destination = rt };
                RenderPipeline.SubmitRenderRequest(camera, request);
                RenderTexture.active = rt;
                var texture = new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                texture.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0); texture.Apply();
                File.WriteAllBytes("Logs/Village-" + name + ".png",texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }
            finally { RenderTexture.active=old; camera.targetTexture=null; rt.Release(); UnityEngine.Object.DestroyImmediate(rt); UnityEngine.Object.DestroyImmediate(obj); }
        }
    }
}
#endif

