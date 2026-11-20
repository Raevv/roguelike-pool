using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Procedural pixel-art UI pieces. The shot canvas uses a constant pixel size equal to the world
    /// pixel scale and a reference PPU of 1, so one canvas unit = one art pixel, and every sprite
    /// here is authored 1 texel = 1 art pixel.
    /// </summary>
    public static class PixelUI
    {
        public static readonly Color32 Outline = PixelArt.Hex("07040b");
        public static readonly Color32 TextLight = PixelArt.Hex("fff0cf");
        public static readonly Color32 TextShadow = PixelArt.Hex("1a0d14");
        public static readonly Color32 Gold = PixelArt.Hex("f5c86a");
        static readonly Color32[] Brass = PixelArt.Palette("3a2412", "6e4a1e", "a8742a", "d9a441", "f5d37a");

        public struct ButtonStyle
        {
            public Color32[] body;  // dark → light (4 colors)
            public Color32 text;
            public Color32 textShadow;
        }

        public static readonly ButtonStyle Fire = new ButtonStyle
        {
            body = PixelArt.Palette("5a1018", "a3221f", "e0452b", "ff8a45"),
            text = PixelArt.Hex("fff0cf"), textShadow = PixelArt.Hex("4a0a12")
        };
        public static readonly ButtonStyle Stone = new ButtonStyle
        {
            body = PixelArt.Palette("12111c", "1f1d2e", "2e2b44", "48446a"),
            text = PixelArt.Hex("d8cbb0"), textShadow = PixelArt.Hex("07040b")
        };
        public static readonly ButtonStyle GoldOn = new ButtonStyle
        {
            body = PixelArt.Palette("5a3515", "a8742a", "e0a83e", "ffe08a"),
            text = PixelArt.Hex("2a1408"), textShadow = PixelArt.Hex("ffe9b0")
        };
        public static readonly ButtonStyle Disabled = new ButtonStyle
        {
            body = PixelArt.Palette("100e15", "1a1720", "25212d", "322d3c"),
            text = PixelArt.Hex("5e5568"), textShadow = PixelArt.Hex("07040b")
        };

        /// <summary>Beveled button with a brass frame. Pass a null label: button texts are TextMeshPro children.</summary>
        public static Sprite Button(int w, int h, ButtonStyle style, string label, int textScale, bool pressed, bool hover)
        {
            var px = new Color32[w * h];
            int press = pressed ? 2 : 0;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                bool corner = (x == 0 || x == w - 1) && (y == 0 || y == h - 1);
                if (corner) continue;
                Color32 c;
                bool outline = x == 0 || y == 0 || x == w - 1 || y == h - 1;
                bool frame = x == 1 || y == 1 || x == w - 2 || y == h - 2;
                if (outline) c = Outline;
                else if (frame)
                {
                    bool lit = x == 1 || y == h - 2;
                    c = Brass[lit ? 4 : 2];
                    if ((x == 1 || x == w - 2) && (y == 1 || y == h - 2)) c = Brass[1];
                }
                else
                {
                    // Body: smooth vertical gradient; bottom lip gives depth (moves to the top when pressed).
                    int top = h - 3, bottom = 2;
                    if (!pressed && y <= bottom + 2) c = style.body[0];
                    else if (pressed && y >= top - 1) c = style.body[0];
                    else
                    {
                        float t = (y - bottom) / (float)(top - bottom);
                        t = t * 0.75f + (hover ? 0.18f : 0.05f) - (pressed ? 0.1f : 0f);
                        c = PixelArt.Ramp(new[] { style.body[1], style.body[2], style.body[3] }, t);
                        if (y == top - press - (pressed ? 1 : 0)) c = style.body[3]; // inner top highlight
                    }
                }
                px[y * w + x] = c;
            }

            if (!string.IsNullOrEmpty(label))
            {
                var size = PixelFont.Measure(label, textScale);
                int lx = (w - size.x) / 2;
                int ly = h - 1 - (h - size.y) / 2 + 1 - press;
                PixelFont.Draw(px, w, h, label, lx + textScale / 2, ly - textScale, style.textShadow, textScale);
                PixelFont.Draw(px, w, h, label, lx, ly, style.text, textScale);
            }

            var tex = PixelArt.NewTexture(w, h, "Button_" + label);
            return PixelArt.ToSprite(tex, px, 1f);
        }

        /// <summary>Tiled 32x32 slate panel with an ornate left border (gold trim, rivets).</summary>
        public static Sprite Panel()
        {
            const int S = 32, B = 8;
            var px = new Color32[S * S];
            var slate = PixelArt.Palette("0b0a12", "100f19", "151421", "1b1a2a");
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                const float v = 0.45f; // flat slate: clean, and seamless when tiled
                Color32 c = PixelArt.Ramp(slate, v);
                int e = Mathf.Min(x, y, S - 1 - x, S - 1 - y); // distance to any edge (tiled borders)
                if (e == 0) c = Outline;
                else if (e == 1) c = Brass[3];
                else if (e == 2) c = Brass[1];
                else if (e == 3) c = Outline;
                else if (e == 4) c = slate[0];
                if (e == 6 && (x == 16 || y == 16)) c = Brass[4]; // rivets along the border
                px[y * S + x] = c;
            }
            var tex = PixelArt.NewTexture(S, S, "Panel");
            return PixelArt.ToSprite(tex, px, 1f, new Vector2(0.5f, 0.5f), new Vector4(B, B, B, B));
        }

        /// <summary>Dark banner plate with gold trims and diamond end caps (the text is a TextMeshPro child).</summary>
        public static Texture2D Banner(int w, int h)
        {
            var px = new Color32[w * h];
            var plate = PixelArt.Hex("0c0a14");
            for (int y = 0; y < h; y++)
            for (int x = 6; x < w - 6; x++)
            {
                Color32 c = PixelArt.WithAlpha(plate, 235);
                if (y == 0 || y == h - 1) c = Outline;
                else if (y == 1 || y == h - 2) c = Brass[3];
                else if (y == 2 || y == h - 3) c = Brass[1];
                px[y * w + x] = c;
            }
            // Diamond end caps.
            int cy = h / 2;
            for (int i = 0; i < 7; i++)
            for (int dy = -i; dy <= i; dy++)
            {
                int yy = cy + dy;
                if (yy < 0 || yy >= h) continue;
                Color32 c = i == 0 || dy == -i || dy == i ? Outline : Brass[Mathf.Abs(dy) < 2 ? 4 : 2];
                px[yy * w + i] = c;
                px[yy * w + (w - 1 - i)] = c;
            }
            var tex = PixelArt.NewTexture(w, h, "Banner");
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
        }
    }
}
