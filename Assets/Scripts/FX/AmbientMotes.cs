using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Slow, faintly glowing dust motes drifting in the lamp light above the table (1 px each).
    /// </summary>
    public static class AmbientMotes
    {
        public static ParticleSystem Create(PoolTable table)
        {
            var go = new GameObject("AmbientMotes");
            go.transform.position = table.transform.position;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 11f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(PixelArt.Pixel * 1.1f, PixelArt.Pixel * 1.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.6f, 0.9f), new Color(1f, 0.65f, 0.35f, 0.7f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            var emission = ps.emission;
            emission.rateOverTime = 7f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            Rect outer = table.OuterBounds;
            shape.scale = new Vector3(outer.width + 2f, outer.height + 2f, 0f);

            var velocity = ps.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.12f, 0.12f);
            velocity.y = new ParticleSystem.MinMaxCurve(0.02f, 0.18f);
            velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 0.25f;
            noise.scrollSpeed = 0.1f;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            var mat = PixelArt.GlowMaterial(new Color(1.5f, 1.35f, 1.1f));
            mat.mainTexture = PixelArt.White;
            renderer.sharedMaterial = mat;
            renderer.sortingOrder = 25;

            ps.Play();
            return ps;
        }
    }
}
