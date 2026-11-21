using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// The bonus menu: a column of <see cref="BonusSlot"/> tabs on the right screen edge (the spacing,
    /// negative for the slight overlap, is the VerticalLayoutGroup's). The layout uses Reverse Arrangement
    /// so the first child sits at the bottom and the top tab is drawn last, over the one below (its
    /// shadow falls on it). One tab is selected at a time; tapping it again puts it back.
    /// </summary>
    public class BonusTabs : MonoBehaviour
    {
        BonusSlot[] slots;

        void Awake()
        {
            slots = GetComponentsInChildren<BonusSlot>(true);
            foreach (var slot in slots) slot.Clicked += OnClicked;
        }

        void OnClicked(BonusSlot clicked)
        {
            bool select = !clicked.Selected;
            foreach (var slot in slots) slot.SetSelected(select && slot == clicked);
        }
    }
}
