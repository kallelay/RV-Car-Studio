using System.Numerics;
using SkiaSharp;

namespace RvStudio.Core;

/// <summary>A face corner: which face and which of its 3 or 4 corners.</summary>
public readonly record struct UvCorner(int Face, int Corner);

/// <summary>
/// The texture places of a model's faces (U, V from 0 to 1 over the texture page): islands, moving, turning, sizing,
/// flipping, projecting from a side, unwrapping by sides and packing into the page. Only textured faces (TPage 0 or more).
/// </summary>
public static class UvKit
{
    public static bool Textured(PrmFace f) => f.TPage >= 0;

    public static Vector2 Uv(PrmFace f, int c) => new(f.U[c], f.V[c]);
    public static void SetUv(PrmFace f, int c, Vector2 p) { f.U[c] = p.X; f.V[c] = p.Y; }

    /// <summary>Every face's U and V, for undo.</summary>
    public static float[] Copy(Prm m)
    {
        var r = new float[m.Faces.Length * 8];
        for (int i = 0; i < m.Faces.Length; i++)
            for (int k = 0; k < 4; k++) { r[i * 8 + k * 2] = m.Faces[i].U[k]; r[i * 8 + k * 2 + 1] = m.Faces[i].V[k]; }
        return r;
    }

    public static void Paste(Prm m, float[] uv)
    {
        if (uv.Length != m.Faces.Length * 8) return;
        for (int i = 0; i < m.Faces.Length; i++)
            for (int k = 0; k < 4; k++) { m.Faces[i].U[k] = uv[i * 8 + k * 2]; m.Faces[i].V[k] = uv[i * 8 + k * 2 + 1]; }
    }

    public static IEnumerable<UvCorner> Corners(Prm m, IEnumerable<int> faces)
    {
        foreach (var i in faces)
            for (int c = 0; c < m.Faces[i].Corners; c++) yield return new UvCorner(i, c);
    }

    /// <summary>
    /// Faces joined in the texture: they share a vertex at the same texture place (within a pixel of 1024). Each island is
    /// a list of face numbers; untextured faces are left out.
    /// </summary>
    public static List<List<int>> Islands(Prm m, IEnumerable<int>? only = null)
    {
        var faces = (only ?? Enumerable.Range(0, m.Faces.Length)).Where(i => Textured(m.Faces[i])).ToList();
        var parent = new Dictionary<int, int>();
        foreach (var f in faces) parent[f] = f;
        int Find(int x) { while (parent[x] != x) x = parent[x] = parent[parent[x]]; return x; }
        var seen = new Dictionary<(int V, int U, int W), int>();
        foreach (var f in faces)
        {
            var face = m.Faces[f];
            for (int c = 0; c < face.Corners; c++)
            {
                var key = (V: (int)face.Index(c), U: (int)MathF.Round(face.U[c] * 1024), W: (int)MathF.Round(face.V[c] * 1024));
                if (seen.TryGetValue(key, out var other)) { int a = Find(f), b = Find(other); if (a != b) parent[a] = b; }
                else seen[key] = f;
            }
        }
        return faces.GroupBy(Find).Select(g => g.ToList()).ToList();
    }

    /// <summary>The island holding face `face`.</summary>
    public static List<int> IslandOf(Prm m, int face) => Islands(m).FirstOrDefault(i => i.Contains(face)) ?? new() { face };

    public static (Vector2 Min, Vector2 Max) Bounds(Prm m, IEnumerable<int> faces)
    {
        Vector2 lo = new(float.MaxValue), hi = new(float.MinValue);
        foreach (var (f, c) in Corners(m, faces)) { var p = Uv(m.Faces[f], c); lo = Vector2.Min(lo, p); hi = Vector2.Max(hi, p); }
        return lo.X > hi.X ? (default, default) : (lo, hi);
    }

    public static Vector2 Centre(Prm m, IEnumerable<int> faces) { var (lo, hi) = Bounds(m, faces); return (lo + hi) / 2; }

    /// <summary>Each corner of the faces through `map`. Shared vertices of other faces stay where they are: the texture is per face corner.</summary>
    public static void Map(Prm m, IEnumerable<int> faces, Func<Vector2, Vector2> map)
    {
        foreach (var (f, c) in Corners(m, faces.Distinct().ToList())) SetUv(m.Faces[f], c, map(Uv(m.Faces[f], c)));
    }

    public static void Move(Prm m, IEnumerable<int> faces, Vector2 by) => Map(m, faces, p => p + by);

    public static void Scale(Prm m, IEnumerable<int> faces, Vector2 by, Vector2? about = null)
    {
        var list = faces.ToList();
        var o = about ?? Centre(m, list);
        Map(m, list, p => o + (p - o) * by);
    }

