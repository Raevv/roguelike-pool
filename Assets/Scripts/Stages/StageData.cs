using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// One stage of a run: reach <see cref="targetScore"/> points within <see cref="maxShots"/> shots.
    /// Assets live in Assets/Data/Stages and are ordered by a <see cref="StageCatalog"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "Stage", menuName = "Roguelike Pool/Stage Data")]
    public class StageData : ScriptableObject
    {
        [Tooltip("Points à atteindre pour gagner le stage.")]
        [Min(1)] public int targetScore = 100;

        [Tooltip("Nombre maximum de tirs pour atteindre l'objectif.")]
        [Min(1)] public int maxShots = 5;
    }
}
