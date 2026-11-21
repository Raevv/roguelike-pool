using UnityEngine;
using RoguelikePool;
public static class CueOverMoney {
  // Puts the cue ball near the top-right of the table and aims so the cue crosses the money (and the TopBar).
  public static string Run() {
    Ball cue = null; foreach (var b in Ball.All) if (b.IsCue) cue = b;
    cue.Body.position = new Vector2(3.5f, 8.5f); cue.transform.position = cue.Body.position;
    var cc = Object.FindAnyObjectByType<CueController>();
    float a = (180f + 70f) * Mathf.Deg2Rad; // shoot down-left: the cue extends up-right
    var F = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
    typeof(CueController).GetField("aimAngle", F).SetValue(cc, a);
    typeof(CueController).GetField("targetAngle", F).SetValue(cc, a);
    return "ok " + cue.Body.position;
  }
}
