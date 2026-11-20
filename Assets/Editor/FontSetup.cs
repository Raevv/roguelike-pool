using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Creates the TextMeshPro font asset of the game font (Assets/Art/Font/VeniceClassic.ttf), makes it
/// TextMeshPro's default font and applies it to every text of the open scene.
/// VeniceClassic is a pixel font drawn for 19 pt (capitals 11 px). The SDF atlas is sampled at 76 pt
/// (4 texels per font pixel) so the pixel edges stay sharp; use font sizes that are multiples of 19
/// (19, 38, 57…) to keep one font pixel = a whole number of art pixels.
/// The asset is dynamic: any missing character is added to the atlas on demand.
/// Its material carries a 1-pixel drop shadow (Underlay), editable on the material in the Inspector.
/// </summary>
public static class FontSetup
{
    public const string FontPath = "Assets/Art/Font/VeniceClassic.ttf";
    public const string AssetPath = "Assets/Art/Font/VeniceClassic SDF.asset";
    const int SamplingSize = 76, Padding = 8, AtlasSize = 512;
    const string Preload = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
                           "ÀÂÄÇÉÈÊËÎÏÔÖÙÛÜŸàâäçéèêëîïôöùûüÿœŒ«»’…";

    [MenuItem("Tools/Roguelike Pool/9. VeniceClassic as TextMeshPro default font (and apply to the scene)")]
    static void Menu() => Debug.Log(Run());

    public static string Run()
    {
        var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
        if (fontAsset == null) fontAsset = Create();
        if (fontAsset == null) return "FontSetup: could not create the font asset.";

        // TextMeshPro default font, with the LiberationSans default kept as a fallback for exotic characters.
        var settings = TMP_Settings.instance;
        var liberation = TMP_Settings.defaultFontAsset != fontAsset ? TMP_Settings.defaultFontAsset : null;
        TMP_Settings.defaultFontAsset = fontAsset;
        if (liberation != null && !TMP_Settings.fallbackFontAssets.Contains(liberation))
            TMP_Settings.fallbackFontAssets.Add(liberation);
        EditorUtility.SetDirty(settings);

        // Every text of the open scene (active or not).
        int count = 0;
        foreach (var text in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include))
        {
            if (text.font == fontAsset && text.fontSharedMaterial == fontAsset.material) continue;
            Undo.RecordObject(text, "VeniceClassic font");
            text.font = fontAsset;
            text.fontSharedMaterial = fontAsset.material;
            EditorUtility.SetDirty(text);
            count++;
        }
        var scene = EditorSceneManager.GetActiveScene();
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        AssetDatabase.SaveAssets();
        return $"FontSetup: {AssetPath} is the TMP default font, applied to {count} text(s) in {scene.path}.";
    }

    static TMP_FontAsset Create()
    {
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        if (font == null) { Debug.LogError("FontSetup: " + FontPath + " not found."); return null; }

        var fontAsset = TMP_FontAsset.CreateFontAsset(font, SamplingSize, Padding, GlyphRenderMode.SDFAA,
                                                      AtlasSize, AtlasSize, AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null) return null;
        fontAsset.name = "VeniceClassic SDF";
        AssetDatabase.CreateAsset(fontAsset, AssetPath);

        var atlas = fontAsset.atlasTextures[0];
        atlas.name = "VeniceClassic SDF Atlas";
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        var material = fontAsset.material;
        material.name = "VeniceClassic SDF Material";
        // Drop shadow one font pixel down-right, like the old bitmap texts.
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color32(0x1a, 0x0d, 0x14, 0xff));
        material.SetFloat("_UnderlayOffsetX", 0.5f);
        material.SetFloat("_UnderlayOffsetY", -0.5f);
        material.SetFloat("_UnderlayDilate", 0f);
        material.SetFloat("_UnderlaySoftness", 0f);
        AssetDatabase.AddObjectToAsset(material, fontAsset);

        fontAsset.TryAddCharacters(Preload, out string missing);
        if (!string.IsNullOrEmpty(missing)) Debug.LogWarning("FontSetup: characters missing from VeniceClassic: " + missing);

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(AssetPath);
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
    }
}
