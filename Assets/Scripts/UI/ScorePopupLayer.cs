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

        readonly List<ScorePopup> active = new List<ScorePopup>();
        readonly Stack<ScorePopup> pool = new Stack<ScorePopup>();
        StageManager stages;
        TableRules rules;
        PoolTable table;
        Canvas canvas;

        public IReadOnlyList<ScorePopup> Active => active;
        RectTransform Rect => (RectTransform)transform;

        public void Init(StageManager stageManager, TableRules tableRules, PoolTable poolTable)
        {
            stages = stageManager;
            rules = tableRules;
            table = poolTable;
            canvas = GetComponentInParent<Canvas>().rootCanvas;
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
            popup.Offset = Outward(pocket) * pocketDistance;
            popup.transform.SetAsLastSibling(); // newest on top
            active.Add(popup);
            Place(popup);
            popup.Show(data);
            return popup;
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
            var rig = PixelCameraRig.Instance;
            Vector2 screen = rig != null ? rig.WorldToScreen(world) : (Vector2)Camera.main.WorldToScreenPoint(world);
            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screen, cam, out var local);
            return local - Rect.rect.center;
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
