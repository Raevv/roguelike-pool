using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoguelikePool.EditorTools
{
    /// <summary>
    /// Builds the ball assets from the user's sprites (Assets/Art/Sprites/Balls/ball_1..10, ball_cue):
    /// 1 import the sprites with a PPU that makes the visible ball (measured from the alpha) exactly as wide
    /// as the physics collider, 2 build the Ball prefab and its CueBall variant.
    /// Physics (collider, mass, Rigidbody2D) is never set here: <see cref="Ball"/> still configures it in Awake.
    /// </summary>
    public static class BallSpriteBuilder
    {
        public const string SpriteFolder = "Assets/Art/Sprites/Balls";
        public const string PrefabFolder = "Assets/Prefabs/Balls";
        public const string BallPrefabPath = PrefabFolder + "/Ball.prefab";
        public const string CueBallPrefabPath = PrefabFolder + "/CueBall.prefab";

        /// <summary>Alpha threshold used to find the visible edge of the ball in the PNG.</summary>
        const byte AlphaThreshold = 128;

        public static float Diameter => BallFactory.DefaultRadius * 2f;

        public static string SpritePath(int number) =>
            $"{SpriteFolder}/{(number == 0 ? "ball_cue" : "ball_" + number)}.png";

        [MenuItem("Tools/Roguelike Pool/Balls/1 Import ball sprites")]
        public static string ImportSprites()
        {
            var log = new System.Text.StringBuilder();
            for (int n = 0; n <= 10; n++)
            {
                string path = SpritePath(n);
                if (!File.Exists(path)) { log.AppendLine("MANQUANT " + path); continue; }

                int visible = MeasureVisibleDiameter(path, out int w, out int h);
                float ppu = visible / Diameter;

                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Center;
                settings.spritePivot = new Vector2(0.5f, 0.5f);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spritePixelsPerUnit = ppu;
                importer.SetTextureSettings(settings);
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();

                log.AppendLine($"{Path.GetFileName(path)} {w}x{h} visible {visible} px → PPU {ppu:0.###}");
            }
            Debug.Log("[BallSprites] " + log);
            return log.ToString();
        }

        /// <summary>Visible ball diameter in pixels: the larger side of the bounding box of pixels with alpha ≥ 128.</summary>
        public static int MeasureVisibleDiameter(string path, out int width, out int height)
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            width = tex.width; height = tex.height;
            var px = tex.GetPixels32();
            int minX = width, maxX = -1, minY = height, maxY = -1;
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (px[y * width + x].a < AlphaThreshold) continue;
                if (x < minX) minX = x; if (x > maxX) maxX = x;
                if (y < minY) minY = y; if (y > maxY) maxY = y;
            }
            Object.DestroyImmediate(tex);
            // Height is used: two sprites (6, 7) have a 1 px antialiasing smear on their sides.
            return maxY - minY + 1;
        }

        public const string DataFolder = "Assets/Data/Balls";

        public static string DataPath(int number) => $"{DataFolder}/Ball_{number}.asset";

        /// <summary>One BallData per colored ball (1-7 and 8 solid, 9-10 stripes). Existing assets are left untouched.</summary>
        [MenuItem("Tools/Roguelike Pool/Balls/3 Create BallData assets")]
        public static string CreateBallData()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
                AssetDatabase.CreateFolder("Assets/Data", "Balls");
            var log = new System.Text.StringBuilder();
            for (int n = 1; n <= 10; n++)
            {
                string path = DataPath(n);
                if (AssetDatabase.LoadAssetAtPath<BallData>(path) != null) { log.AppendLine("existe déjà " + path); continue; }
                var data = ScriptableObject.CreateInstance<BallData>();
                data.number = n;
                data.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(n));
                data.type = BallFactory.IsStripe(n) ? BallType.Stripe : BallType.Solid;
                AssetDatabase.CreateAsset(data, path);
                log.AppendLine($"créé {path} : {n}, {data.type}, sprite {(data.sprite != null ? data.sprite.name : "MANQUANT")}");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[BallSprites] " + log);
            return log.ToString();
        }

        public const string RackSetupPath = "Assets/Data/RackSetup.asset";

        /// <summary>Default triangle: 1 at the apex, 8 in the center, a solid (5) and a stripe (10) in the back corners.</summary>
        static readonly int[] DefaultRack = { 1, 2, 9, 3, 8, 4, 5, 6, 7, 10 };

        /// <summary>Creates the RackSetup asset if missing, then assigns the prefabs and the RackSetup to GameBootstrap and saves the scene.</summary>
        [MenuItem("Tools/Roguelike Pool/Balls/4 RackSetup + assign to GameBootstrap")]
        public static string AssignToBootstrap()
        {
            var log = new System.Text.StringBuilder();
            var setup = AssetDatabase.LoadAssetAtPath<RackSetup>(RackSetupPath);
            if (setup == null)
            {
                setup = ScriptableObject.CreateInstance<RackSetup>();
                setup.spots.Clear();
                foreach (int n in DefaultRack) setup.spots.Add(AssetDatabase.LoadAssetAtPath<BallData>(DataPath(n)));
                AssetDatabase.CreateAsset(setup, RackSetupPath);
                AssetDatabase.SaveAssets();
                log.AppendLine("créé " + RackSetupPath);
            }

            var boot = Object.FindAnyObjectByType<GameBootstrap>();
            if (boot == null) return log + "Aucun GameBootstrap dans la scène ouverte.";
            var so = new SerializedObject(boot);
            so.FindProperty("ballPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(BallPrefabPath);
            so.FindProperty("cueBallPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(CueBallPrefabPath);
            so.FindProperty("rackSetup").objectReferenceValue = setup;
            so.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(boot.gameObject.scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(boot.gameObject.scene);
            log.AppendLine("GameBootstrap : prefabs et RackSetup assignés, scène sauvegardée.");
            Debug.Log("[BallSprites] " + log);
            return log.ToString();
        }

        [MenuItem("Tools/Roguelike Pool/Balls/2 Build Ball + CueBall prefabs")]
        public static string BuildPrefabs()
        {
            if (!AssetDatabase.IsValidFolder(PrefabFolder))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Balls");

            var sprite1 = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(1));
            var spriteCue = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(0));
            if (sprite1 == null || spriteCue == null)
                return "Sprites introuvables : lancer d'abord 1 Import ball sprites.";

            // Root: same components and scale as the balls BallFactory built in code
            // (scale = diameter, collider radius 0.5; Ball.Awake sets the Rigidbody2D and the material).
            var root = new GameObject("Ball");
            root.transform.localScale = Vector3.one * Diameter;
            root.AddComponent<Rigidbody2D>().gravityScale = 0f;
            root.AddComponent<CircleCollider2D>().radius = 0.5f;
            root.AddComponent<Ball>();
            root.AddComponent<BallImpactFX>();

            // Visual: the sprite at its own PPU, so its scale cancels the root scale (world size = sprite size).
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localScale = Vector3.one / Diameter;
            var sr = visual.AddComponent<SpriteRenderer>();
            sr.sprite = sprite1;
            sr.sortingLayerName = "Default"; // above the Table sorting layer; the cue is drawn by the overlay camera, above everything
            sr.sortingOrder = 10;

            var basePrefab = PrefabUtility.SaveAsPrefabAsset(root, BallPrefabPath);
            Object.DestroyImmediate(root);

            // Variant for the cue ball: only the sprite changes.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            instance.name = "CueBall";
            instance.transform.Find("Visual").GetComponent<SpriteRenderer>().sprite = spriteCue;
            PrefabUtility.SaveAsPrefabAsset(instance, CueBallPrefabPath);
            Object.DestroyImmediate(instance);

            AssetDatabase.SaveAssets();
            string msg = $"Prefabs créés : {BallPrefabPath}, {CueBallPrefabPath} (variante).";
            Debug.Log("[BallSprites] " + msg);
            return msg;
        }
    }
}
