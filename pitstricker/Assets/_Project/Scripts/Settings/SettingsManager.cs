using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using PitStriker.Audio;

namespace PitStriker.Settings
{
    public enum SettingsCategory
    {
        Audio,
        Gameplay,
        Display,
        Accessibility,
        Other
    }

    public enum GraphicsQualityLevel
    {
        Performance = 0, // 30 FPS, lighter shadow/scale
        Balanced = 1,    // 60 FPS, standard balance (default)
        HighFidelity = 2 // 60 FPS, extended shadows
    }

    /// <summary>
    /// Centralized, persistent settings manager for Pit Striker.
    /// Handles Audio levels, Gameplay vibration, Graphics quality presets, and Language.
    /// Persists all values cleanly via PlayerPrefs and applies changes immediately to the active engine.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        public static SettingsManager Instance { get; private set; }

        public const string PrefMasterVolume = "Settings.MasterVolume";
        public const string PrefMusicVolume = "Audio.MusicVolume";
        public const string PrefSfxVolume = "Audio.SfxVolume";
        public const string PrefHaptics = "Settings.Haptics";
        public const string PrefGraphicsQuality = "Settings.GraphicsQuality";
        public const string PrefLanguage = "Settings.Language";

        // Safe Defaults
        public const float DefaultMasterVolume = 1.0f;
        public const float DefaultMusicVolume = 1.0f;
        public const float DefaultSfxVolume = 1.0f;
        public const bool DefaultHaptics = true;
        public const GraphicsQualityLevel DefaultGraphicsQuality = GraphicsQualityLevel.Balanced;
        public const string DefaultLanguage = "en";

        private float _masterVolume = DefaultMasterVolume;
        private float _musicVolume = DefaultMusicVolume;
        private float _sfxVolume = DefaultSfxVolume;
        private bool _hapticsEnabled = DefaultHaptics;
        private GraphicsQualityLevel _graphicsQuality = DefaultGraphicsQuality;
        private string _language = DefaultLanguage;

        public float MasterVolume
        {
            get => _masterVolume;
            set
            {
                _masterVolume = Mathf.Clamp01(value);
                AudioListener.volume = _masterVolume;
                PlayerPrefs.SetFloat(PrefMasterVolume, _masterVolume);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.MusicVolume = _musicVolume;
                }
                else
                {
                    PlayerPrefs.SetFloat(PrefMusicVolume, _musicVolume);
                    PlayerPrefs.Save();
                }
                OnSettingsChanged?.Invoke();
            }
        }

        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.SfxVolume = _sfxVolume;
                }
                else
                {
                    PlayerPrefs.SetFloat(PrefSfxVolume, _sfxVolume);
                    PlayerPrefs.Save();
                }
                OnSettingsChanged?.Invoke();
            }
        }

        public bool HapticsEnabled
        {
            get => _hapticsEnabled;
            set
            {
                _hapticsEnabled = value;
                PlayerPrefs.SetInt(PrefHaptics, _hapticsEnabled ? 1 : 0);
                PlayerPrefs.Save();
                if (_hapticsEnabled) TriggerHapticFeedback();
                OnSettingsChanged?.Invoke();
            }
        }

        public GraphicsQualityLevel GraphicsQuality
        {
            get => _graphicsQuality;
            set
            {
                _graphicsQuality = value;
                PlayerPrefs.SetInt(PrefGraphicsQuality, (int)_graphicsQuality);
                PlayerPrefs.Save();
                ApplyGraphicsQuality();
                OnSettingsChanged?.Invoke();
            }
        }

        public string Language
        {
            get => _language;
            set
            {
                _language = value;
                PlayerPrefs.SetString(PrefLanguage, _language);
                PlayerPrefs.Save();
                OnSettingsChanged?.Invoke();
            }
        }

        public event Action OnSettingsChanged;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadSettings();
                ApplyAllSettings();
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void LoadSettings()
        {
            _masterVolume = PlayerPrefs.GetFloat(PrefMasterVolume, DefaultMasterVolume);
            _musicVolume = PlayerPrefs.GetFloat(PrefMusicVolume, DefaultMusicVolume);
            _sfxVolume = PlayerPrefs.GetFloat(PrefSfxVolume, DefaultSfxVolume);
            _hapticsEnabled = PlayerPrefs.GetInt(PrefHaptics, DefaultHaptics ? 1 : 0) == 1;
            _graphicsQuality = (GraphicsQualityLevel)PlayerPrefs.GetInt(PrefGraphicsQuality, (int)DefaultGraphicsQuality);
            _language = PlayerPrefs.GetString(PrefLanguage, DefaultLanguage);
        }

        public void ApplyAllSettings()
        {
            AudioListener.volume = _masterVolume;
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.MusicVolume = _musicVolume;
                AudioManager.Instance.SfxVolume = _sfxVolume;
            }
            ApplyGraphicsQuality();
        }

        public void ResetAudioDefaults()
        {
            MasterVolume = DefaultMasterVolume;
            MusicVolume = DefaultMusicVolume;
            SfxVolume = DefaultSfxVolume;
        }

        public void ResetAllDefaults()
        {
            ResetAudioDefaults();
            HapticsEnabled = DefaultHaptics;
            GraphicsQuality = DefaultGraphicsQuality;
            Language = DefaultLanguage;
        }

        public void TriggerHapticFeedback()
        {
            if (!_hapticsEnabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }

        private void ApplyGraphicsQuality()
        {
            switch (_graphicsQuality)
            {
                case GraphicsQualityLevel.Performance:
                    Application.targetFrameRate = 30;
                    QualitySettings.vSyncCount = 0;
                    SetUrpQuality(renderScale: 0.80f, shadowDistance: 20f);
                    break;
                case GraphicsQualityLevel.Balanced:
                    Application.targetFrameRate = 60;
                    QualitySettings.vSyncCount = 0;
                    SetUrpQuality(renderScale: 1.0f, shadowDistance: 40f);
                    break;
                case GraphicsQualityLevel.HighFidelity:
                    Application.targetFrameRate = 60;
                    QualitySettings.vSyncCount = 0;
                    SetUrpQuality(renderScale: 1.0f, shadowDistance: 60f);
                    break;
            }
        }

        private void SetUrpQuality(float renderScale, float shadowDistance)
        {
            var urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                urpAsset.renderScale = renderScale;
                urpAsset.shadowDistance = shadowDistance;
            }
        }
    }
}
