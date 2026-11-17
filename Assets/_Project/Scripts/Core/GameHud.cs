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

            // Restauración, puntos y residuos restantes
            Image status = UiKit.MakePanel(r, "Status", new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(560f, 196f), UiKit.Panel);
            UiKit.MakeLabel(status.transform, "RESTAURACIÓN", 22f, UiKit.GreenLight, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26f, -16f), new Vector2(300f, 30f), FontStyles.Bold);
            percentText = UiKit.MakeLabel(status.transform, "0%", 44f, UiKit.Cream, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(-26f, -8f), new Vector2(200f, 56f), FontStyles.Bold);
            healthFill = UiKit.MakeBar(status.transform, "HealthBar", new Vector2(0f, 1f), new Vector2(26f, -72f), new Vector2(508f, 24f), new Color(0f, 0f, 0f, 0.45f), UiKit.Green, out healthFillImage);
            pointsText = UiKit.MakeLabel(status.transform, "Puntos eco   0", 28f, UiKit.Cream, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26f, -112f), new Vector2(508f, 36f));
            remainingText = UiKit.MakeLabel(status.transform, "", 24f, UiKit.Soft, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26f, -152f), new Vector2(508f, 32f));

            // Misiones (la altura se ajusta sola)
            Image missions = UiKit.MakePanel(r, "Missions", new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(620f, 100f), UiKit.Panel);
            var layout = missions.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 18, 20);
            layout.spacing = 8f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            missions.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            missionPanel = missions.rectTransform;

            // Mochila
            Image inventory = UiKit.MakePanel(r, "Inventory", new Vector2(0f, 0f), new Vector2(30f, 30f), new Vector2(640f, 196f), UiKit.Panel);
            inventoryPanel = inventory.rectTransform;
            UiKit.MakeLabel(inventoryPanel, "MOCHILA", 22f, UiKit.GreenLight, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26f, -16f), new Vector2(300f, 30f), FontStyles.Bold);
            countText = UiKit.MakeLabel(inventoryPanel, "", 22f, UiKit.Cream, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(-26f, -16f), new Vector2(200f, 30f));
            selectedText = UiKit.MakeLabel(inventoryPanel, "", 24f, UiKit.Cream, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), new Vector2(26f, -160f), new Vector2(590f, 30f));
            EnsureSlots(6);

            // Indicación de interacción
            Image pill = UiKit.MakePanel(r, "Prompt", new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(720f, 84f), UiKit.Panel);
            promptPill = pill.gameObject;
            Image key = UiKit.MakePanel(pill.transform, "Key", new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(56f, 56f), UiKit.Blue);
            UiKit.MakeLabel(key.transform, "E", 34f, Color.white, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56f, 56f), FontStyles.Bold);
            promptText = UiKit.MakeLabel(pill.transform, "", 34f, UiKit.Cream, TextAlignmentOptions.Left, new Vector2(0f, 0.5f), new Vector2(100f, 0f), new Vector2(600f, 60f));
            promptPill.SetActive(false);

            // Avisos
            Image toast = UiKit.MakePanel(r, "Toast", new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(1100f, 92f), UiKit.Panel);
            toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            toastText = UiKit.MakeLabel(toast.transform, "", 30f, UiKit.Cream, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1040f, 80f));

            // Banner de nivel
            RectTransform banner = UiKit.Stretch(r, "Banner");
            bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerTitle = UiKit.MakeLabel(banner, "", 96f, UiKit.Cream, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1500f, 130f), FontStyles.Bold);
            bannerSubtitle = UiKit.MakeLabel(banner, "", 40f, UiKit.GreenLight, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(1500f, 60f));

            // Mira
            UiKit.MakeCircle(r, "Crosshair", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(10f, 10f), new Color(0.91f, 0.85f, 0.71f, 0.8f));

            OnMissions(null);
        }

        private void EnsureSlots(int count)
        {
            while (slots.Count < count)
            {
                int i = slots.Count;
                RectTransform rt = UiKit.Rect(inventoryPanel, "Slot" + i, new Vector2(0f, 1f), new Vector2(26f + i * 96f, -64f), new Vector2(84f, 84f));
                var slot = new Slot();
                slot.border = UiKit.MakePanel(rt, "Border", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(94f, 94f), UiKit.Gold);
                slot.border.raycastTarget = false;
                slot.border.gameObject.SetActive(false);
                Image bg = UiKit.MakePanel(rt, "Bg", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f), new Color(0f, 0f, 0f, 0.5f));
                bg.raycastTarget = false;
                slot.chip = UiKit.MakePanel(rt, "Chip", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(58f, 58f), Color.white);
                slot.chip.raycastTarget = false;
                slot.chip.gameObject.SetActive(false);
                slot.letter = UiKit.MakeLabel(rt, "", 30f, UiKit.Ink, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(84f, 84f), FontStyles.Bold);
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
            selectedText.text = selected != null ? selected.displayName + "  ·  " + selected.wasteType.DisplayName() + "   (Q / R: cambiar)" : "Mochila vacía";
        }

        private void OnMissions(IReadOnlyList<MissionRuntime> active)
        {
            foreach (GameObject row in missionRows) Destroy(row);
            missionRows.Clear();

            AddMissionText("MISIONES", 22f, UiKit.GreenLight, FontStyles.Bold);
            if (active == null || active.Count == 0)
            {
                AddMissionText("Habla con Elena para recibir una misión.", 24f, UiKit.Cream, FontStyles.Normal);
                return;
            }

            foreach (MissionRuntime mission in active)
            {
                AddMissionText(mission.Data.title, 28f, UiKit.Cream, FontStyles.Bold);
                for (int i = 0; i < mission.Data.objectives.Count; i++)
                {
                    MissionObjective objective = mission.Data.objectives[i];
                    bool done = mission.ObjectiveDone(i);
                    int progress = Mathf.Min(mission.Progress[i], objective.requiredAmount);
                    AddObjectiveRow(objective.Describe(), progress, objective.requiredAmount, done);
                }
            }
        }

        private void AddMissionText(string text, float size, Color color, FontStyles style)
        {
            var go = new GameObject("Row", typeof(RectTransform));
            go.transform.SetParent(missionPanel, false);
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
            go.GetComponent<LayoutElement>().preferredHeight = 54f;
            missionRows.Add(go);

            Color color = done ? UiKit.GreenLight : UiKit.Cream;
            UiKit.MakeLabel(go.transform, text, 22f, color, TextAlignmentOptions.TopLeft, new Vector2(0f, 1f), Vector2.zero, new Vector2(500f, 28f));
            UiKit.MakeLabel(go.transform, progress + " / " + total, 22f, color, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), Vector2.zero, new Vector2(100f, 28f));
            RectTransform fill = UiKit.MakeBar(go.transform, "Bar", new Vector2(0f, 0f), Vector2.zero, new Vector2(572f, 10f), new Color(0f, 0f, 0f, 0.45f), done ? UiKit.GreenLight : UiKit.Blue, out Image _);
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
            pointsText.text = "Puntos eco   " + shownPoints;

            remainingTimer -= dt;
            if (remainingTimer <= 0f)
            {
                remainingTimer = 0.7f;
                int remaining = FindObjectsByType<TrashItem>(FindObjectsSortMode.None).Length;
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
