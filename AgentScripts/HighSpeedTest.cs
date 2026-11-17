using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class HighSpeedTest
{
    // Fires `shots` full-power shots (speed = fraction × BallPhysicsSettings.maxShotSpeed) from wherever the
    // cue ball is, toward a random colored ball or a random direction, and watches every rendered frame for
    // tunneling: a ball center inside a cushion/wall polygon, outside the table's outer bounds, or two balls
    // overlapping by more than 20 % of a diameter. Also reports how long shots take to come to rest.
    // Run right after entering Play Mode (it uses the real rack).
    public static async Task<string> Run(int shots, float fraction, int seed)
    {
        var rng = new System.Random(seed);
        var table = Object.FindAnyObjectByType<PoolTable>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        var walls = table.GetComponentsInChildren<Collider2D>(true).Where(c => c.GetComponent<Ball>() == null).ToArray();
        Rect outer = table.OuterBounds;
        float speed = fraction * BallPhysicsSettings.Current.maxShotSpeed;

        int inWall = 0, outside = 0, overlaps = 0, pots = 0, scratches = 0;
        float maxSeen = 0f, totalTime = 0f, maxTime = 0f;
        var events = new List<string>();
        void OnPot(Ball b, Pocket p) { if (b.IsCue) scratches++; else pots++; }
        Ball.Pocketed += OnPot;
        try
        {
            for (int s = 0; s < shots; s++)
            {
                rules.Locked = false;
                var cue = Ball.All.FirstOrDefault(b => b.IsCue);
                if (cue == null) { await Task.Delay(1500); cue = Ball.All.FirstOrDefault(b => b.IsCue); }
                if (cue == null) return "cue ball missing";
                var colored = Ball.All.Where(b => !b.IsCue).ToList();
                Vector2 dir;
                if (colored.Count > 0 && rng.NextDouble() < 0.7)
                    dir = (colored[rng.Next(colored.Count)].Body.position - cue.Body.position).normalized;
                else
                {
                    float a = (float)(rng.NextDouble() * Mathf.PI * 2);
                    dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                }
                rules.TakeSnapshot();
                cue.Strike(dir * speed);

                float t0 = Time.time;
                await Task.Yield();
                while (Time.time - t0 < 40f)
                {
                    var balls = Ball.All.Where(b => !b.IsPocketed).ToList();
                    foreach (var b in balls)
                    {
                        Vector2 p = b.Body.position;
                        maxSeen = Mathf.Max(maxSeen, b.Body.linearVelocity.magnitude);
                        if (!outer.Contains(p)) { outside++; events.Add($"s{s} {b.name} outside {p}"); }
                        else if (walls.Any(w => w.enabled && (w.excludeLayers.value & (1 << b.gameObject.layer)) == 0 && w.OverlapPoint(p)))
                        { inWall++; events.Add($"s{s} {b.name} in wall {p}"); }
                    }
                    for (int i = 0; i < balls.Count; i++)
                        for (int j = i + 1; j < balls.Count; j++)
                        {
                            float d = Vector2.Distance(balls[i].Body.position, balls[j].Body.position);
                            if (d < (balls[i].Radius + balls[j].Radius) * 0.8f)
                            { overlaps++; events.Add($"s{s} {balls[i].name}/{balls[j].name} d={d:F3}"); }
                        }
                    if (Ball.AllStopped && Time.time - t0 > 0.3f && Ball.All.Any(b => b.IsCue)) break;
                    await Task.Yield();
                }
                float dt = Time.time - t0;
                totalTime += dt; maxTime = Mathf.Max(maxTime, dt);
                if (Ball.All.Count(b => !b.IsCue) == 0)
                {
                    Object.FindAnyObjectByType<StageManager>()?.RestartRun(); // re-rack to keep testing
                    await Task.Delay(100);
                }
                await Task.Delay(1200); // let a scratch rewind finish
            }
        }
        finally { Ball.Pocketed -= OnPot; }
        string ev = string.Join("; ", events.Distinct().Take(12));
        return $"shots={shots} speed={speed:F1} maxSeen={maxSeen:F1} inWall={inWall} outside={outside} overlaps={overlaps} " +
               $"pots={pots} scratches={scratches} avgTime={totalTime / shots:F1}s maxTime={maxTime:F1}s {ev}";
    }
}
