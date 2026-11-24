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

/// <summary>
/// How the car is lit when shading is baked, as a clay render: a soft sky all round, a floor under the car (dark
/// underneath, shadows where it touches), a soft key light from above, and a little light back from the floor.
/// Ambient is the sky, Sun the key light, Occlusion how much the car's own shape darkens what it hides (Depth in the
/// panel). The colours tint them; Contrast is applied last.
/// </summary>
public sealed record ShadeOptions(
    float Ambient = 0.45f, float Sun = 0.55f, float Occlusion = 1f, Vector3? SunDir = null, int Rays = 128,
    ShadeSmooth Smooth = ShadeSmooth.ByAngle, float Angle = 45,
    Vector3? SkyColour = null, Vector3? SunColour = null, Vector3? BounceColour = null, float Bounce = 0.25f,
    float Contrast = 1, float SunSoft = 0.6f, bool Floor = true);

/// <summary>Triangles that can block light: one model, or the whole car standing together. Kept in a tree of boxes so a
/// ray only meets the triangles near it.</summary>
public sealed class ShadeScene
{
    readonly List<(Vector3 A, Vector3 E1, Vector3 E2)> tris = new();
    public float Size { get; private set; }
    /// <summary>The floor's height: the lowest point of the car (Y goes down).</summary>
    public float FloorY { get; private set; }
    public int Triangles => tris.Count;

