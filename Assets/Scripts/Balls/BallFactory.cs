using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Creates the balls from the Ball / CueBall prefabs (Assets/Prefabs/Balls) and the BallData assets:
    /// the prefab holds the physics components and the sprite renderer, the BallData the number and the sprite.
    /// Adds the drop shadow. Balls never rotate, so numbers stay upright.
    /// </summary>
    public static class BallFactory
    {
        public const float DefaultRadius = 0.446875f; // arcade: regulation 57.15 mm ball × 1.25 × 1.25

        public static bool IsStripe(int number) => number >= 9;

        public static Ball CreateCue(GameObject cueBallPrefab, Vector2 position)
        {
            var ball = Spawn(cueBallPrefab, position, "CueBall");
            ball.Number = 0;
            return ball;
        }

        public static Ball Create(GameObject ballPrefab, BallData data, Vector2 position)
        {
            var ball = Spawn(ballPrefab, position, "Ball" + data.number);
            ball.Data = data;
            ball.Number = data.number;
            var visual = ball.Visual;
            if (visual != null && data.sprite != null) visual.sprite = data.sprite;
            return ball;
        }

        static Ball Spawn(GameObject prefab, Vector2 position, string name)
        {
            var go = Object.Instantiate(prefab, position, Quaternion.identity);
            go.name = name;
            AddShadow(go.transform);
            return go.GetComponent<Ball>();
        }

        /// <summary>Drop shadow: 2 art px right, 2 down (child space is in ball diameters, the root is scaled by the diameter).</summary>
        static void AddShadow(Transform ball)
        {
            float diameter = DefaultRadius * 2f;
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(ball, false);
            shadow.transform.localPosition = new Vector2(2f, -2f) * PixelArt.Pixel / diameter;
            var renderer = shadow.AddComponent<SpriteRenderer>();
            renderer.sprite = BallArt.Shadow(diameter);
            renderer.sortingOrder = 8;
        }

        /// <summary>
        /// Arcade 10-ball triangle (4 rows): apex on <paramref name="apex"/>, rows growing toward +Y (away from
        /// the head). Each spot gets the ball the <see cref="RackSetup"/> lists for it (an empty entry leaves it empty).
        /// </summary>
        public static List<Ball> CreateRack(GameObject ballPrefab, RackSetup setup, Vector2 apex,
                                            float radius = DefaultRadius, float gap = 0.002f)
        {
            var balls = new List<Ball>(RackSetup.SpotCount);
            float d = radius * 2f + gap;
            float rowStep = d * Mathf.Sqrt(3f) / 2f;
            int spot = 0;
            for (int row = 0; row < RackSetup.Rows; row++)
            {
                for (int j = 0; j <= row; j++, spot++)
                {
                    var data = spot < setup.spots.Count ? setup.spots[spot] : null;
                    if (data == null) continue;
                    var pos = apex + new Vector2((j - row / 2f) * d, row * rowStep);
                    balls.Add(Create(ballPrefab, data, pos));
                }
            }
            return balls;
        }
    }
}
