using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// A pocket hole. Every physics step, any ball whose center is over the hole
    /// (closer than <see cref="captureRadius"/>) falls in. A distance check is used instead of a
    /// trigger so that slow or sleeping bodies are still caught.
    /// </summary>
    public class Pocket : MonoBehaviour
    {
        public float captureRadius = 0.6f;

        void FixedUpdate()
        {
            Vector2 center = transform.position;
            float sqr = captureRadius * captureRadius;
            for (int i = Ball.All.Count - 1; i >= 0; i--)
            {
                var ball = Ball.All[i];
                if (ball.IsPocketed) continue;
                if ((ball.Body.position - center).sqrMagnitude < sqr) ball.FallInto(this);
            }
        }
    }
}
