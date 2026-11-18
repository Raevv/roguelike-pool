using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BuildPoolScene
{
    public static string Run()
    {
        // Top-down game: no gravity, bounce even at low speeds, finer physics step.
        Physics2D.gravity = Vector2.zero;
        Physics2D.bounceThreshold = 0.1f;
        Time.fixedDeltaTime = 1f / 120f;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 9f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.09f, 0.08f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        new GameObject("Game").AddComponent<RoguelikePool.GameBootstrap>();

        const string path = "Assets/Scenes/Pool.unity";
        EditorSceneManager.SaveScene(scene, path);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(path, true) };
        // Sprite table and cue: prefab references on GameBootstrap, instances placed in the scene, camera framed.
        TableSpriteBuilder.AssignPrefabToBootstrap();
        TableSpriteBuilder.PlaceInScene();
        AssetDatabase.SaveAssets();
        return "Saved " + path + " | gravity=" + Physics2D.gravity + " bounce=" + Physics2D.bounceThreshold + " dt=" + Time.fixedDeltaTime;
    }
}
