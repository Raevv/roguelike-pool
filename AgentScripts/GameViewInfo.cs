using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class GameViewInfo
{
    public static string Run()
    {
        var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
        var gv = Resources.FindObjectsOfTypeAll(t);
        if (gv.Length == 0) return "no game view";
        var w = (EditorWindow)gv[0];
        const BindingFlags f = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        var zoomArea = t.GetField("m_ZoomArea", f)?.GetValue(w);
        object scale = zoomArea?.GetType().GetProperty("scale", f)?.GetValue(zoomArea);
        return $"window={w.position.width}x{w.position.height} zoomScale={scale} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} target={EditorUserBuildSettings.activeBuildTarget}";
    }
}
