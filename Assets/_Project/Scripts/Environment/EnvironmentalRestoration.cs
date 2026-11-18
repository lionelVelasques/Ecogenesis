using System;
using System.Collections.Generic;
using EcoGenesis.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace EcoGenesis.Environment
{
    /// <summary>Una etapa de restauración: se activa al superar un porcentaje de salud.</summary>
    [Serializable]
    public class RestorationStage
    {
        public string name = "Etapa";
        [Range(0f, 1f)] public float threshold = 0.25f;
        public GameObject[] activateOnReach;
        public GameObject[] deactivateOnReach;
        public UnityEvent onReached;
        [NonSerialized] public bool reached;
    }

    /// <summary>
    /// Controlador visual: transforma el parque según la salud (0-1) con transición suave.
    /// Cambia capas de vegetación, luz del sol, ambiente, niebla, Volumes URP, color del agua y audio de naturaleza.
    /// Todo es opcional: los campos vacíos se ignoran.
    /// </summary>
    public class EnvironmentalRestoration : MonoBehaviour
    {
        [SerializeField] private EnvironmentalManager environment;
        [Tooltip("Velocidad de la transición visual (fracción de salud por segundo).")]
        [SerializeField, Min(0.01f)] private float transitionSpeed = 0.3f;

        [Header("Capas de vegetación / props")]
        [SerializeField] private List<RestorationStage> stages = new List<RestorationStage>();

        [Header("Iluminación URP")]
        [SerializeField] private Light sun;
        [SerializeField] private Gradient sunColor = new Gradient();
        [SerializeField] private Vector2 sunIntensityRange = new Vector2(1.2f, 1.55f);
        [SerializeField] private bool controlAmbient = true;
        [SerializeField] private Gradient ambientColor = new Gradient();
        [SerializeField] private bool controlFog = true;
        [SerializeField] private Gradient fogColor = new Gradient();
        [Tooltip("x = densidad contaminado, y = densidad limpio")]
        [SerializeField] private Vector2 fogDensityRange = new Vector2(0.007f, 0.0018f);

        [Header("Volumes URP (opcional)")]
        [SerializeField] private Volume pollutedVolume;
        [SerializeField] private Volume cleanVolume;

        [Header("Agua")]
        [SerializeField] private Renderer waterRenderer;
        [SerializeField] private Gradient waterColor = new Gradient();
        [SerializeField] private string waterColorProperty = "_BaseColor";
        [SerializeField] private string waterSmoothnessProperty = "_Smoothness";
        [SerializeField] private Vector2 waterSmoothnessRange = new Vector2(0.1f, 0.9f);

        [Header("Audio")]
        [SerializeField] private AudioSource natureAudio;

        private MaterialPropertyBlock propertyBlock;
        private float target;
        private float shown;
        private float lastApplied = -1f;

        private void Reset() => ConfigureDefaults();

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void Start()
        {
            target = environment != null ? environment.Health01 : 0f;
            shown = target;
            Apply(shown);
        }

        private void Update()
        {
            shown = Mathf.MoveTowards(shown, target, transitionSpeed * Time.deltaTime);
            if (!Mathf.Approximately(shown, lastApplied)) Apply(shown);
        }

        private void OnHealthChanged(float health01) => target = Mathf.Clamp01(health01);

        // ---------- API de configuración por código (usada por el editor de escena) ----------

        public void AddStage(string stageName, float threshold, GameObject[] activate, GameObject[] deactivate)
        {
            stages.Add(new RestorationStage
            {
                name = stageName,
                threshold = threshold,
                activateOnReach = activate,
                deactivateOnReach = deactivate
            });
        }

        public void SetReferences(EnvironmentalManager manager, Light sunLight, Renderer water)
        {
            environment = manager;
            sun = sunLight;
            waterRenderer = water;
        }

        /// <summary>Gradientes por defecto con la paleta del GDD (gris velado -> cálido y vivo).</summary>
        public void ConfigureDefaults()
        {
            sunColor = MakeGradient(Hex("#F0EEE2"), Hex("#FFF1D6"));
            ambientColor = MakeGradient(Hex("#B6BEC0"), Hex("#D2E6D4"));
            fogColor = MakeGradient(Hex("#C4C9C7"), Hex("#D6EAF2"));
            waterColor = MakeGradient(Hex("#6B5A3C"), Hex("#4A91B8"));
            sunIntensityRange = new Vector2(1.2f, 1.55f);
            fogDensityRange = new Vector2(0.007f, 0.0018f);
        }

        /// <summary>Cambia la iluminación del nivel (hora del día, niebla, ambiente).</summary>
        public void SetLook(Color sunDead, Color sunClean, Color ambientDead, Color ambientClean,
            Color fogDead, Color fogClean, Vector2 sunIntensity, Vector2 fogDensity)
        {
            sunColor = MakeGradient(sunDead, sunClean);
            ambientColor = MakeGradient(ambientDead, ambientClean);
            fogColor = MakeGradient(fogDead, fogClean);
            sunIntensityRange = sunIntensity;
            fogDensityRange = fogDensity;
        }

        // ---------- Aplicación visual ----------

        private void Apply(float t)
        {
            lastApplied = t;
            ApplyStages(t);
            ApplyLighting(t);
            ApplyVolumes(t);
            ApplyWater(t);
            if (natureAudio != null) natureAudio.volume = t;
        }

        private void ApplyStages(float t)
        {
            foreach (RestorationStage stage in stages)
            {
                bool should = t >= stage.threshold;
                if (should == stage.reached) continue;
                stage.reached = should;

                SetActive(stage.activateOnReach, should);
                SetActive(stage.deactivateOnReach, !should);
                if (should) stage.onReached?.Invoke();
            }
        }

        private void ApplyLighting(float t)
        {
            if (sun != null)
            {
                sun.color = sunColor.Evaluate(t);
                sun.intensity = Mathf.Lerp(sunIntensityRange.x, sunIntensityRange.y, t);
            }

            if (controlAmbient)
            {
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = ambientColor.Evaluate(t);
            }

            if (controlFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Exponential;
                RenderSettings.fogColor = fogColor.Evaluate(t);
                RenderSettings.fogDensity = Mathf.Lerp(fogDensityRange.x, fogDensityRange.y, t);
            }
        }

        private void ApplyVolumes(float t)
        {
            if (pollutedVolume != null) pollutedVolume.weight = 1f - t;
            if (cleanVolume != null) cleanVolume.weight = t;
        }

        private void ApplyWater(float t)
        {
            if (waterRenderer == null) return;
            if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();

            waterRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(waterColorProperty, waterColor.Evaluate(t));
            propertyBlock.SetFloat(waterSmoothnessProperty, Mathf.Lerp(waterSmoothnessRange.x, waterSmoothnessRange.y, t));
            waterRenderer.SetPropertyBlock(propertyBlock);
        }

        private static void SetActive(GameObject[] objects, bool active)
        {
            if (objects == null) return;
            foreach (GameObject go in objects)
            {
                if (go != null) go.SetActive(active);
            }
        }

        private static Gradient MakeGradient(Color from, Color to)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(from, 0f), new GradientColorKey(to, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }
    }
}
