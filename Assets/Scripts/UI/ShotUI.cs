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
    /// messages over the table. The canvas scales with the screen (reference 1080×1920, match 0.5); one
    /// art pixel = <see cref="PixelUI.ArtPixel"/> canvas units. Layout and anchors live in the scene (SafeArea).
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

        [Header("Police")]
        [Tooltip("Matériau de la police sans ombre, utilisé automatiquement pour les textes trop sombres (ombre illisible).")]
        [SerializeField] Material plainTextMaterial;

        [Header("Bandeau")]
        [Tooltip("Marge horizontale totale autour du texte du bandeau (unités du canvas).")]
        [SerializeField] int bannerPaddingX = 80;
        [Tooltip("Marge verticale totale autour du texte du bandeau (unités du canvas).")]
        [SerializeField] int bannerPaddingY = 32;

        public float Power
        {
            get => gauge.Slider.value;
            set => gauge.Slider.value = Mathf.Clamp01(value);
        }

        /// <summary>Fine aim toggle state (touch-friendly replacement for holding Shift).</summary>
        public bool FineAim { get; private set; }

        /// <summary>Screen area left of the panel (over the table), for overlays such as the stage HUD.</summary>
        public RectTransform TableArea => tableArea;

        Image fineImage;
        Sprite fineOff, fineOffPressed, fineOn, fineOnPressed;
        Color shootLabelColor, fineLabelOffColor;
        Vector2 bannerBasePosition;
        Coroutine messageRoutine;

        readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

        public void Init()
        {
            EnsureEventSystem();

            // The layout (Canvas Scaler, safe area, anchors) is set in the scene; only the art is generated.
            if (panelImage != null)
            {
                panelImage.sprite = PixelUI.Panel();
                panelImage.type = Image.Type.Tiled;
                panelImage.pixelsPerUnitMultiplier = 1f / PixelUI.ArtPixel; // 1 texel = 1 art pixel
            }

            ApplyFixedTexts(GameTexts.Current);
            gauge.Init(0f); // the cue starts at rest; the power comes from pulling it back
            SetupShootButton();
            SetupFineAimButton();

            bannerBasePosition = banner.rectTransform.anchoredPosition;
            banner.raycastTarget = false;
            bannerText.raycastTarget = false;
            banner.gameObject.SetActive(false);
            TextShadow.Plain = plainTextMaterial;
            TextShadow.FitAll(this); // every text of the canvas, with the colors set in the scene
        }

        /// <summary>Writes the fixed texts (FORCE, TIRER, VISÉE FINE) from <see cref="GameTexts"/>.</summary>
        public void ApplyFixedTexts(GameTexts texts, bool inEditor = false)
        {
            GameTexts.Write(forceTitle, texts.powerTitle, inEditor);
            GameTexts.Write(shootLabel, texts.shootButton, inEditor);
            GameTexts.Write(fineLabel, texts.fineAimButton, inEditor);
        }

        public void SetCanShoot(bool canShoot)
        {
            if (shootButton.interactable == canShoot) return;
            shootButton.interactable = canShoot;
            var texts = GameTexts.Current;
            shootLabel.text = canShoot ? texts.shootButton : texts.shootButtonBusy;
            shootLabel.color = canShoot ? shootLabelColor : shootLabelDisabledColor;
            TextShadow.Fit(shootLabel);
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
            // The plate is sized around the text, in whole art pixels (bannerPadding is in canvas units).
            int w = Mathf.CeilToInt((bannerText.preferredWidth + bannerPaddingX) / PixelUI.ArtPixel);
            int h = Mathf.CeilToInt((bannerText.preferredHeight + bannerPaddingY) / PixelUI.ArtPixel);
            if (banner.texture != null) Destroy(banner.texture);
            banner.texture = PixelUI.Banner(w, h);
            var rt = banner.rectTransform;
            rt.sizeDelta = new Vector2(w, h) * PixelUI.ArtPixel;

            // Quick pixel "pop": drop in by a few art pixels.
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                rt.anchoredPosition = bannerBasePosition + new Vector2(0f, Mathf.Round(Mathf.Lerp(8f, 0f, t / 0.12f)) * PixelUI.ArtPixel);
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
                TextShadow.Fit(fineLabel);
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

        /// <summary>Size of a UI element in whole art pixels (for the generated sprites, which are stretched over the rect).</summary>
        public static Vector2Int SizeOf(RectTransform rt)
        {
            Vector2 size = rt.rect.size / PixelUI.ArtPixel;
            return new Vector2Int(Mathf.Max(8, Mathf.RoundToInt(size.x)), Mathf.Max(8, Mathf.RoundToInt(size.y)));
        }

        static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
    }
}
