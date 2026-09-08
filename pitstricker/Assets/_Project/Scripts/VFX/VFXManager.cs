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
            _launchDustPS = CreateParticleEmitter("VFX_LaunchDust", new Color(0.85f, 0.72f, 0.5f, 0.8f), 0.15f, 0.4f, 15);
            _impactSparksPS = CreateParticleEmitter("VFX_ImpactSparks", new Color(1f, 0.9f, 0.6f, 0.9f), 0.08f, 0.25f, 20);
            _pitCelebrationPS = CreateCelebrationEmitter("VFX_PitCelebration");
        }

        private ParticleSystem CreateParticleEmitter(string name, Color color, float startSize, float lifetime, int maxParticles)
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
            main.startSpeed = 2.5f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = maxParticles;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            return ps;
        }

        private ParticleSystem CreateCelebrationEmitter(string name)
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

            return ps;
        }

        public void PlayLaunchDust(Vector3 position, Vector3 direction, float power)
        {
            if (_launchDustPS == null) return;
            _launchDustPS.transform.position = position;
            _launchDustPS.transform.rotation = Quaternion.LookRotation(-direction);
            int count = Mathf.Clamp((int)(10 + power * 20), 8, 30);
            _launchDustPS.Emit(count);
        }

        public void PlayCollisionSparks(Vector3 contactPoint, float relativeVelocity)
        {
            if (_impactSparksPS == null) return;
            _impactSparksPS.transform.position = contactPoint;
            int count = Mathf.Clamp((int)(5 + relativeVelocity * 2), 4, 20);
            _impactSparksPS.Emit(count);
        }

        public void PlayPitCelebration(Vector3 pitPosition)
        {
            if (_pitCelebrationPS == null) return;
            Vector3 pos = pitPosition;
            pos.y += 0.1f;
            _pitCelebrationPS.transform.position = pos;
            _pitCelebrationPS.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // Shoot upward like a fountain
            _pitCelebrationPS.Emit(35);
        }
    }
}
