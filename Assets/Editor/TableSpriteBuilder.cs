using System.Collections.Generic;
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
        BuildCuePrefab();
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

            // Pockets: centers and radii detected in the PNG (dark discs).
            var pockets = new GameObject("Pockets").transform;
            pockets.SetParent(root.transform, false);
            foreach (var guess in PocketGuesses) AddPocket(pockets, guess.name, Vector2.zero, 1f);
            PlacePockets(pockets);

            // Landmarks: same spots as the old table (quarter of the play length from the center).
            float quarter = (FeltBottom - FeltTop) / PixelsPerUnit / 4f;
            AddPoint(root.transform, "RackPoint", new Vector2(0f, quarter));
            AddPoint(root.transform, "CueBallSpawn", new Vector2(0f, -quarter));

            SetupTableComponent(root);

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

    // ---------------------------------------------------------------- PoolTable component

    /// <summary>
    /// Adds (if missing) the PoolTable component to the prefab root and wires its references by child name,
    /// play area and visible bounds. Nothing is moved: hand edits of cushions, pockets and landmarks are kept.
    /// </summary>
    [MenuItem("Tools/Roguelike Pool/4. Set up the PoolTable component on the prefab")]
    public static void SetupTableComponentOnPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(TablePrefabPath);
        try
        {
            SetupTableComponent(root);
            PrefabUtility.SaveAsPrefabAsset(root, TablePrefabPath);
            Debug.Log("[TableSprites] PoolTable component set up on the prefab.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void SetupTableComponent(GameObject root)
    {
        var table = root.GetComponent<PoolTable>();
        if (table == null) table = root.AddComponent<PoolTable>();
        table.visual = root.transform.Find("Visual")?.GetComponent<SpriteRenderer>();
        var cushions = root.transform.Find("Cushions");
        table.cushions = cushions != null ? cushions.GetComponentsInChildren<PolygonCollider2D>(true) : new PolygonCollider2D[0];
        var pockets = new List<Pocket>();
        foreach (var (name, _) in PocketGuesses)
        {
            var p = root.transform.Find("Pockets/" + name)?.GetComponent<Pocket>();
            if (p != null) pockets.Add(p); else Debug.LogError($"[TableSprites] Missing Pockets/{name}");
        }
        table.pockets = pockets.ToArray();
        table.rackPoint = root.transform.Find("RackPoint");
        table.cueBallSpawn = root.transform.Find("CueBallSpawn");
        table.cushionMaterial = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(CushionMaterialPath);

        Vector2 min = Px(FeltLeft, FeltBottom), max = Px(FeltRight, FeltTop);
        table.playArea = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        table.tableBounds = OpaqueBounds();
        EditorUtility.SetDirty(table);
    }

    /// <summary>Bounds of the opaque pixels of the table PNG (frame and legs), in prefab local units.</summary>
    static Rect OpaqueBounds()
    {
        var img = LoadPixels(TablePng, out int w, out int h);
        int x0 = w, x1 = -1, y0 = h, y1 = -1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (img[y * w + x].a < 128) continue;
            if (x < x0) x0 = x; if (x > x1) x1 = x;
            if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
        Vector2 min = Px(x0, y1 + 1), max = Px(x1 + 1, y0);
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    /// <summary>Assigns the prefab to GameBootstrap.tablePrefab in Pool.unity and saves the scene.</summary>
    [MenuItem("Tools/Roguelike Pool/5. Assign the prefab to GameBootstrap (Pool scene)")]
    public static void AssignPrefabToBootstrap()
    {
        const string scenePath = "Assets/Scenes/Pool.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        var bootstrap = Object.FindAnyObjectByType<GameBootstrap>();
        if (bootstrap == null) { Debug.LogError("[TableSprites] No GameBootstrap in Pool.unity"); return; }
        var so = new SerializedObject(bootstrap);
        so.FindProperty("tablePrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<PoolTable>(TablePrefabPath);
        var cuePrefab = AssetDatabase.LoadAssetAtPath<CueStick>(CuePrefabPath);
        if (cuePrefab != null) so.FindProperty("cuePrefab").objectReferenceValue = cuePrefab;
        so.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[TableSprites] GameBootstrap.tablePrefab assigned in Pool.unity.");
    }

    // ---------------------------------------------------------------- Cue

    public const string CuePrefabPath = PrefabFolder + "/Cue.prefab";
    /// <summary>Cue ball radius + CueController.tipGap: resting tip distance, only for the prefab's default pose.</summary>
    const float CueRestDistance = 0.446875f + 0.15f;

    /// <summary>
    /// Cue prefab: CueRoot (CueStick, sits on the cue ball and turns with the aim) → Stick (sprite, pivot on
    /// the tip, rotated -90° so the tip points along +X) → Shadow (same sprite, fixed world offset).
    /// </summary>
    [MenuItem("Tools/Roguelike Pool/6. Build Cue prefab")]
    public static void BuildCuePrefab()
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CuePng);
        if (sprite == null) { Debug.LogError($"[TableSprites] {CuePng} is not imported as a Sprite: run step 1."); return; }

        var root = new GameObject("CueRoot");
        try
        {
            var cue = root.AddComponent<CueStick>();
            var stick = new GameObject("Stick");
            stick.transform.SetParent(root.transform, false);
            var body = stick.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sortingOrder = 20; // above the balls (10) and the aim line

            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(stick.transform, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = sprite;
            shadowRenderer.color = new Color(0.01f, 0.005f, 0.03f, 0.5f);
            shadowRenderer.sortingOrder = 19;
            shadow.AddComponent<WorldOffset>().offset = new Vector2(3f, -4f) / 24f; // 3, -4 px of the 24 px/unit render

            cue.stick = stick.transform;
            cue.Pose(Vector2.zero, Vector2.right, CueRestDistance); // default pose: tip just behind a ball at the origin

            if (!AssetDatabase.IsValidFolder(PrefabFolder)) AssetDatabase.CreateFolder("Assets", "Prefabs");
            PrefabUtility.SaveAsPrefabAsset(root, CuePrefabPath, out bool ok);
            Debug.Log(ok ? $"[TableSprites] {CuePrefabPath} saved." : $"[TableSprites] Failed to save {CuePrefabPath}");
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    // ---------------------------------------------------------------- Scene

    /// <summary>Margin around the table when framing the camera (same as PixelCameraRig.margin).</summary>
    const float CameraMargin = 0.5f;

    /// <summary>
    /// Puts one instance of each prefab in Pool.unity (if not there yet), poses the cue behind the cue ball
    /// spawn, frames the orthographic camera on the table with a margin and saves the scene.
    /// At runtime PixelCameraRig still re-frames the camera for the actual screen and the right panel.
    /// </summary>
    [MenuItem("Tools/Roguelike Pool/7. Place table and cue in the Pool scene")]
    public static void PlaceInScene()
    {
        const string scenePath = "Assets/Scenes/Pool.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath) scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        var table = Object.FindAnyObjectByType<PoolTable>(FindObjectsInactive.Include);
        if (table == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<PoolTable>(TablePrefabPath);
            table = (PoolTable)PrefabUtility.InstantiatePrefab(prefab, scene);
            table.transform.position = Vector3.zero;
        }

        var cue = Object.FindAnyObjectByType<CueStick>(FindObjectsInactive.Include);
        if (cue == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<CueStick>(CuePrefabPath);
            cue = (CueStick)PrefabUtility.InstantiatePrefab(prefab, scene);
            cue.name = "CueRoot";
        }
        cue.Pose(table.HeadSpot, Vector2.up, CueRestDistance); // edit-mode preview: aiming at the rack
        PrefabUtility.RecordPrefabInstancePropertyModifications(cue.transform);
        PrefabUtility.RecordPrefabInstancePropertyModifications(cue.stick);

        var cam = Camera.main;
        if (cam != null)
        {
            Rect b = table.OuterBounds;
            Vector2 center = table.transform.TransformPoint(b.center);
            const float referenceAspect = 540f / 960f; // portrait reference resolution
            cam.orthographic = true;
            cam.orthographicSize = Mathf.Max(b.height / 2f + CameraMargin, (b.width / 2f + CameraMargin) / referenceAspect);
            cam.transform.position = new Vector3(center.x, center.y, -10f);
            EditorUtility.SetDirty(cam);
            EditorUtility.SetDirty(cam.transform);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log($"[TableSprites] Pool.unity: {table.name} and {cue.name} placed, camera framed.");
    }

    // ---------------------------------------------------------------- Pocket detection

    /// <summary>Rough pocket centers in the PNG (px, top-left origin): only seeds for the detection.</summary>
    static readonly (string name, Vector2 seed)[] PocketGuesses =
    {
        ("Pocket_TL", new Vector2(166f, 141f)), ("Pocket_TR", new Vector2(1040f, 141f)),
        ("Pocket_ML", new Vector2(146f, 881f)), ("Pocket_MR", new Vector2(1060f, 881f)),
        ("Pocket_BL", new Vector2(166f, 1595f)), ("Pocket_BR", new Vector2(1040f, 1595f)),
    };

    /// <summary>Cue capture fraction for the side pockets (same as the old PoolTable.sideCueCaptureScale).</summary>
    const float SideCueCaptureScale = 0.7f;

    /// <summary>
    /// Moves only the pocket children of the existing prefab to the detected positions/radii (cushions and
    /// any other hand edits are kept). Pockets moved by hand are overwritten.
    /// </summary>
    [MenuItem("Tools/Roguelike Pool/3. Detect pockets in the PNG and place them in the prefab")]
    public static void DetectAndPlacePockets()
    {
        var root = PrefabUtility.LoadPrefabContents(TablePrefabPath);
        try
        {
            var pockets = root.transform.Find("Pockets");
            if (pockets == null) { Debug.LogError("[TableSprites] The prefab has no Pockets child: run step 2."); return; }
            PlacePockets(pockets);
            PrefabUtility.SaveAsPrefabAsset(root, TablePrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PlacePockets(Transform pocketsParent)
    {
        var image = LoadPixels(TablePng, out int w, out int h);
        var log = new System.Text.StringBuilder("[TableSprites] Pockets detected (px: center, radius → local units):\n");
        foreach (var (name, seed) in PocketGuesses)
        {
            var child = pocketsParent.Find(name);
            if (child == null) { Debug.LogError($"[TableSprites] Missing {name}"); continue; }
            if (!DetectDisc(image, w, h, seed, out Vector2 center, out float radiusPx))
            {
                Debug.LogError($"[TableSprites] {name}: no dark disc found near {seed}");
                continue;
            }
            Vector2 local = Px(center.x, center.y);
            float radius = radiusPx / PixelsPerUnit;
            child.localPosition = local;
            child.GetComponent<CircleCollider2D>().radius = radius;
            var pocket = child.GetComponent<Pocket>();
            pocket.radius = radius;
            if (name.EndsWith("ML") || name.EndsWith("MR")) pocket.cueCaptureScale = SideCueCaptureScale;
            log.AppendLine($"  {name}: ({center.x:0.0}, {center.y:0.0}) r {radiusPx:0.0} px → ({local.x:0.000}, {local.y:0.000}) r {radius:0.000}");
        }
        Debug.Log(log.ToString());
    }

    /// <summary>Reads the PNG straight from disk (the imported texture is not readable). Row 0 = top.</summary>
    static Color32[] LoadPixels(string path, out int w, out int h)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(System.IO.File.ReadAllBytes(path));
        w = tex.width; h = tex.height;
        var bottomUp = tex.GetPixels32();
        Object.DestroyImmediate(tex);
        var topDown = new Color32[bottomUp.Length];
        for (int y = 0; y < h; y++) System.Array.Copy(bottomUp, (h - 1 - y) * w, topDown, y * w, w);
        return topDown;
    }

    /// <summary>Hole interior: opaque and very dark (felt starts at G ≈ 60, the brass rim at R ≈ 60).</summary>
    static bool IsHole(Color32 c) => c.a > 128 && c.g < 25 && c.r < 55 && c.b < 35;

    /// <summary>
    /// Casts 360 rays from the seed through the dark hole pixels; each ray stops at the first non-hole pixel.
    /// A least-squares circle is fitted to the ray ends, rays far from the median radius are rejected (thin
    /// dark outlines along the cushions can leak), and the fit is repeated from the new center.
    /// </summary>
    static bool DetectDisc(Color32[] img, int w, int h, Vector2 seed, out Vector2 center, out float radius)
    {
        center = seed; radius = 0f;
        const int rays = 360;
        var ends = new List<Vector2>(rays);
        var dists = new List<float>(rays);
        for (int pass = 0; pass < 4; pass++)
        {
            ends.Clear(); dists.Clear();
            for (int i = 0; i < rays; i++)
            {
                float a = i * Mathf.PI * 2f / rays;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                float d = 0f;
                while (d < 200f)
                {
                    var p = center + dir * (d + 0.5f);
                    int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y);
                    if (x < 0 || y < 0 || x >= w || y >= h || !IsHole(img[y * w + x])) break;
                    d += 0.5f;
                }
                ends.Add(center + dir * d);
                dists.Add(d);
            }
            var sorted = new List<float>(dists);
            sorted.Sort();
            float median = sorted[rays / 2];
            if (median < 10f) return false;

            // Kasa circle fit on the inlier ray ends.
            double sx = 0, sy = 0, sxx = 0, syy = 0, sxy = 0, sxz = 0, syz = 0, sz = 0; int n = 0;
            for (int i = 0; i < rays; i++)
            {
                if (Mathf.Abs(dists[i] - median) > median * 0.12f) continue;
                double x = ends[i].x - center.x, y = ends[i].y - center.y, z = x * x + y * y;
                sx += x; sy += y; sxx += x * x; syy += y * y; sxy += x * y; sxz += x * z; syz += y * z; sz += z; n++;
            }
            if (n < rays / 3) return false;
            // Solve [sxx sxy sx; sxy syy sy; sx sy n] [A B C] = [-sxz -syz -sz]; center = (-A/2, -B/2).
            var m = new double[3, 4] { { sxx, sxy, sx, -sxz }, { sxy, syy, sy, -syz }, { sx, sy, n, -sz } };
            if (!Solve3(m, out double A, out double B, out double C)) return false;
            double cx = -A / 2, cy = -B / 2;
            radius = (float)System.Math.Sqrt(cx * cx + cy * cy - C);
            center += new Vector2((float)cx, (float)cy);
        }
        return radius > 10f;
    }

    static bool Solve3(double[,] m, out double a, out double b, out double c)
    {
        a = b = c = 0;
        for (int col = 0; col < 3; col++)
        {
            int pivot = col;
            for (int r = col + 1; r < 3; r++) if (System.Math.Abs(m[r, col]) > System.Math.Abs(m[pivot, col])) pivot = r;
            if (System.Math.Abs(m[pivot, col]) < 1e-9) return false;
            for (int k = 0; k < 4; k++) (m[col, k], m[pivot, k]) = (m[pivot, k], m[col, k]);
            for (int r = 0; r < 3; r++)
            {
                if (r == col) continue;
                double f = m[r, col] / m[col, col];
                for (int k = col; k < 4; k++) m[r, k] -= f * m[col, k];
            }
        }
        a = m[0, 3] / m[0, 0]; b = m[1, 3] / m[1, 1]; c = m[2, 3] / m[2, 2];
        return true;
    }

    // ---------------------------------------------------------------- Shapes

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
