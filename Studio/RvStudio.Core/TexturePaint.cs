using SkiaSharp;

namespace RvStudio.Core;

/// <summary>
/// Painting on a car texture and making one: brush, eraser back to the original, flood fill, colour pick, and the
/// generators (plain, gradient, stripes, checks, carbon fibre, grunge, number, panel lines, a colour per piece).
/// A generator can be kept inside a mask (the selected faces' outline), so "paint these faces" is one click.
/// All pictures are BGRA 8888 premultiplied, as Skia and Avalonia draw them.
/// </summary>
public static class TexturePaint
{
    public static SKBitmap Blank(int w, int h, SKColor c)
    {
        var b = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
        b.Erase(c);
        return b;
    }

    /// <summary>A copy in the working format, whatever the file was.</summary>
    public static SKBitmap Working(SKBitmap src)
    {
        var b = new SKBitmap(new SKImageInfo(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Premul));
        using (var c = new SKCanvas(b)) { c.Clear(SKColors.Transparent); c.DrawBitmap(src, 0, 0); }
        // Black without alpha is see-through in the game: keep it opaque here, it's saved as black again
        if (src.AlphaType == SKAlphaType.Opaque || src.ColorType is SKColorType.Rgb888x or SKColorType.Rgb565)
        {
            var px = b.Pixels;
            for (int i = 0; i < px.Length; i++) px[i] = px[i].WithAlpha(255);
            b.Pixels = px;
        }
        return b;
    }

    static SKColor Mix(SKColor a, SKColor b, float t)
    {
        t = Math.Clamp(t, 0, 1);
        return new SKColor((byte)(a.Red + (b.Red - a.Red) * t), (byte)(a.Green + (b.Green - a.Green) * t),
            (byte)(a.Blue + (b.Blue - a.Blue) * t), (byte)(a.Alpha + (b.Alpha - a.Alpha) * t));
    }

    /// <summary>The brush's tip: round, or square (crisp edges, pixel art, panel lines)</summary>
    public enum Tip { Round, Square }

    /// <summary>What a dab does: paint the colour, put the first texture back (eraser), or work on what's there</summary>
    public enum Effect { Paint, Restore, Blur, Sharpen, Lighten, Burn }

