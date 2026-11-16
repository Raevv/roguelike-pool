using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Pixel-art ball sprites (13x13 px, odd so the number sits on a center pixel): clean 3-tone sphere
    /// shading with cool hue-shifted shadows, a pure-white specular that catches the bloom, a colored
    /// edge (dark on the shadow side, light rim on the lit side), stripe band, number disc with a 3x5
    /// digit. Plus a soft drop shadow sprite.
    /// Sprites are sized so one texel = one screen art pixel once the ball transform scale is applied.
    /// </summary>
    public static class BallArt
    {
        public const int Size = 13;

        static readonly Color Ivory = PixelArt.Hex("e6dcc4");
        static readonly Color NumberInk = PixelArt.Hex("1a1020");

        /// <summary>Base colors indexed by number 0-8 (stripes reuse 1-7). Kept below the bloom threshold.</summary>
        static readonly Color[] Colors =
        {
            PixelArt.Hex("e6dcc4"), // cue ball (ivory)
            PixelArt.Hex("f0b11e"), // 1 yellow
            PixelArt.Hex("2c56d1"), // 2 blue
            PixelArt.Hex("d92a32"), // 3 red
            PixelArt.Hex("7d34bd"), // 4 purple
            PixelArt.Hex("f06a1c"), // 5 orange
            PixelArt.Hex("1f9e4c"), // 6 green
            PixelArt.Hex("962c22"), // 7 maroon
            PixelArt.Hex("23222e"), // 8 black
        };

        static readonly Dictionary<int, Sprite> cache = new Dictionary<int, Sprite>();
        static Sprite shadow;

        /// <summary>Sprite PPU so that the sprite, scaled by the ball diameter, maps 1 texel → 1 art pixel.</summary>
        public static float SpritePPU(float diameter) => PixelArt.PPU * diameter;

        /// <summary>
        /// Domain reload is disabled on Play (Enter Play Mode Options), so statics survive between
        /// sessions while the sprites they point to are destroyed: clear the cache on every Play.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            cache.Clear();
            shadow = null;
        }

        public static Sprite Get(int number, float diameter)
        {
            // Unity null check too: a cached sprite may have been destroyed.
            if (cache.TryGetValue(number, out var s) && s != null) return s;
            s = Create(number, diameter);
            cache[number] = s;
            return s;
        }

        static Sprite Create(int number, float diameter)
        {
            var px = new Color32[Size * Size];
            Color baseColor = number <= 8 ? Colors[number] : Colors[number - 8];
            bool stripe = number >= 9;
            const float r = Size / 2f;               // 6.5
            Vector3 light = new Vector3(-0.5f, 0.6f, 0.62f).normalized;

            for (int y = 0; y < Size; y++)
            for (int x = 0; x < Size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d > r - 0.15f) continue;

                float nx = dx / r, ny = dy / r;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - nx * nx - ny * ny));
                float diffuse = Mathf.Max(0f, nx * light.x + ny * light.y + nz * light.z);

                Color surface = baseColor;
                if (stripe && Mathf.Abs(dy) > 3.2f) surface = Ivory;
                bool disc = number > 0 && d < 3.3f;
                if (disc) surface = Ivory;

                // Clean 3-tone cel shading (no dithering), kept bright so the color reads at a glance.
                float tone = diffuse > 0.82f ? 0.66f : diffuse > 0.45f ? 0.5f : 0.36f;
                if (disc) tone = Mathf.Max(tone, 0.5f);
                Color c = PixelArt.Shade(surface, tone);

                // Edge: colored dark line on the shadow side only, light rim on the lit side.
                if (d > r - 1.15f)
                    c = nx - ny > -0.35f ? PixelArt.Shade(surface, 0.24f) : PixelArt.Shade(surface, 0.58f);

                // Specular: 2 px pure white (HDR-ish at 1.0 → bloom) + soft neighbor.
                float sx = dx + 2.2f, sy = dy - 2.2f;
                float spec = sx * sx + sy * sy;
                if (spec < 0.8f) c = Color.white;
                else if (spec < 2.2f && !disc) c = Color.Lerp(c, Color.white, 0.5f);

                px[y * Size + x] = c;
            }

            if (number > 0) PixelFont.DrawBallNumber(px, Size, Size, number, Size / 2, Size / 2, NumberInk);

            var tex = PixelArt.NewTexture(Size, Size, "Ball" + number);
            return PixelArt.ToSprite(tex, px, SpritePPU(diameter));
        }

        /// <summary>Drop shadow: soft-edged dark disc (contact occlusion), barely larger than the ball.</summary>
        public static Sprite Shadow(float diameter)
        {
            if (shadow != null) return shadow;
            const int S = 15;
            var px = new Color32[S * S];
            var dark = PixelArt.Hex("040208");
            const float r = S / 2f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((7.3f - d) / 1.8f) * 115f;
                px[y * S + x] = PixelArt.WithAlpha(dark, (byte)a);
            }
            var tex = PixelArt.NewTexture(S, S, "BallShadow");
            shadow = PixelArt.ToSprite(tex, px, SpritePPU(diameter));
            return shadow;
        }
    }
}
