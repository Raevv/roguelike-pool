using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class JawTest
{
    // Shoots one ball (colored = number 1, or the cue ball) from several start points toward targets
    // spread around the top-right corner mouth and the right side mouth (jaw tips included), and counts
    // how many fall in. Other balls are parked far from the paths.
    // Geometry is read from the PoolTable prefab: jaw tips = ends of the cushion noses.
    // With the user-tuned friction (2026-11), use speed 25 (12 no longer reaches the pockets).
    public static async Task<string> Run(int cue, float speed)
    {
        var table = Object.FindAnyObjectByType<PoolTable>();
        var area = table.PlayAreaWorld;
        PolygonCollider2D C(string n) => table.cushions.First(c => c.name == n);
        Vector2 W(PolygonCollider2D c, int i) => c.transform.TransformPoint(c.points[i]);
        Vector2 tipLong = W(C("Cushion_RightTop"), 0), tipShort = W(C("Cushion_Top"), 1);
        Vector2 sideUp = W(C("Cushion_RightTop"), 1), sideDown = W(C("Cushion_RightBottom"), 0);
        float sideC = (sideUp.y + sideDown.y) / 2f, s = (sideUp.y - sideDown.y) / 2f;
        float ys = area.yMax / 12.7f; // start points scaled from the old 25.4-long table

        var all = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include);
        var ball = cue != 0 ? all.First(b => b.IsCue) : all.First(b => b.Number == 1);
        // Park everything else in a tight cluster at the bottom left.
        int k = 0;
        foreach (var b in all)
            if (b != ball) b.ResetTo(new Vector2(area.xMin + 1.85f + (k % 4) * 0.95f, area.yMin + 0.9f + (k++ / 4) * 0.95f));

        int shots = 0, pots = 0;
        string detail = "";
        // Corner: targets along the long rail and the short rail near the mouth, from three start points.
        Vector2[] starts = { new Vector2(1f, 7f * ys), new Vector2(3.5f, 5f * ys), new Vector2(-1f, 10f * ys) };
        float[] offs = { -0.6f, -0.3f, 0f, 0.3f, 0.6f };
        foreach (var start in starts)
        foreach (var o in offs)
        {
            foreach (var target in new[] { tipLong + new Vector2(0f, o), tipShort + new Vector2(o, 0f) })
            {
                bool potted = await ShootAndWait(ball, start, target, speed);
                shots++; if (potted) pots++;
                detail += potted ? "1" : "0";
            }
        }
        detail += " | side:";
        Vector2[] sideStarts = { new Vector2(0f, sideC + 3f), new Vector2(0f, sideC - 3f), new Vector2(1f, sideC + 0.5f) };
        foreach (var start in sideStarts)
        foreach (var o in offs)
        {
            bool potted = await ShootAndWait(ball, start, new Vector2(area.xMax, sideC + (start.y > sideC ? 1f : -1f) * (s + o)), speed);
            shots++; if (potted) pots++;
            detail += potted ? "1" : "0";
        }
        ball.ResetTo(ball.IsCue ? table.HeadSpot : table.FootSpot);
        return $"{(ball.IsCue ? "CUE" : "COLOR")} speed={speed}: {pots}/{shots} potted  [{detail}]";
    }

    static async Task<bool> ShootAndWait(Ball ball, Vector2 start, Vector2 target, float speed)
    {
        ball.ResetTo(start);
        await Task.Delay(30);
        ball.Strike((target - start).normalized * speed);
        for (int i = 0; i < 80 && !ball.IsPocketed && ball.IsMoving; i++) await Task.Delay(50);
        bool potted = ball.IsPocketed;
        await Task.Delay(potted ? 250 : 0);
        return potted;
    }
}
