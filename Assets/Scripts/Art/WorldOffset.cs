using UnityEngine;

namespace RoguelikePool
{
    /// <summary>Keeps a child at a fixed world-space offset from its parent (for drop shadows, e.g. the cue's).</summary>
    public class WorldOffset : MonoBehaviour
    {
        public Vector2 offset;

        void LateUpdate()
        {
            if (transform.parent == null) return;
            transform.position = (Vector2)transform.parent.position + offset;
            transform.rotation = transform.parent.rotation;
        }
    }
}
