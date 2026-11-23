using UnityEngine;

namespace RoguelikePool
{
    public enum BallType
    {
        [InspectorName("Pleine")] Solid,
        [InspectorName("Rayée")] Stripe
    }

    /// <summary>
    /// Data of one colored ball (Assets/Data/Balls/Ball_N): its number, its sprite and its type.
    /// The number and the type change no rule yet, they are only stored.
    /// </summary>
    [CreateAssetMenu(fileName = "Ball_", menuName = "Roguelike Pool/Ball Data")]
    public class BallData : ScriptableObject
    {
        [Tooltip("Numéro de la bille (1 à 10).")]
        [Range(1, 10)] public int number = 1;
        [Tooltip("Sprite de la bille (Assets/Art/Sprites/Balls).")]
        public Sprite sprite;
        [Tooltip("Pleine ou Rayée. Ne change aucune règle pour l'instant.")]
        public BallType type = BallType.Solid;
    }
}
