using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// Money under the score, centered on the table axis: the amount then a coin icon, laid out as one group
    /// (HorizontalLayoutGroup + ContentSizeFitter, so it stays centered whatever the amount). The currency
    /// will be dollars; nothing earns or spends money yet, so the scene shows the "999" preview until
    /// some system calls <see cref="SetDollars"/>. The coin is a placeholder image for now.
    /// </summary>
    public class MoneyDisplay : MonoBehaviour
    {
        [SerializeField] TMP_Text amountText;
        [Tooltip("Icône de la pièce (placeholder rond jaune pour l'instant).")]
        [SerializeField] Image coinIcon;

        public int Dollars { get; private set; }

        void Awake()
        {
            if (amountText != null) amountText.raycastTarget = false;
            if (coinIcon != null) coinIcon.raycastTarget = false;
        }

        /// <summary>Shows an amount, written with <see cref="GameTexts.moneyFormat"/>.</summary>
        public void SetDollars(int dollars)
        {
            Dollars = dollars;
            if (amountText != null) amountText.text = GameTexts.Format(GameTexts.Current.moneyFormat, dollars);
        }
    }
}
