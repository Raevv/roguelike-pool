using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using RoguelikePool;
public static class CueDragSim {
  // where: 0 = start on the cue, 1 = start on the table center, 2 = start above the screen.
  // Drags 300 px (at 1920) toward the cue butt, in 15 steps. Reports power and aim change.
  public static async Task<string> Run(int where) {
    var mouse = Mouse.current;
    var cc = Object.FindAnyObjectByType<CueController>();
    var ui = Object.FindAnyObjectByType<ShotUI>();
    var cue = Object.FindAnyObjectByType<CueStick>();
    var rig = PixelCameraRig.Instance;
    ui.Power = 0.2f;
    Vector2 dir = cc.AimDirection;
    Vector2 start = where == 0 ? rig.WorldToScreen((Vector2)cue.stick.position - dir * 3f)
                  : where == 1 ? rig.WorldToScreen(new Vector2(3f, 2f))
                  : new Vector2(Screen.width / 2f, Screen.height + 30f);
    Vector2 back = -dir; float k = Screen.height / 1920f;
    float p0 = ui.Power; Vector2 a0 = cc.AimDirection;
    async Task Send(Vector2 pos, bool down) {
      InputSystem.QueueStateEvent(mouse, new MouseState { position = pos, buttons = (ushort)(down ? 1 : 0) });
      await Task.Delay(30);
    }
    await Send(start, false);
    await Send(start, true);
    for (int i = 1; i <= 15; i++) await Send(start + back * (20f * i * k) + new Vector2(0, 0), true);
    await Send(start + back * 300f * k, false);
    await Task.Delay(100);
    return $"where={where} start={start} power {p0:F2}->{ui.Power:F2} aimChange={Vector2.Angle(a0, cc.AimDirection):F2}deg";
  }
}
