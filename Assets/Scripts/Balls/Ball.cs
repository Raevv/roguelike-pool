using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// A pool ball on a frictionless Rigidbody2D. Cloth friction is applied manually so the ball
    /// decelerates like a real rolling ball (mostly constant deceleration, plus a small
    /// speed-proportional drag) and snaps to a full stop below <see cref="stopSpeed"/>.
    /// Every active ball is registered in <see cref="All"/>; a pocketed ball plays a short fall
    /// animation, then is deactivated (and so leaves <see cref="All"/>).
    /// Number 0 is the cue ball, 1-7 solids, 8 the black, 9-10 stripes.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Ball : MonoBehaviour
    {
        public static readonly List<Ball> All = new List<Ball>();

        /// <summary>Raised when a ball starts falling into a pocket.</summary>
        public static event Action<Ball, Pocket> Pocketed;

        /// <summary>Domain reload is disabled on Play: reset static state at the start of each session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            All.Clear();
            Pocketed = null;
        }

        public static bool AllStopped
        {
            get
            {
                foreach (var ball in All)
                    if (ball.IsMoving) return false;
                return true;
            }
        }

        [Header("Friction du tapis (unités/s, 1 unité = 10 cm)")]
        [Tooltip("Décélération constante de roulement (u/s²).")]
        public float rollingDeceleration = 2.0f;
        [Tooltip("Freinage proportionnel à la vitesse (1/s). Raccourcit surtout les tirs puissants.")]
        public float speedDrag = 0.25f;
        [Tooltip("En dessous de cette vitesse, la boule s'arrête net.")]
        public float stopSpeed = 0.08f;

        [Header("Rebonds")]
        [Range(0f, 1f)] public float bounciness = 0.97f;
        public float mass = 0.17f;

        [Header("Empochage")]
        public float fallDuration = 0.18f;

        /// <summary>Physics layer of the cue ball (unnamed user layer). Cue-only jaw guards exclude every other layer.</summary>
        public const int CueLayer = 6;

        int number;
        public int Number
        {
            get => number;
            set { number = value; gameObject.layer = value == 0 ? CueLayer : 0; }
        }
        public bool IsCue => Number == 0;
        public bool IsPocketed { get; private set; }

        public Rigidbody2D Body { get; private set; }
        public CircleCollider2D Collider { get; private set; }
        public PhysicsMaterial2D Material { get; private set; }

        public float Radius => Collider.radius * Mathf.Abs(baseScale.x);
        public bool IsMoving => !IsPocketed && Body.linearVelocity.sqrMagnitude > 0f;

        Vector3 baseScale;

        void Awake()
        {
            baseScale = transform.localScale;
            Material = new PhysicsMaterial2D(name + "_Material")
            {
                bounciness = bounciness,
                friction = 0f,
                bounceCombine = PhysicsMaterialCombine2D.Multiply,
                frictionCombine = PhysicsMaterialCombine2D.Multiply
            };

            Collider = GetComponent<CircleCollider2D>();
            Collider.radius = 0.5f; // sprite is 1 unit wide; the transform scale sets the real diameter
            Collider.sharedMaterial = Material;

            Body = GetComponent<Rigidbody2D>();
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = 0f;
            Body.mass = mass;
            Body.linearDamping = 0f; // friction handled in FixedUpdate
            Body.angularDamping = 0f;
            Body.freezeRotation = true;
            Body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            Body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Body.sharedMaterial = Material;
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void FixedUpdate()
        {
            if (IsPocketed) return;
            Vector2 v = Body.linearVelocity;
            float speed = v.magnitude;
            if (speed <= 0f) return;

            float newSpeed = speed - (rollingDeceleration + speedDrag * speed) * Time.fixedDeltaTime;
            if (newSpeed <= stopSpeed)
            {
                Body.linearVelocity = Vector2.zero;
                Body.angularVelocity = 0f;
                return;
            }
            Body.linearVelocity = v * (newSpeed / speed);
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            // Arcade: a ball bouncing off a cushion/jaw near a pocket is steered toward the hole
            // (strongly for colored balls, mildly for the cue ball and only at the side pockets).
            if (IsPocketed || collision.rigidbody != null || collision.contactCount == 0) return;
            Pocket.FunnelBounce(this, collision.GetContact(0).point);
        }

        public void Strike(Vector2 velocity)
        {
            Body.WakeUp();
            Body.linearVelocity = velocity;
        }

        public void FallInto(Pocket pocket)
        {
            if (IsPocketed) return;
            IsPocketed = true;
            Body.linearVelocity = Vector2.zero;
            Body.simulated = false;
            StartCoroutine(FallRoutine(pocket.transform.position));
            Pocketed?.Invoke(this, pocket);
        }

        IEnumerator FallRoutine(Vector2 pocketCenter)
        {
            Vector2 start = transform.position;
            for (float t = 0f; t < fallDuration; t += Time.deltaTime)
            {
                float k = t / fallDuration;
                transform.position = Vector2.Lerp(start, pocketCenter, k);
                transform.localScale = baseScale * Mathf.Lerp(1f, 0.55f, k);
                yield return null;
            }
            gameObject.SetActive(false);
        }

        /// <summary>Puts the ball back on the table at rest (used when a shot is cancelled).</summary>
        public void ResetTo(Vector2 position)
        {
            StopAllCoroutines();
            gameObject.SetActive(true);
            IsPocketed = false;
            transform.localScale = baseScale;
            transform.position = position;
            Body.simulated = true;
            Body.position = position;
            Body.linearVelocity = Vector2.zero;
            Body.angularVelocity = 0f;
        }
    }
}
