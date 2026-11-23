using System.Text;
using UnityEditor;
using UnityEngine;

public static class BallPrefabCheck
{
    public static string Run()
    {
        var sb = new StringBuilder();
        foreach (var path in new[] { "Assets/Prefabs/Balls/Ball.prefab", "Assets/Prefabs/Balls/CueBall.prefab" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var sr = go.transform.Find("Visual").GetComponent<SpriteRenderer>();
            var col = go.GetComponent<CircleCollider2D>();
            var sprite = sr.sprite;
            // Visible diameter in world = visible px / PPU × visual lossy scale.
            float visiblePx = sprite.name == "ball_cue" ? 244 : 242;
            float visibleWorld = visiblePx / sprite.pixelsPerUnit * sr.transform.lossyScale.x;
            float colliderWorld = col.radius * 2f * go.transform.lossyScale.x;
            sb.AppendLine($"{path}: variant={PrefabUtility.GetPrefabAssetType(prefab)} sprite={sprite.name} ppu={sprite.pixelsPerUnit:0.###} " +
                          $"spriteWorld={sr.bounds.size.x:0.####} visibleWorld={visibleWorld:0.#####} colliderWorld={colliderWorld:0.#####} " +
                          $"layer={sr.sortingLayerName}/{sr.sortingOrder} comps={string.Join(",", System.Array.ConvertAll(go.GetComponents<Component>(), c => c.GetType().Name))}");
            Object.DestroyImmediate(go);
        }
        return sb.ToString();
    }
}
