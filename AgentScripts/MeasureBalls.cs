using System.IO;
using System.Text;
using UnityEngine;

public static class MeasureBalls
{
    public static string Run()
    {
        var sb = new StringBuilder();
        foreach (var path in Directory.GetFiles("Assets/Art/Sprites/Balls", "*.png"))
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(path));
            var px = tex.GetPixels32();
            int w = tex.width, h = tex.height;
            foreach (byte thr in new byte[] { 16, 128 })
            {
                int minX = w, maxX = -1, minY = h, maxY = -1;
                for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    if (px[y * w + x].a >= thr)
                    {
                        if (x < minX) minX = x; if (x > maxX) maxX = x;
                        if (y < minY) minY = y; if (y > maxY) maxY = y;
                    }
                float cx = (minX + maxX + 1) / 2f, cy = (minY + maxY + 1) / 2f;
                sb.AppendLine($"{Path.GetFileName(path)} {w}x{h} a>={thr}: x {minX}-{maxX} ({maxX - minX + 1}) y {minY}-{maxY} ({maxY - minY + 1}) center ({cx},{cy})");
            }
            Object.DestroyImmediate(tex);
        }
        return sb.ToString();
    }
}
