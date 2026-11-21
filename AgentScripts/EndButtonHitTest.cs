using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using RoguelikePool;
public static class EndButtonHitTest {
  // With an end screen shown: raycasts above/below the end button's visual (inside the 120 px zone, and outside it).
  public static string Run() {
    var screens = Object.FindAnyObjectByType<StageScreens>();
    var button = screens.GetComponentInChildren<Button>(false);
    if (button == null) return "no end screen shown";
    var rt = (RectTransform)button.transform; var c = new Vector3[4]; rt.GetWorldCorners(c);
    var gr = rt.GetComponentInParent<GraphicRaycaster>();
    float k = rt.lossyScale.y; // screen px per canvas unit
    var sb = new System.Text.StringBuilder($"visual h={(c[1].y - c[0].y) / k:F0} units | ");
    float x = (c[0].x + c[2].x) / 2;
    foreach (var dy in new[] { 15f, 30f }) foreach (var top in new[] { true, false }) {
      float y = top ? c[1].y + dy * k : c[0].y - dy * k;
      var res = new List<RaycastResult>();
      gr.Raycast(new PointerEventData(EventSystem.current) { position = new Vector2(x, y) }, res);
      var hit = res.Count > 0 ? ExecuteEvents.GetEventHandler<IPointerClickHandler>(res[0].gameObject) : null;
      sb.Append($"{(top ? "above" : "below")} {dy}: {(hit == button.gameObject ? "BUTTON" : hit != null ? hit.name : "none")} | ");
    }
    return sb.ToString();
  }
}
