using System.Collections.Generic;
using EcoGenesis.Data;
using EcoGenesis.Interaction;
using EcoGenesis.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Sistema de feedback visual flotante (Floating Score & Feedback):
    /// Muestra un cartel animado con estilo y código de colores sobre el contenedor o jugador
    /// cada vez que se clasifica un residuo (+10 puntos, -5 error) o se completa una misión.
    /// Se auto-instancia al cargar cualquier escena (cero configuración requerida).
    /// </summary>
    public class FloatingScoreFeedback : MonoBehaviour
    {
        private class FloatingItem
        {
            public RectTransform rect;
            public CanvasGroup canvasGroup;
            public Vector3 worldOrigin;
            public float elapsed;
            public float duration;
            public float floatHeight;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI subtitleText;
        }

        private Canvas canvas;
        private RectTransform canvasRect;
        private Camera mainCamera;
        private readonly List<FloatingItem> activeItems = new List<FloatingItem>();

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
            if (FindFirstObjectByType<FloatingScoreFeedback>() != null) return;
            var go = new GameObject("FloatingScoreFeedback");
            go.AddComponent<FloatingScoreFeedback>();
        }

        private void Awake()
        {
            CreateCanvas();
        }

        private void OnEnable()
        {
            GameEvents.TrashSorted += OnTrashSorted;
            GameEvents.MissionCompleted += OnMissionCompleted;
        }

        private void OnDisable()
        {
            GameEvents.TrashSorted -= OnTrashSorted;
            GameEvents.MissionCompleted -= OnMissionCompleted;
        }

        private void CreateCanvas()
        {
            var go = new GameObject("FloatingFeedbackCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);

            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 25; // Por encima del juego, pero debajo de menús modales

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasRect = go.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            {
                mainCamera = Camera.main;
            }

            float dt = Time.unscaledDeltaTime;

            for (int i = activeItems.Count - 1; i >= 0; i--)
            {
                var item = activeItems[i];
                if (item.rect == null)
                {
                    activeItems.RemoveAt(i);
                    continue;
                }

                item.elapsed += dt;
                float progress = Mathf.Clamp01(item.elapsed / item.duration);

                // Posición proyectada en pantalla con elevación suave
                Vector3 currentWorldPos = item.worldOrigin + Vector3.up * Mathf.Lerp(0f, item.floatHeight, EaseOutCubic(progress));
                Vector2 screenPos = WorldToCanvasPosition(currentWorldPos);
                item.rect.anchoredPosition = screenPos;

                // Animación de escala: Pop de entrada y estabilización
                if (progress < 0.2f)
                {
                    float pop = Mathf.Lerp(0.6f, 1.12f, progress / 0.2f);
                    item.rect.localScale = Vector3.one * pop;
                }
                else if (progress < 0.35f)
                {
                    float settle = Mathf.Lerp(1.12f, 1.0f, (progress - 0.2f) / 0.15f);
                    item.rect.localScale = Vector3.one * settle;
                }
                else
                {
                    item.rect.localScale = Vector3.one;
                }

                // Desvanecimiento suave al final
                if (progress > 0.7f)
                {
                    item.canvasGroup.alpha = Mathf.Lerp(1f, 0f, (progress - 0.7f) / 0.3f);
                }
                else
                {
                    item.canvasGroup.alpha = Mathf.Clamp01(progress / 0.15f);
                }

                // Destrucción al terminar la animación
                if (progress >= 1f)
                {
                    Destroy(item.rect.gameObject);
                    activeItems.RemoveAt(i);
                }
            }
        }

        private Vector2 WorldToCanvasPosition(Vector3 worldPos)
        {
            if (mainCamera == null || canvasRect == null) return Vector2.zero;

            Vector3 viewPos = mainCamera.WorldToViewportPoint(worldPos);
            // Si el objeto está detrás de la cámara, mantenerlo en el centro visible
            if (viewPos.z < 0f) viewPos = new Vector3(0.5f, 0.5f, 1f);

            // Restringir a los bordes de pantalla para que no se salga
            float clampedX = Mathf.Clamp(viewPos.x, 0.15f, 0.85f);
            float clampedY = Mathf.Clamp(viewPos.y, 0.2f, 0.85f);

            return new Vector2(
                (clampedX - 0.5f) * canvasRect.sizeDelta.x,
                (clampedY - 0.5f) * canvasRect.sizeDelta.y
            );
        }

        private void OnTrashSorted(TrashData data, bool correct)
        {
            if (data == null) return;

            Vector3 spawnPos = GetBestFeedbackPosition();

            if (correct)
            {
                string title = $"+{data.points} PUNTOS";
                string subtitle = $"¡{data.displayName.ToUpper()} RECICLADO!";
                Color color = UiKit.GreenLight;
                Color border = UiKit.GreenVibrant;
                SpawnPopup(spawnPos, title, subtitle, color, border, UiKit.IconLeaf);
            }
            else
            {
                string title = $"-{data.wrongPenalty} PUNTOS";
                string subtitle = $"¡Tacho Incorrecto!";
                if (!string.IsNullOrEmpty(data.recyclingTip))
                {
                    subtitle += $" ({data.recyclingTip})";
                }
                Color color = new Color(1f, 0.42f, 0.42f, 1f);
                Color border = new Color(0.9f, 0.2f, 0.2f, 0.85f);
                SpawnPopup(spawnPos, title, subtitle, color, border, UiKit.IconInfo);
            }
        }

        private void OnMissionCompleted(MissionData mission)
        {
            if (mission == null) return;
            Vector3 spawnPos = GetBestFeedbackPosition();
            string title = "🏆 ¡MISIÓN COMPLETADA!";
            string subtitle = mission.title;
            Color gold = UiKit.Gold;
            SpawnPopup(spawnPos, title, subtitle, gold, gold, UiKit.IconStar, 2.4f, 1.6f);
        }

        private Vector3 GetBestFeedbackPosition()
        {
            var player = FindFirstObjectByType<PlayerController>();
            Vector3 basePos = player != null ? player.transform.position + Vector3.up * 1.8f : Vector3.zero;

            // Si hay un contenedor de basura muy cerca del jugador, usar la parte superior del contenedor
            var bins = FindObjectsByType<BinController>();
            float closestDist = 3.5f;
            foreach (var bin in bins)
            {
                if (bin == null) continue;
                float d = Vector3.Distance(player != null ? player.transform.position : Vector3.zero, bin.transform.position);
                if (d < closestDist)
                {
                    closestDist = d;
                    basePos = bin.transform.position + Vector3.up * 1.9f;
                }
            }

            // Si ya hay carteles activos en esa misma zona, elevar ligeramente el nuevo para evitar solapamiento
            basePos += Vector3.up * (activeItems.Count * 0.35f);
            return basePos;
        }

        private void SpawnPopup(Vector3 worldPos, string title, string subtitle, Color textColor, Color borderColor, Sprite icon, float duration = 1.8f, float floatHeight = 1.3f)
        {
            if (canvas == null) CreateCanvas();

            // Panel contenedor del badge
            var card = UiKit.MakeBorderedPanel(
                canvas.transform,
                "FeedbackBadge",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(360f, 76f),
                new Color(0.04f, 0.09f, 0.07f, 0.94f),
                borderColor,
                2f
            );

            var group = card.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Icono temático
            if (icon != null)
            {
                UiKit.MakeIcon(card.transform, icon, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(32f, 32f), textColor);
            }

            // Título principal (+10 PUNTOS)
            var tTitle = UiKit.MakeLabel(
                card.transform,
                title,
                23f,
                textColor,
                TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f),
                new Vector2(66f, 12f),
                new Vector2(280f, 28f),
                FontStyles.Bold
            );

            // Subtítulo (¡PLÁSTICO RECICLADO!)
            var tSubtitle = UiKit.MakeLabel(
                card.transform,
                subtitle,
                15f,
                UiKit.Cream,
                TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f),
                new Vector2(66f, -14f),
                new Vector2(280f, 24f),
                FontStyles.Normal
            );

            activeItems.Add(new FloatingItem
            {
                rect = card.rectTransform,
                canvasGroup = group,
                worldOrigin = worldPos,
                elapsed = 0f,
                duration = duration,
                floatHeight = floatHeight,
                titleText = tTitle,
                subtitleText = tSubtitle
            });
        }

        private static float EaseOutCubic(float x)
        {
            return 1f - Mathf.Pow(1f - x, 3f);
        }
    }
}
