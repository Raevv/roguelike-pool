using UnityEngine;
using RoguelikePool;
public static class CueTouchTest {
  // Reports, for screen points along/around the cue and elsewhere, whether a press there may set the power.
  public static string Run() {
    var cc = Object.FindAnyObjectByType<CueController>();
    var cue = Object.FindAnyObjectByType<CueStick>();
    var isOn = typeof(CueController).GetMethod("IsOnCue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
    var onScreen = typeof(CueController).GetMethod("OnScreen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
    var rig = PixelCameraRig.Instance;
    Vector2 tip = cue.stick.position; Vector2 dir = cc.AimDirection; Vector2 n = new Vector2(-dir.y, dir.x);
    float k = Screen.height / 1920f;
    var sb = new System.Text.StringBuilder($"cueLength={cue.Length:F2}u | ");
    void Check(string name, Vector2 s) { sb.Append($"{name}: onCue={(bool)isOn.Invoke(cc, new object[] { s })} onScreen={(bool)onScreen.Invoke(null, new object[] { s })} | "); }
    Vector2 mid = rig.WorldToScreen(tip - dir * cue.Length * 0.3f);
    Vector2 side = rig.WorldToScreen(tip - dir * cue.Length * 0.3f + n) - mid; side.Normalize();
    Check("on axis", mid);
    Check("60px aside", mid + side * 60 * k);
    Check("90px aside", mid + side * 90 * k);
    Check("table center", rig.WorldToScreen(Vector2.zero));
    Check("above screen", new Vector2(Screen.width / 2f, Screen.height + 20));
    return sb.ToString();
  }
}
