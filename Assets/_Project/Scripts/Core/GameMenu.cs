using System.Collections;
using EcoGenesis.Audio;
using EcoGenesis.Player;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Menú completo construido por código: principal (cámara orbitando), selección de niveles con estrellas,
    /// opciones, créditos, pausa (Esc) y pantalla de nivel completado. No necesita referencias: busca lo que usa.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class GameMenu : MonoBehaviour
    {
        private enum State { Menu, Playing, Paused, Complete }

        private const string AutoStartKey = "eco_autostart";
        private const string LoadKey = "eco_loadsave";

        private State state = State.Menu;
        private PlayerInputReader input;
        private CameraController gameCamera;
        private SaveSystem save;
        private GameHud hud;
        private LevelManager level;
        private Camera cam;
        private Transform orbitCenter;
        private float angle = 200f;

        private CanvasGroup fade;
        private GameObject mainPanel, levelsPanel, optionsPanel, creditsPanel, pausePanel, completePanel;
        private RectTransform mainButtons;
        private GameObject optionsReturn;
        private TextMeshProUGUI qualityLabel, completeTitle, completeStats;
        private Image[] completeStars;
        private Button nextButton;
        private int currentIndex;

        // ------------------------------------------------------------------ CICLO DE VIDA

        private void Awake()
        {
            Time.timeScale = 1f;
            cam = Camera.main;
            input = FindFirstObjectByType<PlayerInputReader>();
            gameCamera = FindFirstObjectByType<CameraController>();
            save = FindFirstObjectByType<SaveSystem>();
            hud = FindFirstObjectByType<GameHud>();
            level = FindFirstObjectByType<LevelManager>();
            orbitCenter = input != null ? input.transform : transform;
            currentIndex = LevelCatalog.IndexOfScene(SceneManager.GetActiveScene().name);

            EnsureEventSystem();
            BuildUi();

            if (gameCamera != null) gameCamera.enabled = false;
            if (input != null) input.InputEnabled = false;
            SetCursor(true);
        }

        private void OnEnable() => LevelManager.Completed += OnLevelCompleted;
        private void OnDisable() => LevelManager.Completed -= OnLevelCompleted;

        private void Start()
        {
            if (hud != null) hud.SetVisible(false);

            if (PlayerPrefs.GetInt(AutoStartKey, 0) == 1)
            {
                bool load = PlayerPrefs.GetInt(LoadKey, 0) == 1;
                PlayerPrefs.SetInt(AutoStartKey, 0);
                PlayerPrefs.SetInt(LoadKey, 0);
                StartCoroutine(BeginPlay(load, true));
            }
            else
            {
                RebuildMainButtons();
                ShowPanel(mainPanel);
                StartCoroutine(Fade(0f, 1.2f));
            }
        }

        private void Update()
        {
            switch (state)
            {
                case State.Menu:
                case State.Complete:
                    OrbitCamera();
                    if (levelsPanel.activeSelf && Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
                    {
                        LevelCatalog.UnlockAll(); // atajo de pruebas: tecla U en la pantalla de niveles
                        RefreshLevels();
                    }
                    break;
                case State.Playing:
                    if (EscapePressed()) Pause();
                    break;
                case State.Paused:
                    if (EscapePressed())
                    {
                        if (optionsPanel.activeSelf) CloseOptions();
                        else Resume();
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ FLUJO DE JUEGO

        private IEnumerator BeginPlay(bool load, bool alreadyBlack)
        {
            if (!alreadyBlack) yield return Fade(1f, 0.45f);

            ShowPanel(null);
            state = State.Playing;
            if (hud != null) hud.SetVisible(true);
            if (load && save != null) save.Load();
            if (save != null) save.AutoSave = true;
            if (level != null) level.Running = true;
            if (gameCamera != null) gameCamera.enabled = true;
            if (input != null) input.InputEnabled = true;
            SetCursor(false);

            if (hud != null)
            {
                string title = currentIndex >= 0 ? LevelCatalog.Levels[currentIndex].title : SceneManager.GetActiveScene().name;
                hud.ShowBanner("NIVEL " + (currentIndex + 1), title);
            }
            yield return Fade(0f, 0.8f);
        }

        private void Pause()
        {
            state = State.Paused;
            Time.timeScale = 0f;
            if (input != null) input.InputEnabled = false;
            if (gameCamera != null) gameCamera.enabled = false;
            ShowPanel(pausePanel);
            SetCursor(true);
        }

        private void Resume()
        {
            state = State.Playing;
            Time.timeScale = 1f;
            ShowPanel(null);
            if (input != null) input.InputEnabled = true;
            if (gameCamera != null) gameCamera.enabled = true;
            SetCursor(false);
        }

        private void OnLevelCompleted(LevelManager.LevelResult result)
        {
            state = State.Complete;
            if (save != null) save.AutoSave = false;
            if (input != null) input.InputEnabled = false;
            if (gameCamera != null) gameCamera.enabled = false;
            if (hud != null) hud.SetVisible(false);
            SetCursor(true);

            completeTitle.text = result.title;
            for (int i = 0; i < completeStars.Length; i++)
                completeStars[i].color = i < result.stars ? UiKit.Gold : new Color(1f, 1f, 1f, 0.18f);
            int minutes = Mathf.FloorToInt(result.seconds / 60f);
            int seconds = Mathf.FloorToInt(result.seconds % 60f);
            completeStats.text = "Puntos eco:  " + result.points + "\nPrecisión:  " + Mathf.RoundToInt(result.accuracy * 100f) + "%\nTiempo:  " + minutes.ToString("00") + ":" + seconds.ToString("00");
            nextButton.gameObject.SetActive(result.hasNext);
            ShowPanel(completePanel);
        }

        private void ContinueGame()
        {
            string scene = SaveSystem.SavedSceneName();
            if (string.IsNullOrEmpty(scene) || scene == SceneManager.GetActiveScene().name) StartCoroutine(BeginPlay(true, false));
            else StartCoroutine(LoadSceneRoutine(scene, true));
        }

        private void NewGame()
        {
            if (save != null) save.Delete();
            LoadLevel(0, false);
        }

        private void LoadLevel(int index, bool loadSave)
        {
            if (index < 0 || index >= LevelCatalog.Levels.Length) return;
            StartCoroutine(LoadSceneRoutine(LevelCatalog.Levels[index].scene, loadSave));
        }

        private IEnumerator LoadSceneRoutine(string scene, bool loadSave)
        {
            Time.timeScale = 1f;
            PlayerPrefs.SetInt(AutoStartKey, 1);
            PlayerPrefs.SetInt(LoadKey, loadSave ? 1 : 0);
            yield return Fade(1f, 0.45f);
            SceneManager.LoadScene(scene);
        }

        private void BackToMainMenu()
        {
            if (save != null && state == State.Paused) save.Save(false);
            Time.timeScale = 1f;
            PlayerPrefs.SetInt(AutoStartKey, 0);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ CONSTRUCCIÓN DE PANTALLAS

        private void BuildUi()
        {
            Canvas canvas = UiKit.MakeCanvas("MenuUi", 20);
            canvas.transform.SetParent(transform, false);
            Transform r = canvas.transform;

            mainPanel = BuildMain(r);
            levelsPanel = UiKit.Stretch(r, "Levels").gameObject;
            optionsPanel = BuildOptions(r);
            creditsPanel = BuildCredits(r);
            pausePanel = BuildPause(r);
            completePanel = BuildComplete(r);

            Canvas fadeCanvas = UiKit.MakeCanvas("FadeUi", 100);
            fadeCanvas.transform.SetParent(transform, false);
            RectTransform black = UiKit.Stretch(fadeCanvas.transform, "Black");
            black.gameObject.AddComponent<Image>().color = Color.black;
            fade = fadeCanvas.gameObject.AddComponent<CanvasGroup>();
            fade.alpha = 1f;
            fade.blocksRaycasts = true;
            ShowPanel(null);
        }

        private GameObject BuildMain(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Main").gameObject;

            RectTransform side = UiKit.Stretch(p.transform, "Side");
            side.anchorMin = new Vector2(0f, 0f);
            side.anchorMax = new Vector2(0f, 1f);
            side.pivot = new Vector2(0f, 0.5f);
            side.sizeDelta = new Vector2(1000f, 0f);
            var sideImage = side.gameObject.AddComponent<Image>();
            sideImage.sprite = UiKit.Gradient;
            sideImage.color = new Color(0.03f, 0.09f, 0.06f, 0.94f);
            sideImage.raycastTarget = false;

            UiKit.MakeLabel(p.transform, "ECOGENESIS", 132f, UiKit.Cream, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(110f, -110f), new Vector2(860f, 160f), FontStyles.Bold);
            UiKit.MakeLabel(p.transform, "Restaura el parque. Cuida el planeta.", 36f, UiKit.GreenLight, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(116f, -270f), new Vector2(760f, 50f));
            UiKit.MakePanel(p.transform, "Accent", new Vector2(0f, 1f), new Vector2(118f, -335f), new Vector2(190f, 7f), UiKit.Green).raycastTarget = false;

            mainButtons = UiKit.Rect(p.transform, "Buttons", new Vector2(0f, 1f), new Vector2(116f, -390f), new Vector2(480f, 560f));
            UiKit.MakeLabel(p.transform, "ODS 11 · 12 · 15    |    Huancayo, Perú", 24f, UiKit.Soft, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(116f, 40f), new Vector2(760f, 40f));
            UiKit.MakeLabel(p.transform, "Prototipo v0.3", 22f, UiKit.Soft, TextAlignmentOptions.BottomRight, new Vector2(1f, 0f), new Vector2(-40f, 40f), new Vector2(300f, 36f));
            return p;
        }

        private void RebuildMainButtons()
        {
            foreach (Transform child in mainButtons) Destroy(child.gameObject);

            float y = 0f;
            if (save != null && save.HasSave) { AddMainButton("Continuar", y, ContinueGame); y += 80f; }
            AddMainButton("Nueva partida", y, NewGame); y += 80f;
            AddMainButton("Niveles", y, () => { RefreshLevels(); ShowPanel(levelsPanel); }); y += 80f;
            AddMainButton("Opciones", y, () => OpenOptions(mainPanel)); y += 80f;
            AddMainButton("Créditos", y, () => ShowPanel(creditsPanel)); y += 80f;
            AddMainButton("Salir", y, Quit);
        }

        private void AddMainButton(string label, float y, UnityEngine.Events.UnityAction action)
        {
            UiKit.MakeButton(mainButtons, label, new Vector2(0f, 1f), new Vector2(0f, -y), new Vector2(460f, 66f), action);
        }

        private void RefreshLevels()
        {
            foreach (Transform child in levelsPanel.transform) Destroy(child.gameObject);

            Image dim = UiKit.MakePanel(levelsPanel.transform, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0.03f, 0.02f, 0.72f));
            dim.raycastTarget = true;
            UiKit.MakeLabel(levelsPanel.transform, "SELECCIONAR NIVEL", 60f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1200f, 80f), FontStyles.Bold);

            int count = LevelCatalog.Levels.Length;
            for (int i = 0; i < count; i++)
            {
                LevelCatalog.Info info = LevelCatalog.Levels[i];
                bool unlocked = LevelCatalog.IsUnlocked(i);
                int index = i;

                float x = (i - (count - 1) * 0.5f) * 560f;
                Image card = UiKit.MakePanel(levelsPanel.transform, "Card" + i, new Vector2(0.5f, 0.5f), new Vector2(x, -20f), new Vector2(520f, 640f), UiKit.PanelSolid);
                Image band = UiKit.MakePanel(card.transform, "Band", new Vector2(0.5f, 1f), Vector2.zero, new Vector2(520f, 150f), unlocked ? info.color : new Color(0.3f, 0.33f, 0.31f, 1f));
                band.raycastTarget = false;
                UiKit.MakeLabel(band.transform, "NIVEL " + (i + 1), 26f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(300f, 34f), FontStyles.Bold);
                UiKit.MakeLabel(band.transform, info.title, 42f, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -60f), new Vector2(470f, 60f), FontStyles.Bold);
                UiKit.MakeLabel(card.transform, info.description, 26f, UiKit.Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -176f), new Vector2(464f, 210f));

                int stars = LevelCatalog.Stars(i);
                for (int s = 0; s < 3; s++)
                    UiKit.MakeCircle(card.transform, "Star" + s, new Vector2(0f, 0f), new Vector2(28f + s * 52f, 150f), new Vector2(40f, 40f), s < stars ? UiKit.Gold : new Color(1f, 1f, 1f, 0.18f));

                string status = !unlocked ? "Completa el nivel anterior" : (LevelCatalog.IsCompleted(i) ? "Completado" : "Disponible");
                UiKit.MakeLabel(card.transform, status, 24f, unlocked ? UiKit.GreenLight : UiKit.Cream, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(28f, 104f), new Vector2(464f, 34f));

                Button play = UiKit.MakeButton(card.transform, unlocked ? "Jugar" : "Bloqueado", new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(440f, 62f), () => LoadLevel(index, false), unlocked ? info.color : UiKit.Green);
                play.interactable = unlocked;
            }

            UiKit.MakeButton(levelsPanel.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 50f), new Vector2(360f, 62f), () => ShowPanel(mainPanel), UiKit.Blue);
        }

        private GameObject BuildOptions(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Options").gameObject;
            AddDim(p.transform);
            Image card = UiKit.MakePanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 700f), UiKit.PanelSolid);
            UiKit.MakeLabel(card.transform, "OPCIONES", 56f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(700f, 80f), FontStyles.Bold);

            AddOptionRow(card.transform, "Volumen general", -170f, GameSettings.Master, GameSettings.SetMaster);
            AddOptionRow(card.transform, "Música", -260f, GameSettings.Music, GameSettings.SetMusic);
            AddOptionRow(card.transform, "Efectos y ambiente", -350f, GameSettings.Effects, GameSettings.SetEffects);

            UiKit.MakeLabel(card.transform, "Calidad gráfica", 32f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(70f, -440f), new Vector2(360f, 44f));
            Button quality = UiKit.MakeButton(card.transform, "", new Vector2(0f, 1f), new Vector2(450f, -430f), new Vector2(380f, 54f), CycleQuality, UiKit.Blue, 28f);
            qualityLabel = quality.GetComponentInChildren<TextMeshProUGUI>();
            qualityLabel.text = QualitySettings.names[QualitySettings.GetQualityLevel()];

            UiKit.MakeButton(card.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 62f), CloseOptions);
            return p;
        }

        private static void AddOptionRow(Transform parent, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            UiKit.MakeLabel(parent, label, 32f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 1f), new Vector2(70f, y), new Vector2(360f, 44f));
            Slider slider = UiKit.MakeSlider(parent, label, new Vector2(0f, 1f), new Vector2(450f, y - 10f), new Vector2(380f, 26f));
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(onChange);
        }

        private void CycleQuality()
        {
            int next = (QualitySettings.GetQualityLevel() + 1) % QualitySettings.names.Length;
            QualitySettings.SetQualityLevel(next, true);
            qualityLabel.text = QualitySettings.names[next];
        }

        private GameObject BuildCredits(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Credits").gameObject;
            AddDim(p.transform);
            Image card = UiKit.MakePanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 720f), UiKit.PanelSolid);
            UiKit.MakeLabel(card.transform, "CRÉDITOS", 56f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(800f, 80f), FontStyles.Bold);
            UiKit.MakeLabel(card.transform,
                "EcoGenesis 3D\nVideojuego de aventura, exploración y restauración ambiental.\n\n" +
                "Basado en la problemática real de los residuos sólidos.\nObjetivos de Desarrollo Sostenible 11, 12 y 15.\n\n" +
                "Motor: Unity 6 (URP)   ·   Código: C#\nModelos de naturaleza: Quaternius (CC0)\n\n" +
                "Universidad Continental — Huancayo, Perú",
                28f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(880f, 440f));
            UiKit.MakeButton(card.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(360f, 62f), () => ShowPanel(mainPanel));
            return p;
        }

        private GameObject BuildPause(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Pause").gameObject;
            AddDim(p.transform);
            Image card = UiKit.MakePanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620f, 600f), UiKit.PanelSolid);
            UiKit.MakeLabel(card.transform, "PAUSA", 56f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(500f, 80f), FontStyles.Bold);
            float y = -140f;
            UiKit.MakeButton(card.transform, "Continuar", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 66f), Resume); y -= 82f;
            UiKit.MakeButton(card.transform, "Guardar partida", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 66f), () => { if (save != null) save.Save(true); }); y -= 82f;
            UiKit.MakeButton(card.transform, "Opciones", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 66f), () => OpenOptions(pausePanel)); y -= 82f;
            UiKit.MakeButton(card.transform, "Menú principal", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(460f, 66f), BackToMainMenu, UiKit.Blue);
            return p;
        }

        private GameObject BuildComplete(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Complete").gameObject;
            AddDim(p.transform);
            Image card = UiKit.MakePanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 740f), UiKit.PanelSolid);
            UiKit.MakeLabel(card.transform, "¡NIVEL COMPLETADO!", 58f, UiKit.Gold, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(800f, 80f), FontStyles.Bold);
            completeTitle = UiKit.MakeLabel(card.transform, "", 40f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(800f, 56f));

            completeStars = new Image[3];
            for (int i = 0; i < 3; i++)
                completeStars[i] = UiKit.MakeCircle(card.transform, "Star" + i, new Vector2(0.5f, 1f), new Vector2((i - 1) * 110f, -200f), new Vector2(84f, 84f), UiKit.Gold);

            completeStats = UiKit.MakeLabel(card.transform, "", 34f, UiKit.Soft, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -320f), new Vector2(700f, 180f));

            nextButton = UiKit.MakeButton(card.transform, "Siguiente nivel", new Vector2(0.5f, 0f), new Vector2(0f, 190f), new Vector2(460f, 66f), () => LoadLevel(currentIndex + 1, false));
            UiKit.MakeButton(card.transform, "Repetir nivel", new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(460f, 62f), () => { if (save != null) save.Delete(); LoadLevel(currentIndex, false); }, UiKit.Blue);
            UiKit.MakeButton(card.transform, "Menú principal", new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(460f, 62f), BackToMainMenu, UiKit.Blue);
            return p;
        }

        private static void AddDim(Transform parent)
        {
            Image dim = UiKit.MakePanel(parent, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0.03f, 0.02f, 0.72f));
            dim.raycastTarget = true;
        }

        // ------------------------------------------------------------------ AYUDAS

        private void OpenOptions(GameObject returnTo)
        {
            optionsReturn = returnTo;
            ShowPanel(optionsPanel);
        }

        private void CloseOptions() => ShowPanel(optionsReturn != null ? optionsReturn : mainPanel);

        private void ShowPanel(GameObject panel)
        {
            mainPanel.SetActive(panel == mainPanel);
            levelsPanel.SetActive(panel == levelsPanel);
            optionsPanel.SetActive(panel == optionsPanel);
            creditsPanel.SetActive(panel == creditsPanel);
            pausePanel.SetActive(panel == pausePanel);
            completePanel.SetActive(panel == completePanel);
        }

        private void OrbitCamera()
        {
            if (cam == null || orbitCenter == null) return;
            angle += 5f * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 center = orbitCenter.position;
            cam.transform.position = center + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 27f + Vector3.up * 12f;
            cam.transform.LookAt(center + Vector3.up * 3f);
        }

        private IEnumerator Fade(float target, float seconds)
        {
            if (fade == null) yield break;
            float from = fade.alpha;
            float elapsed = 0f;
            fade.blocksRaycasts = true;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                fade.alpha = Mathf.Lerp(from, target, elapsed / seconds);
                yield return null;
            }
            fade.alpha = target;
            fade.blocksRaycasts = target > 0.5f;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem));
            go.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private static void SetCursor(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private static bool EscapePressed() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    }
}
