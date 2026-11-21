using UnityEngine;
public static class EdgeScan {
  // For each given row (y from the top), the x of the rightmost pixel brighter than the background, scanning from xFrom leftwards.
  public static string Run(string path, int xFrom, string rowsCsv) {
    var t = new Texture2D(2, 2); t.LoadImage(System.IO.File.ReadAllBytes(path));
    var sb = new System.Text.StringBuilder($"{t.width}x{t.height} ");
    foreach (var s in rowsCsv.Split(',')) {
      int y = int.Parse(s); int found = -1;
      for (int x = xFrom; x >= 0; x--) { var c = t.GetPixel(x, t.height - 1 - y); if (c.r + c.g + c.b > 0.45f) { found = x; break; } }
      sb.Append($"y{y}:x{found} ");
    }
    return sb.ToString();
  }
}
