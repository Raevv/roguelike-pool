using UnityEngine;
using RoguelikePool;
public static class ColumnTest {
  // Bonus tabs and power bar edges in mockup coordinates (x from left on a 1080-wide screen, y from top).
  public static string Run() {
    float k = 1080f / Screen.width;
    var c = new Vector3[4];
    string Edges(RectTransform r) { r.GetWorldCorners(c); return $"y {(Screen.height - c[1].y) * k:F0}→{(Screen.height - c[0].y) * k:F0}"; }
    var tabs = (RectTransform)Object.FindAnyObjectByType<BonusTabs>().transform;
    var bar = (RectTransform)Object.FindAnyObjectByType<PowerGauge>().transform;
    var shoot = (RectTransform)GameObject.Find("ShootButton").transform;
    return $"screen {Screen.width}x{Screen.height} tabs {Edges(tabs)} bar {Edges(bar)} shoot {Edges(shoot)}";
  }
}
