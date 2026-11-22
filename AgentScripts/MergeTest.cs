using System.Linq;
using System.Threading.Tasks;
using RoguelikePool;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// Score popups merge (Play Mode). Run(power, angle, tapAfterMergeStart): a break shot from the current
/// cue position; logs the real / displayed score while rolling, when the merge starts, each PopupMerged,
/// and when it ends. tapAfterMergeStart ≥ 0 simulates a mouse click that long after the merge starts.
/// </summary>
public static class MergeTest
{
    public static async Task<string> Run(float power, float angleOffsetDeg, float tapAfterMergeStart)
    {
        var stages = Object.FindAnyObjectByType<StageManager>();
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>();
        var cue = Object.FindAnyObjectByType<CueController>();
        var ui = Object.FindAnyObjectByType<ShotUI>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var cueBall = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include).First(b => b.IsCue);
        stages.pointsPerBall = 1;
        Vector2 dir = table.FootSpot - cueBall.Body.position;
        foreach (var n in new[] { "aimAngle", "targetAngle" })
            typeof(CueController).GetField(n, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(cue, (Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + angleOffsetDeg) * Mathf.Deg2Rad);
        ui.Power = power;
        await Task.Delay(100);

        string log = "";
        int scoreBefore = stages.Score;
        float t0 = Time.time, mergeStart = -1f, mergeEnd = -1f, stopped = -1f;
        int merged = 0, maxPopups = 0, displayedWhileRolling = -1;
        System.Action<int> onMerged = i => { merged++; log += $"merged#{i}@{Time.time - t0:F2}s disp={stages.DisplayedScore}; "; };
        layer.PopupMerged += onMerged;
        bool tapped = false;
        cue.Shoot();
        while (Time.time - t0 < 30f)
        {
            await Task.Yield();
            maxPopups = Mathf.Max(maxPopups, layer.Active.Count);
            if (!Ball.AllStopped && stages.Score > scoreBefore) displayedWhileRolling = stages.DisplayedScore;
            if (stopped < 0f && Time.time - t0 > 0.5f && Ball.AllStopped) stopped = Time.time - t0;
            if (mergeStart < 0f && layer.IsMerging) mergeStart = Time.time - t0;
            if (mergeStart >= 0f && !tapped && tapAfterMergeStart >= 0f && Time.time - t0 - mergeStart >= tapAfterMergeStart)
            {
                tapped = true;
                var mouse = Mouse.current;
                var pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pos, buttons = 1 });
                await Task.Yield(); await Task.Yield();
                InputSystem.QueueStateEvent(mouse, new MouseState { position = pos, buttons = 0 });
                log += $"TAP@{Time.time - t0:F2}s; ";
            }
            if (mergeStart >= 0f && mergeEnd < 0f && !layer.IsMerging) mergeEnd = Time.time - t0;
            if (mergeEnd >= 0f && Time.time - t0 > mergeEnd + 0.3f) break;
            if (stopped >= 0f && mergeStart < 0f && Time.time - t0 > stopped + 2f) break; // nothing to merge
        }
        layer.PopupMerged -= onMerged;
        return $"score {scoreBefore}->{stages.Score} displayed={stages.DisplayedScore} displayedWhileRolling={displayedWhileRolling} " +
               $"maxPopups={maxPopups} stopped@{stopped:F2}s mergeStart@{mergeStart:F2}s mergeEnd@{mergeEnd:F2}s " +
               $"(merge {(mergeEnd - mergeStart):F2}s) mergedEvents={merged} state={stages.CurrentState} | {log}";
    }
}

public static class MergeCapTest
{
    /// <summary>Spawns n popups (round robin over the pockets) with balls at rest, measures the merge.</summary>
    public static async Task<string> Run(int n, float tapAfterMergeStart)
    {
        var stages = Object.FindAnyObjectByType<StageManager>();
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>();
        stages.RestartRun();
        await Task.Delay(200);
        // Give the real score the points so the displayed score can follow (AddDisplayedScore never passes it).
        typeof(StageManager).GetField("score", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(stages, n);
        var pockets = Pocket.Active.ToArray();
        for (int i = 0; i < n; i++) layer.Spawn(pockets[i % pockets.Length], new ScorePopupData(1));
        float t0 = Time.time, start = -1f, end = -1f; int merged = 0; bool tapped = false; string log = "";
        System.Action<int> onMerged = i => merged++;
        layer.PopupMerged += onMerged;
        while (Time.time - t0 < 10f)
        {
            await Task.Yield();
            if (start < 0f && layer.IsMerging) start = Time.time;
            if (start >= 0f && !tapped && tapAfterMergeStart >= 0f && Time.time - start >= tapAfterMergeStart)
            {
                tapped = true;
                var pos = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pos, buttons = 1 });
                await Task.Yield(); await Task.Yield();
                InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pos, buttons = 0 });
                log = $"tap@{Time.time - start:F2}s mergedBeforeTap={merged} ";
            }
            if (start >= 0f && !layer.IsMerging) { end = Time.time; break; }
        }
        layer.PopupMerged -= onMerged;
        return $"n={n} waitBeforeMerge={start - t0:F2}s merge={end - start:F2}s mergedEvents={merged} displayed={stages.DisplayedScore}/{stages.Score} active={layer.Active.Count} {log}";
    }
}
