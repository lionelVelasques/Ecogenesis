using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Hojas que caen girando suavemente alrededor del jugador cuando hay bastante naturaleza.
    /// Usan una hoja con nervadura (textura generada por código) en tonos verdes y dorados.
    /// Se crea sola al cargar cada escena.
    /// </summary>
    public class FallingLeaves : MonoBehaviour
    {
        [SerializeField] private float maxRate = 6f;

        private ParticleSystem system;
        private Transform target;
        private float health;
        private float shown;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<FallingLeaves>() != null) return;
            new GameObject("FallingLeaves").AddComponent<FallingLeaves>();
        }

        private void Awake()
        {
            system = gameObject.AddComponent<ParticleSystem>();

            var main = system.main;
            main.loop = true;
            main.startLifetime = 8f;
            main.startSpeed = 0f;
            main.gravityModifier = 0.05f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.35f, 0.72f, 0.3f), new Color(0.88f, 0.72f, 0.25f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            var emission = system.emission;
            emission.rateOverTime = 0f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 0.1f, 30f);
            shape.position = new Vector3(0f, 8f, 0f);

            var noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.4f;

            var rotation = system.rotationOverLifetime;
            rotation.enabled = true;
            rotation.z = new ParticleSystem.MinMaxCurve(-2.2f, 2.2f);

            var sway = system.velocityOverLifetime;
            sway.enabled = true;
            sway.space = ParticleSystemSimulationSpace.World;
            sway.x = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            sway.z = new ParticleSystem.MinMaxCurve(-0.5f, 0.5f);
            sway.y = new ParticleSystem.MinMaxCurve(-0.15f);

            var particleRenderer = system.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var material = new Material(shader);
                material.mainTexture = BuildLeafTexture();
                particleRenderer.material = material;
            }

            system.Play();
        }

        /// <summary>Hoja puntiaguda con nervadura central; es clara para que el color de la partícula la tiña.</summary>
        private static Texture2D BuildLeafTexture()
        {
            const int n = 48;
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
                    float t = (y + 0.5f) / n;
                    float halfWidth = 0.48f * Mathf.Pow(Mathf.Sin(Mathf.PI * t), 0.75f);
                    float curve = 0.08f * Mathf.Sin(t * Mathf.PI * 2f);
                    float dx = Mathf.Abs(nx - curve);

                    float alpha = Mathf.Clamp01((halfWidth - dx) * 14f);
                    float shade = 1f - 0.25f * Mathf.Clamp01(dx / Mathf.Max(halfWidth, 0.01f));
                    if (dx < 0.035f && t > 0.08f && t < 0.92f) shade = 0.55f;
                    if (dx < 0.02f && t <= 0.08f) { shade = 0.5f; alpha = Mathf.Max(alpha, 0.9f); }

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
            emission.rateOverTime = Mathf.InverseLerp(0.4f, 1f, shown) * maxRate;
        }
    }
}
