using System.Numerics;

namespace RvStudio.Core;

/// <summary>
/// The collision hull's spheres, and its convex parts, from the body's shape.
///
/// In the game the spheres are what touches the world, and they also touch the other car's convex parts: together they
/// should have the body's outline. They don't need to be inside it: the stock cars have 10 to 34 of them, of 7 to 15
/// units, that stick a few units out here and there and leave gaps of 3 to 5 at most between them and the body. So
/// they're fitted, not packed: the spheres that leave the least of the body uncovered while sticking out the least, and
/// whose outline in every direction (front, sides, roof, corners) ends where the body's does.
/// </summary>
public static class HullMaker
{
    /// <summary>How well a set of spheres stands for the body, in units (a car is about 60 x 30 x 130)</summary>
    /// <param name="Gap">How far the body's surface is from being covered, on average</param>
    /// <param name="Gap95">and for all but the worst twentieth of it</param>
    /// <param name="GapMax">and at worst</param>
    /// <param name="OutMax">How far a sphere sticks out of the body at most</param>
    /// <param name="Outline">How far the spheres' outline is from the body's, on average over all directions (root mean square)</param>
    public sealed record Quality(int Spheres, float Gap, float Gap95, float GapMax, float OutMax, float Outline);

    public enum Detail { Few, Normal, Fine }
    public static readonly string[] DetailNames = { "Few spheres (8 to 12)", "Normal (up to 22)", "Fine (up to 34)" };

    static int MaxOf(Detail d) => d switch { Detail.Few => 12, Detail.Normal => 22, _ => 34 };

    // ---------------------------- the body as a solid ----------------------------

    /// <summary>The body in little cubes: what's inside it, and how far every place is from its surface</summary>
    sealed class Solid
    {
        public Vector3 Origin;
        public float H;
        public int Nx, Ny, Nz;
        public bool[] In = Array.Empty<bool>();
        public float[] Sd = Array.Empty<float>(); // below 0 inside
        public bool Symmetric;
        public Vector3[] Points = Array.Empty<Vector3>();   // on the surface
        public Vector3[] Normals = Array.Empty<Vector3>();
        public float Area;
        public float Bottom;        // no sphere reaches below this (Y goes down): the road clearance, or the body's underside

        public int At(int x, int y, int z) => (x * Ny + y) * Nz + z;
        public Vector3 Centre(int x, int y, int z) => Origin + new Vector3(x, y, z) * H;

        /// <summary>The distance to the surface, between the cubes too</summary>
        public float Distance(Vector3 p)
        {
            var q = (p - Origin) / H;
            float ox = Math.Clamp(q.X, 0, Nx - 1.001f), oy = Math.Clamp(q.Y, 0, Ny - 1.001f), oz = Math.Clamp(q.Z, 0, Nz - 1.001f);
            float beyond = new Vector3(q.X - ox, q.Y - oy, q.Z - oz).Length() * H;
            int x = (int)ox, y = (int)oy, z = (int)oz;
            float fx = ox - x, fy = oy - y, fz = oz - z;
            float S(int a, int b, int c) => Sd[At(x + a, y + b, z + c)];
            float L(float a, float b, float t) => a + (b - a) * t;
            return L(L(L(S(0, 0, 0), S(0, 0, 1), fz), L(S(0, 1, 0), S(0, 1, 1), fz), fy),
                     L(L(S(1, 0, 0), S(1, 0, 1), fz), L(S(1, 1, 0), S(1, 1, 1), fz), fy), fx) + (beyond > 0.01f ? beyond : 0);
        }
    }

    static List<(Vector3 A, Vector3 B, Vector3 C)> Triangles(Prm body, Vector3 offset)
    {
        var tris = new List<(Vector3, Vector3, Vector3)>();
        foreach (var f in body.Faces)
        {
            Vector3 P(short i) => body.Verts[i].Pos + offset;
            if (f.V0 >= body.Verts.Length || f.V1 >= body.Verts.Length || f.V2 >= body.Verts.Length) continue;
            tris.Add((P(f.V0), P(f.V1), P(f.V2)));
            if (f.IsQuad && f.V3 < body.Verts.Length) tris.Add((P(f.V0), P(f.V2), P(f.V3)));
        }
        return tris;
    }

