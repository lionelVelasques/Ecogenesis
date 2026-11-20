using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Luciérnagas: puntos de luz suaves que parpadean cerca del jugador cuando el parque está bastante recuperado.
    /// La textura del punto de luz se genera por código. Se crea sola al cargar cada escena.
    /// </summary>
    public class FireflyParticles : MonoBehaviour
    {
        [SerializeField] private float maxRate = 5f;

        private ParticleSystem system;
        private Transform target;
        private float health;
        private float shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            // El menú recarga la escena con LoadScene, que destruye este objeto: se vuelve a crear en cada carga.
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Spawn();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Spawn();

        private static void Spawn()
        {
            if (FindFirstObjectByType<FireflyParticles>() != null) return;
            new GameObject("FireflyParticles").AddComponent<FireflyParticles>();
        }

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.95f, 0.45f), new Color(0.7f, 1f, 0.5f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(28f, 2.5f, 28f);
            shape.position = new Vector3(0f, 1.4f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.1f;
            noise.frequency = 0.5f;

            // Parpadeo: sube y baja la opacidad varias veces en la vida de la luciérnaga.
            var blink = new Gradient();
            blink.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.15f, 0.3f),
                    new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(0.2f, 0.7f), new GradientAlphaKey(1f, 0.85f),
                    new GradientAlphaKey(0f, 1f)
                });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = blink;

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                material.mainTexture = BuildGlowTexture();
                particleRenderer.material = material;
            }

            system.Play();
        }

        private static Texture2D BuildGlowTexture()
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
                    float dx = (x + 0.5f) / n * 2f - 1f;
                    float dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    float a = d * d;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
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
            emission.rateOverTime = Mathf.InverseLerp(0.6f, 1f, shown) * maxRate;
        }
    }
}
