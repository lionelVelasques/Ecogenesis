using EcoGenesis.Core;
using EcoGenesis.Data;
using UnityEngine;
using UnityEngine.Events;

namespace EcoGenesis.Interaction
{
    /// <summary>
    /// Contenedor de reciclaje. Valida si el residuo en mano coincide con el tipo que acepta
    /// y publica el resultado con GameEvents.TrashSorted (puntos y salud los calculan otros sistemas).
    /// </summary>
    public class BinController : MonoBehaviour, IInteractable
    {
        [SerializeField] private WasteType acceptedType = WasteType.Plastic;
        [SerializeField] private string binName = "Contenedor";
        [SerializeField] private Color binColor = Color.white;
        [SerializeField] private Renderer[] tintRenderers;
        [Tooltip("Si es true, un residuo mal depositado se pierde. Si es false, queda en la mochila.")]
        [SerializeField] private bool consumeOnWrong;
        [SerializeField] private UnityEvent onCorrect;
        [SerializeField] private UnityEvent onWrong;

        public WasteType AcceptedType => acceptedType;
        public string Prompt => "Depositar en " + binName;

        private void Awake()
        {
            if (tintRenderers == null || tintRenderers.Length == 0)
                tintRenderers = GetComponentsInChildren<Renderer>();
            ApplyTint();
        }

        public void Configure(WasteType type, string displayName, Color color)
        {
            acceptedType = type;
            binName = displayName;
            binColor = color;
            if (Application.isPlaying) ApplyTint();
        }

        public void Interact(GameObject interactor)
        {
            if (!interactor.TryGetComponent(out PlayerInventory inventory)) return;

            TrashData held = inventory.Selected;
            if (held == null)
            {
                GameEvents.RaiseMessage("No llevas ningún residuo. Recoge uno primero.");
                return;
            }

            bool correct = held.wasteType == acceptedType;
            if (correct || consumeOnWrong) inventory.RemoveSelected(out _);

            (correct ? onCorrect : onWrong)?.Invoke();
            GameEvents.RaiseTrashSorted(held, correct);
        }

        private void ApplyTint()
        {
            if (tintRenderers == null) return;
            foreach (Renderer r in tintRenderers)
            {
                if (r != null) r.material.color = binColor;
            }
        }
    }
}
