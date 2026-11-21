using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Pixel-art power gauge in the spirit of an ARPG experience bar: a brass-framed glass tube with
    /// a solid, continuous fill (glossy highlight on the left, shade on the right, a slow light sweep
    /// rising through it, a bright leading edge), split by ornate separators with brass studs at 25,
    /// 50 and 75 %. The fill heats from green to red as the value rises; near the maximum the gauge
    /// glows (pulsing halo, hot fill) and throws pixel embers. No dithering: crisp pixels, smooth color.
    /// Interaction is a regular uGUI <see cref="Slider"/> laid over the tube. The power can also be set by
    /// pulling the cue back (<see cref="CueController"/>): both work, the player chooses.
    /// </summary>
    public class PowerGauge : MonoBehaviour
    {
        public Slider Slider { get; private set; }

        [Tooltip("Valeur à partir de laquelle la jauge commence à briller.")]
        public float glowStart = 0.85f;
        public float maxEmbersPerSecond = 80f;
        [Tooltip("Vitesse du reflet qui remonte dans la jauge (cycles/s).")]
        public float sweepSpeed = 0.3f;
        [Tooltip("Texte du pourcentage sous la jauge (objet TextMeshPro de la scène).")]
        [SerializeField] TMP_Text percentText;
        [Tooltip("Largeur dessinée de la barre (unités du canvas), centrée dans le RectTransform. La zone cliquable se règle avec Touch Width. 0 = toute la largeur.")]
        [SerializeField] float visualWidth = 52f;
        [Tooltip("Largeur de la zone cliquable / touchable (unités du canvas), centrée sur la barre. Peut dépasser le RectTransform. 0 = largeur du RectTransform.")]
        [SerializeField] float touchWidth = 70f;

        // Sizes in art pixels. The frame is visualWidth wide, centered in the RectTransform (the touch zone);
        // the texture overflows the frame by Pad columns on each side (brass studs).
        const int Pad = 3;
        int frameWidth;
        int Width => frameWidth + 2 * Pad;
        int FrameLeft => Pad;
        int FrameRight => Pad + frameWidth - 1;    // frame columns (inclusive)
        int TubeLeft => FrameLeft + 3;
        int TubeRight => FrameRight - 3;           // fill columns (inclusive)
        const int AreaBottom = 3;                  // first fill row
        const int HaloPad = 12;

        int height;
        int AreaTop => height - 4;                 // last fill row (inclusive)
        int AreaHeight => AreaTop - AreaBottom + 1;

        RectTransform root;
        Vector2 fittedRect;
        RectTransform touchZone;
        RawImage gaugeImage, halo;
        Texture2D texture;
        Color32[] pixels;
        RectTransform handleArea;
        RectTransform emberLayer;
        int shownPercent = -1;
        float emberAccumulator;

        struct Ember { public RectTransform rt; public Image img; public Vector2 pos, vel; public float age, life; }
        readonly List<Ember> embers = new List<Ember>();
        readonly Stack<Image> emberPool = new Stack<Image>();

        static readonly Color32 Empty = PixelArt.Hex("14111d");
        static readonly Color32 EmptyShade = PixelArt.Hex("0b0911");
        static readonly Color32 Separator = PixelArt.Hex("07040b");
        static readonly Color32[] Brass = PixelArt.Palette("3a2412", "6e4a1e", "a8742a", "d9a441", "f5d37a", "fff0b8");
        static readonly Color[] Heat =
        {
            PixelArt.Hex("3fbf4a"), PixelArt.Hex("a6cf33"), PixelArt.Hex("f2c230"),
            PixelArt.Hex("f0861c"), PixelArt.Hex("e8401f"), PixelArt.Hex("cf1a22")
        };

        public void Init(float initialValue)
        {
            root = (RectTransform)transform;

            halo = new GameObject("Halo", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            halo.transform.SetParent(root, false);
            halo.raycastTarget = false;
            halo.color = new Color(1f, 0.45f, 0.15f, 0f);

            gaugeImage = new GameObject("Gauge", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            gaugeImage.transform.SetParent(root, false);
            gaugeImage.raycastTarget = false;

            emberLayer = new GameObject("Embers", typeof(RectTransform)).GetComponent<RectTransform>();
            emberLayer.SetParent(root, false);
            emberLayer.anchorMin = emberLayer.anchorMax = emberLayer.pivot = Vector2.zero;

            // Slider over the tube: a transparent raycast target (the TouchZone child, touchWidth wide; its
            // pointer events bubble up to the Slider) + an invisible handle in a container matching the fill
            // area vertically, so clicks map 1:1 to the drawn fill.
            touchZone = new GameObject("TouchZone", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            touchZone.SetParent(root, false);
            touchZone.SetAsFirstSibling();
            var hit = touchZone.GetComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            touchZone.anchorMin = Vector2.zero;
            touchZone.anchorMax = Vector2.one;
            Slider = gameObject.AddComponent<Slider>();
            handleArea = new GameObject("HandleArea", typeof(RectTransform)).GetComponent<RectTransform>();
            handleArea.SetParent(root, false);
            var handle = new GameObject("Handle", typeof(RectTransform)).GetComponent<RectTransform>();
            handle.SetParent(handleArea, false);
            handle.sizeDelta = Vector2.zero;
            Slider.targetGraphic = hit;
            Slider.transition = Selectable.Transition.None;
            Slider.navigation = new Navigation { mode = Navigation.Mode.None };
            Slider.direction = Slider.Direction.BottomToTop;
            Slider.handleRect = handle;
            Slider.minValue = 0f;
            Slider.maxValue = 1f;
            Slider.value = initialValue;

            if (percentText != null) percentText.transform.SetAsLastSibling(); // above the runtime-built gauge layers
        }

        /// <summary>
        /// Follows the RectTransform: rebuilds the textures when its size changes. The frame
        /// is drawn <see cref="visualWidth"/> wide, centered, so the bar can look thin while staying easy to
        /// touch. Called every frame (cheap when nothing changed), so the layout can be edited live.
        /// </summary>
        void FitToRect()
        {
            const int px = PixelUI.ArtPixel;
            Vector2 rect = root.rect.size;
            float drawn = visualWidth > 0f ? Mathf.Min(visualWidth, rect.x) : rect.x;
            int newWidth = Mathf.Max(10, Mathf.RoundToInt(drawn / px));
            int newHeight = Mathf.Max(60, Mathf.RoundToInt(rect.y / px));
            if (newWidth == frameWidth && newHeight == height && rect == fittedRect) return;
            fittedRect = rect;
            frameWidth = newWidth;
            height = newHeight;
            float inset = (rect.x - frameWidth * px) / 2f; // from each side of the touch zone to the frame

            if (texture != null) Destroy(texture);
            texture = PixelArt.NewTexture(Width, height, "PowerGauge");
            pixels = new Color32[Width * height];
            gaugeImage.texture = texture;
            var grt = gaugeImage.rectTransform;
            grt.anchorMin = Vector2.zero;
            grt.anchorMax = Vector2.one;
            grt.offsetMin = new Vector2(inset - Pad * px, 0f); // the studs overflow the frame
            grt.offsetMax = new Vector2(-(inset - Pad * px), 0f);

            if (halo.texture != null) Destroy(halo.texture);
            halo.texture = CreateHalo(frameWidth, height, HaloPad);
            var hrt = halo.rectTransform;
            hrt.anchorMin = Vector2.zero;
            hrt.anchorMax = Vector2.one;
            hrt.offsetMin = new Vector2(inset - HaloPad * px, -HaloPad * px);
            hrt.offsetMax = new Vector2(-(inset - HaloPad * px), HaloPad * px);

            // The handle area maps clicks to values vertically (the touch zone width is FitTouchZone's).
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(0f, AreaBottom * px);
            handleArea.offsetMax = new Vector2(0f, -(height - 1 - AreaTop) * px);

            // Embers move in texture (art pixel) coordinates.
            emberLayer.anchoredPosition = new Vector2(inset - Pad * px, 0f);
            emberLayer.sizeDelta = new Vector2(Width, height);
            emberLayer.localScale = new Vector3(px, px, 1f);
        }

        /// <summary>Keeps the touch zone touchWidth wide, centered on the bar, as tall as the RectTransform.</summary>
        void FitTouchZone()
        {
            if (touchZone == null) return;
            float inset = touchWidth > 0f ? (root.rect.width - touchWidth) / 2f : 0f;
            var min = new Vector2(inset, 0f);
            var max = new Vector2(-inset, 0f);
            if (touchZone.offsetMin != min) touchZone.offsetMin = min;
            if (touchZone.offsetMax != max) touchZone.offsetMax = max;
        }

        void Update()
        {
            if (root == null) return;
            FitToRect();
            FitTouchZone();
            if (texture == null) return;
            float v = Slider.value;
            float glow = Mathf.Clamp01((v - glowStart) / (1f - glowStart));
            glow *= glow;
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 10f);
            float heat = glow * (0.55f + 0.45f * pulse);

            Redraw(v, heat); // every frame: the light sweep is animated

            halo.color = new Color(1f, 0.42f, 0.12f, glow * (0.35f + 0.45f * pulse));

            int percent = Mathf.RoundToInt(v * 100f);
            if (percent != shownPercent && percentText != null)
            {
                shownPercent = percent;
                percentText.text = GameTexts.Format(GameTexts.Current.powerFormat, percent);
            }
            if (percentText == null) { UpdateEmbers(v, glow); return; }
            Color tint = Color.Lerp(PixelUI.TextLight, PixelArt.Hex("ff7a3d"), v * v);
            if (glow > 0f) tint = Color.Lerp(tint, PixelArt.Hex("ffe9a0"), heat * 0.6f);
            percentText.color = tint;

            UpdateEmbers(v, glow);
        }

        void Redraw(float v, float heat)
        {
            int fillRows = Mathf.RoundToInt(v * AreaHeight);
            int fillTop = AreaBottom + fillRows - 1;
            float sweep = Mathf.Repeat(Time.time * sweepSpeed, 1.4f) - 0.2f; // 0..1 along the tube, with pauses
            float sweepRow = AreaBottom + sweep * AreaHeight;

            // Separator rows at 25 / 50 / 75 %.
            int sep1 = AreaBottom + Mathf.RoundToInt(AreaHeight * 0.25f);
            int sep2 = AreaBottom + Mathf.RoundToInt(AreaHeight * 0.5f);
            int sep3 = AreaBottom + Mathf.RoundToInt(AreaHeight * 0.75f);
            Color32 hotFrame = PixelArt.Hex("ffd27a");
            Color hotFill = PixelArt.Hex("ffb04a");

            for (int y = 0; y < height; y++)
            for (int x = 0; x < Width; x++)
            {
                Color32 c = new Color32(0, 0, 0, 0);
                bool isSep = y == sep1 || y == sep2 || y == sep3;

                if (x >= FrameLeft && x <= FrameRight)
                {
                    bool outline = x == FrameLeft || x == FrameRight || y == 0 || y == height - 1;
                    bool frame = !outline && (x < TubeLeft || x > TubeRight || y < AreaBottom || y > AreaTop);
                    if (outline) c = PixelUI.Outline;
                    else if (frame)
                    {
                        bool inner = x == TubeLeft - 1 || x == TubeRight + 1 || y == AreaBottom - 1 || y == AreaTop + 1;
                        bool lit = x < TubeLeft || y > AreaTop;
                        c = inner ? Brass[0] : Brass[lit ? 4 : 2];
                        if (heat > 0f && !inner) c = Color32.Lerp(c, hotFrame, heat * 0.5f);
                    }
                    else
                    {
                        // Tube interior.
                        float u = (x - TubeLeft) / (float)(TubeRight - TubeLeft); // 0 left → 1 right
                        int local = y - AreaBottom;
                        if (local < fillRows)
                        {
                            float s = (local + 0.5f) / AreaHeight;
                            Color col = HeatColor(s * 0.6f + v * 0.45f); // whole fill reddens as power rises
                            // Glass tube: bright reflection strip on the left, shade on the right.
                            col = Color.Lerp(col, Color.white, Mathf.Clamp01(1f - u * 4f) * 0.42f);
                            col = Color.Lerp(col, Color.black, Mathf.Clamp01((u - 0.55f) / 0.45f) * 0.38f);
                            // Slow light sweep rising through the fill.
                            float sd = Mathf.Abs(y - sweepRow);
                            if (sd < 10f) col = Color.Lerp(col, Color.white, (1f - sd / 10f) * 0.16f);
                            // Bright leading edge.
                            if (y == fillTop) col = Color.Lerp(col, Color.white, 0.55f);
                            else if (y == fillTop - 1) col = Color.Lerp(col, Color.white, 0.22f);
                            if (heat > 0f) col = Color.Lerp(col, hotFill, heat * 0.3f);
                            c = col;
                        }
                        else
                        {
                            c = Color32.Lerp(Empty, EmptyShade, Mathf.Clamp01((u - 0.5f) * 2f));
                            if (x == TubeLeft) c = PixelArt.Hex("1f1a2b"); // faint glass edge
                        }

                        // Separators: dark engraved groove, lit bevel above, soft shade below.
                        if (isSep) c = Separator;
                        else if (y == sep1 + 1 || y == sep2 + 1 || y == sep3 + 1) c = Color32.Lerp(c, Color.white, 0.18f);
                        else if (y == sep1 - 1 || y == sep2 - 1 || y == sep3 - 1) c = Color32.Lerp(c, Color.black, 0.3f);
                    }
                }

                // Brass studs (small diamonds) on both sides of the frame at each separator.
                int dSep = Mathf.Min(Mathf.Abs(y - sep1), Mathf.Min(Mathf.Abs(y - sep2), Mathf.Abs(y - sep3)));
                if (dSep <= 3)
                {
                    for (int side = 0; side < 2; side++)
                    {
                        int cx = side == 0 ? FrameLeft + 1 : FrameRight - 1;
                        int m = Mathf.Abs(x - cx) + dSep;
                        if (m == 3) c = PixelUI.Outline;
                        else if (m == 2) c = Brass[2];
                        else if (m == 1) c = Brass[4];
                        else if (m == 0) c = Brass[5];
                    }
                }

                pixels[y * Width + x] = c;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        static Color HeatColor(float h)
        {
            float f = Mathf.Clamp01(h) * (Heat.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), Heat.Length - 2);
            return Color.Lerp(Heat[i], Heat[i + 1], f - i);
        }

        void UpdateEmbers(float v, float glow)
        {
            float dt = Time.deltaTime;
            emberAccumulator += glow * maxEmbersPerSecond * dt;
            int fillTop = AreaBottom + Mathf.RoundToInt(v * AreaHeight);
            while (emberAccumulator >= 1f)
            {
                emberAccumulator -= 1f;
                bool fromTop = Random.value < 0.7f;
                var e = new Ember
                {
                    pos = fromTop
                        ? new Vector2(Random.Range(TubeLeft, TubeRight + 1), fillTop)
                        : new Vector2(Random.value < 0.5f ? FrameLeft : FrameRight, Random.Range(AreaBottom, fillTop)),
                    vel = new Vector2(Random.Range(-14f, 14f), Random.Range(22f, 62f)),
                    life = Random.Range(0.45f, 1.1f)
                };
                e.img = emberPool.Count > 0 ? emberPool.Pop() : CreateEmberImage();
                e.rt = e.img.rectTransform;
                int size = Random.value < 0.35f ? 2 : 1;
                e.rt.sizeDelta = new Vector2(size, size);
                e.img.gameObject.SetActive(true);
                embers.Add(e);
            }

            for (int i = embers.Count - 1; i >= 0; i--)
            {
                var e = embers[i];
                e.age += dt;
                if (e.age >= e.life)
                {
                    e.img.gameObject.SetActive(false);
                    emberPool.Push(e.img);
                    embers.RemoveAt(i);
                    continue;
                }
                e.vel.x += Random.Range(-40f, 40f) * dt; // flicker drift
                e.pos += e.vel * dt;
                float k = e.age / e.life;
                Color c = k < 0.3f
                    ? Color.Lerp(new Color(1f, 0.96f, 0.75f), new Color(1f, 0.7f, 0.25f), k / 0.3f)
                    : Color.Lerp(new Color(1f, 0.7f, 0.25f), new Color(0.85f, 0.15f, 0.1f), (k - 0.3f) / 0.7f);
                c.a = 1f - k * k;
                e.img.color = c;
                e.rt.anchoredPosition = new Vector2(Mathf.Round(e.pos.x), Mathf.Round(e.pos.y)); // stay on the pixel grid
                embers[i] = e;
            }
        }

        Image CreateEmberImage()
        {
            var go = new GameObject("Ember", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(emberLayer, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            return img;
        }

        /// <summary>Soft rectangular glow around the frame (smooth alpha).</summary>
        static Texture2D CreateHalo(int frameWidth, int h, int pad)
        {
            int W = frameWidth + pad * 2, H = h + pad * 2;
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(pad - x, x - (W - 1 - pad)));
                float dy = Mathf.Max(0f, Mathf.Max(pad - y, y - (H - 1 - pad)));
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy) / pad);
                px[y * W + x] = new Color32(255, 255, 255, (byte)(a * a * 255f));
            }
            var tex = PixelArt.NewTexture(W, H, "GaugeHalo");
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }
    }
}
