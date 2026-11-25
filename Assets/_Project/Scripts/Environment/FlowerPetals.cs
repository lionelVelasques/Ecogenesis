using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Pétalos de flores (rosados, blancos y amarillos) que flotan suavemente cerca del jugador
    /// a medida que el área se recupera. Dibuja un pétalo real (textura generada por código).
    /// Se crea sola al cargar cada escena, igual que ButterflyParticles: no hay que configurar nada.
    /// </summary>
    public class FlowerPetals : MonoBehaviour
    {
        [SerializeField] private float maxRate = 12f;

        private ParticleSystem system;
        private Transform target;
        private float health;
        private float shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Spawn();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

        private static void Spawn()
        {
            if (FindFirstObjectByType<FlowerPetals>() != null) return;
            new GameObject("FlowerPetals").AddComponent<FlowerPetals>();
        }

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = 10f;
            main.startSpeed = 0.1f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.62f, 0.78f), new Color(1f, 0.95f, 0.7f));
            main.gravityModifier = 0.06f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(24f, 3f, 24f);
            shape.position = new Vector3(0f, 3f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.2f;
            noise.frequency = 0.5f;

            var spin = system.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                material.mainTexture = BuildPetalTexture();
                particleRenderer.material = material;
            }

            system.Play();
        }

        /// <summary>Pétalo gris con una vena central; el color de la partícula lo tiñe.</summary>
        private static Texture2D BuildPetalTexture()
        {
            const int n = 32;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float nx = (x + 0.5f) / n * 2f - 1f;
                    float ny = (y + 0.5f) / n * 2f - 1f;

                    // Forma de gota: más ancha abajo, puntiaguda arriba.
                    float width = Mathf.Lerp(0.55f, 0.12f, Mathf.InverseLerp(-0.8f, 0.9f, ny));
                    float dist = Mathf.Sqrt((nx * nx) / (width * width) + (ny * ny) / (0.9f * 0.9f));

                    float alpha = Mathf.Clamp01((1f - dist) * 6f);
                    float shade = Mathf.Lerp(1f, 0.7f, Mathf.SmoothStep(0.4f, 1f, dist));
                    if (Mathf.Abs(nx) < 0.04f) shade *= 0.85f;

                    tex.SetPixel(x, y, new Color(shade, shade, shade, alpha));
                }
            }

            tex.Apply();
            return tex;
        }

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void OnHealthChanged(float health01) => health = health01;

        private void LateUpdate()
        {
            if (target == null)
            {
                var player = FindFirstObjectByType<PlayerController>();
                if (player != null) target = player.transform;
            }
            if (target != null) transform.position = target.position;

            shown = Mathf.MoveTowards(shown, health, 0.15f * Time.deltaTime);
            var emission = system.emission;
            emission.rateOverTime = Mathf.InverseLerp(0.1f, 0.8f, shown) * maxRate;
        }
    }
}
