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
        /// <summary>Text color (alpha 0 = the color of <see cref="ScorePopupSettings"/>).</summary>
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
    /// then floats forever: a slow vertical bob of a few pixels and a small random tilt. All values come
    /// from <see cref="ScorePopupSettings"/>.
    /// </summary>
    public class ScorePopup : MonoBehaviour
    {
        [SerializeField] TMP_Text text;

        /// <summary>Set by the layer; every value (pop, float, size, color, outline) is read from it.</summary>
        public ScorePopupSettings Settings { get; set; }
        public ScorePopupData Data { get; private set; }
        public RectTransform Rect => (RectTransform)transform;
        /// <summary>The pocket this popup belongs to (the layer keeps it next to that pocket).</summary>
        public Pocket Pocket { get; set; }
        /// <summary>Offset from the pocket's screen position, in canvas units of the layer.</summary>
        public Vector2 Offset { get; set; }
        /// <summary>Text size used for placement (set by the layer when shown).</summary>
        public Vector2 Size { get; set; }

        ScorePopupSettings S => Settings != null ? Settings : ScorePopupSettings.Defaults;

        float age, phase, tilt;
        bool initialized;
        Material outlineMaterial;

        void Init()
        {
            if (initialized) return;
            initialized = true;
            if (text == null) text = GetComponentInChildren<TMP_Text>(true);
            text.raycastTarget = false;
        }

        void OnDestroy()
        {
            if (outlineMaterial != null) Destroy(outlineMaterial);
        }

        /// <summary>Size of the shown text (canvas units, at scale 1, without tilt).</summary>
        public Vector2 TextSize
        {
            get
            {
                Init();
                // Drawn glyphs only (preferred values include the line height, far taller than "+1").
                text.ForceMeshUpdate();
                Vector2 size = text.textBounds.size;
                return size.x > 0f ? size : text.GetPreferredValues(text.text);
            }
        }

        public void Show(ScorePopupData data)
        {
            Init();
            var s = S;
            Data = data;
            text.text = string.IsNullOrEmpty(data.label)
                ? GameTexts.Format(GameTexts.Current.popupPointsFormat, data.points)
                : data.label;
            text.fontSize = s.FontSizeFor(data.points);
            text.color = data.color.a > 0f ? data.color : s.textColor;
            ApplyOutline(s);
            age = 0f;
            transform.localScale = Vector3.one; // a pooled popup may come back shrunk by its flight
            phase = Random.value * Mathf.PI * 2f;
            tilt = Random.Range(-s.maxTilt, s.maxTilt);
            gameObject.SetActive(true);
            Animate();
        }

        /// <summary>
        /// No outline: the font's own material (with its drop shadow, or the shadowless preset for a dark
        /// text). Outline: one material per popup, a copy of the font material with the outline on.
        /// </summary>
        void ApplyOutline(ScorePopupSettings s)
        {
            if (s.outlineWidth <= 0f || text.font == null)
            {
                if (text.font != null && text.fontSharedMaterial != text.font.material) text.fontSharedMaterial = text.font.material;
                TextShadow.Fit(text);
                return;
            }
            if (outlineMaterial == null)
            {
                outlineMaterial = new Material(text.font.material) { name = "ScorePopup Outline (instance)", hideFlags = HideFlags.DontSave };
                outlineMaterial.EnableKeyword(ShaderUtilities.Keyword_Outline);
            }
            // TMP centers the outline on the glyph edge: its inner half would eat the thin pixel strokes.
            // Dilating the face by the same amount keeps the whole outline outside the letters.
            outlineMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, s.outlineWidth);
            outlineMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, s.outlineWidth);
            outlineMaterial.SetColor(ShaderUtilities.ID_OutlineColor, s.outlineColor);
            ShaderUtilities.UpdateShaderRatios(outlineMaterial);
            if (text.fontSharedMaterial != outlineMaterial) text.fontSharedMaterial = outlineMaterial;
        }

        void Update()
        {
            age += Time.deltaTime;
            Animate();
        }

        void Animate()
        {
            var s = S;
            var t = text.rectTransform;
            float u = age / Mathf.Max(s.popDuration, 0.0001f);
            float scale = u >= 1f || s.popCurve == null || s.popCurve.length == 0 ? 1f : s.popCurve.Evaluate(u);
            t.localScale = Vector3.one * scale;
            float bob = Mathf.Sin(age * s.floatSpeed * Mathf.PI * 2f + phase) * s.floatAmplitude;
            t.anchoredPosition = new Vector2(0f, bob);
            // The tilt sways a little around its random angle.
            t.localRotation = Quaternion.Euler(0f, 0f, tilt + Mathf.Sin(age * s.floatSpeed * Mathf.PI + phase) * s.tiltSway);
        }
    }
}
