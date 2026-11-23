using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Tiny bitmap fonts drawn straight into pixel buffers: a 5x7 font for the UI (uppercase,
    /// digits, É, a few symbols including /).
    /// Buffers are bottom-up (Unity texture layout); positions given here are the glyph's top-left.
    /// </summary>
    public static class PixelFont
    {
        public const int GlyphWidth = 5;
        public const int GlyphHeight = 7;
        public const int Spacing = 1;

        static readonly Dictionary<char, string[]> Glyphs = new Dictionary<char, string[]>
        {
            ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
            ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
            ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
            ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
            ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
            ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
            ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
            ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
            ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
            ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
            ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
            ['N'] = new[] { "#...#", "##..#", "#.#.#", "#.#.#", "#..##", "#...#", "#...#" },
            ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
            ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
            ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
            ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
            ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
            ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
            ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
            ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" },
            ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
            ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
            ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
            ['É'] = new[] { "...#.", "..#..", "#####", "#....", "####.", "#....", "#####" },
            ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
            ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
            ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
            ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
            ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
            ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
            ['6'] = new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." },
            ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
            ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
            ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." },
            ['%'] = new[] { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" },
            ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "..#.." },
            [':'] = new[] { ".....", "..#..", ".....", ".....", ".....", "..#..", "....." },
            ['-'] = new[] { ".....", ".....", ".....", ".###.", ".....", ".....", "....." },
            ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
            ['/'] = new[] { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." },
            [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        };

        public static Vector2Int Measure(string text, int scale = 1, int lineGap = 3)
        {
            var lines = text.ToUpperInvariant().Split('\n');
            int maxChars = 0;
            foreach (var line in lines) maxChars = Mathf.Max(maxChars, line.Length);
            int w = maxChars * (GlyphWidth + Spacing) - Spacing;
            int h = lines.Length * (GlyphHeight + lineGap) - lineGap;
            return new Vector2Int(Mathf.Max(0, w) * scale, h * scale);
        }

        /// <summary>Draws text (each line centered on the widest one) with its top-left at (x, top).</summary>
        public static void Draw(Color32[] px, int width, int height, string text, int x, int top, Color32 color,
            int scale = 1, int lineGap = 3)
        {
            var lines = text.ToUpperInvariant().Split('\n');
            int blockWidth = Measure(text, 1, lineGap).x;
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l];
                int lineWidth = line.Length * (GlyphWidth + Spacing) - Spacing;
                int cx = x + (blockWidth - lineWidth) / 2 * scale;
                int cy = top - l * (GlyphHeight + lineGap) * scale;
                foreach (char ch in line)
                {
                    if (Glyphs.TryGetValue(ch, out var rows)) DrawRows(px, width, height, rows, cx, cy, color, scale);
                    cx += (GlyphWidth + Spacing) * scale;
                }
            }
        }

        /// <summary>Renders text into a new texture with a 1-px (scaled) drop shadow, padded by <paramref name="pad"/>.</summary>
        public static Texture2D Render(string text, Color32 color, Color32 shadow, int scale = 1, int pad = 0)
        {
            var size = Measure(text, scale);
            int w = size.x + scale + pad * 2, h = size.y + scale + pad * 2;
            var px = new Color32[w * h];
            Draw(px, w, h, text, pad + scale, h - 1 - pad - scale, shadow, scale);
            Draw(px, w, h, text, pad, h - 1 - pad, color, scale);
            var tex = PixelArt.NewTexture(w, h, "Text_" + text);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            return tex;
        }

        static void DrawRows(Color32[] px, int width, int height, string[] rows, int x, int top, Color32 color, int scale)
        {
            for (int r = 0; r < rows.Length; r++)
            for (int c = 0; c < rows[r].Length; c++)
            {
                if (rows[r][c] != '#') continue;
                for (int sy = 0; sy < scale; sy++)
                for (int sx = 0; sx < scale; sx++)
                {
                    int px0 = x + c * scale + sx;
                    int py0 = top - r * scale - sy;
                    if (px0 < 0 || py0 < 0 || px0 >= width || py0 >= height) continue;
                    px[py0 * width + px0] = color;
                }
            }
        }
    }
}
