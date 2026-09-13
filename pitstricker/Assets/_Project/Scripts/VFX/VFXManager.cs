using UnityEngine;

namespace PitStriker.VFX
{
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        private ParticleSystem _launchDustPS;
        private ParticleSystem _impactSparksPS;
        private ParticleSystem _pitCelebrationPS;
        private ParticleSystem _rollDustPS;
        private ParticleSystem _scoreBurstPS;

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
            _pitCelebrationPS = CreateCelebrationEmitter("VFX_PitCelebration", sparkMat);
            _rollDustPS = CreateParticleEmitter("VFX_RollDust", new Color(0.62f, 0.42f, 0.22f, 0.45f), 0.08f, 0.28f, 18, dustMat);
            _scoreBurstPS = CreateCelebrationEmitter("VFX_ScoreBurst", sparkMat);
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

        private ParticleSystem CreateCelebrationEmitter(string name, Material mat)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(transform);
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.1f, 1f), new Color(0f, 0.9f, 1f, 1f));
            main.startSize = 0.12f;
            main.startLifetime = 1.0f;
            main.startSpeed = 5.0f;
            main.gravityModifier = 0.8f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 50;
            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.25f;

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
            int count = Mathf.Clamp((int)(8 + power * 16), 6, 24);
            _launchDustPS.Emit(count);
        }

        public void PlayCollisionSparks(Vector3 contactPoint, float relativeVelocity)
        {
            if (_impactSparksPS == null) return;
            _impactSparksPS.transform.position = contactPoint;
            int count = Mathf.Clamp((int)(4 + relativeVelocity * 2), 3, 16);
            _impactSparksPS.Emit(count);
        }

        public void PlayPitCelebration(Vector3 pitPosition)
        {
            if (_pitCelebrationPS == null) return;
            _pitCelebrationPS.transform.position = pitPosition + Vector3.up * 0.1f;
            _pitCelebrationPS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            _pitCelebrationPS.Emit(48);
        }

        public void PlayRollDust(Vector3 position, Vector3 direction, float power)
        {
            if (_rollDustPS == null) return;
            _rollDustPS.transform.position = position + Vector3.up * 0.02f;
            if (direction.sqrMagnitude > 0.01f)
                _rollDustPS.transform.rotation = Quaternion.LookRotation(-direction);
            _rollDustPS.Emit(Mathf.Clamp((int)(3 + power * 8), 2, 10));
        }

        public void PlayScoreBurst(Vector3 pitPosition)
        {
            if (_scoreBurstPS == null) return;
            _scoreBurstPS.transform.position = pitPosition + Vector3.up * 0.2f;
            _scoreBurstPS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);
            _scoreBurstPS.Emit(28);
        }
    }
}
