using UnityEngine;
public static class SpriteEdge {
  // Rightmost opaque (alpha > 0.5) column of pool_table.png on a few rows, in pixels and in world units from the pivot.
  public static string Run() {
    var t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes("Assets/Art/Sprites/pool_table.png"));
    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Sprites/pool_table.png");
    var table = Object.FindAnyObjectByType<RoguelikePool.PoolTable>();
    var sb = new System.Text.StringBuilder($"{t.width}x{t.height} ppu={sprite.pixelsPerUnit} pivotPx={sprite.pivot} boundsXMax={table.OuterBounds.xMax:F3} | ");
    foreach (int y in new[] { 300, 600, 900, 1200, 1500 }) {
      int right = -1, left = -1;
      for (int x = t.width - 1; x >= 0; x--) if (t.GetPixel(x, y).a > 0.5f) { right = x; break; }
      for (int x = 0; x < t.width; x++) if (t.GetPixel(x, y).a > 0.5f) { left = x; break; }
      sb.Append($"row{y}: left {left} right {right} → rightWorld {(right + 1 - sprite.pivot.x) / sprite.pixelsPerUnit:F3} | ");
    }
    return sb.ToString();
  }
}
