using RoguelikePool;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the game UI hierarchy (canvas, right panel, buttons, stage HUD, message banner, end screens)
/// as real objects of the open scene, with every text as a TextMeshPro - Text (UI) object, and wires
/// the [SerializeField] references of ShotUI, PowerGauge, StageHUD, StageScreens and GameBootstrap.
/// One canvas unit = one art pixel (the runtime sets the canvas scale factor to the pixel scale), so
/// the positions below are in art pixels, for the 540×960 reference screen.
/// Run once; afterwards tune everything by hand in the scene (rebuilding wipes those edits).
/// OUTDATED since the ui-layout branch: it builds the old 540×960 layout. The current layout comes from
/// running it once, then Tools > Roguelike Pool > UI Layout > 1 → 6 (UILayoutBuilder), in order.
/// </summary>
public static class UISceneBuilder
{
    public const string RootName = "UI";

    static readonly Color Gold = new Color32(0xf5, 0xc8, 0x6a, 0xff);
    static readonly Color Light = new Color32(0xff, 0xf0, 0xcf, 0xff);
    static readonly Color Stone = new Color32(0xd8, 0xcb, 0xb0, 0xff);
    static readonly Color LabelGold = new Color32(0xc9, 0xa5, 0x60, 0xff);
    static readonly Color DarkInk = new Color32(0x2a, 0x14, 0x08, 0xff);

    const float TextSize = 19f;   // VeniceClassic's native pixel size: 1 font pixel = 1 art pixel
    const float TitleSize = 38f;  // 2× native

    [MenuItem("Tools/Roguelike Pool/8. Build UI in the scene (canvas and TextMeshPro texts)")]
    static void BuildMenu()
    {
        if (GameObject.Find(RootName) != null &&
            !EditorUtility.DisplayDialog("Build UI", "Un objet \"UI\" existe déjà dans la scène. Le supprimer et le reconstruire ? Les réglages faits à la main dessus seront perdus, et ce menu reconstruit l'ANCIENNE mise en page : il faudra relancer UI Layout 1 à 6.", "Reconstruire", "Annuler"))
            return;
        Build(true);
    }

