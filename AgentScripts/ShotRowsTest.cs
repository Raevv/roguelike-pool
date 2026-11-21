using UnityEngine;
using RoguelikePool;
public static class ShotRowsTest {
  // Forces total/used and prints each row (mockup coords: x from the left edge of a 1080 screen, y from the top).
  public static string Run(int total, int used) {
    var c = Object.FindAnyObjectByType<ShotsCounter>();
    c.SetShots(total, used);
    var rows = new System.Collections.Generic.SortedDictionary<int, string>();
    float k = 1080f / Screen.width; float size = 0;
    foreach (var i in c.GetComponentsInChildren<ShotIcon>(false)) {
      var r = (RectTransform)i.transform; var w = new Vector3[4]; r.GetWorldCorners(w);
      size = (w[2].x - w[0].x) * k;
      int top = Mathf.RoundToInt((Screen.height - w[1].y) * k);
      rows.TryGetValue(top, out var s);
      rows[top] = s + $"{w[0].x * k:F0}{(i.Used ? "x" : "")} ";
    }
    var sb = new System.Text.StringBuilder($"n={total} used={used} size={size:F0} | ");
    foreach (var kv in rows) sb.Append($"y{kv.Key}: {kv.Value}| ");
    return sb.ToString();
  }
}
