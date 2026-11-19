using System.Collections.Generic;
using EcoGenesis.Data;
using EcoGenesis.Interaction;
using EcoGenesis.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// HUD moderno construido por código al jugar (no deja lienzos en la escena):
    /// restauración con barra, puntos, residuos restantes, misiones con progreso, mochila con casillas,
    /// indicación de interacción, avisos y banner de nivel.
    /// </summary>
    public class GameHud : MonoBehaviour
    {
        private sealed class Slot
        {
            public Image border;
            public Image chip;
            public TextMeshProUGUI letter;
        }

        private CanvasGroup root;
        private RectTransform healthFill;
        private Image healthFillImage;
        private TextMeshProUGUI percentText, pointsText, remainingText, selectedText, countText, promptText, toastText, bannerTitle, bannerSubtitle;
        private RectTransform missionPanel;
        private readonly List<GameObject> missionRows = new List<GameObject>();
        private RectTransform inventoryPanel;
        private readonly List<Slot> slots = new List<Slot>();
        private GameObject promptPill;
        private CanvasGroup toastGroup, bannerGroup;

        private float health, shownHealth, toastTimer, bannerTimer, remainingTimer;
        private int points, shownPoints;

        private void Awake() => BuildUi();

        private void OnEnable()
        {
            GameEvents.PromptChanged += OnPrompt;
            GameEvents.PointsChanged += OnPoints;
            GameEvents.HealthChanged += OnHealth;
            GameEvents.InventoryChanged += OnInventory;
            GameEvents.MissionsUpdated += OnMissions;
            GameEvents.MessageRaised += OnMessage;
        }

        private void OnDisable()
        {
            GameEvents.PromptChanged -= OnPrompt;
            GameEvents.PointsChanged -= OnPoints;
            GameEvents.HealthChanged -= OnHealth;
            GameEvents.InventoryChanged -= OnInventory;
            GameEvents.MissionsUpdated -= OnMissions;
            GameEvents.MessageRaised -= OnMessage;
        }

        public void SetVisible(bool visible)
        {
            if (root != null) root.alpha = visible ? 1f : 0f;
        }

        public void ShowBanner(string title, string subtitle)
        {
            bannerTitle.text = title;
            bannerSubtitle.text = subtitle;
            bannerTimer = 4f;
        }

        // ------------------------------------------------------------------ CONSTRUCCIÓN

        private void BuildUi()
        {
            Canvas canvas = UiKit.MakeCanvas("GameHud", 5);
            canvas.transform.SetParent(transform, false);
            root = canvas.gameObject.AddComponent<CanvasGroup>();
            root.blocksRaycasts = false;
            root.interactable = false;
            Transform r = canvas.transform;

            // 1. Panel superior izquierdo: Restauración, puntos y residuos restantes
            Image status = UiKit.MakeBorderedPanel(r, "Status", new Vector2(0f, 1f), new Vector2(30f, -30f),
                new Vector2(480f, 186f), UiKit.CardGlass, UiKit.CardBorder, 2f);
            UiKit.MakeIcon(status.transform, UiKit.IconLeaf, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(20f, 20f), UiKit.GreenLight);
            UiKit.MakeLabel(status.transform, "RESTAURACIÓN", 20f, UiKit.GreenLight, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(52f, -16f), new Vector2(260f, 28f), FontStyles.Bold);
            percentText = UiKit.MakeLabel(status.transform, "0%", 38f, UiKit.Cream, TextAlignmentOptions.TopRight,
                new Vector2(1f, 1f), new Vector2(-24f, -10f), new Vector2(140f, 48f), FontStyles.Bold);

            healthFill = UiKit.MakeBar(status.transform, "HealthBar", new Vector2(0f, 1f), new Vector2(24f, -66f),
                new Vector2(432f, 20f), new Color(0f, 0f, 0f, 0.48f), UiKit.Green, out healthFillImage);

            UiKit.MakePanel(status.transform, "Divider", new Vector2(0f, 1f), new Vector2(24f, -98f),
                new Vector2(432f, 2f), new Color(1f, 1f, 1f, 0.08f)).raycastTarget = false;

            pointsText = UiKit.MakeLabel(status.transform, "Puntos eco:  0", 24f, UiKit.Cream, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(24f, -110f), new Vector2(432f, 32f), FontStyles.Bold);
            remainingText = UiKit.MakeLabel(status.transform, "", 21f, UiKit.Soft, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(24f, -146f), new Vector2(432f, 28f));

            // 2. Panel superior derecho: Misiones (altura automática sin solaparse)
            Image missions = UiKit.MakeBorderedPanel(r, "Missions", new Vector2(1f, 1f), new Vector2(-30f, -30f),
                new Vector2(500f, 110f), UiKit.CardGlass, UiKit.CardBorder, 2f);
            Transform innerMissions = missions.transform.Find("Inner");
            if (innerMissions != null) innerMissions.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

            var layout = missions.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 22);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            missions.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            missionPanel = missions.rectTransform;

            // 3. Panel inferior izquierdo: Mochila compacta (termina en X=560, lejos del botón [E])
            Image inventory = UiKit.MakeBorderedPanel(r, "Inventory", new Vector2(0f, 0f), new Vector2(30f, 30f),
                new Vector2(530f, 178f), UiKit.CardGlass, UiKit.CardBorder, 2f);
            inventoryPanel = inventory.rectTransform;
            UiKit.MakeIcon(inventoryPanel, UiKit.IconGrid, new Vector2(0f, 1f), new Vector2(24f, -18f), new Vector2(18f, 18f), UiKit.GreenLight);
            UiKit.MakeLabel(inventoryPanel, "MOCHILA", 20f, UiKit.GreenLight, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(50f, -16f), new Vector2(240f, 28f), FontStyles.Bold);
            countText = UiKit.MakeLabel(inventoryPanel, "0 / 6", 21f, UiKit.Cream, TextAlignmentOptions.TopRight,
                new Vector2(1f, 1f), new Vector2(-24f, -16f), new Vector2(140f, 28f), FontStyles.Bold);
            selectedText = UiKit.MakeLabel(inventoryPanel, "Mochila vacía", 21f, UiKit.Soft, TextAlignmentOptions.TopLeft,
                new Vector2(0f, 1f), new Vector2(24f, -140f), new Vector2(482f, 28f));
            EnsureSlots(6);

            // 4. Indicación de interacción [E] centrada sin tocar la mochila
            Image pill = UiKit.MakeBorderedPanel(r, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 108f),
                new Vector2(520f, 68f), UiKit.PanelSolid, UiKit.CardBorder, 2f);
            promptPill = pill.gameObject;
            Image key = UiKit.MakePanel(pill.transform, "Key", new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(44f, 44f), UiKit.Blue);
            UiKit.MakeLabel(key.transform, "E", 26f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f), FontStyles.Bold);
            promptText = UiKit.MakeLabel(pill.transform, "", 25f, UiKit.Cream, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(74f, 0f), new Vector2(424f, 48f), FontStyles.Bold);
            promptText.textWrappingMode = TextWrappingModes.NoWrap;
            promptPill.SetActive(false);

            // 5. Avisos y diálogos centrados en el espacio libre entre Restauración y Misiones
            Image toast = UiKit.MakeBorderedPanel(r, "Toast", new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                new Vector2(780f, 86f), UiKit.PanelSolid, UiKit.CardBorder, 2f);
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            Image toastBadge = UiKit.MakePanel(toast.transform, "Badge", new Vector2(0f, 0.5f), new Vector2(18f, 0f),
                new Vector2(44f, 44f), new Color(0.14f, 0.30f, 0.21f, 0.9f));
            UiKit.MakeIcon(toastBadge.transform, UiKit.IconInfo, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f), UiKit.GreenLight);
            toastText = UiKit.MakeLabel(toast.transform, "", 23f, UiKit.Cream, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(78f, 0f), new Vector2(676f, 68f));

            // 6. Banner de inicio de nivel
            RectTransform banner = UiKit.Stretch(r, "Banner");
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerTitle = UiKit.MakeLabel(banner, "", 92f, UiKit.Cream, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1500f, 130f), FontStyles.Bold);
            bannerSubtitle = UiKit.MakeLabel(banner, "", 38f, UiKit.GreenLight, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1500f, 60f));

            // 7. Mira central
            UiKit.MakeCircle(r, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, 9f), new Color(0.95f, 0.90f, 0.79f, 0.85f));

            OnMissions(null);
        }

        private void EnsureSlots(int count)
        {
            if (count > 6 && inventoryPanel != null)
                inventoryPanel.sizeDelta = new Vector2(48f + count * 82f - 10f, 178f);

            while (slots.Count < count)
            {
                int i = slots.Count;
                RectTransform rt = UiKit.Rect(inventoryPanel, "Slot" + i, new Vector2(0f, 1f), new Vector2(24f + i * 82f, -56f), new Vector2(72f, 72f));
                var slot = new Slot();
                slot.border = UiKit.MakePanel(rt, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(80f, 80f), UiKit.Gold);
                slot.border.raycastTarget = false;
                slot.border.gameObject.SetActive(false);
                Image bg = UiKit.MakePanel(rt, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f), new Color(0f, 0f, 0f, 0.5f));
                bg.raycastTarget = false;
                slot.chip = UiKit.MakePanel(rt, "Chip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(48f, 48f), Color.white);
                slot.chip.raycastTarget = false;
                slot.chip.gameObject.SetActive(false);
                slot.letter = UiKit.MakeLabel(rt, "", 26f, UiKit.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(72f, 72f), FontStyles.Bold);
                slots.Add(slot);
            }
        }

        // ------------------------------------------------------------------ EVENTOS

        private void OnHealth(float value) => health = value;
        private void OnPoints(int value) => points = value;

        private void OnPrompt(string prompt)
        {
            promptPill.SetActive(!string.IsNullOrEmpty(prompt));
            promptText.text = prompt;
        }

        private void OnMessage(string message)
        {
            toastText.text = message;
            toastTimer = 3.6f;
        }

        private void OnInventory(PlayerInventory inventory)
        {
            EnsureSlots(inventory.Capacity);
            for (int i = 0; i < slots.Count; i++)
            {
                bool has = i < inventory.Items.Count;
                Slot slot = slots[i];
                slot.chip.gameObject.SetActive(has);
                slot.border.gameObject.SetActive(has && i == inventory.SelectedIndex);
                if (has)
                {
                    TrashData item = inventory.Items[i];
                    slot.chip.color = item.tint;
                    string label = item.wasteType.DisplayName();
                    slot.letter.text = label.Length > 0 ? label.Substring(0, 1) : "";
                }
                else
                {
                    slot.letter.text = "";
                }
            }

            countText.text = inventory.Items.Count + " / " + inventory.Capacity;
            TrashData selected = inventory.Selected;
            selectedText.text = selected != null ? selected.displayName + " · " + selected.wasteType.DisplayName() + " (Q/R)" : "Mochila vacía";
        }

        private void OnMissions(IReadOnlyList<MissionRuntime> active)
        {
            foreach (GameObject row in missionRows) Destroy(row);
            missionRows.Clear();

            AddMissionHeader();

            if (active == null || active.Count == 0)
            {
                AddMissionText("Habla con Elena para recibir una misión.", 22f, UiKit.Cream, FontStyles.Normal, 32f);
                return;
            }

            foreach (MissionRuntime mission in active)
            {
                AddMissionText(mission.Data.title, 25f, UiKit.Cream, FontStyles.Bold, 34f);
                for (int i = 0; i < mission.Data.objectives.Count; i++)
                {
                    MissionObjective objective = mission.Data.objectives[i];
                    bool done = mission.ObjectiveDone(i);
                    int progress = Mathf.Min(mission.Progress[i], objective.requiredAmount);
                    AddObjectiveRow(objective.Describe(), progress, objective.requiredAmount, done);
                }
            }
        }

        private void AddMissionHeader()
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(missionPanel, false);
            go.GetComponent<LayoutElement>().preferredHeight = 26f;
            missionRows.Add(go);

            UiKit.MakeIcon(go.transform, UiKit.IconLeaf, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(18f, 18f), UiKit.GreenLight);
            UiKit.MakeLabel(go.transform, "MISIONES", 20f, UiKit.GreenLight, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(300f, 26f), FontStyles.Bold);
        }

        private void AddMissionText(string text, float size, Color color, FontStyles style, float minHeight)
        {
            var go = new GameObject("Row", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(missionPanel, false);
            go.GetComponent<LayoutElement>().minHeight = minHeight;
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.fontStyle = style;
            t.raycastTarget = false;
            missionRows.Add(go);
        }

        private void AddObjectiveRow(string text, int progress, int total, bool done)
        {
            var go = new GameObject("Objective", typeof(RectTransform), typeof(LayoutElement));
            go.transform.SetParent(missionPanel, false);
            go.GetComponent<LayoutElement>().preferredHeight = 52f;
            missionRows.Add(go);

            Color color = done ? UiKit.GreenLight : UiKit.Cream;
            UiKit.MakeLabel(go.transform, text, 20f, color, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(365f, 30f));
            UiKit.MakeLabel(go.transform, progress + " / " + total, 20f, color, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), Vector2.zero, new Vector2(80f, 30f), FontStyles.Bold);
            RectTransform fill = UiKit.MakeBar(go.transform, "Bar", new Vector2(0f, 0f), Vector2.zero, new Vector2(452f, 9f), new Color(0f, 0f, 0f, 0.48f), done ? UiKit.GreenLight : UiKit.Blue, out Image _);
            UiKit.SetBar(fill, total > 0 ? (float)progress / total : 1f);
        }

        // ------------------------------------------------------------------ ACTUALIZACIÓN

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            shownHealth = Mathf.MoveTowards(shownHealth, health, 0.6f * dt);
            UiKit.SetBar(healthFill, shownHealth);
            healthFillImage.color = Color.Lerp(UiKit.Hex("#C2A96B"), UiKit.Green, Mathf.SmoothStep(0f, 1f, shownHealth));
            percentText.text = Mathf.RoundToInt(shownHealth * 100f) + "%";

            if (shownPoints != points)
            {
                float speed = Mathf.Max(40f, Mathf.Abs(points - shownPoints) * 4f);
                shownPoints = (int)Mathf.MoveTowards(shownPoints, points, speed * dt);
            }
            pointsText.text = "Puntos eco:  " + shownPoints;

            remainingTimer -= dt;
            if (remainingTimer <= 0f)
            {
                remainingTimer = 0.7f;
                int remaining = FindObjectsByType<TrashItem>().Length;
                remainingText.text = remaining > 0 ? "Residuos en el parque:  " + remaining : "¡El parque está limpio!";
            }

            if (toastTimer > 0f)
            {
                toastTimer -= dt;
                toastGroup.alpha = Mathf.Clamp01(Mathf.Min(toastTimer / 0.4f, (3.6f - toastTimer) / 0.15f));
            }
            else
            {
                toastGroup.alpha = 0f;
            }

            if (bannerTimer > 0f)
            {
                bannerTimer -= dt;
                bannerGroup.alpha = Mathf.Clamp01(Mathf.Min(bannerTimer / 1f, (4f - bannerTimer) / 0.5f));
            }
            else
            {
                bannerGroup.alpha = 0f;
            }
        }
    }
}