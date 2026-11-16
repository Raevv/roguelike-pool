using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Authentic low-resolution rendering: the world camera renders at exactly
    /// <see cref="PixelArt.PPU"/> pixels per world unit into a small point-filtered RenderTexture,
    /// which is shown full screen at an integer scale (<see cref="Scale"/>). Post-processing (bloom,
    /// vignette) runs at that low resolution, so glows are pixelated too.
    /// The scale is the largest integer that fits the whole table in the area left of the UI panel.
    /// The camera position is snapped to the pixel grid so static art never shimmers.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PixelCameraRig : MonoBehaviour
    {
        public static PixelCameraRig Instance { get; private set; }

        public PoolTable table;
        [Range(0f, 0.4f)] public float rightPanelFraction = 0.2f;
        [Tooltip("Marge autour de la table (unités).")]
        public float margin = 0.5f;
        public Color screenColor = new Color(0.02f, 0.02f, 0.035f);

        /// <summary>Screen pixels per art pixel.</summary>
        public int Scale { get; private set; } = 1;

        /// <summary>Optional vignette to keep centered on the table.</summary>
        public Vignette Vignette { get; set; }

        Camera cam;
        RenderTexture target;
        RawImage display;

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

            CreateScreenCamera();
            CreateDisplay();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (target != null) target.Release();
        }

        /// <summary>Converts a screen position (pointer) to world space.</summary>
        public Vector2 ScreenToWorld(Vector2 screen)
        {
            return cam.ScreenToWorldPoint(new Vector3(screen.x / Scale, screen.y / Scale, 0f));
        }

        void LateUpdate() => Refresh();

        public void Refresh()
        {
            if (table == null) return;
            int sw = Screen.width, sh = Screen.height;
            if (sw <= 0 || sh <= 0) return;
            int ppu = PixelArt.PPU;

            Rect b = table.OuterBounds;
            b.position += (Vector2)table.transform.position;
            float needW = (b.width + 2f * margin) * ppu;
            float needH = (b.height + 2f * margin) * ppu;
            int k = Mathf.Max(1, Mathf.FloorToInt(Mathf.Min(sw * (1f - rightPanelFraction) / needW, sh / needH)));
            int w = Mathf.CeilToInt(sw / (float)k);
            int h = Mathf.CeilToInt(sh / (float)k);

            if (target == null || target.width != w || target.height != h)
            {
                if (target != null) target.Release();
                target = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32)
                {
                    name = "PixelTarget",
                    filterMode = FilterMode.Point,
                    antiAliasing = 1,
                    useMipMap = false
                };
                target.Create();
                cam.targetTexture = target;
                display.texture = target;
            }
            Scale = k;
            display.rectTransform.sizeDelta = new Vector2(w * k, h * k);
            cam.orthographicSize = h / (2f * ppu);

            // Center the table in the visible area left of the panel (the target may overflow the
            // screen by less than one art pixel at the top/right).
            float leftArea = sw * (1f - rightPanelFraction) / k;
            float visibleH = sh / (float)k;
            float cx = b.center.x + (w / 2f - leftArea / 2f) / ppu;
            float cy = b.center.y + (h / 2f - visibleH / 2f) / ppu;
            cx = (Mathf.Round(cx * ppu - w / 2f) + w / 2f) / ppu;
            cy = (Mathf.Round(cy * ppu - h / 2f) + h / 2f) / ppu;
            transform.position = new Vector3(cx, cy, -10f);

            if (Vignette != null)
                Vignette.center.Override(new Vector2(leftArea / 2f / w, visibleH / 2f / h));
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
            var canvasGo = new GameObject("PixelDisplay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;
            canvas.pixelPerfect = true;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            var go = new GameObject("World", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(canvasGo.transform, false);
            display = go.GetComponent<RawImage>();
            display.raycastTarget = false;
            var rt = display.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
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
