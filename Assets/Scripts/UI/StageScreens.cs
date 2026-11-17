using System.Collections;
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
    /// Only the bitmap font glyphs are usable (A–Z, É, digits, % . : - ! /).
    /// </summary>
    public class StageScreens : MonoBehaviour
    {
        const int PanelW = 300, PanelH = 150, ButtonW = 168, ButtonH = 40;

        static readonly Color32 LineColor = PixelArt.Hex("d8cbb0");
        static readonly Color32 DefeatColor = PixelArt.Hex("ff6a4a");

        StageManager manager;
        ShotUI ui;
        GameObject root;
        RectTransform panel;
        RawImage title, line1, line2;
        Image buttonImage;
        Button button;
        System.Action buttonAction;

        public void Build(ShotUI shotUI, StageManager stageManager)
        {
            ui = shotUI;
            manager = stageManager;

            // Veil over the whole table area (stretched).
            var veil = new GameObject("StageScreen", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            veil.transform.SetParent(ui.TableArea, false);
            veil.color = new Color(0.01f, 0.01f, 0.03f, 0.6f);
            veil.raycastTarget = true;
            var vrt = veil.rectTransform;
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = vrt.offsetMax = Vector2.zero;
            root = veil.gameObject;

            var panelImage = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            panelImage.transform.SetParent(root.transform, false);
            panelImage.sprite = PixelUI.Panel();
            panelImage.type = Image.Type.Tiled;
            panelImage.pixelsPerUnitMultiplier = 1f;
            panel = panelImage.rectTransform;
            panel.sizeDelta = new Vector2(PanelW, PanelH);
            PixelUI.Place(panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

            title = PixelUI.Text(panel, "Title", " ", PixelUI.Gold, 3);
            line1 = PixelUI.Text(panel, "Line1", " ", LineColor, 1);
            line2 = PixelUI.Text(panel, "Line2", " ", LineColor, 1);
            PixelUI.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f));
            PixelUI.Place(line1.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f));
            PixelUI.Place(line2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f));

            buttonImage = new GameObject("Button", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            buttonImage.transform.SetParent(panel, false);
            buttonImage.rectTransform.sizeDelta = new Vector2(ButtonW, ButtonH);
            PixelUI.Place(buttonImage.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f));
            button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.transition = Selectable.Transition.SpriteSwap;
            button.onClick.AddListener(OnButton);

            root.SetActive(false);
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
        public bool IsShown => root.activeSelf;

        void OnChanged()
        {
            if (manager.CurrentState == StageManager.State.Playing && root.activeSelf) root.SetActive(false);
        }

        void OnStageWon()
        {
            if (manager.CurrentState == StageManager.State.RunWon)
                Show("BRAVO !", PixelUI.Gold, "PARTIE GAGNÉE", $"LES {manager.StageCount} STAGES SONT RÉUSSIS",
                     "RECOMMENCER", PixelUI.GoldOn, manager.RestartRun);
            else
                Show("VICTOIRE !", PixelUI.Gold, $"STAGE {manager.StageNumber} RÉUSSI",
                     $"SCORE {manager.Score}/{manager.Target} - {manager.ShotsLeft} TIR{(manager.ShotsLeft > 1 ? "S" : "")} RESTANT{(manager.ShotsLeft > 1 ? "S" : "")}",
                     "STAGE SUIVANT", PixelUI.GoldOn, manager.NextStage);
        }

        void OnStageLost()
        {
            Show("DÉFAITE", DefeatColor, $"PLUS DE TIRS AU STAGE {manager.StageNumber}",
                 $"SCORE {manager.Score}/{manager.Target}", "RECOMMENCER", PixelUI.Fire, manager.RestartRun);
        }

        void Show(string titleText, Color32 titleColor, string text1, string text2, string label,
                  PixelUI.ButtonStyle style, System.Action action)
        {
            int scale = PixelFont.Measure(titleText, 3).x <= PanelW - 24 ? 3 : 2;
            PixelUI.SetText(title, titleText, titleColor, scale);
            PixelUI.SetText(line1, text1, LineColor, 1);
            PixelUI.SetText(line2, text2, LineColor, 1);

            DestroySprites();
            buttonImage.sprite = PixelUI.Button(ButtonW, ButtonH, style, label, 2, false, false);
            button.spriteState = new SpriteState
            {
                highlightedSprite = PixelUI.Button(ButtonW, ButtonH, style, label, 2, false, true),
                pressedSprite = PixelUI.Button(ButtonW, ButtonH, style, label, 2, true, true),
                selectedSprite = buttonImage.sprite
            };
            buttonAction = action;

            ui.HideMessage(); // e.g. a scratch banner still on screen
            root.transform.SetAsLastSibling();
            root.SetActive(true);
            StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            // Same quick pixel drop-in as the message banner.
            for (float t = 0f; t < 0.12f; t += Time.deltaTime)
            {
                panel.anchoredPosition = new Vector2(0f, Mathf.Round(Mathf.Lerp(10f, 0f, t / 0.12f)));
                yield return null;
            }
            panel.anchoredPosition = Vector2.zero;
        }

        void OnButton()
        {
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            root.SetActive(false);
            buttonAction?.Invoke();
        }

        void DestroySprites()
        {
            if (buttonImage.sprite == null) return;
            var s = button.spriteState;
            foreach (var sprite in new[] { buttonImage.sprite, s.highlightedSprite, s.pressedSprite })
                if (sprite != null) { Destroy(sprite.texture); Destroy(sprite); }
        }
    }
}
