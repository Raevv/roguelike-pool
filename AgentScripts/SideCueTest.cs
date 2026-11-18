using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class SideCueTest
{
    // Sets the side pockets' cue capture scale, cue funnel and the cue-only side jaw bevel at runtime, then
    // shoots the cue ball at the right side pocket from 9 start points (3 distances from the rail x 3
    // distances along it) and 3 aim points (mouth center just inside the rail line, +-0.35 along the rail). Rows: rail = ball against
    // the rail, near = 1 unit off it, far = 3 units off it. Pass colored=1 to shoot colored ball 1 instead.
    // Other balls are parked bottom-left: restart Play Mode afterwards.
    // With the user-tuned friction (2026-11), use speed 20.
    public static async Task<string> Run(float captureScale, float bevel, float funnel, float speed, int colored)
    {
        var table = Object.FindAnyObjectByType<PoolTable>();
        var area = table.PlayAreaWorld;
        float hw = area.xMax;
        foreach (var p in table.pockets)
            if (table.IsSidePocket(p)) { p.cueCaptureScale = captureScale; p.cueFunnelStrength = funnel; }
        table.sideCueBevel = bevel;
        table.RebuildCueGuards();

        PolygonCollider2D C(string n) => table.cushions.First(c => c.name == n);
        Vector2 W(PolygonCollider2D c, int i) => c.transform.TransformPoint(c.points[i]);
        float sideC = (W(C("Cushion_RightTop"), 1).y + W(C("Cushion_RightBottom"), 0).y) / 2f;

        var all = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include);
        var ball = colored != 0 ? all.First(b => b.Number == 1) : all.First(b => b.IsCue);
        int k = 0;
        foreach (var b in all)
            if (b != ball) b.ResetTo(new Vector2(area.xMin + 1.85f + (k % 4) * 0.95f, area.yMin + 0.9f + (k++ / 4) * 0.95f));

        string report = $"{(colored != 0 ? "COLOR" : "CUE")} capture={captureScale} bevel={bevel} funnel={funnel}: ";
        int total = 0;
        foreach (var (label, inset) in new[] { ("rail", 0.46f), ("near", 1.0f), ("far", 3.0f) })
        {
            int pots = 0, shots = 0;
            string bits = "";
            foreach (float along in new[] { 2f, 4f, 6f })
            foreach (float aim in new[] { 0f, 0.35f, -0.35f })
            {
                Vector2 start = new Vector2(hw - inset, sideC + along);
                Vector2 target = new Vector2(hw - 0.25f, sideC + aim); // ball center over the mouth, just inside the rail line
                ball.ResetTo(start);
                await Task.Delay(30);
                ball.Strike((target - start).normalized * speed);
                for (int i = 0; i < 80 && !ball.IsPocketed && ball.IsMoving; i++) await Task.Delay(40);
                bool potted = ball.IsPocketed;
                if (potted) await Task.Delay(ball.IsCue ? 1300 : 300); // let TableRules restore, then continue
                shots++; if (potted) pots++;
                bits += potted ? "1" : "0";
            }
            total += pots;
            report += $"{label} {pots}/{shots} [{bits}]  ";
        }
        ball.ResetTo(ball.IsCue ? table.HeadSpot : table.FootSpot);
        return report + $"TOTAL {total}/27";
    }
}
