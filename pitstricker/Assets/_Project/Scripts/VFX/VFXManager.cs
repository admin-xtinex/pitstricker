using UnityEngine;

namespace PitStriker.VFX
{
    /// <summary>
    /// Phase 7 Visual Juice Orchestrator:
    /// Manages procedural particle bursts for launches, collision impacts,
    /// and golden pit capture celebrations.
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        private ParticleSystem _launchDustPS;
        private ParticleSystem _impactSparksPS;
        private ParticleSystem _pitCelebrationPS;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeParticleSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeParticleSystems()
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (particleShader == null) particleShader = Shader.Find("Particles/Standard Unlit");

            Material dustMat = particleShader != null ? new Material(particleShader) : null;
            if (dustMat != null)
            {
                dustMat.color = new Color(0.82f, 0.72f, 0.54f, 0.6f);
            }

            Material sparkMat = particleShader != null ? new Material(particleShader) : null;
            if (sparkMat != null)
            {
                sparkMat.color = new Color(1.0f, 0.92f, 0.45f, 0.8f);
            }

            _launchDustPS = CreateParticleEmitter("VFX_LaunchDust", new Color(0.82f, 0.72f, 0.54f, 0.6f), 0.12f, 0.35f, 15, dustMat);
            _impactSparksPS = CreateParticleEmitter("VFX_ImpactSparks", new Color(1f, 0.92f, 0.45f, 0.85f), 0.06f, 0.20f, 15, sparkMat);
        }

        private ParticleSystem CreateParticleEmitter(string name, Color color, float startSize, float lifetime, int maxParticles, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startColor = color;
            main.startSize = startSize;
            main.startLifetime = lifetime;
            main.startSpeed = 2.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
            if (psr != null && mat != null)
            {
                psr.sharedMaterial = mat;
            }

            return ps;
        }

        public void PlayLaunchDust(Vector3 position, Vector3 direction, float power)
        {
            if (_launchDustPS == null) return;
            _launchDustPS.transform.position = position;
            _launchDustPS.transform.rotation = Quaternion.LookRotation(-direction);
            int count = Mathf.Clamp((int)(6 + power * 12), 4, 16);
            _launchDustPS.Emit(count);
        }

        public void PlayCollisionSparks(Vector3 contactPoint, float relativeVelocity)
        {
            if (_impactSparksPS == null) return;
            _impactSparksPS.transform.position = contactPoint;
            int count = Mathf.Clamp((int)(3 + relativeVelocity * 2), 3, 12);
            _impactSparksPS.Emit(count);
        }

        public void PlayPitCelebration(Vector3 pitPosition)
        {
            // Untextured purple celebration particle burst has been completely removed.
            // Celebratory feedback is now elegantly handled by StylizedPitFeedback (golden shockwave ripple & lip bounce)
            // and StylizedFlagAnimation (joyful 360 victory spin).
        }
    }
}
