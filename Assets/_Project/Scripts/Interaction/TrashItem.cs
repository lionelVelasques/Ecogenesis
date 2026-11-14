using EcoGenesis.Core;
using EcoGenesis.Data;
using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>Residuo recolectable. El tipo y los datos vienen del ScriptableObject TrashData.</summary>
    public class TrashItem : MonoBehaviour, IInteractable
    {
        [SerializeField] private TrashData data;

        public TrashData Data => data;
        public string Prompt => data != null ? "Recoger " + data.displayName : "Recoger";

        /// <summary>Permite asignar los datos al instanciar el objeto por código (spawners).</summary>
        public void Configure(TrashData trashData) => data = trashData;

        public void Interact(GameObject interactor)
        {
            if (data == null) return;
            if (!interactor.TryGetComponent(out PlayerInventory inventory)) return;

            if (!inventory.TryAdd(data))
            {
                GameEvents.RaiseMessage("Tu mochila está llena. Deposita residuos en un contenedor.");
                return;
            }

            GameEvents.RaiseTrashCollected(data);
            gameObject.SetActive(false);
        }
    }
}
