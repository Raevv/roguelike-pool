using UnityEngine;

namespace RoguelikePool
{
    /// <summary>
    /// Pixel-art cue (352x5 px, pivot on the tip): blue chalked tip, ivory ferrule, tapered maple
    /// shaft with a highlight row, brass joint, dark forearm with ivory/brass inlaid points,
    /// stitched wrap and butt cap. Its drop shadow keeps a fixed world-space offset.
    /// </summary>
    public static class CueArt
    {
        const int Length = 352, Height = 5;

        public static void Build(Transform cueRoot)
        {
            var sprite = CreateSprite();

            var shadowGo = new GameObject("CueShadow");
            shadowGo.transform.SetParent(cueRoot, false);
            var shadow = shadowGo.AddComponent<SpriteRenderer>();
            shadow.sprite = sprite;
            shadow.color = new Color(0.01f, 0.005f, 0.03f, 0.5f);
            shadow.sortingOrder = 19;
            shadowGo.AddComponent<WorldOffset>().offset = new Vector2(3f, -4f) * PixelArt.Pixel;

            var bodyGo = new GameObject("CueBody");
            bodyGo.transform.SetParent(cueRoot, false);
            var body = bodyGo.AddComponent<SpriteRenderer>();
            body.sprite = sprite;
            body.sortingOrder = 20;
        }

        static Sprite CreateSprite()
        {
            var px = new Color32[Length * Height];
            Color32 chalk = PixelArt.Hex("3f86e0"), chalkDark = PixelArt.Hex("23509a");
            Color32 ivory = PixelArt.Hex("efe4c8"), ivoryDark = PixelArt.Hex("b8a888");
            var maple = PixelArt.Palette("7a5530", "a87c45", "d4a865", "f0cf8c");
            var brass = PixelArt.Palette("6e4a1e", "c4892e", "ffe08a");
            var ebony = PixelArt.Palette("12080e", "2a1418", "45201f", "62302a");
            Color32 wrap = PixelArt.Hex("1b1524"), wrapLight = PixelArt.Hex("3a3048");
            Color32 outline = PixelArt.Hex("0a0610");

            for (int i = 0; i < Length; i++)
            {
                int fromTip = Length - 1 - i;
                bool thin = fromTip < 150;
                for (int row = 0; row < Height; row++)
                {
                    bool edge = row == 0 || row == Height - 1;
                    if (thin && edge) continue; // 3 px thin end, 5 px thick end
                    float shade = row == Height - 2 ? 1f : row == 2 ? 0.6f : row == 1 ? 0.25f : 0f; // top row lit
                    Color32 c;
                    if (fromTip < 2) c = row == 3 ? PixelArt.Hex("6aa8f0") : row == 1 ? chalkDark : chalk;
                    else if (fromTip < 8) c = row == 3 ? ivory : row == 1 ? ivoryDark : ivory;
                    else if (fromTip < 210)
                    {
                        if (!thin && edge) c = outline;
                        else c = maple[Mathf.Clamp(Mathf.RoundToInt(shade * 3f), 0, 3)];
                    }
                    else if (fromTip < 214) c = edge ? outline : brass[row == 3 ? 2 : row == 2 ? 1 : 0];
                    else if (fromTip < 300)
                    {
                        if (edge) c = outline;
                        else
                        {
                            // Inlaid points (triangles pointing at the tip) every 22 px.
                            int local = (fromTip - 214) % 43;
                            int half = local < 22 ? local : 43 - local;
                            bool point = local < 22 && Mathf.Abs(row - 2) <= half / 7;
                            c = point ? (local % 2 == 0 ? ivory : (Color32)brass[1]) : ebony[Mathf.Clamp(Mathf.RoundToInt(shade * 3f), 0, 3)];
                        }
                    }
                    else if (fromTip < 340)
                    {
                        if (edge) c = outline;
                        else c = ((fromTip + row) % 3 == 0) ? wrapLight : wrap; // stitched wrap
                    }
                    else if (fromTip < 344) c = edge ? outline : brass[row == 3 ? 2 : 1];
                    else c = edge ? outline : ebony[row == 3 ? 2 : 0];
                    px[row * Length + i] = c;
                }
            }

            var tex = PixelArt.NewTexture(Length, Height, "Cue");
            return PixelArt.ToSprite(tex, px, PixelArt.PPU, new Vector2(1f, 0.5f));
        }
    }

    /// <summary>Keeps a child at a fixed world-space offset from its parent (for drop shadows).</summary>
    public class WorldOffset : MonoBehaviour
    {
        public Vector2 offset;

        void LateUpdate()
        {
            if (transform.parent == null) return;
            transform.position = (Vector2)transform.parent.position + offset;
            transform.rotation = transform.parent.rotation;
        }
    }
}
