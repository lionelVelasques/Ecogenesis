using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Cielo vivo: del cielo cargado al cielo limpio según la salud del parque. Cada nivel puede definir
    /// sus propios colores (mañana con neblina, atardecer...). Usa Skybox/Procedural (se copia el material).
    /// </summary>
    public class SkyRestoration : MonoBehaviour
    {
        [SerializeField] private Material skySource;
        [SerializeField] private float transitionSpeed = 0.25f;
        [SerializeField] private Color pollutedTint = new Color(0.72f, 0.64f, 0.5f);
        [SerializeField] private Color cleanTint = new Color(0.5f, 0.5f, 0.5f);
        [SerializeField] private float pollutedThickness = 2.4f;
        [SerializeField] private float cleanThickness = 1f;
        [SerializeField] private float cleanExposure = 1.25f;

        private Material sky;
        private float target;
        private float shown = -1f;

        public void SetLook(Color pollutedSkyTint, Color cleanSkyTint, float pollutedAtmosphere, float cleanAtmosphere, float exposureClean)
        {
            pollutedTint = pollutedSkyTint;
            cleanTint = cleanSkyTint;
            pollutedThickness = pollutedAtmosphere;
            cleanThickness = cleanAtmosphere;
            cleanExposure = exposureClean;
        }

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
            sky.SetColor("_SkyTint", Color.Lerp(pollutedTint, cleanTint, shown));
            sky.SetColor("_GroundColor", Color.Lerp(new Color(0.45f, 0.45f, 0.43f), new Color(0.55f, 0.62f, 0.55f), shown));
            sky.SetFloat("_AtmosphereThickness", Mathf.Lerp(pollutedThickness, cleanThickness, shown));
            sky.SetFloat("_Exposure", Mathf.Lerp(1.0f, cleanExposure, shown));
        }
    }
}