    /// <summary>
    /// Filled in from its polygons: a place is inside if the body is on both sides of it in each of the three directions.
    /// That needs no closed mesh (car bodies have no floor, open arches), and keeps the windscreen's slope and the gap
    /// under a spoiler.
    /// </summary>
    static Solid? Fill(Prm body, Vector3 offset, float? floorY)
    {
        var tris = Triangles(body, offset);
        if (tris.Count == 0) return null;
        var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
        foreach (var (a, b, c) in tris) { lo = Vector3.Min(lo, Vector3.Min(a, Vector3.Min(b, c))); hi = Vector3.Max(hi, Vector3.Max(a, Vector3.Max(b, c))); }
        // The road's clearance, as the convex part keeps it: what's lower is moved up to it, so a low nose or bumper keeps
        // its reach
        float bottom = floorY is float fl ? Math.Min(hi.Y, Math.Max(fl, lo.Y + 0.55f * (hi.Y - lo.Y))) : hi.Y;
        Vector3 Up(Vector3 v) => new(v.X, Math.Min(v.Y, bottom), v.Z);
        tris = tris.Select(t => (Up(t.A), Up(t.B), Up(t.C))).ToList();
        hi.Y = bottom;
        var size = hi - lo;
        float longest = Math.Max(size.X, Math.Max(size.Y, size.Z));
        if (longest < 1) return null;
        float h = Math.Clamp(longest / 80, 0.3f, 40f);
        const int pad = 3;
        // A layer of cubes' middles is on the car's middle plane, so left and right come out the same
        float side = Math.Max(Math.Abs(lo.X), Math.Abs(hi.X));
        bool centred = Math.Abs(lo.X + hi.X) < 0.08f * Math.Max(size.X, 1);
        var s = new Solid { H = h, Bottom = bottom };
        int half = (int)MathF.Ceiling(side / h) + pad;
        s.Nx = centred ? 2 * half + 1 : (int)MathF.Ceiling(size.X / h) + 1 + 2 * pad;
        s.Ny = (int)MathF.Ceiling(size.Y / h) + 1 + 2 * pad;
        s.Nz = (int)MathF.Ceiling(size.Z / h) + 1 + 2 * pad;
        s.Origin = new Vector3(centred ? -half * h : lo.X - pad * h, lo.Y - pad * h, lo.Z - pad * h);
        int n = s.Nx * s.Ny * s.Nz;
        if (n > 4_000_000) return null;
        var mark = new bool[n];
        foreach (var (a, b, c) in tris)
        {
            float ab = (b - a).Length(), ac = (c - a).Length(), bc = (c - b).Length();
            int steps = Math.Clamp((int)(Math.Max(ab, Math.Max(ac, bc)) / (0.6f * h)) + 1, 1, 400);
            for (int i = 0; i <= steps; i++)
                for (int j = 0; j <= steps - i; j++)
                {
                    var p = a + (b - a) * (i / (float)steps) + (c - a) * (j / (float)steps);
                    var q = (p - s.Origin) / h;
                    int x = (int)MathF.Round(q.X), y = (int)MathF.Round(q.Y), z = (int)MathF.Round(q.Z);
                    if (x < 0 || y < 0 || z < 0 || x >= s.Nx || y >= s.Ny || z >= s.Nz) continue;
                    mark[s.At(x, y, z)] = true;
                }
        }
        // Between the first and the last marked cube of each line, counted over the three directions
        var count = new byte[n];
        void Lines(int na, int nb, int nc, Func<int, int, int, int> at)
        {
            for (int a = 0; a < na; a++)
                for (int b = 0; b < nb; b++)
                {
                    int first = -1, last = -1;
                    for (int c = 0; c < nc; c++) if (mark[at(a, b, c)]) { if (first < 0) first = c; last = c; }
                    if (first < 0) continue;
                    for (int c = first; c <= last; c++) count[at(a, b, c)]++;
                }
        }
        Lines(s.Ny, s.Nz, s.Nx, (y, z, x) => s.At(x, y, z));
        Lines(s.Nx, s.Nz, s.Ny, (x, z, y) => s.At(x, y, z));
        Lines(s.Nx, s.Ny, s.Nz, (x, y, z) => s.At(x, y, z));
        s.In = new bool[n];
        for (int i = 0; i < n; i++) s.In[i] = mark[i] || count[i] == 3;
        // One side, the other, or both
        if (centred)
        {
            int same = 0, all = 0;
            for (int x = 0; x < half; x++)
                for (int y = 0; y < s.Ny; y++)
                    for (int z = 0; z < s.Nz; z++)
                    {
                        bool l = s.In[s.At(x, y, z)], r = s.In[s.At(s.Nx - 1 - x, y, z)];
                        if (l || r) { all++; if (l && r) same++; }
                    }
            s.Symmetric = all > 0 && same > 0.88f * all;
            if (s.Symmetric)
                for (int x = 0; x < half; x++)
                    for (int y = 0; y < s.Ny; y++)
                        for (int z = 0; z < s.Nz; z++)
                        {
                            int l = s.At(x, y, z), r = s.At(s.Nx - 1 - x, y, z);
                            s.In[l] = s.In[r] = s.In[l] || s.In[r];
                        }
        }
        Distances(s);
        Surface(s);
        return s.Points.Length < 20 ? null : s;
    }

