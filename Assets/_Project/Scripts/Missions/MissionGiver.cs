using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Interaction;
using UnityEngine;

namespace EcoGenesis.Missions
{
    /// <summary>NPC que entrega misiones (GDD: Elena, guía ambiental). Ofrece la primera misión disponible.</summary>
    public class MissionGiver : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName = "Elena";
        [SerializeField] private MissionManager missions;
        [SerializeField] private MissionData[] offered;
        [SerializeField, TextArea] private string idleLine = "Sigue así, el parque ya se siente distinto.";

        public string Prompt => "Hablar con " + npcName;

        public void Interact(GameObject interactor)
        {
            if (missions == null) return;

            foreach (MissionData mission in offered)
            {
                if (!missions.StartMission(mission)) continue;

                if (!string.IsNullOrEmpty(mission.startDialogue))
                    GameEvents.RaiseMessage(npcName + ": " + mission.startDialogue);
                return;
            }

            GameEvents.RaiseMessage(npcName + ": " + idleLine);
        }
    }
}
