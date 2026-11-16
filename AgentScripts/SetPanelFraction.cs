using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SetPanelFraction
{
    public static string Run()
    {
        var boot = Object.FindAnyObjectByType<RoguelikePool.GameBootstrap>();
        Undo.RecordObject(boot, "Panel fraction");
        boot.uiPanelFraction = 0.2f;
        EditorUtility.SetDirty(boot);
        EditorSceneManager.SaveScene(boot.gameObject.scene);
        return "uiPanelFraction=" + boot.uiPanelFraction + " in " + boot.gameObject.scene.path;
    }
}