    /// <summary>Distance to the surface everywhere: exact squared distances along each direction in turn. The surface is at the middle of the outer cubes</summary>
    static void Distances(Solid s)
    {
        float[] To(bool what)
        {
            const float far = 1e12f;
            var d = new float[s.In.Length];
            for (int i = 0; i < d.Length; i++) d[i] = s.In[i] == what ? 0 : far;
            int longest = Math.Max(s.Nx, Math.Max(s.Ny, s.Nz));
            var f = new float[longest]; var o = new float[longest]; var v = new int[longest]; var zz = new float[longest + 1];
            void Line(int start, int step, int n)
            {
                for (int i = 0; i < n; i++) f[i] = d[start + i * step];
                // The lower envelope of the parabolas of the line's values
                int k = 0;
                v[0] = 0; zz[0] = float.NegativeInfinity; zz[1] = float.PositiveInfinity;
                for (int q = 1; q < n; q++)
                {
                    float x;
                    do
                    {
                        int r = v[k];
                        x = (float)(((double)f[q] + q * q - (double)f[r] - r * r) / (2.0 * (q - r)));
                    } while (x <= zz[k] && --k >= 0);
                    k++;
                    v[k] = q; zz[k] = x; zz[k + 1] = float.PositiveInfinity;
                }
                k = 0;
                for (int q = 0; q < n; q++)
                {
                    while (zz[k + 1] < q) k++;
                    o[q] = (q - v[k]) * (q - v[k]) + f[v[k]];
                }
                for (int i = 0; i < n; i++) d[start + i * step] = o[i];
            }
            for (int x = 0; x < s.Nx; x++) for (int y = 0; y < s.Ny; y++) Line(s.At(x, y, 0), 1, s.Nz);
            for (int x = 0; x < s.Nx; x++) for (int z = 0; z < s.Nz; z++) Line(s.At(x, 0, z), s.Nz, s.Ny);
            for (int y = 0; y < s.Ny; y++) for (int z = 0; z < s.Nz; z++) Line(s.At(0, y, z), s.Ny * s.Nz, s.Nx);
            return d;
        }
        var toOut = To(false); // inside: how far to the first cube outside
        var toIn = To(true);
        s.Sd = new float[s.In.Length];
        for (int i = 0; i < s.Sd.Length; i++) s.Sd[i] = s.In[i] ? -(MathF.Sqrt(toOut[i]) - 1) * s.H : MathF.Sqrt(toIn[i]) * s.H;
    }

    static void Surface(Solid s)
    {
        var pts = new List<Vector3>(); var nrm = new List<Vector3>();
        for (int x = 1; x < s.Nx - 1; x++)
            for (int y = 1; y < s.Ny - 1; y++)
                for (int z = 1; z < s.Nz - 1; z++)
                {
                    if (!s.In[s.At(x, y, z)]) continue;
                    var n = Vector3.Zero;
                    if (!s.In[s.At(x - 1, y, z)]) n.X--;
                    if (!s.In[s.At(x + 1, y, z)]) n.X++;
                    if (!s.In[s.At(x, y - 1, z)]) n.Y--;
                    if (!s.In[s.At(x, y + 1, z)]) n.Y++;
                    if (!s.In[s.At(x, y, z - 1)]) n.Z--;
                    if (!s.In[s.At(x, y, z + 1)]) n.Z++;
                    bool edge = n != Vector3.Zero || !s.In[s.At(x - 1, y, z)] || !s.In[s.At(x, y - 1, z)] || !s.In[s.At(x, y, z - 1)];
                    if (!edge) continue;
                    pts.Add(s.Centre(x, y, z));
                    nrm.Add(n == Vector3.Zero ? Vector3.UnitY : Vector3.Normalize(n));
                }
        s.Points = pts.ToArray();
        s.Normals = nrm.ToArray();
        s.Area = pts.Count * s.H * s.H;
    }

