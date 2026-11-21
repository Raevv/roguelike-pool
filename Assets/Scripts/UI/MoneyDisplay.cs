using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Money under the score, centered on the table axis: the amount then a coin icon, laid out as one group
    /// (HorizontalLayoutGroup + ContentSizeFitter, so it stays centered whatever the amount). The currency
    /// will be dollars; nothing earns or spends money yet, so the scene shows the "999" preview until
    /// some system calls <see cref="SetDollars"/>. The coin is a placeholder image for now. At runtime it
    /// follows the drawn table: centered on its axis, <see cref="tableMargin"/> above its top edge.
    /// </summary>
    public class MoneyDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text amountText;
        [Tooltip("Icône de la pièce (placeholder rond jaune pour l'instant).")]
        [SerializeField] Image coinIcon;

        [Header("Position par rapport à la table")]
        [Tooltip("Zone de la table (TableAnchor) : l'argent suit la table réellement dessinée (centré sur son axe, au-dessus de son bord haut).")]
        [SerializeField] TableFitter tableFitter;
        [Tooltip("Espace entre le bas de l'argent et le haut de la table (unités du canvas ; maquette : 430 − 405 = 25).")]
        [SerializeField] float tableMargin = 25f;

        public int Dollars { get; private set; }

        void Awake()
        {
            if (amountText != null) amountText.raycastTarget = false;
            if (coinIcon != null) coinIcon.raycastTarget = false;
        }

        void LateUpdate() => FollowTable();

        static readonly Vector3[] parentCorners = new Vector3[4];

        /// <summary>
        /// Keeps the money anchored to the table as it is really drawn (any screen format, any TableFitter
        /// alignment): centered on the table axis, its bottom <see cref="tableMargin"/> above the table's top.
        /// </summary>
        void FollowTable()
        {
            if (tableFitter == null) return;
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Rect table = tableFitter.FittedScreenRect; // screen pixels (overlay canvas)
            if (table.width <= 0f) return;
            float toCanvas = parent.lossyScale.x > 0f ? 1f / parent.lossyScale.x : 1f;
            parent.GetWorldCorners(parentCorners);
            var anchor = Vector2.zero;
            var pivot = new Vector2(0.5f, 0f);
            var position = new Vector2((table.center.x - parentCorners[0].x) * toCanvas,
                                       (table.yMax - parentCorners[0].y) * toCanvas + tableMargin);
            if (rt.anchorMin != anchor || rt.anchorMax != anchor) rt.anchorMin = rt.anchorMax = anchor;
            if (rt.pivot != pivot) rt.pivot = pivot;
            if ((rt.anchoredPosition - position).sqrMagnitude > 0.01f) rt.anchoredPosition = position;
        }

        /// <summary>Shows an amount, written with <see cref="GameTexts.moneyFormat"/>.</summary>
        public void SetDollars(int dollars)
        {
            Dollars = dollars;
            if (amountText != null) amountText.text = GameTexts.Format(GameTexts.Current.moneyFormat, dollars);
        }
    }
}
