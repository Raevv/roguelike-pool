using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Adapts the right column to tall screens. Lives on BonusTabs. The tabs' top margin goes from
    /// <see cref="topMargin"/> on a 1080×1920 screen to <see cref="topMarginTall"/> on a 1080×2400 one
    /// (in between: interpolated; beyond: clamped), and the power bar (its percentage label included) starts
    /// that same distance below the tabs (or <see cref="gapBelowTabs"/>), so the bar gets shorter on tall screens. The bar's bottom (above
    /// TIRER) does not move. Canvas units (Scale With Screen Size, reference 1080×1920, match 0.5).
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class RightColumnLayout : MonoBehaviour
    {
        [Tooltip("Barre de puissance dont le haut suit le bas des onglets.")]
        [SerializeField] RectTransform powerBar;
        [Tooltip("Marge au-dessus des onglets sur un écran 1080×1920 (maquette : 26).")]
        [SerializeField] float topMargin = 26f;
        [Tooltip("Marge au-dessus des onglets sur un écran 1080×2400 (un peu moins qu'un onglet de 137).")]
        [SerializeField] float topMarginTall = 110f;
        [Tooltip("L'espace entre le bas des onglets et le haut de la barre (pourcentage compris) est égal à la marge au-dessus des onglets.")]
        [SerializeField] bool gapEqualsTopMargin = true;
        [Tooltip("Espace entre le bas des onglets et le haut de la barre (pourcentage compris) quand il n'est pas égal à la marge du haut.")]
        [SerializeField] float gapBelowTabs = 90f;
        [Tooltip("Texte du pourcentage posé au-dessus de la barre : l'espace est mesuré jusqu'à son haut.")]
        [SerializeField] RectTransform percentLabel;

        // Canvas heights of the two reference screens with match 0.5: 1920, and 2400 / sqrt(2400 / 1920).
        const float ShortHeight = 1920f;
        static readonly float TallHeight = 2400f / Mathf.Sqrt(2400f / 1920f);

        static readonly Vector3[] corners = new Vector3[4];
        RectTransform rt;
        Canvas canvas;

        void LateUpdate()
        {
            if (rt == null) rt = (RectTransform)transform;
            if (canvas == null) canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            var canvasRt = (RectTransform)canvas.rootCanvas.transform;
            float t = Mathf.InverseLerp(ShortHeight, TallHeight, canvasRt.rect.height);
            float top = Mathf.Lerp(topMargin, topMarginTall, t);

            var p = rt.anchoredPosition;
            if (Mathf.Abs(p.y + top) > 0.01f) rt.anchoredPosition = new Vector2(p.x, -top);

            if (powerBar == null || !(powerBar.parent is RectTransform parent)) return;
            // Tabs' bottom edge in the bar's parent space, minus the gap = the bar's top edge.
            rt.GetWorldCorners(corners);
            float bottomLocal = parent.InverseTransformPoint(corners[0]).y;
            float gap = gapEqualsTopMargin ? top : gapBelowTabs;
            // The label hangs above the bar (pivot at its bottom): its top is this far above the bar's top.
            float label = percentLabel != null && percentLabel.gameObject.activeInHierarchy
                ? percentLabel.anchoredPosition.y + percentLabel.rect.height * (1f - percentLabel.pivot.y) : 0f;
            float offsetTop = bottomLocal - gap - label - parent.rect.yMax * powerBar.anchorMax.y - parent.rect.yMin * (1f - powerBar.anchorMax.y);
            var max = powerBar.offsetMax;
            if (Mathf.Abs(max.y - offsetTop) > 0.01f) powerBar.offsetMax = new Vector2(max.x, offsetTop);
        }
    }
}
