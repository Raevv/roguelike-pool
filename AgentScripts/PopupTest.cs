using RoguelikePool;
using UnityEngine;

/// <summary>Score popups: spawns one popup per pocket (Play Mode) and reports their screen positions.</summary>
public static class PopupTest
{
    public static string Run(int perPocket)
    {
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>();
        if (layer == null) return "no ScorePopupLayer";
        var s = "";
        foreach (var pocket in Pocket.Active)
            for (int i = 0; i < perPocket; i++)
            {
                var p = layer.Spawn(pocket, new ScorePopupData(i + 1));
                s += $"{pocket.name}:{p.Rect.anchoredPosition} ";
            }
        return $"active={layer.Active.Count} {s}";
    }
}
