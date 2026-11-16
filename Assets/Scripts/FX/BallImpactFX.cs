using UnityEngine;

namespace RoguelikePool
{
    /// <summary>Plays <see cref="ImpactFX"/> when this ball hits another ball (once per pair). Visual only.</summary>
    public class BallImpactFX : MonoBehaviour
    {
        void OnCollisionEnter2D(Collision2D collision)
        {
            var other = collision.collider.GetComponent<Ball>();
            if (other == null || ImpactFX.Instance == null) return;
            if (GetComponent<Ball>().Number > other.Number) return; // numbers are unique: the other ball plays it

            var contact = collision.GetContact(0);
            ImpactFX.Instance.Emit(contact.point, contact.normal, collision.relativeVelocity.magnitude);
        }
    }
}
