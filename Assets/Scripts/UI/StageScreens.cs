using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// End-of-stage screens over the table area: a dark veil (it also blocks aiming, since
    /// <see cref="ShotUI.IsOverUI"/> sees it) and a slate panel with a title, two lines and one button.
    /// - Stage won: "VICTOIRE !" + STAGE SUIVANT → <see cref="StageManager.NextStage"/>.
    /// - Last stage won: "BRAVO !" / PARTIE GAGNÉE + RECOMMENCER → <see cref="StageManager.RestartRun"/>.
    /// - Stage lost: "DÉFAITE" + RECOMMENCER → <see cref="StageManager.RestartRun"/>.
    /// The hierarchy and the texts are scene objects; only the panel and button sprites are generated.
    /// </summary>
    public class StageScreens : MonoBehaviour
    {
        [Header("Objets de la scène")]
        [Tooltip("Voile sombre plein écran (bloque la visée), parent du panneau. Caché pendant le jeu.")]
        [SerializeField] GameObject veil;
        [SerializeField] Image panelImage;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text line1;
        [SerializeField] TMP_Text line2;
        [SerializeField] Button button;
        [SerializeField] TMP_Text buttonLabel;

        [Header("Couleurs")]
        [SerializeField] Color victoryTitleColor = new Color32(0xf5, 0xc8, 0x6a, 0xff);
        [SerializeField] Color defeatTitleColor = new Color32(0xff, 0x6a, 0x4a, 0xff);
        [Tooltip("Libellé sur le bouton doré (victoire).")]
        [SerializeField] Color goldButtonLabelColor = new Color32(0x2a, 0x14, 0x08, 0xff);
        [Tooltip("Libellé sur le bouton rouge (défaite).")]
        [SerializeField] Color redButtonLabelColor = new Color32(0xff, 0xf0, 0xcf, 0xff);

        StageManager manager;
        ShotUI ui;
        Image buttonImage;
        Vector2 panelBasePosition;
        System.Action buttonAction;
        bool spritesGenerated;

        public void Init(ShotUI shotUI, StageManager stageManager)
        {
            ui = shotUI;
            manager = stageManager;

            panelImage.sprite = PixelUI.Panel();
            panelImage.type = Image.Type.Tiled;
            panelImage.pixelsPerUnitMultiplier = 1f;
            panelBasePosition = panelImage.rectTransform.anchoredPosition;

            buttonImage = (Image)button.targetGraphic;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.transition = Selectable.Transition.SpriteSwap;
            button.onClick.AddListener(OnButton);
            foreach (var text in GetComponentsInChildren<TMP_Text>(true)) text.raycastTarget = false;

            veil.SetActive(false);
            manager.StageWon += OnStageWon;
            manager.StageLost += OnStageLost;
            manager.Changed += OnChanged;
        }

        void OnDestroy()
        {
            if (manager == null) return;
            manager.StageWon -= OnStageWon;
            manager.StageLost -= OnStageLost;
            manager.Changed -= OnChanged;
        }

        /// <summary>The screen is visible while the stage is over (also hides it if the stage is restarted from the Inspector).</summary>
        public bool IsShown => veil.activeSelf;

        void OnChanged()
        {
            if (manager.CurrentState == StageManager.State.Playing && veil.activeSelf) veil.SetActive(false);
        }

        void OnStageWon()
        {
            var texts = GameTexts.Current;
            if (manager.CurrentState == StageManager.State.RunWon)
                Show(GameTexts.Pick(texts.runWonTitles), victoryTitleColor, "PARTIE GAGNÉE", $"LES {manager.StageCount} STAGES SONT RÉUSSIS",
                     texts.newRunButton, PixelUI.GoldOn, goldButtonLabelColor, manager.RestartRun);
            else
                Show(GameTexts.Pick(texts.victoryTitles), victoryTitleColor, $"STAGE {manager.StageNumber} RÉUSSI",
                     $"SCORE {manager.Score}/{manager.Target} - {manager.ShotsLeft} TIR{(manager.ShotsLeft > 1 ? "S" : "")} RESTANT{(manager.ShotsLeft > 1 ? "S" : "")}",
                     texts.nextStageButton, PixelUI.GoldOn, goldButtonLabelColor, manager.NextStage);
        }

        void OnStageLost()
        {
            var texts = GameTexts.Current;
            Show(GameTexts.Pick(texts.defeatTitles), defeatTitleColor, $"PLUS DE TIRS AU STAGE {manager.StageNumber}",
                 $"SCORE {manager.Score}/{manager.Target}", texts.retryButton, PixelUI.Fire, redButtonLabelColor, manager.RestartRun);
        }

        void Show(string titleText, Color titleColor, string text1, string text2, string label,
                  PixelUI.ButtonStyle style, Color labelColor, System.Action action)
        {
            title.text = titleText;
            title.color = titleColor;
            line1.text = text1;
            line2.text = text2;
            buttonLabel.text = label;
            buttonLabel.color = labelColor;

            DestroySprites();
            var size = ShotUI.SizeOf(buttonImage.rectTransform);
            buttonImage.sprite = PixelUI.Button(size.x, size.y, style, null, 2, false, false);
            button.spriteState = new SpriteState
            {
                highlightedSprite = PixelUI.Button(size.x, size.y, style, null, 2, false, true),
                pressedSprite = PixelUI.Button(size.x, size.y, style, null, 2, true, true),
                selectedSprite = buttonImage.sprite
            };
            buttonAction = action;

            ui.HideMessage(); // e.g. a scratch banner still on screen
            transform.SetAsLastSibling();
            veil.SetActive(true);
            StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            // Same quick pixel drop-in as the message banner.
            var panel = panelImage.rectTransform;
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                panel.anchoredPosition = panelBasePosition + new Vector2(0f, Mathf.Round(Mathf.Lerp(10f, 0f, t / 0.12f)));
                yield return null;
            }
            panel.anchoredPosition = panelBasePosition;
        }

        void OnButton()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            veil.SetActive(false);
            buttonAction?.Invoke();
        }

        void DestroySprites()
        {
            if (!spritesGenerated) { spritesGenerated = true; return; } // never destroy a sprite asset set in the scene
            var s = button.spriteState;
            foreach (var sprite in new[] { buttonImage.sprite, s.highlightedSprite, s.pressedSprite })
                if (sprite != null) { Destroy(sprite.texture); Destroy(sprite); }
        }
    }
}
