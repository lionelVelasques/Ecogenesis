using System.Collections.Generic;
using EcoGenesis.Core;
using EcoGenesis.Data;
using UnityEngine;

namespace EcoGenesis.Missions
{
    /// <summary>
    /// Gestor de misiones: lleva el progreso de las misiones activas escuchando eventos de recolección
    /// y clasificación, y publica las misiones completadas. Expone el estado para guardar y cargar.
    /// </summary>
    public class MissionManager : MonoBehaviour
    {
        [SerializeField] private MissionData startingMission;
        [Tooltip("Si es false, las misiones las entrega un MissionGiver (ej. Elena).")]
        [SerializeField] private bool startAutomatically;

        private readonly List<MissionRuntime> active = new List<MissionRuntime>();
        private readonly HashSet<string> completed = new HashSet<string>();

        public IReadOnlyList<MissionRuntime> Active => active;
        public IEnumerable<string> CompletedIds => completed;

        private void OnEnable()
        {
            GameEvents.TrashCollected += OnTrashCollected;
            GameEvents.TrashSorted += OnTrashSorted;
        }

        private void OnDisable()
        {
            GameEvents.TrashCollected -= OnTrashCollected;
            GameEvents.TrashSorted -= OnTrashSorted;
        }

        private void Start()
        {
            if (startAutomatically && startingMission != null) StartMission(startingMission);
            else NotifyUpdated();
        }

        public bool IsActive(MissionData mission) => active.Exists(m => m.Data == mission);
        public bool IsCompleted(MissionData mission) => mission != null && completed.Contains(mission.missionId);

        /// <summary>Inicia una misión. Devuelve false si ya está activa o completada.</summary>
        public bool StartMission(MissionData mission)
        {
            if (mission == null || IsActive(mission) || IsCompleted(mission)) return false;

            active.Add(new MissionRuntime(mission));
            GameEvents.RaiseMessage("Nueva misión: " + mission.title);
            NotifyUpdated();
            return true;
        }

        /// <summary>Reemplaza el estado actual (al cargar una partida guardada).</summary>
        public void RestoreState(IEnumerable<string> completedIds, IEnumerable<MissionRuntime> restored)
        {
            active.Clear();
            completed.Clear();
            if (completedIds != null)
            {
                foreach (string id in completedIds) completed.Add(id);
            }
            if (restored != null) active.AddRange(restored);
            NotifyUpdated();
        }

        private void OnTrashCollected(TrashData data) => Dispatch(ObjectiveType.CollectTrash, data);

        private void OnTrashSorted(TrashData data, bool correct)
        {
            if (correct) Dispatch(ObjectiveType.SortCorrectly, data);
        }

        private void Dispatch(ObjectiveType type, TrashData data)
        {
            bool anyChange = false;

            // Recorrido inverso: completar una misión la quita de la lista.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                MissionRuntime mission = active[i];
                if (!mission.Register(type, data)) continue;

                anyChange = true;
                if (mission.IsComplete) Complete(mission);
            }

            if (anyChange) NotifyUpdated();
        }

        private void Complete(MissionRuntime mission)
        {
            active.Remove(mission);
            completed.Add(mission.Data.missionId);

            string line = string.IsNullOrEmpty(mission.Data.completeDialogue) ? string.Empty : " " + mission.Data.completeDialogue;
            GameEvents.RaiseMessage("Misión completada: " + mission.Data.title + "." + line);
            GameEvents.RaiseMissionCompleted(mission.Data);

            if (mission.Data.nextMission != null) StartMission(mission.Data.nextMission);
        }

        private void NotifyUpdated() => GameEvents.RaiseMissionsUpdated(active);
    }
}
