using EcoGenesis.Core;
using EcoGenesis.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Arcoíris que aparece en el cielo, frente al jugador, al completar el nivel.
    /// Son 7 arcos (LineRenderer) que se desvanecen con el tiempo. Se crea solo al cargar cada escena.
    /// </summary>
    public class RainbowOnComplete : MonoBehaviour
    {
        [SerializeField] private float distance = 70f;
        [SerializeField] private float radius = 38f;
        [SerializeField] private float fadeInTime = 3f;
        [SerializeField] private float visibleTime = 25f;

        private static readonly Color[] Bands =
        {
            new Color(0.9f, 0.2f, 0.2f), new Color(1f, 0.55f, 0.15f), new Color(1f, 0.9f, 0.25f),
            new Color(0.35f, 0.8f, 0.3f), new Color(0.3f, 0.6f, 1f), new Color(0.3f, 0.35f, 0.85f),
            new Color(0.6f, 0.35f, 0.8f)
        };

        private LineRenderer[] lines;
        private float timer = -1f;

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
            if (FindFirstObjectByType<RainbowOnComplete>() != null) return;
            new GameObject("RainbowOnComplete").AddComponent<RainbowOnComplete>();
        }

        private void OnEnable() => LevelManager.Completed += OnCompleted;
        private void OnDisable() => LevelManager.Completed -= OnCompleted;

        private void OnCompleted(LevelManager.LevelResult result)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;

            Vector3 forward = Camera.main != null ? Camera.main.transform.forward : player.transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            Build(player.transform.position + forward * distance, forward);
            timer = 0f;
        }

        private void Build(Vector3 center, Vector3 forward)
        {
            if (lines != null)
            {
                foreach (LineRenderer old in lines) if (old != null) Destroy(old.gameObject);
            }

            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Shader shader = Shader.Find("Sprites/Default");
            lines = new LineRenderer[Bands.Length];

            for (int b = 0; b < Bands.Length; b++)
            {
                var go = new GameObject("RainbowBand_" + b);
                go.transform.SetParent(transform, false);
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 48;
                line.widthMultiplier = 1.6f;
                line.numCapVertices = 4;
                if (shader != null) line.material = new Material(shader);

                float r = radius - b * 1.5f;
                for (int i = 0; i < line.positionCount; i++)
                {
                    float a = Mathf.PI * i / (line.positionCount - 1);
                    Vector3 p = center + right * (Mathf.Cos(a) * r) + Vector3.up * (Mathf.Sin(a) * r);
                    line.SetPosition(i, p);
                }
                SetAlpha(line, Bands[b], 0f);
                lines[b] = line;
            }
        }

        private static void SetAlpha(LineRenderer line, Color color, float alpha)
        {
            var c = new Color(color.r, color.g, color.b, alpha);
            line.startColor = c;
            line.endColor = c;
        }

        private void Update()
        {
            if (timer < 0f || lines == null) return;
            timer += Time.deltaTime;

            float fadeIn = Mathf.Clamp01(timer / fadeInTime);
            float fadeOut = Mathf.Clamp01((visibleTime - timer) / 4f);
            float alpha = 0.55f * Mathf.Min(fadeIn, fadeOut);

            for (int b = 0; b < lines.Length; b++)
            {
                if (lines[b] != null) SetAlpha(lines[b], Bands[b], alpha);
            }

            if (timer >= visibleTime)
            {
                foreach (LineRenderer line in lines) if (line != null) Destroy(line.gameObject);
                lines = null;
                timer = -1f;
            }
        }
    }
}
