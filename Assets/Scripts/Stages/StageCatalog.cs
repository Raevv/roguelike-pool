using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// The ordered list of stages of a run (stage 1 first). Reorder, add or remove stages in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "StageCatalog", menuName = "Roguelike Pool/Stage Catalog")]
    public class StageCatalog : ScriptableObject
    {
        public List<StageData> stages = new List<StageData>();

        public int Count => stages.Count;

        /// <summary>Stage at a 0-based index, or null if out of range.</summary>
        public StageData Get(int index) => index >= 0 && index < stages.Count ? stages[index] : null;
    }
}
