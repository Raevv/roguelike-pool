using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewFit
{
    // Sets the Game view zoom so the target fits the window (capped at 1x), and lists zoom methods.
    public static string Run()
    {
        var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var w = (EditorWindow)Resources.FindObjectsOfTypeAll(t)[0];
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var size = (Vector2)t.GetProperty("targetSize", f).GetValue(w);
        float fit = Mathf.Min((w.position.width) / size.x, (w.position.height - 22f) / size.y);
        float zoom = Mathf.Min(1f, fit);
        var snap = t.GetMethod("SnapZoom", f);
        if (snap != null) snap.Invoke(w, new object[] { zoom });
        w.Repaint();
        var zoomArea = t.GetField("m_ZoomArea", f).GetValue(w);
        var scale = zoomArea.GetType().GetProperty("scale", f).GetValue(zoomArea);
        return $"target={size} fit={fit:F2} applied={zoom:F2} snap={(snap != null)} nowScale={scale}";
    }
}