    // ---------------------------- fitting the spheres ----------------------------

    static Vector3[] Around(int n)
    {
        var r = new Vector3[n];
        float g = MathF.PI * (3 - MathF.Sqrt(5));
        for (int i = 0; i < n; i++)
        {
            float y = 1 - 2 * (i + 0.5f) / n, q = MathF.Sqrt(1 - y * y);
            r[i] = new Vector3(MathF.Cos(g * i) * q, y, MathF.Sin(g * i) * q);
        }
        return r;
    }

    static readonly Vector3[] Ball = Around(64), Outlines = Around(90).Concat(new[]
    {
        Vector3.UnitX, -Vector3.UnitX, Vector3.UnitY, -Vector3.UnitY, Vector3.UnitZ, -Vector3.UnitZ
    }).ToArray();

    /// <summary>One sphere, or a pair left and right of the middle (X above 0)</summary>
    struct Piece
    {
        public Vector3 P; public float R; public bool Pair;
        public readonly IEnumerable<(Vector3 P, float R)> Spheres()
        {
            yield return (P, R);
            if (Pair) yield return (new Vector3(-P.X, P.Y, P.Z), R);
        }
        public readonly int Count => Pair ? 2 : 1;
        public readonly float Gap(Vector3 q)
        {
            float a = (q - P).Length() - R;
            if (!Pair) return a;
            float b = (q - new Vector3(-P.X, P.Y, P.Z)).Length() - R;
            return Math.Min(a, b);
        }
        public readonly float Reach(Vector3 dir)
        {
            float a = Vector3.Dot(P, dir) + R;
            return Pair ? Math.Max(a, a - 2 * P.X * dir.X) : a;
        }
    }

    const float Free = 0.8f;    // a gap or an overhang this small doesn't matter
    const float FarGap = 25f;

