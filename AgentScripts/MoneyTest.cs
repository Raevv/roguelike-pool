using UnityEngine;
using RoguelikePool;
public static class MoneyTest {
  // align >= 0: sets the TableFitter's vertical alignment first. Reports the money vs the drawn table
  // (canvas units from the top of the screen; x centers in screen px on a 1080-wide base).
  public static string Run(float align) {
    var fitter = Object.FindAnyObjectByType<TableFitter>();
    if (align >= 0f) typeof(TableFitter).GetField("verticalAlignment", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(fitter, align);
    fitter.Fit();
    var m = Object.FindAnyObjectByType<MoneyDisplay>();
    typeof(MoneyDisplay).GetMethod("FollowTable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(m, null);
    var c = new Vector3[4]; ((RectTransform)m.transform).GetWorldCorners(c);
    float s = ((RectTransform)Object.FindAnyObjectByType<ShotUI>().transform).lossyScale.y, k = 1080f / Screen.width;
    var t = fitter.FittedScreenRect;
    return $"{Screen.width}x{Screen.height} align={align} | money bottom {(Screen.height - c[0].y) / s:F1} center x {(c[0].x + c[2].x) / 2 * k:F1} | table top {(Screen.height - t.yMax) / s:F1} center x {t.center.x * k:F1} | gap {((c[0].y - t.yMax) / s):F1}";
  }
}
