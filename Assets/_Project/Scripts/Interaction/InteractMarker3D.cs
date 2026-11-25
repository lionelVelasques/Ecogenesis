using EcoGenesis.Core;
using EcoGenesis.Missions;
using EcoGenesis.Player;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace EcoGenesis.Interaction
{
    /// <summary>
    /// Indicador visual flotante 3D sobre la basura y objetos interactuables:
    /// Muestra un marcador tipo "Billboard" (siempre mirando a la cámara) que flota
    /// sobre el residuo más cercano. Cuando el jugador entra en rango, el marcador
    /// se ilumina y muestra la tecla [E] con la acción a realizar.
    /// Auto-instanciable al cargar cualquier escena (cero configuración manual).
    /// </summary>
    public class InteractMarker3D : MonoBehaviour
    {
        private const float WorldScale = 0.0075f; // Escala para Canvas WorldSpace
        private const float MaxDetectDistance = 5.5f;
        private const float InteractDistance = 2.4f;

        private Canvas canvas;
        private RectTransform canvasRect;
        private RectTransform markerBadge;
        private CanvasGroup canvasGroup;
        private Image badgeBg;
        private TextMeshProUGUI keyText;
        private TextMeshProUGUI actionText;
        private Camera mainCam;

        private Transform currentTarget;
        private Vector3 currentPos;
        private float currentAlpha;
        private float currentScale = 1f;

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
            if (FindFirstObjectByType<InteractMarker3D>() != null) return;
            var go = new GameObject("InteractMarker3D");
            go.AddComponent<InteractMarker3D>();
        }

        private void Awake()
        {
            BuildMarker();
        }

        private void BuildMarker()
        {
            var canvasGO = new GameObject("WorldMarkerCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);

            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 15;

            canvasRect = canvasGO.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(280f, 60f);
            canvasRect.localScale = Vector3.one * WorldScale;

            // Panel contenedor del marcador
            badgeBg = UiKit.MakeBorderedPanel(
                canvasRect,
                "MarkerBadge",
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(240f, 54f),
                new Color(0.04f, 0.10f, 0.07f, 0.92f),
                UiKit.CardBorder,
                2f
            );
            markerBadge = badgeBg.rectTransform;

            canvasGroup = badgeBg.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            // Cuadrito de la tecla [E] con estilo visual
            Image keyBox = UiKit.MakePanel(
                markerBadge,
                "KeyBox",
                new Vector2(0f, 0.5f),
                new Vector2(8f, 0f),
                new Vector2(38f, 38f),
                UiKit.Blue
            );
            keyText = UiKit.MakeLabel(
                keyBox.transform,
                "E",
                22f,
                Color.white,
                TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(38f, 38f),
                FontStyles.Bold
            );

            // Texto de la acción ("Recoger", "Depositar", "Hablar")
            actionText = UiKit.MakeLabel(
                markerBadge,
                "Recoger",
                19f,
                UiKit.Cream,
                TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f),
                new Vector2(56f, 0f),
                new Vector2(174f, 36f),
                FontStyles.Bold
            );
            actionText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        private void LateUpdate()
        {
            if (mainCam == null || !mainCam.gameObject.activeInHierarchy)
            {
                mainCam = Camera.main;
                if (mainCam == null) return;
            }

            var player = FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                SetVisible(false);
                return;
            }

            // 1. Determinar el mejor objetivo: el detectado por el InteractionDetector o el residuo más cercano
            Transform bestTarget = null;
            string prompt = "Recoger";
            float bestDistance = float.MaxValue;

            var detector = player.GetComponent<InteractionDetector>();
            if (detector != null && detector.Current != null)
            {
                var comp = detector.Current as Component;
                if (comp != null && comp.gameObject.activeInHierarchy)
                {
                    bestTarget = comp.transform;
                    prompt = CleanPrompt(detector.Current.Prompt);
                    bestDistance = Vector3.Distance(player.transform.position, bestTarget.position);
                }
            }

            // Si no hay un objeto en foco directo, buscar el residuo más cercano en el cono visual
            if (bestTarget == null)
            {
                var trashItems = FindObjectsByType<TrashItem>();
                foreach (var trash in trashItems)
                {
                    if (trash == null || !trash.gameObject.activeInHierarchy) continue;

                    float dist = Vector3.Distance(player.transform.position, trash.transform.position);
                    if (dist < MaxDetectDistance && dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestTarget = trash.transform;
                        prompt = CleanPrompt(trash.Prompt);
                    }
                }
            }

            // 2. Controlar visibilidad y transiciones
            if (bestTarget == null || bestDistance > MaxDetectDistance)
            {
                currentTarget = null;
                SetVisible(false);
                return;
            }

            currentTarget = bestTarget;
            actionText.text = prompt;

            // Altura relativa según si es residuo (pequeño en suelo), contenedor o NPC (por encima de la cabeza)
            float yOffset = 0.55f;
            if (currentTarget.GetComponent<BinController>() != null) yOffset = 1.35f;
            else if (currentTarget.GetComponent<NpcTalker>() != null || currentTarget.GetComponent<MissionGiver>() != null || currentTarget.name.StartsWith("NPC_")) yOffset = 2.35f;

            // Flotación suave animada
            float hoverBob = Mathf.Sin(Time.time * 3.2f) * 0.04f;
            Vector3 targetWorldPos = currentTarget.position + Vector3.up * (yOffset + hoverBob);

            // Suavizado de posición para que no salte bruscamente al cambiar de objetivo
            if (canvasGroup.alpha < 0.05f) currentPos = targetWorldPos;
            else currentPos = Vector3.Lerp(currentPos, targetWorldPos, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));

            canvasRect.position = currentPos;

            // Billboard perfecto: siempre mirando hacia la cámara
            canvasRect.forward = mainCam.transform.forward;

            // 3. Estilo visual dinámico según cercanía
            bool inInteractRange = bestDistance <= InteractDistance;

            float targetAlpha = inInteractRange ? 1f : Mathf.InverseLerp(MaxDetectDistance, InteractDistance + 0.6f, bestDistance) * 0.75f;
            currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, 6f * Time.unscaledDeltaTime);
            canvasGroup.alpha = currentAlpha;

            float targetScale = inInteractRange ? 1.08f : 0.82f;
            currentScale = Mathf.Lerp(currentScale, targetScale, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
            canvasRect.localScale = Vector3.one * (WorldScale * currentScale);

            // Color del borde: resalta cuando estás en rango exacto para presionar [E]
            Color targetBorder = inInteractRange ? UiKit.GreenLight : new Color(0.35f, 0.6f, 0.45f, 0.4f);
            badgeBg.color = inInteractRange ? new Color(0.05f, 0.14f, 0.09f, 0.95f) : new Color(0.04f, 0.10f, 0.07f, 0.75f);
        }

        private static string CleanPrompt(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "Interactuar";
            // Acorta textos largos para que quepan estéticamente en el badge 3D
            if (raw.StartsWith("Recoger ")) return raw.Substring(8);
            if (raw.StartsWith("Depositar en ")) return raw.Substring(13);
            return raw;
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null) return;
            float target = visible ? 1f : 0f;
            currentAlpha = Mathf.MoveTowards(currentAlpha, target, 8f * Time.unscaledDeltaTime);
            canvasGroup.alpha = currentAlpha;
        }
    }
}
