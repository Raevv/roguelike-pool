using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Row of shot icons under the table, centered on the table axis: one <see cref="ShotIcon"/> per shot
    /// of the stage (<see cref="StageManager.MaxShots"/>), greyed once used. Icons are
    /// <see cref="maxIconSize"/> wide with <see cref="spacing"/> between them; when they do not fit in
    /// the row's width, icons and spacing shrink together. The icons placed in the scene are only a
    /// preview: at runtime the row is rebuilt from the prefab with the stage's shot count.
    /// Refreshed on <see cref="StageManager.Changed"/>.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ShotsCounter : MonoBehaviour
    {
        [Tooltip("Prefab d'une icône de tir (Assets/Prefabs/UI/ShotIcon).")]
        [SerializeField] ShotIcon iconPrefab;
        [Tooltip("Taille maximale d'une icône (unités du canvas). Réduite automatiquement si la rangée déborde.")]
        [SerializeField] float maxIconSize = 64f;
        [Tooltip("Espace entre deux icônes à la taille maximale (réduit dans la même proportion que les icônes).")]
        [SerializeField] float spacing = 40f;
        [Tooltip("Les tirs utilisés se grisent depuis la gauche (sinon depuis la droite).")]
        [SerializeField] bool greyFromLeft = false;

        readonly List<ShotIcon> icons = new List<ShotIcon>();
        StageManager manager;

        public void Init(StageManager stageManager)
        {
            manager = stageManager;
            icons.Clear();
            GetComponentsInChildren(true, icons);
            manager.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (manager != null) manager.Changed -= Refresh;
        }

        void Refresh() => SetShots(manager.MaxShots, manager.MaxShots - manager.ShotsLeft);

        /// <summary>Shows <paramref name="total"/> icons, <paramref name="used"/> of them greyed.</summary>
        public void SetShots(int total, int used)
        {
            total = Mathf.Max(0, total);
            while (icons.Count < total && iconPrefab != null) icons.Add(Instantiate(iconPrefab, transform));
            for (int i = 0; i < icons.Count; i++)
            {
                bool active = i < total;
                icons[i].gameObject.SetActive(active);
                if (!active) continue;
                int fromStart = greyFromLeft ? i : total - 1 - i;
                icons[i].SetUsed(fromStart < used);
            }
            Arrange();
        }

        /// <summary>Lays the active icons out, centered, shrinking them if the row is too narrow.</summary>
        [ContextMenu("Arranger les icônes")]
        public void Arrange()
        {
            var rt = (RectTransform)transform;
            var active = new List<RectTransform>();
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf && child.GetComponent<ShotIcon>() != null) active.Add((RectTransform)child);
            int n = active.Count;
            if (n == 0) return;

            float ratio = maxIconSize > 0f ? spacing / maxIconSize : 0f;
            float size = Mathf.Min(maxIconSize, rt.rect.width / (n + (n - 1) * ratio));
            float gap = size * ratio;
            float x = -(n * size + (n - 1) * gap) / 2f + size / 2f;
            foreach (var icon in active)
            {
                icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
                icon.sizeDelta = new Vector2(size, size);
                icon.anchoredPosition = new Vector2(x, 0f);
                x += size + gap;
            }
        }

        // The row resizes with the screen: re-arrange on the next frame (not inside the layout callback).
        bool arrangePending;
        void OnRectTransformDimensionsChange() => arrangePending = true;

        void LateUpdate()
        {
            if (!arrangePending) return;
            arrangePending = false;
            Arrange();
        }
    }
}
