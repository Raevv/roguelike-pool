using UnityEngine;
using RoguelikePool;
public static class MoneyTest {
  // Money rect vs drawn table (mockup coords: x from the left of a 1080-wide screen, y from the top).
  public static string Run() {
    float k = 1080f / Screen.width; var c = new Vector3[4];
    var money = (RectTransform)Object.FindAnyObjectByType<MoneyDisplay>().transform; money.GetWorldCorners(c);
    var t = Object.FindAnyObjectByType<TableFitter>().FittedScreenRect;
    return $"{Screen.width}x{Screen.height} money right {c[2].x * k:F1} bottom {(Screen.height - c[0].y) * k:F1} | table right {t.xMax * k:F1} top {(Screen.height - t.yMax) * k:F1} | gap {((Screen.height - t.yMax) - (Screen.height - c[0].y)) * k:F1}";
  }
}
