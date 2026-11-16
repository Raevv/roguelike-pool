using System.IO;
using UnityEngine;

public static class CropPng
{
    // Nearest-neighbour crop + zoom of a PNG (coordinates from the bottom-left).
    public static string Run(string src, int x, int y, int w, int h, int zoom, string dst)
    {
        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(src));
        var outTex = new Texture2D(w * zoom, h * zoom, TextureFormat.RGBA32, false);
        for (int j = 0; j < h * zoom; j++)
        for (int i = 0; i < w * zoom; i++)
            outTex.SetPixel(i, j, tex.GetPixel(x + i / zoom, y + j / zoom));
        File.WriteAllBytes(dst, outTex.EncodeToPNG());
        return $"src={tex.width}x{tex.height} saved {dst}";
    }
}
