using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// The PopupLayer: a full-screen UI layer (under UI, below the overlays) holding the floating score
    /// texts. Listens to <see cref="StageManager.BallPocketed"/> and shows a <see cref="ScorePopup"/> next
    /// to the pocket. Popups are pooled (inactive children, reused).
    ///
    /// Positions: pocket world position → real screen pixels with <see cref="PixelCameraRig.WorldToScreen"/>
    /// (the project's WorldToScreenPoint: the world is drawn into a low-res texture, so Camera.WorldToScreenPoint
    /// alone would be wrong) → layer coordinates with RectTransformUtility, using the canvas camera
    /// (none in Screen Space - Overlay, the canvas camera in Screen Space - Camera). Recomputed every frame,
    /// so popups follow the table if the layout moves it.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ScorePopupLayer : MonoBehaviour
    {
        [SerializeField] ScorePopup popupPrefab;
        [Tooltip("Popups créés d'avance dans le pool.")]
        [SerializeField] int prewarm = 10;
        [Tooltip("Distance entre la poche et le texte, vers l'extérieur de la table (unités du canvas). Provisoire : l'éventail arrive à l'étape 3.")]
        [SerializeField] float pocketDistance = 110f;
        [Tooltip("Place minimale entre la poche et le bord du cadre, de son côté extérieur (unités du canvas). En dessous : poche de coin → texte au-dessus / en dessous d'elle, poche du milieu → texte sur la table.")]
        [SerializeField] float minSideRoom = 200f;
        [Tooltip("Distance quand le texte va sur la table (poche du milieu sans place à l'extérieur).")]
        [SerializeField] float inwardDistance = 120f;
        [Tooltip("Marge entre le texte et le bord du cadre du jeu (Safe Area), unités du canvas.")]
        [SerializeField] float screenMargin = 12f;
        [Tooltip("Zone visible où les textes doivent rester (SafeArea). Vide = cherchée dans la scène, sinon tout l'écran.")]
        [SerializeField] RectTransform visibleArea;

        readonly List<ScorePopup> active = new List<ScorePopup>();
        readonly Stack<ScorePopup> pool = new Stack<ScorePopup>();
        StageManager stages;
        TableRules rules;
        PoolTable table;
        Canvas canvas;
        PixelCameraRig rig;

        public IReadOnlyList<ScorePopup> Active => active;
        RectTransform Layer => (RectTransform)transform;

        public void Init(StageManager stageManager, TableRules tableRules, PoolTable poolTable)
        {
            stages = stageManager;
            rules = tableRules;
            table = poolTable;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
            if (visibleArea == null)
            {
                var safe = FindAnyObjectByType<SafeAreaPanel>(FindObjectsInactive.Include);
                if (safe != null) visibleArea = (RectTransform)safe.transform;
            }
            for (int i = pool.Count + active.Count; i < prewarm; i++) pool.Push(Create());
            stages.BallPocketed += OnBallPocketed;
            // Provisional until the merge (step 4): popups leave when the shot ends, and a scratch removes them.
            stages.ShotEnded += Clear;
            rules.ShotCancelled += Clear;
        }

        void OnDestroy()
        {
            if (stages != null) { stages.BallPocketed -= OnBallPocketed; stages.ShotEnded -= Clear; }
            if (rules != null) rules.ShotCancelled -= Clear;
        }

        void OnBallPocketed(Ball ball, Pocket pocket, int points) => Spawn(pocket, new ScorePopupData(points));

        /// <summary>Shows a popup next to a pocket.</summary>
        public ScorePopup Spawn(Pocket pocket, ScorePopupData data)
        {
            var popup = pool.Count > 0 ? pool.Pop() : Create();
            popup.Pocket = pocket;
            popup.transform.SetAsLastSibling(); // newest on top
            active.Add(popup);
            popup.Show(data); // sets the text first: its size decides the placement
            popup.Offset = ChooseOffset(pocket, popup.TextSize);
            Place(popup);
            return popup;
        }

        /// <summary>
        /// Away from the table if there is room on that side (<see cref="minSideRoom"/>) and the text fits
        /// inside the visible area. Otherwise (table close to the screen edge): a corner pocket puts it straight above / below itself, a side pocket puts it
        /// on the table. The result is always clamped inside the visible area.
        /// </summary>
        Vector2 ChooseOffset(Pocket pocket, Vector2 size)
        {
            Vector2 pocketPos = ToLayer(pocket.transform.position);
            Rect area = VisibleRect();
            Vector2 outward = Outward(pocket);
            Vector2 offset = outward * pocketDistance;
            float sideRoom = outward.x < 0f ? pocketPos.x - area.xMin : area.xMax - pocketPos.x;
            if (sideRoom < minSideRoom || !Fits(pocketPos + offset, size, area))
            {
                bool side = Mathf.Abs(outward.y) < 0.5f;
                offset = side ? -outward * inwardDistance
                              : new Vector2(0f, Mathf.Sign(outward.y)) * pocketDistance;
            }
            return Clamp(pocketPos + offset, size, area) - pocketPos;
        }

        bool Fits(Vector2 center, Vector2 size, Rect area)
        {
            Vector2 half = size * 0.5f + Vector2.one * screenMargin;
            return center.x - half.x >= area.xMin && center.x + half.x <= area.xMax &&
                   center.y - half.y >= area.yMin && center.y + half.y <= area.yMax;
        }

        Vector2 Clamp(Vector2 center, Vector2 size, Rect area)
        {
            Vector2 half = size * 0.5f + Vector2.one * screenMargin;
            return new Vector2(
                Mathf.Clamp(center.x, area.xMin + half.x, Mathf.Max(area.xMin + half.x, area.xMax - half.x)),
                Mathf.Clamp(center.y, area.yMin + half.y, Mathf.Max(area.yMin + half.y, area.yMax - half.y)));
        }

        static readonly Vector3[] corners = new Vector3[4];

        /// <summary>The visible area (safe area) in this layer's centered coordinates.</summary>
        Rect VisibleRect()
        {
            if (visibleArea == null) { var r = Layer.rect; return new Rect(r.position - r.center, r.size); }
            visibleArea.GetWorldCorners(corners);
            Vector2 a = (Vector2)Layer.InverseTransformPoint(corners[0]) - Layer.rect.center;
            Vector2 b = (Vector2)Layer.InverseTransformPoint(corners[2]) - Layer.rect.center;
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        /// <summary>Puts every active popup back in the pool.</summary>
        public void Clear()
        {
            foreach (var popup in active) Release(popup);
            active.Clear();
        }

        public void Release(ScorePopup popup)
        {
            popup.gameObject.SetActive(false);
            popup.Pocket = null;
            pool.Push(popup);
        }

        ScorePopup Create()
        {
            var popup = Instantiate(popupPrefab, transform);
            popup.name = popupPrefab.name;
            popup.gameObject.SetActive(false);
            return popup;
        }

        void LateUpdate()
        {
            foreach (var popup in active) Place(popup);
        }

        void Place(ScorePopup popup)
        {
            if (popup.Pocket == null) return;
            popup.Rect.anchoredPosition = ToLayer(popup.Pocket.transform.position) + popup.Offset;
        }

        /// <summary>World point → anchored position in this layer (anchors at its center).</summary>
        public Vector2 ToLayer(Vector2 world)
        {
            // Own reference: the static Instance can be lost by a domain reload during Play.
            if (rig == null) rig = PixelCameraRig.Instance != null ? PixelCameraRig.Instance : FindAnyObjectByType<PixelCameraRig>();
            Vector2 screen = rig != null ? rig.WorldToScreen(world) : (Vector2)Camera.main.WorldToScreenPoint(world);
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Layer, screen, cam, out var local);
            return local - Layer.rect.center;
        }

        /// <summary>Direction from the table center to the pocket, in layer space (away from the table).</summary>
        Vector2 Outward(Pocket pocket)
        {
            Vector2 center = table != null ? (Vector2)table.transform.position : Vector2.zero;
            Vector2 dir = ToLayer(pocket.transform.position) - ToLayer(center);
            return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.up;
        }
    }
}
