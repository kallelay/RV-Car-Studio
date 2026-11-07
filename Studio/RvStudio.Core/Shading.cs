using System.Numerics;

namespace RvStudio.Core;

/// <summary>How shading reads the surface of a model</summary>
public enum ShadeSmooth
{
    /// <summary>The normals stored in the model</summary>
    ModelNormals,
    /// <summary>Averaged at each point: no visible edges</summary>
    Smooth,
    /// <summary>Averaged only across edges below an angle: rounded surfaces, sharp edges</summary>
    ByAngle,
    /// <summary>One light per polygon: the faceted look</summary>
    Faces
}

public sealed record ShadeOptions(
    float Ambient = 0.45f, float Sun = 0.55f, float Occlusion = 1f, Vector3? SunDir = null, int Rays = 48,
    ShadeSmooth Smooth = ShadeSmooth.ModelNormals, float Angle = 45,
    Vector3? SkyColour = null, Vector3? SunColour = null, Vector3? BounceColour = null, float Bounce = 0,
    float Contrast = 1, float SunSoft = 0);

/// <summary>Triangles that can block light: one model, or the whole car standing together</summary>
public sealed class ShadeScene
{
    readonly List<(Vector3 A, Vector3 E1, Vector3 E2)> tris = new();
    public float Size { get; private set; }

    public ShadeScene(IEnumerable<(Prm Model, Matrix4x4 World)> parts)
    {
        var min = new Vector3(float.MaxValue);
        var max = new Vector3(float.MinValue);
        foreach (var (m, world) in parts)
            foreach (var f in m.Faces)
            {
                if ((f.Flags & FaceFlags.Translucent) != 0) continue;
                for (int t = 0; t < (f.IsQuad ? 2 : 1); t++)
                {
                    int ia = f.V0, ib = t == 0 ? f.V1 : f.V2, ic = t == 0 ? f.V2 : f.V3;
                    if (ia < 0 || ib < 0 || ic < 0 || ia >= m.Verts.Length || ib >= m.Verts.Length || ic >= m.Verts.Length) continue;
                    var a = Vector3.Transform(m.Verts[ia].Pos, world);
                    var b = Vector3.Transform(m.Verts[ib].Pos, world);
                    var c = Vector3.Transform(m.Verts[ic].Pos, world);
                    tris.Add((a, b - a, c - a));
                    min = Vector3.Min(min, Vector3.Min(a, Vector3.Min(b, c)));
                    max = Vector3.Max(max, Vector3.Max(a, Vector3.Max(b, c)));
                }
            }
        Size = tris.Count > 0 ? (max - min).Length() : 1;
    }

    public bool Hits(Vector3 o, Vector3 d, float maxDist)
    {
        foreach (var (a, e1, e2) in tris)
        {
            var p = Vector3.Cross(d, e2);
            var det = Vector3.Dot(e1, p);
            if (Math.Abs(det) < 1e-7f) continue;
            var inv = 1 / det;
            var s = o - a;
            var u = Vector3.Dot(s, p) * inv;
            if (u < 0 || u > 1) continue;
            var q = Vector3.Cross(s, e1);
            var v = Vector3.Dot(d, q) * inv;
            if (v < 0 || u + v > 1) continue;
            var dist = Vector3.Dot(e2, q) * inv;
            if (dist > 0.05f && dist < maxDist) return true;
        }
        return false;
    }
}

public static class Shader
{
    /// <summary>
    /// Bake light into the vertex colours, which each polygon stores per corner: that's how a model can look faceted
    /// or smooth whatever its normals say. Keeps each polygon's alpha. Y goes down, so the sky is at -Y.
    /// A scene lets the rest of the car block the light, with the model standing where World puts it.
    /// </summary>
    public static void Shade(Prm m, ShadeOptions? options = null, bool withLods = true, ShadeScene? scene = null, Matrix4x4? world = null)
    {
        var o = options ?? new ShadeOptions();
        One(m, o, scene, world ?? Matrix4x4.Identity);
        if (withLods) m.ForOtherLods(l => One(l, o, scene, world ?? Matrix4x4.Identity));
    }

