using UnityEngine;
using RoguelikePool;
public static class ColumnTest {
  // Right column in canvas units from the top of the screen: tabs, percentage label, bar; and the two gaps.
  public static string Run() {
    var c = new Vector3[4];
    var canvas = (RectTransform)Object.FindAnyObjectByType<ShotUI>().transform;
    float s = canvas.lossyScale.y;
    float Top(RectTransform r) { r.GetWorldCorners(c); return (Screen.height - c[1].y) / s; }
    float Bottom(RectTransform r) { r.GetWorldCorners(c); return (Screen.height - c[0].y) / s; }
    var tabs = (RectTransform)Object.FindAnyObjectByType<BonusTabs>().transform;
    var bar = (RectTransform)Object.FindAnyObjectByType<PowerGauge>().transform;
    var label = (RectTransform)bar.Find("PercentText");
    return $"{Screen.width}x{Screen.height} (canvas units) tabs {Top(tabs):F1}→{Bottom(tabs):F1} | label top {Top(label):F1} | bar {Top(bar):F1}→{Bottom(bar):F1} | top margin {Top(tabs):F1} vs gap tabs→label {Top(label) - Bottom(tabs):F1}";
  }
}
