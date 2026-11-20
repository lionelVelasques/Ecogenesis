using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Lluvia breve cada vez que la naturaleza se recupera un tramo más (transición visual al crecer la vegetación).
    /// Se crea sola al cargar cada escena y sigue al jugador. No se dispara con la salud inicial del nivel.
    /// </summary>
    public class RainTransition : MonoBehaviour
    {
        [Tooltip("Cuánta salud (0-1) debe ganar el parque para que vuelva a llover.")]
        [SerializeField, Range(0.02f, 0.5f)] private float healthStep = 0.15f;
        [SerializeField, Min(1f)] private float rainDuration = 6f;
        [SerializeField, Min(0.1f)] private float fadeTime = 1.5f;
        [SerializeField] private float maxRate = 700f;

        private ParticleSystem system;
        private Transform target;
        private float lastRainHealth;
        private float currentHealth;
        private bool baselineSet;
        private float rainTimer;
        private float intensity;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<RainTransition>() != null) return;
            new GameObject("RainTransition").AddComponent<RainTransition>();
        }

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = 1.1f;
            main.startSpeed = 0f;
            main.gravityModifier = 3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
            main.startColor = new Color(0.75f, 0.85f, 0.95f, 0.55f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 2500;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 0.1f, 36f);
            shape.position = new Vector3(0f, 12f, 0f);

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            particleRenderer.renderMode = ParticleSystemRenderMode.Stretch;
            particleRenderer.lengthScale = 6f;
            particleRenderer.velocityScale = 0.04f;
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) particleRenderer.material = new Material(shader);

            system.Play();
        }

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void OnHealthChanged(float health01)
        {
            currentHealth = health01;
            if (!baselineSet)
            {
                // El primer valor del nivel es solo el punto de partida: no llueve.
                baselineSet = true;
                lastRainHealth = health01;
                return;
            }

            if (health01 < lastRainHealth) lastRainHealth = health01;
            if (health01 - lastRainHealth >= healthStep || (health01 >= 1f && lastRainHealth < 1f))
            {
                lastRainHealth = health01;
                rainTimer = rainDuration;
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
            if (target != null) transform.position = target.position;

            bool raining = rainTimer > 0f;
            if (raining) rainTimer -= Time.deltaTime;

            intensity = Mathf.MoveTowards(intensity, raining ? 1f : 0f, Time.deltaTime / fadeTime);
            var emission = system.emission;
            emission.rateOverTime = intensity * maxRate;
        }
    }
}
