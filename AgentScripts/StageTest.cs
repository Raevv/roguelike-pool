using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class StageTest
{
    // Drives the StageManager through victory, next stage, scratch and defeat without aiming: a "shot" is
    // TableRules.TakeSnapshot() (what CueController.Shoot does) followed by pocketing balls directly or a
    // gentle cue nudge. Uses pointsPerBall = 50 at runtime only (no asset is modified), restored at the end.
    public static async Task<string> Run()
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        if (sm == null) return "no StageManager";
        int oldPoints = sm.pointsPerBall;
        sm.pointsPerBall = 50;
        string r = "";
        string S() => $"[st{sm.StageNumber} {sm.Score}/{sm.Target} shots {sm.ShotsLeft}/{sm.MaxShots} {sm.CurrentState} locked={rules.Locked}] ";
        Ball Colored() => Ball.All.First(b => !b.IsCue);
        Ball Cue() => Object.FindObjectsByType<Ball>(FindObjectsInactive.Include).First(b => b.IsCue);
        Pocket P() => table.Pockets[0];
        int won = 0, lost = 0;
        System.Action onWon = () => won++, onLost = () => lost++;
        sm.StageWon += onWon; sm.StageLost += onLost;

        sm.RestartRun();
        r += "start " + S();

        // Victory: 2 shots x 1 ball x 50 = 100 on stage 1 (target 100).
        for (int i = 0; i < 2; i++)
        {
            rules.TakeSnapshot();
            Colored().FallInto(P());
            await Task.Delay(600);
            r += $"shot{i + 1} " + S();
        }
        r += $"wonEvents={won} ";

        // Shooting is locked: a snapshot now must not count.
        rules.TakeSnapshot();
        await Task.Delay(400);
        r += "lockedShot " + S();

        sm.NextStage();
        r += "next " + S();

        // Scratch: a colored ball then the cue fall; after the rewind the points are removed.
        rules.TakeSnapshot();
        Colored().FallInto(P());
        Cue().FallInto(P());
        await Task.Delay(300);
        r += "duringScratch " + S();
        await Task.Delay(1500);
        r += "afterScratch " + S();

        // Defeat: use the remaining shots with a gentle cue nudge (no pot).
        while (sm.CurrentState == StageManager.State.Playing && sm.ShotsLeft > 0)
        {
            rules.TakeSnapshot();
            Cue().Strike(new Vector2(0f, 0.6f));
            await Task.Delay(1200);
        }
        r += $"defeat " + S() + $"lostEvents={lost} ";

        sm.RestartRun();
        r += "restart " + S();

        sm.StageWon -= onWon; sm.StageLost -= onLost;
        sm.pointsPerBall = oldPoints;
        return r;
    }
}