    /// <summary>The spheres for this body. Null if it has no shape to fill</summary>
    public static (List<(Vector3 Pos, float Radius)> Spheres, Quality Quality)? Spheres(Prm body, Vector3 offset, float? floorY, Detail detail = Detail.Normal)
    {
        if (Fill(body, offset, floorY) is not { } s) return null;
        float unit = Math.Clamp(Longest(s) / 125f, 0.2f, 20f); // a car's size against the stock cars'
        float free = Free * unit, minR = 2.2f * unit;
        int max = MaxOf(detail);

        // Places on the surface, the same ones every time, the same on both sides
        var rnd = new Random(7);
        var order = Enumerable.Range(0, s.Points.Length).Where(i => !s.Symmetric || s.Points[i].X >= -0.01f).OrderBy(_ => rnd.Next()).Take(700).ToArray();
        var pts = order.Select(i => s.Points[i]).ToArray();
        // Under the car the wheels meet the road first; the front, sides and roof take the knocks
        var weight = order.Select(i => s.Normals[i].Y > 0.7f ? 0.45f : s.Normals[i].Y < -0.7f ? 0.8f : 1f).ToArray();
        float each = s.Area / Math.Max(1, pts.Length) * (s.Symmetric ? 0.5f : 1);
        var reach = Outlines.Select(d => s.Points.Max(p => Vector3.Dot(p, d))).ToArray();

        float Bad(float gap) { float g = Math.Min(gap, FarGap * unit) - free; return g > 0 ? g * g : 0; }

        // What sticks out of the body: the sphere's own surface against the distances
        float Out(in Piece k)
        {
            float sum = 0;
            foreach (var d in Ball)
            {
                float o = s.Distance(k.P + d * k.R) - free;
                if (o > 0) sum += o * o;
            }
            float area = 4 * MathF.PI * k.R * k.R / Ball.Length / each;
            // Half of a pair counts: so do half of the places on the surface
            return 1.4f * sum * area * (s.Symmetric && !k.Pair ? 0.5f : 1);
        }

        Piece Make(Vector3 p, float r)
        {
            if (!s.Symmetric) return new Piece { P = p, R = r };
            if (Math.Abs(p.X) < 0.3f * r) return new Piece { P = new Vector3(0, p.Y, p.Z), R = r };
            return new Piece { P = new Vector3(Math.Abs(p.X), p.Y, p.Z), R = r, Pair = true };
        }

        // Where a sphere could go: inside, as big as fits, or a little more
        var spots = new List<(int I, Vector3 P, float D)>();
        for (int x = 0; x < s.Nx; x++)
            for (int y = 0; y < s.Ny; y++)
                for (int z = 0; z < s.Nz; z++)
                {
                    int i = s.At(x, y, z);
                    if (!s.In[i]) continue;
                    var p = s.Centre(x, y, z);
                    if (s.Symmetric && p.X < -0.01f) continue;
                    spots.Add((i, p, -s.Sd[i]));
                }
        var chosen = spots.OrderBy(_ => rnd.Next()).Take(1500).Concat(spots.OrderByDescending(q => q.D).Where((_, k) => k % Math.Max(1, spots.Count / 4 / 300) == 0).Take(300)).ToList();
        var cands = new List<(Piece K, float Out)>();
        foreach (var (_, p, dIn) in chosen)
            foreach (var more in new[] { 0.6f, 1.8f, 3.2f })
            {
                float r = dIn + more * unit;
                if (r < minR) continue;
                // Never lower than the road clearance: up to there instead
                var at = p;
                if (at.Y + r > s.Bottom) at.Y = s.Bottom - r;
                var k = Make(at, r);
                cands.Add((k, Out(k)));
            }
        if (cands.Count == 0) return null;

        // One after the other, each the one that helps the most for the spheres it costs
        var gap = new float[pts.Length];
        Array.Fill(gap, FarGap * unit * 2);
        float Cover() { float e = 0; for (int i = 0; i < pts.Length; i++) e += weight[i] * Bad(gap[i]); return e; }
        var pieces = new List<Piece>();
        int used = 0;
        float now = Cover(), start = now;
        var gains = new float[cands.Count];
        while (used < max)
        {
            int room = max - used;
            Parallel.For(0, cands.Count, c =>
            {
                var (k, o) = cands[c];
                if (k.Count > room) { gains[c] = float.MinValue; return; }
                float e = 0;
                for (int i = 0; i < pts.Length; i++) e += weight[i] * (Bad(gap[i]) - Bad(Math.Min(gap[i], k.Gap(pts[i]))));
                gains[c] = (e - o) / k.Count;
            });
            int best = Array.IndexOf(gains, gains.Max());
            float gain = gains[best] * cands[best].K.Count;
            // Enough: what one more would bring is little (never before the few that any car needs)
            if (used >= 8 && gain < (detail == Detail.Fine ? 0.0004f : detail == Detail.Few ? 0.002f : 0.0008f) * start * cands[best].K.Count) break;
            if (gain <= 0) break;
            var pick = cands[best].K;
            pieces.Add(pick);
            used += pick.Count;
            for (int i = 0; i < pts.Length; i++) gap[i] = Math.Min(gap[i], pick.Gap(pts[i]));
            now = Cover();
        }
        if (pieces.Count == 0) return null;
        Refine(pieces);

        // One that does nothing goes
        for (int j = pieces.Count - 1; j >= 0 && pieces.Count > 3; j--)
        {
            float with = Total(pieces);
            var without = pieces.Where((_, k) => k != j).ToList();
            if (Total(without) < with * 1.012f) pieces.RemoveAt(j);
        }
        Refine(pieces);

        var spheres = pieces.SelectMany(k => k.Spheres()).OrderBy(k => k.P.Z).ThenBy(k => k.P.X).Select(k => (k.P, k.R)).ToList();
        return (spheres, Measure(s, spheres));

        // ---- everything at once: covered, not sticking out, the same outline ----

        float OutlineError(IReadOnlyList<Piece> all)
        {
            float e = 0;
            for (int d = 0; d < Outlines.Length; d++)
            {
                float m = float.MinValue;
                foreach (var k in all) m = Math.Max(m, k.Reach(Outlines[d]));
                float x = m - reach[d];
                // Past the body counts more than short of it: that's what touches first
                e += x > 0 ? 1.5f * x * x : x * x;
            }
            return e * pts.Length / Outlines.Length * 0.9f;
        }

        float Total(IReadOnlyList<Piece> all)
        {
            float e = 0;
            for (int i = 0; i < pts.Length; i++)
            {
                float g = float.MaxValue;
                foreach (var k in all) g = Math.Min(g, k.Gap(pts[i]));
                e += weight[i] * Bad(g);
            }
            foreach (var k in all) e += Out(k);
            return e + OutlineError(all);
        }

        void Refine(List<Piece> all)
        {
            var rest = new float[pts.Length];
            float mid = pts.Average(p => p.Z);
            for (float step = 3.2f * unit; step > 0.15f * unit; step *= 0.6f)
                for (int round = 0; round < 3; round++)
                {
                    bool moved = false;
                    for (int j = 0; j < all.Count; j++)
                    {
                        for (int i = 0; i < pts.Length; i++)
                        {
                            float g = float.MaxValue;
                            for (int k = 0; k < all.Count; k++) if (k != j) g = Math.Min(g, all[k].Gap(pts[i]));
                            rest[i] = g;
                        }
                        float Score(in Piece k)
                        {
                            float e = 0;
                            for (int i = 0; i < pts.Length; i++) e += weight[i] * Bad(Math.Min(rest[i], k.Gap(pts[i])));
                            return e + Out(k);
                        }
                        var cur = all[j];
                        float best = Score(cur) + OutlineError(all);
                        for (int m = 0; m < 12; m++)
                        {
                            var t = cur;
                            float a = (m & 1) == 0 ? step : -step;
                            switch (m >> 1)
                            {
                                case 0: if (s.Symmetric && !t.Pair) continue; t.P.X += a; break;
                                case 1: t.P.Y += a; break;
                                case 2: t.P.Z += a; break;
                                case 3: t.R += a * 0.6f; break;
                                // Bigger or smaller with its end where it is, at the front or the back, at a side
                                case 4: t.R += a * 0.6f; t.P.Z -= a * 0.6f * (t.P.Z >= mid ? 1 : -1); break;
                                default: if (!t.Pair) continue; t.R += a * 0.6f; t.P.X -= a * 0.6f; break;
                            }
                            if (t.P.Y + t.R > s.Bottom) t.P.Y = s.Bottom - t.R; // up to the road clearance
                            if (t.R < minR || (t.Pair && t.P.X < 0.12f * t.R)) continue;
                            all[j] = t;
                            float e = Score(t) + OutlineError(all);
                            if (e < best - 1e-3f) { best = e; cur = t; moved = true; }
                            all[j] = cur;
                        }
                    }
                    if (!moved) break;
                }
        }
    }

