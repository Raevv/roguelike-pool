using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Lives on TableAnchor, an empty RectTransform of the UI marking where the pool table goes
    /// (mockup x 41 → 847, y 430 → 1693 on 1080×1920; anchored so it stretches with the screen).
    /// Every frame it tells <see cref="PixelCameraRig"/> to scale and move the orthographic world camera
    /// so the table's frame (<see cref="PoolTable.OuterBounds"/>), plus the margins below, fills this
    /// rect exactly on any screen format. The limiting side fills; on the other the table is placed by
    /// the alignment settings (centered by default).
    /// The rect is drawn as a gizmo in the Scene view.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class TableFitter : MonoBehaviour
    {
        [Tooltip("Table à cadrer (trouvée automatiquement si vide).")]
        [SerializeField] PoolTable table;

        [Header("Marges autour de la table (unités du monde, 1 = 10 cm ; négatif = rogner)")]
        [SerializeField] float marginLeft = 0f;
        [SerializeField] float marginRight = 0f;
        [SerializeField] float marginTop = 0f;
        [SerializeField] float marginBottom = 0f;

        [Header("Quand la zone n'a pas les proportions de la table")]
        [Tooltip("Position verticale de la table dans la zone : 0 = en bas, 0,5 = centrée, 1 = en haut.")]
        [Range(0f, 1f)] [SerializeField] float verticalAlignment = 0.5f;
        [Tooltip("Position horizontale de la table dans la zone : 0 = à gauche, 0,5 = centrée, 1 = à droite.")]
        [Range(0f, 1f)] [SerializeField] float horizontalAlignment = 0.5f;

        [Tooltip("Couleur du gizmo de la zone dans la vue Scene.")]
        [SerializeField] Color gizmoColor = new Color(0.1f, 1f, 0.4f, 0.9f);

        static readonly Vector3[] corners = new Vector3[4];
        RectTransform rt;
        PixelCameraRig rig;

        public void Init(PoolTable poolTable, PixelCameraRig cameraRig)
        {
            table = poolTable;
            rig = cameraRig;
            Fit();
        }

        void LateUpdate()
        {
            if (Application.isPlaying) Fit();
        }

        /// <summary>World rect to frame: the table's frame plus the margins.</summary>
        public Rect WorldRect
        {
            get
            {
                Rect b = table.OuterBounds;
                b.position += (Vector2)table.transform.position;
                return Rect.MinMaxRect(b.xMin - marginLeft, b.yMin - marginBottom, b.xMax + marginRight, b.yMax + marginTop);
            }
        }

        /// <summary>This rect in screen pixels (bottom-left origin). The canvas is Screen Space Overlay.</summary>
        public Rect ScreenRect
        {
            get
            {
                if (rt == null) rt = (RectTransform)transform;
                rt.GetWorldCorners(corners);
                return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
            }
        }

        public void Fit()
        {
            if (table == null) table = FindAnyObjectByType<PoolTable>();
            if (rig == null) rig = PixelCameraRig.Instance;
            if (table == null || rig == null) return;
            // Shrink the screen rect to the table's aspect ratio, placed by the alignment settings.
            Rect world = WorldRect, screen = ScreenRect;
            if (world.width <= 0f || world.height <= 0f) return;
            float s = Mathf.Min(screen.width / world.width, screen.height / world.height);
            var size = world.size * s;
            var fitted = new Rect(screen.x + (screen.width - size.x) * horizontalAlignment,
                                  screen.y + (screen.height - size.y) * verticalAlignment, size.x, size.y);
            rig.SetFit(world, fitted);
        }

        void OnDrawGizmos()
        {
            if (rt == null) rt = (RectTransform)transform;
            rt.GetWorldCorners(corners);
            Gizmos.color = gizmoColor;
            for (int i = 0; i < 4; i++) Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            Gizmos.DrawLine(corners[0], corners[2]);
            Gizmos.DrawLine(corners[1], corners[3]);
        }
    }
}
