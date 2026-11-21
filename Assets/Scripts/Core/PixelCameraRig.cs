using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Low-resolution rendering: the world camera renders at exactly <see cref="PixelArt.PPU"/> pixels
    /// per world unit into a small RenderTexture covering the whole screen, which a full-screen RawImage
    /// shows at <see cref="DisplayScale"/> screen pixels per art pixel. Post-processing (bloom, vignette)
    /// runs at that low resolution, so glows are pixelated too.
    /// The scale is set so that the fit rect (world units, e.g. the table and its margins, given by
    /// <see cref="TableFitter"/>) fills a screen rect exactly. It is usually not an integer: the RawImage
    /// uses the PixelUpscale shader ("sharp bilinear") so every art pixel stays a crisp, even block.
    /// The camera position is snapped to the art pixel grid so static art never shimmers.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelCameraRig : MonoBehaviour
    {
        public static PixelCameraRig Instance { get; private set; }

        public PoolTable table;
        public Color screenColor = new Color(0.02f, 0.02f, 0.035f);
        [Tooltip("Shader d'agrandissement net (Hidden/RoguelikePool/PixelUpscale).")]
        public Shader upscaleShader;

        /// <summary>Screen pixels per art pixel (not an integer in general).</summary>
        public float DisplayScale { get; private set; } = 1f;

        /// <summary>Optional vignette to keep centered on the table.</summary>
        public Vignette Vignette { get; set; }

        /// <summary>
        /// Layer drawn by the overlay camera instead of the world camera (the cue): its render is shown
        /// on its own canvas at <see cref="OverlaySortingOrder"/>, above the world and above UI canvases
        /// sorted lower (shot chalks, money), but below the main UI (order 0).
        /// </summary>
        public const int OverlayLayer = 8;
        public const int WorldSortingOrder = -100;
        public const int BelowOverlaySortingOrder = -50; // UI elements the cue passes over
        public const int OverlaySortingOrder = -40;

        Camera cam;
        RenderTexture target;
        RawImage display;
        Camera overlayCam;
        RenderTexture overlayTarget;
        RawImage overlayDisplay;
        Rect fitWorld, fitScreen;
        bool hasFit;
        Vector2 displayOrigin; // screen position of the render texture's bottom-left corner

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.allowMSAA = false;
            cam.allowHDR = true; // needed for HDR glow → bloom
            cam.allowDynamicResolution = false;
            var data = cam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;

            cam.cullingMask &= ~(1 << OverlayLayer); // the cue is drawn by the overlay camera

            CreateScreenCamera();
            CreateDisplay();
            CreateOverlayCamera();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (target != null) target.Release();
            if (overlayTarget != null) overlayTarget.Release();
        }

        /// <summary>
        /// Makes <paramref name="world"/> (world units) fill <paramref name="screen"/> (screen pixels,
        /// bottom-left origin) exactly, keeping the aspect ratio (the limiting side fills, the other is centered).
        /// </summary>
        public void SetFit(Rect world, Rect screen)
        {
            fitWorld = world;
            fitScreen = screen;
            hasFit = world.width > 0f && world.height > 0f && screen.width > 0f && screen.height > 0f;
            Refresh();
        }

        /// <summary>Converts a screen position (pointer) to world space.</summary>
        public Vector2 ScreenToWorld(Vector2 screen)
        {
            Vector2 low = (screen - displayOrigin) / DisplayScale;
            return cam.ScreenToWorldPoint(new Vector3(low.x, low.y, 0f));
        }

        /// <summary>Inverse of <see cref="ScreenToWorld"/>: world point → real screen pixels.</summary>
        public Vector2 WorldToScreen(Vector2 world)
        {
            Vector3 low = cam.WorldToScreenPoint(world); // pixels of the low-res target
            return displayOrigin + new Vector2(low.x, low.y) * DisplayScale;
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            int sw = Screen.width, sh = Screen.height;
            if (sw <= 0 || sh <= 0) return;
            int ppu = PixelArt.PPU;
            EnsureUpscaleMaterial();

            // Without a fit rect (no TableFitter): the table's frame fills the whole screen.
            Rect world = fitWorld, screen = fitScreen;
            if (!hasFit)
            {
                if (table == null) return;
                world = table.OuterBounds;
                world.position += (Vector2)table.transform.position;
                screen = new Rect(0f, 0f, sw, sh);
            }

            float unitsToScreen = Mathf.Min(screen.width / world.width, screen.height / world.height);
            float k = Mathf.Max(0.25f, unitsToScreen / ppu); // screen pixels per art pixel
            int w = Mathf.CeilToInt(sw / k) + 2;
            int h = Mathf.CeilToInt(sh / k) + 2;

            if (target == null || target.width != w || target.height != h)
            {
                if (target != null) target.Release();
                target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "PixelTarget",
                    filterMode = FilterMode.Bilinear, // the upscale shader keeps the pixels crisp
                    antiAliasing = 1,
                    useMipMap = false
                };
                target.Create();
                cam.targetTexture = target;
                display.texture = target;

                if (overlayTarget != null) overlayTarget.Release();
                overlayTarget = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "PixelOverlayTarget",
                    filterMode = FilterMode.Bilinear,
                    antiAliasing = 1,
                    useMipMap = false
                };
                overlayTarget.Create();
                overlayCam.targetTexture = overlayTarget;
                overlayDisplay.texture = overlayTarget;
            }
            DisplayScale = k;
            cam.orthographicSize = h / (2f * ppu);
            overlayCam.orthographicSize = cam.orthographicSize;

            // Camera on the fit center, snapped so world-grid art lands on whole texels.
            Vector2 c = world.center;
            float cx = (Mathf.Round(c.x * ppu - w / 2f) + w / 2f) / ppu;
            float cy = (Mathf.Round(c.y * ppu - h / 2f) + h / 2f) / ppu;
            transform.position = new Vector3(cx, cy, -10f);

            // Place the texture so the fit center lands on the screen rect center.
            Vector2 centerTexel = new Vector2((c.x - cx) * ppu + w / 2f, (c.y - cy) * ppu + h / 2f);
            displayOrigin = screen.center - centerTexel * k;
            var rt = display.rectTransform;
            rt.anchoredPosition = displayOrigin;
            rt.sizeDelta = new Vector2(w * k, h * k);
            var ort = overlayDisplay.rectTransform;
            ort.anchoredPosition = displayOrigin;
            ort.sizeDelta = rt.sizeDelta;

            if (Vignette != null)
                Vignette.center.Override(new Vector2(centerTexel.x / w, centerTexel.y / h));
        }

        bool materialChecked;

        /// <summary>Puts the sharp-bilinear material on the display (once; the shader is set by GameBootstrap).</summary>
        void EnsureUpscaleMaterial()
        {
            if (materialChecked) return;
            materialChecked = true;
            if (upscaleShader == null) upscaleShader = Shader.Find("Hidden/RoguelikePool/PixelUpscale");
            if (upscaleShader != null)
            {
                display.material = new Material(upscaleShader) { name = "PixelUpscale" };
                // The overlay render is transparent and premultiplied (sprites blended over a clear black).
                var overlay = new Material(upscaleShader) { name = "PixelUpscaleOverlay" };
                overlay.SetFloat("_KeepAlpha", 1f);
                overlay.SetFloat("_SrcBlend", (float)BlendMode.One);
                overlay.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                overlayDisplay.material = overlay;
            }
            else Debug.LogWarning("PixelCameraRig : shader PixelUpscale introuvable, agrandissement bilinéaire simple.");
        }

        void CreateScreenCamera()
        {
            // Clears the real screen (the world camera renders into the texture).
            var go = new GameObject("ScreenCamera");
            var screenCam = go.AddComponent<Camera>();
            screenCam.cullingMask = 0;
            screenCam.clearFlags = CameraClearFlags.SolidColor;
            screenCam.backgroundColor = screenColor;
            screenCam.orthographic = true;
            screenCam.depth = cam.depth - 10f;
            screenCam.allowMSAA = false;
            screenCam.allowHDR = false;
            screenCam.GetUniversalAdditionalCameraData().renderPostProcessing = false;
        }

        void CreateDisplay()
        {
            display = CreateDisplayImage("PixelDisplay", "World", WorldSortingOrder);
            overlayDisplay = CreateDisplayImage("PixelOverlayDisplay", "Overlay", OverlaySortingOrder);
        }

        static RawImage CreateDisplayImage(string canvasName, string imageName, int sortingOrder)
        {
            var canvasGo = new GameObject(canvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            canvas.pixelPerfect = false; // the texture sits on fractional positions
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var go = new GameObject(imageName, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(canvasGo.transform, false);
            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            return image;
        }

        /// <summary>
        /// Second camera, child of the world camera (same position and size), that renders only
        /// <see cref="OverlayLayer"/> on a transparent background, with the same post-processing (needs
        /// "Allow Post Process Alpha Output" on the URP assets to keep the alpha).
        /// </summary>
        void CreateOverlayCamera()
        {
            var go = new GameObject("OverlayCamera");
            go.transform.SetParent(transform, false);
            overlayCam = go.AddComponent<Camera>();
            overlayCam.orthographic = true;
            overlayCam.cullingMask = 1 << OverlayLayer;
            overlayCam.clearFlags = CameraClearFlags.SolidColor;
            overlayCam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            overlayCam.depth = cam.depth + 1f;
            overlayCam.allowMSAA = false;
            overlayCam.allowHDR = true;
            overlayCam.allowDynamicResolution = false;
            var data = overlayCam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.None;
        }

        /// <summary>Global volume: low-res bloom on HDR glows, cool vignette, punchier contrast.</summary>
        public static Volume CreatePostProcessing(out Vignette vignette)
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(0.85f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(1f, 0.86f, 0.72f));
            bloom.highQualityFiltering.Override(false);

            vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(new Color(0.03f, 0.015f, 0.07f));

            var color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(14f);
            color.saturation.Override(10f);

            var tonemapping = profile.Add<Tonemapping>(true);
            tonemapping.mode.Override(TonemappingMode.None); // keep the palette exact

            var go = new GameObject("PostProcessVolume");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            volume.sharedProfile = profile;
            return volume;
        }
    }
}
