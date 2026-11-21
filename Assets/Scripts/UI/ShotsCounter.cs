using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Shot icons under the table, centered on the table axis, in rows of at most maxPerRow (5): one <see cref="ShotIcon"/> per shot
    /// of the stage (<see cref="StageManager.MaxShots"/>), greyed once used. Icons are
    /// <see cref="maxIconSize"/> wide with <see cref="spacing"/> between them; when they do not fit in
    /// the rect (width or height), icons and spacings shrink together. The icons placed in the scene are only a
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
        [Tooltip("Nombre maximum d'icônes par ligne ; au-delà, une nouvelle ligne commence (chaque ligne est centrée).")]
        [SerializeField] int maxPerRow = 5;
        [Tooltip("Espace vertical entre deux lignes à la taille maximale (réduit dans la même proportion que les icônes).")]
        [SerializeField] float rowSpacing = 12f;
        [Tooltip("Les tirs utilisés se grisent depuis la gauche (sinon depuis la droite).")]
        [SerializeField] bool greyFromLeft = false;

        [Header("Position sous la table")]
        [Tooltip("Zone de la table (TableAnchor) : le compteur suit le bord bas réel de la table.")]
        [SerializeField] TableFitter tableFitter;
        [Tooltip("Espace entre le bas de la table et le haut de la première ligne (unités du canvas).")]
        [SerializeField] float tableMargin = 53f;

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

        /// <summary>
        /// Lays the active icons out in centered rows of at most <see cref="maxPerRow"/> (the last row is
        /// centered too), from the top of the rect down, shrinking icons and gaps if they do not fit.
        /// </summary>
        [ContextMenu("Arranger les icônes")]
        public void Arrange()
        {
            var rt = (RectTransform)transform;
            var active = new List<RectTransform>();
            foreach (Transform child in transform)
                if (child.gameObject.activeSelf && child.GetComponent<ShotIcon>() != null) active.Add((RectTransform)child);
            int n = active.Count;
            if (n == 0) return;

            int perRow = Mathf.Max(1, maxPerRow);
            int columns = Mathf.Min(n, perRow);
            int rows = (n + perRow - 1) / perRow;
            float ratio = maxIconSize > 0f ? spacing / maxIconSize : 0f;
            float rowRatio = maxIconSize > 0f ? rowSpacing / maxIconSize : 0f;
            float size = Mathf.Min(maxIconSize,
                rt.rect.width / (columns + (columns - 1) * ratio),
                rt.rect.height / (rows + (rows - 1) * rowRatio));
            float gap = size * ratio, rowGap = size * rowRatio;

            float y = rt.rect.height / 2f - size / 2f; // first row against the top of the rect (near the table)
            for (int row = 0; row < rows; row++)
            {
                int first = row * perRow;
                int count = Mathf.Min(perRow, n - first);
                float x = -(count * size + (count - 1) * gap) / 2f + size / 2f;
                for (int i = first; i < first + count; i++)
                {
                    var icon = active[i];
                    icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(0.5f, 0.5f);
                    icon.sizeDelta = new Vector2(size, size);
                    icon.anchoredPosition = new Vector2(x, y);
                    x += size + gap;
                }
                y -= size + rowGap;
            }
        }

        // The row resizes with the screen: re-arrange on the next frame (not inside the layout callback).
        bool arrangePending;
        void OnRectTransformDimensionsChange() => arrangePending = true;

        void LateUpdate()
        {
            FollowTable();
            if (!arrangePending) return;
            arrangePending = false;
            Arrange();
        }

        static readonly Vector3[] parentCorners = new Vector3[4];

        /// <summary>
        /// Keeps the rect just under the table as it is really drawn (any screen format, any TableFitter
        /// alignment): as wide as the table and centered on it, its top <see cref="tableMargin"/> below the
        /// table's bottom edge. The height (room for the rows) is the one set in the scene.
        /// </summary>
        void FollowTable()
        {
            if (tableFitter == null) return;
            var rt = (RectTransform)transform;
            var parent = rt.parent as RectTransform;
            if (parent == null) return;
            Rect table = tableFitter.FittedScreenRect; // screen pixels (overlay canvas)
            if (table.width <= 0f) return;
            float toCanvas = parent.lossyScale.x > 0f ? 1f / parent.lossyScale.x : 1f; // screen px → canvas units
            parent.GetWorldCorners(parentCorners);
            var anchor = new Vector2(0f, 0f);
            var position = new Vector2((table.center.x - parentCorners[0].x) * toCanvas,
                                       (table.yMin - parentCorners[0].y) * toCanvas - tableMargin);
            var size = new Vector2(table.width * toCanvas, rt.sizeDelta.y);
            if (rt.anchorMin != anchor || rt.anchorMax != anchor) rt.anchorMin = rt.anchorMax = anchor;
            if (rt.pivot != new Vector2(0.5f, 1f)) rt.pivot = new Vector2(0.5f, 1f);
            if ((rt.anchoredPosition - position).sqrMagnitude > 0.01f) rt.anchoredPosition = position;
            if (Mathf.Abs(rt.sizeDelta.x - size.x) > 0.1f) rt.sizeDelta = size;
        }
    }
}
