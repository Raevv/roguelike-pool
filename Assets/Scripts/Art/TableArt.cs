using System.Collections.Generic;
using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Bakes the whole static table into one pixel-art texture at <see cref="PixelArt.PPU"/>:
    /// wood rails with grain and a lit bevel, felt with cloth noise, an overhead lamp pool and
    /// painted ambient occlusion along the cushions and around the pockets, beveled cushions,
    /// brass-ringed pockets. Also builds the dungeon-stone floor, the lamp spill on it, the
    /// table's drop shadow and the glowing diamond studs.
    /// Gradients are smooth interpolations along small palettes (no dithering): crisp pixel shapes, clean light.
    /// </summary>
    public static class TableArt
    {
        // Palettes (sRGB), dark → light. Cool, desaturated shadows; warm highlights.
        static readonly Color32[] Felt = PixelArt.Palette("06191c", "0a2626", "0d3431", "12443b", "175446", "1d6650", "267a5b", "36935f");
        static readonly Color32[] Cushion = PixelArt.Palette("061d1e", "0a2c2a", "0f3d36", "165043", "1f6650", "2e8259", "4aa065");
        static readonly Color32[] Wood = PixelArt.Palette("12080e", "1f0d14", "321419", "4a1e1e", "662a23", "86392a", "a84c31", "c96a3b");
        static readonly Color32[] Brass = PixelArt.Palette("2a170d", "5a3515", "8f5a1f", "c4892e", "e8b84a", "ffe08a");
        static readonly Color32[] Hole = PixelArt.Palette("020104", "05030a", "0b0714", "150e22");
        static readonly Color32[] Floor = PixelArt.Palette("05060a", "080a11", "0c0f18", "111521", "161c2b", "1d2537");
        static readonly Color32 Outline = PixelArt.Hex("07040b");

        public static void Build(PoolTable table)
        {
            var root = table.transform;
            BuildFloor(root);
            BuildLampSpill(root, table);
            BuildShadow(root, table);
            BuildTable(root, table);
            BuildDiamonds(root, table);
        }

        static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Vector2 pos, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        // ---------------------------------------------------------------- Table

        static void BuildTable(Transform root, PoolTable t)
        {
            float hw = t.playWidth / 2f, hh = t.playHeight / 2f, cw = t.cushionWidth;
            Rect outer = t.OuterBounds;
            int W = Even(Mathf.CeilToInt(outer.width * PixelArt.PPU));
            int H = Even(Mathf.CeilToInt(outer.height * PixelArt.PPU));
            var px = new Color32[W * H];

            Vector2 halfOuter = outer.size / 2f;
            const float cornerRadius = 0.55f;
            float feltHW = hw + cw, feltHH = hh + cw;
            var quads = t.CushionQuads;
            var pockets = t.Pockets;
            Vector2 lightDir = new Vector2(-0.55f, 0.83f); // light from top-left (screen up)

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2((x + 0.5f - W / 2f) / PixelArt.PPU, (y + 0.5f - H / 2f) / PixelArt.PPU);
                float outerDist = RoundedRectDistance(p, halfOuter, cornerRadius) * PixelArt.PPU; // <0 inside, in px
                if (outerDist > 0f) continue;

                float lamp = Lamp(p, hw, hh);
                Color32 c;

                // Pocket holes.
                Pocket hole = null;
                float holeDist = 0f;
                foreach (var pocket in pockets)
                {
                    float d = Vector2.Distance(p, pocket.transform.localPosition);
                    if (d < pocket.radius) { hole = pocket; holeDist = d; break; }
                }

                if (hole != null)
                {
                    float r = hole.radius;
                    Vector2 dir = (p - (Vector2)hole.transform.localPosition) / r;
                    float edge = (r - holeDist) * PixelArt.PPU;
                    if (edge < 1f) { c = Outline; }
                    else
                    {
                        // Far inner wall catches a little light (bottom-right crescent).
                        float wall = Mathf.Clamp01((holeDist / r - 0.55f) / 0.45f) * Mathf.Clamp01(Vector2.Dot(dir, -lightDir) + 0.2f);
                        c = PixelArt.Ramp(Hole, wall * 0.9f);
                    }
                }
                else if (InsideAny(quads, p, out var quad))
                {
                    // Cushion: top surface lit near the nose, darker toward the back.
                    float depth = Mathf.Max(Mathf.Abs(p.x) - hw, Mathf.Abs(p.y) - hh); // 0 at nose → cw at back
                    float depthPx = depth * PixelArt.PPU;
                    float v = 0.78f - depth / cw * 0.55f + lamp * 0.2f;
                    if (depthPx < 1f) v = 1f;                      // bright rubber nose line
                    else if (EdgeDistance(quad, p) * PixelArt.PPU < 1f) v -= 0.35f; // jaw / back outline
                    c = PixelArt.Ramp(Cushion, v);
                }
                else if (Mathf.Abs(p.x) <= feltHW && Mathf.Abs(p.y) <= feltHH)
                {
                    // Felt: lamp pool, painted AO along the cushions and around the pockets, cloth noise.
                    float edge = Mathf.Min(hw - Mathf.Abs(p.x), hh - Mathf.Abs(p.y)); // distance to cushion noses
                    float ao = (1f - Mathf.Clamp01(edge / 0.55f));
                    ao = ao * ao * 0.42f;
                    float pocketAo = 0f;
                    foreach (var pocket in pockets)
                    {
                        float d = Vector2.Distance(p, pocket.transform.localPosition) - pocket.radius;
                        pocketAo = Mathf.Max(pocketAo, 1f - Mathf.Clamp01(d / 0.6f));
                    }
                    float cloth = (PixelArt.ValueNoise(x * 0.08f, y * 0.08f, 11) - 0.5f) * 0.05f; // soft cloth variation, no per-pixel noise
                    float v = 0.18f + lamp * 0.72f - ao - pocketAo * 0.35f + cloth;
                    if (edge < 0f) v -= 0.12f; // channels into the pockets are in shadow
                    // Felt markings: head string and spots.
                    if (Mathf.Abs(p.y + hh / 2f) < 0.5f * PixelArt.Pixel && Mathf.Abs(p.x) < hw) v += 0.1f;
                    if (IsSpot(p, new Vector2(0f, -hh / 2f)) || IsSpot(p, new Vector2(0f, hh / 2f))) v += 0.25f;
                    c = PixelArt.Ramp(Felt, v);
                }
                else
                {
                    // Wood rail.
                    float innerDist = Mathf.Max(Mathf.Abs(p.x) - feltHW, Mathf.Abs(p.y) - feltHH) * PixelArt.PPU; // px from felt edge
                    bool longRail = Mathf.Abs(p.x) - feltHW > Mathf.Abs(p.y) - feltHH;
                    float grain = longRail
                        ? PixelArt.ValueNoise(x * 0.55f, y * 0.035f, 21) * 0.7f + PixelArt.ValueNoise(x * 1.7f, y * 0.12f, 22) * 0.3f
                        : PixelArt.ValueNoise(x * 0.035f, y * 0.55f, 23) * 0.7f + PixelArt.ValueNoise(x * 0.12f, y * 1.7f, 24) * 0.3f;
                    float v = 0.32f + (grain - 0.5f) * 0.42f + lamp * 0.32f;

                    // Bevels: outer edge catches the light on its top-left faces.
                    float fromOuter = -outerDist;
                    Vector2 n = OuterNormal(p, halfOuter);
                    float lit = Vector2.Dot(n, lightDir);
                    if (fromOuter < 1f) { c = Outline; goto write; }
                    if (fromOuter < 3f) v += lit * 0.3f + 0.05f;
                    if (innerDist < 1f) { c = Outline; goto write; }
                    if (innerDist < 2.5f) v += 0.28f; // polished inner lip

                    c = PixelArt.Ramp(Wood, v);

                    // Brass rings around the pockets.
                    foreach (var pocket in pockets)
                    {
                        Vector2 d = p - (Vector2)pocket.transform.localPosition;
                        float ring = (d.magnitude - pocket.radius) * PixelArt.PPU;
                        if (ring >= 0f && ring < 4f)
                        {
                            if (ring >= 3f) { c = Outline; break; }
                            float shine = Vector2.Dot(d.normalized, lightDir) * 0.5f + 0.5f;
                            c = PixelArt.Ramp(Brass, 0.25f + shine * 0.65f - ring * 0.08f);
                            break;
                        }
                    }
                }

                write:
                px[y * W + x] = c;
            }

            var tex = PixelArt.NewTexture(W, H, "TableBake");
            AddSprite("TableArt", root, PixelArt.ToSprite(tex, px), Vector2.zero, 0);
        }

        static void BuildDiamonds(Transform root, PoolTable t)
        {
            // Small glowing brass studs (HDR material → they catch the bloom).
            var px = new Color32[9];
            var gold = PixelArt.Hex("ffd27a");
            var light = PixelArt.Hex("fff4d0");
            px[1] = px[3] = px[5] = px[7] = gold;
            px[4] = light;
            var sprite = PixelArt.ToSprite(PixelArt.NewTexture(3, 3, "Diamond"), px);
            var glow = PixelArt.GlowMaterial(new Color(1.35f, 1.2f, 1f));

            float hw = t.playWidth / 2f, hh = t.playHeight / 2f;
            float mid = t.cushionWidth + t.railWidth / 2f;
            var positions = new List<Vector2>();
            for (int k = -3; k <= 3; k++)
            {
                if (k == 0) continue;
                positions.Add(new Vector2(-hw - mid, k * t.playHeight / 8f));
                positions.Add(new Vector2(hw + mid, k * t.playHeight / 8f));
            }
            for (int k = -1; k <= 1; k++)
            {
                positions.Add(new Vector2(k * t.playWidth / 4f, hh + mid));
                positions.Add(new Vector2(k * t.playWidth / 4f, -hh - mid));
            }
            foreach (var pos in positions)
            {
                // Odd-sized sprite: center it on a pixel center.
                var snapped = new Vector2(Mathf.Floor(pos.x * PixelArt.PPU) + 0.5f, Mathf.Floor(pos.y * PixelArt.PPU) + 0.5f) / PixelArt.PPU;
                AddSprite("Diamond", root, sprite, snapped, 1).sharedMaterial = glow;
            }
        }

        // ---------------------------------------------------------------- Shadow & floor

        static void BuildShadow(Transform root, PoolTable t)
        {
            Rect outer = t.OuterBounds;
            const int fringe = 10;
            int W = Even(Mathf.CeilToInt(outer.width * PixelArt.PPU) + fringe * 2);
            int H = Even(Mathf.CeilToInt(outer.height * PixelArt.PPU) + fringe * 2);
            var px = new Color32[W * H];
            var shadow = PixelArt.Hex("030207");
            Vector2 half = outer.size / 2f;
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                Vector2 p = new Vector2((x + 0.5f - W / 2f) / PixelArt.PPU, (y + 0.5f - H / 2f) / PixelArt.PPU);
                float d = RoundedRectDistance(p, half, 0.55f) * PixelArt.PPU; // px
                float a = 1f - Mathf.Clamp01((d + 2f) / fringe);
                px[y * W + x] = PixelArt.WithAlpha(shadow, (byte)(a * a * 0.8f * 255f)); // smooth falloff
            }
            var tex = PixelArt.NewTexture(W, H, "TableShadow");
            AddSprite("TableShadow", root, PixelArt.ToSprite(tex, px), new Vector2(10, -14) * PixelArt.Pixel, -10);
        }

        static void BuildFloor(Transform root)
        {
            // 48x48 px tile of large, uneven dungeon flagstones.
            const int S = 48;
            var px = new Color32[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int row = y / 24;
                int ox = row % 2 == 0 ? 0 : 12; // staggered rows
                int lx = (x + ox) % 24, ly = y % 24;
                int stone = ((x + ox) / 24) * 7 + row * 13;
                bool mortar = lx == 0 || ly == 0;
                float v;
                if (mortar) v = 0.02f;
                else
                {
                    v = 0.38f + (PixelArt.Hash(stone, 1, 31) - 0.5f) * 0.25f
                        + (PixelArt.ValueNoise(x * 0.15f, y * 0.15f, 32) - 0.5f) * 0.15f;
                    if (lx == 1 || ly == 23) v += 0.18f; // lit top-left edge
                    if (lx == 23 || ly == 1) v -= 0.15f;
                }
                px[y * S + x] = PixelArt.Ramp(Floor, v);
            }
            var tex = PixelArt.NewTexture(S, S, "FloorTile", repeat: true);
            var sr = AddSprite("Floor", root, PixelArt.ToSprite(tex, px, PixelArt.PPU, Vector2.one * 0.5f), Vector2.zero, -30);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(96, 96);
        }

        static void BuildLampSpill(Transform root, PoolTable t)
        {
            // Warm light pool on the floor around the table, smooth alpha.
            Rect outer = t.OuterBounds;
            int W = Even(Mathf.CeilToInt((outer.width + 10f) * PixelArt.PPU));
            int H = Even(Mathf.CeilToInt((outer.height + 10f) * PixelArt.PPU));
            var px = new Color32[W * H];
            var warm = PixelArt.Hex("ff9a4a");
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float nx = (x + 0.5f) / W * 2f - 1f, ny = (y + 0.5f) / H * 2f - 1f;
                float e = nx * nx + ny * ny;
                float a = Mathf.Clamp01(1f - e) * 0.1f;
                float q = a; // smooth
                px[y * W + x] = PixelArt.WithAlpha(warm, (byte)(q * 255f));
            }
            var tex = PixelArt.NewTexture(W, H, "LampSpill");
            AddSprite("LampSpill", root, PixelArt.ToSprite(tex, px), Vector2.zero, -25);
        }

        // ---------------------------------------------------------------- Geometry helpers

        static int Even(int v) => v % 2 == 0 ? v : v + 1;

        static float Lamp(Vector2 p, float hw, float hh)
        {
            float ex = p.x / (hw * 1.45f), ey = p.y / (hh * 1.2f);
            float e = ex * ex + ey * ey;
            return Mathf.Clamp01(1.05f - e * 0.85f);
        }

        static bool IsSpot(Vector2 p, Vector2 spot)
        {
            Vector2 d = (p - spot) * PixelArt.PPU;
            return d.x >= -1f && d.x < 1f && d.y >= -1f && d.y < 1f;
        }

        static float RoundedRectDistance(Vector2 p, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
            Vector2 qm = Vector2.Max(q, Vector2.zero);
            return qm.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }

        static Vector2 OuterNormal(Vector2 p, Vector2 half)
        {
            float dx = half.x - Mathf.Abs(p.x), dy = half.y - Mathf.Abs(p.y);
            return dx < dy ? new Vector2(Mathf.Sign(p.x), 0f) : new Vector2(0f, Mathf.Sign(p.y));
        }

        static bool InsideAny(IReadOnlyList<Vector2[]> quads, Vector2 p, out Vector2[] hit)
        {
            foreach (var q in quads)
            {
                if (InsideConvex(q, p)) { hit = q; return true; }
            }
            hit = null;
            return false;
        }

        static bool InsideConvex(Vector2[] poly, Vector2 p)
        {
            bool? sign = null;
            for (int i = 0; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                float cross = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
                if (Mathf.Abs(cross) < 1e-7f) continue;
                bool s = cross > 0f;
                if (sign == null) sign = s;
                else if (sign != s) return false;
            }
            return true;
        }

        /// <summary>Distance to the closest polygon edge, ignoring the nose edge (index 0 → 1).</summary>
        static float EdgeDistance(Vector2[] poly, Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < poly.Length; i++)
            {
                Vector2 a = poly[i], b = poly[(i + 1) % poly.Length];
                Vector2 ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
    }
}
