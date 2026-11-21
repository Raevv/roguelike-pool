using UnityEngine;
using RoguelikePool;
public static class SettingsMarginTest {
  // Distances (canvas units) from the safe area's left and top edges to the settings button's visual.
  public static string Run() {
    var menu = Object.FindAnyObjectByType<SettingsMenu>(FindObjectsInactive.Include);
    var visual = (RectTransform)menu.transform.Find("Placeholder_Settings");
    var safe = (RectTransform)Object.FindAnyObjectByType<SafeAreaPanel>().transform;
    var v = new Vector3[4]; var s = new Vector3[4]; visual.GetWorldCorners(v); safe.GetWorldCorners(s);
    float k = safe.lossyScale.x;
    return $"{Screen.width}x{Screen.height} left {(v[0].x - s[0].x) / k:F1} top {(s[1].y - v[1].y) / k:F1} size {(v[2].x - v[0].x) / k:F0}";
  }
}
