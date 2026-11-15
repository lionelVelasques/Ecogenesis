using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Cielo vivo: de un cielo cargado y amarillento a un cielo azul y limpio según la salud del parque.
    /// Usa el material Skybox/Procedural (se copia para no modificar el asset original).
    /// </summary>
    public class SkyRestoration : MonoBehaviour
    {
        [SerializeField] private Material skySource;
        [SerializeField] private float transitionSpeed = 0.25f;

        private Material sky;
        private float target;
        private float shown = -1f;

        private void Awake()
        {
            if (skySource == null) return;
            sky = new Material(skySource);
            RenderSettings.skybox = sky;
        }

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void OnHealthChanged(float health01) => target = Mathf.Clamp01(health01);

        private void Update()
        {
            if (sky == null) return;
            float next = shown < 0f ? target : Mathf.MoveTowards(shown, target, transitionSpeed * Time.deltaTime);
            if (Mathf.Approximately(next, shown)) return;

            shown = next;
            sky.SetColor("_SkyTint", Color.Lerp(new Color(0.72f, 0.64f, 0.5f), new Color(0.5f, 0.5f, 0.5f), shown));
            sky.SetColor("_GroundColor", Color.Lerp(new Color(0.45f, 0.45f, 0.43f), new Color(0.55f, 0.62f, 0.55f), shown));
            sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(2.4f, 1f, shown));
            sky.SetFloat("_Exposure", Mathf.Lerp(0.85f, 1.25f, shown));
        }
    }
}
