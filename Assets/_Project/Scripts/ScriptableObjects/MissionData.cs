using System;
using System.Collections.Generic;
using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Data
{
    public enum ObjectiveType
    {
        CollectTrash,
        SortCorrectly
    }

    [Serializable]
    public class MissionObjective
    {
        public ObjectiveType type = ObjectiveType.CollectTrash;
        [Tooltip("Si está activo, cuenta cualquier tipo de residuo.")]
        public bool anyWasteType;
        public WasteType wasteType = WasteType.Plastic;
        [Min(1)] public int requiredAmount = 5;

        public bool Matches(TrashData trash)
        {
            return trash != null && (anyWasteType || trash.wasteType == wasteType);
        }

        public string Describe()
        {
            string what = anyWasteType ? "residuos" : wasteType.PluralName();
            string verb = type == ObjectiveType.CollectTrash ? "Recolecta" : "Deposita correctamente";
            return verb + " " + requiredAmount + " " + what;
        }
    }

    /// <summary>Datos de una misión. Crear con: Assets > Create > EcoGenesis > Mission Data.</summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "EcoGenesis/Mission Data")]
    public class MissionData : ScriptableObject
    {
        [Header("Identidad")]
        [Tooltip("Id único (se usará para guardar/cargar progreso).")]
        public string missionId = "mission_01";
        public string title = "Nueva misión";
        [TextArea] public string description = "";
        [TextArea] public string startDialogue = "";
        [TextArea] public string completeDialogue = "";

        [Header("Objetivos")]
        public List<MissionObjective> objectives = new List<MissionObjective>();
        [Tooltip("Si está activo, los objetivos se cumplen en orden.")]
        public bool sequential;

        [Header("Recompensas")]
        [Min(0)] public int rewardPoints = 50;
        [Tooltip("Puntos porcentuales extra de salud del parque al completar.")]
        [Min(0f)] public float restorationBonus = 2f;
        [Tooltip("Misión que se inicia automáticamente al completar esta.")]
        public MissionData nextMission;
    }
}