    static Vector3[] Directions(int count)
    {
        // Fibonacci sphere: even directions, no randomness, same result each time
        var dirs = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float y = 1 - (i + 0.5f) * 2f / count, r = MathF.Sqrt(Math.Max(0, 1 - y * y)), a = i * 2.3999632f;
            dirs[i] = new Vector3(MathF.Cos(a) * r, y, MathF.Sin(a) * r);
        }
        return dirs;
    }

    static void One(Prm m, ShadeOptions o, ShadeScene? given, Matrix4x4 world)
    {
        var scene = given ?? new ShadeScene(new[] { (m, world) });
        var sun = Vector3.Normalize(o.SunDir ?? new Vector3(0.35f, -1f, 0.25f));
        var sunRays = SunRays(sun, o.SunSoft);
        var dirs = Directions(Math.Max(o.Rays, 4));
        var sky = o.SkyColour ?? Vector3.One;
        var sunCol = o.SunColour ?? Vector3.One;
        var bounce = o.BounceColour ?? Vector3.One;

        var normals = CornerNormals(m, o.Smooth, o.Angle);

        // Corners at one point with one normal get one answer: fewer rays
        var samples = new List<(Vector3 Pos, Vector3 Normal)>();
        var known = new Dictionary<(int, int, int, int, int, int), int>();
        var use = new int[m.Faces.Length][];
        for (int fi = 0; fi < m.Faces.Length; fi++)
        {
            var f = m.Faces[fi];
            use[fi] = new int[f.Corners];
            var centre = Vector3.Zero;
            int valid = 0;
            for (int k = 0; k < f.Corners; k++)
                if (f.Index(k) is var i && i >= 0 && i < m.Verts.Length) { centre += m.Verts[i].Pos; valid++; }
            if (valid > 0) centre /= valid;
            for (int k = 0; k < f.Corners; k++)
            {
                var i = f.Index(k);
                if (i < 0 || i >= m.Verts.Length) { use[fi][k] = -1; continue; }
                var p = o.Smooth == ShadeSmooth.Faces ? centre : m.Verts[i].Pos;
                var n = normals[fi][k];
                var key = (Q(p.X, 100), Q(p.Y, 100), Q(p.Z, 100), Q(n.X, 200), Q(n.Y, 200), Q(n.Z, 200));
                if (!known.TryGetValue(key, out var at))
                {
                    at = samples.Count;
                    known[key] = at;
                    samples.Add((p, n));
                }
                use[fi][k] = at;
            }
        }

        var results = new Vector3[samples.Count];
        Parallel.For(0, samples.Count, s =>
        {
            var (p, n0) = samples[s];
            var n = Vector3.TransformNormal(n0, world);
            n = n.LengthSquared() < 1e-6f ? -Vector3.UnitY : Vector3.Normalize(n);
            var start = Vector3.Transform(p, world) + n * 0.15f;
            float open = 0, total = 0, ground = 0, groundAll = 0;
            foreach (var d in dirs)
            {
                var c = Vector3.Dot(d, n);
                if (c <= 0) continue;
                // ground hides what comes from below
                var skyWeight = 0.35f + 0.65f * Math.Max(0, -d.Y);
                total += c * skyWeight;
                bool free = !scene.Hits(start, d, scene.Size);
                if (free) open += c * skyWeight;
                if (d.Y > 0)
                {
                    groundAll += c;
                    if (free) ground += c;
                }
            }
            float occl = total > 0 ? open / total : 1;
            occl = 1 - (1 - occl) * o.Occlusion;
            float lit = Math.Max(0, Vector3.Dot(n, sun));
            if (lit > 0)
            {
                int free = 0;
                foreach (var r in sunRays) if (!scene.Hits(start, r, scene.Size)) free++;
                lit *= 0.15f + 0.85f * free / sunRays.Length;
            }
            // Light coming back up from the floor reaches what faces down
            float up = groundAll > 0 ? ground / groundAll * Math.Max(0, n.Y) : 0;
            var col = sky * (o.Ambient * occl * 1.4f) + sunCol * (o.Sun * lit) + bounce * (o.Bounce * up);
            if (o.Contrast != 1) col = (col - new Vector3(0.5f)) * o.Contrast + new Vector3(0.5f);
            results[s] = Vector3.Clamp(col, new Vector3(0.04f), Vector3.One);
        });

        for (int fi = 0; fi < m.Faces.Length; fi++)
        {
            var f = m.Faces[fi];
            for (int k = 0; k < f.Corners; k++)
            {
                if (use[fi][k] < 0) continue;
                var c = results[use[fi][k]];
                f.Colors[k] = (f.Colors[k] & 0xFF000000) | ((uint)(c.X * 255) << 16) | ((uint)(c.Y * 255) << 8) | (uint)(c.Z * 255);
            }
        }
    }

    static int Q(float v, float scale) => (int)MathF.Round(v * scale);

    // One ray, or a small cone of them for a soft edge
    static Vector3[] SunRays(Vector3 sun, float soft)
    {
        if (soft <= 0) return new[] { sun };
        var side = Vector3.Normalize(Vector3.Cross(sun, Math.Abs(sun.Z) < 0.9f ? Vector3.UnitZ : Vector3.UnitX));
        var other = Vector3.Cross(sun, side);
        var r = new Vector3[9];
        r[0] = sun;
        for (int i = 0; i < 8; i++)
        {
            float a = i * MathF.Tau / 8, spread = soft * 0.3f * (i % 2 == 0 ? 1 : 0.55f);
            r[i + 1] = Vector3.Normalize(sun + (side * MathF.Cos(a) + other * MathF.Sin(a)) * spread);
        }
        return r;
    }

    /// <summary>The direction each polygon faces: which way round its corners run is read from the model's own normals</summary>
    static Vector3[] FaceNormals(Prm m, out float[] areas)
    {
        var faces = new Vector3[m.Faces.Length];
        areas = new float[m.Faces.Length];
        float agree = 0;
        for (int i = 0; i < faces.Length; i++)
        {
            var f = m.Faces[i];
            int a = f.V0, b = f.V1, c = f.V2;
            if (a < 0 || b < 0 || c < 0 || a >= m.Verts.Length || b >= m.Verts.Length || c >= m.Verts.Length) continue;
            var n = Vector3.Cross(m.Verts[b].Pos - m.Verts[a].Pos, m.Verts[c].Pos - m.Verts[a].Pos);
            float len = n.Length();
            if (len < 1e-6f) continue;
            faces[i] = n / len;
            areas[i] = len;
            agree += Vector3.Dot(faces[i], m.Verts[a].Normal + m.Verts[b].Normal + m.Verts[c].Normal);
        }
        if (agree < 0) for (int i = 0; i < faces.Length; i++) faces[i] = -faces[i];
        return faces;
    }

    /// <summary>A normal for each corner of each polygon</summary>
    public static Vector3[][] CornerNormals(Prm m, ShadeSmooth mode, float angle = 45)
    {
        var result = new Vector3[m.Faces.Length][];
        var faces = FaceNormals(m, out var areas);
        Dictionary<(int, int, int), List<int>>? atPoint = null;
        if (mode is ShadeSmooth.Smooth or ShadeSmooth.ByAngle)
        {
            atPoint = new();
            for (int fi = 0; fi < m.Faces.Length; fi++)
            {
                var f = m.Faces[fi];
                for (int k = 0; k < f.Corners; k++)
                {
                    var i = f.Index(k);
                    if (i < 0 || i >= m.Verts.Length) continue;
                    var p = m.Verts[i].Pos;
                    var key = (Q(p.X, 100), Q(p.Y, 100), Q(p.Z, 100));
                    if (!atPoint.TryGetValue(key, out var list)) atPoint[key] = list = new();
                    list.Add(fi);
                }
            }
        }
        float limit = MathF.Cos(angle * MathF.PI / 180);
        for (int fi = 0; fi < m.Faces.Length; fi++)
        {
            var f = m.Faces[fi];
            result[fi] = new Vector3[f.Corners];
            for (int k = 0; k < f.Corners; k++)
            {
                var i = f.Index(k);
                if (i < 0 || i >= m.Verts.Length) continue;
                Vector3 n;
                switch (mode)
                {
                    case ShadeSmooth.Faces:
                        n = faces[fi];
                        break;
                    case ShadeSmooth.ModelNormals:
                        n = m.Verts[i].Normal;
                        break;
                    default:
                        var p = m.Verts[i].Pos;
                        n = Vector3.Zero;
                        foreach (var other in atPoint![(Q(p.X, 100), Q(p.Y, 100), Q(p.Z, 100))].Distinct())
                            if (mode == ShadeSmooth.Smooth || Vector3.Dot(faces[fi], faces[other]) >= limit)
                                n += faces[other] * areas[other];
                        break;
                }
                result[fi][k] = n.LengthSquared() < 1e-8f ? faces[fi] : Vector3.Normalize(n);
            }
        }
        return result;
    }

    /// <summary>
    /// Rewrite the normals, which give the game its reflections and moving lights. A vertex holds only one normal,
    /// so a sharp edge has to be two vertices at one place: models get more vertices with Faces and ByAngle.
    /// False if it would need more vertices than the format holds.
    /// </summary>
    public static bool SetNormals(Prm m, ShadeSmooth mode, float angle = 45, bool withLods = true)
    {
        if (!One(m, mode, angle)) return false;
        if (withLods) m.ForOtherLods(l => One(l, mode, angle));
        return true;
    }

    static bool One(Prm m, ShadeSmooth mode, float angle)
    {
        if (mode == ShadeSmooth.ModelNormals) return true;
        var normals = CornerNormals(m, mode, angle);
        var verts = new List<PrmVertex>(m.Verts.Length);
        var made = new Dictionary<(int, int, int, int), short>();
        var indices = new short[m.Faces.Length][];
        for (int fi = 0; fi < m.Faces.Length; fi++)
        {
            var f = m.Faces[fi];
            indices[fi] = new short[4];
            for (int k = 0; k < 4; k++) indices[fi][k] = f.Index(k);
            for (int k = 0; k < f.Corners; k++)
            {
                var i = f.Index(k);
                if (i < 0 || i >= m.Verts.Length) continue;
                var n = normals[fi][k];
                var key = (i, Q(n.X, 500), Q(n.Y, 500), Q(n.Z, 500));
                if (!made.TryGetValue(key, out var at))
                {
                    if (verts.Count >= short.MaxValue) return false;
                    at = (short)verts.Count;
                    made[key] = at;
                    verts.Add(new PrmVertex { Pos = m.Verts[i].Pos, Normal = n });
                }
                indices[fi][k] = at;
            }
        }
        m.Verts = verts.ToArray();
        for (int fi = 0; fi < m.Faces.Length; fi++)
        {
            var f = m.Faces[fi];
            f.V0 = indices[fi][0]; f.V1 = indices[fi][1]; f.V2 = indices[fi][2]; f.V3 = indices[fi][3];
        }
        return true;
    }
}
