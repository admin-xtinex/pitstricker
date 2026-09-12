#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Phase 6: sunset directional light, atmospheric haze fog, sunset skybox, and a
    /// scene-local (non-global-asset) Volume profile with controlled bloom/vignette/color
    /// grading. Entirely scoped to the Sunset Coastal scene's own RenderSettings/Volume —
    /// Village's lighting and its SampleSceneProfile.asset are never touched.
    /// </summary>
    public static class SunsetCoastalLighting
    {
        private const string ProfilePath = "Assets/_Project/Art/Environments/SunsetCoastal/SunsetCoastal_PostFX.asset";

        public static void Configure(GameObject artRoot)
        {
            ConfigureDirectionalLight();
            ConfigureAmbientAndFog();
            ConfigureCameraBackground();
            ConfigureVolume(artRoot);
        }

        private static void ConfigureDirectionalLight()
        {
            var lightGo = GameObject.Find("Directional Light");
            if (lightGo == null)
            {
                Debug.LogWarning("[SUNSET COASTAL LIGHTING] 'Directional Light' not found in scene.");
                return;
            }
            var light = lightGo.GetComponent<Light>();
            light.color = new Color(1.0f, 0.74f, 0.48f);
            light.intensity = 1.75f;
            light.transform.rotation = Quaternion.Euler(22f, -128f, 0f);
        }

        // Fog/ambient were muddying the vivid warm palette in live play testing —
        // pulled fog density down and shifted ambient warmer so direct-lit color
        // (sand, wood, foliage) reads punchy instead of washed-out and grey.
        private static void ConfigureAmbientAndFog()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.60f, 0.66f);
            RenderSettings.ambientEquatorColor = new Color(0.68f, 0.52f, 0.40f);
            RenderSettings.ambientGroundColor = new Color(0.34f, 0.28f, 0.24f);
            RenderSettings.ambientIntensity = 0.85f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.90f, 0.74f, 0.60f);
            RenderSettings.fogDensity = 0.0040f;
        }

        /// <summary>
        /// "Skybox/Procedural" is a legacy Built-in-RP shader with no UniversalPipeline
        /// tag, so URP renders it as an invalid/magenta shader even though it resolves
        /// fine via Shader.Find. A flat solid-color background avoids that pipeline
        /// mismatch entirely; the SunsetCloud/AtmosphericHaze impostor cards (Phase 5)
        /// already carry the actual sky detail, per the doc's own layering approach.
        /// </summary>
        private static void ConfigureCameraBackground()
        {
            RenderSettings.skybox = null;
            var camGo = GameObject.Find("Main Camera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.93f, 0.66f, 0.48f);
        }

        private static void ConfigureVolume(GameObject artRoot)
        {
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, ProfilePath);
            }

            var bloom = GetOrAdd<Bloom>(profile);
            bloom.active = true;
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.24f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.55f;
            bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.85f, 0.65f);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.overrideState = true; color.postExposure.value = 0.15f;
            color.contrast.overrideState = true; color.contrast.value = 14f;
            color.saturation.overrideState = true; color.saturation.value = 22f;
            color.colorFilter.overrideState = true; color.colorFilter.value = new Color(1.0f, 0.95f, 0.90f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.20f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.6f;

            var tonemap = GetOrAdd<Tonemapping>(profile);
            tonemap.active = true;
            tonemap.mode.overrideState = true; tonemap.mode.value = TonemappingMode.ACES;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volumeGo = new GameObject("SunsetCoastal_Volume");
            volumeGo.transform.SetParent(artRoot.transform, false);
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (!profile.TryGet<T>(out var component))
            {
                component = profile.Add<T>(true);
            }
            return component;
        }
    }
}
#endif
