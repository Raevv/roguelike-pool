using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using RoguelikePool;

public static class ScreensTest
{
    // Brings up an end-of-stage screen, or clicks its button.
    //   "win"   : RestartRun, then pocket balls (pointsPerBall 500, runtime only) until the stage is won.
    //   "lose"  : RestartRun, then use every shot without pocketing.
    //   "runwon": RestartRun, then win every stage, clicking STAGE SUIVANT between them.
    //   "click" : click the screen's button, report the new state.
    // Reports the state, whether the screen is shown, its texts, and whether the table center is blocked for aiming.
    public static async Task<string> Run(string mode)
    {
        var sm = Object.FindAnyObjectByType<StageManager>();
        var rules = Object.FindAnyObjectByType<TableRules>();
        var table = Object.FindAnyObjectByType<PoolTable>();
        var screens = Object.FindAnyObjectByType<StageScreens>();
        var ui = Object.FindAnyObjectByType<ShotUI>();
        sm.pointsPerBall = 500;

        async Task Shot(bool pot)
        {
            rules.TakeSnapshot();
            if (pot) Ball.All.First(b => !b.IsCue && !b.IsPocketed).FallInto(table.Pockets[0]);
            await Task.Delay(500);
        }
        Button ScreenButton() => Object.FindObjectsByType<Button>(FindObjectsInactive.Include).First(b => b.name == "Button");

        if (mode == "win") { sm.RestartRun(); await Shot(true); }
        else if (mode == "lose") { sm.RestartRun(); while (sm.CurrentState == StageManager.State.Playing) await Shot(false); }
        else if (mode == "runwon")
        {
            sm.RestartRun();
            while (true)
            {
                await Shot(true);
                if (sm.CurrentState != StageManager.State.StageWon) break;
                ScreenButton().onClick.Invoke();
                await Task.Delay(100);
            }
        }
        else if (mode == "click") { ScreenButton().onClick.Invoke(); await Task.Delay(200); }

        await Task.Delay(300);
        Vector2 center = new Vector2(Screen.width * 0.4f, Screen.height * 0.5f);
        string texts = string.Join(" | ", Object.FindObjectsByType<RawImage>(FindObjectsInactive.Exclude)
            .Where(r => r.transform.parent != null && r.transform.parent.name == "Panel" && r.texture != null)
            .Select(r => r.name + "=" + r.texture.name));
        return $"{mode}: state={sm.CurrentState} st{sm.StageNumber} {sm.Score}/{sm.Target} shots {sm.ShotsLeft}/{sm.MaxShots} " +
               $"shown={screens.IsShown} locked={rules.Locked} centerBlocked={ui.IsOverUI(center)} [{texts}]";
    }
}
