using UnityEngine;
using System.Text;
public static class DumpUI {
  public static string Run() {
    var sb = new StringBuilder();
    foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()) Walk(root.transform, 0, sb, root.name=="UI");
    return sb.ToString();
  }
  static void Walk(Transform t, int d, StringBuilder sb, bool deep) {
    var rt = t as RectTransform;
    sb.Append(new string(' ', d*2)).Append(t.name).Append(t.gameObject.activeSelf?"":" (off)");
    if (rt != null) sb.Append($" a[{rt.anchorMin}-{rt.anchorMax}] p{rt.pivot} pos{rt.anchoredPosition} size{rt.sizeDelta}");
    foreach (var c in t.GetComponents<Component>()) if (!(c is Transform)) sb.Append(" ").Append(c.GetType().Name);
    sb.Append('\n');
    if (deep || d==0) foreach (Transform c in t) Walk(c, d+1, sb, deep);
  }
}
