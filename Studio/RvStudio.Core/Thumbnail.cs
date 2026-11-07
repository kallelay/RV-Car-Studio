using System.Numerics;
using SkiaSharp;

namespace RvStudio.Core;

/// <summary>Small pictures of cars and models, drawn without the graphics card so a long list can be filled in the background</summary>
public static class Thumbnail
{
    public sealed record Part(Prm Model, Matrix4x4 World, SKBitmap? Texture);

    /// <summary>Black is see-through in textures without an alpha channel, also in a thumbnail</summary>
    public static SKBitmap? KeyedTexture(string? path)
    {
        using var src = Images.Load(path);
        if (src == null) return null;
        var o = new SKBitmap(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Premul);
        bool key = src.AlphaType == SKAlphaType.Opaque;
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var c = src.GetPixel(x, y);
                o.SetPixel(x, y, key && c.Red == 0 && c.Green == 0 && c.Blue == 0 ? SKColors.Transparent : c);
            }
        return o;
    }

    public static SKBitmap Render(IReadOnlyList<Part> parts, int size, SKColor? background = null, float yaw = 0.75f, float pitch = 0.4f)
    {
        var bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var cv = new SKCanvas(bmp);
        cv.Clear(background ?? new SKColor(0x2B, 0x2F, 0x36));

        var dir = new Vector3(MathF.Sin(yaw) * MathF.Cos(pitch), -MathF.Sin(pitch), MathF.Cos(yaw) * MathF.Cos(pitch));
        var right = Vector3.Normalize(Vector3.Cross(-dir, new Vector3(0, -1, 0)));
        var up = Vector3.Cross(right, -dir);

        var tris = new List<(float Depth, SKPoint A, SKPoint B, SKPoint C, SKPoint Ta, SKPoint Tb, SKPoint Tc, SKColor Ca, SKColor Cb, SKColor Cc, SKBitmap? Tex)>();
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;

        foreach (var part in parts)
        {
            var m = part.Model;
            var tex = part.Texture;
            foreach (var f in m.Faces)
                for (int t = 0; t < (f.IsQuad ? 2 : 1); t++)
                {
                    int i0 = 0, i1 = t == 0 ? 1 : 2, i2 = t == 0 ? 2 : 3;
                    int a = f.Index(i0), b = f.Index(i1), c = f.Index(i2);
                    if (a < 0 || b < 0 || c < 0 || a >= m.Verts.Length || b >= m.Verts.Length || c >= m.Verts.Length) continue;
                    var pa = Vector3.Transform(m.Verts[a].Pos, part.World);
                    var pb = Vector3.Transform(m.Verts[b].Pos, part.World);
                    var pc = Vector3.Transform(m.Verts[c].Pos, part.World);
                    SKPoint S(Vector3 p) => new(Vector3.Dot(p, right), -Vector3.Dot(p, up));
                    SKPoint sa = S(pa), sb = S(pb), sc = S(pc);
                    // The side turned away doesn't show, unless the polygon has two sides
                    float area = (sb.X - sa.X) * (sc.Y - sa.Y) - (sb.Y - sa.Y) * (sc.X - sa.X);
                    if (area > 0 && (f.Flags & FaceFlags.DoubleSided) == 0) continue;
                    minX = Math.Min(minX, Math.Min(sa.X, Math.Min(sb.X, sc.X)));
                    maxX = Math.Max(maxX, Math.Max(sa.X, Math.Max(sb.X, sc.X)));
                    minY = Math.Min(minY, Math.Min(sa.Y, Math.Min(sb.Y, sc.Y)));
                    maxY = Math.Max(maxY, Math.Max(sa.Y, Math.Max(sb.Y, sc.Y)));
                    float depth = Vector3.Dot(pa + pb + pc, dir);
                    SKBitmap? use = f.TPage >= 0 ? tex : null;
                    float w = use?.Width ?? 1, h = use?.Height ?? 1;
                    SKColor Col(int k)
                    {
                        var v = f.Colors[k];
                        byte al = (f.Flags & FaceFlags.Translucent) != 0 ? (byte)(v >> 24) : (byte)255;
                        return new SKColor((byte)(v >> 16), (byte)(v >> 8), (byte)v, al);
                    }
                    tris.Add((depth, sa, sb, sc,
                        new SKPoint(f.U[i0] * w, f.V[i0] * h), new SKPoint(f.U[i1] * w, f.V[i1] * h), new SKPoint(f.U[i2] * w, f.V[i2] * h),
                        Col(i0), Col(i1), Col(i2), use));
                }
        }
        if (tris.Count == 0) return bmp;

        // Far ones first: enough for low polygon cars
        tris.Sort((p, q) => p.Depth.CompareTo(q.Depth));
        float scale = size * 0.9f / Math.Max(Math.Max(maxX - minX, maxY - minY), 1e-3f);
        float ox = size / 2f - (minX + maxX) / 2 * scale, oy = size / 2f - (minY + maxY) / 2 * scale;
        SKPoint Fit(SKPoint p) => new(p.X * scale + ox, p.Y * scale + oy);

        var shaders = new Dictionary<SKBitmap, SKShader>();
        using var paint = new SKPaint { IsAntialias = true };
        foreach (var t in tris)
        {
            var pts = new[] { Fit(t.A), Fit(t.B), Fit(t.C) };
            var cols = new[] { t.Ca, t.Cb, t.Cc };
            if (t.Tex != null)
            {
                if (!shaders.TryGetValue(t.Tex, out var sh)) shaders[t.Tex] = sh = SKShader.CreateBitmap(t.Tex, SKShaderTileMode.Repeat, SKShaderTileMode.Repeat);
                paint.Shader = sh;
                cv.DrawVertices(SKVertexMode.Triangles, pts, new[] { t.Ta, t.Tb, t.Tc }, cols, SKBlendMode.Modulate, null, paint);
            }
            else
            {
                paint.Shader = null;
                cv.DrawVertices(SKVertexMode.Triangles, pts, null, cols, paint);
            }
        }
        foreach (var s in shaders.Values) s.Dispose();
        return bmp;
    }

    /// <summary>The car as it stands, without its aerial</summary>
    public static SKBitmap OfCar(Car c, int size)
    {
        var tex = KeyedTexture(c.Game.Find(c.Data.TPage));
        var parts = new List<Part>();
        foreach (var p in CarPose.Build(c.Data))
        {
            if (p.Kind is PartKind.AerialSection or PartKind.AerialTop) continue;
            if (CarTools.LoadModel(c, p.Slot) is { } m) parts.Add(new Part(m, p.World, tex));
        }
        var r = Render(parts, size);
        tex?.Dispose();
        return r;
    }

    /// <summary>A model on its own. The texture is the car.bmp next to it, if there is one</summary>
    public static SKBitmap OfModel(Prm m, string? texturePath, int size)
    {
        using var tex = KeyedTexture(texturePath);
        return Render(new[] { new Part(m, Matrix4x4.Identity, tex) }, size);
    }

    public static byte[] Png(SKBitmap b)
    {
        using var img = SKImage.FromBitmap(b);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        return data.ToArray();
    }
}