    /// <summary>Turned by `degrees` about `about` (default the middle), in the page's pixels so a page wider than high stays true.</summary>
    public static void Rotate(Prm m, IEnumerable<int> faces, float degrees, Vector2? about = null, float aspect = 1)
    {
        var list = faces.ToList();
        var o = about ?? Centre(m, list);
        float a = degrees * MathF.PI / 180, cs = MathF.Cos(a), sn = MathF.Sin(a);
        Map(m, list, p =>
        {
            var d = (p - o) * new Vector2(aspect, 1);
            return o + new Vector2((d.X * cs - d.Y * sn) / aspect, d.X * sn + d.Y * cs);
        });
    }

    public static void Flip(Prm m, IEnumerable<int> faces, bool across)
    {
        var list = faces.ToList();
        var o = Centre(m, list);
        Map(m, list, p => across ? new Vector2(2 * o.X - p.X, p.Y) : new Vector2(p.X, 2 * o.Y - p.Y));
    }

    /// <summary>Every corner onto the nearest pixel corner of a w x h page: crisp edges in the game.</summary>
    public static void Snap(Prm m, IEnumerable<int> faces, int w, int h) =>
        Map(m, faces, p => new Vector2(MathF.Round(p.X * w) / w, MathF.Round(p.Y * h) / h));

    /// <summary>Puts the faces into the box `to` (0..1 page), as large as fits, keeping their shape.</summary>
    public static void Fit(Prm m, IEnumerable<int> faces, Vector2 toMin, Vector2 toMax, bool keepShape = true)
    {
        var list = faces.ToList();
        var (lo, hi) = Bounds(m, list);
        var size = Vector2.Max(hi - lo, new Vector2(1e-6f));
        var room = toMax - toMin;
        var s = room / size;
        if (keepShape) s = new Vector2(MathF.Min(s.X, s.Y));
        var off = toMin + (room - size * s) / 2;
        Map(m, list, p => off + (p - lo) * s);
    }

    /// <summary>The axis a face looks along most: 0 X, 1 Y, 2 Z, the sign in the second value.</summary>
    public static (int Axis, bool Neg) Facing(Prm m, int face)
    {
        var f = m.Faces[face];
        var a = m.Verts[f.V0].Pos; var b = m.Verts[f.V1].Pos; var c = m.Verts[f.V2].Pos;
        var n = Vector3.Cross(b - a, c - a);
        if (f.IsQuad) n += Vector3.Cross(c - a, m.Verts[f.V3].Pos - a);
        var abs = Vector3.Abs(n);
        int axis = abs.X >= abs.Y && abs.X >= abs.Z ? 0 : abs.Y >= abs.Z ? 1 : 2;
        float v = axis == 0 ? n.X : axis == 1 ? n.Y : n.Z;
        return (axis, v < 0);
    }

    // Re-Volt: X right, Y down, Z forward. A side's picture as seen from it, top up
    static Vector2 Flat(Vector3 p, int axis, bool neg) => axis switch
    {
        0 => new Vector2(neg ? -p.Z : p.Z, p.Y),   // from the side
        1 => new Vector2(p.X, neg ? p.Z : -p.Z),   // from above / below
        _ => new Vector2(neg ? p.X : -p.X, p.Y),   // from the front / back
    };

    /// <summary>
    /// Projects the faces from one side (0 X: the side, 1 Y: the top, 2 Z: the front) as one flat picture, fitted into
    /// the box of the page they used to cover (the whole page if they covered nothing).
    /// </summary>
    public static void Project(Prm m, IEnumerable<int> faces, int axis, bool neg = false)
    {
        var list = faces.Where(i => Textured(m.Faces[i])).ToList();
        if (list.Count == 0) return;
        var (lo, hi) = Bounds(m, list);
        if ((hi - lo).X < 1e-3f || (hi - lo).Y < 1e-3f) (lo, hi) = (Vector2.Zero, Vector2.One);
        foreach (var (f, c) in Corners(m, list)) SetUv(m.Faces[f], c, Flat(m.Verts[m.Faces[f].Index(c)].Pos, axis, neg));
        Fit(m, list, lo, hi);
    }

    /// <summary>
    /// Unwrap by sides: the faces split by the way they face (6 sides), each side projected flat and split into joined
    /// pieces, then all pieces packed into the page at one scale. Returns how many pieces.
    /// </summary>
    public static int Unwrap(Prm m, IEnumerable<int> faces, float margin = 0.01f)
    {
        var list = faces.Where(i => Textured(m.Faces[i])).ToList();
        if (list.Count == 0) return 0;
        var pieces = new List<List<int>>();
        foreach (var side in list.GroupBy(i => Facing(m, i)))
        {
            foreach (var f in side) for (int c = 0; c < m.Faces[f].Corners; c++)
                SetUv(m.Faces[f], c, Flat(m.Verts[m.Faces[f].Index(c)].Pos, side.Key.Axis, side.Key.Neg) / 1000f);
            // Joined by shared vertices within the side
            pieces.AddRange(Islands(m, side));
        }
        Pack(m, pieces, margin);
        return pieces.Count;
    }

