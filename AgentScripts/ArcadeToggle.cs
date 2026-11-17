using UnityEngine;
using RoguelikePool;

public static class ArcadeToggle
{
    // on=0: previous tuning (capture 1.1 / 0.65, no assist, no funnel, no corner cue guards; side pocket tuning untouched). on=1: new tuning.
    public static string Run(int on)
    {
        foreach (var p in Pocket.Active)
        {
            p.objectCaptureScale = on != 0 ? 1.35f : 1.1f;
            p.cueCaptureScale = on != 0 ? 0.45f : 0.65f;
            p.assistStrength = on != 0 ? 7f : 0f;
            p.funnelStrength = on != 0 ? 0.6f : 0f;
        }
        var table = Object.FindAnyObjectByType<PoolTable>();
        // Only the corner guards: the CueJawSide twins replace the long-rail cushions for the cue ball.
        foreach (Transform g in table.transform.Find("CueGuards"))
            if (g.name == "CueGuardCorner") g.gameObject.SetActive(on != 0);
        return "arcade=" + on;
    }
}
