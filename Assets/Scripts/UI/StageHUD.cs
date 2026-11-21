using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Stage HUD above the table: the stage title ("Stage n") and the score / target line. The texts are
    /// TextMeshPro objects of the scene. Optional leftovers of the old plate layout: the plate (drawn at
    /// the size of its RectTransform when enabled), the column labels and the shots text (left empty
    /// once the ShotsCounter icons replace it).
    /// Values are refreshed only when <see cref="StageManager.Changed"/> fires.
    /// The score turns green once the target is reached; the shots turn red on the last shot.
    /// </summary>
    public class StageHUD : MonoBehaviour
    {
        [Header("Objets de la scène")]
        [Tooltip("Fond de la plaque (dessiné à la taille de son RectTransform). Facultatif : vide ou désactivé = pas de plaque.")]
        [SerializeField] RawImage plate;
        [SerializeField] TMP_Text stageLabel;
        [SerializeField] TMP_Text stageValue;
        [SerializeField] TMP_Text scoreLabel;
        [SerializeField] TMP_Text scoreValue;
        [SerializeField] TMP_Text shotsLabel;
        [SerializeField] TMP_Text shotsValue;

        [Header("Couleurs des valeurs")]
        [SerializeField] Color stageColor = new Color32(0xff, 0xf0, 0xcf, 0xff);
        [SerializeField] Color scoreColor = new Color32(0xf5, 0xc8, 0x6a, 0xff);
        [Tooltip("Score une fois l'objectif atteint.")]
        [SerializeField] Color scoreReachedColor = new Color32(0x8f, 0xe3, 0x6a, 0xff);
        [SerializeField] Color shotsColor = new Color32(0xff, 0xf0, 0xcf, 0xff);
        [Tooltip("Tirs restants au dernier tir.")]
        [SerializeField] Color shotsDangerColor = new Color32(0xff, 0x6a, 0x4a, 0xff);

        const int Margin = 10;
        static readonly Color32 Plate = PixelArt.Hex("0c0a14");
        static readonly Color32[] Brass = PixelArt.Palette("3a2412", "6e4a1e", "a8742a", "d9a441", "f5d37a");

        StageManager manager;
        Texture2D texture;

        public void Init(StageManager stageManager)
        {
            manager = stageManager;
            if (plate != null) plate.raycastTarget = false; // never blocks aiming
            foreach (var text in GetComponentsInChildren<TMP_Text>(true)) text.raycastTarget = false;
            ApplyFixedTexts(GameTexts.Current);
            if (plate != null && plate.enabled) DrawPlate();
            manager.Changed += Redraw;
            Redraw();
        }

        /// <summary>Writes the column labels (STAGE, SCORE, TIRS) from <see cref="GameTexts"/>.</summary>
        public void ApplyFixedTexts(GameTexts texts, bool inEditor = false)
        {
            GameTexts.Write(stageLabel, texts.hudStageLabel, inEditor);
            GameTexts.Write(scoreLabel, texts.hudScoreLabel, inEditor);
            GameTexts.Write(shotsLabel, texts.hudShotsLabel, inEditor);
        }

        void OnDestroy()
        {
            if (manager != null) manager.Changed -= Redraw;
            if (texture != null) Destroy(texture);
        }

        void Redraw()
        {
            var texts = GameTexts.Current;
            int shots = manager.ShotsLeft;
            stageValue.text = GameTexts.Format(texts.hudStageFormat, manager.StageNumber, manager.StageCount);
            stageValue.color = stageColor;
            scoreValue.text = GameTexts.Format(texts.hudScoreFormat, manager.Score, manager.Target);
            scoreValue.color = manager.Score >= manager.Target ? scoreReachedColor : scoreColor;
            TextShadow.Fit(stageValue);
            TextShadow.Fit(scoreValue);
            if (shotsValue == null) return; // replaced by the ShotsCounter icons
            shotsValue.text = GameTexts.Format(texts.hudShotsFormat, shots, manager.MaxShots);
            shotsValue.color = shots <= 1 ? shotsDangerColor : shotsColor;
            TextShadow.Fit(shotsValue);
        }

        /// <summary>Dark plate, brass frame and two column separators with a bright stud.</summary>
        void DrawPlate()
        {
            var size = ShotUI.SizeOf(plate.rectTransform);
            int width = size.x, height = size.y;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                bool corner = (x == 0 || x == width - 1) && (y == 0 || y == height - 1);
                int e = Mathf.Min(x, y, width - 1 - x, height - 1 - y);
                Color32 c = PixelArt.WithAlpha(Plate, 235);
                if (corner) c = default;
                else if (e == 0) c = PixelUI.Outline;
                else if (e == 1) c = Brass[y == height - 2 ? 4 : 3];
                else if (e == 2) c = Brass[1];
                pixels[y * width + x] = c;
            }
            int column = (width - Margin * 2) / 3;
            for (int i = 1; i < 3; i++)
            {
                int sx = Margin + i * column;
                for (int y = 6; y < height - 6; y++) pixels[y * width + sx] = Brass[1];
                for (int dy = -1; dy <= 1; dy++) pixels[(height / 2 + dy) * width + sx] = Brass[4];
            }
            texture = PixelArt.NewTexture(width, height, "StageHUD");
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            plate.texture = texture;
        }
    }
}
