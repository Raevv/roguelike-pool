using System.Reflection;
using System.Text;
using UnityEngine;
using RoguelikePool;

/// <summary>
/// Checks the cue-drag power rework without real pointer input: calls CueController.ApplyDrag with
/// synthetic screen deltas (aim pointing up, so the cue butt is down) and reports power/angle changes,
/// plus whether the gauge slider is non-interactable.
/// </summary>
public static class PowerDragTest
{
    const BindingFlags F = BindingFlags.NonPublic | BindingFlags.Instance;

    public static string Run()
    {
        var cue = Object.FindAnyObjectByType<CueController>();
        var ui = Object.FindAnyObjectByType<ShotUI>();
        var gauge = Object.FindAnyObjectByType<PowerGauge>();
        var t = typeof(CueController);
        var target = t.GetField("targetAngle", F);
        var axis = t.GetField("dragAxis", F);
        var apply = t.GetMethod("ApplyDrag", F);
        var axisType = t.GetNestedType("DragAxis", BindingFlags.NonPublic);
        float h = Screen.height;
        var sb = new StringBuilder();
        sb.Append($"sliderInteractable(expect True)={gauge.Slider.interactable} screenH={h} ");

        string Case(string name, string mode, Vector2 delta, float startPower, float sign = 1f)
        {
            target.SetValue(cue, Mathf.PI / 2f); // aim up
            ui.Power = startPower;
            axis.SetValue(cue, System.Enum.Parse(axisType, mode));
            t.GetField("aimSign", F).SetValue(cue, sign);
            float a = (float)target.GetValue(cue);
            Vector2 back = new Vector2(-Mathf.Cos(a), -Mathf.Sin(a));
            Vector2 side = new Vector2(-back.y, back.x);
            apply.Invoke(cue, new object[] { delta, back, side, h });
            float da = ((float)target.GetValue(cue) - a) * Mathf.Rad2Deg;
            return $"[{name}: power {startPower:F2}->{ui.Power:F3} dAngle={da:F2}] ";
        }

        sb.Append(Case("pullBack 0.2H (Power)", "Power", new Vector2(0, -0.2f * h), 0.2f));
        sb.Append(Case("pushIn 0.1H (Power)", "Power", new Vector2(0, 0.1f * h), 0.5f));
        sb.Append(Case("pullBack beyond max", "Power", new Vector2(0, -1f * h), 0.5f));
        sb.Append(Case("side 0.1H (Aim)", "Aim", new Vector2(0.1f * h, 0), 0.5f));
        sb.Append(Case("pullBack in Aim mode", "Aim", new Vector2(0, -0.2f * h), 0.5f));
        sb.Append(Case("side in Power mode", "Power", new Vector2(0.1f * h, 0), 0.5f));
        sb.Append(Case("side 0.1H right from FRONT (Aim)", "Aim", new Vector2(0.1f * h, 0), 0.5f, -1f));
        t.GetField("aimSign", F).SetValue(cue, 1f);
        target.SetValue(cue, Mathf.PI / 2f);
        t.GetField("aimAngle", F).SetValue(cue, Mathf.PI / 2f);
        ui.Power = 0f;
        return sb.ToString();
    }
}
