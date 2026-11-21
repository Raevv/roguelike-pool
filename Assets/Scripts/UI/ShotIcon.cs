using UnityEngine;
using UnityEngine.UI;

namespace RoguelikePool
{
    /// <summary>
    /// One icon of the <see cref="ShotsCounter"/> (prefab Assets/Prefabs/UI/ShotIcon). A shot already
    /// taken is greyed by setting the image's saturation to <see cref="usedSaturation"/> (0 = grayscale)
    /// with the UI/RoguelikePool/Saturation shader, so the same code works on the blue placeholder now
    /// and on the final sprite later (put the sprite on the Image and set its color to white).
    /// </summary>
    public class ShotIcon : MonoBehaviour
    {
        [SerializeField] Image image;
        [Tooltip("Shader UI avec saturation (Assets/Art/Shaders/UISaturation).")]
        [SerializeField] Shader saturationShader;
        [Tooltip("Saturation d'un tir déjà utilisé (0 = gris).")]
        [Range(0f, 1f)] [SerializeField] float usedSaturation = 0f;

        static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        Material material;

        public bool Used { get; private set; }

        public void SetUsed(bool used)
        {
            Used = used;
            if (!EnsureMaterial()) return;
            material.SetFloat(SaturationId, used ? usedSaturation : 1f);
        }

        bool EnsureMaterial()
        {
            if (material != null) return true;
            if (image == null) image = GetComponentInChildren<Image>();
            if (saturationShader == null) saturationShader = Shader.Find("UI/RoguelikePool/Saturation");
            if (image == null || saturationShader == null) return false;
            material = new Material(saturationShader) { name = "ShotIconSaturation", hideFlags = HideFlags.DontSave };
            image.material = material; // own instance: each icon has its own saturation
            return true;
        }

        void OnDestroy()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material);
            else DestroyImmediate(material);
        }
    }
}
