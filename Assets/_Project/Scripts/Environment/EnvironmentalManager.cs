using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Interaction;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Mantiene el porcentaje de salud del área (0-100). Sube al clasificar bien y al completar misiones.
    /// Con "balanceFromScene" la salud llega al 100% justo cuando se clasifican todos los residuos de la escena,
    /// repartida según el restorationWeight de cada TrashData.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class EnvironmentalManager : MonoBehaviour
    {
        [SerializeField, Range(0f, 100f)] private float startingHealth = 5f;
        [SerializeField] private bool balanceFromScene = true;
        [Tooltip("Ganancia por residuo (x restorationWeight) si no se balancea desde la escena.")]
        [SerializeField, Min(0f)] private float fallbackGainPerItem = 2f;

        private float health;
        private float totalWeight;

        public float Health => health;
        public float Health01 => health / 100f;

        private void Awake()
        {
            health = startingHealth;
            if (!balanceFromScene) return;

            TrashItem[] items = FindObjectsByType<TrashItem>(FindObjectsSortMode.None);
            foreach (TrashItem item in items)
            {
                if (item.Data != null) totalWeight += item.Data.restorationWeight;
            }
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

        private void Start() => GameEvents.RaiseHealthChanged(Health01);

        public void AddHealth(float amount)
        {
            SetHealth(health + amount);
        }

        public void SetHealth(float value)
        {
            health = Mathf.Clamp(value, 0f, 100f);
            GameEvents.RaiseHealthChanged(Health01);
        }

        private void OnTrashSorted(TrashData data, bool correct)
        {
            if (!correct || data == null) return;

            float gain = totalWeight > 0f
                ? (100f - startingHealth) * data.restorationWeight / totalWeight
                : fallbackGainPerItem * data.restorationWeight;
            AddHealth(gain);
        }

        private void OnMissionCompleted(MissionData mission)
        {
            if (mission != null && mission.restorationBonus > 0f) AddHealth(mission.restorationBonus);
        }
    }
}
