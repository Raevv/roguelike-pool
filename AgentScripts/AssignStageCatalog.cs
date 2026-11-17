using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoguelikePool;

public static class AssignStageCatalog
{
    // Assigns Assets/Data/StageCatalog.asset to GameBootstrap.stageCatalog in the Pool scene and saves it.
    public static string Run()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Pool.unity") return "active scene is " + scene.path + ", open Pool.unity first";
        var boot = Object.FindAnyObjectByType<GameBootstrap>();
        var catalog = AssetDatabase.LoadAssetAtPath<StageCatalog>("Assets/Data/StageCatalog.asset");
        if (boot == null || catalog == null) return $"boot={boot != null} catalog={catalog != null}";
        var so = new SerializedObject(boot);
        so.FindProperty("stageCatalog").objectReferenceValue = catalog;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"assigned {AssetDatabase.GetAssetPath(catalog)} ({catalog.Count} stages) to {boot.name} in {scene.path}";
    }
}
