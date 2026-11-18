using RoguelikePool;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports the table/cue sprites and generates the PoolTable prefab (menu Tools > Roguelike Pool).
///
/// Every number below was measured in Assets/Art/Sprites/pool_table.png (1200 x 1800 px, pixel
/// coordinates with the origin at the TOP-LEFT of the image, as in an image editor). The play area
/// is the felt inside the cushion noses: x 173..1033, y 135..1601 (860 x 1466 px). The sprite pivot
/// is the felt center, so the prefab origin is the center of the play area, like the old code table.
/// </summary>
public static class TableSpriteBuilder
{
    public const string TablePng = "Assets/Art/Sprites/pool_table.png";
    public const string CuePng = "Assets/Art/Sprites/cue_stick.png";
    public const string PrefabFolder = "Assets/Prefabs";
    public const string TablePrefabPath = PrefabFolder + "/PoolTable.prefab";
    public const string CushionMaterialPath = "Assets/Data/Cushion.physicsMaterial2D";
    public const string TableSortingLayer = "Table";

    // Felt (play area) edges in the PNG, in pixels from the top-left corner.
    const float FeltLeft = 173f, FeltRight = 1033f, FeltTop = 135f, FeltBottom = 1601f;
    const float ImageWidth = 1200f, ImageHeight = 1800f;
    /// <summary>Play width of the old code-drawn table (PoolTable.playWidth), kept identical.</summary>
    const float PlayWidthUnits = 12.7f;

    /// <summary>Pixels per unit: felt width in pixels / old play width in units (= 67.7165).</summary>
    public static float PixelsPerUnit => (FeltRight - FeltLeft) / PlayWidthUnits;

    static float FeltCenterX => (FeltLeft + FeltRight) / 2f;
    static float FeltCenterY => (FeltTop + FeltBottom) / 2f;

    // Cue sprite (64 x 900 px): the white tip's top row is 884 (from the bottom), centered on x = 32.
    const float CueTipPivotX = 32f / 64f, CueTipPivotY = 885f / 900f;

    // Cushion noses in the PNG: each segment runs along the nose line between its two jaw ends.
    // The back of the cushion (against the wood) is CushionDepth px behind the nose; jaws are at 45°.
    const float CushionDepth = 25f;

    /// <summary>PNG pixel (top-left origin) → prefab local position (felt center origin, Y up).</summary>
    static Vector2 Px(float x, float y) =>
        new Vector2((x - FeltCenterX) / PixelsPerUnit, (FeltCenterY - y) / PixelsPerUnit);

    [MenuItem("Tools/Roguelike Pool/Build All (sprites + PoolTable prefab)")]
    public static void BuildAll()
    {
        ImportSprites();
        BuildTablePrefab();
    }

    [MenuItem("Tools/Roguelike Pool/1. Import table and cue sprites")]
    public static void ImportSprites()
    {
        var feltPivot = new Vector2(FeltCenterX / ImageWidth, 1f - FeltCenterY / ImageHeight);
        ConfigureSprite(TablePng, feltPivot);
        ConfigureSprite(CuePng, new Vector2(CueTipPivotX, CueTipPivotY));
        Debug.Log($"[TableSprites] Sprites imported, Pixels Per Unit = {PixelsPerUnit:0.0000}, table pivot = {feltPivot}");
    }

