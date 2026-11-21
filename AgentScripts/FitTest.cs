using UnityEngine;
using RoguelikePool;
public static class FitTest {
  public static string Run() {
    var rig = PixelCameraRig.Instance;
    var fitter = Object.FindAnyObjectByType<TableFitter>();
    var table = Object.FindAnyObjectByType<PoolTable>();
    Rect w = fitter.WorldRect, s = fitter.ScreenRect;
    Vector2 bl = rig.WorldToScreen(w.min), tr = rig.WorldToScreen(w.max);
    Vector2 p = new Vector2(1.234f, -3.21f);
    Vector2 back = rig.ScreenToWorld(rig.WorldToScreen(p));
    // table spans in mockup coords (1080 wide reference): x from left, y from top
    float k = 1080f / Screen.width;
    return $"screen={Screen.width}x{Screen.height} scale={rig.DisplayScale:F3} anchorPx=[{s.xMin:F1},{s.yMin:F1} → {s.xMax:F1},{s.yMax:F1}] tablePx=[{bl.x:F1},{bl.y:F1} → {tr.x:F1},{tr.y:F1}] " +
           $"tableMock(x {bl.x*k:F0}→{tr.x*k:F0}, yTop {(Screen.height-tr.y)*k:F0}→{(Screen.height-bl.y)*k:F0}) roundTripErr={(back-p).magnitude:F5}";
  }
}
