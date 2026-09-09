#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering.Universal;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace PitStriker.EditorTools
{
    [InitializeOnLoad]
    public static class MobileVisualRepair
    {
        static MobileVisualRepair(){ EditorApplication.update+=Tick; }
        static void Tick()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode) return;
            const string request="Library/MobileVisual.request";
            if(!File.Exists(request)) return;
            string action=File.ReadAllText(request).Trim(); File.Delete(request);
            try
            {
                if(action=="capture")
                {
                    QualitySettings.SetQualityLevel(0,true);
                    var camera=Camera.main;
                    VillageGraphicsIntegration.Capture(camera);
                    var renderers=UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude);
                    File.WriteAllText("Library/MobileVisual.result",string.Join("\n",renderers.Where(r=>r.name.Contains("Plaster")||r.name.Contains("Bark")).Select(r=>r.name+" enabled="+r.enabled+" forceOff="+r.forceRenderingOff+" shader="+r.sharedMaterial.shader.name+" pass="+r.sharedMaterial.GetShaderPassEnabled("ForwardLit"))));
                }
                else if(action=="apply") { Configure(); VillageGraphicsIntegration.Integrate(); File.WriteAllText("Library/MobileVisual.result","Applied"); }
                else if(action=="build") BuildApk();
            }
            catch(Exception e){File.WriteAllText("Library/MobileVisual.result",e.ToString()); Debug.LogException(e);}
        }
        public static void Configure()
        {
            var pipeline=AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Settings/Mobile_RPAsset.asset");
            pipeline.renderScale=1f;pipeline.msaaSampleCount=2;pipeline.shadowCascadeCount=2;
            pipeline.mainLightShadowmapResolution=2048;
            var pipelineSo=new SerializedObject(pipeline);pipelineSo.FindProperty("m_SoftShadowsSupported").boolValue=true;pipelineSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipeline);
            QualitySettings.SetQualityLevel(0,true);
            const string texturePath="Assets/_Project/Art/Environments/Village/Reference_Dirt_Albedo.png";
            var textureImporter=AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if(textureImporter){textureImporter.wrapMode=TextureWrapMode.Repeat;textureImporter.anisoLevel=8;textureImporter.mipmapEnabled=true;textureImporter.maxTextureSize=2048;textureImporter.SaveAndReimport();}
            const string iconPath="Assets/_Project/Art/AppIcon/PitStriker_Icon.png";
            var importer=AssetImporter.GetAtPath(iconPath) as TextureImporter;
            if(importer){importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.SaveAndReimport();}
            var icon=AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
            if(!icon)throw new InvalidOperationException("App icon has not imported.");
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown,new[]{icon},IconKind.Any);
            foreach(var kind in PlayerSettings.GetSupportedIconKinds(NamedBuildTarget.Android))
            {
                if(kind.ToString().ToLowerInvariant().Contains("monochrome"))continue;
                var icons=PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android,kind);
                foreach(var slot in icons)
                {
                    var textures=new Texture2D[slot.maxLayerCount];
                    for(int i=0;i<slot.minLayerCount;i++)textures[i]=icon;
                    slot.SetTextures(textures);
                }
                PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android,kind,icons);
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Pit Striker/Build Updated Android APK")]
        public static void BuildApk()
        {
            Configure();
            string output=Path.GetFullPath("../Builds/PitStriker-graphics-physics.apk");
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            EditorUserBuildSettings.buildAppBundle=false;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes=new[]{VillageGraphicsIntegration.Destination},locationPathName=output,
                target=BuildTarget.Android,options=BuildOptions.None
            });
            File.WriteAllText("Library/MobileBuild.result",report.summary.result+"\n"+output+"\nErrors="+report.summary.totalErrors+"; bytes="+report.summary.totalSize);
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Android build failed. See MobileBuild.result and Editor log.");
        }
    }
}
#endif
