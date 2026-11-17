using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using RoguelikePool;

public static class CreateBallPhysicsSettings
{
    const string Path = "Assets/Data/BallPhysicsSettings.asset";

    // Creates Assets/Data/BallPhysicsSettings.asset with the code defaults (never overwrites an existing one)
    // and assigns it to GameBootstrap.ballPhysics in the Pool scene, then saves the scene.
    public static string Run()
    {
        var settings = AssetDatabase.LoadAssetAtPath<BallPhysicsSettings>(Path);
        string created = "existing";
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<BallPhysicsSettings>();
            AssetDatabase.CreateAsset(settings, Path);
            AssetDatabase.SaveAssets();
            created = "created";
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Pool.unity") return $"{created} {Path}; active scene is {scene.path}, open Pool.unity first";
        var boot = Object.FindAnyObjectByType<GameBootstrap>();
        if (boot == null) return $"{created} {Path}; no GameBootstrap";
        var so = new SerializedObject(boot);
        so.FindProperty("ballPhysics").objectReferenceValue = settings;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"{created} {Path} (maxShotSpeed={settings.maxShotSpeed}) and assigned it to {boot.name}";
    }
}
