using UnityEngine;

namespace PitStriker.Audio
{
    /// <summary>
    /// Phase 7 Audio Juice Orchestrator:
    /// Synthesizes and plays tactile sound effects for launches, marble collisions,
    /// boundary bounces, and pit capture fanfares without external audio dependencies.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private AudioSource _sfxSource;
        private AudioSource _jingleSource;

        // Procedural Audio Clips
        private AudioClip _strikeClip;
        private AudioClip _glassClackClip;
        private AudioClip _woodThudClip;
        private AudioClip _pitSinkClip;
        private AudioClip _victoryFanfareClip;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudio();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeAudio()
        {
            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f; // 2D clean audio

            _jingleSource = gameObject.AddComponent<AudioSource>();
            _jingleSource.playOnAwake = false;
            _jingleSource.spatialBlend = 0f;

            GenerateProceduralClips();
        }

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
