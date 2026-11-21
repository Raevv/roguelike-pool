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

    // ---------------------------------------------------------------- Step 2: top elements

    public static readonly Color MockGrey = Hex("A1A1A1"), MockCoin = Hex("FFF568"), MockPurple = Hex("92278F"),
        MockRed = Hex("9E0B0F"), MockOrange = Hex("F7941D"), MockBlue = Hex("0076A3"), MockGreen = Hex("007236");
    static readonly Color TextWhite = Color.white;
    public const float MinTouch = 120f;

    [MenuItem("Tools/Roguelike Pool/UI Layout/2. Top elements (settings, stage, score, money)")]
    public static string Step2()
    {
        var root = RequireRoot();
        var content = Content(root);
        var hud = Object.FindAnyObjectByType<StageHUD>(FindObjectsInactive.Include);
        if (hud == null) throw new System.InvalidOperationException("No StageHUD in the scene.");

        // TopBar = the StageHUD object, spanning the table column (mockup x 41 → 847, y 0 → 430):
        // its center is the table axis and its right edge the table's right edge.
        var top = (RectTransform)hud.transform;
        top.name = "TopBar";
        top.SetParent(content, false);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.offsetMin = new Vector2(41f - Side, -430f);
        top.offsetMax = new Vector2(847f - ContentRight, 0f);
        var plate = top.GetComponent<RawImage>();
        if (plate != null) plate.enabled = false; // the old plate is not in the mockup

        var so = new SerializedObject(hud);
        var stageTitle = (TMP_Text)so.FindProperty("stageValue").objectReferenceValue;
        var score = (TMP_Text)so.FindProperty("scoreValue").objectReferenceValue;
        var shots = (TMP_Text)so.FindProperty("shotsValue").objectReferenceValue;
        foreach (var label in new[] { "stageLabel", "scoreLabel", "shotsLabel" })
        {
            var t = (TMP_Text)so.FindProperty(label).objectReferenceValue;
            if (t != null) t.gameObject.SetActive(false);
        }

        stageTitle.name = "StageTitle";
        PlaceTop(stageTitle.rectTransform, new Vector2(0.5f, 1f), 0f, 75f, new Vector2(800f, 115f));
        SetText(stageTitle, "Stage 1", 133f, TextAlignmentOptions.Center);
        score.name = "ScoreObjective";
        PlaceTop(score.rectTransform, new Vector2(0.5f, 1f), 0f, 230f, new Vector2(800f, 80f));
        SetText(score, "0 / 100", 95f, TextAlignmentOptions.Center);
        // Shots text: kept below the table until the ShotsCounter icons replace it (step 5).
        PlaceTop(shots.rectTransform, new Vector2(0.5f, 1f), 0f, 1746f, new Vector2(400f, 64f));
        SetText(shots, "50/50", 57f, TextAlignmentOptions.Center);

        // Money: amount right-aligned + 64×64 coin, on the table's right edge (x = 847), y 345 → 405.
        var money = Child(top, "Money");
        PlaceTop(money, new Vector2(1f, 1f), 0f, 345f, new Vector2(400f, 60f));
        var moneyDisplay = money.GetComponent<MoneyDisplay>();
        if (moneyDisplay == null) moneyDisplay = money.gameObject.AddComponent<MoneyDisplay>();
        var coin = Placeholder(money, "Placeholder_Coin", MockCoin);
        coin.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        Place(coin.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(64f, 64f));
        var amount = Text(money, "MoneyText", "999", 86f, TextAlignmentOptions.Right);
        Place(amount.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-92f, 0f), new Vector2(300f, 60f));
        Wire(moneyDisplay, ("amountText", amount), ("coinIcon", coin));

        // Settings button: 100×100 grey square, top left (mockup x 12 → pushed to the 40 px margin, y 18).
        var settings = TouchButton(content, "SettingsButton", "Placeholder_Settings", MockGrey, new Vector2(100f, 100f));
        Place(settings.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(50f, -68f), null);

        // Settings panel: full-screen overlay (outside the safe area so the veil covers everything).
        var overlays = Child(root.transform, "Overlays");
        Stretch(overlays);
        overlays.SetAsLastSibling();
        var panel = Child(overlays, "SettingsPanel");
        Stretch(panel);
        // The veil is a sibling of the menu (not its parent), so a tap on the menu never bubbles up to it.
        var veilRt = Child(panel, "Veil");
        Stretch(veilRt);
        veilRt.SetSiblingIndex(0);
        var veil = veilRt.GetComponent<Image>();
        if (veil == null) veil = veilRt.gameObject.AddComponent<Image>();
        veil.color = new Color(0f, 0f, 0f, 0.6f);
        veil.raycastTarget = true; // blocks aiming while open
        var veilButton = veilRt.GetComponent<Button>();
        if (veilButton == null) veilButton = veilRt.gameObject.AddComponent<Button>();
        veilButton.transition = Selectable.Transition.None;
        veilButton.navigation = new Navigation { mode = Navigation.Mode.None };
        var menu = Placeholder(panel, "Placeholder_SettingsMenu", MockGrey);
        Place(menu.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700f, 900f));
        menu.raycastTarget = true; // a tap on the menu itself does not close it
        var title = Text(menu.transform, "Title", "MENU", 95f, TextAlignmentOptions.Center);
        Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(640f, 110f));
        string[] names = { "OptionsButton", "RestartButton", "QuitButton", "CloseButton" };
        string[] labels = { "Paramètres", "Recommencer la partie", "Quitter", "Fermer" };
        var buttons = new Button[4];
        var buttonLabels = new TMP_Text[4];
        for (int i = 0; i < 4; i++)
        {
            var b = TouchButton(menu.transform, names[i], "Placeholder_" + names[i], new Color(0.35f, 0.35f, 0.35f), new Vector2(560f, 120f));
            Place(b.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -200f - i * 165f), null);
            buttons[i] = b;
            buttonLabels[i] = Text(b.transform.GetChild(0), "Label", labels[i], 57f, TextAlignmentOptions.Center);
            Stretch(buttonLabels[i].rectTransform);
        }
        panel.gameObject.SetActive(false);

        var settingsMenu = settings.GetComponent<SettingsMenu>();
        if (settingsMenu == null) settingsMenu = settings.gameObject.AddComponent<SettingsMenu>();
        Wire(settingsMenu, ("openButton", settings), ("panel", panel.gameObject), ("veilButton", veilButton),
             ("optionsButton", buttons[0]), ("restartButton", buttons[1]), ("quitButton", buttons[2]), ("closeButton", buttons[3]),
             ("titleText", title), ("optionsLabel", buttonLabels[0]), ("restartLabel", buttonLabels[1]),
             ("quitLabel", buttonLabels[2]), ("closeLabel", buttonLabels[3]));

        // New text formats (the asset keeps hand edits of every other text).
        var texts = AssetDatabase.LoadAssetAtPath<GameTexts>("Assets/Data/GameTexts.asset");
        if (texts != null)
        {
            texts.hudStageFormat = "Stage {0}";
            texts.hudScoreFormat = "{0} / {1}";
            EditorUtility.SetDirty(texts);
            AssetDatabase.SaveAssetIfDirty(texts);
        }
        return Save("Step 2 done.");
    }

    // ---------------------------------------------------------------- Step 3: TableAnchor + camera

    [MenuItem("Tools/Roguelike Pool/UI Layout/3. TableAnchor + TableFitter (camera)")]
    public static string Step3()
    {
        var root = RequireRoot();
        var content = Content(root);

        // TableAnchor: mockup x 41 → 847, y 430 → 1693, stretched with the content area so it grows on
        // taller screens (the right column and the top/bottom rows keep their size).
        var anchor = Child(content, "TableAnchor");
        StretchMock(anchor, 41f, 430f, 847f, 1693f);
        var fitter = anchor.GetComponent<TableFitter>();
        if (fitter == null) fitter = anchor.gameObject.AddComponent<TableFitter>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        if (table != null) Wire(fitter, ("table", table));
        anchor.SetSiblingIndex(0);

        // The message banner (scratch) is centered on the table: TableArea follows the anchor.
        var tableArea = content.Find("TableArea") as RectTransform;
        if (tableArea != null) StretchMock(tableArea, 41f, 430f, 847f, 1693f);

        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        var shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/PixelUpscale.shader");
        if (bootstrap != null) Wire(bootstrap, ("tableFitter", fitter), ("pixelUpscaleShader", shader));
        return Save($"Step 3 done (bootstrap wired: {bootstrap != null}, shader: {shader != null}).");
    }

    // ---------------------------------------------------------------- Step 4: right column

    public const string BonusSlotPrefabPath = "Assets/Prefabs/UI/BonusSlot.prefab";

    [MenuItem("Tools/Roguelike Pool/UI Layout/4. Right column (bonus tabs, power bar, buttons)")]
    public static string Step4()
    {
        var root = RequireRoot();
        var content = Content(root);
        var safe = (RectTransform)content.parent;
        var shotUI = root.GetComponent<ShotUI>();
        var so = new SerializedObject(shotUI);
        var gauge = (PowerGauge)so.FindProperty("gauge").objectReferenceValue;
        var shoot = (Button)so.FindProperty("shootButton").objectReferenceValue;
        var fine = (Button)so.FindProperty("fineButton").objectReferenceValue;

        // Power bar, TIRER and VISÉE FINE leave the slate panel, which is deleted (no background).
        var gaugeRt = (RectTransform)gauge.transform;
        gaugeRt.SetParent(content, false);
        // x 931 → 1001 (70 px), y 681 → 1481: right-anchored, stretched vertically with the screen.
        gaugeRt.anchorMin = new Vector2(1f, 0f);
        gaugeRt.anchorMax = new Vector2(1f, 1f);
        gaugeRt.pivot = new Vector2(1f, 0.5f);
        gaugeRt.offsetMin = new Vector2(931f - ContentRight, ContentBottom - 1481f);
        gaugeRt.offsetMax = new Vector2(1001f - ContentRight, -681f);

        // TIRER: x 869 → 1063 in the mockup, cut at the 40 px margin (→ 1040), y 1532 → 1690.
        var shootRt = (RectTransform)shoot.transform;
        shootRt.SetParent(content, false);
        PlaceBottomRight(shootRt, 869f, 1532f, ContentRight, 1690f);
        // VISÉE FINE under TIRER: x 869 → 1040, y 1715 → 1835 (120 px touch height).
        var fineRt = (RectTransform)fine.transform;
        fineRt.SetParent(content, false);
        PlaceBottomRight(fineRt, 869f, 1715f, ContentRight, 1835f);
        foreach (var label in new[] { shoot.GetComponentInChildren<TMP_Text>(true), fine.GetComponentInChildren<TMP_Text>(true) })
            if (label != null) label.fontSize = 38f;

        var panel = content.Find("Panel");
        if (panel != null) Object.DestroyImmediate(panel.gameObject); // with the FORCE title
        Wire(shotUI, ("panelImage", null), ("forceTitle", null));

        // Bonus tabs: on the right screen edge (safe area, no margin), x 931 → 1080, y 26 → 591.
        var prefab = BonusSlotPrefab();
        var tabs = Child(safe, "BonusTabs");
        Place(tabs, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -26f), new Vector2(149f, 565f));
        var layout = tabs.GetComponent<VerticalLayoutGroup>();
        if (layout == null) layout = tabs.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = -30f; // 5 × 137 − 4 × 30 = 565: the slight overlap of the mockup
        layout.childAlignment = TextAnchor.UpperRight;
        layout.reverseArrangement = true; // first child at the bottom, the top tab drawn over the one below
        layout.childControlWidth = layout.childControlHeight = false;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        if (tabs.GetComponent<BonusTabs>() == null) tabs.gameObject.AddComponent<BonusTabs>();
        for (int i = tabs.childCount - 1; i >= 0; i--) Object.DestroyImmediate(tabs.GetChild(i).gameObject);
        for (int i = 5; i >= 1; i--) // children bottom → top
        {
            var slot = (GameObject)PrefabUtility.InstantiatePrefab(prefab, tabs);
            slot.name = "BonusSlot_" + i;
        }
        tabs.SetAsLastSibling();
        return Save("Step 4 done.");
    }

    /// <summary>The bonus tab prefab: touch zone + Button + BonusSlot, a purple visual that slides, a shadow.</summary>
    static GameObject BonusSlotPrefab()
    {
        System.IO.Directory.CreateDirectory("Assets/Prefabs/UI");
        var go = new GameObject("BonusSlot", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BonusSlot));
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(149f, 137f);
        var hit = go.GetComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f); // invisible touch zone
        var visual = Placeholder(rt, "Placeholder_Bonus", MockPurple);
        // Extends 30 px past the right edge so sliding left never opens a gap.
        visual.rectTransform.anchorMin = Vector2.zero;
        visual.rectTransform.anchorMax = Vector2.one;
        visual.rectTransform.offsetMin = Vector2.zero;
        visual.rectTransform.offsetMax = new Vector2(30f, 0f);
        var shadow = Placeholder(visual.transform, "Shadow", new Color(0f, 0f, 0f, 0.45f));
        shadow.rectTransform.anchorMin = Vector2.zero;
        shadow.rectTransform.anchorMax = new Vector2(1f, 0f);
        shadow.rectTransform.pivot = new Vector2(0.5f, 1f);
        shadow.rectTransform.anchoredPosition = Vector2.zero;
        shadow.rectTransform.sizeDelta = new Vector2(0f, 12f);
        var button = go.GetComponent<Button>();
        button.targetGraphic = visual;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Wire(go.GetComponent<BonusSlot>(), ("visual", visual.rectTransform));
        var prefab = PrefabUtility.SaveAsPrefabAsset(go, BonusSlotPrefabPath);
        Object.DestroyImmediate(go);
        return prefab;
    }

    /// <summary>Places a Content child by its mockup edges, anchored to the content's bottom-right corner.</summary>
    static void PlaceBottomRight(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(right - ContentRight, ContentBottom - bottom);
        rt.sizeDelta = new Vector2(right - left, bottom - top);
    }

    /// <summary>Stretches a Content child so its edges sit at these mockup coordinates on 1080×1920.</summary>
    public static void StretchMock(RectTransform rt, float left, float top, float right, float bottom)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(left - Side, ContentBottom - bottom);
        rt.offsetMax = new Vector2(right - ContentRight, -top);
    }

    // ---------------------------------------------------------------- helpers

    static RectTransform Content(GameObject root)
    {
        var t = root.transform.Find("SafeArea/Content");
        if (t == null) throw new System.InvalidOperationException("Run step 1 first (no UI/SafeArea/Content).");
        return (RectTransform)t;
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }

    /// <summary>Places a rect hanging from the top of its parent at a mockup y (top edge), x offset from the anchor.</summary>
    public static void PlaceTop(RectTransform rt, Vector2 anchor, float x, float mockTop, Vector2 size)
    {
        Place(rt, anchor, new Vector2(anchor.x, 1f), new Vector2(x, -mockTop), size);
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2? size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = position;
        if (size.HasValue) rt.sizeDelta = size.Value;
    }

    /// <summary>A plain colored Image named Placeholder_… (found or created).</summary>
    public static Image Placeholder(Transform parent, string name, Color color)
    {
        var rt = Child(parent, name);
        var img = rt.GetComponent<Image>();
        if (img == null) img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    /// <summary>
    /// A button whose touch zone is at least <see cref="MinTouch"/> on each side: the root holds a
    /// transparent raycast Image of max(visual, 120) and the Button; the visual is a centered child.
    /// </summary>
    public static Button TouchButton(Transform parent, string name, string visualName, Color color, Vector2 visualSize)
    {
        var rt = Child(parent, name);
        rt.sizeDelta = new Vector2(Mathf.Max(MinTouch, visualSize.x), Mathf.Max(MinTouch, visualSize.y));
        var hit = rt.GetComponent<Image>();
        if (hit == null) hit = rt.gameObject.AddComponent<Image>();
        hit.color = new Color(1f, 1f, 1f, 0f); // invisible, still catches touches
        hit.raycastTarget = true;
        var visual = Placeholder(rt, visualName, color);
        visual.transform.SetSiblingIndex(0);
        Place(visual.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, visualSize);
        var button = rt.GetComponent<Button>();
        if (button == null) button = rt.gameObject.AddComponent<Button>();
        button.targetGraphic = visual; // color tint on the visual only
        button.transition = Selectable.Transition.ColorTint;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        return button;
    }

    public static TMP_Text Text(Transform parent, string name, string value, float size, TextAlignmentOptions align)
    {
        var rt = Child(parent, name);
        var tmp = rt.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = rt.gameObject.AddComponent<TextMeshProUGUI>(); // project default font (VeniceClassic)
        SetText(tmp, value, size, align);
        tmp.color = TextWhite;
        return tmp;
    }

    static void SetText(TMP_Text tmp, string value, float size, TextAlignmentOptions align)
    {
        tmp.text = value;
        tmp.fontSize = size;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false; // texts never block aiming
    }

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
