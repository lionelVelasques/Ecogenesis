using System;
using EcoGenesis.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>Efecto al pasar el mouse por un botón: crece con suavidad.</summary>
    public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scale = 1.03f;
        private Vector3 target = Vector3.one;
        private bool active;

        public void OnPointerEnter(PointerEventData eventData) { active = true; target = Vector3.one * scale; }
        public void OnPointerExit(PointerEventData eventData) { active = true; target = Vector3.one; }
        private void OnDisable() { active = false; target = Vector3.one; transform.localScale = Vector3.one; }

        private void Update()
        {
            if (!active) return;
            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }
    }

    /// <summary>Animación de entrada suave (desvanecimiento y escala) con retardo en cascada.</summary>
    public class UiEntrance : MonoBehaviour
    {
        public float delay;
        public float duration = 0.35f;
        private CanvasGroup group;
        private float timer;
        private bool finished;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
        }

        private void OnEnable()
        {
            timer = -delay;
            finished = false;
            if (group != null) group.alpha = 0f;
            transform.localScale = Vector3.one * 0.94f;
        }

        private void Update()
        {
            if (finished) return;
            timer += Time.unscaledDeltaTime;
            if (timer < 0f) return;

            float t = Mathf.Clamp01(timer / duration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); // EaseOutCubic
            group.alpha = ease;
            transform.localScale = Vector3.LerpUnclamped(Vector3.one * 0.94f, Vector3.one, ease);

            if (t >= 1f)
            {
                group.alpha = 1f;
                transform.localScale = Vector3.one;
                finished = true;
            }
        }
    }

    /// <summary>Respiración suave de opacidad para el halo de brillo exterior.</summary>
    public class UiPulse : MonoBehaviour
    {
        public float minAlpha = 0.24f;
        public float maxAlpha = 0.48f;
        public float speed = 1.8f;
        private Graphic graphic;

        private void Awake() => graphic = GetComponent<Graphic>();

        private void Update()
        {
            if (graphic == null) return;
            Color c = graphic.color;
            float wave = (Mathf.Sin(Time.unscaledTime * speed) + 1f) * 0.5f;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, wave);
            graphic.color = c;
        }
    }

    /// <summary>Destello diagonal periódico que cruza el botón principal.</summary>
    public class UiShimmer : MonoBehaviour
    {
        public float interval = 3.4f;
        public float travelWidth = 620f;
        private RectTransform rt;
        private float timer = 1.2f;

        private void Awake() => rt = (RectTransform)transform;

        private void Update()
        {
            if (rt == null) return;
            timer += Time.unscaledDeltaTime;
            float cycle = timer % interval;
            float sweepDuration = 0.75f;
            if (cycle < sweepDuration)
            {
                float t = cycle / sweepDuration;
                float ease = t * t * (3f - 2f * t);
                rt.anchoredPosition = new Vector2(Mathf.Lerp(-travelWidth * 0.6f, travelWidth * 0.6f, ease), 0f);
            }
            else
            {
                rt.anchoredPosition = new Vector2(-travelWidth, 0f);
            }
        }
    }

    /// <summary>Partículas ambientales (polen/luciérnagas) flotando suavemente en el menú.</summary>
    public class UiAmbientParticles : MonoBehaviour
    {
        private struct Particle
        {
            public RectTransform rt;
            public Image img;
            public float speedY;
            public float driftAmp;
            public float driftFreq;
            public float phase;
            public float baseX;
            public float baseAlpha;
        }

        private readonly Particle[] items = new Particle[18];

        private void Start()
        {
            for (int i = 0; i < items.Length; i++)
            {
                float size = UnityEngine.Random.Range(6f, 16f);
                float x = UnityEngine.Random.Range(-250f, 920f);
                float y = UnityEngine.Random.Range(-520f, 520f);
                bool warm = i % 3 == 0;
                Color col = warm ? UiKit.Gold : UiKit.GreenLight;
                float alpha = UnityEngine.Random.Range(0.18f, 0.45f);
                col.a = alpha;

                Image img = UiKit.MakeCircle(transform, "Mote" + i, new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(size, size), col);
                items[i] = new Particle
                {
                    rt = img.rectTransform,
                    img = img,
                    speedY = UnityEngine.Random.Range(14f, 34f),
                    driftAmp = UnityEngine.Random.Range(18f, 48f),
                    driftFreq = UnityEngine.Random.Range(0.6f, 1.4f),
                    phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                    baseX = x,
                    baseAlpha = alpha
                };
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            for (int i = 0; i < items.Length; i++)
            {
                var p = items[i];
                if (p.rt == null) continue;
                Vector2 pos = p.rt.anchoredPosition;
                pos.y += p.speedY * dt;
                if (pos.y > 560f) pos.y = -560f;
                pos.x = p.baseX + Mathf.Sin(time * p.driftFreq + p.phase) * p.driftAmp;
                p.rt.anchoredPosition = pos;

                Color c = p.img.color;
                c.a = p.baseAlpha * (0.65f + 0.35f * Mathf.Sin(time * 2.2f + p.phase));
                p.img.color = c;
            }
        }
    }

    /// <summary>
    /// Herramientas para construir interfaz por código (sin imágenes): esquinas redondeadas,
    /// brillo exterior, íconos vectoriales procedurales, paneles con borde, textos, barras y controles.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Panel = new Color(0.04f, 0.10f, 0.07f, 0.84f);
        public static readonly Color PanelSolid = new Color(0.05f, 0.12f, 0.09f, 0.96f);
        public static readonly Color CardGlass = new Color(0.04f, 0.09f, 0.07f, 0.92f);
        public static readonly Color CardBorder = new Color(0.42f, 0.80f, 0.52f, 0.55f);
        public static readonly Color ButtonSecondary = new Color(0.09f, 0.19f, 0.14f, 0.95f);
        public static readonly Color ButtonExit = new Color(0.18f, 0.11f, 0.12f, 0.92f);

        public static readonly Color Green = Hex("#3D8E52");
        public static readonly Color GreenVibrant = Hex("#46A35E");
        public static readonly Color GreenLight = Hex("#8CE09B");
        public static readonly Color Cream = Hex("#F2E6C9");
        public static readonly Color Soft = Hex("#C4DEC9");
        public static readonly Color Blue = Hex("#4A91B8");
        public static readonly Color Gold = Hex("#F2C14E");
        public static readonly Color Ink = Hex("#14261B");

        private static Sprite rounded, circle, gradient, glow;
        private static Sprite iconPlay, iconGrid, iconSliders, iconInfo, iconPower, iconLeaf, iconStar, iconSave, iconRefresh;

        public static Sprite Rounded { get { if (rounded == null) rounded = BuildRounded(64, 20, true); return rounded; } }
        public static Sprite Circle { get { if (circle == null) circle = BuildRounded(64, 32, false); return circle; } }
        public static Sprite Glow { get { if (glow == null) glow = BuildGlow(128, 36); return glow; } }

        public static Sprite IconPlay => iconPlay ??= BuildIcon(64, p =>
            SdTriangle(p, new Vector2(-0.32f, 0.48f), new Vector2(-0.32f, -0.48f), new Vector2(0.54f, 0f)) - 0.08f);

        public static Sprite IconGrid => iconGrid ??= BuildIcon(64, p =>
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(0.32f, 0.32f);
            return SdBox(q, new Vector2(0.20f, 0.20f)) - 0.06f;
        });

        public static Sprite IconSliders => iconSliders ??= BuildIcon(64, p =>
        {
            float l1 = Mathf.Min(SdSegment(p, new Vector2(-0.62f, 0.44f), new Vector2(0.62f, 0.44f)) - 0.055f, (p - new Vector2(-0.22f, 0.44f)).magnitude - 0.15f);
            float l2 = Mathf.Min(SdSegment(p, new Vector2(-0.62f, 0.00f), new Vector2(0.62f, 0.00f)) - 0.055f, (p - new Vector2(0.26f, 0.00f)).magnitude - 0.15f);
            float l3 = Mathf.Min(SdSegment(p, new Vector2(-0.62f, -0.44f), new Vector2(0.62f, -0.44f)) - 0.055f, (p - new Vector2(-0.10f, -0.44f)).magnitude - 0.15f);
            return Mathf.Min(l1, Mathf.Min(l2, l3));
        });

        public static Sprite IconInfo => iconInfo ??= BuildIcon(64, p =>
        {
            float ring = Mathf.Abs(p.magnitude - 0.64f) - 0.07f;
            float dot = (p - new Vector2(0f, 0.28f)).magnitude - 0.085f;
            float stem = SdSegment(p, new Vector2(0f, 0.03f), new Vector2(0f, -0.34f)) - 0.075f;
            return Mathf.Min(ring, Mathf.Min(dot, stem));
        });

        public static Sprite IconPower => iconPower ??= BuildIcon(64, p =>
        {
            float ang = Mathf.Abs(Mathf.Atan2(p.x, p.y));
            float ring = Mathf.Abs(p.magnitude - 0.56f) - 0.08f;
            if (ang < 0.62f) ring = 1f;
            float bar = SdSegment(p, new Vector2(0f, 0.12f), new Vector2(0f, 0.68f)) - 0.08f;
            return Mathf.Min(ring, bar);
        });

        public static Sprite IconLeaf => iconLeaf ??= BuildIcon(64, p =>
        {
            float c = 0.7071f;
            Vector2 q = new Vector2(p.x * c - p.y * c, p.x * c + p.y * c);
            float d1 = (q - new Vector2(0f, 0.42f)).magnitude - 0.78f;
            float d2 = (q + new Vector2(0f, 0.42f)).magnitude - 0.78f;
            return Mathf.Max(d1, d2);
        });

        public static Sprite IconStar => iconStar ??= BuildIcon(64, p => SdStar5(p, 0.74f, 0.44f));

        public static Sprite IconSave => iconSave ??= BuildIcon(64, p =>
        {
            float shaft = SdSegment(p, new Vector2(0f, 0.56f), new Vector2(0f, -0.14f)) - 0.075f;
            float headL = SdSegment(p, new Vector2(-0.28f, 0.12f), new Vector2(0f, -0.18f)) - 0.075f;
            float headR = SdSegment(p, new Vector2(0.28f, 0.12f), new Vector2(0f, -0.18f)) - 0.075f;
            float trayB = SdSegment(p, new Vector2(-0.54f, -0.48f), new Vector2(0.54f, -0.48f)) - 0.075f;
            float trayL = SdSegment(p, new Vector2(-0.54f, -0.48f), new Vector2(-0.54f, -0.22f)) - 0.075f;
            float trayR = SdSegment(p, new Vector2(0.54f, -0.48f), new Vector2(0.54f, -0.22f)) - 0.075f;
            return Mathf.Min(Mathf.Min(shaft, Mathf.Min(headL, headR)), Mathf.Min(trayB, Mathf.Min(trayL, trayR)));
        });

        public static Sprite IconRefresh => iconRefresh ??= BuildIcon(64, p =>
        {
            float ang = Mathf.Atan2(p.y, p.x);
            float ring = Mathf.Abs(p.magnitude - 0.52f) - 0.08f;
            if (ang > 0.25f && ang < 1.15f) ring = 1f;
            float arrow = SdTriangle(p, new Vector2(0.54f, 0.30f), new Vector2(0.26f, 0.04f), new Vector2(0.68f, -0.02f)) - 0.03f;
            return Mathf.Min(ring, arrow);
        });

        public static Sprite Gradient
        {
            get
            {
                if (gradient != null) return gradient;
                var tex = new Texture2D(128, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < 128; x++)
                {
                    float a = Mathf.Pow(1f - x / 127f, 1.8f);
                    for (int y = 0; y < 2; y++) tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
                tex.Apply();
                gradient = Sprite.Create(tex, new Rect(0, 0, 128, 2), new Vector2(0.5f, 0.5f), 100f);
                return gradient;
            }
        }

        public static Color Hex(string html)
        {
            ColorUtility.TryParseHtmlString(html, out Color color);
            return color;
        }

        private static Sprite BuildRounded(int size, int radius, bool sliced)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - (half - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - (half - radius), 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - dist)));
                }
            }
            tex.Apply();
            Vector4 border = sliced ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite BuildGlow(int size, int falloff)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float half = size * 0.5f;
            float inner = half - falloff;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + 0.5f - half) - inner, 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + 0.5f - half) - inner, 0f);
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float t = Mathf.Clamp01(1f - dist / falloff);
                    float alpha = t * t * (3f - 2f * t);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            Vector4 border = new Vector4(falloff, falloff, falloff, falloff);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        private static Sprite BuildIcon(int size, Func<Vector2, float> sdf)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            float scale = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2((x + 0.5f) / scale - 1f, (y + 0.5f) / scale - 1f);
                    float distPx = sdf(p) * scale;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distPx)));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static float SdBox(Vector2 p, Vector2 b)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - b;
            return Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        private static float SdSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        private static float SdTriangle(Vector2 p, Vector2 p0, Vector2 p1, Vector2 p2)
        {
            Vector2 e0 = p1 - p0, e1 = p2 - p1, e2 = p0 - p2;
            Vector2 v0 = p - p0, v1 = p - p1, v2 = p - p2;
            Vector2 pq0 = v0 - e0 * Mathf.Clamp01(Vector2.Dot(v0, e0) / Vector2.Dot(e0, e0));
            Vector2 pq1 = v1 - e1 * Mathf.Clamp01(Vector2.Dot(v1, e1) / Vector2.Dot(e1, e1));
            Vector2 pq2 = v2 - e2 * Mathf.Clamp01(Vector2.Dot(v2, e2) / Vector2.Dot(e2, e2));
            float s = Mathf.Sign(e0.x * e2.y - e0.y * e2.x);
            Vector2 d = Vector2.Min(Vector2.Min(
                new Vector2(Vector2.Dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x)),
                new Vector2(Vector2.Dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x))),
                new Vector2(Vector2.Dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x)));
            return -Mathf.Sqrt(d.x) * Mathf.Sign(d.y);
        }

        private static float SdStar5(Vector2 p, float r, float rf)
        {
            const float k1x = 0.809016994f;
            const float k1y = -0.587785252f;
            const float k2x = -k1x;
            const float k2y = k1y;
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(new Vector2(k1x, k1y), p), 0f) * new Vector2(k1x, k1y);
            p -= 2f * Mathf.Max(Vector2.Dot(new Vector2(k2x, k2y), p), 0f) * new Vector2(k2x, k2y);
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            Vector2 ba = rf * new Vector2(-k1y, k1x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        public static Canvas MakeCanvas(string name, int order)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        /// <summary>Rectángulo anclado: el ancla y el pivote son el mismo punto.</summary>
        public static RectTransform Rect(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image MakePanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        /// <summary>Halo de brillo exterior suave detrás de una tarjeta.</summary>
        public static Image MakeGlow(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color, bool pulse = true)
        {
            RectTransform rt = Rect(parent, name, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Glow;
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            if (pulse)
            {
                var p = rt.gameObject.AddComponent<UiPulse>();
                p.minAlpha = color.a * 0.65f;
                p.maxAlpha = Mathf.Clamp01(color.a * 1.35f);
            }
            return image;
        }

        /// <summary>Panel redondeado con borde sutil exterior.</summary>
        public static Image MakeBorderedPanel(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
            Color fillColor, Color borderColor, float borderThickness = 2.5f)
        {
            Image outer = MakePanel(parent, name, anchor, pos, size, borderColor);
            RectTransform innerRt = Stretch(outer.transform, "Inner");
            innerRt.offsetMin = new Vector2(borderThickness, borderThickness);
            innerRt.offsetMax = new Vector2(-borderThickness, -borderThickness);
            var innerImg = innerRt.gameObject.AddComponent<Image>();
            innerImg.sprite = Rounded;
            innerImg.type = Image.Type.Sliced;
            innerImg.color = fillColor;
            return outer;
        }

        public static Image MakeCircle(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Circle;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static Image MakeIcon(Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, "Icon", anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TextMeshProUGUI MakeLabel(Transform parent, string text, float fontSize, Color color,
            TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 size, FontStyles style = FontStyles.Normal)
        {
            RectTransform rt = Rect(parent, "Label", anchor, pos, size);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text;
            t.fontSize = fontSize;
            t.color = color;
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>Barra de progreso: devuelve el rectángulo de relleno (usar SetBar).</summary>
        public static RectTransform MakeBar(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size,
            Color back, Color fill, out Image fillImage)
        {
            Image bg = MakePanel(parent, name, anchor, pos, size, back);
            bg.raycastTarget = false;
            RectTransform fillRt = Stretch(bg.transform, "Fill");
            fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.sprite = Rounded;
            fillImage.type = Image.Type.Sliced;
            fillImage.color = fill;
            fillImage.raycastTarget = false;
            fillRt.anchorMax = new Vector2(0.001f, 1f);
            return fillRt;
        }

        public static void SetBar(RectTransform fill, float value)
        {
            fill.anchorMax = new Vector2(Mathf.Clamp(value, 0.001f, 1f), 1f);
        }

        public static Button MakeButton(Transform parent, string label, Vector2 anchor, Vector2 pos, Vector2 size,
            UnityAction onClick, Color? tint = null, float fontSize = 30f, TextAlignmentOptions align = TextAlignmentOptions.Center,
            Color? textColor = null, float horizontalPadding = 24f, Sprite icon = null, Color? iconColor = null, Color? badgeColor = null)
        {
            RectTransform rt = Rect(parent, label, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            Color c = tint ?? Green;
            ColorBlock colors = button.colors;
            colors.normalColor = c;
            colors.highlightedColor = Color.Lerp(c, Color.white, 0.18f);
            colors.pressedColor = Color.Lerp(c, Color.black, 0.22f);
            colors.selectedColor = Color.Lerp(c, Color.white, 0.12f);
            colors.disabledColor = new Color(0.25f, 0.29f, 0.27f, 0.85f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            float leftInset = horizontalPadding;
            if (icon != null)
            {
                float badgeSize = Mathf.Min(size.y - 18f, 44f);
                Image badge = MakePanel(rt, "IconBadge", new Vector2(0f, 0.5f), new Vector2(14f, 0f),
                    new Vector2(badgeSize, badgeSize), badgeColor ?? new Color(0f, 0f, 0f, 0.24f));
                badge.raycastTarget = false;
                MakeIcon(badge.transform, icon, new Vector2(0.5f, 0.5f), Vector2.zero,
                    new Vector2(badgeSize * 0.54f, badgeSize * 0.54f), iconColor ?? Cream);
                leftInset = 14f + badgeSize + 18f;
            }

            RectTransform labelRt = Stretch(rt, "Label");
            labelRt.offsetMin = new Vector2(leftInset, 4f);
            labelRt.offsetMax = new Vector2(-horizontalPadding, -4f);
            var t = labelRt.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = label;
            t.fontSize = fontSize;
            t.color = textColor ?? Cream;
            t.alignment = align;
            t.fontStyle = FontStyles.Bold;
            t.raycastTarget = false;

            rt.gameObject.AddComponent<UiHover>();

            if (onClick != null) button.onClick.AddListener(onClick);
            button.onClick.AddListener(AudioDirector.PlayUiClick);
            return button;
        }

        public static Slider MakeSlider(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            GameObject go = DefaultControls.CreateSlider(new DefaultControls.Resources());
            go.name = name;
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            Transform background = go.transform.Find("Background");
            if (background != null) background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            if (slider.fillRect != null) slider.fillRect.GetComponent<Image>().color = Green;
            if (slider.handleRect != null)
            {
                slider.handleRect.GetComponent<Image>().color = Cream;
                slider.handleRect.sizeDelta = new Vector2(30f, 0f);
            }
            return slider;
        }
    }
}