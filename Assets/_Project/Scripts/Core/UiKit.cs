using EcoGenesis.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EcoGenesis.Core
{
    /// <summary>Efecto al pasar el mouse por un botón: crece un poco con suavidad.</summary>
    public class UiHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float scale = 1.04f;
        private Vector3 target = Vector3.one;

        public void OnPointerEnter(PointerEventData eventData) => target = Vector3.one * scale;
        public void OnPointerExit(PointerEventData eventData) => target = Vector3.one;
        private void OnDisable() { target = Vector3.one; transform.localScale = Vector3.one; }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, target, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }
    }

    /// <summary>
    /// Herramientas para construir interfaz por código (sin imágenes): esquinas redondeadas generadas,
    /// paneles, textos, barras, botones y deslizadores con la paleta del GDD.
    /// </summary>
    public static class UiKit
    {
        public static readonly Color Panel = new Color(0.04f, 0.10f, 0.07f, 0.80f);
        public static readonly Color PanelSolid = new Color(0.05f, 0.12f, 0.09f, 0.97f);
        public static readonly Color Green = Hex("#4F8B57");
        public static readonly Color GreenLight = Hex("#8FC79A");
        public static readonly Color Cream = Hex("#E9D9B4");
        public static readonly Color Soft = Hex("#C6E0CB");
        public static readonly Color Blue = Hex("#4A91B8");
        public static readonly Color Gold = Hex("#F2C14E");
        public static readonly Color Ink = Hex("#14261B");

        private static Sprite rounded, circle, gradient;

        public static Sprite Rounded { get { if (rounded == null) rounded = BuildRounded(64, 22, true); return rounded; } }
        public static Sprite Circle { get { if (circle == null) circle = BuildRounded(64, 32, false); return circle; } }

        public static Sprite Gradient
        {
            get
            {
                if (gradient != null) return gradient;
                var tex = new Texture2D(128, 2, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int x = 0; x < 128; x++)
                {
                    float a = Mathf.Pow(1f - x / 127f, 1.6f);
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

        public static Image MakeCircle(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Color color)
        {
            RectTransform rt = Rect(parent, name, anchor, pos, size);
            var image = rt.gameObject.AddComponent<Image>();
            image.sprite = Circle;
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
            UnityAction onClick, Color? tint = null, float fontSize = 32f)
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
            colors.highlightedColor = Color.Lerp(c, Color.white, 0.22f);
            colors.pressedColor = Color.Lerp(c, Color.black, 0.25f);
            colors.selectedColor = Color.Lerp(c, Color.white, 0.12f);
            colors.disabledColor = new Color(0.30f, 0.34f, 0.31f, 0.85f);
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            MakeLabel(rt, label, fontSize, Cream, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), Vector2.zero,
                size - new Vector2(20f, 10f), FontStyles.Bold);
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
