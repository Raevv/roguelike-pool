using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// A pocket hole. Every physics step, any ball whose center gets within its capture radius falls in.
    /// A distance check is used instead of a trigger so that slow or sleeping bodies are still caught.
    /// Arcade tuning: colored balls are caught slightly beyond the drawn hole (<see cref="objectCaptureScale"/>),
    /// while the cue ball must reach closer to the center (<see cref="cueCaptureScale"/> of the object
    /// ball capture radius), so scratches are rarer than pots.
    /// </summary>
    public class Pocket : MonoBehaviour
    {
        [Tooltip("Rayon du trou dessiné (unités).")]
        public float radius = 0.6f;
        [Tooltip("Rayon de capture des boules de couleur, en multiple du rayon dessiné.")]
        public float objectCaptureScale = 1.1f;
        [Tooltip("Rayon de capture de la blanche, en fraction du rayon de capture des boules de couleur.")]
        [Range(0.1f, 1f)] public float cueCaptureScale = 0.65f;

        public float ObjectCaptureRadius => radius * objectCaptureScale;
        public float CueCaptureRadius => ObjectCaptureRadius * cueCaptureScale;
        public float CaptureRadiusFor(Ball ball) => ball.IsCue ? CueCaptureRadius : ObjectCaptureRadius;

        void FixedUpdate()
        {
            Vector2 center = transform.position;
            for (int i = Ball.All.Count - 1; i >= 0; i--)
            {
                var ball = Ball.All[i];
                if (ball.IsPocketed) continue;
                float r = CaptureRadiusFor(ball);
                if ((ball.Body.position - center).sqrMagnitude < r * r) ball.FallInto(this);
            }
        }
    }
}
