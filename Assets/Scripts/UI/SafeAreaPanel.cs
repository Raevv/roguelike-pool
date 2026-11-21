using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Keeps this RectTransform (a direct child of the canvas, stretched) inside <see cref="Screen.safeArea"/>
    /// (notch, rounded corners, gesture bar), and insets <see cref="content"/> by extra margins, in canvas
    /// units (reference 1080×1920). Elements that must touch the screen edge (the bonus tabs) are children
    /// of this object; everything else goes under <see cref="content"/>.
    /// Runs in edit mode too (full screen there, the margins apply), so the Scene view shows the layout;
    /// the real safe area (Device Simulator, phones) applies in Play Mode.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaPanel : MonoBehaviour
    {
        [Tooltip("Enfant qui reçoit les marges en plus de la zone sûre (tout sauf ce qui doit toucher le bord).")]
        [SerializeField] RectTransform content;
        [Tooltip("Marge à gauche et à droite (unités du canvas, référence 1080×1920).")]
        [SerializeField] float sideMargin = 40f;
        [Tooltip("Marge en bas (unités du canvas).")]
        [SerializeField] float bottomMargin = 80f;
        [Tooltip("Marge en haut (unités du canvas).")]
        [SerializeField] float topMargin = 0f;

        public RectTransform Content => content;

        RectTransform rt;
        Rect lastSafe;
        Vector2Int lastScreen;
        Vector4 lastMargins;

        void OnEnable() => Apply(true);
        void OnValidate() => Apply(true);
        void Update() => Apply(false);

        void Apply(bool force)
        {
            if (rt == null) rt = (RectTransform)transform;
            // In edit mode Screen.* may describe whichever editor view is repainting: use the full canvas.
            var screen = Application.isPlaying ? new Vector2Int(Screen.width, Screen.height) : new Vector2Int(1, 1);
            Rect safe = Application.isPlaying ? Screen.safeArea : new Rect(0f, 0f, 1f, 1f);
            var margins = new Vector4(sideMargin, bottomMargin, topMargin, 0f);
            if (!force && safe == lastSafe && screen == lastScreen && margins == lastMargins) return;
            if (screen.x <= 0 || screen.y <= 0) return;
            lastSafe = safe;
            lastScreen = screen;
            lastMargins = margins;

            SetIfChanged(rt, new Vector2(safe.xMin / screen.x, safe.yMin / screen.y),
                             new Vector2(safe.xMax / screen.x, safe.yMax / screen.y), Vector2.zero, Vector2.zero);
            if (content != null)
                SetIfChanged(content, Vector2.zero, Vector2.one,
                             new Vector2(sideMargin, bottomMargin), new Vector2(-sideMargin, -topMargin));
        }

        /// <summary>Only writes values that differ, so edit mode never dirties the scene for nothing.</summary>
        static void SetIfChanged(RectTransform t, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
        {
            if (t.anchorMin != aMin) t.anchorMin = aMin;
            if (t.anchorMax != aMax) t.anchorMax = aMax;
            if (t.offsetMin != oMin) t.offsetMin = oMin;
            if (t.offsetMax != oMax) t.offsetMax = oMax;
        }
    }
}
