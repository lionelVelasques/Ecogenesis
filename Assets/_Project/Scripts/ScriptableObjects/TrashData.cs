using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Data
{
    /// <summary>
    /// Datos de un tipo de residuo. Crear con: Assets > Create > EcoGenesis > Trash Data.
    /// Es un contenedor de datos: la lógica vive en otros scripts.
    /// </summary>
    [CreateAssetMenu(fileName = "TrashData_", menuName = "EcoGenesis/Trash Data")]
    public class TrashData : ScriptableObject
    {
        [Header("Identidad")]
        public string displayName = "Residuo";
        public Sprite icon;
        public WasteType wasteType = WasteType.Plastic;
        [Tooltip("Color de referencia (UI, contenedor, modelo placeholder).")]
        public Color tint = Color.white;

        [Header("Puntuación")]
        [Min(0)] public int points = 10;
        [Min(0)] public int wrongPenalty = 5;
        [Tooltip("Peso relativo de este residuo al restaurar el parque (ver EnvironmentalManager).")]
        [Min(0f)] public float restorationWeight = 1f;

        [Header("Educación")]
        [TextArea] public string recyclingTip = "";
    }
}
