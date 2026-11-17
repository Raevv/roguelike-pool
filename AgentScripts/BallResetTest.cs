using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class BallResetTest
{
    // Checks that every ball returns to its starting position after STAGE SUIVANT and after RECOMMENCER.
    // Run it right after entering Play Mode: the reference layout is read from the balls as they are then.
    // Messes the table up with real strikes and pockets (pointsPerBall 500 at runtime only).
    public static async Task<string> Run()
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var all = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include).OrderBy(b => b.Number).ToArray();
        var reference = all.ToDictionary(b => b, b => b.Body.position);
        sm.pointsPerBall = 500;
        Ball Cue() => all.First(b => b.IsCue);

        string Check(string label)
        {
            int active = all.Count(b => b.gameObject.activeSelf && !b.IsPocketed);
            float maxDelta = all.Max(b => Vector2.Distance(b.Body.position, reference[b]));
            bool still = Ball.AllStopped;
            return $"{label}: active={active}/{all.Length} maxPosDelta={maxDelta:F4} allStopped={still} st{sm.StageNumber} {sm.Score}/{sm.Target} shots {sm.ShotsLeft}/{sm.MaxShots}; ";
        }

        string r = "";
        // Stage 1: break hard (real strike), then pocket two more balls → won.
        rules.TakeSnapshot();
        Cue().Strike(new Vector2(0.05f, 40f));
        for (int i = 0; i < 200 && !Ball.AllStopped; i++) await Task.Delay(50);
        await Task.Delay(500);
        if (sm.CurrentState == StageManager.State.Playing)
        {
            rules.TakeSnapshot();
            foreach (var b in Ball.All.Where(b => !b.IsCue).Take(2).ToList()) b.FallInto(table.Pockets[1]);
            await Task.Delay(600);
        }
        r += Check("beforeNext") + $"state={sm.CurrentState}; ";
        sm.NextStage();
        await Task.Delay(300);
        r += Check("afterNext");

        // Stage 2: move balls around and lose.
        int guard = 0;
        while (sm.CurrentState == StageManager.State.Playing && guard++ < 10)
        {
            rules.TakeSnapshot();
            Cue().Strike(new Vector2(Random.Range(-10f, 10f), 25f));
            for (int i = 0; i < 200 && (!Ball.AllStopped || rules.Busy); i++) await Task.Delay(50);
            await Task.Delay(400);
        }
        r += Check("beforeRestart") + $"state={sm.CurrentState}; ";
        sm.RestartRun();
        await Task.Delay(300);
        r += Check("afterRestart");
        sm.pointsPerBall = 1;
        return r;
    }
}
