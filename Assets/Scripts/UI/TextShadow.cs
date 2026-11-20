using TMPro;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// The font material draws a dark drop shadow (Underlay). On a dark text (e.g. black on a gold
    /// button) that shadow blurs the letters, so such texts switch to the shadowless material preset.
    /// Called whenever a text color may have changed.
    /// </summary>
    public static class TextShadow
    {
        /// <summary>Shadowless material preset of the font (Assets/Art/Font/VeniceClassic SDF NoShadow), set by ShotUI.</summary>
        public static Material Plain;

        /// <summary>Minimum brightness gap between the text and its shadow for the shadow to stay.</summary>
        public const float MinContrast = 0.25f;

        static readonly int UnderlayColor = Shader.PropertyToID("_UnderlayColor");

        /// <summary>Uses the shadowless material if the text color is too close to the shadow color, the font's own material otherwise.</summary>
        public static void Fit(TMP_Text text)
        {
            if (text == null || text.font == null) return;
            var shadowed = text.font.material;
            if (Plain == null || shadowed == null || !shadowed.HasProperty(UnderlayColor)) return;
            float gap = Mathf.Abs(Luminance(text.color) - Luminance(shadowed.GetColor(UnderlayColor)));
            var wanted = gap < MinContrast ? Plain : shadowed;
            if (text.fontSharedMaterial != wanted) text.fontSharedMaterial = wanted;
        }

        public static void FitAll(Component root)
        {
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) Fit(text);
        }

        static float Luminance(Color c) => 0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b;
    }
}
