using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Mariposas de colores que aparecen y revolotean cerca del jugador cuando el parque se recupera.
    /// Dibujan una mariposa real (textura generada por código) que bate las alas con 2 cuadros.
    /// Se crea sola al cargar cada escena.
    /// </summary>
    public class ButterflyParticles : MonoBehaviour
    {
        [SerializeField] private float maxRate = 4f;

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
            if (FindFirstObjectByType<ButterflyParticles>() != null) return;
            new GameObject("ButterflyParticles").AddComponent<ButterflyParticles>();
        }

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = 12f;
            main.startSpeed = 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.45f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.15f), new Color(0.55f, 0.7f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 60;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 2f, 30f);
            shape.position = new Vector3(0f, 1.5f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 1.6f;
            noise.frequency = 0.6f;

            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
            var colorOverLifetime = system.colorOverLifetime;
            colorOverLifetime.enabled = true;
            colorOverLifetime.color = fade;

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                material.mainTexture = BuildButterflySheet();
                particleRenderer.material = material;
            }

            var sheet = system.textureSheetAnimation;
            sheet.enabled = true;
            sheet.mode = ParticleSystemAnimationMode.Grid;
            sheet.numTilesX = 2;
            sheet.numTilesY = 1;
            sheet.animation = ParticleSystemAnimationType.WholeSheet;
            sheet.timeMode = ParticleSystemAnimationTimeMode.FPS;
            sheet.fps = 9f;

            system.Play();
        }

        /// <summary>Hoja de 2 cuadros: alas abiertas y alas cerradas. Es gris para que el color de la partícula la tiña.</summary>
        private static Texture2D BuildButterflySheet()
        {
            const int n = 48;
            var tex = new Texture2D(n * 2, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int frame = 0; frame < 2; frame++)
            {
                float wingX = frame == 0 ? 0.48f : 0.14f;
                float wingRx = frame == 0 ? 0.46f : 0.14f;
                for (int y = 0; y < n; y++)
                {
                    for (int x = 0; x < n; x++)
                    {
                        float nx = Mathf.Abs((x + 0.5f) / n * 2f - 1f);
                        float ny = (y + 0.5f) / n * 2f - 1f;

                        float upper = Ellipse(nx, ny, wingX, 0.28f, wingRx, 0.5f);
                        float lower = Ellipse(nx, ny, wingX * 0.8f, -0.4f, wingRx * 0.75f, 0.34f);
                        float wing = Mathf.Min(upper, lower);
                        float body = Ellipse(nx, ny, 0f, -0.05f, 0.07f, 0.62f);

                        float alpha = Mathf.Clamp01((1f - Mathf.Min(wing, body)) * 8f);
                        float shade = 1f;
                        if (wing < 1f) shade = Mathf.Lerp(1f, 0.55f, Mathf.SmoothStep(0.6f, 1f, wing));
                        if (frame == 0 && Ellipse(nx, ny, wingX + 0.05f, 0.35f, 0.1f, 0.1f) < 1f) shade *= 0.6f;
                        if (body < 1f) shade = 0.28f;

                        tex.SetPixel(frame * n + x, y, new Color(shade, shade, shade, alpha));
                    }
                }
            }

            tex.Apply();
            return tex;
        }

        private static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx;
            float dy = (y - cy) / ry;
            return Mathf.Sqrt(dx * dx + dy * dy);
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
            emission.rateOverTime = Mathf.InverseLerp(0.5f, 1f, shown) * maxRate;
        }
    }
}
