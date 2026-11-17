using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class PocketTest
{
    static CueController Cue => Object.FindAnyObjectByType<CueController>();
    static ShotUI UI => Object.FindAnyObjectByType<ShotUI>();
    static TableRules Rules => Object.FindAnyObjectByType<TableRules>();
    static Ball CueBall => Object.FindObjectsByType<Ball>(FindObjectsInactive.Include).First(b => b.IsCue);

    static void Aim(float deg)
    {
        foreach (var n in new[] { "aimAngle", "targetAngle" })
            typeof(CueController).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(Cue, deg * Mathf.Deg2Rad);
    }

    static string State() => $"onTable={Ball.All.Count} [{string.Join(",", Ball.All.Select(b => b.Number).OrderBy(n => n))}]";

    // Shoots the cue ball straight into a pocket aimed at targetX,targetY. Expects the shot to be cancelled.
    public static async Task<string> Scratch(float targetX, float targetY, float power)
    {
        var cue = CueBall; Vector2 start = cue.Body.position;
        var before = Ball.All.ToDictionary(b => b, b => b.Body.position);
        Vector2 dir = new Vector2(targetX, targetY) - start;
        Aim(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
        UI.Power = power;
        await Task.Delay(100);
        bool cuePocketed = false, busySeen = false, messageSeen = false;
        System.Action<Ball, Pocket> h = (b, p) => { if (b.IsCue) cuePocketed = true; };
        Ball.Pocketed += h;
        Cue.Shoot();
        float t0 = Time.time;
        while (Time.time - t0 < 20f)
        {
            await Task.Yield();
            if (Rules.Busy) busySeen = true;
            var msg = GameObject.Find("Banner"); if (msg != null && msg.activeInHierarchy) messageSeen = true;
            if (Time.time - t0 > 0.5f && !Rules.Busy && Ball.AllStopped && cue.gameObject.activeInHierarchy) break;
        }
        Ball.Pocketed -= h;
        float maxDelta = before.Max(kv => Vector2.Distance(kv.Key.Body.position, kv.Value));
        bool allBack = before.Keys.All(b => b.gameObject.activeInHierarchy);
        return $"cuePocketed={cuePocketed} busySeen={busySeen} messageSeen={messageSeen} restoredAllActive={allBack} maxPosDelta={maxDelta:F4} cueAt={cue.Body.position} start={start} {State()} t={Time.time - t0:F2}s";
    }

    // Break shot from the current cue position toward the rack apex. Reports pocketed balls and bounds check.
    public static async Task<string> Break(float power, float angleOffsetDeg)
    {
        var table = Object.FindAnyObjectByType<PoolTable>();
        var cue = CueBall;
        Vector2 dir = table.FootSpot - cue.Body.position;
        Aim(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg);
        UI.Power = power;
        await Task.Delay(100);
        var pocketed = new System.Collections.Generic.List<int>();
        System.Action<Ball, Pocket> h = (b, p) => pocketed.Add(b.Number);
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
        return $"pocketed=[{string.Join(",", pocketed)}] escaped={escaped} t={Time.time - t0:F2}s {State()} cueActive={cue.gameObject.activeInHierarchy}";
    }
}
