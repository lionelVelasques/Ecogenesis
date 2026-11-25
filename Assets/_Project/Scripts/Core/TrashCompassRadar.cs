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
    /// Radar y Brújula de Residuos de Alta Precisión (Smart Eco-Tracker):
    /// - Se activa automáticamente cuando el jugador alcanza el 85% de recolección (últimos residuos difíciles de encontrar).
    /// - Antes del 85% permanece oculto para mantener la pantalla limpia.
    /// - Muestra una flecha grande, gruesa y brillante que rota 360° apuntando en tiempo real al residuo más cercano.
    /// - Muestra la distancia exacta en metros con pulso visual.
    /// </summary>
    public class TrashCompassRadar : MonoBehaviour
    {
        private const float ActivationThreshold = 0.85f; // Se activa al 85% de limpieza

        private Canvas canvas;
        private CanvasGroup canvasGroup;
        private RectTransform radarPanel;
        private Image arrowImage;
        private RectTransform arrowRect;
        private Image dialBg;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI distanceText;

        private Camera mainCamera;
        private PlayerController player;
        private EnvironmentalManager environment;
        private TrashItem closestTrash;
        private float closestDistance;
        private int remainingTrashCount;
        private int initialTrashCount;
        private bool hasInitializedCounts;

        private float sampleTimer;
        private float currentAngle;
        private float angleVelocity;
        private static Sprite thickArrowSprite;

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
            if (FindFirstObjectByType<TrashCompassRadar>() != null) return;
            var go = new GameObject("TrashCompassRadar");
            go.AddComponent<TrashCompassRadar>();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void Start()
        {
            environment = FindFirstObjectByType<EnvironmentalManager>();
            player = FindFirstObjectByType<PlayerController>();
            RegistrarConteoInicial();
        }

        private void RegistrarConteoInicial()
        {
            var items = FindObjectsByType<TrashItem>();
            initialTrashCount = items.Length;
            remainingTrashCount = initialTrashCount;
            hasInitializedCounts = true;
        }

        private void BuildUi()
        {
            var go = new GameObject("TrashCompassCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);

            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 22;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Panel contenedor del Radar en la parte superior central
            Image panel = UiKit.MakeBorderedPanel(
                go.transform,
                "RadarPanel",
                new Vector2(0.5f, 1f),
                new Vector2(0f, -24f),
                new Vector2(330f, 78f),
                new Color(0.03f, 0.08f, 0.06f, 0.94f),
                new Color(0.18f, 0.85f, 0.52f, 0.90f),
                2.5f
            );
            radarPanel = panel.rectTransform;

            canvasGroup = panel.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f; // Empieza oculto hasta llegar al 85%
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Esfera de la brújula (más grande y notoria)
            dialBg = UiKit.MakeCircle(
                radarPanel,
                "CompassDial",
                new Vector2(0f, 0.5f),
                new Vector2(10f, 0f),
                new Vector2(58f, 58f),
                new Color(0.06f, 0.16f, 0.12f, 0.98f)
            );

            // Borde exterior del dial
            UiKit.MakeCircle(
                dialBg.transform,
                "DialRing",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(54f, 54f),
                new Color(0.2f, 0.9f, 0.55f, 0.35f)
            );

            // Flecha grande, gruesa y de alto contraste
            if (thickArrowSprite == null) thickArrowSprite = BuildThickArrowSprite();
            var arrowGO = new GameObject("ThickArrow", typeof(RectTransform), typeof(Image));
            arrowGO.transform.SetParent(dialBg.transform, false);
            arrowRect = arrowGO.GetComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(40f, 40f); // Flecha grande y visible
            arrowImage = arrowGO.GetComponent<Image>();
            arrowImage.sprite = thickArrowSprite;
            arrowImage.color = new Color(0.22f, 0.95f, 0.60f, 1f); // Verde neón brillante

            // Título superior
            titleText = UiKit.MakeLabel(
                radarPanel,
                "📡 RASTREO FINAL ACTIVADO",
                14f,
                new Color(0.22f, 0.95f, 0.60f, 1f),
                TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f),
                new Vector2(80f, 14f),
                new Vector2(235f, 22f),
                FontStyles.Bold
            );

            // Texto de distancia y residuos
            distanceText = UiKit.MakeLabel(
                radarPanel,
                "-- m",
                20f,
                UiKit.Cream,
                TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f),
                new Vector2(80f, -13f),
                new Vector2(235f, 26f),
                FontStyles.Bold
            );
        }

        private void Update()
        {
            if (mainCamera == null || !mainCamera.gameObject.activeInHierarchy)
            {
                mainCamera = Camera.main;
            }

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerController>();
                if (player == null)
                {
                    canvasGroup.alpha = 0f;
                    return;
                }
            }

            // Muestreo periódico de residuos
            sampleTimer -= Time.unscaledDeltaTime;
            if (sampleTimer <= 0f)
            {
                sampleTimer = 0.12f;
                BuscarResiduoMasCercano();
            }

            // Calcular porcentaje de avance
            float health = environment != null ? environment.Health01 : 0f;
            float collectedRatio = 0f;
            if (initialTrashCount > 0)
            {
                int collected = initialTrashCount - remainingTrashCount;
                collectedRatio = Mathf.Clamp01((float)collected / initialTrashCount);
            }

            // Condición solicitada: Visible cuando se alcanza el 85% de recolección
            bool activarPorPorcentaje = collectedRatio >= ActivationThreshold || health >= ActivationThreshold || (hasInitializedCounts && remainingTrashCount <= 3 && remainingTrashCount > 0);

            if (!activarPorPorcentaje)
            {
                // Oculto antes del 85% para no estorbar
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, 3f * Time.unscaledDeltaTime);
                return;
            }

            ActualizarBrujula();
        }

        private void BuscarResiduoMasCercano()
        {
            Vector3 playerPos = player.transform.position;
            var items = FindObjectsByType<TrashItem>();

            closestTrash = null;
            closestDistance = float.MaxValue;
            remainingTrashCount = 0;

            foreach (var item in items)
            {
                if (item == null || !item.gameObject.activeInHierarchy) continue;

                remainingTrashCount++;
                float d = Vector3.Distance(playerPos, item.transform.position);
                if (d < closestDistance)
                {
                    closestDistance = d;
                    closestTrash = item;
                }
            }

            if (!hasInitializedCounts || remainingTrashCount > initialTrashCount)
            {
                initialTrashCount = Mathf.Max(initialTrashCount, remainingTrashCount);
                hasInitializedCounts = true;
            }
        }

        private void ActualizarBrujula()
        {
            if (mainCamera == null) return;

            // Si ya no queda ningún residuo en el suelo
            if (remainingTrashCount == 0 || closestTrash == null)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0.9f, 4f * Time.unscaledDeltaTime);
                titleText.text = "🏆 ¡TODO RECOGIDO!";
                titleText.color = UiKit.Gold;
                distanceText.text = "Deposita en los tachos";
                arrowImage.color = UiKit.Gold;
                arrowRect.localRotation = Quaternion.Euler(0f, 0f, 0f);
                return;
            }

            // Mostrar el panel suavemente
            canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, 4f * Time.unscaledDeltaTime);

            // Calcular dirección en 360° relativa a la vista de la cámara
            Vector3 playerPos = player.transform.position;
            Vector3 dir = closestTrash.transform.position - playerPos;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                dir.Normalize();

                Vector3 camForward = mainCamera.transform.forward;
                camForward.y = 0f;
                camForward.Normalize();

                Vector3 camRight = mainCamera.transform.right;
                camRight.y = 0f;
                camRight.Normalize();

                float fwdDot = Vector3.Dot(camForward, dir);
                float rightDot = Vector3.Dot(camRight, dir);

                float targetAngle = Mathf.Atan2(rightDot, fwdDot) * Mathf.Rad2Deg;
                currentAngle = Mathf.SmoothDampAngle(currentAngle, -targetAngle, ref angleVelocity, 0.06f);
                arrowRect.localRotation = Quaternion.Euler(0f, 0f, currentAngle);
            }

            // Distancia y efectos visuales
            int distMeters = Mathf.RoundToInt(closestDistance);

            // Pulso sutil de la flecha
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4.5f) * 0.08f;
            arrowRect.localScale = Vector3.one * pulse;

            if (closestDistance <= 3.2f)
            {
                titleText.text = "🎯 ¡RESIDUO AL FRENTE!";
                titleText.color = new Color(0.35f, 1f, 0.65f, 1f);
                distanceText.text = $"<color=#57FFA3>{distMeters}m</color> · Restan solo {remainingTrashCount}";
                arrowImage.color = new Color(0.35f, 1f, 0.65f, 1f);
            }
            else
            {
                titleText.text = "📡 RASTREO FINAL (85%+)";
                titleText.color = new Color(0.25f, 0.95f, 0.55f, 1f);
                distanceText.text = $"{distMeters} metros · Restan {remainingTrashCount}";
                arrowImage.color = new Color(0.20f, 0.88f, 0.50f, 1f);
            }
        }

        private static Sprite BuildThickArrowSprite()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    float nx = (x + 0.5f) / n * 2f - 1f; // -1 .. 1
                    float ny = (y + 0.5f) / n * 2f - 1f; // -1 .. 1

                    // Cabeza de flecha ancha y triangular
                    bool isArrowHead = ny >= -0.1f && ny <= 0.85f && Mathf.Abs(nx) <= (0.85f - ny) * 0.88f;

                    // Cuerpo / Tallo grueso de la flecha
                    bool isArrowShaft = ny >= -0.72f && ny <= 0.05f && Mathf.Abs(nx) <= 0.24f;

                    bool inside = isArrowHead || isArrowShaft;

                    // Borde / Bisel para contraste máximo
                    float alpha = inside ? 1f : 0f;
                    Color col = inside ? Color.white : Color.clear;

                    tex.SetPixel(x, y, col);
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }
    }
}
