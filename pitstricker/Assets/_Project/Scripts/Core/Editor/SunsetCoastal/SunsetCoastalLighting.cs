#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PitStriker.EditorTools
{
    /// <summary>
    /// Configures the warm, cinematic golden-hour sunset lighting matching the Phase 4 concept:
    /// - Directional Light angled from upper-left (Pitch 22°, Yaw 122°) casting diagonal palm shadows.
    /// - Warm peach & amber trilight ambient bounce.
    /// - Soft atmospheric sunset haze.
    /// - URP Volume with bloom (lanterns, torches, sun corona) and rich color saturation.
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
            // Key light from upper-left casting diagonal palm shadows across fairway towards lower-right
            light.color = new Color(1.0f, 0.78f, 0.52f);
            light.intensity = 2.2f;
            light.transform.rotation = Quaternion.Euler(22f, 122f, 0f);
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;
            light.shadowBias = 0.03f;
            light.shadowNormalBias = 0.2f;
        }

        private static void ConfigureAmbientAndFog()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.92f, 0.60f, 0.48f); // Warm peach/amber sky
            RenderSettings.ambientEquatorColor = new Color(0.85f, 0.52f, 0.38f); // Golden dusk horizon
            RenderSettings.ambientGroundColor = new Color(0.48f, 0.32f, 0.22f); // Warm sand bounce
            RenderSettings.ambientIntensity = 1.15f;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.96f, 0.72f, 0.52f);
            RenderSettings.fogDensity = 0.0018f;
        }

        private static void ConfigureCameraBackground()
        {
            RenderSettings.skybox = null;
            var camGo = GameObject.Find("Main Camera");
            var cam = camGo != null ? camGo.GetComponent<Camera>() : null;
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.96f, 0.70f, 0.48f);
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
            bloom.threshold.overrideState = true; bloom.threshold.value = 0.82f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.65f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.60f;
            bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.88f, 0.68f);

            var color = GetOrAdd<ColorAdjustments>(profile);
            color.active = true;
            color.postExposure.overrideState = true; color.postExposure.value = 0.12f;
            color.contrast.overrideState = true; color.contrast.value = 16f;
            color.saturation.overrideState = true; color.saturation.value = 25f;
            color.colorFilter.overrideState = true; color.colorFilter.value = new Color(1.0f, 0.96f, 0.92f);

            var vignette = GetOrAdd<Vignette>(profile);
            vignette.active = true;
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.18f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.35f;

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            var volGo = artRoot.transform.Find("SunsetCoastal_PostFX_Volume");
            Volume volume;
            if (volGo == null)
            {
                var go = new GameObject("SunsetCoastal_PostFX_Volume");
                go.transform.SetParent(artRoot.transform, false);
                volume = go.AddComponent<Volume>();
            }
            else
            {
                volume = volGo.GetComponent<Volume>();
            }
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.profile = profile;
        }

        private static T GetOrAdd<T>(VolumeProfile profile) where T : VolumeComponent
        {
            if (profile.TryGet<T>(out var component)) return component;
            return profile.Add<T>(true);
        }
    }
}
#endif
