using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>Contrato para todo lo que el jugador puede usar con la tecla de interacción.</summary>
    public interface IInteractable
    {
        /// <summary>Texto que se muestra en pantalla ("Recoger botella").</summary>
        string Prompt { get; }

        /// <summary>Se llama cuando el jugador interactúa. "interactor" es el GameObject del jugador.</summary>
        void Interact(GameObject interactor);
    }
}
