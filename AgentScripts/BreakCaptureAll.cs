using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class BreakCapture
{
    // Fires a full-power break, waits until the first impact, then a few frames, and grabs the art.
    public static async Task<string> Run(int framesAfterImpact)
    {
        var ui = Object.FindAnyObjectByType<ShotUI>();
        var cue = Object.FindAnyObjectByType<CueController>();
        ui.Power = 1f;
        await Task.Delay(50);
        bool hit = false;
        System.Action<Ball, Pocket> _ = null;
        var apex = Ball.All.Find(b => b.Number == 1);
        cue.Shoot();
        float t0 = Time.time;
        while (!hit && Time.time - t0 < 3f)
        {
            await Task.Yield();
            if (apex.Body.linearVelocity.sqrMagnitude > 0.01f) hit = true;
        }
        for (int i = 0; i < framesAfterImpact; i++) await Task.Yield();
        return CaptureArt.Run(140, 560, 160, 140, 5, "Screenshots/zoom_break.png") + " hit=" + hit;
    }
}

public static class CaptureArt
{
    // Saves a nearest-neighbour zoom of a region of the low-res world render (art pixels).
    public static string Run(int x, int y, int w, int h, int zoom, string path)
    {
        var rt = RoguelikePool.PixelCameraRig.Instance.GetComponent<Camera>().targetTexture;
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var src = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        src.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        src.Apply();
        RenderTexture.active = prev;
        if (w <= 0) { w = rt.width; h = rt.height; x = 0; y = 0; }
        var dst = new Texture2D(w * zoom, h * zoom, TextureFormat.RGBA32, false);
        for (int j = 0; j < h * zoom; j++)
        for (int i = 0; i < w * zoom; i++)
            dst.SetPixel(i, j, src.GetPixel(Mathf.Clamp(x + i / zoom, 0, rt.width - 1), Mathf.Clamp(y + j / zoom, 0, rt.height - 1)));
        File.WriteAllBytes(path, dst.EncodeToPNG());
        return $"rt={rt.width}x{rt.height} saved {path}";
    }
}
