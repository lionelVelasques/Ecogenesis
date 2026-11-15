using TMPro;
using UnityEngine;

namespace EcoGenesis.Core
{
    /// <summary>Oculta un panel del HUD mientras su texto está vacío (indicaciones y avisos).</summary>
    public class HudPanelAutoHide : MonoBehaviour
    {
        public TMP_Text watched;
        public GameObject panel;

        private void Update()
        {
            if (watched == null || panel == null) return;
            bool show = !string.IsNullOrEmpty(watched.text);
            if (panel.activeSelf != show) panel.SetActive(show);
        }
    }
}
