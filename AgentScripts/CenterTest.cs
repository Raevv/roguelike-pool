using UnityEngine;
using RoguelikePool;
public static class CenterTest {
  // Horizontal centers / edges (mockup coords, 1080 wide) of the power bar's drawn frame, TIRER and VISÉE FINE.
  public static string Run() {
    float k = 1080f / Screen.width; var c = new Vector3[4];
    string X(Transform t) { ((RectTransform)t).GetWorldCorners(c); return $"x {c[0].x * k:F1}→{c[2].x * k:F1} center {(c[0].x + c[2].x) / 2 * k:F1}"; }
    var bar = Object.FindAnyObjectByType<PowerGauge>().transform;
    return $"{Screen.width}x{Screen.height} | bar {X(bar)} | drawn {X(bar.Find("Gauge"))} | shoot {X(GameObject.Find("ShootButton").transform)} | fine {X(GameObject.Find("FineAimButton").transform)}";
  }
}
