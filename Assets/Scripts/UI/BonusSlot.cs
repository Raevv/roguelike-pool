using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// One tab of the bonus menu (prefab Assets/Prefabs/UI/BonusSlot). A tap selects it: its visual
    /// slides a little to the left, like a bookmark pulled out; <see cref="BonusTabs"/> keeps one tab
    /// selected at a time. The visual extends past the right screen edge by the slide distance, so no
    /// gap opens when it slides. No bonus behaviour yet: the visual is a placeholder.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class BonusSlot : MonoBehaviour
    {
        [Tooltip("Visuel qui glisse (enfant).")]
        [SerializeField] RectTransform visual;
        [Tooltip("Distance du glissement vers la gauche quand l'onglet est sélectionné (unités du canvas).")]
        [SerializeField] float slideDistance = 30f;
        [Tooltip("Durée du glissement (s).")]
        [SerializeField] float slideTime = 0.12f;

        public event Action<BonusSlot> Clicked;
        public bool Selected { get; private set; }

        float baseX, offset, velocity;

        void Awake()
        {
            if (visual != null) baseX = visual.anchoredPosition.x;
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
                Clicked?.Invoke(this);
            });
        }

        public void SetSelected(bool selected) => Selected = selected;

        void Update()
        {
            if (visual == null) return;
            float target = Selected ? -slideDistance : 0f;
            offset = slideTime > 0f ? Mathf.SmoothDamp(offset, target, ref velocity, slideTime / 3f) : target;
            var p = visual.anchoredPosition;
            p.x = baseX + offset;
            visual.anchoredPosition = p;
        }
    }
}