    /// <summary>Builds the UI in the open scene. Without <paramref name="rebuild"/>, an existing "UI" object is kept.</summary>
    public static string Build(bool rebuild)
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!rebuild) return "UI already in the scene: nothing done.";
            Object.DestroyImmediate(existing);
        }

        // Canvas.
        var root = new GameObject(RootName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = true;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        scaler.referencePixelsPerUnit = 1f;
        var shotUI = root.AddComponent<ShotUI>();

        // Right panel.
        var panel = NewImage("Panel", root.transform);
        Stretch(panel.rectTransform, new Vector2(0.8f, 0f), Vector2.one);
        var forceTitle = NewText("ForceTitle", panel.transform, "FORCE", TextSize, Gold);
        Place(forceTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1f, -12f), new Vector2(96f, 22f));

        var gaugeGo = new GameObject("PowerGauge", typeof(RectTransform));
        gaugeGo.transform.SetParent(panel.transform, false);
        Place((RectTransform)gaugeGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f), new Vector2(30f, 500f));
        var gauge = gaugeGo.AddComponent<PowerGauge>();
        var percent = NewText("PercentText", gaugeGo.transform, "50%", TextSize, Light);
        Place(percent.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(1f, -4f), new Vector2(64f, 22f));

        var fineButton = NewButton("FineAimButton", panel.transform, new Vector2(0f, 80f), new Vector2(92f, 48f));
        var fineLabel = NewText("Label", fineButton.transform, "VISÉE\nFINE", TextSize, Stone);
        Stretch(fineLabel.rectTransform, Vector2.zero, Vector2.one);
        fineLabel.lineSpacing = -12f;

        var shootButton = NewButton("ShootButton", panel.transform, new Vector2(0f, 10f), new Vector2(96f, 60f));
        var shootLabel = NewText("Label", shootButton.transform, "TIRER", TextSize, Light);
        Stretch(shootLabel.rectTransform, Vector2.zero, Vector2.one);

        // Table area (left of the panel).
        var tableArea = new GameObject("TableArea", typeof(RectTransform)).GetComponent<RectTransform>();
        tableArea.SetParent(root.transform, false);
        Stretch(tableArea, Vector2.zero, new Vector2(0.8f, 1f));

        // Stage HUD: three columns of 104 px inside a 332 px plate.
        var hudGo = new GameObject("StageHUD", typeof(RectTransform), typeof(RawImage));
        hudGo.transform.SetParent(tableArea, false);
        var hudPlate = hudGo.GetComponent<RawImage>();
        hudPlate.raycastTarget = false;
        Place(hudPlate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(332f, 50f));
        var hud = hudGo.AddComponent<StageHUD>();
        string[] labels = { "STAGE", "SCORE", "TIRS" };
        string[] values = { "1/6", "0/100", "5/5" };
        string[] names = { "Stage", "Score", "Shots" };
        var hudTexts = new TMP_Text[6];
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 104f;
            var label = NewText(names[i] + "Label", hudGo.transform, labels[i], TextSize, LabelGold);
            Place(label.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -3f), new Vector2(100f, 22f));
            var value = NewText(names[i] + "Value", hudGo.transform, values[i], TextSize, Light);
            Place(value.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -24f), new Vector2(100f, 22f));
            hudTexts[i * 2] = label;
            hudTexts[i * 2 + 1] = value;
        }

        // Message banner (hidden until a message is shown; its plate is resized around the text).
        var bannerGo = new GameObject("Banner", typeof(RectTransform), typeof(RawImage));
        bannerGo.transform.SetParent(tableArea, false);
        var banner = bannerGo.GetComponent<RawImage>();
        banner.raycastTarget = false;
        banner.color = Color.white;
        Place(banner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 38f));
        var bannerText = NewText("Text", bannerGo.transform, "Blanche empochée : tir annulé", TextSize, Gold);
        Stretch(bannerText.rectTransform, Vector2.zero, Vector2.one);
        bannerGo.SetActive(false);

        // End screens: veil (blocks aiming) > slate panel > title, two lines, one button.
        var screensGo = new GameObject("StageScreens", typeof(RectTransform));
        screensGo.transform.SetParent(tableArea, false);
        Stretch((RectTransform)screensGo.transform, Vector2.zero, Vector2.one);
        var screens = screensGo.AddComponent<StageScreens>();
        var veil = NewImage("Veil", screensGo.transform);
        veil.color = new Color(0.01f, 0.01f, 0.03f, 0.6f);
        veil.raycastTarget = true;
        Stretch(veil.rectTransform, Vector2.zero, Vector2.one);
        var screenPanel = NewImage("Panel", veil.transform);
        Place(screenPanel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400f, 180f));
        var title = NewText("Title", screenPanel.transform, "VICTOIRE !", TitleSize, Gold);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(376f, 44f));
        var line1 = NewText("Line1", screenPanel.transform, "STAGE 1 RÉUSSI", TextSize, Stone);
        Place(line1.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -62f), new Vector2(376f, 22f));
        var line2 = NewText("Line2", screenPanel.transform, "SCORE 100/100 - 2 TIRS RESTANTS", TextSize, Stone);
        Place(line2.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -84f), new Vector2(376f, 22f));
        var screenButton = NewButton("Button", screenPanel.transform, new Vector2(0f, 16f), new Vector2(168f, 40f));
        var buttonLabel = NewText("Label", screenButton.transform, "STAGE SUIVANT", TextSize, DarkInk);
        Stretch(buttonLabel.rectTransform, Vector2.zero, Vector2.one);
        veil.gameObject.SetActive(false);

        // References.
        Wire(shotUI, ("scaler", scaler), ("raycaster", root.GetComponent<GraphicRaycaster>()), ("panelImage", panel),
             ("forceTitle", forceTitle), ("gauge", gauge), ("shootButton", shootButton), ("shootLabel", shootLabel),
             ("fineButton", fineButton), ("fineLabel", fineLabel), ("tableArea", tableArea),
             ("banner", banner), ("bannerText", bannerText));
        Wire(gauge, ("percentText", percent));
        Wire(hud, ("plate", hudPlate), ("stageLabel", hudTexts[0]), ("stageValue", hudTexts[1]),
             ("scoreLabel", hudTexts[2]), ("scoreValue", hudTexts[3]), ("shotsLabel", hudTexts[4]), ("shotsValue", hudTexts[5]));
        Wire(screens, ("veil", veil.gameObject), ("panelImage", screenPanel), ("title", title), ("line1", line1),
             ("line2", line2), ("button", screenButton), ("buttonLabel", buttonLabel));

        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        if (bootstrap != null) Wire(bootstrap, ("shotUI", shotUI), ("stageHUD", hud), ("stageScreens", screens));

        if (Object.FindAnyObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var scene = root.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;
        return $"UI built in {scene.path} (bootstrap wired: {bootstrap != null}).";
    }

    static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false; // texts must never block aiming
        return tmp;
    }

    static Image NewImage(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        return go.GetComponent<Image>();
    }

    static Button NewButton(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var image = NewImage(name, parent);
        Place(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.SpriteSwap;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Wire(Object target, params (string field, Object value)[] refs)
    {
        var so = new SerializedObject(target);
        foreach (var (field, value) in refs)
        {
            var prop = so.FindProperty(field);
            if (prop == null) { Debug.LogError($"UISceneBuilder: no field '{field}' on {target.GetType().Name}"); continue; }
            prop.objectReferenceValue = value;
        }
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
