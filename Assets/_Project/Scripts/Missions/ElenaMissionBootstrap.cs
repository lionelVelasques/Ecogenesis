using EcoGenesis.Core;
using EcoGenesis.Data;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace EcoGenesis.Missions
{
    /// <summary>
    /// Vincula la misión interactiva de Elena ("Guía Ambiental") de forma automática:
    /// - Al hablarle con [E], Elena le asigna al jugador la misión: "Limpieza de Plásticos".
    /// - Registra el objetivo en el panel de misiones del HUD (Recolectar 4 plásticos).
    /// - Al completarla, otorga +50 puntos de recompensa, diálogo de felicitación y bonus ecológico.
    /// - Auto-instanciable en cualquier escena con NPC_Elena (cero configuración manual).
    /// </summary>
    public class ElenaMissionBootstrap : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ConfigurarElena();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => ConfigurarElena();

        private static void ConfigurarElena()
        {
            var elenaGO = GameObject.Find("NPC_Elena");
            if (elenaGO == null) return;

            var manager = FindFirstObjectByType<MissionManager>();
            if (manager == null) return;

            var giver = elenaGO.GetComponent<MissionGiver>();
            if (giver == null) giver = elenaGO.AddComponent<MissionGiver>();

            // Crear la misión educativa de recojo de plástico
            var misionPlastico = ScriptableObject.CreateInstance<MissionData>();
            misionPlastico.missionId = "mision_elena_plastico_huancayo";
            misionPlastico.title = "Limpieza de Plásticos";
            misionPlastico.description = "Ayuda a Elena a recolectar 4 residuos plásticos en el parque";
            misionPlastico.startDialogue = "¡Hola! El parque está descuidado. Ayúdame a recolectar 4 plásticos para limpiar los jardines.";
            misionPlastico.completeDialogue = "¡Excelente trabajo! Gracias a ti el parque se ve mucho más limpio. ¡Toma 50 puntos extra!";
            misionPlastico.rewardPoints = 50;
            misionPlastico.restorationBonus = 3.5f;

            misionPlastico.objectives.Add(new MissionObjective
            {
                type = ObjectiveType.CollectTrash,
                anyWasteType = false,
                wasteType = WasteType.Plastic,
                requiredAmount = 4
            });

            // Asignar al MissionGiver mediante reflexión para no alterar los campos privados
            var offeredField = typeof(MissionGiver).GetField("offered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (offeredField != null)
            {
                offeredField.SetValue(giver, new[] { misionPlastico });
            }

            var missionsField = typeof(MissionGiver).GetField("missions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (missionsField != null)
            {
                missionsField.SetValue(giver, manager);
            }
        }
    }
}