    /// <summary>
    /// Lays the islands out in rows, tallest first, all at the same scale, filling the page (0..1) as much as fits.
    /// Islands keep their size relative to each other, so the texture has the same detail everywhere.
    /// </summary>
    public static void Pack(Prm m, List<List<int>> islands, float margin = 0.01f)
    {
        if (islands.Count == 0) return;
        var boxes = islands.Select(i => (Faces: i, B: Bounds(m, i))).Select(x => (x.Faces, x.B.Min, Size: Vector2.Max(x.B.Max - x.B.Min, new Vector2(1e-5f)))).ToList();
        boxes.Sort((a, b) => b.Size.Y.CompareTo(a.Size.Y));
        // Find the largest scale at which the rows fit in the page
        float area = boxes.Sum(b => b.Size.X * b.Size.Y);
        float scale = MathF.Sqrt(0.7f / MathF.Max(area, 1e-9f));
        List<Vector2>? spots = null;
        for (int tries = 0; tries < 60; tries++)
        {
            spots = Shelf(boxes.Select(b => b.Size * scale).ToList(), margin);
            if (spots != null) break;
            scale *= 0.94f;
        }
        if (spots == null) return;
        // As big as possible: grow while it still fits
        for (int tries = 0; tries < 30; tries++)
        {
            var bigger = Shelf(boxes.Select(b => b.Size * scale * 1.03f).ToList(), margin);
            if (bigger == null) break;
            scale *= 1.03f; spots = bigger;
        }
        for (int k = 0; k < boxes.Count; k++)
        {
            var (f, min, _) = boxes[k];
            var at = spots[k];
            Map(m, f, p => at + (p - min) * scale);
        }
    }

    static List<Vector2>? Shelf(List<Vector2> sizes, float margin)
    {
        var r = new List<Vector2>();
        float x = margin, y = margin, row = 0;
        foreach (var s in sizes)
        {
            if (s.X + 2 * margin > 1) return null;
            if (x + s.X + margin > 1) { x = margin; y += row + margin; row = 0; }
            r.Add(new Vector2(x, y));
            x += s.X + margin;
            row = MathF.Max(row, s.Y);
        }
        return y + row + margin <= 1 ? r : null;
    }

    /// <summary>The part of the page the faces cover, 0 to 1 (overlaps counted once, measured on a 256 grid).</summary>
    public static float Coverage(Prm m, IEnumerable<int> faces)
    {
        using var b = new SKBitmap(256, 256, SKColorType.Gray8, SKAlphaType.Opaque);
        using (var c = new SKCanvas(b))
        {
            c.Clear(SKColors.Black);
            using var paint = new SKPaint { Color = SKColors.White, IsAntialias = false };
            c.DrawPath(Outline(m, faces, 256, 256), paint);
        }
        int n = 0;
        var px = b.GetPixelSpan();
        foreach (var v in px) if (v > 127) n++;
        return n / (256f * 256f);
    }

    /// <summary>The faces' outlines in a w x h page's pixels, for masks and drawing.</summary>
    public static SKPath Outline(Prm m, IEnumerable<int> faces, float w, float h)
    {
        var path = new SKPath { FillType = SKPathFillType.Winding };
        foreach (var i in faces)
        {
            var f = m.Faces[i];
            if (!Textured(f)) continue;
            // A face's corners can wind either way: add each so both fill
            var pts = Enumerable.Range(0, f.Corners).Select(c => new SKPoint(f.U[c] * w, f.V[c] * h)).ToArray();
            float area = 0;
            for (int k = 0; k < pts.Length; k++) { var a = pts[k]; var b = pts[(k + 1) % pts.Length]; area += a.X * b.Y - b.X * a.Y; }
            if (area < 0) Array.Reverse(pts);
            path.AddPoly(pts, true);
        }
        return path;
    }

    /// <summary>
    /// A layout picture for painting elsewhere: the page (or the given picture under it) with the faces' edges drawn on.
    /// </summary>
    public static SKBitmap Template(Prm m, IEnumerable<int> faces, int w, int h, SKBitmap? under = null, SKColor? line = null)
    {
        var b = new SKBitmap(w, h, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var c = new SKCanvas(b);
        c.Clear(new SKColor(40, 40, 46));
        if (under != null) c.DrawBitmap(under, new SKRect(0, 0, w, h));
        using var p = new SKPaint { Color = line ?? new SKColor(255, 230, 90), IsStroke = true, StrokeWidth = MathF.Max(1, w / 512f), IsAntialias = true };
        c.DrawPath(Outline(m, faces, w, h), p);
        return b;
    }
}
