using RoguelikePool;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Moves the existing UI of the open scene to the mockup layout (References/UI_Layout.png), one step
/// at a time. Each step edits the scene objects in place (nothing is rebuilt from scratch, hand edits
/// on untouched objects survive) and saves the scene. Coordinates are those of the mockup, in pixels
/// of the 1080×1920 reference screen, top-left origin; <see cref="Content"/> is the mockup screen
/// minus the margins (40 px left/right, 80 px bottom), so a mockup x becomes x − 40 from its left edge.
/// </summary>
public static class UILayoutBuilder
{
    public const float RefWidth = 1080f, RefHeight = 1920f;
    public const float Side = 40f, Bottom = 80f;
    /// <summary>Right edge of the content area in mockup coordinates.</summary>
    public const float ContentRight = RefWidth - Side;
    /// <summary>Bottom edge of the content area in mockup coordinates.</summary>
    public const float ContentBottom = RefHeight - Bottom;

    // ---------------------------------------------------------------- Step 1: scaler, safe area, anchors

    [MenuItem("Tools/Roguelike Pool/UI Layout/1. Canvas Scaler + SafeArea")]
    public static string Step1()
    {
        var root = RequireRoot();
        var scaler = root.GetComponent<CanvasScaler>();
        bool firstRun = scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        scaler.referencePixelsPerUnit = 1f; // generated sprites are 1 texel per unit
        root.GetComponent<Canvas>().pixelPerfect = false; // non-integer scale: snapping would only jitter

        // SafeArea (Screen.safeArea) > Content (margins).
        var safe = Child(root.transform, "SafeArea");
        Stretch(safe);
        var content = Child(safe, "Content");
        Stretch(content);
        content.offsetMin = new Vector2(Side, Bottom);
        content.offsetMax = new Vector2(-Side, 0f);
        var safeArea = safe.GetComponent<SafeAreaPanel>();
        if (safeArea == null) safeArea = safe.gameObject.AddComponent<SafeAreaPanel>();
        Wire(safeArea, ("content", content));

        // Existing UI goes under Content. On the first run, everything was in art pixels of a 540×960
        // screen: double positions, sizes and fonts so the 1080×1920 reference looks as before.
        foreach (var name in new[] { "Panel", "TableArea" })
        {
            var t = root.transform.Find(name) ?? content.Find(name);
            if (t == null) { Debug.LogWarning($"UILayoutBuilder: {name} not found"); continue; }
            t.SetParent(content, false);
            if (firstRun) DoubleAll((RectTransform)t);
        }
        if (firstRun)
        {
            var gauge = Object.FindAnyObjectByType<PowerGauge>(FindObjectsInactive.Include);
            if (gauge != null) ((RectTransform)gauge.transform).sizeDelta = new Vector2(48f, 1240f); // the rect is now the frame (24 art px)
            var shotUI = root.GetComponent<ShotUI>();
            Wire(shotUI, ("bannerPaddingX", 80), ("bannerPaddingY", 32));
        }
        safe.SetSiblingIndex(0);
        return Save($"Step 1 done (first run: {firstRun}).");
    }

    // ---------------------------------------------------------------- helpers

    static GameObject RequireRoot()
    {
        var root = GameObject.Find(UISceneBuilder.RootName);
        if (root == null) throw new System.InvalidOperationException("No \"UI\" object in the open scene.");
        return root;
    }

    /// <summary>Finds or creates a RectTransform child.</summary>
    public static RectTransform Child(Transform parent, string name)
    {
        var t = parent.Find(name);
        if (t == null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            t = go.transform;
        }
        return (RectTransform)t;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void DoubleAll(RectTransform rt)
    {
        foreach (var t in rt.GetComponentsInChildren<RectTransform>(true))
        {
            t.anchoredPosition *= 2f;
            t.sizeDelta *= 2f;
            var text = t.GetComponent<TMP_Text>();
            if (text != null) text.fontSize *= 2f;
        }
    }

    public static void Wire(Object target, params (string field, object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"UILayoutBuilder: no field '{field}' on {target.GetType().Name}"); continue; }
            switch (value)
            {
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                default: prop.objectReferenceValue = (Object)value; break;
            }
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static string Save(string message)
    {
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        return message + " Saved " + scene.path;
    }
}
