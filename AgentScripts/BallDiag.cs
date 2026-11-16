using System.Linq;
using UnityEngine;
using RoguelikePool;

public static class BallDiag
{
    public static string Run()
    {
        var b = Ball.All.First(x => x.IsCue);
        var sr = b.GetComponent<SpriteRenderer>();
        var sh = b.transform.Find("Shadow").GetComponent<SpriteRenderer>();
        string Info(SpriteRenderer r) => $"enabled={r.enabled} visible={r.isVisible} sprite={(r.sprite ? r.sprite.name + " tex=" + (r.sprite.texture ? r.sprite.texture.width + "x" + r.sprite.texture.height : "NULL") + " ppu=" + r.sprite.pixelsPerUnit + " rect=" + r.sprite.rect : "NULL")} color={r.color} order={r.sortingOrder} layer={r.sortingLayerName} mat={(r.sharedMaterial ? r.sharedMaterial.name + "/" + r.sharedMaterial.shader.name : "NULL")} bounds={r.bounds}";
        return "BALL " + Info(sr) + "\nSHADOW " + Info(sh) + $"\npos={b.transform.position} scale={b.transform.lossyScale} z={b.transform.position.z}";
    }
}
