using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Player
{
    /// <summary>
    /// Sistema de partículas de polvo y huellas dinámicas:
    /// - Suelta pequeñas motas de polvo suave bajo los pies del personaje al esprintar.
    /// - Genera una ráfaga de polvo circular cuando el jugador aterriza tras un salto.
    /// - Auto-instanciable al cargar cualquier escena (cero configuración manual).
    /// </summary>
    public class PlayerFootstepVfx : MonoBehaviour
    {
        private PlayerController player;
        private CharacterController controller;
        private ParticleSystem particleSystemRef;
        private static Texture2D dustTexture;

        private bool wasGrounded = true;
        private float stepTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureInstance();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => EnsureInstance();

        private static void EnsureInstance()
        {
            if (FindFirstObjectByType<PlayerFootstepVfx>() != null) return;
            var go = new GameObject("PlayerFootstepVfx");
            go.AddComponent<PlayerFootstepVfx>();
        }

        private void Awake()
        {
            BuildParticleSystem();
        }

        private void Start()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player != null) controller = player.GetComponent<CharacterController>();
        }

        private void BuildParticleSystem()
        {
            particleSystemRef = gameObject.AddComponent<ParticleSystem>();

            var main = particleSystemRef.main;
            main.loop = false;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 80;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.32f);
            main.startColor = new Color(0.85f, 0.82f, 0.72f, 0.40f); // Polvo cálido suave
            main.gravityModifier = -0.05f; // Flota suavemente hacia arriba

            var emission = particleSystemRef.emission;
            emission.enabled = false; // Emisión controlada por código

            var shape = particleSystemRef.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;

            var sizeOverLife = particleSystemRef.sizeOverLifetime;
            sizeOverLife.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(0.3f, 1f);
            curve.AddKey(1f, 1.4f);
            sizeOverLife.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var colorOverLife = particleSystemRef.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.88f, 0.85f, 0.75f), 0f), new GradientColorKey(new Color(0.75f, 0.85f, 0.72f), 1f) },
                new[] { new GradientAlphaKey(0.5f, 0f), new GradientAlphaKey(0.35f, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLife.color = grad;

            // Material suave
            var renderer = particleSystemRef.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Particles/Standard Unlit") ?? Shader.Find("Sprites/Default");
            if (shader != null)
            {
                var mat = new Material(shader);
                if (dustTexture == null) dustTexture = CreateSoftCircleTexture();
                mat.mainTexture = dustTexture;
                renderer.material = mat;
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player != null) controller = player.GetComponent<CharacterController>();
                if (player == null) return;
            }

            bool isGrounded = controller != null ? controller.isGrounded : true;
            Vector3 playerPos = player.transform.position;

            // 1. Impacto al aterrizar de un salto
            if (!wasGrounded && isGrounded)
            {
                EmitirPolvoAterrizaje(playerPos);
            }
            wasGrounded = isGrounded;

            // 2. Polvo continuo al correr
            if (isGrounded && player.Speed01 > 0.45f)
            {
                stepTimer -= Time.deltaTime;
                if (stepTimer <= 0f)
                {
                    stepTimer = Mathf.Lerp(0.25f, 0.14f, player.Speed01);
                    EmitirPolvoPasos(playerPos);
                }
            }
        }

        private void EmitirPolvoPasos(Vector3 pos)
        {
            Vector3 spawnPos = pos + Vector3.up * 0.05f - player.transform.forward * 0.2f;
            var emitParams = new ParticleSystem.EmitParams
            {
                position = spawnPos + Random.insideUnitSphere * 0.1f,
                velocity = -player.transform.forward * Random.Range(0.2f, 0.5f) + Vector3.up * Random.Range(0.1f, 0.3f),
                startSize = Random.Range(0.15f, 0.28f),
                startColor = new Color(0.85f, 0.82f, 0.72f, Random.Range(0.35f, 0.55f))
            };
            particleSystemRef.Emit(emitParams, 1);
        }

        private void EmitirPolvoAterrizaje(Vector3 pos)
        {
            Vector3 groundCenter = pos + Vector3.up * 0.05f;
            int count = 10;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                Vector3 outward = new Vector3(Mathf.Cos(angle), 0.2f, Mathf.Sin(angle));
                var emitParams = new ParticleSystem.EmitParams
                {
                    position = groundCenter + outward * 0.15f,
                    velocity = outward * Random.Range(0.6f, 1.2f) + Vector3.up * 0.35f,
                    startSize = Random.Range(0.22f, 0.35f),
                    startColor = new Color(0.88f, 0.85f, 0.72f, 0.55f)
                };
                particleSystemRef.Emit(emitParams, 1);
            }
        }

        private static Texture2D CreateSoftCircleTexture()
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
                    float dist = Mathf.Sqrt(nx * nx + ny * ny);
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.SmoothStep(0f, 1f, alpha);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
