using UnityEngine;
using UnityEngine.UI;
using RoguelikePool;
public static class BonusTest {
  // click = slot number to click (0 = none); reports each slot's selected state and visual x
  public static string Run(int click) {
    var slots = Object.FindAnyObjectByType<BonusTabs>().GetComponentsInChildren<BonusSlot>();
    if (click > 0) GameObject.Find("BonusSlot_" + click).GetComponent<Button>().onClick.Invoke();
    var sb = new System.Text.StringBuilder();
    foreach (var s in slots) {
      var v = (RectTransform)s.transform.GetChild(0);
      var r = (RectTransform)s.transform;
      Vector3[] c = new Vector3[4]; r.GetWorldCorners(c);
      sb.Append($"{s.name}: sel={s.Selected} visX={v.anchoredPosition.x:F1} screenTopY={Screen.height - c[1].y:F0} | ");
    }
    return sb.ToString();
  }
}
