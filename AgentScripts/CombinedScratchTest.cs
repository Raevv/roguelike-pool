using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class CombinedScratchTest
{
    // Snapshot, pocket ball 5 then the cue ball (as during one shot), and check that both come back.
    public static async Task<string> Run()
    {
        var rules = Object.FindAnyObjectByType<TableRules>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var five = Ball.All.First(b => b.Number == 5);
        var cue = Ball.All.First(b => b.IsCue);
        Vector2 fivePos = five.Body.position, cuePos = cue.Body.position;
        rules.TakeSnapshot();
        five.FallInto(table.Pockets[0]);
        await Task.Delay(400);
        bool fiveGone = !five.gameObject.activeInHierarchy;
        cue.FallInto(table.Pockets[1]);
        await Task.Delay(1600);
        return $"fiveGoneAfterFall={fiveGone} fiveBack={five.gameObject.activeInHierarchy} fiveAt={five.Body.position} (was {fivePos}) cueBack={cue.gameObject.activeInHierarchy} cueAt={cue.Body.position} (was {cuePos}) onTable={Ball.All.Count} busy={rules.Busy}";
    }
}
