using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class CaptureMarginTest
{
    // For each pocket, drops a colored ball and the cue ball at rest at `frac` × the object capture
    // radius from the pocket center (toward the table), and reports which ones fall in.
    public static async Task<string> Run(float frac)
    {
        var table = Object.FindAnyObjectByType<PoolTable>();
        var all = Object.FindObjectsByType<Ball>(FindObjectsInactive.Include);
        var cue = all.First(b => b.IsCue);
        var obj = all.First(b => b.Number == 1);
        string report = $"ballR={cue.Radius:F3} ";
        foreach (var p in table.Pockets)
        {
            Vector2 c = p.transform.position;
            Vector2 inward = (-c).normalized;
            Vector2 pos = c + inward * p.ObjectCaptureRadius * frac;
            obj.ResetTo(pos);
            cue.ResetTo(pos + new Vector2(30f, 0f)); // off-table parking would be wrong; test one at a time
            cue.ResetTo(table.HeadSpot);
            await Task.Delay(150);
            bool objIn = obj.IsPocketed;
            obj.ResetTo(table.FootSpot + new Vector2(0, 4f));
            cue.ResetTo(pos);
            await Task.Delay(150);
            bool cueIn = cue.IsPocketed;
            cue.ResetTo(table.HeadSpot);
            await Task.Delay(50);
            report += $"{p.name}(r={p.radius:F3} obj={p.ObjectCaptureRadius:F3} cue={p.CueCaptureRadius:F3}): objIn={objIn} cueIn={cueIn}; ";
        }
        return report;
    }
}
