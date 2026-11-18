using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// What surrounds the sprite table (Assets/Art/Sprites/pool_table.png), in pixel art at
    /// <see cref="PixelArt.PPU"/>: the dungeon-stone floor, the warm lamp spill on it and the table's
    /// drop shadow. Gradients are smooth interpolations along small palettes (no dithering).
    /// </summary>
    public static class TableArt
    {
        // Palette (sRGB), dark → light.
        static readonly Color32[] Floor = PixelArt.Palette("05060a", "080a11", "0c0f18", "111521", "161c2b", "1d2537");

        /// <summary>
        /// Sprite table: only what surrounds it (floor, lamp spill, drop shadow), under a "Surroundings" child.
        /// They go on the table's sorting layer with negative orders, so they stay below the table sprite.
        /// </summary>
        public static void BuildSurroundings(PoolTable table)
        {
            var root = new GameObject("Surroundings").transform;
            root.SetParent(table.transform, false);
            BuildFloor(root);
            BuildLampSpill(root, table.OuterBounds);
            BuildShadow(root, table.OuterBounds);
            string layer = table.visual != null ? table.visual.sortingLayerName : "Default";
            foreach (var sr in root.GetComponentsInChildren<SpriteRenderer>()) sr.sortingLayerName = layer;
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

        // ---------------------------------------------------------------- Shadow & floor

        static void BuildShadow(Transform root, Rect outer)
        {
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
            AddSprite("TableShadow", root, PixelArt.ToSprite(tex, px), outer.center + new Vector2(10, -14) * PixelArt.Pixel, -10);
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

        static void BuildLampSpill(Transform root, Rect outer)
        {
            // Warm light pool on the floor around the table, smooth alpha.
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
            AddSprite("LampSpill", root, PixelArt.ToSprite(tex, px), outer.center, -25);
        }

        // ---------------------------------------------------------------- Geometry helpers

        static int Even(int v) => v % 2 == 0 ? v : v + 1;

        static float RoundedRectDistance(Vector2 p, Vector2 half, float radius)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (half - Vector2.one * radius);
            Vector2 qm = Vector2.Max(q, Vector2.zero);
            return qm.magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
        }
    }
}
