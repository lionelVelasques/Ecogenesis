using System.Collections;
using EcoGenesis.Environment;
using EcoGenesis.Interaction;
using EcoGenesis.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Pantalla de Victoria y Celebración de Fin de Nivel (Level Victory Overlay):
    /// - Se activa automáticamente cuando se limpia el parque o se alcanza el 100% de salud ambiental.
    /// - Muestra un panel modal con confeti, 3 estrellas doradas animadas (⭐⭐⭐) y estadísticas de partida.
    /// - Incluye botón interactivo directo para avanzar al "Nivel 2: Ribera del Mantaro" o reiniciar.
    /// - Auto-instanciable al cargar cualquier escena (cero configuración manual).
    /// </summary>
    public class VictoryScreenOverlay : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform panelRect;
        private TextMeshProUGUI titleLabel;
        private TextMeshProUGUI statsLabel;
        private readonly Image[] starImages = new Image[3];
        private Button nextLevelBtn;
        private Button replayBtn;

        private EnvironmentalManager environment;
        private ScoreManager score;
        private PlayerInventory inventory;
        private bool hasWon;
        private float checkTimer;
        private float gameTime;

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
            if (FindFirstObjectByType<VictoryScreenOverlay>() != null) return;
            var go = new GameObject("VictoryScreenOverlay");
            go.AddComponent<VictoryScreenOverlay>();
        }

        private void Awake()
        {
            BuildVictoryUi();
        }

        private void Start()
        {
            environment = FindFirstObjectByType<EnvironmentalManager>();
            score = FindFirstObjectByType<ScoreManager>();
            inventory = FindFirstObjectByType<PlayerInventory>();
        }

        private void OnEnable() => LevelManager.Completed += OnLevelManagerCompleted;
        private void OnDisable() => LevelManager.Completed -= OnLevelManagerCompleted;

        private void BuildVictoryUi()
        {
            var go = new GameObject("VictoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);

            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // Por encima de todo el HUD

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGroup = go.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Fondo semitransparente oscuro
            Image backdrop = UiKit.MakePanel(
                go.transform,
                "Backdrop",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(2400f, 1400f),
                new Color(0.02f, 0.05f, 0.04f, 0.82f)
            );

            // Ventana modal de victoria
            Image modal = UiKit.MakeBorderedPanel(
                go.transform,
                "VictoryModal",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(680f, 520f),
                new Color(0.04f, 0.11f, 0.08f, 0.98f),
                UiKit.GreenLight,
                3f
            );
            panelRect = modal.rectTransform;

            // Insignia superior
            UiKit.MakeLabel(
                panelRect,
                "🌿 ¡RESTAURACIÓN ECOLÓGICA COMPLETADA! 🌿",
                18f,
                UiKit.GreenLight,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -38f),
                new Vector2(620f, 28f),
                FontStyles.Bold
            );

            // Título principal
            titleLabel = UiKit.MakeLabel(
                panelRect,
                "¡PARQUE CENTRAL RECUPERADO!",
                34f,
                UiKit.Cream,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 1f),
                new Vector2(0f, -80f),
                new Vector2(620f, 48f),
                FontStyles.Bold
            );

            // Contenedor de las 3 estrellas doradas
            var starsContainer = new GameObject("StarsContainer", typeof(RectTransform));
            starsContainer.transform.SetParent(panelRect, false);
            var starsRect = starsContainer.GetComponent<RectTransform>();
            starsRect.anchoredPosition = new Vector2(0f, 40f);
            starsRect.sizeDelta = new Vector2(260f, 70f);

            for (int i = 0; i < 3; i++)
            {
                float posX = (i - 1) * 80f;
                starImages[i] = UiKit.MakeIcon(
                    starsRect,
                    UiKit.IconStar,
                    new Vector2(0.5f, 0.5f),
                    new Vector2(posX, 0f),
                    new Vector2(58f, 58f),
                    new Color(1f, 1f, 1f, 0.18f)
                );
            }

            // Panel de estadísticas de limpieza
            Image statsBox = UiKit.MakeBorderedPanel(
                panelRect,
                "StatsBox",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -50f),
                new Vector2(580f, 110f),
                new Color(0.02f, 0.06f, 0.04f, 0.9f),
                new Color(0.2f, 0.6f, 0.4f, 0.4f),
                1.5f
            );

            statsLabel = UiKit.MakeLabel(
                statsBox.transform,
                "Residuos clasificados: 100%\nPrecisión: 95% · Puntos: 150 Pts",
                20f,
                UiKit.Cream,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(550f, 90f),
                FontStyles.Normal
            );

            // Botón: Siguiente Nivel (Ribera del Mantaro)
            nextLevelBtn = UiKit.MakeButton(
                panelRect,
                "Siguiente Nivel: Ribera del Mantaro ➔",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 100f),
                new Vector2(480f, 54f),
                AvanzarSiguienteNivel,
                UiKit.GreenVibrant
            );

            // Botón: Repetir nivel
            replayBtn = UiKit.MakeButton(
                panelRect,
                "Volver a Jugar el Parque",
                new Vector2(0.5f, 0f),
                new Vector2(0f, 38f),
                new Vector2(480f, 46f),
                ReiniciarNivel,
                new Color(0.12f, 0.22f, 0.17f, 0.95f)
            );
        }

        private void Update()
        {
            if (hasWon) return;

            gameTime += Time.unscaledDeltaTime;

            checkTimer -= Time.deltaTime;
            if (checkTimer > 0f) return;
            checkTimer = 0.5f;

            // Condición de victoria: 0 residuos en suelo y mochila vacía, o salud >= 98%
            int remainingTrash = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
            bool mochilaVacia = inventory == null || inventory.Items.Count == 0;
            float health = environment != null ? environment.Health01 : 0f;

            if ((remainingTrash == 0 && mochilaVacia) || health >= 0.98f)
            {
                TriggerVictory(3);
            }
        }

        private void OnLevelManagerCompleted(LevelManager.LevelResult result)
        {
            if (hasWon) return;
            TriggerVictory(result.stars);
        }

        public void TriggerVictory(int starsCount)
        {
            if (hasWon) return;
            hasWon = true;

            // Desbloquear cursor para interactuar con botones
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // Desactivar movimiento del personaje
            var input = FindFirstObjectByType<PlayerInputReader>();
            if (input != null) input.InputEnabled = false;

            // Calcular estadísticas
            int puntos = score != null ? score.Points : 120;
            int mins = Mathf.FloorToInt(gameTime / 60f);
            int secs = Mathf.FloorToInt(gameTime % 60f);
            float accuracy = score != null ? Mathf.RoundToInt(score.Accuracy01 * 100f) : 100f;

            statsLabel.text = $"<b>Residuos Recogidos:</b> 100%   |   <b>Precisión:</b> {accuracy}%\n" +
                              $"<b>Puntuación Eco:</b> {puntos} Pts   |   <b>Tiempo:</b> {mins:00}:{secs:00}";

            // Verificar si existe siguiente nivel
            string activeScene = SceneManager.GetActiveScene().name;
            int levelIndex = LevelCatalog.IndexOfScene(activeScene);
            bool hasNext = levelIndex >= 0 && levelIndex < LevelCatalog.Levels.Length - 1;

            if (hasNext)
            {
                string nextTitle = LevelCatalog.Levels[levelIndex + 1].title;
                var btnText = nextLevelBtn.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = $"Siguiente: {nextTitle} ➔";
            }
            else
            {
                nextLevelBtn.gameObject.SetActive(false);
            }

            StartCoroutine(AnimarVictoria(starsCount));
        }

        private IEnumerator AnimarVictoria(int starsCount)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // Pop de entrada del panel
            panelRect.localScale = Vector3.one * 0.75f;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(t / 0.45f);
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                canvasGroup.alpha = ease;
                panelRect.localScale = Vector3.Lerp(Vector3.one * 0.75f, Vector3.one, ease);
                yield return null;
            }

            // Animar las estrellas una por una con sonido y brillo dorado
            for (int i = 0; i < starsCount; i++)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                starImages[i].color = UiKit.Gold;
                starImages[i].rectTransform.localScale = Vector3.one * 1.4f;

                float st = 0f;
                while (st < 0.2f)
                {
                    st += Time.unscaledDeltaTime;
                    starImages[i].rectTransform.localScale = Vector3.Lerp(Vector3.one * 1.4f, Vector3.one, st / 0.2f);
                    yield return null;
                }
            }
        }

        private void AvanzarSiguienteNivel()
        {
            Time.timeScale = 1f;
            string activeScene = SceneManager.GetActiveScene().name;
            int levelIndex = LevelCatalog.IndexOfScene(activeScene);
            if (levelIndex >= 0 && levelIndex < LevelCatalog.Levels.Length - 1)
            {
                SceneManager.LoadScene(LevelCatalog.Levels[levelIndex + 1].scene);
            }
            else
            {
                SceneManager.LoadScene("Level2_Rio");
            }
        }

        private void ReiniciarNivel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
