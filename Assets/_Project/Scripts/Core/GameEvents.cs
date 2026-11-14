using System;
using System.Collections.Generic;
using EcoGenesis.Data;
using EcoGenesis.Interaction;
using EcoGenesis.Missions;

namespace EcoGenesis.Core
{
    /// <summary>
    /// Bus de eventos global. Desacopla los sistemas: quien produce un evento no conoce a quien lo consume.
    /// Regla: suscribirse en OnEnable y desuscribirse en OnDisable.
    /// </summary>
    public static class GameEvents
    {
        public static event Action<TrashData> TrashCollected;
        /// <summary>(residuo, ¿contenedor correcto?)</summary>
        public static event Action<TrashData, bool> TrashSorted;
        public static event Action<int> PointsChanged;
        /// <summary>Salud del área normalizada 0..1.</summary>
        public static event Action<float> HealthChanged;
        public static event Action<PlayerInventory> InventoryChanged;
        public static event Action<IReadOnlyList<MissionRuntime>> MissionsUpdated;
        public static event Action<MissionData> MissionCompleted;
        public static event Action<string> PromptChanged;
        public static event Action<string> MessageRaised;

        public static void RaiseTrashCollected(TrashData data) => TrashCollected?.Invoke(data);
        public static void RaiseTrashSorted(TrashData data, bool correct) => TrashSorted?.Invoke(data, correct);
        public static void RaisePointsChanged(int total) => PointsChanged?.Invoke(total);
        public static void RaiseHealthChanged(float health01) => HealthChanged?.Invoke(health01);
        public static void RaiseInventoryChanged(PlayerInventory inventory) => InventoryChanged?.Invoke(inventory);
        public static void RaiseMissionsUpdated(IReadOnlyList<MissionRuntime> active) => MissionsUpdated?.Invoke(active);
        public static void RaiseMissionCompleted(MissionData mission) => MissionCompleted?.Invoke(mission);
        public static void RaisePromptChanged(string prompt) => PromptChanged?.Invoke(prompt);
        public static void RaiseMessage(string message) => MessageRaised?.Invoke(message);
    }
}
