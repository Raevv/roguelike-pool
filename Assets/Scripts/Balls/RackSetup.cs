using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Which ball goes on each spot of the triangle (Assets/Data/RackSetup). Spots are listed row by row from
    /// the apex (the spot nearest the cue ball), each row from left to right:
    /// <code>
    ///   row 4:  6  7  8  9
    ///   row 3:   3  4  5
    ///   row 2:    1  2
    ///   row 1:     0        (apex, toward the cue ball)
    /// </code>
    /// </summary>
    [CreateAssetMenu(fileName = "RackSetup", menuName = "Roguelike Pool/Rack Setup")]
    public class RackSetup : ScriptableObject
    {
        public const int Rows = 4;
        public const int SpotCount = Rows * (Rows + 1) / 2;

        [Tooltip("Une bille par position du triangle, rangée par rangée depuis la pointe (côté blanche), de gauche à droite :\n" +
                 "0 = pointe ; 1-2 = rangée 2 ; 3-5 = rangée 3 (4 = centre) ; 6-9 = rangée du fond.\n" +
                 "Une case vide laisse la position vide.")]
        public List<BallData> spots = new List<BallData>(new BallData[SpotCount]);

        void OnValidate()
        {
            // Always exactly one entry per spot of the triangle.
            while (spots.Count < SpotCount) spots.Add(null);
            if (spots.Count > SpotCount) spots.RemoveRange(SpotCount, spots.Count - SpotCount);
        }
    }
}
