using System.Linq;
using System.Text;
using UnityEngine;
using RoguelikePool;

/// <summary>
/// Play Mode: checks that the prefab balls have exactly the physics the code-built balls had
/// (scale = diameter, collider radius 0.5 / offset 0, mass, bounciness, layer, body settings), that they sit on
/// the old layout (head spot, triangle with the same spacing), and that the visible sprite equals the collider.
/// </summary>
public static class BallSpritesCheck
{
    public static string Run()
    {
        var sb = new StringBuilder();
        var table = Object.FindAnyObjectByType<PoolTable>();
        const float r = BallFactory.DefaultRadius;
        float d = r * 2f + 0.002f, rowStep = d * Mathf.Sqrt(3f) / 2f;
        int problems = 0;

        foreach (var b in Ball.All.OrderBy(x => x.Number))
        {
            var col = b.Collider; var body = b.Body; var p = BallPhysicsSettings.Current.For(b);
            var v = b.Visual;
            float visiblePx = b.IsCue ? 244 : 242;
            float visibleWorld = v != null ? visiblePx / v.sprite.pixelsPerUnit * v.transform.lossyScale.x : -1f;

            // Expected position from the old layout.
            Vector2 expected;
            int spot = -1;
            if (b.IsCue) expected = table.HeadSpot;
            else
            {
                spot = Object.FindAnyObjectByType<GameBootstrap>().rackSetup.spots.IndexOf(b.Data);
                int row = 0, first = 0;
                while (spot >= first + row + 1) { first += row + 1; row++; }
                int j = spot - first;
                expected = table.FootSpot + new Vector2((j - row / 2f) * d, row * rowStep);
            }
            float posErr = Vector2.Distance(b.transform.position, expected);

            bool ok = Mathf.Abs(b.transform.localScale.x - 2f * r) < 1e-5f && Mathf.Abs(col.radius - 0.5f) < 1e-6f
                      && col.offset == Vector2.zero && Mathf.Abs(b.Radius - r) < 1e-5f
                      && Mathf.Approximately(body.mass, p.mass) && Mathf.Approximately(b.Material.bounciness, p.bounciness)
                      && body.gravityScale == 0f && body.linearDamping == 0f && body.freezeRotation
                      && body.collisionDetectionMode == CollisionDetectionMode2D.Continuous
                      && b.gameObject.layer == (b.IsCue ? Ball.CueLayer : 0)
                      && posErr < 1e-4f && Mathf.Abs(visibleWorld - 2f * r) < 1e-4f
                      && b.GetComponent<BallImpactFX>() != null && b.transform.Find("Shadow") != null;
            if (!ok) problems++;
            sb.AppendLine($"{(ok ? "OK " : "BAD")} {b.name} n={b.Number} data={(b.Data ? b.Data.name + "/" + b.Data.type : "-")} spot={spot} " +
                          $"sprite={(v ? v.sprite.name : "NULL")} scale={b.transform.localScale.x:0.#####} colR={col.radius} off={col.offset.x:0.###},{col.offset.y:0.###} " +
                          $"R={b.Radius:0.######} mass={body.mass} bounce={b.Material.bounciness} layer={b.gameObject.layer} posErr={posErr:0.######} visible={visibleWorld:0.#####} " +
                          $"sort={(v ? v.sortingLayerName + "/" + v.sortingOrder : "-")}");
        }
        return $"balls={Ball.All.Count} problems={problems}\n" + sb;
    }
}
