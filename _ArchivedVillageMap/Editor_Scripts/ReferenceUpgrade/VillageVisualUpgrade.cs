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
    public static class VillageVisualUpgrade
    {
        internal const string RootName = "Village_Reference_Upgrade";
        const string VillageFolder = "Assets/_Project/Art/Environments/Village/";

        [MenuItem("Pit Striker/Graphics/Apply Full Village Upgrade")]
        public static void ApplyToOpenScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before applying and saving the village upgrade.");
            var scene=SceneManager.GetActiveScene();
            if(scene.path!=VillageGraphicsIntegration.Destination)
                throw new InvalidOperationException("Open SC_Village_Graphics_Test before applying the village graphics upgrade.");
            var graphics=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Environment_Blender_Village_Graphics");
            if(!graphics) throw new InvalidOperationException("Create the village graphics test scene first.");
            Apply(scene,graphics);
            EditorSceneManager.MarkSceneDirty(scene);
            AssetDatabase.SaveAssets();
            // Deliberately leave the scene open and dirty for visual review and Ctrl+S.
            Debug.Log("Village upgrade applied. Review the Game view, run Validate Village Upgrade, then save the scene.");
        }

        // Use with Unity -batchmode -quit -projectPath ... -executeMethod
        // PitStriker.EditorTools.VillageVisualUpgrade.RunBatch
        public static void RunBatch()
        {
            var scene=EditorSceneManager.OpenScene(VillageGraphicsIntegration.Destination,OpenSceneMode.Single);
            ApplyToOpenScene();
            if(!EditorSceneManager.SaveScene(scene))throw new IOException("Could not save the upgraded village scene.");
            AssetDatabase.SaveAssets();
        }

        public static void Apply(Scene scene,GameObject graphics)
        {
            string before=GameplaySnapshot(scene);
            VillageSurfaceLibrary.EnsureFolder();
            var previous=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(previous && (previous.GetComponentsInChildren<Collider>(true).Length>0 ||
                previous.GetComponentsInChildren<Rigidbody>(true).Length>0 || previous.GetComponentsInChildren<MonoBehaviour>(true).Length>0))
                throw new InvalidOperationException("The generated scenery root contains manually added gameplay components. Move them outside the graphics root before regenerating.");
            if(previous) UnityEngine.Object.DestroyImmediate(previous);
            var signsToDestroy = scene.GetRootGameObjects()
                .SelectMany(g => g.GetComponentsInChildren<Transform>(true))
                .Where(t => t != null && (t.name == "Rustic_Signpost_Hero" || t.name == "Reference_Signs" || t.name == "HouseWall_Sticker"))
                .Select(t => t.gameObject)
                .Distinct()
                .ToArray();
            foreach (var go in signsToDestroy)
            {
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            }
            var root=new GameObject(RootName); SceneManager.MoveGameObjectToScene(root,scene);
            try
            {
                var soil=VillageSurfaceLibrary.Surface("Soil",new Color(.48f,.35f,.20f),new Color(.72f,.58f,.38f),1.6f,true);
                soil.SetFloat("_VergeStrength",1);
                var pitSoil=VillageSurfaceLibrary.Material("PitSoil","Pit Striker/Village Ground",Color.white);
                pitSoil.CopyPropertiesFromMaterial(soil);
                pitSoil.SetFloat("_VergeStrength",0);
                pitSoil.SetColor("_BaseColor",new Color(.7f,.65f,.57f));
                EditorUtility.SetDirty(pitSoil);
                var bark=VillageSurfaceLibrary.Surface("Bark",new Color(.28f,.20f,.14f),new Color(.46f,.35f,.24f),2.2f);
                var wood=VillageSurfaceLibrary.Surface("Wood",new Color(.26f,.18f,.11f),new Color(.43f,.32f,.21f),1.5f);
                var plaster=VillageSurfaceLibrary.Surface("Plaster",new Color(.65f,.60f,.49f),new Color(.76f,.71f,.59f),.5f);
                var stone=VillageSurfaceLibrary.Surface("Stone",new Color(.17f,.18f,.14f),new Color(.46f,.44f,.35f),3);
                var roof=VillageSurfaceLibrary.Surface("Roof",new Color(.15f,.055f,.025f),new Color(.43f,.20f,.10f),2);
                var grass=VillageSurfaceLibrary.Material("Meadow","Pit Striker/Village Foliage",new Color(.38f,.52f,.12f));
                var canopy=VillageSurfaceLibrary.Foliage("Canopy",new Color(.24f,.42f,.09f),new Color(.52f,.72f,.16f));
                var palmFoliage=VillageSurfaceLibrary.Foliage("Palm",new Color(.26f,.45f,.10f),new Color(.58f,.78f,.20f));
                var mountain=VillageSurfaceLibrary.Material("Mountain","Pit Striker/Village Surface",new Color(.18f,.28f,.26f));
                mountain.SetFloat("_Smoothness",0);
                grass.SetFloat("_AmbientFill",.55f);
                grass.SetFloat("_Transmission",.35f);
                VillageVegetationBuilder.Build(root.transform,grass,canopy,palmFoliage,bark);
                VillageSceneryDetails.Build(root.transform,wood,plaster,stone,soil,mountain,canopy);
                VillageFinishingPass.Build(root.transform,wood,canopy);

                foreach(var renderer in graphics.GetComponentsInChildren<Renderer>(true))
                {
                    // Source batches mix palms and coarse grass. Replace their foliage and trunks together.
                    // Preserve and explicitly enable Detail_Mountain: it is the authentic low-poly mountain backdrop from original reference.
                    bool replaced=renderer.name.StartsWith("Foliage_")||renderer.name.StartsWith("Detail_Bark")||
                        renderer.name.StartsWith("Detail_Earth");
                    if(replaced) renderer.enabled=false;
                    if(renderer.name.StartsWith("Detail_Mountain")) renderer.enabled=true;
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                    {
                        if(!m) return m;
                        if(m.name.StartsWith("Detail_Wood"))return wood;
                        if(m.name.StartsWith("Detail_Plaster"))return plaster;
                        if(m.name.StartsWith("Detail_Stone"))return stone;
                        if(m.name.StartsWith("Detail_Terracotta"))return roof;
                        if(m.name.StartsWith("Detail_Mountain"))return mountain;
                        return m;
                    }).ToArray();
                }
                // Rebind only existing soil materials; do not change the pit's mesh or collider.
                foreach(var renderer in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Renderer>(true)))
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>
                    {
                        if(m.name=="Reference_Soil")return soil;
                        if(m.name=="M_Pit_DarkRed"||m.name=="M_Pit_BrightRed"||m.name=="Reference_PitRed")return pitSoil;
                        return m;
                    }).ToArray();

                VillageReferencePolish.Apply(scene,graphics);
                // House wall sign removed per user request
                TuneLighting(scene);
                AddReflectionProbe(root);
                if(before!=GameplaySnapshot(scene)) throw new InvalidOperationException("Graphics upgrade changed protected gameplay state.");
                Validate(scene);
            }
            catch
            {
                Debug.LogError("Village upgrade failed. Do not save the scene; reopen its saved version before retrying.");
                throw;
            }
        }

        static void TuneLighting(Scene scene)
        {
            RenderSettings.ambientMode=AmbientMode.Trilight;
            RenderSettings.ambientSkyColor=new Color(.58f,.70f,.88f);
            RenderSettings.ambientEquatorColor=new Color(.54f,.64f,.40f);
            RenderSettings.ambientGroundColor=new Color(.40f,.32f,.20f);
            RenderSettings.fogColor=new Color(.72f,.80f,.86f); RenderSettings.fogDensity=.0034f;
            if(RenderSettings.sun)
            {
                RenderSettings.sun.transform.rotation=Quaternion.Euler(34,-45,0);
                RenderSettings.sun.color=new Color(1f,.95f,.82f); RenderSettings.sun.intensity=2.6f;
                RenderSettings.sun.shadowNormalBias=.08f; RenderSettings.sun.shadowBias=.015f;
                // Match the sky's sun disk to the actual key light direction.
                if(RenderSettings.skybox&&RenderSettings.skybox.HasProperty("_SunDirection"))
                {
                    Vector3 d=-RenderSettings.sun.transform.forward;
                    RenderSettings.skybox.SetVector("_SunDirection",new Vector4(d.x,d.y,d.z,0));
                    EditorUtility.SetDirty(RenderSettings.skybox);
                }
            }
            var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(VillageFolder+"Reference_Grade.asset");
            if(profile&&profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.postExposure.Override(.35f);grade.contrast.Override(12);grade.saturation.Override(10);
                EditorUtility.SetDirty(grade);EditorUtility.SetDirty(profile);
            }
            foreach(var camera in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Camera>(true)))
            {
                if(!camera.CompareTag("MainCamera"))continue;
                camera.allowHDR=true;
                camera.fieldOfView=55;
                var follow=camera.GetComponent<PitStriker.CameraSystem.SmoothFollowCamera>();
                if(follow)
                {
                    var settings=new SerializedObject(follow);
                    settings.FindProperty("_height").floatValue=1.05f;
                    settings.FindProperty("_distance").floatValue=3.2f;
                    settings.FindProperty("_lookAtHeightOffset").floatValue=.2f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    var target=settings.FindProperty("_target").objectReferenceValue as Transform;
                    if(!target)
                        target=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<PitStriker.Physics.MarbleController>(true))
                            .OrderBy(m=>m.name).Select(m=>m.transform).FirstOrDefault();
                    if(target)
                    {
                        camera.transform.position=target.position+new Vector3(0,1.05f,-3.2f);
                        camera.transform.LookAt(target.position+new Vector3(0,.2f,1.5f));
                    }
                }
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=true;
            }
            DynamicGI.UpdateEnvironment();
        }

        static void AddReflectionProbe(GameObject root)
        {
            var obj=new GameObject("Village_Reflection");obj.transform.SetParent(root.transform,false);
            obj.transform.position=new Vector3(0,1.8f,14);
            var probe=obj.AddComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.IndividualFaces;
            probe.resolution=128;probe.size=new Vector3(42,18,75);probe.boxProjection=true;
            probe.nearClipPlane=.2f;probe.farClipPlane=130;probe.intensity=1;
            // One time-sliced capture at startup; no per-frame probe updates on mobile.
        }

        internal static string GameplaySnapshot(Scene scene)
        {
            return string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Component>(true))
                .Where(c=>c&&(c is Collider||c is Rigidbody||c is PitStriker.Physics.MarbleController||
                    c is PitStriker.Gameplay.PitZone||c is PitStriker.Gameplay.TurnManager||
                    c is PitStriker.Input.SwipeLaunchController))
                .OrderBy(c=>c.GetEntityId().ToString(),StringComparer.Ordinal)
                .Select(c=>c.GetEntityId().ToString()+":"+EditorJsonUtility.ToJson(c)+":"+EditorJsonUtility.ToJson(c.transform)));
        }

        [MenuItem("Pit Striker/Graphics/Validate Village Upgrade")]
        public static void ValidateOpenScene() => Validate(SceneManager.GetActiveScene());

        internal static void Validate(Scene scene)
        {
            var root=scene.GetRootGameObjects().FirstOrDefault(g=>g.name==RootName);
            if(!root)throw new InvalidOperationException("Village upgrade root is missing.");
            if(root.GetComponentsInChildren<Collider>(true).Length!=0||root.GetComponentsInChildren<Rigidbody>(true).Length!=0)
                throw new InvalidOperationException("Decorative scenery contains physics components.");
            long triangles=0;
            foreach(var filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                var mesh=filter.sharedMesh;
                if(!mesh||mesh.vertexCount==0)throw new InvalidOperationException("Empty generated mesh: "+filter.name);
                foreach(var p in mesh.vertices)
                    if(float.IsNaN(p.x)||float.IsInfinity(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.y)||float.IsNaN(p.z)||float.IsInfinity(p.z))
                        throw new InvalidOperationException("Non-finite vertex: "+filter.name);
                var points=mesh.vertices;var indices=mesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                    if(Vector3.Cross(points[indices[i+1]]-points[indices[i]],points[indices[i+2]]-points[indices[i]]).sqrMagnitude<1e-14f)
                        throw new InvalidOperationException("Degenerate triangle: "+filter.name);
                triangles+=indices.Length/3;
                var renderer=filter.GetComponent<Renderer>();
                foreach(var material in renderer.sharedMaterials)
                {
                    if(!material||!material.shader||!material.shader.isSupported)throw new InvalidOperationException("Invalid material: "+filter.name);
                    if(ShaderUtil.GetShaderMessages(material.shader).Any(m=>m.severity.ToString()=="Error"))
                        throw new InvalidOperationException("Shader compilation error: "+material.shader.name);
                }
            }
            foreach(var group in root.GetComponentsInChildren<LODGroup>())
                if(group.GetLODs().Any(l=>l.renderers.Any(r=>!r)))throw new InvalidOperationException("Missing LOD renderer: "+group.name);
            if(triangles>450000)throw new InvalidOperationException("Generated all-LOD geometry exceeds the 450k triangle review budget.");
            File.WriteAllText("Library/VillageUpgradeValidation.txt","PASS: generated geometry, materials, LODs and decorative-only structure.\nAll-LOD triangles="+triangles+"\nAndroid GPU profiling and visual approval still required.");
            Debug.Log("Village graphics structure validated. All-LOD triangles="+triangles+". Profile the target device before release.");
        }

        static void RestoreHouseWallSign(Scene scene, Material plasterMat)
        {
            var signObj = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "Reference_Signs");
            if (!signObj)
            {
                signObj = new GameObject("Reference_Signs");
                SceneManager.MoveGameObjectToScene(signObj, scene);
            }
            // Positioned exactly on the front wall of the authentic Blender house
            signObj.transform.position = new Vector3(-6.70f, 1.70f, 6.28f);

            var plaque = signObj.transform.Find("HouseWall_Sticker");
            if (!plaque)
            {
                var pGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pGo.name = "HouseWall_Sticker";
                pGo.transform.SetParent(signObj.transform, false);
                pGo.transform.localPosition = Vector3.zero;
                pGo.transform.localScale = new Vector3(1.6f, 0.7f, 0.02f);
                UnityEngine.Object.DestroyImmediate(pGo.GetComponent<Collider>());
                if (plasterMat) pGo.GetComponent<Renderer>().sharedMaterial = plasterMat;
            }

            var textObj = signObj.transform.Find("HouseWall_StickerText");
            if (!textObj)
            {
                var tGo = new GameObject("HouseWall_StickerText");
                tGo.transform.SetParent(signObj.transform, false);
                tGo.transform.localPosition = new Vector3(0, 0, -0.015f);
                var tm = tGo.AddComponent<TextMesh>();
                tm.text = "PIT\nSTRIKER";
                tm.characterSize = 0.035f;
                tm.fontSize = 64;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.fontStyle = FontStyle.Bold;
                tm.color = new Color(0.26f, 0.18f, 0.11f);
            }
        }
    }
}
#endif