    // The tree: nodes with a box; a leaf holds tris[First..First+Count)
    struct Node { public Vector3 Min, Max; public int Left, Right, First, Count; }
    Node[] nodes = Array.Empty<Node>();
    int[] order = Array.Empty<int>();

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
        FloorY = tris.Count > 0 ? max.Y : 0;
        Build();
    }

    void Build()
    {
        int n = tris.Count;
        order = Enumerable.Range(0, n).ToArray();
        var centres = tris.Select(t => t.A + (t.E1 + t.E2) / 3).ToArray();
        var list = new List<Node>(Math.Max(1, n / 2));
        int Make(int first, int count)
        {
            var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
            for (int i = first; i < first + count; i++)
            {
                var (a, e1, e2) = tris[order[i]];
                lo = Vector3.Min(lo, Vector3.Min(a, Vector3.Min(a + e1, a + e2)));
                hi = Vector3.Max(hi, Vector3.Max(a, Vector3.Max(a + e1, a + e2)));
            }
            int at = list.Count;
            list.Add(new Node { Min = lo, Max = hi, First = first, Count = count, Left = -1, Right = -1 });
            if (count <= 4) return at;
            var size = hi - lo;
            int axis = size.X >= size.Y && size.X >= size.Z ? 0 : size.Y >= size.Z ? 1 : 2;
            float Key(int t) => axis == 0 ? centres[t].X : axis == 1 ? centres[t].Y : centres[t].Z;
            Array.Sort(order, first, count, Comparer<int>.Create((x, y) => Key(x).CompareTo(Key(y))));
            int half = count / 2;
            int l = Make(first, half), r = Make(first + half, count - half);
            var node = list[at]; node.Left = l; node.Right = r; node.Count = 0; list[at] = node;
            return at;
        }
        if (n > 0) Make(0, n);
        nodes = list.ToArray();
    }

    static bool BoxHit(in Node b, Vector3 o, Vector3 inv, float maxDist)
    {
        var t1 = (b.Min - o) * inv; var t2 = (b.Max - o) * inv;
        var tmin = Vector3.Min(t1, t2); var tmax = Vector3.Max(t1, t2);
        float near = MathF.Max(MathF.Max(tmin.X, tmin.Y), MathF.Max(tmin.Z, 0));
        float far = MathF.Min(MathF.Min(tmax.X, tmax.Y), MathF.Min(tmax.Z, maxDist));
        return near <= far;
    }

    /// <summary>Is anything of the car in the way along d from o, within maxDist?</summary>
    public bool Hits(Vector3 o, Vector3 d, float maxDist)
    {
        if (nodes.Length == 0) return false;
        var inv = new Vector3(1 / (MathF.Abs(d.X) < 1e-9f ? 1e-9f : d.X), 1 / (MathF.Abs(d.Y) < 1e-9f ? 1e-9f : d.Y), 1 / (MathF.Abs(d.Z) < 1e-9f ? 1e-9f : d.Z));
        Span<int> stack = stackalloc int[64];
        int sp = 0;
        stack[sp++] = 0;
        while (sp > 0)
        {
            ref readonly var nd = ref nodes[stack[--sp]];
            if (!BoxHit(nd, o, inv, maxDist)) continue;
            if (nd.Left >= 0)
            {
                if (sp < 62) { stack[sp++] = nd.Left; stack[sp++] = nd.Right; }
                continue;
            }
            for (int i = nd.First; i < nd.First + nd.Count; i++)
            {
                var (a, e1, e2) = tris[order[i]];
                var p = Vector3.Cross(d, e2);
                var det = Vector3.Dot(e1, p);
                if (Math.Abs(det) < 1e-9f) continue;
                var invDet = 1 / det;
                var s = o - a;
                var u = Vector3.Dot(s, p) * invDet;
                if (u < 0 || u > 1) continue;
                var q = Vector3.Cross(s, e1);
                var v = Vector3.Dot(d, q) * invDet;
                if (v < 0 || u + v > 1) continue;
                var dist = Vector3.Dot(e2, q) * invDet;
                if (dist > 0 && dist < maxDist) return true;
            }
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

    /// <summary>Directions over the half sphere round +Z, more of them near the top (cosine weighted): their plain average
    /// is the light a surface gets. Even, not random: the same result each time, no speckle.</summary>
    static Vector3[] Hemisphere(int count)
    {
        var dirs = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            float r = MathF.Sqrt((i + 0.5f) / count), a = i * 2.3999632f;
            dirs[i] = new Vector3(MathF.Cos(a) * r, MathF.Sin(a) * r, MathF.Sqrt(MathF.Max(0, 1 - r * r)));
        }
        return dirs;
    }

    static void Basis(Vector3 n, out Vector3 t, out Vector3 b)
    {
        t = Vector3.Normalize(Vector3.Cross(n, MathF.Abs(n.Y) < 0.9f ? Vector3.UnitY : Vector3.UnitX));
        b = Vector3.Cross(n, t);
    }

    static void One(Prm m, ShadeOptions o, ShadeScene? given, Matrix4x4 world)
    {
        var scene = given ?? new ShadeScene(new[] { (m, world) });
        var key = Vector3.Normalize(o.SunDir ?? new Vector3(0.45f, -1f, 0.55f));
        var keyRays = KeyRays(key, o.SunSoft);
        var hemi = Hemisphere(Math.Max(o.Rays, 8));
        var sky = o.SkyColour ?? Vector3.One;
        var keyCol = o.SunColour ?? Vector3.One;
        var bounce = o.BounceColour ?? Vector3.One;
        float eps = MathF.Max(scene.Size * 0.0015f, 0.02f), reach = scene.Size;
        // The floor: just under the lowest point of the car standing as it is
        float floorY = scene.FloorY + eps;

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
                var qk = (Q(p.X, 100), Q(p.Y, 100), Q(p.Z, 100), Q(n.X, 200), Q(n.Y, 200), Q(n.Z, 200));
                if (!known.TryGetValue(qk, out var at))
                {
                    at = samples.Count;
                    known[qk] = at;
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
            var wp = Vector3.Transform(p, world);
            var start = wp + n * eps;
            Basis(n, out var t, out var b);
            // Sky and floor, as seen from here: what's open gets the sky (brighter overhead), what reaches the floor gets
            // its bounce, what the car hides gets nothing
            float skyOpen = 0, skyAll = 0, floorSeen = 0;
            foreach (var h in hemi)
            {
                var d = t * h.X + b * h.Y + n * h.Z;
                bool down = d.Y > 0;
                float overhead = 0.75f + 0.25f * MathF.Max(0, -d.Y);
                // Unhidden, a direction gets the sky, or the floor's light if it looks down onto the floor
                float plain = down && o.Floor ? o.Bounce * 0.5f : overhead;
                skyAll += plain;
                float distToFloor = down && o.Floor ? (floorY - start.Y) / d.Y : reach;
                if (distToFloor < 0) distToFloor = reach;
                bool hidden = scene.Hits(start, d, MathF.Min(reach, distToFloor));
                if (hidden) continue;
                if (down && o.Floor) floorSeen += o.Bounce * 0.5f;
                else skyOpen += overhead;
            }
            int count = hemi.Length;
            float skyLight = skyOpen / count, floorLight = floorSeen / count, plainLight = skyAll / count;
            // Depth: how much the car's own shape darkens; 0 leaves the plain sky and floor light
            float depth = Math.Clamp(o.Occlusion, 0, 1);
            float ambient = plainLight + (skyLight + floorLight - plainLight) * depth;
            // The key light, soft: the share of a small disc of light that gets through
            float lit = MathF.Max(0, Vector3.Dot(n, key));
            if (lit > 0)
            {
                int free = 0;
                foreach (var r in keyRays)
                {
                    // Towards the light; one going down meets the floor first
                    float toFloor = o.Floor && r.Y > 0 ? (floorY - start.Y) / r.Y : reach;
                    if (!scene.Hits(start, r, MathF.Min(reach, toFloor < 0 ? reach : toFloor))) free++;
                }
                float shade = free / (float)keyRays.Length;
                lit *= 1 - (1 - shade) * (0.35f + 0.65f * depth);
            }
            var col = sky * (o.Ambient * 1.45f * (ambient - floorLight * depth)) + bounce * (o.Ambient * 1.45f * floorLight * depth) + keyCol * (o.Sun * 0.8f * lit);
            col += new Vector3(0.06f);
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

    // The key light as a small soft disc: rays towards points across it (key is the way to the light, up is -Y)
    static Vector3[] KeyRays(Vector3 key, float soft)
    {
        if (soft <= 0) return new[] { key };
        Basis(key, out var side, out var other);
        var r = new Vector3[16];
        r[0] = key;
        for (int i = 1; i < 16; i++)
        {
            float rad = MathF.Sqrt(i / 15f) * soft * 0.35f, a = i * 2.3999632f;
            r[i] = Vector3.Normalize(key + (side * MathF.Cos(a) + other * MathF.Sin(a)) * rad);
        }
        return r;
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
