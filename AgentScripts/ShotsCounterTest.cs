using UnityEngine;
using RoguelikePool;
public static class ShotsCounterTest {
  // total < 0: report only (uses the real stage values). Otherwise force total/used.
  public static string Run(int total, int used) {
    var c = Object.FindAnyObjectByType<ShotsCounter>();
    if (total >= 0) c.SetShots(total, used);
    var icons = c.GetComponentsInChildren<ShotIcon>(false);
    var rt = (RectTransform)c.transform;
    string pattern = ""; float size = 0, minX = 1e9f, maxX = -1e9f;
    foreach (var i in icons) {
      pattern += i.Used ? "x" : "o";
      var r = (RectTransform)i.transform; size = r.sizeDelta.x;
      minX = Mathf.Min(minX, r.anchoredPosition.x - size / 2); maxX = Mathf.Max(maxX, r.anchoredPosition.x + size / 2);
    }
    var sm = Object.FindAnyObjectByType<StageManager>();
    return $"stage shots {sm.ShotsLeft}/{sm.MaxShots} icons={icons.Length} size={size:F1} span={minX:F1}..{maxX:F1} rowWidth={rt.rect.width:F0} pattern={pattern}";
  }
}
