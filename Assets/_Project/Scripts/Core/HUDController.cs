using System.Collections.Generic;
using System.Text;
using EcoGenesis.Interaction;
using EcoGenesis.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>
    /// HUD básico (uGUI + TextMeshPro). Solo escucha eventos: no conoce a ningún otro sistema.
    /// Se puede reemplazar por UI Toolkit sin tocar el resto del código.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text pointsText;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private Slider healthBar;
        [SerializeField] private TMP_Text inventoryText;
        [SerializeField] private TMP_Text missionsText;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private float messageDuration = 3.5f;

        private float messageTimer;

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

        private void Update()
        {
            if (messageTimer <= 0f) return;
            messageTimer -= Time.deltaTime;
            if (messageTimer <= 0f) SetText(messageText, string.Empty);
        }

        private void OnPrompt(string prompt) =>
            SetText(promptText, string.IsNullOrEmpty(prompt) ? string.Empty : "[E] " + prompt);

        private void OnPoints(int points) => SetText(pointsText, "Puntos eco: " + points);

        private void OnHealth(float health01)
        {
            SetText(healthText, "Salud del parque: " + Mathf.RoundToInt(health01 * 100f) + "%");
            if (healthBar != null) healthBar.value = health01;
        }

        private void OnInventory(PlayerInventory inventory)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Mochila " + inventory.Items.Count + "/" + inventory.Capacity + "  (Q/R: cambiar)");
            for (int i = 0; i < inventory.Items.Count; i++)
            {
                string marker = i == inventory.SelectedIndex ? "> " : "   ";
                var item = inventory.Items[i];
                sb.AppendLine(marker + item.displayName + " [" + item.wasteType.DisplayName() + "]");
            }
            SetText(inventoryText, sb.ToString());
        }

        private void OnMissions(IReadOnlyList<MissionRuntime> active)
        {
            if (active == null || active.Count == 0)
            {
                SetText(missionsText, "Sin misiones activas.\nHabla con Elena.");
                return;
            }

            var sb = new StringBuilder();
            foreach (var mission in active)
            {
                sb.AppendLine(mission.Data.title);
                for (int i = 0; i < mission.Data.objectives.Count; i++)
                {
                    var objective = mission.Data.objectives[i];
                    string check = mission.ObjectiveDone(i) ? "[x] " : "[ ] ";
                    sb.AppendLine(check + objective.Describe() + " (" + mission.Progress[i] + "/" + objective.requiredAmount + ")");
                }
                sb.AppendLine();
            }
            SetText(missionsText, sb.ToString());
        }

        private void OnMessage(string message)
        {
            SetText(messageText, message);
            messageTimer = messageDuration;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