    /// <summary>
    /// One dab of the brush at (x, y) in pixels. `hardness` 1 is a hard edge, 0 fades from the middle.
    /// With `restore` set the dab paints the original picture back (the eraser). Returns the pixels' box it touched.
    /// </summary>
    public static SKRectI Dab(SKBitmap b, float x, float y, float radius, float hardness, SKColor colour, float opacity, SKBitmap? restore = null, SKPath? mask = null,
        Tip tip = Tip.Round, Effect fx = Effect.Paint)
    {
        if (restore != null && fx == Effect.Paint) fx = Effect.Restore;
        int x0 = Math.Max(0, (int)(x - radius - 1)), x1 = Math.Min(b.Width - 1, (int)(x + radius + 1));
        int y0 = Math.Max(0, (int)(y - radius - 1)), y1 = Math.Min(b.Height - 1, (int)(y + radius + 1));
        if (x1 < x0 || y1 < y0) return SKRectI.Empty;
        float inner = radius * Math.Clamp(hardness, 0, 1);
        // Blur and sharpen read what was there before this dab: a copy of the box, one pixel wider
        SKColor[,]? before = null;
        int bx0 = Math.Max(0, x0 - 1), by0 = Math.Max(0, y0 - 1), bx1 = Math.Min(b.Width - 1, x1 + 1), by1 = Math.Min(b.Height - 1, y1 + 1);
        if (fx is Effect.Blur or Effect.Sharpen)
        {
            before = new SKColor[bx1 - bx0 + 1, by1 - by0 + 1];
            for (int py = by0; py <= by1; py++) for (int px = bx0; px <= bx1; px++) before[px - bx0, py - by0] = b.GetPixel(px, py);
        }
        SKColor Around(int px, int py)
        {
            int r = 0, g = 0, bl = 0, a = 0, n = 0;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int qx = Math.Clamp(px + dx, bx0, bx1) - bx0, qy = Math.Clamp(py + dy, by0, by1) - by0;
                    var c = before![qx, qy];
                    r += c.Red; g += c.Green; bl += c.Blue; a += c.Alpha; n++;
                }
            return new SKColor((byte)(r / n), (byte)(g / n), (byte)(bl / n), (byte)(a / n));
        }
        static byte B(float v) => (byte)Math.Clamp(v, 0, 255);
        for (int py = y0; py <= y1; py++)
            for (int px = x0; px <= x1; px++)
            {
                float ddx = px + 0.5f - x, ddy = py + 0.5f - y;
                float d = tip == Tip.Square ? MathF.Max(MathF.Abs(ddx), MathF.Abs(ddy)) : MathF.Sqrt(ddx * ddx + ddy * ddy);
                if (d > radius) continue;
                if (mask != null && !mask.Contains(px + 0.5f, py + 0.5f)) continue;
                float w = d <= inner ? 1 : 1 - (d - inner) / MathF.Max(radius - inner, 1e-3f);
                w *= opacity;
                if (w <= 0) continue;
                var now = b.GetPixel(px, py);
                SKColor to;
                switch (fx)
                {
                    // The eraser past the first texture (a bigger new page): back to the page's white, not the brush colour
                    case Effect.Restore: to = restore != null && px < restore.Width && py < restore.Height ? restore.GetPixel(px, py) : SKColors.White; break;
                    case Effect.Blur: to = Around(px, py); break;
                    case Effect.Sharpen:
                        {
                            var c = before![px - bx0, py - by0];
                            var m = Around(px, py);
                            to = new SKColor(B(c.Red * 2f - m.Red), B(c.Green * 2f - m.Green), B(c.Blue * 2f - m.Blue), c.Alpha);
                            break;
                        }
                    // Lighten (dodge) goes half way to white, burn half way to black, each dab: strokes build up
                    case Effect.Lighten: to = new SKColor(B(now.Red + (255 - now.Red) * 0.5f), B(now.Green + (255 - now.Green) * 0.5f), B(now.Blue + (255 - now.Blue) * 0.5f), now.Alpha); break;
                    case Effect.Burn: to = new SKColor(B(now.Red * 0.5f), B(now.Green * 0.5f), B(now.Blue * 0.5f), now.Alpha); break;
                    default: to = colour; break;
                }
                b.SetPixel(px, py, Mix(now, to, w));
            }
        return new SKRectI(x0, y0, x1 + 1, y1 + 1);
    }

    /// <summary>A line of dabs from a to b, close enough together to look like one stroke.</summary>
    public static void Stroke(SKBitmap b, SKPoint a, SKPoint c, float radius, float hardness, SKColor colour, float opacity, SKBitmap? restore = null, SKPath? mask = null,
        Tip tip = Tip.Round, Effect fx = Effect.Paint)
    {
        float len = SKPoint.Distance(a, c);
        int n = Math.Max(1, (int)(len / MathF.Max(radius * 0.3f, 0.5f)));
        // Dabs overlap: each one lighter so a stroke reaches about `opacity`
        float each = 1 - MathF.Pow(1 - Math.Clamp(opacity, 0.01f, 1), 1f / Math.Max(1, MathF.Min(n, radius / MathF.Max(radius * 0.3f, 0.5f))));
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            Dab(b, a.X + (c.X - a.X) * t, a.Y + (c.Y - a.Y) * t, radius, hardness, colour, opacity >= 0.999f && fx is Effect.Paint or Effect.Restore ? 1 : each, restore, mask, tip, fx);
        }
    }

    /// <summary>A filled polygon through the points (pixels), smooth edged, inside the mask if there's one. Returns how many pixels changed</summary>
    public static int Polygon(SKBitmap b, IReadOnlyList<SKPoint> pts, SKColor colour, float opacity, SKPath? mask = null)
    {
        if (pts.Count < 3) return 0;
        using var shape = new SKPath();
        shape.MoveTo(pts[0]);
        for (int i = 1; i < pts.Count; i++) shape.LineTo(pts[i]);
        shape.Close();
        // Drawn on its own layer first: how much of each pixel the shape covers, then mixed in as the brush does
        using var cover = new SKBitmap(b.Width, b.Height, SKColorType.Alpha8, SKAlphaType.Premul);
        using (var cv = new SKCanvas(cover))
        {
            cv.Clear(SKColors.Transparent);
            if (mask != null) cv.ClipPath(mask, antialias: true);
            using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill };
            cv.DrawPath(shape, paint);
        }
        var r = SKRectI.Ceiling(shape.Bounds);
        int changed = 0;
        for (int y = Math.Max(0, r.Top); y < Math.Min(b.Height, r.Bottom + 1); y++)
            for (int x = Math.Max(0, r.Left); x < Math.Min(b.Width, r.Right + 1); x++)
            {
                float k = cover.GetPixel(x, y).Alpha / 255f * Math.Clamp(opacity, 0, 1);
                if (k <= 0) continue;
                b.SetPixel(x, y, Mix(b.GetPixel(x, y), colour, k));
                changed++;
            }
        return changed;
    }

    /// <summary>Flood fill from (x, y): joined pixels within `tolerance` (0 to 1) of the one clicked. Returns how many changed.</summary>
    public static int Fill(SKBitmap b, int x, int y, SKColor colour, float tolerance, SKPath? mask = null)
    {
        if (x < 0 || y < 0 || x >= b.Width || y >= b.Height) return 0;
        var px = b.Pixels;
        int w = b.Width, h = b.Height;
        var start = px[y * w + x];
        int tol = (int)(tolerance * 255 * 3);
        bool Near(SKColor c) => Math.Abs(c.Red - start.Red) + Math.Abs(c.Green - start.Green) + Math.Abs(c.Blue - start.Blue) <= tol;
        var done = new bool[w * h];
        var stack = new Stack<int>();
        stack.Push(y * w + x);
        int n = 0;
        while (stack.Count > 0)
        {
            int i = stack.Pop();
            if (done[i]) continue;
            done[i] = true;
            if (!Near(px[i])) continue;
            int cx = i % w, cy = i / w;
            if (mask != null && !mask.Contains(cx + 0.5f, cy + 0.5f)) continue;
            px[i] = colour; n++;
            if (cx > 0) stack.Push(i - 1);
            if (cx < w - 1) stack.Push(i + 1);
            if (cy > 0) stack.Push(i - w);
            if (cy < h - 1) stack.Push(i + w);
        }
        b.Pixels = px;
        return n;
    }

    public static SKColor Pick(SKBitmap b, int x, int y) =>
        x < 0 || y < 0 || x >= b.Width || y >= b.Height ? SKColors.Black : b.GetPixel(x, y);

    // ------------------------------------------------ generators ------------------------------------------------

    public static readonly string[] Kinds = { "Plain colour", "Gradient", "Racing stripes", "Checks", "Carbon fibre", "Grunge and dirt", "Number", "Panel lines", "A colour per piece", "Light and shade" };

    /// <summary>What a generator goes by. Not every one uses everything.</summary>
    public sealed record GenOptions(SKColor A, SKColor B, float Size = 0.1f, float Angle = 0, float Amount = 1, int Seed = 1, string Text = "7");

    /// <summary>
    /// Draws generator `kind` over the picture, kept inside `mask` when given. `m` and `faces` give the pieces for
    /// panel lines, a colour per piece and light and shade.
    /// </summary>
    public static void Generate(SKBitmap b, int kind, GenOptions o, SKPath? mask = null, Prm? m = null, IList<int>? faces = null)
    {
        int w = b.Width, h = b.Height;
        using var layer = Blank(w, h, SKColors.Transparent);
        using (var c = new SKCanvas(layer))
        {
            switch (kind)
            {
                case 0: c.Clear(o.A); break;
                case 1:
                    {
                        float a = o.Angle * MathF.PI / 180;
                        var mid = new SKPoint(w / 2f, h / 2f);
                        var d = new SKPoint(MathF.Sin(a) * w / 2, -MathF.Cos(a) * h / 2);
                        using var sh = SKShader.CreateLinearGradient(mid - d, mid + d, new[] { o.A, o.B }, SKShaderTileMode.Clamp);
                        using var p = new SKPaint { Shader = sh };
                        c.DrawRect(0, 0, w, h, p);
                        break;
                    }
                case 2:
                    {
                        // Two stripes down the middle with a gap, as on a race car; Size is a stripe's width
                        c.Save();
                        c.RotateDegrees(o.Angle, w / 2f, h / 2f);
                        float sw = MathF.Max(1, o.Size * w), gap = sw * 0.35f;
                        using var p = new SKPaint { Color = o.A, IsAntialias = true };
                        using var edge = new SKPaint { Color = o.B, IsAntialias = true };
                        float big = MathF.Max(w, h) * 2;
                        foreach (float x in new[] { w / 2f - gap / 2 - sw, w / 2f + gap / 2 })
                        {
                            c.DrawRect(x - sw * 0.12f, -big, sw * 1.24f, big * 3, edge);
                            c.DrawRect(x, -big, sw, big * 3, p);
                        }
                        c.Restore();
                        break;
                    }
                case 3:
                    {
                        float s = MathF.Max(2, o.Size * w);
                        c.Save();
                        c.RotateDegrees(o.Angle, w / 2f, h / 2f);
                        using var pa = new SKPaint { Color = o.A }; using var pb = new SKPaint { Color = o.B };
                        int n = (int)(MathF.Max(w, h) * 2 / s) + 2;
                        for (int i = -n; i < n; i++) for (int j = -n; j < n; j++)
                            c.DrawRect(w / 2f + i * s, h / 2f + j * s, s + 0.5f, s + 0.5f, ((i + j) & 1) == 0 ? pa : pb);
                        c.Restore();
                        break;
                    }
                case 4:
                    {
                        // Woven tows: each cell lit along its weave, alternate cells turned
                        float s = MathF.Max(2, o.Size * w * 0.25f);
                        for (float y = 0; y < h; y += s)
                            for (float x = 0; x < w; x += s)
                            {
                                bool across = (((int)(x / s) + (int)(y / s)) & 1) == 0;
                                var a = new SKPoint(x, y);
                                var e = across ? new SKPoint(x, y + s) : new SKPoint(x + s, y);
                                using var sh = SKShader.CreateLinearGradient(a, e, new[] { o.A, o.B, o.A }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
                                using var p = new SKPaint { Shader = sh };
                                c.DrawRect(x, y, s, s, p);
                            }
                        break;
                    }
                case 5:
                    {
                        // Soft blotches of B, more at the bottom where dirt flies
                        var rnd = new Random(o.Seed);
                        int n = (int)(w * h / 900f * Math.Clamp(o.Amount, 0.05f, 3));
                        using var p = new SKPaint { IsAntialias = true };
                        for (int i = 0; i < n; i++)
                        {
                            float x = (float)rnd.NextDouble() * w;
                            float y = MathF.Pow((float)rnd.NextDouble(), 0.6f) * h;
                            float r = (0.2f + (float)rnd.NextDouble()) * o.Size * w * 0.3f + 1;
                            using var sh = SKShader.CreateRadialGradient(new SKPoint(x, y), r, new[] { o.B.WithAlpha((byte)(o.B.Alpha * (0.15 + 0.35 * rnd.NextDouble()))), o.B.WithAlpha(0) }, SKShaderTileMode.Clamp);
                            p.Shader = sh;
                            c.DrawCircle(x, y, r, p);
                        }
                        break;
                    }
                case 6:
                    {
                        // A number in a round plate, in the middle of the mask (or the page)
                        var box = mask != null && !mask.Bounds.IsEmpty ? mask.Bounds : new SKRect(0, 0, w, h);
                        float r = MathF.Min(box.Width, box.Height) * 0.42f * Math.Clamp(o.Amount, 0.1f, 2f);
                        using var plate = new SKPaint { Color = o.B, IsAntialias = true };
                        c.DrawCircle(box.MidX, box.MidY, r, plate);
                        using var ink = new SKPaint { Color = o.A, IsAntialias = true, Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold), TextSize = r * 1.2f };
                        var text = string.IsNullOrEmpty(o.Text) ? "7" : o.Text;
                        float tw = ink.MeasureText(text);
                        while (tw > r * 1.7f && ink.TextSize > 4) { ink.TextSize *= 0.9f; tw = ink.MeasureText(text); }
                        c.Save();
                        c.RotateDegrees(o.Angle, box.MidX, box.MidY);
                        c.DrawText(text, box.MidX - tw / 2, box.MidY + ink.TextSize * 0.36f, ink);
                        c.Restore();
                        break;
                    }
                case 7:
                    if (m != null && faces != null)
                    {
                        // The pieces' edges as dark seams, as panel gaps; light on one side for depth
                        float lw = MathF.Max(1, o.Size * w * 0.05f);
                        using var dark = new SKPaint { Color = o.A, IsStroke = true, StrokeWidth = lw, IsAntialias = true };
                        using var light = new SKPaint { Color = o.B, IsStroke = true, StrokeWidth = lw * 0.6f, IsAntialias = true };
                        foreach (var island in UvKit.Islands(m, faces))
                        {
                            using var path = UvKit.Outline(m, island, w, h);
                            using var shifted = new SKPath(path);
                            shifted.Transform(SKMatrix.CreateTranslation(lw * 0.7f, lw * 0.7f));
                            c.DrawPath(shifted, light);
                            c.DrawPath(path, dark);
                        }
                    }
                    break;
                case 8:
                    if (m != null && faces != null)
                    {
                        var rnd = new Random(o.Seed);
                        using var p = new SKPaint { IsAntialias = true };
                        foreach (var island in UvKit.Islands(m, faces))
                        {
                            float hue = (float)rnd.NextDouble() * 360;
                            p.Color = SKColor.FromHsl(hue, 65, 55);
                            using var path = UvKit.Outline(m, island, w, h);
                            c.DrawPath(path, p);
                        }
                    }
                    break;
                case 9:
                    if (m != null && faces != null)
                    {
                        // Each face lit by the sun from above and ahead: darker underneath, as the game's own shading
                        var sun = System.Numerics.Vector3.Normalize(new System.Numerics.Vector3(0.3f, -1f, 0.5f));
                        using var p = new SKPaint { IsAntialias = true };
                        foreach (var i in faces)
                        {
                            var f = m.Faces[i];
                            if (!UvKit.Textured(f)) continue;
                            var a = m.Verts[f.V0].Pos; var bb = m.Verts[f.V1].Pos; var cc = m.Verts[f.V2].Pos;
                            var n = System.Numerics.Vector3.Cross(bb - a, cc - a);
                            if (n.LengthSquared() < 1e-12f) continue;
                            // Re-Volt winds faces so the cross product points inwards
                            float lit = Math.Clamp(System.Numerics.Vector3.Dot(-System.Numerics.Vector3.Normalize(n), -sun), -1, 1);
                            float dark = (1 - (lit + 1) / 2) * Math.Clamp(o.Amount, 0, 1);
                            p.Color = SKColors.Black.WithAlpha((byte)(dark * 200));
                            using var path = UvKit.Outline(m, new[] { i }, w, h);
                            c.DrawPath(path, p);
                        }
                    }
                    break;
            }
        }
        // Onto the picture, inside the mask only
        using var cv = new SKCanvas(b);
        if (mask != null) cv.ClipPath(mask, SKClipOperation.Intersect, true);
        using var put = new SKPaint { Color = SKColors.White.WithAlpha((byte)(255 * (kind is 5 or 9 ? 1 : Math.Clamp(o.Amount, 0, 1)))) };
        if (kind is 6) put.Color = SKColors.White;
        cv.DrawBitmap(layer, 0, 0, put);
    }
}
