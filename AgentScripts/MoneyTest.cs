using UnityEngine;
using RoguelikePool;
public static class MoneyTest {
  // Optional amount; reports the money group's center vs the drawn table's center (x on a 1080-wide screen).
  public static string Run(int dollars) {
    var m = Object.FindAnyObjectByType<MoneyDisplay>();
    if (dollars >= 0) m.SetDollars(dollars);
    UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)m.transform);
    float k = 1080f / Screen.width; var c = new Vector3[4];
    ((RectTransform)m.transform).GetWorldCorners(c);
    var t = Object.FindAnyObjectByType<TableFitter>().FittedScreenRect;
    return $"{Screen.width}x{Screen.height} money x {c[0].x * k:F1}→{c[2].x * k:F1} center {(c[0].x + c[2].x) / 2 * k:F1} top {(Screen.height - c[1].y) * k:F1} | table center {t.center.x * k:F1}";
  }
}
