using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

/// <summary>
/// Play Mode: turns one ball's root by 45° (transform only, for the test) and checks the Visual child stays upright
/// with rotateVisual off, and follows with it on. Restores everything afterwards.
/// </summary>
public static class RotateVisualTest
{
    public static async Task<string> Run()
    {
        var b = Ball.All.First(x => !x.IsCue);
        bool saved = b.rotateVisual;
        string r = $"{b.name} default rotateVisual={b.rotateVisual}; ";
        b.transform.rotation = Quaternion.Euler(0, 0, 45f);
        b.rotateVisual = false;
        await Task.Delay(100);
        r += $"off: root={b.transform.eulerAngles.z:0} visual={b.Visual.transform.eulerAngles.z:0}; ";
        b.rotateVisual = true;
        await Task.Delay(100);
        r += $"on: root={b.transform.eulerAngles.z:0} visual={b.Visual.transform.eulerAngles.z:0}; ";
        b.rotateVisual = saved;
        b.transform.rotation = Quaternion.identity;
        await Task.Delay(100);
        r += $"restored: root={b.transform.eulerAngles.z:0} visual={b.Visual.transform.eulerAngles.z:0}";
        return r;
    }
}
