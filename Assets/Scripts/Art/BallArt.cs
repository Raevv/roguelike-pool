using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Procedural art that goes with the balls. The balls themselves are the user's sprites
    /// (Assets/Art/Sprites/Balls, on the Ball / CueBall prefabs); only the soft drop shadow is generated here.
    /// </summary>
    public static class BallArt
    {
        static Sprite shadow;

        /// <summary>Sprite PPU so that the sprite, scaled by the ball diameter, maps 1 texel → 1 art pixel.</summary>
        public static float SpritePPU(float diameter) => PixelArt.PPU * diameter;

        /// <summary>
        /// Domain reload is disabled on Play (Enter Play Mode Options), so statics survive between
        /// sessions while the sprite they point to is destroyed: clear the cache on every Play.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => shadow = null;

        /// <summary>Drop shadow: soft-edged dark disc (contact occlusion), barely larger than the ball.</summary>
        public static Sprite Shadow(float diameter)
        {
            if (shadow != null) return shadow;
            const int S = 23;
            var px = new Color32[S * S];
            var dark = PixelArt.Hex("040208");
            const float r = S / 2f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01((11.3f - d) / 2.7f) * 115f;
                px[y * S + x] = PixelArt.WithAlpha(dark, (byte)a);
            }
            var tex = PixelArt.NewTexture(S, S, "BallShadow");
            shadow = PixelArt.ToSprite(tex, px, SpritePPU(diameter));
            return shadow;
        }
    }
}
