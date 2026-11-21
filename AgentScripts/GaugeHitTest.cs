using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikePool;
public static class GaugeHitTest {
  // Raycasts the UI at the gauge rect's left/right edges (2 px inside) and center; reports what is hit and the drawn width.
  public static string Run() {
    var g = Object.FindAnyObjectByType<PowerGauge>();
    var rt = (RectTransform)g.transform;
    Vector3[] c = new Vector3[4]; rt.GetWorldCorners(c);
    var gr = g.GetComponentInParent<GraphicRaycaster>();
    var img = rt.Find("Gauge") as RectTransform; Vector3[] d = new Vector3[4]; img.GetWorldCorners(d);
    var sb = new System.Text.StringBuilder($"touchRectPx x {c[0].x:F0}→{c[2].x:F0} (w {c[2].x-c[0].x:F0}) gaugeTexPx x {d[0].x:F0}→{d[2].x:F0} | ");
    float y = (c[0].y + c[1].y) / 2;
    foreach (var x in new[] { c[0].x + 2, (c[0].x + c[2].x) / 2, c[2].x - 2 }) {
      var res = new List<RaycastResult>();
      gr.Raycast(new PointerEventData(EventSystem.current) { position = new Vector2(x, y) }, res);
      string hit = "none"; foreach (var r in res) if (r.gameObject.GetComponentInParent<Slider>() != null) { hit = r.gameObject.name; break; }
      sb.Append($"x={x:F0}: slider hit={hit} | ");
    }
    return sb.ToString();
  }
}
