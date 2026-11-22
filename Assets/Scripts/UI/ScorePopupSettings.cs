using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Every setting of the score popups (asset Assets/Data/ScorePopupSettings, assigned to UI/PopupLayer).
    /// Read live: Inspector edits apply in Play Mode (text size, color and outline from the next popup on).
    /// Distances are in canvas units (reference 1080×1920; 2 units = 1 art pixel).
    /// </summary>
    [CreateAssetMenu(fileName = "ScorePopupSettings", menuName = "Roguelike Pool/Score Popup Settings")]
    public class ScorePopupSettings : ScriptableObject
    {
        [Header("Éventail autour de la poche")]
        [Tooltip("Rayon de l'éventail : distance entre la poche et les textes, vers l'extérieur de la table.")]
        public float fanRadius = 110f;
        [Tooltip("Rayon de l'éventail quand les textes vont sur la table (poche du milieu sans place à l'extérieur).")]
        public float inwardFanRadius = 120f;
        [Tooltip("Espace minimal entre deux textes, et entre un texte et un élément d'interface. Couvre aussi le flottement.")]
        public float fanSpacing = 14f;
        [Tooltip("Angle maximal de l'éventail de chaque côté de sa direction (degrés).")]
        [Range(10f, 180f)] public float maxFanAngle = 110f;
        [Tooltip("Anneaux successifs quand l'arc est plein (chacun plus loin d'un texte).")]
        [Min(1)] public int maxRings = 4;
        [Tooltip("Place minimale entre la poche et le bord du cadre, de son côté extérieur. En dessous : poche de coin → textes au-dessus / en dessous d'elle, poche du milieu → textes sur la table.")]
        public float minSideRoom = 200f;
        [Tooltip("Marge entre un texte et le bord du cadre du jeu (Safe Area).")]
        public float screenMargin = 12f;

        [Header("Apparition (pop)")]
        [Tooltip("Durée du pop, en secondes.")]
        [Min(0.01f)] public float popDuration = 0.15f;
        [Tooltip("Échelle du texte pendant le pop (temps 0 → 1). Par défaut 0 → 1,2 → 1.")]
        public AnimationCurve popCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 3f), new Keyframe(0.6f, 1.2f, 0f, 0f), new Keyframe(1f, 1f, 0f, 0f));

        [Header("Flottement")]
        [Tooltip("Amplitude du flottement vertical (2 = 1 pixel d'art).")]
        public float floatAmplitude = 6f;
        [Tooltip("Vitesse du flottement (oscillations par seconde).")]
        public float floatSpeed = 0.8f;
        [Tooltip("Inclinaison aléatoire maximale (degrés, ±).")]
        [Range(0f, 30f)] public float maxTilt = 5f;
        [Tooltip("Balancement autour de l'inclinaison (degrés, ±).")]
        [Range(0f, 15f)] public float tiltSway = 1.5f;

        [Header("Attente avant la fusion")]
        [Tooltip("Vitesse sous laquelle une boule compte comme immobile (unités/s). La fusion part quand toutes le sont.")]
        [Min(0f)] public float stillSpeed = 0.5f;
        [Tooltip("Délai maximal d'attente après le tir (s) : la fusion part même si des boules roulent encore.")]
        [Min(0.5f)] public float maxWait = 12f;

        [Header("Fusion vers le score")]
        [Tooltip("Durée totale maximale de la fusion (s) : le délai entre les textes raccourcit s'il y en a beaucoup.")]
        [Min(0.05f)] public float mergeMaxDuration = 1f;
        [Tooltip("Délai entre le départ de deux textes (s), s'il tient dans la durée maximale.")]
        [Min(0f)] public float delayBetween = 0.15f;
        [Tooltip("Durée du vol d'un texte jusqu'au score (s), plafonnée par la durée maximale.")]
        [Min(0.01f)] public float flightDuration = 0.4f;
        [Tooltip("Courbe d'accélération du vol (temps 0 → 1, distance 0 → 1). Par défaut : départ lent, arrivée rapide.")]
        public AnimationCurve flightCurve = new AnimationCurve(new Keyframe(0f, 0f, 0f, 0f), new Keyframe(1f, 1f, 2.2f, 2.2f));
        [Tooltip("Échelle du texte pendant le vol (temps 0 → 1). Par défaut il rétrécit de 1 à 0,7.")]
        public AnimationCurve flightScaleCurve = AnimationCurve.Linear(0f, 1f, 1f, 0.7f);

        [Header("Pulsation du score à chaque arrivée")]
        [Tooltip("Agrandissement maximal du texte du score (0,2 = +20 %).")]
        [Min(0f)] public float scorePulseAmount = 0.2f;
        [Tooltip("Durée d'une pulsation (s).")]
        [Min(0.01f)] public float scorePulseDuration = 0.15f;

        [Header("Texte")]
        [Tooltip("Taille du texte pour 1 point. VeniceClassic est net aux multiples de 19 (76 = 4×).")]
        [Min(1f)] public float fontSize = 76f;
        [Tooltip("Taille ajoutée par point au-delà du premier.")]
        [Min(0f)] public float fontSizePerPoint = 19f;
        [Tooltip("Taille maximale, quelle que soit la valeur.")]
        [Min(1f)] public float maxFontSize = 114f;
        [Tooltip("Couleur du texte (une couleur donnée par le popup lui-même, pour un bonus plus tard, passe avant).")]
        public Color textColor = Color.white;
        [Tooltip("Couleur du contour.")]
        public Color outlineColor = new Color32(0x14, 0x0c, 0x1c, 0xff);
        [Tooltip("Épaisseur du contour (0 = pas de contour : seule l'ombre portée de la police reste ; 0,1 à 0,3 = contour).")]
        [Range(0f, 1f)] public float outlineWidth = 0f;

        /// <summary>Font size for a popup worth this many points (grows with the value, capped).</summary>
        public float FontSizeFor(int points) =>
            Mathf.Min(maxFontSize, fontSize + Mathf.Max(0, points - 1) * fontSizePerPoint);

        static ScorePopupSettings defaults;

        /// <summary>Code defaults, when no asset is assigned.</summary>
        public static ScorePopupSettings Defaults
        {
            get
            {
                if (defaults == null)
                {
                    defaults = CreateInstance<ScorePopupSettings>();
                    defaults.name = "ScorePopupSettings (code defaults)";
                    defaults.hideFlags = HideFlags.DontSave;
                }
                return defaults;
            }
        }
    }
}
