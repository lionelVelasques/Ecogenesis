using System.Collections;
using EcoGenesis.Audio;
using EcoGenesis.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Menú principal (cámara orbitando el parque), menú de pausa (Esc), opciones de volumen, créditos,
    /// guardado/carga y fundidos a negro. Las referencias las asigna la herramienta del editor.
    /// </summary>
    public class MenuController : MonoBehaviour
    {
        [Header("Paneles")]
        public GameObject mainPanel;
        public GameObject optionsPanel;
        public GameObject creditsPanel;
        public GameObject pausePanel;
        public CanvasGroup hud;
        public CanvasGroup fade;

        [Header("Botones")]
        public Button continueButton;
        public Button newGameButton;
        public Button optionsButton;
        public Button creditsButton;
        public Button quitButton;
        public Button resumeButton;
        public Button saveButton;
        public Button pauseOptionsButton;
        public Button pauseMenuButton;
        public Button optionsBackButton;
        public Button creditsBackButton;

        [Header("Opciones")]
        public Slider masterSlider;
        public Slider musicSlider;
        public Slider effectsSlider;

        [Header("Juego")]
        public Transform orbitCenter;
        public PlayerInputReader playerInput;
        public CameraController gameCamera;
        public SaveSystem saveSystem;
        public float orbitRadius = 27f;
        public float orbitHeight = 12f;
        public float orbitSpeed = 5f;

        private enum State { Menu, Playing, Paused }

        private State state = State.Menu;
        private bool optionsFromPause;
        private float angle = 200f;
        private Camera cam;

        private void Awake()
        {
            cam = Camera.main;
            Time.timeScale = 1f;

            if (gameCamera != null) gameCamera.enabled = false;
            if (playerInput != null) playerInput.InputEnabled = false;
            SetHud(false);
            ShowPanel(mainPanel);
            SetCursor(true);
            if (fade != null) fade.alpha = 1f;
        }

        private void Start()
        {
            Bind(continueButton, () => StartCoroutine(StartGame(true)));
            Bind(newGameButton, () =>
            {
                if (saveSystem != null) saveSystem.Delete();
                StartCoroutine(StartGame(false));
            });
            Bind(optionsButton, () => OpenOptions(false));
            Bind(creditsButton, () => ShowPanel(creditsPanel));
            Bind(quitButton, Quit);
            Bind(resumeButton, Resume);
            Bind(saveButton, () => { if (saveSystem != null) saveSystem.Save(true); });
            Bind(pauseOptionsButton, () => OpenOptions(true));
            Bind(pauseMenuButton, BackToMainMenu);
            Bind(optionsBackButton, CloseOptions);
            Bind(creditsBackButton, () => ShowPanel(mainPanel));

            if (continueButton != null) continueButton.gameObject.SetActive(saveSystem != null && saveSystem.HasSave);

            Hook(masterSlider, GameSettings.Master, GameSettings.SetMaster);
            Hook(musicSlider, GameSettings.Music, GameSettings.SetMusic);
            Hook(effectsSlider, GameSettings.Effects, GameSettings.SetEffects);

            StartCoroutine(Fade(0f, 1.2f));
        }

        private void Update()
        {
            switch (state)
            {
                case State.Menu:
                    OrbitCamera();
                    break;
                case State.Playing:
                    if (EscapePressed()) Pause();
                    break;
                case State.Paused:
                    if (EscapePressed())
                    {
                        if (optionsPanel != null && optionsPanel.activeSelf) CloseOptions();
                        else Resume();
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ FLUJO

        private IEnumerator StartGame(bool load)
        {
            yield return Fade(1f, 0.5f);

            state = State.Playing;
            ShowPanel(null);
            SetHud(true);
            if (load && saveSystem != null) saveSystem.Load();
            if (saveSystem != null) saveSystem.AutoSave = true;
            if (gameCamera != null) gameCamera.enabled = true;
            if (playerInput != null) playerInput.InputEnabled = true;
            SetCursor(false);

            yield return Fade(0f, 0.7f);
        }

        private void Pause()
        {
            state = State.Paused;
            Time.timeScale = 0f;
            if (playerInput != null) playerInput.InputEnabled = false;
            if (gameCamera != null) gameCamera.enabled = false;
            ShowPanel(pausePanel);
            SetCursor(true);
        }

        private void Resume()
        {
            state = State.Playing;
            Time.timeScale = 1f;
            ShowPanel(null);
            if (playerInput != null) playerInput.InputEnabled = true;
            if (gameCamera != null) gameCamera.enabled = true;
            SetCursor(false);
        }

        private void BackToMainMenu()
        {
            if (saveSystem != null) saveSystem.Save(false);
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OpenOptions(bool fromPause)
        {
            optionsFromPause = fromPause;
            ShowPanel(optionsPanel);
        }

        private void CloseOptions() => ShowPanel(optionsFromPause ? pausePanel : mainPanel);

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ AYUDAS

        private void OrbitCamera()
        {
            if (cam == null || orbitCenter == null) return;

            angle += orbitSpeed * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 center = orbitCenter.position;
            cam.transform.position = center + new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * orbitRadius + Vector3.up * orbitHeight;
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

        private void ShowPanel(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
            if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
            if (pausePanel != null) pausePanel.SetActive(panel == pausePanel);
        }

        private void SetHud(bool visible)
        {
            if (hud == null) return;
            hud.alpha = visible ? 1f : 0f;
            hud.blocksRaycasts = false;
        }

        private static void SetCursor(bool visible)
        {
            Cursor.lockState = visible ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = visible;
        }

        private static bool EscapePressed() => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        private static void Bind(Button button, UnityAction action)
        {
            if (button == null) return;
            button.onClick.AddListener(action);
            button.onClick.AddListener(AudioDirector.PlayUiClick);
        }

        private static void Hook(Slider slider, float value, UnityAction<float> onChange)
        {
            if (slider == null) return;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(onChange);
        }
    }
}
