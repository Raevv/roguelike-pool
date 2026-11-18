using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// A pocket hole. Every physics step, any ball whose center gets within its capture radius falls in.
    /// A distance check is used instead of a trigger so that slow or sleeping bodies are still caught.
    ///
    /// Arcade tuning (colored balls helped, cue ball hindered):
    /// - Colored balls are caught well beyond the drawn hole (<see cref="objectCaptureScale"/>); the cue
    ///   ball must reach much closer to the center (<see cref="cueCaptureScale"/> of the object radius).
    /// - Moving colored balls near the pocket are gently pulled toward it (<see cref="assistStrength"/>).
    /// - When a colored ball bounces off a cushion/jaw near the pocket, its rebound is bent toward the
    ///   hole (<see cref="FunnelBounce"/>, called by <see cref="Ball"/>).
    /// - The cue ball also meets invisible cue-only jaw extensions (see <see cref="PoolTable"/>).
    /// </summary>
    public class Pocket : MonoBehaviour
    {
        public static readonly List<Pocket> Active = new List<Pocket>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();

        [Tooltip("Rayon du trou dessiné (unités).")]
        public float radius = 0.6f;
        [Tooltip("Rayon de capture des boules de couleur, en multiple du rayon dessiné.")]
        public float objectCaptureScale = 1.35f;
        [Tooltip("Rayon de capture de la blanche, en fraction du rayon de capture des boules de couleur.")]
        [Range(0.1f, 1f)] public float cueCaptureScale = 0.45f;

        [Header("Aide arcade (boules de couleur uniquement)")]
        [Tooltip("Portée de l'aspiration, en multiple du rayon dessiné.")]
        public float assistRadiusScale = 2.2f;
        [Tooltip("Accélération vers le trou au plus près (u/s²), décroît linéairement jusqu'au bord de la portée.")]
        public float assistStrength = 7f;
        [Tooltip("Portée de la déviation des rebonds de bande/mâchoire, en multiple du rayon dessiné.")]
        public float funnelRadiusScale = 2.3f;
        [Tooltip("Part du rebond redirigée vers le trou (0 = rebond normal, 1 = droit dans le trou).")]
        [Range(0f, 1f)] public float funnelStrength = 0.6f;
        [Tooltip("Pareil pour la blanche (0 = aucune aide). Réglé par PoolTable pour les poches du milieu uniquement.")]
        [Range(0f, 1f)] public float cueFunnelStrength = 0f;

        public float ObjectCaptureRadius => radius * objectCaptureScale;
        public float CueCaptureRadius => ObjectCaptureRadius * cueCaptureScale;
        public float CaptureRadiusFor(Ball ball) => ball.IsCue ? CueCaptureRadius : ObjectCaptureRadius;

        /// <summary>Keeps the (informative) trigger circle the same size as the drawn hole when Radius is edited.</summary>
        void OnValidate()
        {
            if (TryGetComponent(out CircleCollider2D circle)) circle.radius = radius;
        }

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        void FixedUpdate()
        {
            Vector2 center = transform.position;
            float assistRadius = radius * assistRadiusScale;
            for (int i = Ball.All.Count - 1; i >= 0; i--)
            {
                var ball = Ball.All[i];
                if (ball.IsPocketed) continue;
                Vector2 toHole = center - ball.Body.position;
                float d = toHole.magnitude;
                float r = CaptureRadiusFor(ball);
                if (d < r) { ball.FallInto(this); continue; }

                // Aspiration: only moving colored balls, so nothing creeps in from rest.
                if (!ball.IsCue && ball.IsMoving && d < assistRadius)
                {
                    float k = 1f - d / assistRadius;
                    ball.Body.linearVelocity += toHole / d * (assistStrength * k * Time.fixedDeltaTime);
                }
            }
        }

        /// <summary>
        /// Called right after a ball bounced off a table collider at <paramref name="contact"/>.
        /// If the contact is near a pocket, the post-bounce velocity is bent toward that pocket
        /// (by <see cref="funnelStrength"/> for colored balls, <see cref="cueFunnelStrength"/> for the cue ball).
        /// </summary>
        public static void FunnelBounce(Ball ball, Vector2 contact)
        {
            Vector2 v = ball.Body.linearVelocity;
            float speed = v.magnitude;
            if (speed < 1e-3f) return;

            Pocket best = null;
            float bestDist = float.MaxValue;
            foreach (var p in Active)
            {
                float dist = Vector2.Distance(contact, p.transform.position);
                if (dist < p.radius * p.funnelRadiusScale && dist < bestDist) { best = p; bestDist = dist; }
            }
            if (best == null) return;
            float strength = ball.IsCue ? best.cueFunnelStrength : best.funnelStrength;
            if (strength <= 0f) return;

            Vector2 pos = ball.Body.position;
            Vector2 toHole = ((Vector2)best.transform.position - pos).normalized;
            Vector2 dir = v / speed;
            if (Vector2.Dot(dir, toHole) > 0.999f) return;
            Vector2 bent = Vector2.Lerp(dir, toHole, strength);
            if (bent.sqrMagnitude < 1e-6f) bent = toHole;
            bent.Normalize();
            // Never push the ball back into the surface it just bounced off.
            Vector2 away = (pos - contact).normalized;
            if (Vector2.Dot(bent, away) < 0f) bent = (bent - Vector2.Dot(bent, away) * away).normalized;
            ball.Body.linearVelocity = bent * speed;
        }
    }
}
