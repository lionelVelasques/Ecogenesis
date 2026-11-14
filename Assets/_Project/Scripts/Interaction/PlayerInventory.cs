using System.Collections.Generic;
using EcoGenesis.Core;
using EcoGenesis.Data;
using EcoGenesis.Player;
using UnityEngine;

namespace EcoGenesis.Interaction
{
    /// <summary>
    /// Mochila del jugador. Guarda los residuos recogidos; el residuo "en mano" es el seleccionado
    /// (Q/R para cambiar). Notifica los cambios con GameEvents.InventoryChanged.
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [SerializeField, Min(1)] private int capacity = 6;
        [SerializeField] private PlayerInputReader input;

        private readonly List<TrashData> items = new List<TrashData>();
        private int selectedIndex;

        public IReadOnlyList<TrashData> Items => items;
        public int Capacity => capacity;
        public bool IsFull => items.Count >= capacity;
        public int SelectedIndex => selectedIndex;
        public TrashData Selected => items.Count > 0 ? items[selectedIndex] : null;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
        }

        private void Start() => Notify();

        private void Update()
        {
            if (input == null || items.Count < 2) return;
            if (input.NextItemPressed) Cycle(1);
            else if (input.PreviousItemPressed) Cycle(-1);
        }

        public bool TryAdd(TrashData data)
        {
            if (data == null || IsFull) return false;
            items.Add(data);
            Notify();
            return true;
        }

        /// <summary>Quita el residuo seleccionado (el que está "en mano").</summary>
        public bool RemoveSelected(out TrashData removed)
        {
            removed = Selected;
            if (removed == null) return false;

            items.RemoveAt(selectedIndex);
            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, items.Count - 1));
            Notify();
            return true;
        }

        public void Cycle(int delta)
        {
            if (items.Count == 0) return;
            selectedIndex = (selectedIndex + delta + items.Count) % items.Count;
            Notify();
        }

        private void Notify() => GameEvents.RaiseInventoryChanged(this);
    }
}
