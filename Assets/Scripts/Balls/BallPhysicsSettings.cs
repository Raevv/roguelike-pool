using System;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Every ball physics tuning value in one asset (Assets/Data/BallPhysicsSettings): shot power, then
    /// mass / cloth friction / bounciness for the cue ball and for the colored balls separately, and the
    /// cushion bounciness. <see cref="GameBootstrap"/> makes it <see cref="Current"/> before building the
    /// table and the balls; without an asset, the code defaults below are used.
    /// Edits made in the Inspector during Play Mode apply immediately (see <see cref="Changed"/>) and,
    /// since it is an asset, they are kept after leaving Play Mode.
    /// Restitution uses the Multiply combine: ball-ball = bounciness A × bounciness B,
    /// ball-cushion = ball bounciness × cushion bounciness.
    /// Pocket difficulty (capture radii, guards, assists) is not here: see <see cref="Pocket"/> / <see cref="PoolTable"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "Roguelike Pool/Ball Physics Settings", fileName = "BallPhysicsSettings")]
    public class BallPhysicsSettings : ScriptableObject
    {
        [Serializable]
        public class BallParams
        {
            [Tooltip("Masse (kg). Une boule de couleur plus légère que la blanche part plus vite au choc, et la blanche continue un peu vers l'avant après un choc plein.")]
            [Min(0.01f)] public float mass = 0.17f;
            [Tooltip("Frottement du tapis : décélération constante (u/s²). Plus bas = la boule roule plus longtemps, surtout à faible vitesse.")]
            [Min(0f)] public float rollingDeceleration = 2f;
            [Tooltip("Amortissement linéaire (Linear Drag, 1/s) : freinage proportionnel à la vitesse. Plus bas = les tirs puissants vont plus loin.")]
            [Min(0f)] public float linearDrag = 0.25f;
            [Tooltip("Rebond (0 à 1). Multiplié par celui de l'autre boule au choc, et par celui des bandes sur une bande.")]
            [Range(0f, 1f)] public float bounciness = 0.97f;

            public BallParams() { }
            public BallParams(float mass, float rollingDeceleration, float linearDrag, float bounciness)
            {
                this.mass = mass;
                this.rollingDeceleration = rollingDeceleration;
                this.linearDrag = linearDrag;
                this.bounciness = bounciness;
            }
        }

        [Header("Tir")]
        [Tooltip("Max Shot Speed : vitesse de la blanche à 100 % de puissance (u/s, 1 u = 10 cm → 80 = 8 m/s).")]
        [Min(1f)] public float maxShotSpeed = 80f;

        [Header("Blanche")]
        [Tooltip("Réglages de la boule blanche.")]
        public BallParams cueBall = new BallParams(0.35f, 15f, 0.1f, 1f);

        [Header("Boules de couleur")]
        [Tooltip("Réglages des boules 1 à 10 : plus légères et moins freinées que la blanche pour des combos.")]
        public BallParams coloredBalls = new BallParams(0.15f, 10f, 0.12f, 1f);

        [Header("Commun")]
        [Tooltip("Rebond des bandes (0 à 1), multiplié par celui de la boule.")]
        [Range(0f, 1f)] public float cushionBounciness = 0.86f;
        [Tooltip("En dessous de cette vitesse (u/s), une boule s'arrête net.")]
        [Min(0f)] public float stopSpeed = 0.8f;
        // Angular damping: not exposed, balls never rotate (Rigidbody2D.freezeRotation; no spin yet).

        public BallParams For(Ball ball) => ball.IsCue ? cueBall : coloredBalls;

        /// <summary>Raised when a value is edited in the Inspector (or <see cref="Current"/> is replaced).</summary>
        public static event Action Changed;

        static BallPhysicsSettings active, fallback;

        /// <summary>Settings in use: the asset given by <see cref="GameBootstrap"/>, else code defaults.</summary>
        public static BallPhysicsSettings Current
        {
            get
            {
                if (active != null) return active;
                if (fallback == null)
                {
                    fallback = CreateInstance<BallPhysicsSettings>();
                    fallback.hideFlags = HideFlags.DontSave;
                }
                return fallback;
            }
            set { active = value; Changed?.Invoke(); }
        }

        /// <summary>Domain reload is disabled on Play: reset static state at the start of each session.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            active = null;
            Changed = null;
        }

        void OnValidate()
        {
            if (Application.isPlaying && this == active) Changed?.Invoke();
        }
    }
}
