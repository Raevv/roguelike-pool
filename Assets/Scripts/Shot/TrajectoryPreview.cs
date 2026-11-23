using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Draws the cue ball's predicted path: a line to the first contact, a ghost ball at the contact
    /// point, and a short segment for the path right after the bounce.
    /// The bounce direction accounts for cushion restitution (frictionless cushion: the normal
    /// component is scaled by e, the tangential one is kept), matching what the physics does.
    /// </summary>
    public class TrajectoryPreview : MonoBehaviour
    {
        [Tooltip("Longueur du segment après rebond (unités, 1 = 10 cm).")]
        public float bounceLength = 3f;
        [Tooltip("Longueur de la ligne de direction de la boule touchée.")]
        public float objectLineLength = 3f;
        public float maxDistance = 60f;

        [Header("Style pixel art")]
        [Tooltip("Épaisseur des lignes en pixels d'art.")]
        public float lineWidthPixels = 1.25f;
        public Color aimColor = new Color(1f, 0.95f, 0.82f, 0.9f);
        public Color bounceColor = new Color(1f, 0.95f, 0.82f, 0.45f);
        public Color ghostColor = new Color(1f, 0.95f, 0.85f, 0.75f);
        public Color objectColor = new Color(1f, 0.78f, 0.35f, 0.9f);

        LineRenderer aimLine, bounceLine, objectLine;
        SpriteRenderer ghostBall;
        readonly List<RaycastHit2D> hits = new List<RaycastHit2D>();
        ContactFilter2D filter;

        void Awake()
        {
            // Dotted pixel lines (2 px on, 2 px off); slight HDR tint so they glow faintly.
            var dots = PixelArt.NewTexture(4, 1, "AimDots", repeat: true);
            dots.SetPixels32(new Color32[] { (Color32)Color.white, Color.white, new Color32(0, 0, 0, 0), new Color32(0, 0, 0, 0) });
            dots.Apply(false, false);
            aimLine = CreateLine("AimLine", DottedMaterial(dots, 1.3f), aimColor);
            bounceLine = CreateLine("BounceLine", DottedMaterial(dots, 1f), bounceColor);
            objectLine = CreateLine("ObjectBallLine", DottedMaterial(dots, 1.5f), objectColor);
            ghostBall = CreateGhost();

            filter = ContactFilter2D.noFilter;
            filter.useTriggers = false;
            filter.SetLayerMask(~(1 << PoolTable.ObjectOnlyLayer)); // the preview is for the cue ball: skip colored-only colliders
            Hide();
        }

        static Material DottedMaterial(Texture2D dots, float intensity)
        {
            var m = PixelArt.GlowMaterial(new Color(intensity, intensity, intensity, 1f));
            m.mainTexture = dots;
            return m;
        }

        LineRenderer CreateLine(string lineName, Material material, Color color)
        {
            var go = new GameObject(lineName);
            go.transform.SetParent(transform, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = material;
            lr.useWorldSpace = true;
            lr.textureMode = LineTextureMode.Tile;
            lr.textureScale = new Vector2(PixelArt.PPU / 4f, 1f); // one 4-px pattern per 4 art pixels
            lr.startWidth = lr.endWidth = lineWidthPixels * PixelArt.Pixel;
            lr.startColor = lr.endColor = color;
            lr.numCapVertices = 0;
            lr.sortingOrder = 6;
            lr.positionCount = 2;
            return lr;
        }

        SpriteRenderer CreateGhost()
        {
            // Ring about the size of a ball (21 art px at 24 px/unit ≈ the 0.894 diameter), with a faint flat inner tint.
            const int S = 21;
            var px = new Color32[S * S];
            const float r = S / 2f;
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d < r - 0.15f && d > r - 1.2f) px[y * S + x] = Color.white;
                else if (d < r - 1.2f) px[y * S + x] = new Color32(255, 255, 255, 28);
            }
            var go = new GameObject("GhostBall");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = PixelArt.ToSprite(PixelArt.NewTexture(S, S, "GhostBall"), px);
            sr.sharedMaterial = PixelArt.GlowMaterial(new Color(1.3f, 1.25f, 1.1f));
            sr.color = ghostColor;
            sr.sortingOrder = 7;
            return sr;
        }

        public void Hide()
        {
            aimLine.enabled = bounceLine.enabled = ghostBall.enabled = objectLine.enabled = false;
        }

        public void Show(Ball ball, Vector2 direction, float cushionRestitution, IReadOnlyList<Pocket> pockets)
        {
            Vector2 origin = ball.Body.position;
            float radius = ball.Radius;
            direction.Normalize();

            aimLine.enabled = true;
            bool hasHit = Cast(origin, radius, direction, maxDistance, ball.Collider, out RaycastHit2D hit);

            // The path ends early if the ball's center passes over a pocket first.
            if (FirstPocketEntry(ball, origin, direction, hasHit ? hit.distance : maxDistance, pockets, out float pocketDistance))
            {
                Vector2 end = origin + direction * pocketDistance;
                SetLine(aimLine, origin, end);
                DrawGhost(end, radius);
                bounceLine.enabled = objectLine.enabled = false;
                return;
            }

            if (!hasHit)
            {
                SetLine(aimLine, origin, origin + direction * maxDistance);
                bounceLine.enabled = ghostBall.enabled = objectLine.enabled = false;
                return;
            }

            Vector2 contact = hit.centroid;
            SetLine(aimLine, origin, contact);
            DrawGhost(contact, radius);

            Vector2 n = hit.normal;
            float dn = Vector2.Dot(direction, n);
            var objectBall = hit.collider.GetComponent<Ball>();
            // Object ball: frictionless impact along the line of centers. The cue ball keeps its tangent
            // component and a normal part k = (m1 - e m2) / (m1 + m2): 0 for equal masses and e = 1 (stun
            // shot, tangent line), > 0 (follows through a little) when the object ball is lighter.
            float k = 0f;
            if (objectBall != null)
            {
                float m1 = ball.Body.mass, m2 = objectBall.Body.mass;
                float e = ball.Material.bounciness * objectBall.Material.bounciness; // Multiply combine
                k = (m1 - e * m2) / (m1 + m2);
            }
            Vector2 next = objectBall != null
                ? direction - (1f - k) * dn * n
                : direction - (1f + cushionRestitution) * dn * n;  // cushion: damped reflection

            // Object ball leaves along the line of centers (opposite of the contact normal).
            if (objectBall != null)
            {
                Vector2 objectCenter = objectBall.Body.position;
                objectLine.enabled = true;
                SetLine(objectLine, objectCenter, objectCenter - n * objectLineLength);
            }
            else
            {
                objectLine.enabled = false;
            }

            if (next.sqrMagnitude < 1e-6f)
            {
                bounceLine.enabled = false;
                return;
            }
            next.Normalize();

            float length = bounceLength;
            Vector2 start = contact + n * 0.01f;
            if (Cast(start, radius, next, bounceLength, ball.Collider, out RaycastHit2D second))
                length = second.distance;
            if (FirstPocketEntry(ball, contact, next, length, pockets, out float bouncePocket))
                length = bouncePocket;

            bounceLine.enabled = true;
            SetLine(bounceLine, contact, contact + next * length);
        }

        /// <summary>Distance along the ray at which the ball center first enters a pocket's capture circle (for this ball).</summary>
        static bool FirstPocketEntry(Ball ball, Vector2 origin, Vector2 dir, float maxDist, IReadOnlyList<Pocket> pockets, out float distance)
        {
            distance = float.MaxValue;
            if (pockets == null) return false;
            foreach (var pocket in pockets)
            {
                Vector2 m = origin - (Vector2)pocket.transform.position;
                float b = Vector2.Dot(m, dir);
                float r = pocket.CaptureRadiusFor(ball);
                float c = m.sqrMagnitude - r * r;
                if (c > 0f && b > 0f) continue; // outside and moving away
                float disc = b * b - c;
                if (disc < 0f) continue;        // misses the circle
                float t = Mathf.Max(0f, -b - Mathf.Sqrt(disc));
                if (t <= maxDist && t < distance) distance = t;
            }
            return distance < float.MaxValue;
        }

        bool Cast(Vector2 origin, float radius, Vector2 dir, float distance, Collider2D ignore, out RaycastHit2D best)
        {
            best = default;
            int count = Physics2D.CircleCast(origin, radius, dir, filter, hits, distance);
            float bestDistance = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var h = hits[i];
                if (h.collider == ignore || h.distance >= bestDistance) continue;
                best = h;
                bestDistance = h.distance;
            }
            return bestDistance < float.MaxValue;
        }

        static void SetLine(LineRenderer lr, Vector2 a, Vector2 b)
        {
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
        }

        void DrawGhost(Vector2 center, float radius)
        {
            ghostBall.enabled = true;
            ghostBall.transform.position = center; // sprite is sized like the ball sprites (radius unused)
        }
    }
}
