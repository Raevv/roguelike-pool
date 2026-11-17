using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Builds the top-down table in portrait orientation: long axis along Y, the player's end
    /// (head, cue ball) at the bottom, the rack (foot) at the top.
    /// Scale: 1 world unit = 10 cm. Defaults match a 9ft table (1.27 m x 2.54 m playing surface).
    /// The play area is the rectangle bounded by the cushion noses, centered on this transform.
    ///
    /// Cushions are 6 trapezoids (PolygonCollider2D) cut at the pockets; corner jaws are at 45°, so
    /// each corner mouth is a straight channel leading to the hole. A ball is pocketed when its
    /// center gets within the hole radius (see <see cref="Pocket"/>). Solid walls at the outer rail
    /// edge guarantee nothing can leave the table.
    /// </summary>
    public class PoolTable : MonoBehaviour
    {
        [Header("Dimensions (1 unité = 10 cm)")]
        public float playWidth = 12.7f;
        public float playHeight = 25.4f;
        public float cushionWidth = 0.45f;
        public float railWidth = 1.1f;

        [Header("Poches")]
        [Tooltip("Distance entre le coin de la surface et le bout de chaque bande, le long de la bande. Ouverture ≈ valeur × 1,41.")]
        public float cornerMouthOffset = 1.294f;
        [Tooltip("Demi-ouverture des poches du milieu.")]
        public float sideMouthHalfWidth = 1.006f;
        public float cornerPocketRadius = 0.891f;
        public float sidePocketRadius = 0.834f;
        [Tooltip("Décalage du centre des poches de coin vers l'extérieur (en diagonale).")]
        public float cornerPocketOffset = 0.1f;

        [Header("Mâchoires fantômes (blanche uniquement, invisibles)")]
        [Tooltip("Prolongement des bandes vers les poches de coin, le long de la bande, qui ne bloque que la blanche. Rétrécit l'ouverture pour elle d'environ valeur × 1,41 de chaque côté.")]
        public float cueGuardCorner = 0.35f;
        [Tooltip("Biseau du bout des bandes côté poches du milieu, pour la blanche uniquement : la pointe de mâchoire recule de cette distance le long de la bande, ce qui ouvre l'entrée aux tirs en angle. 0 = mâchoire droite comme pour les couleurs.")]
        public float sideCueBevel = 0.5f;
        [Tooltip("Rayon de capture de la blanche pour les poches du milieu, en fraction du rayon de capture des couleurs (les coins utilisent la valeur par défaut de Pocket).")]
        [Range(0.1f, 1f)] public float sideCueCaptureScale = 0.7f;
        [Tooltip("Déviation des rebonds de la blanche vers les poches du milieu (0 = aucune aide). Les coins n'en ont pas.")]
        [Range(0f, 1f)] public float sideCueFunnel = 0f;

        [Header("Physique")]
        [Range(0f, 1f)] public float cushionBounciness = 0.8f;

        /// <summary>Layer of colliders that only colored balls hit (cushions that have a cue-only twin). Unnamed user layer.</summary>
        public const int ObjectOnlyLayer = 7;

        public PhysicsMaterial2D CushionMaterial { get; private set; }
        public IReadOnlyList<Pocket> Pockets => pockets;
        /// <summary>Cushion polygons in local space (nose edge = points 0 → 1).</summary>
        public IReadOnlyList<Vector2[]> CushionQuads => cushionQuads;

        readonly List<Pocket> pockets = new List<Pocket>();
        readonly List<Vector2[]> cushionQuads = new List<Vector2[]>();

        public Rect OuterBounds
        {
            get
            {
                float border = cushionWidth + railWidth;
                return new Rect(-playWidth / 2f - border, -playHeight / 2f - border,
                    playWidth + 2f * border, playHeight + 2f * border);
            }
        }

        /// <summary>Cue ball start: middle of the head string (bottom quarter).</summary>
        public Vector2 HeadSpot => (Vector2)transform.position + new Vector2(0f, -playHeight / 4f);
        /// <summary>Rack apex position (top quarter).</summary>
        public Vector2 FootSpot => (Vector2)transform.position + new Vector2(0f, playHeight / 4f);

        void Awake()
        {
            CushionMaterial = new PhysicsMaterial2D("Cushion")
            {
                bounciness = cushionBounciness,
                friction = 0f,
                bounceCombine = PhysicsMaterialCombine2D.Multiply,
                frictionCombine = PhysicsMaterialCombine2D.Multiply
            };
            Build();
        }

        void Build()
        {
            float hw = playWidth / 2f, hh = playHeight / 2f, cw = cushionWidth;
            float m = cornerMouthOffset, s = sideMouthHalfWidth;
            Rect outer = OuterBounds;

            // Pockets.
            float co = cornerPocketOffset;
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                CreatePocket("CornerPocket", new Vector2(sx * (hw + co), sy * (hh + co)), cornerPocketRadius);
            foreach (var side in new[] {
                CreatePocket("SidePocketLeft", new Vector2(-(hw + cw * 0.9f), 0f), sidePocketRadius),
                CreatePocket("SidePocketRight", new Vector2(hw + cw * 0.9f, 0f), sidePocketRadius) })
            {
                side.cueCaptureScale = sideCueCaptureScale;
                side.cueFunnelStrength = sideCueFunnel;
            }

            // Cue-only colliders (invisible): corner jaw extensions and beveled side jaws.
            var guards = new GameObject("CueGuards").transform;
            guards.SetParent(transform, false);

            // Cushions (nose points first, then back points).
            var cushions = new GameObject("Cushions").transform;
            cushions.SetParent(transform, false);
            float bevel = Mathf.Clamp(sideCueBevel, 0f, hh - m - s - 0.1f);
            for (int sx = -1; sx <= 1; sx += 2)
            {
                // Long rails, upper and lower segments (between corner and side pocket).
                for (int sy = -1; sy <= 1; sy += 2)
                {
                    var cushion = AddCushion(cushions, Mirror(sx, sy,
                        new Vector2(hw, s), new Vector2(hw, hh - m),
                        new Vector2(hw + cw, hh - m + cw), new Vector2(hw + cw, s)));
                    if (bevel > 0f)
                    {
                        // The drawn cushion stays square for colored balls; the cue ball gets a twin whose
                        // side-pocket end is beveled, so angled cue shots are not stopped by the jaw tip.
                        // Twins instead of an add-on piece: each ball sees one continuous polygon, no seam.
                        cushion.gameObject.layer = ObjectOnlyLayer;
                        cushion.excludeLayers = 1 << Ball.CueLayer;
                        AddCueGuard(guards, "CueJawSide", Mirror(sx, sy,
                            new Vector2(hw, s + bevel), new Vector2(hw, hh - m),
                            new Vector2(hw + cw, hh - m + cw), new Vector2(hw + cw, s)));
                    }
                }
            }
            for (int sy = -1; sy <= 1; sy += 2)
            {
                // Short rails.
                AddCushion(cushions, Mirror(1, sy,
                    new Vector2(-hw + m, hh), new Vector2(hw - m, hh),
                    new Vector2(hw - m + cw, hh + cw), new Vector2(-hw + m - cw, hh + cw)));
            }

            // Cue-only corner guards: invisible extensions of each cushion end toward the corner pocket, so
            // the mouth is narrower for the cue ball only (colored balls pass through them).
            float gc = Mathf.Clamp(cueGuardCorner, 0f, m - 0.05f);
            for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
            {
                if (gc > 0f)
                {
                    // Corner: long-rail end and short-rail end, each shifted along its 45° jaw.
                    AddCueGuard(guards, "CueGuardCorner", Mirror(sx, sy,
                        new Vector2(hw, hh - m), new Vector2(hw, hh - m + gc),
                        new Vector2(hw + cw, hh - m + gc + cw), new Vector2(hw + cw, hh - m + cw)));
                    AddCueGuard(guards, "CueGuardCorner", Mirror(sx, sy,
                        new Vector2(hw - m, hh), new Vector2(hw - m + gc, hh),
                        new Vector2(hw - m + gc + cw, hh + cw), new Vector2(hw - m + cw, hh + cw)));
                }
            }

            // Safety walls at the outer edge of the rails.
            var walls = new GameObject("OuterWalls");
            walls.transform.SetParent(transform, false);
            float t = 2f;
            AddWall(walls, new Vector2(0f, outer.yMax + t / 2f), new Vector2(outer.width + 2f * t, t));
            AddWall(walls, new Vector2(0f, outer.yMin - t / 2f), new Vector2(outer.width + 2f * t, t));
            AddWall(walls, new Vector2(outer.xMin - t / 2f, 0f), new Vector2(t, outer.height + 2f * t));
            AddWall(walls, new Vector2(outer.xMax + t / 2f, 0f), new Vector2(t, outer.height + 2f * t));

            // All visuals (rails, felt, cushions, pockets, diamonds, shadow, floor) are pixel art.
            TableArt.Build(this);
        }

        static Vector2[] Mirror(int sx, int sy, params Vector2[] points)
        {
            var result = new Vector2[points.Length];
            for (int i = 0; i < points.Length; i++) result[i] = new Vector2(points[i].x * sx, points[i].y * sy);
            return result;
        }

        PolygonCollider2D AddCushion(Transform parent, Vector2[] quad)
        {
            var go = new GameObject("Cushion");
            go.transform.SetParent(parent, false);
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.points = quad;
            poly.sharedMaterial = CushionMaterial;
            cushionQuads.Add(quad);
            return poly;
        }

        void AddCueGuard(Transform parent, string guardName, Vector2[] quad)
        {
            var go = new GameObject(guardName);
            go.transform.SetParent(parent, false);
            var poly = go.AddComponent<PolygonCollider2D>();
            poly.points = quad;
            poly.sharedMaterial = CushionMaterial;
            poly.excludeLayers = ~(1 << Ball.CueLayer); // only the cue ball collides with it
        }

        void AddWall(GameObject host, Vector2 center, Vector2 size)
        {
            var box = host.AddComponent<BoxCollider2D>();
            box.offset = center;
            box.size = size;
            box.sharedMaterial = CushionMaterial;
        }

        Pocket CreatePocket(string pocketName, Vector2 pos, float radius)
        {
            var go = new GameObject(pocketName);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = pos;
            var pocket = go.AddComponent<Pocket>();
            pocket.radius = radius;
            pockets.Add(pocket);
            return pocket;
        }
    }
}
