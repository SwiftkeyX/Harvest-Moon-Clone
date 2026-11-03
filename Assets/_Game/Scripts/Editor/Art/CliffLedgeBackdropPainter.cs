using UnityEngine;

namespace Farm.EditorTools.Art
{
    // Paints the far valley behind the Cliff Ledge at the scene's pixel resolution: a warm peach haze,
    // pale mist pools, and thin sketch lines for the distant ridges and trees.
    internal static class CliffLedgeBackdropPainter
    {
        static readonly Color SkyTop = Hex("fff2dc");
        static readonly Color SkyLow = Hex("fde6c4");
        static readonly Color Ridge = Hex("fbe2bd");
        static readonly Color Valley = Hex("f9d39f");
        static readonly Color ValleyLow = Hex("f4c690");
        static readonly Color Mist = Hex("fff0d8");
        static readonly Color Line = Hex("e6c4a0");
        static readonly Color LineFaint = Hex("efd2ae");

        public static Texture2D Paint(int width, int height, bool graybox)
        {
            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[width * height];
            var rnd = new System.Random(11);

            // Row 0 of `px` is the bottom of the image, so `v` counts down from the top.
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float v = 1f - (y + 0.5f) / height, u = (x + 0.5f) / width;
                Color c;
                if (graybox) c = Color.Lerp(Hex("d6d6d6"), Hex("bdbdbd"), v);
                else
                {
                    float ridge = RidgeAt(u);
                    float valleyTop = 0.34f + 0.03f * Mathf.PerlinNoise(u * 6f, 3.1f);
                    if (v < ridge) c = Color.Lerp(SkyTop, SkyLow, v / 0.3f);
                    else if (v < valleyTop) c = Ridge;
                    else
                    {
                        c = Color.Lerp(Valley, ValleyLow, Mathf.InverseLerp(0.55f, 1f, v));
                        float m = Mathf.PerlinNoise(u * 7f + 2f, v * 11f) * 0.7f + Mathf.PerlinNoise(u * 19f, v * 27f + 5f) * 0.3f;
                        float band = Mathf.InverseLerp(0.95f, 0.4f, v); // more mist up near the haze line
                        if (m > 0.62f - band * 0.18f) c = Mist;
                    }
                }
                px[y * width + x] = c;
            }

            if (!graybox)
            {
                // Ridge sketch lines: two layers of distant mountains.
                DrawRidge(px, width, height, RidgeAt, Line);
                DrawRidge(px, width, height, u => RidgeAt(u * 0.7f + 0.3f) + 0.09f, LineFaint);
                // Mist pool edges.
                OutlineMist(px, width, height, Line);
                // Sketched pines down in the valley.
                for (int i = 0; i < 26; i++)
                {
                    int cx = rnd.Next(width / 2, width), cy = (int)(height * (1f - (0.45f + (float)rnd.NextDouble() * 0.5f)));
                    int h = 4 + rnd.Next(6);
                    DrawPine(px, width, height, cx, cy, h, i % 3 == 0 ? Line : LineFaint);
                }
            }

            tex.SetPixels(px);
            tex.Apply();
            return tex;
        }

        static float RidgeAt(float u) =>
            0.2f + 0.07f * Mathf.PerlinNoise(u * 4f, 0.5f) - 0.05f * Mathf.Abs(Mathf.Sin(u * 9f + 1f));

        static void DrawRidge(Color[] px, int w, int h, System.Func<float, float> ridge, Color col)
        {
            int prev = -1;
            for (int x = 0; x < w; x++)
            {
                int y = h - 1 - Mathf.RoundToInt(ridge((x + 0.5f) / w) * h);
                if (prev >= 0)
                    for (int yy = Mathf.Min(prev, y); yy <= Mathf.Max(prev, y); yy++) Set(px, w, h, x, yy, col);
                else Set(px, w, h, x, y, col);
                prev = y;
            }
        }

        static void OutlineMist(Color[] px, int w, int h, Color col)
        {
            var copy = (Color[])px.Clone();
            for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                if (copy[y * w + x] != Mist) continue;
                if (copy[(y - 1) * w + x] == Valley || copy[(y - 1) * w + x] == ValleyLow ||
                    copy[y * w + x + 1] == Valley || copy[y * w + x - 1] == Valley)
                    if ((x + y) % 3 != 0) px[y * w + x] = col;
            }
        }

        static void DrawPine(Color[] px, int w, int h, int cx, int cy, int height, Color col)
        {
            for (int i = 0; i <= height; i++)
            {
                int half = i / 2;
                Set(px, w, h, cx - half, cy + height - i, col);
                Set(px, w, h, cx + half, cy + height - i, col);
            }
            Set(px, w, h, cx, cy - 1, col);
        }

        static void Set(Color[] px, int w, int h, int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            px[y * w + x] = c;
        }

        static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out var c);
            return c;
        }
    }
}