    static float Longest(Solid s)
    {
        var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
        foreach (var p in s.Points) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
        var e = hi - lo;
        return Math.Max(e.X, Math.Max(e.Y, e.Z));
    }

    static Quality Measure(Solid s, List<(Vector3 P, float R)> spheres)
    {
        var gaps = s.Points.Select(p => Math.Max(0, spheres.Min(k => (p - k.P).Length() - k.R))).OrderBy(g => g).ToArray();
        float outMax = 0;
        foreach (var k in spheres) foreach (var d in Ball) outMax = Math.Max(outMax, s.Distance(k.P + d * k.R));
        float line = 0;
        foreach (var d in Outlines)
        {
            float x = spheres.Max(k => Vector3.Dot(k.P, d) + k.R) - s.Points.Max(p => Vector3.Dot(p, d));
            line += x * x;
        }
        return new Quality(spheres.Count, gaps.Average(), gaps[(int)(gaps.Length * 0.95)], gaps[^1], outMax, MathF.Sqrt(line / Outlines.Length));
    }

    /// <summary>How well these spheres stand for this body (any hull's, to compare)</summary>
    public static Quality? Measure(Prm body, Vector3 offset, float? floorY, IEnumerable<(Vector3 Pos, float Radius)> spheres)
    {
        var list = spheres.Select(x => (x.Pos, x.Radius)).ToList();
        return list.Count == 0 || Fill(body, offset, floorY) is not { } s ? null : Measure(s, list);
    }

    // ---------------------------- the convex parts ----------------------------

