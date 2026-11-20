using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
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
    /// The whole hierarchy lives in the scene (Tools > Roguelike Pool > 8 Build UI); every text is a
    /// TextMeshPro object referenced here. Only the pixel-art backgrounds are generated at runtime.
    /// </summary>
    public class ShotUI : MonoBehaviour
    {
        public event Action ShootClicked;

        [Header("Objets de la scène")]
        [SerializeField] CanvasScaler scaler;
        [SerializeField] GraphicRaycaster raycaster;
        [Tooltip("Panneau de droite (jauge, visée fine, tir).")]
        [SerializeField] Image panelImage;
        [Tooltip("Titre FORCE en haut du panneau.")]
        [SerializeField] TMP_Text forceTitle;
        [SerializeField] PowerGauge gauge;
        [SerializeField] Button shootButton;
        [SerializeField] TMP_Text shootLabel;
        [SerializeField] Button fineButton;
        [SerializeField] TMP_Text fineLabel;
        [Tooltip("Zone de l'écran à gauche du panneau (au-dessus de la table).")]
        [SerializeField] RectTransform tableArea;
        [Tooltip("Fond du bandeau de message ; sa taille suit le texte.")]
        [SerializeField] RawImage banner;
        [SerializeField] TMP_Text bannerText;

        [Header("Couleurs des libellés")]
        [Tooltip("Couleur du libellé TIRER quand le tir est impossible (boules en mouvement).")]
        [SerializeField] Color shootLabelDisabledColor = new Color32(0x5e, 0x55, 0x68, 0xff);
        [Tooltip("Couleur du libellé VISÉE FINE quand elle est activée (bouton doré).")]
        [SerializeField] Color fineLabelOnColor = new Color32(0x2a, 0x14, 0x08, 0xff);

        [Header("Bandeau")]
        [Tooltip("Marge horizontale totale autour du texte du bandeau (pixels art).")]
        [SerializeField] int bannerPaddingX = 40;
        [Tooltip("Marge verticale totale autour du texte du bandeau (pixels art).")]
        [SerializeField] int bannerPaddingY = 16;

        public float Power
        {
            get => gauge.Slider.value;
            set => gauge.Slider.value = Mathf.Clamp01(value);
        }

        /// <summary>Fine aim toggle state (touch-friendly replacement for holding Shift).</summary>
        public bool FineAim { get; private set; }

        /// <summary>Screen area left of the panel (over the table), for overlays such as the stage HUD.</summary>
        public RectTransform TableArea => tableArea;

        PixelCameraRig rig;
        Image fineImage;
        Sprite fineOff, fineOffPressed, fineOn, fineOnPressed;
        Color shootLabelColor, fineLabelOffColor;
        Vector2 bannerBasePosition;
        Coroutine messageRoutine;
        int lastScale = -1, lastScreenHeight = -1;
        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public void Init(float panelFraction, PixelCameraRig pixelRig)
        {
            rig = pixelRig;
            EnsureEventSystem();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.referencePixelsPerUnit = 1f; // sprites are 1 texel per unit

            // The panel and the table area follow the camera rig's panel fraction.
            var panel = panelImage.rectTransform;
            panel.anchorMin = new Vector2(1f - panelFraction, 0f);
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            tableArea.anchorMin = Vector2.zero;
            tableArea.anchorMax = new Vector2(1f - panelFraction, 1f);
            tableArea.offsetMin = tableArea.offsetMax = Vector2.zero;

            panelImage.sprite = PixelUI.Panel();
            panelImage.type = Image.Type.Tiled;
            panelImage.pixelsPerUnitMultiplier = 1f;

            ApplyFixedTexts(GameTexts.Current);
            gauge.Init(0.5f);
            SetupShootButton();
            SetupFineAimButton();

            bannerBasePosition = banner.rectTransform.anchoredPosition;
            banner.raycastTarget = false;
            bannerText.raycastTarget = false;
            banner.gameObject.SetActive(false);
            ApplyScale();
        }

        /// <summary>Writes the fixed texts (FORCE, TIRER, VISÉE FINE) from <see cref="GameTexts"/>.</summary>
        public void ApplyFixedTexts(GameTexts texts, bool inEditor = false)
        {
            GameTexts.Write(forceTitle, texts.powerTitle, inEditor);
            GameTexts.Write(shootLabel, texts.shootButton, inEditor);
            GameTexts.Write(fineLabel, texts.fineAimButton, inEditor);
        }

        void Update() => ApplyScale();

        /// <summary>Follows the world pixel scale and resizes the gauge to the available height.</summary>
        void ApplyScale()
        {
            if (scaler == null) return;
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
            if (shootButton.interactable == canShoot) return;
            shootButton.interactable = canShoot;
            var texts = GameTexts.Current;
            shootLabel.text = canShoot ? texts.shootButton : texts.shootButtonBusy;
            shootLabel.color = canShoot ? shootLabelColor : shootLabelDisabledColor;
        }

        public void ShowMessage(string message, float duration)
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = StartCoroutine(MessageRoutine(message, duration));
        }

        /// <summary>Hides the message banner right away (if any).</summary>
        public void HideMessage()
        {
            if (messageRoutine != null) StopCoroutine(messageRoutine);
            messageRoutine = null;
            banner.gameObject.SetActive(false);
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
            banner.gameObject.SetActive(true);
            bannerText.text = message;
            bannerText.ForceMeshUpdate();
            // The plate is sized around the text, in whole art pixels.
            int w = Mathf.CeilToInt(bannerText.preferredWidth) + bannerPaddingX;
            int h = Mathf.CeilToInt(bannerText.preferredHeight) + bannerPaddingY;
            if (banner.texture != null) Destroy(banner.texture);
            banner.texture = PixelUI.Banner(w, h);
            var rt = banner.rectTransform;
            rt.sizeDelta = new Vector2(w, h);

            // Quick pixel "pop": drop in by a few art pixels.
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                rt.anchoredPosition = bannerBasePosition + new Vector2(0f, Mathf.Round(Mathf.Lerp(8f, 0f, t / 0.12f)));
                yield return null;
            }
            rt.anchoredPosition = bannerBasePosition;
            yield return new WaitForSeconds(duration);
            banner.gameObject.SetActive(false);
            messageRoutine = null;
        }

        void SetupShootButton()
        {
            var image = (Image)shootButton.targetGraphic;
            var size = SizeOf(image.rectTransform);
            image.sprite = PixelUI.Button(size.x, size.y, PixelUI.Fire, null, 2, false, false);
            shootButton.navigation = new Navigation { mode = Navigation.Mode.None };
            shootButton.transition = Selectable.Transition.SpriteSwap;
            shootButton.spriteState = new SpriteState
            {
                highlightedSprite = PixelUI.Button(size.x, size.y, PixelUI.Fire, null, 2, false, true),
                pressedSprite = PixelUI.Button(size.x, size.y, PixelUI.Fire, null, 2, true, true),
                selectedSprite = image.sprite,
                disabledSprite = PixelUI.Button(size.x, size.y, PixelUI.Disabled, null, 2, false, false)
            };
            shootLabel.raycastTarget = false;
            shootLabelColor = shootLabel.color;
            shootButton.onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                ShootClicked?.Invoke();
            });
        }

        void SetupFineAimButton()
        {
            fineImage = (Image)fineButton.targetGraphic;
            var size = SizeOf(fineImage.rectTransform);
            fineOff = PixelUI.Button(size.x, size.y, PixelUI.Stone, null, 2, false, false);
            fineOffPressed = PixelUI.Button(size.x, size.y, PixelUI.Stone, null, 2, true, true);
            fineOn = PixelUI.Button(size.x, size.y, PixelUI.GoldOn, null, 2, false, false);
            fineOnPressed = PixelUI.Button(size.x, size.y, PixelUI.GoldOn, null, 2, true, true);
            fineImage.sprite = fineOff;
            fineLabel.raycastTarget = false;
            fineLabelOffColor = fineLabel.color;

            fineButton.navigation = new Navigation { mode = Navigation.Mode.None };
            fineButton.transition = Selectable.Transition.SpriteSwap;
            fineButton.spriteState = new SpriteState { pressedSprite = fineOffPressed, highlightedSprite = fineOff, selectedSprite = fineOff };
            fineButton.onClick.AddListener(() =>
            {
                FineAim = !FineAim;
                fineImage.sprite = FineAim ? fineOn : fineOff;
                fineLabel.color = FineAim ? fineLabelOnColor : fineLabelOffColor;
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

        /// <summary>Size of a fixed-size UI element in whole art pixels (for the generated sprites).</summary>
        public static Vector2Int SizeOf(RectTransform rt)
        {
            return new Vector2Int(Mathf.Max(8, Mathf.RoundToInt(rt.sizeDelta.x)), Mathf.Max(8, Mathf.RoundToInt(rt.sizeDelta.y)));
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
