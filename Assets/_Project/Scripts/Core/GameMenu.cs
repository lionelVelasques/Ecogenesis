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
        private float angle = 42f;

        private CanvasGroup fade;
        private GameObject mainPanel, levelsPanel, optionsPanel, creditsPanel, pausePanel, completePanel;
        private RectTransform mainButtons;
        private GameObject optionsReturn;
        private TextMeshProUGUI qualityLabel, completeTitle, completePointsText, completeAccuracyText, completeTimeText;
        private Image[] completeStars;
        private Button nextButton;
        private int currentIndex;

        // ------------------------------------------------------------------ CICLO DE VIDA

        private void Awake()
        {
            Time.timeScale = 1f;
            cam = Camera.main;
            input = FindAnyObjectByType<PlayerInputReader>();
            gameCamera = FindAnyObjectByType<CameraController>();
            save = FindAnyObjectByType<SaveSystem>();
            hud = FindAnyObjectByType<GameHud>();
            level = FindAnyObjectByType<LevelManager>();
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

#if UNITY_EDITOR
            // En el editor de Unity, iniciar directamente la partida para probar sin trabas
            StartCoroutine(BeginPlay(false, false));
#else
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
#endif
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
                completeStars[i].color = i < result.stars ? UiKit.Gold : new Color(1f, 1f, 1f, 0.16f);
            int minutes = Mathf.FloorToInt(result.seconds / 60f);
            int seconds = Mathf.FloorToInt(result.seconds % 60f);
            completePointsText.text = result.points.ToString();
            completeAccuracyText.text = Mathf.RoundToInt(result.accuracy * 100f) + "%";
            completeTimeText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
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

        private static void AddModalHeader(Transform card, string badgeText, Sprite badgeIcon, string titleText, float cardWidth, float pad = 48f)
        {
            Image badge = UiKit.MakePanel(card, "Badge", new Vector2(0f, 1f), new Vector2(pad, -38f),
                new Vector2(290f, 34f), new Color(0.12f, 0.28f, 0.19f, 0.95f));
            badge.raycastTarget = false;
            UiKit.MakeIcon(badge.transform, badgeIcon, new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(18f, 18f), UiKit.GreenLight);
            UiKit.MakeLabel(badge.transform, badgeText, 16f, UiKit.GreenLight, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(38f, 0f), new Vector2(240f, 28f), FontStyles.Bold);

            UiKit.MakeLabel(card, titleText, 48f, UiKit.Cream, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(pad, -82f), new Vector2(cardWidth - pad * 2f, 62f), FontStyles.Bold);

            UiKit.MakePanel(card, "DividerBg", new Vector2(0f, 1f), new Vector2(pad, -152f),
                new Vector2(cardWidth - pad * 2f, 4f), new Color(1f, 1f, 1f, 0.08f)).raycastTarget = false;
            UiKit.MakePanel(card, "DividerAccent", new Vector2(0f, 1f), new Vector2(pad, -152f),
                new Vector2(125f, 4f), UiKit.GreenLight).raycastTarget = false;
        }

        private static void AttachShimmer(Button btn, float width)
        {
            btn.gameObject.AddComponent<RectMask2D>();
            RectTransform shineRt = UiKit.Rect(btn.transform, "Shimmer", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(90f, 140f));
            shineRt.localRotation = Quaternion.Euler(0f, 0f, -22f);
            var shineImg = shineRt.gameObject.AddComponent<Image>();
            shineImg.sprite = UiKit.Gradient;
            shineImg.color = new Color(1f, 1f, 0.92f, 0.22f);
            shineImg.raycastTarget = false;
            shineRt.gameObject.AddComponent<UiShimmer>().travelWidth = width + 100f;
        }

        private GameObject BuildMain(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Main").gameObject;

            RectTransform sideGlow = UiKit.Stretch(p.transform, "SideGlow");
            sideGlow.anchorMin = new Vector2(0f, 0f);
            sideGlow.anchorMax = new Vector2(0f, 1f);
            sideGlow.pivot = new Vector2(0f, 0.5f);
            sideGlow.sizeDelta = new Vector2(1150f, 0f);
            var sideImage = sideGlow.gameObject.AddComponent<Image>();
            sideImage.sprite = UiKit.Gradient;
            sideImage.color = new Color(0.02f, 0.06f, 0.04f, 0.78f);
            sideImage.raycastTarget = false;

            RectTransform motes = UiKit.Stretch(p.transform, "AmbientParticles");
            motes.gameObject.AddComponent<UiAmbientParticles>();

            UiKit.MakeGlow(p.transform, "CardGlow", new Vector2(0f, 0.5f), new Vector2(36f, 0f),
                new Vector2(682f, 1012f), new Color(0.32f, 0.88f, 0.52f, 0.34f), true);

            Image card = UiKit.MakeBorderedPanel(p.transform, "MainCard", new Vector2(0f, 0.5f), new Vector2(72f, 0f),
                new Vector2(610f, 940f), UiKit.CardGlass, UiKit.CardBorder, 2.5f);
            card.gameObject.AddComponent<UiEntrance>().duration = 0.4f;

            Image badge = UiKit.MakePanel(card.transform, "Badge", new Vector2(0f, 1f), new Vector2(48f, -46f),
                new Vector2(330f, 38f), new Color(0.12f, 0.28f, 0.19f, 0.95f));
            badge.raycastTarget = false;
            UiKit.MakeIcon(badge.transform, UiKit.IconLeaf, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(20f, 20f), UiKit.GreenLight);
            UiKit.MakeLabel(badge.transform, "AVENTURA AMBIENTAL · 3D", 17f, UiKit.GreenLight, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(42f, 0f), new Vector2(275f, 30f), FontStyles.Bold);

            var titleLabel = UiKit.MakeLabel(card.transform, "<color=#F2E6C9>ECO</color><color=#8CE09B>GENESIS</color>", 66f, UiKit.Cream,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(46f, -100f), new Vector2(530f, 84f), FontStyles.Bold);
            titleLabel.textWrappingMode = TextWrappingModes.NoWrap;

            UiKit.MakeLabel(card.transform, "Restaura el parque. Cuida el planeta.", 27f, UiKit.Soft,
                TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(48f, -188f), new Vector2(514f, 44f));

            UiKit.MakePanel(card.transform, "DividerBg", new Vector2(0f, 1f), new Vector2(48f, -246f),
                new Vector2(514f, 4f), new Color(1f, 1f, 1f, 0.08f)).raycastTarget = false;
            UiKit.MakePanel(card.transform, "DividerAccent", new Vector2(0f, 1f), new Vector2(48f, -246f),
                new Vector2(140f, 4f), UiKit.GreenLight).raycastTarget = false;

            mainButtons = UiKit.Rect(card.transform, "Buttons", new Vector2(0f, 1f), new Vector2(48f, -284f), new Vector2(514f, 540f));

            UiKit.MakeLabel(card.transform, "ODS 11 · 12 · 15   |   Huancayo, Perú", 20f, new Color(0.77f, 0.87f, 0.79f, 0.65f),
                TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(48f, 34f), new Vector2(390f, 32f));

            Image verPill = UiKit.MakePanel(card.transform, "VerPill", new Vector2(1f, 0f), new Vector2(-48f, 32f),
                new Vector2(92f, 30f), new Color(1f, 1f, 1f, 0.06f));
            verPill.raycastTarget = false;
            UiKit.MakeLabel(verPill.transform, "v0.3", 18f, UiKit.Soft, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 26f), FontStyles.Bold);

            return p;
        }

        private void RebuildMainButtons()
        {
            foreach (Transform child in mainButtons) Destroy(child.gameObject);

            bool hasSave = save != null && save.HasSave;
            float y = 0f;
            int step = 0;

            if (hasSave)
            {
                AddPrimaryButton("Continuar partida", y, step++, ContinueGame);
                y += 90f;
                AddSecondaryButton("Nueva partida", y, step++, UiKit.IconPlay, NewGame);
                y += 74f;
            }
            else
            {
                AddPrimaryButton("Nueva partida", y, step++, NewGame);
                y += 90f;
            }

            AddSecondaryButton("Seleccionar nivel", y, step++, UiKit.IconGrid, () => { RefreshLevels(); ShowPanel(levelsPanel); });
            y += 74f;
            AddSecondaryButton("Opciones", y, step++, UiKit.IconSliders, () => OpenOptions(mainPanel));
            y += 74f;
            AddSecondaryButton("Créditos", y, step++, UiKit.IconInfo, () => ShowPanel(creditsPanel));

            Button exitBtn = UiKit.MakeButton(mainButtons, "Salir del juego", new Vector2(0f, 1f), new Vector2(0f, -456f),
                new Vector2(514f, 56f), Quit, UiKit.ButtonExit, 25f, TextAlignmentOptions.Left,
                new Color(0.92f, 0.75f, 0.75f, 0.95f), 24f, UiKit.IconPower, new Color(0.95f, 0.58f, 0.58f, 1f),
                new Color(0.32f, 0.14f, 0.16f, 0.75f));
            exitBtn.gameObject.AddComponent<UiEntrance>().delay = step * 0.06f;
        }

        private void AddPrimaryButton(string label, float y, int step, UnityEngine.Events.UnityAction action)
        {
            Button btn = UiKit.MakeButton(mainButtons, label, new Vector2(0f, 1f), new Vector2(0f, -y),
                new Vector2(514f, 74f), action, UiKit.GreenVibrant, 31f, TextAlignmentOptions.Left, UiKit.Cream,
                28f, UiKit.IconPlay, UiKit.Ink, UiKit.Gold);
            btn.gameObject.AddComponent<UiEntrance>().delay = step * 0.06f;
            AttachShimmer(btn, 514f);
        }

        private void AddSecondaryButton(string label, float y, int step, Sprite icon, UnityEngine.Events.UnityAction action)
        {
            Button btn = UiKit.MakeButton(mainButtons, label, new Vector2(0f, 1f), new Vector2(0f, -y),
                new Vector2(514f, 62f), action, UiKit.ButtonSecondary, 27f, TextAlignmentOptions.Left, UiKit.Soft,
                24f, icon, UiKit.GreenLight, new Color(0.14f, 0.30f, 0.21f, 0.85f));
            btn.gameObject.AddComponent<UiEntrance>().delay = step * 0.06f;
        }

        private void RefreshLevels()
        {
            foreach (Transform child in levelsPanel.transform) Destroy(child.gameObject);

            Image dim = UiKit.MakePanel(levelsPanel.transform, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0.03f, 0.02f, 0.78f));
            dim.raycastTarget = true;
            UiKit.MakeLabel(levelsPanel.transform, "SELECCIONAR NIVEL", 56f, UiKit.Cream, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1200f, 80f), FontStyles.Bold);

            int count = LevelCatalog.Levels.Length;
            for (int i = 0; i < count; i++)
            {
                LevelCatalog.Info info = LevelCatalog.Levels[i];
                bool unlocked = LevelCatalog.IsUnlocked(i);
                int index = i;

                float x = (i - (count - 1) * 0.5f) * 560f;
                if (unlocked)
                {
                    UiKit.MakeGlow(levelsPanel.transform, "Glow" + i, new Vector2(0.5f, 0.5f), new Vector2(x, -12f),
                        new Vector2(576f, 696f), new Color(info.color.r, info.color.g, info.color.b, 0.28f), true);
                }

                Image card = UiKit.MakeBorderedPanel(levelsPanel.transform, "Card" + i, new Vector2(0.5f, 0.5f), new Vector2(x, -12f),
                    new Vector2(520f, 640f), UiKit.PanelSolid, unlocked ? UiKit.CardBorder : new Color(1f, 1f, 1f, 0.12f), 2f);
                card.gameObject.AddComponent<UiEntrance>().delay = i * 0.08f;

                Image band = UiKit.MakePanel(card.transform, "Band", new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(516f, 150f), unlocked ? info.color : new Color(0.26f, 0.29f, 0.28f, 1f));
                band.raycastTarget = false;
                UiKit.MakeLabel(band.transform, "NIVEL " + (i + 1), 24f, new Color(1f, 1f, 1f, 0.85f), TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -18f), new Vector2(300f, 34f), FontStyles.Bold);
                UiKit.MakeLabel(band.transform, info.title, 40f, Color.white, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -58f), new Vector2(470f, 60f), FontStyles.Bold);
                UiKit.MakeLabel(card.transform, info.description, 25f, UiKit.Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(28f, -176f), new Vector2(464f, 210f));

                int stars = LevelCatalog.Stars(i);
                for (int s = 0; s < 3; s++)
                    UiKit.MakeIcon(card.transform, UiKit.IconStar, new Vector2(0f, 0f), new Vector2(28f + s * 54f, 148f), new Vector2(42f, 42f), s < stars ? UiKit.Gold : new Color(1f, 1f, 1f, 0.16f));

                string status = !unlocked ? "Completa el nivel anterior" : (LevelCatalog.IsCompleted(i) ? "Completado" : "Disponible");
                UiKit.MakeLabel(card.transform, status, 24f, unlocked ? UiKit.GreenLight : UiKit.Cream, TextAlignmentOptions.BottomLeft, new Vector2(0f, 0f), new Vector2(28f, 104f), new Vector2(464f, 34f));

                Button play = UiKit.MakeButton(card.transform, unlocked ? "Jugar nivel" : "Bloqueado", new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(440f, 64f),
                    () => LoadLevel(index, false), unlocked ? info.color : UiKit.Green, 28f, TextAlignmentOptions.Center, UiKit.Cream, 24f,
                    unlocked ? UiKit.IconPlay : null, UiKit.Cream);
                play.interactable = unlocked;
            }

            UiKit.MakeButton(levelsPanel.transform, "Volver al menú", new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(360f, 62f), () => ShowPanel(mainPanel), UiKit.ButtonSecondary, 26f);
        }

        private GameObject BuildOptions(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Options").gameObject;
            AddDim(p.transform);
            UiKit.MakeGlow(p.transform, "Glow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(964f, 784f), new Color(0.32f, 0.88f, 0.52f, 0.28f), true);
            Image card = UiKit.MakeBorderedPanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 720f), UiKit.PanelSolid, UiKit.CardBorder, 2.5f);
            card.gameObject.AddComponent<UiEntrance>();
            AddModalHeader(card.transform, "CONFIGURACIÓN", UiKit.IconSliders, "OPCIONES", 900f, 48f);

            AddOptionRow(card.transform, "Volumen general", -184f, GameSettings.Master, GameSettings.SetMaster);
            AddOptionRow(card.transform, "Música", -272f, GameSettings.Music, GameSettings.SetMusic);
            AddOptionRow(card.transform, "Efectos y ambiente", -360f, GameSettings.Effects, GameSettings.SetEffects);

            Image qualityRow = UiKit.MakePanel(card.transform, "QualityRow", new Vector2(0.5f, 1f), new Vector2(0f, -456f),
                new Vector2(804f, 76f), new Color(0f, 0f, 0f, 0.26f));
            UiKit.MakeLabel(qualityRow.transform, "Calidad gráfica", 28f, UiKit.Cream, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(340f, 44f), FontStyles.Bold);
            Button quality = UiKit.MakeButton(qualityRow.transform, "", new Vector2(1f, 0.5f), new Vector2(-18f, 0f),
                new Vector2(380f, 52f), CycleQuality, UiKit.Blue, 25f);
            qualityLabel = quality.GetComponentInChildren<TextMeshProUGUI>();
            qualityLabel.text = QualitySettings.names[QualitySettings.GetQualityLevel()];

            UiKit.MakeButton(card.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(380f, 62f),
                CloseOptions, UiKit.ButtonSecondary, 27f);
            return p;
        }

        private static void AddOptionRow(Transform parent, string label, float y, float value, UnityEngine.Events.UnityAction<float> onChange)
        {
            Image row = UiKit.MakePanel(parent, label + "Row", new Vector2(0.5f, 1f), new Vector2(0f, y),
                new Vector2(804f, 74f), new Color(0f, 0f, 0f, 0.26f));
            UiKit.MakeLabel(row.transform, label, 28f, UiKit.Cream, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(320f, 44f), FontStyles.Bold);

            Slider slider = UiKit.MakeSlider(row.transform, label, new Vector2(0f, 0.5f), new Vector2(354f, 0f), new Vector2(340f, 24f));
            TextMeshProUGUI pctLabel = UiKit.MakeLabel(row.transform, Mathf.RoundToInt(value * 100f) + "%", 25f, UiKit.GreenLight,
                TextAlignmentOptions.Right, new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(80f, 40f), FontStyles.Bold);

            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v =>
            {
                onChange?.Invoke(v);
                pctLabel.text = Mathf.RoundToInt(v * 100f) + "%";
            });
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
            UiKit.MakeGlow(p.transform, "Glow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1044f, 804f), new Color(0.32f, 0.88f, 0.52f, 0.28f), true);
            Image card = UiKit.MakeBorderedPanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(980f, 740f), UiKit.PanelSolid, UiKit.CardBorder, 2.5f);
            card.gameObject.AddComponent<UiEntrance>();
            AddModalHeader(card.transform, "ACERCA DEL PROYECTO", UiKit.IconInfo, "CRÉDITOS", 980f, 48f);

            // Bloque principal de descripción
            UiKit.MakeLabel(card.transform,
                "<color=#F2E6C9><b>EcoGenesis 3D</b></color> — Videojuego de aventura, exploración y restauración ambiental.",
                27f, UiKit.Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(48f, -182f), new Vector2(884f, 64f));

            // Tarjeta destacada ODS
            Image odsBox = UiKit.MakeBorderedPanel(card.transform, "OdsBox", new Vector2(0.5f, 1f), new Vector2(0f, -258f),
                new Vector2(884f, 108f), new Color(0.09f, 0.20f, 0.14f, 0.85f), new Color(0.42f, 0.80f, 0.52f, 0.28f), 1.5f);
            UiKit.MakeIcon(odsBox.transform, UiKit.IconLeaf, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(30f, 30f), UiKit.GreenLight);
            UiKit.MakeLabel(odsBox.transform,
                "Basado en la problemática real de los residuos sólidos.\n<color=#8CE09B><b>Objetivos de Desarrollo Sostenible 11, 12 y 15</b></color>",
                25f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(72f, 0f), new Vector2(780f, 82f));

            // Dos tarjetas técnicas lado a lado
            Image techLeft = UiKit.MakePanel(card.transform, "TechLeft", new Vector2(0f, 1f), new Vector2(48f, -386f),
                new Vector2(430f, 96f), new Color(0f, 0f, 0f, 0.26f));
            UiKit.MakeLabel(techLeft.transform, "<color=#8CE09B><b>TECNOLOGÍA</b></color>\nMotor: Unity 6 (URP)  ·  Código: C#",
                23f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(386f, 74f));

            Image techRight = UiKit.MakePanel(card.transform, "TechRight", new Vector2(1f, 1f), new Vector2(-48f, -386f),
                new Vector2(430f, 96f), new Color(0f, 0f, 0f, 0.26f));
            UiKit.MakeLabel(techRight.transform, "<color=#8CE09B><b>ARTE 3D</b></color>\nModelos de naturaleza: Quaternius (CC0)",
                23f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(386f, 74f));

            // Pie institucional
            Image uniBox = UiKit.MakePanel(card.transform, "UniBox", new Vector2(0.5f, 1f), new Vector2(0f, -502f),
                new Vector2(884f, 68f), new Color(0f, 0f, 0f, 0.26f));
            UiKit.MakeLabel(uniBox.transform, "Universidad Continental — Huancayo, Perú",
                26f, UiKit.Gold, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(840f, 44f), FontStyles.Bold);

            UiKit.MakeButton(card.transform, "Volver", new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(380f, 62f),
                () => ShowPanel(mainPanel), UiKit.ButtonSecondary, 27f);
            return p;
        }

        private GameObject BuildPause(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Pause").gameObject;
            AddDim(p.transform);
            UiKit.MakeGlow(p.transform, "Glow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(654f, 694f), new Color(0.32f, 0.88f, 0.52f, 0.28f), true);
            Image card = UiKit.MakeBorderedPanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(590f, 630f), UiKit.PanelSolid, UiKit.CardBorder, 2.5f);
            card.gameObject.AddComponent<UiEntrance>();
            AddModalHeader(card.transform, "JUEGO EN PAUSA", UiKit.IconLeaf, "PAUSA", 590f, 48f);

            float y = -184f;
            Button resumeBtn = UiKit.MakeButton(card.transform, "Continuar", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(494f, 72f), Resume,
                UiKit.GreenVibrant, 30f, TextAlignmentOptions.Left, UiKit.Cream, 26f, UiKit.IconPlay, UiKit.Ink, UiKit.Gold);
            AttachShimmer(resumeBtn, 494f);
            y -= 88f;

            UiKit.MakeButton(card.transform, "Guardar partida", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(494f, 62f),
                () => { if (save != null) save.Save(true); },
                UiKit.ButtonSecondary, 26f, TextAlignmentOptions.Left, UiKit.Soft, 24f, UiKit.IconSave, UiKit.GreenLight, new Color(0.14f, 0.30f, 0.21f, 0.85f));
            y -= 76f;

            UiKit.MakeButton(card.transform, "Opciones", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(494f, 62f),
                () => OpenOptions(pausePanel),
                UiKit.ButtonSecondary, 26f, TextAlignmentOptions.Left, UiKit.Soft, 24f, UiKit.IconSliders, UiKit.GreenLight, new Color(0.14f, 0.30f, 0.21f, 0.85f));
            y -= 84f;

            UiKit.MakePanel(card.transform, "ExitDivider", new Vector2(0.5f, 1f), new Vector2(0f, y),
                new Vector2(494f, 2f), new Color(1f, 1f, 1f, 0.08f)).raycastTarget = false;
            y -= 18f;

            UiKit.MakeButton(card.transform, "Menú principal", new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(494f, 58f), BackToMainMenu,
                UiKit.ButtonExit, 25f, TextAlignmentOptions.Left, new Color(0.92f, 0.75f, 0.75f, 0.95f), 24f, UiKit.IconPower,
                new Color(0.95f, 0.58f, 0.58f, 1f), new Color(0.32f, 0.14f, 0.16f, 0.75f));
            return p;
        }

        private GameObject BuildComplete(Transform parent)
        {
            GameObject p = UiKit.Stretch(parent, "Complete").gameObject;
            AddDim(p.transform);
            UiKit.MakeGlow(p.transform, "Glow", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(964f, 844f), new Color(0.95f, 0.76f, 0.30f, 0.34f), true);
            Image card = UiKit.MakeBorderedPanel(p.transform, "Card", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 780f),
                UiKit.PanelSolid, new Color(0.95f, 0.76f, 0.30f, 0.62f), 2.5f);
            card.gameObject.AddComponent<UiEntrance>();

            // Insignia superior dorada
            Image badge = UiKit.MakePanel(card.transform, "Badge", new Vector2(0.5f, 1f), new Vector2(0f, -34f),
                new Vector2(260f, 34f), new Color(0.30f, 0.24f, 0.10f, 0.95f));
            badge.raycastTarget = false;
            UiKit.MakeIcon(badge.transform, UiKit.IconStar, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(18f, 18f), UiKit.Gold);
            UiKit.MakeLabel(badge.transform, "MISIÓN CUMPLIDA", 16f, UiKit.Gold, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(210f, 28f), FontStyles.Bold);

            UiKit.MakeLabel(card.transform, "¡NIVEL COMPLETADO!", 52f, UiKit.Gold, TextAlignmentOptions.Top,
                new Vector2(0.5f, 1f), new Vector2(0f, -78f), new Vector2(820f, 66f), FontStyles.Bold);
            completeTitle = UiKit.MakeLabel(card.transform, "", 32f, UiKit.Cream, TextAlignmentOptions.Top,
                new Vector2(0.5f, 1f), new Vector2(0f, -142f), new Vector2(820f, 44f));

            // 3 estrellas sobre ranuras oscuras
            completeStars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                float sx = (i - 1) * 124f;
                Image socket = UiKit.MakeCircle(card.transform, "Socket" + i, new Vector2(0.5f, 1f), new Vector2(sx, -196f),
                    new Vector2(96f, 96f), new Color(0f, 0f, 0f, 0.32f));
                completeStars[i] = UiKit.MakeIcon(socket.transform, UiKit.IconStar, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(68f, 68f), UiKit.Gold);
            }

            // 3 tarjetas de métricas lado a lado
            completePointsText = MakeStatCard(card.transform, "PUNTOS ECO", -254f, -314f, UiKit.GreenLight);
            completeAccuracyText = MakeStatCard(card.transform, "PRECISIÓN", 0f, -314f, UiKit.Cream);
            completeTimeText = MakeStatCard(card.transform, "TIEMPO", 254f, -314f, UiKit.Gold);

            // Botones de acción inferiores
            nextButton = UiKit.MakeButton(card.transform, "Siguiente nivel", new Vector2(0.5f, 0f), new Vector2(0f, 186f), new Vector2(490f, 68f),
                () => LoadLevel(currentIndex + 1, false), UiKit.GreenVibrant, 29f, TextAlignmentOptions.Left, UiKit.Cream, 26f,
                UiKit.IconPlay, UiKit.Ink, UiKit.Gold);
            AttachShimmer(nextButton, 490f);

            UiKit.MakeButton(card.transform, "Repetir nivel", new Vector2(0.5f, 0f), new Vector2(0f, 110f), new Vector2(490f, 60f),
                () => { if (save != null) save.Delete(); LoadLevel(currentIndex, false); },
                UiKit.ButtonSecondary, 26f, TextAlignmentOptions.Left, UiKit.Soft, 24f, UiKit.IconRefresh, UiKit.GreenLight,
                new Color(0.14f, 0.30f, 0.21f, 0.85f));

            UiKit.MakeButton(card.transform, "Menú principal", new Vector2(0.5f, 0f), new Vector2(0f, 36f), new Vector2(490f, 60f),
                BackToMainMenu, UiKit.ButtonSecondary, 26f, TextAlignmentOptions.Left, UiKit.Soft, 24f, UiKit.IconPower, UiKit.GreenLight,
                new Color(0.14f, 0.30f, 0.21f, 0.85f));
            return p;
        }

        private static TextMeshProUGUI MakeStatCard(Transform parent, string title, float x, float y, Color valueColor)
        {
            Image box = UiKit.MakeBorderedPanel(parent, title + "Card", new Vector2(0.5f, 1f), new Vector2(x, y),
                new Vector2(236f, 134f), new Color(0f, 0f, 0f, 0.28f), new Color(1f, 1f, 1f, 0.08f), 1.5f);
            UiKit.MakeLabel(box.transform, title, 18f, UiKit.Soft, TextAlignmentOptions.Top,
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(210f, 28f), FontStyles.Bold);
            return UiKit.MakeLabel(box.transform, "0", 42f, valueColor, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), new Vector2(0f, -12f), new Vector2(210f, 54f), FontStyles.Bold);
        }

        private static void AddDim(Transform parent)
        {
            Image dim = UiKit.MakePanel(parent, "Dim", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(4000f, 4000f), new Color(0f, 0.03f, 0.02f, 0.76f));
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
            angle += 2.4f * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 center = orbitCenter.position;
            cam.transform.position = center + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * 18f + Vector3.up * 7.2f;
            cam.transform.LookAt(center + Vector3.up * 2.2f);
            cam.transform.LookAt(center + Vector3.up * 2.2f - cam.transform.right * 3.8f);
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
            if (FindAnyObjectByType<EventSystem>() != null) return;
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