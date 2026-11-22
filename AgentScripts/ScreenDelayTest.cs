using System.Linq;
using System.Threading.Tasks;
using RoguelikePool;
using UnityEngine;

/// <summary>Break shot; measures the time between the end of the score merge and the end screen.</summary>
public static class ScreenDelayTest
{
    public static async Task<string> Run(float power)
    {
        var stages = Object.FindAnyObjectByType<StageManager>();
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>();
        var screens = Object.FindAnyObjectByType<StageScreens>();
        var cue = Object.FindAnyObjectByType<CueController>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var cueBall = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include).First(b => b.IsCue);
        Vector2 dir = table.FootSpot - cueBall.Body.position;
        foreach (var n in new[] { "aimAngle", "targetAngle" })
            typeof(CueController).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(cue, Mathf.Atan2(dir.y, dir.x));
        Object.FindAnyObjectByType<ShotUI>().Power = power;
        await Task.Delay(100);
        cue.Shoot();
        float t0 = Time.time, mergeEnd = -1f, stateAt = -1f, shownAt = -1f; bool wasMerging = false;
        while (Time.time - t0 < 30f)
        {
            await Task.Yield();
            if (layer.IsMerging) wasMerging = true;
            if (wasMerging && mergeEnd < 0f && !layer.IsMerging) mergeEnd = Time.time - t0;
            if (stateAt < 0f && stages.CurrentState != StageManager.State.Playing) stateAt = Time.time - t0;
            if (screens.IsShown) { shownAt = Time.time - t0; break; }
        }
        return $"mergeEnd@{mergeEnd:F2}s state={stages.CurrentState}@{stateAt:F2}s screenShown@{shownAt:F2}s (delay {shownAt - stateAt:F2}s) displayed={stages.DisplayedScore}/{stages.Score}";
    }
}

public static class ScreenDelayCancelTest
{
    /// <summary>Win, then restart during the delay: the end screen must never appear.</summary>
    public static async Task<string> Run()
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        var screens = Object.FindAnyObjectByType<StageScreens>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        sm.RestartRun();
        await Task.Delay(200);
        rules.TakeSnapshot();
        Ball.All.First(b => !b.IsCue && !b.IsPocketed).FallInto(table.Pockets[0]);
        float t0 = Time.time;
        while (sm.CurrentState == StageManager.State.Playing && Time.time - t0 < 5f) await Task.Yield();
        float won = Time.time - t0;
        await Task.Delay(400); // inside the 1 s delay
        bool shownBeforeRestart = screens.IsShown;
        sm.RestartRun();
        await Task.Delay(1500);
        return $"won@{won:F2}s shownBeforeRestart={shownBeforeRestart} shownAfter={screens.IsShown} state={sm.CurrentState}";
    }
}