    static void ConfigureSprite(string path, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null) { Debug.LogError($"[TableSprites] Missing {path}"); return; }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = PixelsPerUnit;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);

        // No platform override may bring compression back.
        foreach (var platform in new[] { "Standalone", "Android", "iPhone" })
        {
            var ps = importer.GetPlatformTextureSettings(platform);
            if (!ps.overridden) continue;
            ps.textureCompression = TextureImporterCompression.Uncompressed;
            ps.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(ps);
        }
        importer.SaveAndReimport();
    }

    [MenuItem("Tools/Roguelike Pool/2. Build PoolTable prefab")]
    public static void BuildTablePrefab()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TablePng);
        if (sprite == null) { Debug.LogError($"[TableSprites] {TablePng} is not imported as a Sprite: run step 1."); return; }
        EnsureSortingLayer(TableSortingLayer);
        var material = EnsureCushionMaterial();

        var root = new GameObject("PoolTable");
        try
        {
            // Visual.
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingLayerName = TableSortingLayer;
            sr.sortingOrder = 0;

            // Cushions: nose points first (point 0 → 1 is the nose edge), then the back points.
            var cushions = new GameObject("Cushions").transform;
            cushions.SetParent(root.transform, false);
            AddCushion(cushions, "Cushion_LeftTop", material, Vertical(FeltLeft, 240f, 789f, -1f));
            AddCushion(cushions, "Cushion_LeftBottom", material, Vertical(FeltLeft, 967f, 1504f, -1f));
            AddCushion(cushions, "Cushion_RightTop", material, Vertical(FeltRight, 240f, 789f, 1f));
            AddCushion(cushions, "Cushion_RightBottom", material, Vertical(FeltRight, 966f, 1506f, 1f));
            AddCushion(cushions, "Cushion_Top", material, Horizontal(FeltTop, 266f, 946f, -1f));
            AddCushion(cushions, "Cushion_Bottom", material, Horizontal(FeltBottom, 263f, 947f, 1f));

            // Pockets: approximate centers/radii read on the PNG (refined by detection in step 2).
            var pockets = new GameObject("Pockets").transform;
            pockets.SetParent(root.transform, false);
            AddPocket(pockets, "Pocket_TL", Px(166f, 141f), 80f);
            AddPocket(pockets, "Pocket_TR", Px(1040f, 141f), 80f);
            AddPocket(pockets, "Pocket_ML", Px(146f, 881f), 68f);
            AddPocket(pockets, "Pocket_MR", Px(1060f, 881f), 68f);
            AddPocket(pockets, "Pocket_BL", Px(166f, 1595f), 80f);
            AddPocket(pockets, "Pocket_BR", Px(1040f, 1595f), 80f);

            // Landmarks: same spots as the old table (quarter of the play length from the center).
            float quarter = (FeltBottom - FeltTop) / PixelsPerUnit / 4f;
            AddPoint(root.transform, "RackPoint", new Vector2(0f, quarter));
            AddPoint(root.transform, "CueBallSpawn", new Vector2(0f, -quarter));

            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, TablePrefabPath, out bool ok);
            Debug.Log(ok
                ? $"[TableSprites] {TablePrefabPath} saved. Play area {(FeltRight - FeltLeft) / PixelsPerUnit:0.###} x {(FeltBottom - FeltTop) / PixelsPerUnit:0.###} units."
                : $"[TableSprites] Failed to save {TablePrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>Long-rail cushion along x = noseX between y0 (top end) and y1 (bottom end); outward = -1 left, +1 right.</summary>
    static Vector2[] Vertical(float noseX, float y0, float y1, float outward)
    {
        float backX = noseX + outward * CushionDepth;
        return new[] { Px(noseX, y0), Px(noseX, y1), Px(backX, y1 + CushionDepth), Px(backX, y0 - CushionDepth) };
    }

    /// <summary>Short-rail cushion along y = noseY between x0 (left end) and x1 (right end); outward = -1 top, +1 bottom.</summary>
    static Vector2[] Horizontal(float noseY, float x0, float x1, float outward)
    {
        float backY = noseY + outward * CushionDepth;
        return new[] { Px(x0, noseY), Px(x1, noseY), Px(x1 + CushionDepth, backY), Px(x0 - CushionDepth, backY) };
    }

    static void AddCushion(Transform parent, string name, PhysicsMaterial2D material, Vector2[] points)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var poly = go.AddComponent<PolygonCollider2D>();
        poly.points = points;
        poly.sharedMaterial = material;
    }

    static void AddPocket(Transform parent, string name, Vector2 localPos, float radiusPx)
    {
        float radius = radiusPx / PixelsPerUnit;
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        var circle = go.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = radius;
        var pocket = go.AddComponent<Pocket>();
        pocket.radius = radius;
    }

    static void AddPoint(Transform parent, string name, Vector2 localPos)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
    }

    /// <summary>Cushion material as an asset, with the same settings as the one PoolTable creates in code.</summary>
    static PhysicsMaterial2D EnsureCushionMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(CushionMaterialPath);
        if (material != null) return material;
        var settings = AssetDatabase.LoadAssetAtPath<BallPhysicsSettings>("Assets/Data/BallPhysicsSettings.asset");
        material = new PhysicsMaterial2D("Cushion")
        {
            bounciness = settings != null ? settings.cushionBounciness : 0.86f,
            friction = 0f,
            bounceCombine = PhysicsMaterialCombine2D.Multiply,
            frictionCombine = PhysicsMaterialCombine2D.Multiply
        };
        AssetDatabase.CreateAsset(material, CushionMaterialPath);
        return material;
    }

    /// <summary>Adds a sorting layer drawn before Default (so balls and FX stay on top).</summary>
    static void EnsureSortingLayer(string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++)
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName) return;
        layers.InsertArrayElementAtIndex(0);
        var layer = layers.GetArrayElementAtIndex(0);
        layer.FindPropertyRelative("name").stringValue = layerName;
        layer.FindPropertyRelative("uniqueID").intValue = Random.Range(1, int.MaxValue);
        layer.FindPropertyRelative("locked").boolValue = false;
        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }
}
