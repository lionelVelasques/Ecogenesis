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

            var offeredField = typeof(MissionGiver).GetField("offered", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var missionsField = typeof(MissionGiver).GetField("missions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (missionsField != null && missionsField.GetValue(giver) == null)
            {
                missionsField.SetValue(giver, manager);
            }

            // Si el MissionGiver ya tiene configuradas misiones en la escena (ej. Mission_01_Plasticos con sus 2 objetivos), respetarlas
            if (offeredField != null)
            {
                var existing = offeredField.GetValue(giver) as MissionData[];
                if (existing != null && existing.Length > 0 && existing[0] != null)
                {
                    return; // Mantiene la misión original completa con sus 2 tareas
                }
            }

            // Misión de respaldo con los DOS objetivos completos: 1) Recolectar y 2) Clasificar
            var misionPlastico = ScriptableObject.CreateInstance<MissionData>();
            misionPlastico.missionId = "m01_bootstrap";
            misionPlastico.title = "Primer día en el parque";
            misionPlastico.description = "Ayuda a Elena a recolectar y clasificar los residuos plásticos";
            misionPlastico.startDialogue = "Hola, soy Elena. Empecemos por los plásticos: recoge tres botellas y deposítalas en el contenedor blanco.";
            misionPlastico.completeDialogue = "¡Excelente trabajo! Así se empieza.";
            misionPlastico.rewardPoints = 50;
            misionPlastico.restorationBonus = 2f;

            // Objetivo 1: Recolectar 3 plásticos
            misionPlastico.objectives.Add(new MissionObjective
            {
                type = ObjectiveType.CollectTrash,
                anyWasteType = false,
                wasteType = WasteType.Plastic,
                requiredAmount = 3
            });

            // Objetivo 2: Depositar 3 plásticos en el contenedor correspondiente
            misionPlastico.objectives.Add(new MissionObjective
            {
                type = ObjectiveType.SortCorrectly,
                anyWasteType = false,
                wasteType = WasteType.Plastic,
                requiredAmount = 3
            });

            if (offeredField != null)
            {
                offeredField.SetValue(giver, new[] { misionPlastico });
            }
        }
    }
}
