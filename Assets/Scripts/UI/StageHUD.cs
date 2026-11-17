using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Stage HUD at the top of the table area: one pixel-art plate (same style as the message banner)
    /// with three columns, STAGE n/N, SCORE s/target and TIRS left/max. Small gold labels, big values.
    /// The texture is redrawn only when <see cref="StageManager.Changed"/> fires.
    /// The score turns green once the target is reached; the shots turn red on the last shot.
    /// </summary>
    public class StageHUD : MonoBehaviour
    {
        const int ColumnWidth = 104, Margin = 10, Height = 38;
        const int Width = ColumnWidth * 3 + Margin * 2;

        static readonly Color32 Plate = PixelArt.Hex("0c0a14");
        static readonly Color32 Label = PixelArt.Hex("c9a560");
        static readonly Color32 Reached = PixelArt.Hex("8fe36a");
        static readonly Color32 Danger = PixelArt.Hex("ff6a4a");
        static readonly Color32[] Brass = PixelArt.Palette("3a2412", "6e4a1e", "a8742a", "d9a441", "f5d37a");

        StageManager manager;
        RawImage image;
        Texture2D texture;
        Color32[] pixels;

        public void Build(RectTransform tableArea, StageManager stageManager)
        {
            manager = stageManager;
            image = new GameObject("StageHUD", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            image.transform.SetParent(tableArea, false);
            image.raycastTarget = false; // never blocks aiming
            PixelUI.Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f));

            texture = PixelArt.NewTexture(Width, Height, "StageHUD");
            pixels = new Color32[Width * Height];
            image.texture = texture;
            image.rectTransform.sizeDelta = new Vector2(Width, Height); // explicit: SetNativeSize ignores the scaler PPU

            manager.Changed += Redraw;
            Redraw();
        }

        void OnDestroy()
        {
            if (manager != null) manager.Changed -= Redraw;
            if (texture != null) Destroy(texture);
        }

        void Redraw()
        {
            DrawPlate();
            int shots = manager.ShotsLeft;
            DrawColumn(0, "STAGE", $"{manager.StageNumber}/{manager.StageCount}", PixelUI.TextLight);
            DrawColumn(1, "SCORE", $"{manager.Score}/{manager.Target}", manager.Score >= manager.Target ? Reached : PixelUI.Gold);
            DrawColumn(2, "TIRS", $"{shots}/{manager.MaxShots}", shots <= 1 ? Danger : PixelUI.TextLight);
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        void DrawPlate()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                bool corner = (x == 0 || x == Width - 1) && (y == 0 || y == Height - 1);
                int e = Mathf.Min(x, y, Width - 1 - x, Height - 1 - y);
                Color32 c = PixelArt.WithAlpha(Plate, 235);
                if (corner) c = default;
                else if (e == 0) c = PixelUI.Outline;
                else if (e == 1) c = Brass[y == Height - 2 ? 4 : 3];
                else if (e == 2) c = Brass[1];
                pixels[y * Width + x] = c;
            }
            // Column separators: thin brass lines with a bright stud in the middle.
            for (int i = 1; i < 3; i++)
            {
                int sx = Margin + i * ColumnWidth;
                for (int y = 6; y < Height - 6; y++) pixels[y * Width + sx] = Brass[1];
                for (int dy = -1; dy <= 1; dy++) pixels[(Height / 2 + dy) * Width + sx] = Brass[4];
            }
        }

        void DrawColumn(int index, string label, string value, Color32 valueColor)
        {
            int left = Margin + index * ColumnWidth;
            DrawCentered(label, left, Height - 5, Label, 1);
            DrawCentered(value, left, Height - 16, valueColor, 2);
        }

        /// <summary>Draws text centered in a column, with its top row at <paramref name="top"/> and a drop shadow.</summary>
        void DrawCentered(string text, int left, int top, Color32 color, int scale)
        {
            int x = left + (ColumnWidth - PixelFont.Measure(text, scale).x) / 2;
            PixelFont.Draw(pixels, Width, Height, text, x + scale, top - scale, PixelUI.TextShadow, scale);
            PixelFont.Draw(pixels, Width, Height, text, x, top, color, scale);
        }
    }
}
