using System.Threading.Tasks;
using RoguelikePool;
using UnityEngine;

/// <summary>Score popups step 7: runs the "Simuler 5 empochages" context menu and logs its timeline.</summary>
public static class SimulateTest
{
    public static async Task<string> Run()
    {
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>();
        var stages = Object.FindAnyObjectByType<StageManager>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        int scoreBefore = stages.Score, maxActive = 0, maxDisp = 0, merged = 0;
        float t0 = Time.time, mergeStart = -1f, mergeEnd = -1f, back = -1f;
        System.Action<int> h = i => merged++;
        layer.PopupMerged += h;
        layer.Simulate5();
        while (Time.time - t0 < 8f)
        {
            await Task.Yield();
            maxActive = Mathf.Max(maxActive, layer.Active.Count);
            maxDisp = Mathf.Max(maxDisp, stages.DisplayedScore);
            if (mergeStart < 0f && layer.IsMerging) mergeStart = Time.time - t0;
            if (mergeStart >= 0f && mergeEnd < 0f && !layer.IsMerging) mergeEnd = Time.time - t0;
            if (mergeEnd >= 0f && stages.DisplayedScore == stages.Score) { back = Time.time - t0; break; }
        }
        layer.PopupMerged -= h;
        return $"maxActive={maxActive} mergeStart@{mergeStart:F2}s mergeEnd@{mergeEnd:F2}s merged={merged} maxDisplayed={maxDisp} backToReal@{back:F2}s " +
               $"score {scoreBefore}->{stages.Score} displayed={stages.DisplayedScore} state={stages.CurrentState} shots={stages.ShotsLeft} busy={rules.Busy}";
    }
}
