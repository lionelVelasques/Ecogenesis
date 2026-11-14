using System;
using System.Collections.Generic;
using EcoGenesis.Core;
using UnityEngine;

namespace EcoGenesis.Environment
{
    /// <summary>
    /// Objeto del parque que se transforma con la salud del área: cambia de color (gris/seco -> vivo),
    /// de tamaño (árboles que crecen, flores que brotan, basura que desaparece) y de luz (farolas que se encienden).
    /// Cada objeto tiene su propio rango [startAt, fullAt] para que el parque reverdezca poco a poco.
    /// </summary>
    [DisallowMultipleComponent]
    public class RestorationProp : MonoBehaviour
    {
        [Serializable]
        public class ColorPart
        {
            public Renderer renderer;
            public Color polluted = Color.gray;
            public Color clean = Color.green;
        }

        [Serializable]
        public class LightPart
        {
            public Light light;
            public float polluted;
            public float clean = 1.5f;
        }

        [SerializeField, Range(0f, 1f)] private float startAt;
        [SerializeField, Range(0f, 1f)] private float fullAt = 1f;
        [Tooltip("Multiplicador de tamaño con el parque contaminado (0 = invisible).")]
        [SerializeField] private float pollutedScale = 1f;
        [Tooltip("Multiplicador de tamaño con el parque restaurado (0 = desaparece).")]
        [SerializeField] private float cleanScale = 1f;
        [SerializeField, Min(0.05f)] private float smoothSpeed = 1.2f;
        [SerializeField] private List<ColorPart> parts = new List<ColorPart>();
        [SerializeField] private List<LightPart> lights = new List<LightPart>();

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MaterialPropertyBlock block;
        private Renderer[] allRenderers;
        private Vector3 baseScale;
        private float target;
        private float shown;
        private bool dirty = true;
        private bool renderersVisible = true;

        // ---------- API de configuración por código (la usa la herramienta del editor) ----------

        public void Configure(float start, float full, float pollutedSize, float cleanSize)
        {
            startAt = start;
            fullAt = full;
            pollutedScale = pollutedSize;
            cleanScale = cleanSize;
        }

        public void AddPart(Renderer renderer, Color polluted, Color clean)
        {
            parts.Add(new ColorPart { renderer = renderer, polluted = polluted, clean = clean });
        }

        public void AddLight(Light light, float pollutedIntensity, float cleanIntensity)
        {
            lights.Add(new LightPart { light = light, polluted = pollutedIntensity, clean = cleanIntensity });
        }

        // ---------- Ciclo de vida ----------

        private void Awake()
        {
            baseScale = transform.localScale;
            block = new MaterialPropertyBlock();
            allRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void OnEnable() => GameEvents.HealthChanged += OnHealthChanged;
        private void OnDisable() => GameEvents.HealthChanged -= OnHealthChanged;

        private void Update()
        {
            float next = Mathf.MoveTowards(shown, target, smoothSpeed * Time.deltaTime);
            if (!dirty && Mathf.Approximately(next, shown)) return;

            shown = next;
            dirty = false;
            Apply(shown);
        }

        private void OnHealthChanged(float health01)
        {
            target = Mathf.Clamp01(Mathf.InverseLerp(startAt, fullAt, health01));
        }

        private void Apply(float t)
        {
            float eased = t * t * (3f - 2f * t);

            float scale = Mathf.Lerp(pollutedScale, cleanScale, eased);
            transform.localScale = baseScale * scale;
            SetRenderersVisible(scale >= 0.02f);

            foreach (ColorPart part in parts)
            {
                if (part.renderer == null) continue;
                part.renderer.GetPropertyBlock(block);
                block.SetColor(BaseColorId, Color.Lerp(part.polluted, part.clean, eased));
                part.renderer.SetPropertyBlock(block);
            }

            foreach (LightPart lp in lights)
            {
                if (lp.light == null) continue;
                lp.light.intensity = Mathf.Lerp(lp.polluted, lp.clean, eased);
                lp.light.enabled = lp.light.intensity > 0.01f;
            }
        }

        private void SetRenderersVisible(bool visible)
        {
            if (visible == renderersVisible) return;
            renderersVisible = visible;
            foreach (Renderer r in allRenderers)
            {
                if (r != null) r.enabled = visible;
            }
        }
    }
}
