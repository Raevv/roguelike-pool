using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

/// <summary>
/// Full-power breaks at angle offsets 0, 1, -1, 2, -2, with the collider offset of the Ball prefab as it is
/// (mode 0) or forced to 0 at runtime only (mode 1, = the code-built balls). The asset is never changed.
/// Reports the pocketed rack spots (spot = index in RackSetup) so results do not depend on the numbering.
/// </summary>
public static class BreakAB
{
    static CueController Cue => Object.FindAnyObjectByType<CueController>();
    static TableRules Rules => Object.FindAnyObjectByType<TableRules>();

    public static async Task<string> Run(int mode)
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var ui = Object.FindAnyObjectByType<ShotUI>();
        
        int savedPoints = sm.pointsPerBall;
        sm.pointsPerBall = 0; // never win: Locked would keep Rules.Busy true
        string r = $"mode={(mode == 0 ? "offset asset" : "offset 0")}\n";
        foreach (float angle in new[] { 0f, 1f, -1f, 2f, -2f })
        {
            sm.RestartRun();
            await Task.Delay(300);
            var balls = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include);
            if (mode == 1) foreach (var b in balls) b.Collider.offset = Vector2.zero;
            var start = balls.ToDictionary(b => b, b => b.Body.position);
            var cue = balls.First(b => b.IsCue);
            Vector2 dir = table.FootSpot - cue.Body.position;
            foreach (var n in new[] { "aimAngle", "targetAngle" })
                typeof(CueController).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .SetValue(Cue, (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angle) * Mathf.Deg2Rad);
            ui.Power = 1f;
            await Task.Delay(100);
            var spots = new System.Collections.Generic.List<string>();
            bool scratch = false;
            System.Action<Ball, Pocket> h = (b, p) => { if (b.IsCue) scratch = true; else spots.Add(SpotOf(start[b]) + "(" + b.Number + ")"); };
            Ball.Pocketed += h;
            Cue.Shoot();
            float t0 = Time.time; bool escaped = false; Rect outer = table.OuterBounds;
            while (Time.time - t0 < 30f)
            {
                await Task.Yield();
                foreach (var b in Ball.All) if (!outer.Contains(b.Body.position)) escaped = true;
                if (Time.time - t0 > 0.5f && !Rules.Busy && Ball.AllStopped) break;
            }
            Ball.Pocketed -= h;
            r += $"angle {angle,2}: pocketed {spots.Count} spots=[{string.Join(",", spots)}] scratch={scratch} escaped={escaped} t={Time.time - t0:F1}s\n";
        }
        if (mode == 1) foreach (var b in Object.FindObjectsByType<Ball>(FindObjectsInactive.Include)) b.Collider.offset = new Vector2(0.01f, 0f);
        sm.pointsPerBall = savedPoints;
        sm.RestartRun();
        return r;
    }

    /// <summary>Rack spot (0 = apex, row by row, left to right) nearest to a start position.</summary>
    static int SpotOf(Vector2 pos)
    {
        var table = Object.FindAnyObjectByType<PoolTable>();
        float d = BallFactory.DefaultRadius * 2f + 0.002f, rowStep = d * Mathf.Sqrt(3f) / 2f;
        int best = -1, spot = 0; float bestDist = float.MaxValue;
        for (int row = 0; row < 4; row++)
            for (int j = 0; j <= row; j++, spot++)
            {
                float dist = Vector2.Distance(pos, table.FootSpot + new Vector2((j - row / 2f) * d, row * rowStep));
                if (dist < bestDist) { bestDist = dist; best = spot; }
            }
        return best;
    }
}
