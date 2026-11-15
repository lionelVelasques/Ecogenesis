using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>Polen y motas de luz flotando alrededor del jugador cuando el parque se recupera.</summary>
    public class PollenParticles : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float maxRate = 22f;

        private ParticleSystem system;
        private float health;
        private float shown;

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new Color(1f, 0.96f, 0.8f, 0.85f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 5f, 40f);
            shape.position = new Vector3(0f, 2.5f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.4f;
            noise.frequency = 0.3f;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) particleRenderer.material = new Material(shader);

            system.Play();
        }

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void OnHealthChanged(float health01) => health = health01;

        private void LateUpdate()
        {
            if (target != null) transform.position = target.position;

            shown = Mathf.MoveTowards(shown, health, 0.2f * Time.deltaTime);
            var emission = system.emission;
            emission.rateOverTime = Mathf.InverseLerp(0.3f, 1f, shown) * maxRate;
        }
    }
}
