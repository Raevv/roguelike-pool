using System.Threading.Tasks;
using UnityEngine;
using RoguelikePool;

public static class ShotTest
{
    // Aims at angleDeg with the given power, shoots, and samples the cue ball until it stops.
    public static async Task<string> Run(float angleDeg, float power)
    {
        var cue = Object.FindAnyObjectByType<CueController>();
        var ui = Object.FindAnyObjectByType<ShotUI>();
        var ball = Ball.All[0];
        var f = typeof(CueController).GetField("aimAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        f.SetValue(cue, angleDeg * Mathf.Deg2Rad);
        typeof(CueController).GetField("targetAngle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(cue, angleDeg * Mathf.Deg2Rad);
        ui.Power = power;
        await Task.Delay(100);
        var bounce = GameObject.Find("BounceLine").GetComponent<LineRenderer>();
        Vector3 bA = bounce.GetPosition(0), bB = bounce.GetPosition(1);
        Vector2 predicted = ((Vector2)(bB - bA)).normalized;
        Vector2 start = ball.Body.position;
        cue.Shoot();
        float t0 = Time.time; Vector2 prevV = Vector2.zero; Vector2 postBounce = Vector2.zero; float maxSpeed = 0; bool buttonDisabledSeen = false;
        var button = GameObject.Find("ShootButton").GetComponent<UnityEngine.UI.Button>();
        while (Time.time - t0 < 30f)
        {
            await Task.Yield();
            var v = ball.Body.linearVelocity;
            maxSpeed = Mathf.Max(maxSpeed, v.magnitude);
            if (ball.IsMoving && !button.interactable) buttonDisabledSeen = true;
            if (postBounce == Vector2.zero && prevV != Vector2.zero && Vector2.Dot(v.normalized, prevV.normalized) < 0.99f) postBounce = v.normalized;
            if (v != Vector2.zero) prevV = v;
            if (Time.time - t0 > 0.3f && Ball.AllStopped) break;
        }
        return $"start={start} end={ball.Body.position} time={Time.time - t0:F2}s maxSpeed={maxSpeed:F1} predictedBounceDir={predicted} actualBounceDir={postBounce} angleErr={Vector2.Angle(predicted, postBounce):F2}deg buttonDisabledWhileMoving={buttonDisabledSeen} buttonNow={button.interactable}";
    }
}
