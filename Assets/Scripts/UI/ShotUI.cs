using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Pixel-art shot UI for a portrait phone screen: a slate panel on the right (thumb side) with the
    /// power gauge, a fine-aim toggle and the TIRER button at the bottom, plus a banner for short
    /// messages over the table. The canvas uses the same integer pixel scale as the world
    /// (<see cref="PixelCameraRig.Scale"/>), so one canvas unit = one art pixel.
    /// </summary>
    public class ShotUI : MonoBehaviour
    {
        public event Action ShootClicked;

        public float Power
        {
            get => gauge.Slider.value;
            set => gauge.Slider.value = Mathf.Clamp01(value);
        }

        /// <summary>Fine aim toggle state (touch-friendly replacement for holding Shift).</summary>
        public bool FineAim { get; private set; }

        /// <summary>Screen area left of the panel (over the table), for overlays such as the stage HUD.</summary>
        public RectTransform TableArea { get; private set; }

        const int ShootW = 96, ShootH = 60, FineW = 92, FineH = 48;

        PixelCameraRig rig;
        CanvasScaler scaler;
        GraphicRaycaster raycaster;
        RectTransform panel;
        PowerGauge gauge;
        Button shootButton;
        Button fineButton;
        Image fineImage;
        Sprite fineOff, fineOffPressed, fineOn, fineOnPressed;
        RawImage banner;
        Coroutine messageRoutine;
        int lastScale = -1, lastScreenHeight = -1;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public void Build(float panelFraction, PixelCameraRig pixelRig)
        {
            rig = pixelRig;
            EnsureEventSystem();

            var canvasGo = new GameObject("ShotCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 1f; // sprites are 1 texel per unit
            canvas.referencePixelsPerUnit = 1f;
            raycaster = canvasGo.GetComponent<GraphicRaycaster>();

            // Panel.
            var panelImage = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            panelImage.transform.SetParent(canvasGo.transform, false);
            panelImage.sprite = PixelUI.Panel();
            panelImage.type = Image.Type.Tiled;
            panelImage.pixelsPerUnitMultiplier = 1f;
            panel = panelImage.rectTransform;
            panel.anchorMin = new Vector2(1f - panelFraction, 0f);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;

            var title = PixelUI.Text(panel, "Title", "FORCE", PixelUI.Gold, 2);
            PixelUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, -14f));

            gauge = new GameObject("PowerGauge", typeof(RectTransform)).AddComponent<PowerGauge>();
            gauge.Build(panel, 0.5f);
            PixelUI.Place((RectTransform)gauge.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f));

            BuildFineAimButton();
            BuildShootButton();
            BuildBanner(canvasGo.transform, panelFraction);
            ApplyScale();
        }

        void Update() => ApplyScale();

        /// <summary>Follows the world pixel scale and resizes the gauge to the available height.</summary>
        void ApplyScale()
        {
            int k = rig != null ? rig.Scale : 1;
            if (k == lastScale && Screen.height == lastScreenHeight) return;
            lastScale = k;
            lastScreenHeight = Screen.height;
            scaler.scaleFactor = k;
            int artHeight = Screen.height / k;
            gauge.SetHeight(Mathf.Clamp(artHeight - 160 - 40, 80, 620));
        }

        public void SetCanShoot(bool canShoot)
        {
            if (shootButton.interactable != canShoot) shootButton.interactable = canShoot;
        }

        public void ShowMessage(string message, float duration)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        /// <summary>True if the given screen position is over any UI element of this canvas.</summary>
        public bool IsOverUI(Vector2 screenPosition)
        {
            if (EventSystem.current == null) return false;
            raycastResults.Clear();
            raycaster.Raycast(new PointerEventData(EventSystem.current) { position = screenPosition }, raycastResults);
            return raycastResults.Count > 0;
        }

        IEnumerator MessageRoutine(string message, float duration)
        {
            if (banner.texture != null) Destroy(banner.texture);
            banner.texture = PixelUI.Banner(message, 2);
            banner.SetNativeSize();
            banner.gameObject.SetActive(true);

            // Quick pixel "pop": drop in by a few art pixels.
            var rt = banner.rectTransform;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                rt.anchoredPosition = new Vector2(0f, Mathf.Round(Mathf.Lerp(8f, 0f, t / 0.12f)));
                yield return null;
            }
            rt.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(duration);
            banner.gameObject.SetActive(false);
            messageRoutine = null;
        }

        void BuildShootButton()
        {
            var image = new GameObject("ShootButton", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(panel, false);
            image.sprite = PixelUI.Button(ShootW, ShootH, PixelUI.Fire, "TIRER", 2, false, false);
            image.rectTransform.sizeDelta = new Vector2(ShootW, ShootH);
            PixelUI.Place(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f));

            shootButton = image.gameObject.AddComponent<Button>();
            shootButton.targetGraphic = image;
            shootButton.navigation = new Navigation { mode = Navigation.Mode.None };
            shootButton.transition = Selectable.Transition.SpriteSwap;
            shootButton.spriteState = new SpriteState
            {
                highlightedSprite = PixelUI.Button(ShootW, ShootH, PixelUI.Fire, "TIRER", 2, false, true),
                pressedSprite = PixelUI.Button(ShootW, ShootH, PixelUI.Fire, "TIRER", 2, true, true),
                selectedSprite = image.sprite,
                disabledSprite = PixelUI.Button(ShootW, ShootH, PixelUI.Disabled, "...", 2, false, false)
            };
            shootButton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                ShootClicked?.Invoke();
            });
        }

        void BuildFineAimButton()
        {
            fineOff = PixelUI.Button(FineW, FineH, PixelUI.Stone, "VISÉE\nFINE", 2, false, false);
            fineOffPressed = PixelUI.Button(FineW, FineH, PixelUI.Stone, "VISÉE\nFINE", 2, true, true);
            fineOn = PixelUI.Button(FineW, FineH, PixelUI.GoldOn, "VISÉE\nFINE", 2, false, false);
            fineOnPressed = PixelUI.Button(FineW, FineH, PixelUI.GoldOn, "VISÉE\nFINE", 2, true, true);

            fineImage = new GameObject("FineAimButton", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fineImage.transform.SetParent(panel, false);
            fineImage.sprite = fineOff;
            fineImage.rectTransform.sizeDelta = new Vector2(FineW, FineH);
            PixelUI.Place(fineImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 80f));

            fineButton = fineImage.gameObject.AddComponent<Button>();
            fineButton.targetGraphic = fineImage;
            fineButton.navigation = new Navigation { mode = Navigation.Mode.None };
            fineButton.transition = Selectable.Transition.SpriteSwap;
            fineButton.spriteState = new SpriteState { pressedSprite = fineOffPressed, highlightedSprite = fineOff, selectedSprite = fineOff };
            fineButton.onClick.AddListener(() =>
            {
                FineAim = !FineAim;
                fineImage.sprite = FineAim ? fineOn : fineOff;
                var normal = fineImage.sprite;
                fineButton.spriteState = new SpriteState
                {
                    pressedSprite = FineAim ? fineOnPressed : fineOffPressed,
                    highlightedSprite = normal,
                    selectedSprite = normal
                };
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            });
        }

        void BuildBanner(Transform canvas, float panelFraction)
        {
            // Container centered on the table area (left of the panel).
            var area = new GameObject("BannerArea", typeof(RectTransform)).GetComponent<RectTransform>();
            area.SetParent(canvas, false);
            area.anchorMin = Vector2.zero;
            area.anchorMax = new Vector2(1f - panelFraction, 1f);
            area.offsetMin = area.offsetMax = Vector2.zero;
            TableArea = area;

            banner = new GameObject("Banner", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            banner.transform.SetParent(area, false);
            banner.raycastTarget = false;
            PixelUI.Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
            banner.gameObject.SetActive(false);
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
