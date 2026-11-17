using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Creates the arcade ball set: solids (1-7), black 8, stripes (9-10) and the cue ball, with
    /// pixel-art sprites from <see cref="BallArt"/>, a drop shadow and collision sparks.
    /// Balls never rotate, so numbers stay upright.
    /// </summary>
    public static class BallFactory
    {
        public const float DefaultRadius = 0.446875f; // arcade: regulation 57.15 mm ball × 1.25 × 1.25

        public static bool IsStripe(int number) => number >= 9;

        public static Ball CreateCue(Vector2 position) => Create(0, position);

        public static Ball Create(int number, Vector2 position, float radius = DefaultRadius)
        {
            float diameter = radius * 2f;
            var go = new GameObject(number == 0 ? "CueBall" : "Ball" + number);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;

            var body = go.AddComponent<SpriteRenderer>();
            body.sprite = BallArt.Get(number, diameter);
            body.sortingOrder = 10;

            // Shadow offset: 2 art px right, 2 down (child space is in ball diameters).
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(go.transform, false);
            shadow.transform.localPosition = new Vector2(2f, -2f) * PixelArt.Pixel / diameter;
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = BallArt.Shadow(diameter);
            shadowRenderer.sortingOrder = 8;

            var ball = go.AddComponent<Ball>();
            ball.Number = number;
            go.AddComponent<BallImpactFX>();
            return ball;
        }

        /// <summary>
        /// Arcade 10-ball triangle (balls 1-10, 4 rows): apex on <paramref name="apex"/>, rows growing
        /// toward +Y (away from the head). 1 at the apex, 8 in the middle of the third row, one solid
        /// and one stripe in the back corners, the rest random.
        /// </summary>
        public static List<Ball> CreateRack(Vector2 apex, float radius = DefaultRadius, float gap = 0.002f)
        {
            const int rows = 4;
            var solids = new List<int> { 2, 3, 4, 5, 6, 7 };
            var stripes = new List<int> { 9, 10 };
            var slots = new int[rows * (rows + 1) / 2];
            slots[0] = 1;
            slots[4] = 8;
            bool solidLeft = Random.value < 0.5f;
            slots[6] = TakeRandom(solidLeft ? solids : stripes);
            slots[9] = TakeRandom(solidLeft ? stripes : solids);

            var rest = new List<int>(solids);
            rest.AddRange(stripes);
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == 0) slots[i] = TakeRandom(rest);

            var balls = new List<Ball>(slots.Length);
            float d = radius * 2f + gap;
            float rowStep = d * Mathf.Sqrt(3f) / 2f;
            int slot = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int j = 0; j <= row; j++)
                {
                    var pos = apex + new Vector2((j - row / 2f) * d, row * rowStep);
                    balls.Add(Create(slots[slot++], pos, radius));
                }
            }
            return balls;
        }

        static int TakeRandom(List<int> list)
        {
            int i = Random.Range(0, list.Count);
            int value = list[i];
            list.RemoveAt(i);
            return value;
        }
    }
}
