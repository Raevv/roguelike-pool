using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Shared helpers for the procedural pixel art: global pixel density, point-filtered textures,
    /// smooth color ramps (no dithering), hash/value noise, and HDR "glow" materials that
    /// push colors above 1 so the bloom picks them up.
    /// World art is authored at <see cref="PPU"/> pixels per world unit (1 px ≈ 4 mm) and the
    /// world camera renders at exactly that density (see <see cref="PixelCameraRig"/>).
    /// </summary>
    public static class PixelArt
    {
        public const int PPU = 24;
        public const float Pixel = 1f / PPU;

        /// <summary>
        /// Color along a palette ramp for t in [0, 1], smoothly interpolated (no dithering): shapes
        /// stay pixel-crisp while light and shadow gradients are clean.
        /// </summary>
        public static Color32 Ramp(Color32[] ramp, float t)
        {
            float f = Mathf.Clamp01(t) * (ramp.Length - 1);
            int i = Mathf.Min(Mathf.FloorToInt(f), ramp.Length - 2);
            return Color32.Lerp(ramp[i], ramp[i + 1], f - i);
        }

        public static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 1442695041);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / 16777216f;
            }
        }

        public static float ValueNoise(float x, float y, int seed = 0)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            float fx = x - xi, fy = y - yi;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float a = Hash(xi, yi, seed), b = Hash(xi + 1, yi, seed);
            float c = Hash(xi, yi + 1, seed), d = Hash(xi + 1, yi + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        public static Color32 Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }

        public static Color32[] Palette(params string[] hex)
        {
            var result = new Color32[hex.Length];
            for (int i = 0; i < hex.Length; i++) result[i] = Hex(hex[i]);
            return result;
        }

        public static Color32 WithAlpha(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);

        public static Texture2D NewTexture(int width, int height, string name, bool repeat = false)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = name,
                filterMode = FilterMode.Point,
                wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp
            };
        }

        /// <summary>Uploads pixels and wraps the texture in a sprite.</summary>
        public static Sprite ToSprite(Texture2D tex, Color32[] pixels, float pixelsPerUnit, Vector2 pivot,
            Vector4 border = default)
        {
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, pixelsPerUnit, 0,
                SpriteMeshType.FullRect, border);
        }

        public static Sprite ToSprite(Texture2D tex, Color32[] pixels, float pixelsPerUnit = PPU)
            => ToSprite(tex, pixels, pixelsPerUnit, new Vector2(0.5f, 0.5f));

        /// <summary>Sprites/Default material with an HDR tint: values above 1 make the sprite bloom.</summary>
        public static Material GlowMaterial(Color hdrTint)
        {
            var m = new Material(Shader.Find("Sprites/Default")) { name = "Glow" };
            m.color = hdrTint;
            return m;
        }

        static Texture2D white;

        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = NewTexture(2, 2, "PixelWhite");
                    white.SetPixels32(new Color32[] { (Color32)Color.white, Color.white, Color.white, Color.white });
                    white.Apply(false, false);
                }
                return white;
            }
        }

        /// <summary>Color shading with hue-shifted shadows (cool purple) and warm highlights.</summary>
        public static Color Shade(Color baseColor, float t)
        {
            var shadow = new Color(0.07f, 0.04f, 0.13f);
            var light = new Color(1f, 0.95f, 0.82f);
            return t < 0.5f
                ? Color.Lerp(shadow, baseColor, t * 2f)
                : Color.Lerp(baseColor, light, (t - 0.5f) * 2f * 0.55f);
        }
    }
}
