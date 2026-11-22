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
        [Header("Éventail")]
        [Tooltip("Rayon de l'éventail : distance entre la poche et les textes, vers l'extérieur de la table (unités du canvas).")]
        [SerializeField] float fanRadius = 110f;
        [Tooltip("Rayon de l'éventail quand les textes vont sur la table (poche du milieu sans place à l'extérieur).")]
        [SerializeField] float inwardFanRadius = 120f;
        [Tooltip("Espace minimal entre deux textes (unités du canvas). Couvre aussi le flottement.")]
        [SerializeField] float fanSpacing = 14f;
        [Tooltip("Angle maximal de l'éventail de chaque côté de sa direction (degrés).")]
        [SerializeField] float maxFanAngle = 110f;
        [Tooltip("Anneaux successifs quand l'arc est plein (chacun plus loin de la hauteur d'un texte).")]
        [SerializeField] int maxRings = 4;
        [Tooltip("Place minimale entre la poche et le bord du cadre, de son côté extérieur (unités du canvas). En dessous : poche de coin → textes au-dessus / en dessous d'elle, poche du milieu → textes sur la table.")]
        [SerializeField] float minSideRoom = 200f;
        [Tooltip("Marge entre le texte et le bord du cadre du jeu (Safe Area), unités du canvas.")]
        [SerializeField] float screenMargin = 12f;
        [Tooltip("Zone visible où les textes doivent rester (SafeArea). Vide = cherchée dans la scène, sinon tout l'écran.")]
        [SerializeField] RectTransform visibleArea;
        [Tooltip("Éléments d'interface que les textes ne doivent pas recouvrir (boutons, jauge, onglets, score, craies…).")]
        [SerializeField] List<RectTransform> avoid = new List<RectTransform>();

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
            popup.Size = popup.TextSize;
            popup.Offset = ChooseOffset(popup);
            Place(popup);
            return popup;
        }

        /// <summary>
        /// Fan placement. The fan's direction: away from the table if there is room on that side
        /// (<see cref="minSideRoom"/>); otherwise (table close to the screen edge) straight above / below a
        /// corner pocket, or onto the table for a side pocket. Successive texts take the next free slot of
        /// an arc around the pocket, sweeping clockwise (−step, 0, +step, +2·step…, then the other side),
        /// so +1, +2, +3 go from one side to the other like the reference. A slot is skipped if the text
        /// would overlap another popup or leave the visible area; a full arc moves on to a farther ring.
        /// </summary>
        Vector2 ChooseOffset(ScorePopup popup)
        {
            Vector2 size = popup.Size;
            Vector2 pocketPos = ToLayer(popup.Pocket.transform.position);
            Rect area = VisibleRect();
            Vector2 outward = Outward(popup.Pocket);
            Vector2 dir = outward;
            float radius = fanRadius;
            float sideRoom = outward.x < 0f ? pocketPos.x - area.xMin : area.xMax - pocketPos.x;
            if (sideRoom < minSideRoom || !Fits(pocketPos + outward * fanRadius, size, area))
            {
                bool side = Mathf.Abs(outward.y) < 0.5f;
                dir = side ? -outward : new Vector2(0f, Mathf.Sign(outward.y));
                if (side) radius = inwardFanRadius;
            }

            // Two texts whose centers are one diagonal apart can never overlap, whatever the angle.
            float ringStep = (size + Vector2.one * fanSpacing).magnitude;
            // Pass 1: the fan. Pass 2 (crowded pocket): all around the pocket, farther away.
            if (TryRings(popup, pocketPos, dir, radius, ringStep, maxFanAngle, Mathf.Max(1, maxRings), size, area, out var offset) ||
                TryRings(popup, pocketPos, dir, radius, ringStep, 180f, Mathf.Max(1, maxRings) * 2, size, area, out offset))
                return offset;
            // Still no free slot: the first slot of the fan, kept on screen.
            float firstStep = SlotAngle(ringStep, radius);
            return Clamp(pocketPos + Rotate(dir, firstStep) * radius, size, area) - pocketPos;
        }

        bool TryRings(ScorePopup popup, Vector2 pocketPos, Vector2 dir, float radius, float ringStep, float maxAngle,
                      int rings, Vector2 size, Rect area, out Vector2 offset)
        {
            for (int ring = 0; ring < rings; ring++)
            {
                float r = radius + ring * ringStep;
                float step = SlotAngle(ringStep, r);
                int perSide = Mathf.Max(1, Mathf.FloorToInt(maxAngle / step));
                foreach (int k in SweepOrder(perSide))
                {
                    offset = Rotate(dir, -k * step) * r; // negative angle = clockwise on screen
                    Vector2 center = pocketPos + offset;
                    if (Fits(center, size, area) && !Overlaps(popup, center, size)) return true;
                }
            }
            offset = default;
            return false;
        }

        /// <summary>Angle between two neighbours on a ring of radius r, so that their centers are one chord apart.</summary>
        static float SlotAngle(float chord, float r) => 2f * Mathf.Asin(Mathf.Min(1f, chord / (2f * r))) * Mathf.Rad2Deg;

        /// <summary>Slot order on one ring: −1, 0, 1, 2 … perSide, then −2 … −perSide.</summary>
        static IEnumerable<int> SweepOrder(int perSide)
        {
            yield return -1;
            for (int k = 0; k <= perSide; k++) yield return k;
            for (int k = 2; k <= perSide; k++) yield return -k;
        }

        static Vector2 Rotate(Vector2 v, float degrees)
        {
            float a = degrees * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        /// <summary>Would a text at this center overlap another active popup (any pocket) or a UI element to avoid?</summary>
        bool Overlaps(ScorePopup self, Vector2 center, Vector2 size)
        {
            var box = new Rect(center - size * 0.5f, size);
            foreach (var element in avoid)
            {
                if (element == null || !element.gameObject.activeInHierarchy) continue;
                Rect r = ToLayerRect(element);
                r.xMin -= fanSpacing; r.yMin -= fanSpacing; r.xMax += fanSpacing; r.yMax += fanSpacing;
                if (r.Overlaps(box)) return true;
            }
            foreach (var other in active)
            {
                if (other == self || other.Pocket == null) continue;
                Vector2 c = ToLayer(other.Pocket.transform.position) + other.Offset;
                Vector2 gap = (size + other.Size) * 0.5f + Vector2.one * fanSpacing;
                if (Mathf.Abs(center.x - c.x) < gap.x && Mathf.Abs(center.y - c.y) < gap.y) return true;
            }
            return false;
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
            return ToLayerRect(visibleArea);
        }

        /// <summary>Any RectTransform of the canvas, in this layer's centered coordinates.</summary>
        Rect ToLayerRect(RectTransform element)
        {
            element.GetWorldCorners(corners);
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