    /// <summary>
    /// The convex parts: what the other cars' spheres bump into. One for most bodies; a body with a cabin on a long flat
    /// (a pickup, a formula car) is a much tighter fit as two, so it's cut where that saves the most air.
    /// </summary>
    public static List<ConvexHull> Parts(Prm body, Vector3 offset, float? floorY, IReadOnlyList<Vector3> dirs, int most = 2)
    {
        var verts = body.Verts.Select(v => v.Pos + offset).ToList();
        float top = verts.Min(v => v.Y), low = verts.Max(v => v.Y);
        float floor = floorY is float fl ? Math.Max(fl, top + 0.55f * (low - top)) : low;
        Vector3 Floored(Vector3 v) => new(v.X, Math.Min(v.Y, floor), v.Z);

        ConvexHull? Through(IEnumerable<Vector3> from)
        {
            var all = from.ToList();
            if (all.Count < 4) return null;
            var pts = new List<Vector3>();
            foreach (var dir in dirs)
            {
                var best = all.MaxBy(v => Vector3.Dot(v, dir));
                if (!pts.Any(p => (p - best).LengthSquared() < 4f)) pts.Add(best);
            }
            var part = CarTools.ConvexFrom(pts);
            // The point that's furthest out of it joins in, until what's left out is little (the stock parts have up to 25 points)
            while (part != null && pts.Count < 26)
            {
                var (far, by) = (default(Vector3), 1.2f);
                foreach (var v in all)
                {
                    float o = part.Faces.Max(f => Vector3.Dot(f.Normal, v) + f.D);
                    if (o > by) (far, by) = (v, o);
                }
                if (by <= 1.2f) break;
                pts.Add(far);
                part = CarTools.ConvexFrom(pts) ?? part;
            }
            return part;
        }

        var result = new List<ConvexHull>();
        if (Through(verts.Select(Floored)) is not { } whole) return result;
        result.Add(whole);
        if (most < 2) return result;
        var tris = Triangles(body, offset).Select(t => (Floored(t.A), Floored(t.B), Floored(t.C))).ToList();
        float all0 = Volume(whole);
        (float V, ConvexHull A, ConvexHull B)? cut = null;
        // Across (front and back) and level (base and cabin)
        foreach (var (axis, lo, hi) in new[] { (Vector3.UnitY, top, floor), (Vector3.UnitZ, verts.Min(v => v.Z), verts.Max(v => v.Z)) })
            for (int i = 2; i <= 8; i++)
            {
                float at = lo + (hi - lo) * i / 10f;
                var above = new List<Vector3>(); var below = new List<Vector3>();
                foreach (var (a, b, c) in tris)
                {
                    foreach (var p in new[] { a, b, c })
                    {
                        float d = Vector3.Dot(p, axis) - at;
                        if (d <= 0) above.Add(p);
                        if (d >= 0) below.Add(p);
                    }
                    foreach (var (p, q) in new[] { (a, b), (b, c), (c, a) })
                    {
                        float dp = Vector3.Dot(p, axis) - at, dq = Vector3.Dot(q, axis) - at;
                        if (dp * dq >= 0) continue;
                        var x = p + (q - p) * (dp / (dp - dq));
                        above.Add(x);
                        below.Add(x);
                    }
                }
                if (Through(above) is not { } ha || Through(below) is not { } hb) continue;
                var sa = ha.Max - ha.Min; var sb = hb.Max - hb.Min;
                // A sliver catches on things
                if (Math.Min(sa.X, Math.Min(sa.Y, sa.Z)) < 4 || Math.Min(sb.X, Math.Min(sb.Y, sb.Z)) < 4) continue;
                float v = Volume(ha) + Volume(hb);
                if (cut == null || v < cut.Value.V) cut = (v, ha, hb);
            }
        if (cut is { } best2 && best2.V < 0.8f * all0) { result.Clear(); result.Add(best2.B); result.Add(best2.A); }
        return result;
    }

    /// <summary>A part's volume, counted on a grid of places</summary>
    public static float Volume(ConvexHull c)
    {
        const int n = 22;
        var lo = c.Offset + c.Min; var size = c.Max - c.Min;
        int inside = 0;
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                for (int z = 0; z < n; z++)
                {
                    var p = lo + size * new Vector3((x + 0.5f) / n, (y + 0.5f) / n, (z + 0.5f) / n);
                    if (c.Faces.All(f => Vector3.Dot(f.Normal, p) + f.D <= 0)) inside++;
                }
        return size.X * size.Y * size.Z * inside / (n * n * n);
    }
}
