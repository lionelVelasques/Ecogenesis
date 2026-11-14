using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>NPC sencillo que dice una línea de diálogo distinta cada vez (ej. Mateo, el jardinero).</summary>
    public class NpcTalker : MonoBehaviour, IInteractable
    {
        [SerializeField] private string npcName = "NPC";
        [SerializeField, TextArea] private string[] lines;

        private int index;

        public string Prompt => "Hablar con " + npcName;

        public void Configure(string displayName, string[] dialogue)
        {
            npcName = displayName;
            lines = dialogue;
        }

        public void Interact(GameObject interactor)
        {
            Vector3 toPlayer = interactor.transform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(toPlayer);

            if (lines == null || lines.Length == 0) return;
            GameEvents.RaiseMessage(npcName + ": " + lines[index]);
            index = (index + 1) % lines.Length;
        }
    }
}
