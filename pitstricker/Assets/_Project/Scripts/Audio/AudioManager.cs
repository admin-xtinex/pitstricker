using System.Collections;
using UnityEngine;

namespace PitStriker.Audio
{
    /// <summary>
    /// Phase 7 Audio Juice Orchestrator:
    /// Synthesizes and plays tactile sound effects for launches, marble collisions,
    /// boundary bounces, pit capture fanfares, and background music orchestration
    /// (Menu theme, Toss phase theme, and In-Game match theme).
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
            private set => _instance = value;
        }

        [Header("Music Configuration (50% Volume)")]
        [Range(0f, 1f)]
        [SerializeField] private float _musicVolume = 0.5f; // 50% sound level required
        [SerializeField] private AudioClip _startMusicClip;
        [SerializeField] private AudioClip _tossMusicClip;
        [SerializeField] private AudioClip _gameplayMusicClip;

        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                if (_musicSource != null && _fadeCoroutine == null)
                {
                    _musicSource.volume = _musicVolume;
                }
            }
        }

        public AudioClip StartMusicClip { get => _startMusicClip; set => _startMusicClip = value; }
        public AudioClip TossMusicClip { get => _tossMusicClip; set => _tossMusicClip = value; }
        public AudioClip GameplayMusicClip { get => _gameplayMusicClip; set => _gameplayMusicClip = value; }

        private AudioSource _musicSource;
        private AudioSource _sfxSource;
        private AudioSource _jingleSource;
        private Coroutine _fadeCoroutine;

        // Procedural Audio Clips
        private AudioClip _strikeClip;
        private AudioClip _glassClackClip;
        private AudioClip _woodThudClip;
        private AudioClip _pitSinkClip;
        private AudioClip _victoryFanfareClip;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // Background music is triggered on demand by MenuManager and TurnManager flows
        }

        private void InitializeAudio()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
                _musicSource.playOnAwake = false;
                _musicSource.spatialBlend = 0f; // 2D clean stereo audio
                _musicSource.loop = true;
                _musicSource.volume = _musicVolume;
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
                _sfxSource.playOnAwake = false;
                _sfxSource.spatialBlend = 0f; // 2D clean audio
            }

            if (_jingleSource == null)
            {
                _jingleSource = gameObject.AddComponent<AudioSource>();
                _jingleSource.playOnAwake = false;
                _jingleSource.spatialBlend = 0f;
            }

            LoadClips();
            GenerateProceduralClips();
        }

        private void LoadClips()
        {
            if (_startMusicClip == null)
            {
                _startMusicClip = Resources.Load<AudioClip>("Music/Music_Start");
            }
            if (_tossMusicClip == null)
            {
                _tossMusicClip = Resources.Load<AudioClip>("Music/Music_Toss");
            }
            if (_gameplayMusicClip == null)
            {
                _gameplayMusicClip = Resources.Load<AudioClip>("Music/Music_Gameplay");
            }
        }

        // ================= MUSIC CONTROLLER =================

        /// <summary>
        /// Plays the Menu / Start music track at 50% volume with optional smooth crossfade.
        /// </summary>
        public void PlayStartMusic(bool fade = true)
        {
            if (_startMusicClip == null)
            {
                _startMusicClip = Resources.Load<AudioClip>("Music/Music_Start");
            }
            PlayMusicTrack(_startMusicClip, fade);
        }

        /// <summary>
        /// Plays the Toss phase music track at 50% volume with optional smooth crossfade.
        /// </summary>
        public void PlayTossMusic(bool fade = true)
        {
            if (_tossMusicClip == null)
            {
                _tossMusicClip = Resources.Load<AudioClip>("Music/Music_Toss");
            }
            PlayMusicTrack(_tossMusicClip, fade);
        }

        /// <summary>
        /// Plays the In-Game Match background music track (after the toss) at 50% volume with optional smooth crossfade.
        /// </summary>
        public void PlayGameplayMusic(bool fade = true)
        {
            if (_gameplayMusicClip == null)
            {
                _gameplayMusicClip = Resources.Load<AudioClip>("Music/Music_Gameplay");
            }
            PlayMusicTrack(_gameplayMusicClip, fade);
        }

        /// <summary>
        /// Plays a specified audio clip on the background music channel at 50% volume.
        /// </summary>
        public void PlayMusicTrack(AudioClip clip, bool fade = true)
        {
            if (_musicSource == null)
            {
                InitializeAudio();
            }

            if (clip == null) return;

            if (_musicSource.clip == clip && _musicSource.isPlaying)
            {
                // Already playing target track; ensure volume matches configuration
                if (_fadeCoroutine == null)
                {
                    _musicSource.volume = _musicVolume;
                }
                return;
            }

            if (!fade)
            {
                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                    _fadeCoroutine = null;
                }
                _musicSource.clip = clip;
                _musicSource.volume = _musicVolume;
                _musicSource.loop = true;
                _musicSource.Play();
            }
            else
            {
                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                }
                _fadeCoroutine = StartCoroutine(CrossfadeRoutine(clip, 0.6f));
            }
        }

        /// <summary>
        /// Smoothly stops music playback.
        /// </summary>
        public void StopMusic(bool fade = true)
        {
            if (_musicSource == null || !_musicSource.isPlaying) return;

            if (!fade)
            {
                if (_fadeCoroutine != null)
                {
                    StopCoroutine(_fadeCoroutine);
                    _fadeCoroutine = null;
                }
                _musicSource.Stop();
            }
            else
            {
                if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = StartCoroutine(FadeOutRoutine(0.4f));
            }
        }

        private IEnumerator CrossfadeRoutine(AudioClip targetClip, float duration)
        {
            float halfDuration = Mathf.Max(0.01f, duration * 0.5f);
            float startVol = _musicSource.isPlaying ? _musicSource.volume : 0f;

            if (_musicSource.isPlaying && startVol > 0.01f)
            {
                float t = 0f;
                while (t < halfDuration)
                {
                    t += Time.unscaledDeltaTime;
                    _musicSource.volume = Mathf.Lerp(startVol, 0f, t / halfDuration);
                    yield return null;
                }
            }

            _musicSource.clip = targetClip;
            _musicSource.loop = true;
            _musicSource.Play();

            float tIn = 0f;
            while (tIn < halfDuration)
            {
                tIn += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(0f, _musicVolume, tIn / halfDuration);
                yield return null;
            }

            _musicSource.volume = _musicVolume;
            _fadeCoroutine = null;
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            float startVol = _musicSource.volume;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                _musicSource.volume = Mathf.Lerp(startVol, 0f, t / duration);
                yield return null;
            }
            _musicSource.Stop();
            _musicSource.volume = _musicVolume;
            _fadeCoroutine = null;
        }

        // ================= PROCEDURAL SOUND EFFECTS =================

        private void GenerateProceduralClips()
        {
            _strikeClip = GenerateStrikeTone(44100);
            _glassClackClip = GenerateGlassClackTone(44100);
            _woodThudClip = GenerateWoodThudTone(44100);
            _pitSinkClip = GenerateSinkChimeTone(44100);
            _victoryFanfareClip = GenerateVictoryFanfareTone(44100);
        }

        public void PlayLaunch(float power01)
        {
            if (_sfxSource == null || _strikeClip == null) return;
            _sfxSource.pitch = Random.Range(0.95f, 1.15f) + (power01 * 0.2f);
            float vol = Mathf.Clamp(0.4f + power01 * 0.6f, 0.2f, 1f);
            _sfxSource.PlayOneShot(_strikeClip, vol);
        }

        public void PlayCollision(float relativeVelocity, bool isMarble)
        {
            if (_sfxSource == null) return;

            float speedNormalized = Mathf.Clamp01(relativeVelocity / 15.0f);
            if (speedNormalized < 0.05f) return; // Ignore micro-jitters

            if (isMarble && _glassClackClip != null)
            {
                _sfxSource.pitch = Random.Range(0.92f, 1.12f);
                _sfxSource.PlayOneShot(_glassClackClip, Mathf.Clamp01(0.3f + speedNormalized * 0.7f));
            }
            else if (_woodThudClip != null)
            {
                _sfxSource.pitch = Random.Range(0.85f, 1.05f);
                _sfxSource.PlayOneShot(_woodThudClip, Mathf.Clamp01(0.25f + speedNormalized * 0.6f));
            }
        }

        public void PlayPitSink()
        {
            if (_jingleSource == null || _pitSinkClip == null) return;
            _jingleSource.pitch = 1.0f;
            _jingleSource.PlayOneShot(_pitSinkClip, 0.85f);
        }

        public void PlayVictory()
        {
            if (_jingleSource == null || _victoryFanfareClip == null) return;
            _jingleSource.pitch = 1.0f;
            _jingleSource.PlayOneShot(_victoryFanfareClip, 1.0f);
        }

        // ================= PROCEDURAL AUDIO SYNTHESIZERS =================

        private AudioClip GenerateStrikeTone(int sampleRate)
        {
            float duration = 0.09f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 55f);
                float wave = Mathf.Sin(2f * Mathf.PI * 520f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1100f * t) * 0.3f;
                // Add tiny white noise click transient
                float noise = (Random.value * 2f - 1f) * Mathf.Exp(-t * 220f) * 0.3f;
                data[i] = (wave + noise) * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_Strike", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateGlassClackTone(int sampleRate)
        {
            float duration = 0.07f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 80f);
                float wave = Mathf.Sin(2f * Mathf.PI * 2200f * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * 3400f * t) * 0.3f;
                float click = (Random.value * 2f - 1f) * Mathf.Exp(-t * 300f) * 0.2f;
                data[i] = (wave + click) * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_GlassClack", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateWoodThudTone(int sampleRate)
        {
            float duration = 0.12f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 35f);
                float wave = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.8f;
                data[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_WoodThud", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateSinkChimeTone(int sampleRate)
        {
            float duration = 0.45f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[] frequencies = new float[] { 523.25f, 659.25f, 783.99f }; // C5, E5, G5
            float noteDuration = duration / 3f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, 2);
                float noteTime = t - (noteIndex * noteDuration);

                float freq = frequencies[noteIndex];
                float decay = Mathf.Exp(-noteTime * 12f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * noteTime);
                data[i] = wave * decay * 0.7f;
            }

            AudioClip clip = AudioClip.Create("SFX_PitSinkChime", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        private AudioClip GenerateVictoryFanfareTone(int sampleRate)
        {
            float duration = 0.85f;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];

            float[] notes = new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
            float noteDuration = duration / 4f;

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                int noteIndex = Mathf.Clamp((int)(t / noteDuration), 0, 3);
                float noteTime = t - (noteIndex * noteDuration);

                float freq = notes[noteIndex];
                float decay = Mathf.Exp(-noteTime * 9f);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * noteTime) * 0.7f + Mathf.Sin(2f * Mathf.PI * freq * 2f * noteTime) * 0.2f;
                data[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("SFX_VictoryFanfare", samples, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
