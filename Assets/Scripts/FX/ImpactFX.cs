using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Ball-to-ball impact effects: a burst of 1-2 px HDR sparks (they bloom) thrown mostly along the
    /// contact tangent, plus a tiny cross-shaped flash for a couple of frames. Strength scales with
    /// the impact speed. Purely visual.
    /// </summary>
    public class ImpactFX : MonoBehaviour
    {
        public static ImpactFX Instance { get; private set; }

        [Tooltip("Vitesse relative (u/s) en dessous de laquelle aucun effet n'est joué.")]
        public float minSpeed = 0.6f;

        ParticleSystem sparks;
        Sprite flashSprite;
        Material flashMaterial;
        readonly List<(SpriteRenderer renderer, float age)> flashes = new List<(SpriteRenderer, float)>();
        readonly Stack<SpriteRenderer> flashPool = new Stack<SpriteRenderer>();

        const float FlashDuration = 0.09f;

        void Awake()
        {
            Instance = this;
            sparks = CreateSparks();
            flashSprite = CreateFlashSprite();
            flashMaterial = PixelArt.GlowMaterial(new Color(2.6f, 2.3f, 1.8f));
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Emit(Vector2 point, Vector2 normal, float speed)
        {
            if (speed < minSpeed) return;
            float strength = Mathf.InverseLerp(minSpeed, 25f, speed);
            int count = Mathf.RoundToInt(Mathf.Lerp(3f, 22f, strength));
            Vector2 tangent = new Vector2(-normal.y, normal.x);

            var p = new ParticleSystem.EmitParams { applyShapeToPosition = false };
            for (int i = 0; i < count; i++)
            {
                // Mostly along the tangent (both ways), some spray along the normal.
                Vector2 dir = tangent * (Random.value < 0.5f ? -1f : 1f) + normal * Random.Range(-0.7f, 0.7f);
                dir = dir.normalized;
                p.position = point + Random.insideUnitCircle * PixelArt.Pixel;
                p.velocity = dir * Random.Range(1.5f, 3.5f + 6f * strength);
                p.startLifetime = Random.Range(0.12f, 0.25f + 0.25f * strength);
                p.startSize = Random.value < 0.3f ? PixelArt.Pixel * 2f : PixelArt.Pixel * 1.3f;
                p.startColor = Color.Lerp(new Color(1f, 0.95f, 0.8f), new Color(1f, 0.55f, 0.2f), Random.value);
                sparks.Emit(p, 1);
            }

            if (strength > 0.15f) SpawnFlash(point); // only clear, hard hits flash
        }

        void Update()
        {
            for (int i = flashes.Count - 1; i >= 0; i--)
            {
                var (renderer, age) = flashes[i];
                age += Time.deltaTime;
                if (age >= FlashDuration)
                {
                    renderer.gameObject.SetActive(false);
                    flashPool.Push(renderer);
                    flashes.RemoveAt(i);
                    continue;
                }
                renderer.color = new Color(1f, 1f, 1f, 1f - age / FlashDuration);
                flashes[i] = (renderer, age);
            }
        }

        void SpawnFlash(Vector2 point)
        {
            SpriteRenderer r;
            if (flashPool.Count > 0) r = flashPool.Pop();
            else
            {
                var go = new GameObject("ImpactFlash");
                go.transform.SetParent(transform, false);
                r = go.AddComponent<SpriteRenderer>();
                r.sprite = flashSprite;
                r.sharedMaterial = flashMaterial;
                r.sortingOrder = 31;
            }
            // Odd-sized sprite: center on a pixel center so the cross stays symmetric.
            r.transform.position = new Vector2(Mathf.Floor(point.x * PixelArt.PPU) + 0.5f, Mathf.Floor(point.y * PixelArt.PPU) + 0.5f) / PixelArt.PPU;
            r.color = Color.white;
            r.gameObject.SetActive(true);
            flashes.Add((r, 0f));
        }

        ParticleSystem CreateSparks()
        {
            var go = new GameObject("Sparks");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            main.startSpeed = 0f;
            main.gravityModifier = 0f;

            var emission = ps.emission;
            emission.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;

            var limit = ps.limitVelocityOverLifetime;
            limit.enabled = true;
            limit.limit = 100f;
            limit.drag = 7f;
            limit.multiplyDragByParticleSize = false;
            limit.multiplyDragByParticleVelocity = false;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.6f), new GradientColorKey(new Color(0.8f, 0.2f, 0.1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.55f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            var mat = PixelArt.GlowMaterial(new Color(2.4f, 2.1f, 1.7f));
            mat.mainTexture = PixelArt.White;
            renderer.sharedMaterial = mat;
            renderer.sortingOrder = 30;
            ps.Play();
            return ps;
        }

        static Sprite CreateFlashSprite()
        {
            const int S = 5;
            var px = new Color32[S * S];
            var white = new Color32(255, 250, 235, 255);
            var warm = new Color32(255, 200, 120, 200);
            for (int i = 0; i < S; i++)
            {
                px[2 * S + i] = i == 0 || i == 4 ? warm : white;
                px[i * S + 2] = i == 0 || i == 4 ? warm : white;
            }
            return PixelArt.ToSprite(PixelArt.NewTexture(S, S, "ImpactFlash"), px);
        }
    }
}
