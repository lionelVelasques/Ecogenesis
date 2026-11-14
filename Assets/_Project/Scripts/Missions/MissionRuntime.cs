using EcoGenesis.Data;

namespace EcoGenesis.Missions
{
    /// <summary>Estado en juego de una misión (el MissionData es solo la plantilla inmutable).</summary>
    public class MissionRuntime
    {
        public MissionData Data { get; }
        public int[] Progress { get; }

        public MissionRuntime(MissionData data)
        {
            Data = data;
            Progress = new int[data.objectives.Count];
        }

        public bool ObjectiveDone(int index) => Progress[index] >= Data.objectives[index].requiredAmount;

        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < Progress.Length; i++)
                {
                    if (!ObjectiveDone(i)) return false;
                }
                return true;
            }
        }

        /// <summary>Suma progreso a los objetivos que coincidan. Devuelve true si algo cambió.</summary>
        public bool Register(ObjectiveType type, TrashData trash)
        {
            bool changed = false;
            for (int i = 0; i < Progress.Length; i++)
            {
                if (ObjectiveDone(i)) continue;

                MissionObjective objective = Data.objectives[i];
                if (objective.type == type && objective.Matches(trash))
                {
                    Progress[i]++;
                    changed = true;
                }

                // En modo secuencial solo cuenta el primer objetivo pendiente.
                if (Data.sequential) break;
            }
            return changed;
        }
    }
}
