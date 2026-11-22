using RoguelikePool;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Score popups (branch score-popups): builds the ScorePopup prefab and the UI/PopupLayer object of the
/// open scene, and wires them. Safe to rerun: the prefab is rebuilt, the layer is reused.
/// </summary>
public static class ScorePopupBuilder
{
    public const string PrefabPath = "Assets/Prefabs/UI/ScorePopup.prefab";
    /// <summary>VeniceClassic is sampled at 76 (4× its native 19 pt): multiples of 38 keep crisp art pixels.</summary>
    const float FontSize = 76f;

    [MenuItem("Tools/Roguelike Pool/Score Popups/2. Prefab ScorePopup + PopupLayer")]
    public static string Step2()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var prefab = BuildPrefab();

        var root = GameObject.Find(UISceneBuilder.RootName);
        if (root == null) throw new System.InvalidOperationException("No \"UI\" object in the open scene.");
        var layerRt = UILayoutBuilder.Child(root.transform, "PopupLayer");
        UILayoutBuilder.Stretch(layerRt);
        // Above the game UI (and the cue), below the overlays (end screens, settings).
        var overlays = root.transform.Find("Overlays");
        if (overlays != null && layerRt.GetSiblingIndex() > overlays.GetSiblingIndex())
            layerRt.SetSiblingIndex(overlays.GetSiblingIndex());
        var group = layerRt.GetComponent<CanvasGroup>();
        if (group == null) group = layerRt.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false; // never blocks aiming
        group.interactable = false;
        var layer = layerRt.GetComponent<ScorePopupLayer>();
        if (layer == null) layer = layerRt.gameObject.AddComponent<ScorePopupLayer>();
        UILayoutBuilder.Wire(layer, ("popupPrefab", prefab.GetComponent<ScorePopup>()));

        var boot = Object.FindAnyObjectByType<GameBootstrap>(FindObjectsInactive.Include);
        if (boot != null)
        {
            Undo.RecordObject(boot, "Wire popups");
            boot.scorePopups = layer;
            EditorUtility.SetDirty(boot);
        }

        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"Score popups step 2 done: {PrefabPath}, UI/PopupLayer (sibling {layerRt.GetSiblingIndex()}), bootstrap wired={boot != null}.";
    }

    /// <summary>UI elements the popups must not cover (scene object names under UI).</summary>
    static readonly string[] AvoidNames =
    {
        "SettingsButton", "StageTitle", "ScoreObjective", "Money", "BonusTabs",
        "PowerGauge", "ShootButton", "FineAimButton", "ShotsCounter",
    };

    [MenuItem("Tools/Roguelike Pool/Score Popups/3. UI elements the popups avoid")]
    public static string Step3()
    {
        if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
        var layer = Object.FindAnyObjectByType<ScorePopupLayer>(FindObjectsInactive.Include);
        if (layer == null) throw new System.InvalidOperationException("Run step 2 first.");
        var root = GameObject.Find(UISceneBuilder.RootName).transform;
        var so = new SerializedObject(layer);
        var list = so.FindProperty("avoid");
        list.ClearArray();
        string found = "", missing = "";
        foreach (var name in AvoidNames)
        {
            var t = FindDeep(root, name) as RectTransform;
            if (t == null) { missing += name + " "; continue; }
            list.InsertArrayElementAtIndex(list.arraySize);
            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = t;
            found += $"{name}{t.rect.size} ";
        }
        so.ApplyModifiedPropertiesWithoutUndo();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return $"Score popups step 3 done. Avoid: {found}| missing: {missing}";
    }

    static Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }

    static GameObject BuildPrefab()
    {
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        var go = new GameObject("ScorePopup", typeof(RectTransform), typeof(ScorePopup));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(160f, 90f);

        var textRt = UILayoutBuilder.Child(go.transform, "Text");
        UILayoutBuilder.Stretch(textRt);
        var text = textRt.gameObject.AddComponent<TextMeshProUGUI>(); // project default font (VeniceClassic, drop shadow)
        text.text = "+1";
        text.fontSize = FontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.color = Color.white;
        text.raycastTarget = false;
        UILayoutBuilder.Wire(go.GetComponent<ScorePopup>(), ("text", text));

        var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }
}
