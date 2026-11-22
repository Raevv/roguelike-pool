using TMPro;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>What a score popup shows. <see cref="label"/> and <see cref="color"/> are for later (bonuses such as "TIR LONG").</summary>
    public struct ScorePopupData
    {
        public int points;
        /// <summary>Optional text replacing "+points" (empty = "+points" from <see cref="GameTexts.popupPointsFormat"/>).</summary>
        public string label;
        /// <summary>Text color (alpha 0 = the prefab's color).</summary>
        public Color color;

        public ScorePopupData(int points, string label = null, Color color = default)
        {
            this.points = points;
            this.label = label;
            this.color = color;
        }
    }

    /// <summary>
    /// One floating score text (prefab Assets/Prefabs/UI/ScorePopup, pooled by <see cref="ScorePopupLayer"/>).
    /// The root is placed by the layer; the <see cref="text"/> child pops in (scale 0 → overshoot → 1)
    /// then floats forever: a slow vertical bob of a few pixels and a small random tilt.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] TMP_Text text;

        [Header("Apparition")]
        [Tooltip("Durée du pop (échelle 0 → dépassement → 1), en secondes.")]
        public float popDuration = 0.15f;
        [Tooltip("Échelle maximale atteinte pendant le pop.")]
        public float popOvershoot = 1.2f;

        [Header("Flottement")]
        [Tooltip("Amplitude du flottement vertical (unités du canvas, 2 = 1 pixel d'art).")]
        public float floatAmplitude = 6f;
        [Tooltip("Vitesse du flottement (oscillations par seconde).")]
        public float floatSpeed = 0.8f;
        [Tooltip("Inclinaison aléatoire maximale (degrés, ±).")]
        public float maxTilt = 5f;

        public ScorePopupData Data { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        /// <summary>The pocket this popup belongs to (the layer keeps it next to that pocket).</summary>
        public Pocket Pocket { get; set; }
        /// <summary>Offset from the pocket's screen position, in canvas units of the layer.</summary>
        public Vector2 Offset { get; set; }

        float age, phase, tilt;
        Color defaultColor;
        bool initialized;

        void Init()
        {
            if (initialized) return;
            initialized = true;
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
            text.raycastTarget = false;
            defaultColor = text.color;
        }

        public void Show(ScorePopupData data)
        {
            Init();
            Data = data;
            text.text = string.IsNullOrEmpty(data.label)
                ? GameTexts.Format(GameTexts.Current.popupPointsFormat, data.points)
                : data.label;
            text.color = data.color.a > 0f ? data.color : defaultColor;
            TextShadow.Fit(text);
            age = 0f;
            phase = Random.value * Mathf.PI * 2f;
            tilt = Random.Range(-maxTilt, maxTilt);
            gameObject.SetActive(true);
            Animate();
        }

        void Update()
        {
            age += Time.deltaTime;
            Animate();
        }

        void Animate()
        {
            var t = text.rectTransform;
            t.localScale = Vector3.one * PopScale(age / Mathf.Max(popDuration, 0.0001f));
            float bob = Mathf.Sin(age * floatSpeed * Mathf.PI * 2f + phase) * floatAmplitude;
            t.anchoredPosition = new Vector2(0f, bob);
            // The tilt sways a little around its random angle.
            t.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(age * floatSpeed * Mathf.PI + phase) * 1.5f);
        }

        /// <summary>0 → overshoot over the first 60 %, overshoot → 1 over the rest, then 1.</summary>
        float PopScale(float u)
        {
            if (u >= 1f) return 1f;
            const float peak = 0.6f;
            if (u < peak) return Mathf.SmoothStep(0f, popOvershoot, u / peak);
            return Mathf.SmoothStep(popOvershoot, 1f, (u - peak) / (1f - peak));
        }
    }
}
