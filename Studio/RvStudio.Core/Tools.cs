using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using SkiaSharp;

namespace RvStudio.Core;

public static class Images
{
    /// <summary>Decode any image the game takes. RVGL allows other formats inside a .bmp name</summary>
    public static SKBitmap? Load(string? path)
    {
        if (path == null) return null;
        try { return SKBitmap.Decode(File.ReadAllBytes(path)); }
        catch { return null; }
    }

    /// <summary>24 bit BMP, what the game has always read</summary>
    public static void SaveBmp24(SKBitmap b, string path)
    {
        int w = b.Width, h = b.Height, stride = (w * 3 + 3) & ~3;
        using var o = new BinaryWriter(File.Create(path));
        o.Write((byte)'B'); o.Write((byte)'M');
        o.Write(54 + stride * h); o.Write(0); o.Write(54);
        o.Write(40); o.Write(w); o.Write(h); o.Write((short)1); o.Write((short)24);
        o.Write(0); o.Write(stride * h); o.Write(2835); o.Write(2835); o.Write(0); o.Write(0);
        var row = new byte[stride];
        for (int y = h - 1; y >= 0; y--)
        {
            for (int x = 0; x < w; x++)
            {
                var c = b.GetPixel(x, y);
                row[x * 3] = c.Blue; row[x * 3 + 1] = c.Green; row[x * 3 + 2] = c.Red;
            }
            o.Write(row);
        }
    }
}

public sealed class CheckReport
{
    public readonly List<string> Errors = new(), Warnings = new(), Infos = new();
    public int Score => Math.Max(0, 100 - Errors.Count * 15 - Warnings.Count * 4);
}

public static class CarChecker
{
    static bool None(string? p) => string.IsNullOrWhiteSpace(p) || p.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase);

    public static CheckReport Check(Car c)
    {
        var r = new CheckReport();
        var d = c.Data;
        if (d.Name.Trim() == "") r.Errors.Add("The car has no name");

        Prm? Slot(int n, string what)
        {
            if (n < 0) return null;
            if (n > 18) { r.Errors.Add($"{what} uses model {n}, the last one is 18"); return null; }
            if (None(d.Models[n])) { r.Errors.Add($"{what} uses model {n}, which is NONE"); return null; }
            var f = c.Game.Find(d.Models[n]);
            if (f == null) { r.Errors.Add($"{what}: {d.Models[n]}: file not found"); return null; }
            try
            {
                var p = Prm.Load(f);
                if (p.Faces.Length == 0) { r.Errors.Add($"{what}: {d.Models[n]}: empty model"); return null; }
                if (!p.IsValid()) { r.Errors.Add($"{what}: {d.Models[n]}: a polygon uses a vertex that isn't there"); return null; }
                return p;
            }
            catch (UnauthorizedAccessException) { r.Errors.Add($"{what}: {d.Models[n]}: no access to the file (check its permissions)"); }
            catch (Exception) { r.Errors.Add($"{what}: {d.Models[n]}: empty or damaged file"); }
            return null;
        }

        Prm? body = null;
        if (d.BodyModel < 0) r.Errors.Add("The body has no model");
        else body = Slot(d.BodyModel, "Body");

        var wheels = new Prm?[4];
        for (int i = 0; i < 4; i++)
        {
            if (d.Wheels[i].Present)
            {
                if (d.Wheels[i].Model < 0) r.Warnings.Add($"Wheel {i} is present but has no model");
                wheels[i] = Slot(d.Wheels[i].Model, "Wheel " + i);
            }
            Slot(d.Springs[i].Model, "Spring " + i);
            Slot(d.Axles[i].Model, "Axle " + i);
            Slot(d.Pins[i].Model, "Pin " + i);
            if (d.Springs[i].Model >= 0 && d.Springs[i].Length == 0) r.Errors.Add($"Spring {i} has a model but length 0");
            if (d.Axles[i].Model >= 0 && d.Axles[i].Length == 0) r.Errors.Add($"Axle {i} has a model but length 0");
        }
        Slot(d.Spinner.Model, "Spinner");
        Slot(d.Aerial.SecModel, "Aerial");
        Slot(d.Aerial.TopModel, "Aerial top");

        var texPath = c.Game.Find(d.TPage);
        if (texPath == null) r.Errors.Add("Texture not found: " + d.TPage);
        else
        {
            using var b = Images.Load(texPath);
            if (b == null) r.Errors.Add("Texture can't be read: " + d.TPage);
            else
            {
                r.Infos.Add($"Texture {b.Width} x {b.Height}");
                if (b.Width != b.Height || (b.Width & (b.Width - 1)) != 0) r.Errors.Add($"Texture must be square, power of 2 (it's {b.Width} x {b.Height})");
                if (b.Width > 256) r.Infos.Add("Texture is over 256: only RVGL can use it");
            }
        }

        var coll = c.Game.Find(d.Coll);
        if (coll == null) r.Errors.Add("Collision hull not found: " + d.Coll);
        else if (new FileInfo(coll).Length == 0) r.Errors.Add("Collision hull is empty: " + d.Coll);
        if (None(d.TCarBox)) r.Infos.Add("No carbox");
        else if (c.Game.Find(d.TCarBox) == null) r.Warnings.Add("Carbox not found: " + d.TCarBox);
        if (!None(d.TShadow) && c.Game.Find(d.TShadow) == null) r.Warnings.Add("Shadow texture not found: " + d.TShadow);
        foreach (var s in new[] { d.SfxEngine, d.SfxHonk, d.SfxServo })
            if (!None(s) && c.Game.Find(s) == null) r.Warnings.Add("Sound not found: " + s);

        foreach (var f in c.ExternalFiles()) r.Warnings.Add("Depends on another folder: " + f);

        if (d.EnvR == 0 && d.EnvG == 0 && d.EnvB == 0) r.Warnings.Add("EnvRGB is 0 0 0: no reflections at all");
        if (!d.Selectable) r.Warnings.Add("Selectable is FALSE: players can't pick it");
        if (!d.BestTime) r.Infos.Add("BestTime is FALSE: its times don't count");
        if (d.Obtain != 0) r.Infos.Add($"Obtain is {d.Obtain}: not available from the start");
        if (d.Weapon == Vector3.Zero) r.Infos.Add("Weapon offset is 0 0 0: weapons start in the middle of the car");

        var bp = body?.Faces.Length ?? 0;
        r.Infos.Add($"Polygons: body {bp}, wheels {wheels.Where(w => w != null).Sum(w => w!.Faces.Length)}");
        if (bp > 5000) r.Warnings.Add($"Body has a lot of polygons ({bp}), stock cars have a few hundred");

        if (body != null)
        {
            var size = body.Max - body.Min;
            r.Infos.Add($"Body size {size.X:0} x {size.Y:0} x {size.Z:0} (W x H x L)");
            if (size.Z < 30 || size.Z > 600) r.Warnings.Add($"Unusual body length: {size.Z:0} (stock cars: 100 to 300)");
            if (size.X > size.Z * 2) r.Warnings.Add("Body is much wider than long, is it turned the wrong way?");
            if (body.Faces.All(f => f.Colors.All(x => (x & 0xFFFFFF) == 0xFFFFFF))) r.Warnings.Add("Body has no shading at all (try Auto shade)");
        }

        var present = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Present).ToArray();
        if (present.Length > 0)
        {
            if (!present.Any(i => d.Wheels[i].Powered)) r.Errors.Add("No wheel is powered: the car won't move");
            if (!present.Any(i => d.Wheels[i].Turnable)) r.Warnings.Add("No wheel can steer");
            foreach (var i in present)
            {
                if (d.Wheels[i].Radius <= 0) r.Errors.Add($"Wheel {i} has no radius");
                else if (wheels[i] is { } w)
                {
                    var size = (w.Max.Y - w.Min.Y) / 2;
                    if (size > 0 && Math.Abs(size - d.Wheels[i].Radius) / size > 0.5)
                        r.Warnings.Add($"Wheel {i}: radius is {d.Wheels[i].Radius:0.0} but the model's is about {size:0.0}");
                }
            }
            foreach (var (a, b) in new[] { (0, 1), (2, 3) })
                if (d.Wheels[a].Present && d.Wheels[b].Present &&
                    (Math.Abs(Math.Abs(d.Wheels[a].Offset1.X) - Math.Abs(d.Wheels[b].Offset1.X)) > 1 || Math.Abs(d.Wheels[a].Offset1.Z - d.Wheels[b].Offset1.Z) > 1))
                    r.Warnings.Add($"Wheels {a} and {b} aren't symmetrical");
        }

        if (d.BodyMass <= 0) r.Errors.Add("Body mass must be above 0");
        if (d.TopSpeed <= 0) r.Errors.Add("TopSpeed must be above 0");
        if (d.EngineRate <= 0) r.Warnings.Add("EngineRate is 0");
        if (d.SteerRate <= 0) r.Warnings.Add("SteerRate is 0");
        if (d.Inertia[0].X <= 0 || d.Inertia[1].Y <= 0 || d.Inertia[2].Z <= 0) r.Errors.Add("Inertia matrix: the diagonal must be above 0");
        if (d.Ai == null) r.Warnings.Add("No AI section: CPU cars can't use it well");
        if (Math.Abs(d.CoM.Y) > 25) r.Warnings.Add($"Center of Mass's Y looks wrong: {d.CoM.Y:0.0}");
        if (Math.Abs(d.CoM.X) > 40) r.Warnings.Add($"Center of Mass is far to one side: {d.CoM.X:0.0}");
        r.Infos.Add($"Center of Mass {d.CoM.X:0.00} {d.CoM.Y:0.00} {d.CoM.Z:0.00}");

        if (!GameFolder.IsStock(c.Folder) && Directory.Exists(c.Dir) &&
            !Directory.GetFiles(c.Dir).Any(f => Path.GetFileName(f).Contains("read", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f).Contains("me", StringComparison.OrdinalIgnoreCase)))
            r.Warnings.Add("No readme (try the readme tool)");
        return r;
    }
}

/// <summary>Things people do by hand with a calculator, one click each</summary>
public static class CarTools
{
    public static Prm? LoadModel(Car c, int slot)
    {
        if (slot < 0 || slot > 18) return null;
        var f = c.Game.Find(c.Data.Models[slot]);
        if (f == null) return null;
        try { var p = Prm.Load(f); return p.IsValid() ? p : null; }
        catch (Exception) { return null; }
    }

    // ---------------------------- Symmetry ----------------------------

    /// <summary>The wheel on the other side of the car</summary>
    public static int Mirror(int wheel) => wheel ^ 1;
    /// <summary>The other wheels of the same axle or side</summary>
    public static int[] Axle(int wheel) => wheel < 2 ? new[] { 0, 1 } : new[] { 2, 3 };

    /// <summary>Make the right side the mirror of the left one</summary>
    public static void Symmetrize(CarData d, int fromSide = 0)
    {
        foreach (var (a0, b0) in new[] { (0, 1), (2, 3) })
        {
            int a = fromSide == 0 ? a0 : b0, b = fromSide == 0 ? b0 : a0;
            Vector3 M(Vector3 v) => new(-v.X, v.Y, v.Z);
            var wa = d.Wheels[a]; var wb = d.Wheels[b];
            int model = wb.Model;
            d.Wheels[b] = wa.Clone();
            d.Wheels[b].Model = model; // stock cars ship mirrored models
            d.Wheels[b].Offset1 = M(wa.Offset1);
            d.Wheels[b].Offset2 = M(wa.Offset2);
            d.Springs[b].Offset = M(d.Springs[a].Offset);
            d.Springs[b].Length = d.Springs[a].Length;
            d.Springs[b].Stiffness = d.Springs[a].Stiffness;
            d.Springs[b].Damping = d.Springs[a].Damping;
            d.Springs[b].Restitution = d.Springs[a].Restitution;
            d.Axles[b].Offset = M(d.Axles[a].Offset);
            d.Axles[b].Length = d.Axles[a].Length;
            d.Pins[b].Offset = M(d.Pins[a].Offset);
            d.Pins[b].Length = d.Pins[a].Length;
        }
    }

    // ---------------------------- Fitting ----------------------------

    /// <summary>Radius and skid width from the wheel models, spring and axle length from their models</summary>
    public static int FitToModels(Car c)
    {
        var d = c.Data;
        int n = 0;
        for (int i = 0; i < 4; i++)
        {
            if (LoadModel(c, d.Wheels[i].Model) is { } w && w.Verts.Length > 0)
            {
                d.Wheels[i].Radius = Math.Round((w.Max.Y - w.Min.Y) / 2, 2);
                d.Wheels[i].SkidWidth = Math.Round(w.Max.X - w.Min.X, 2);
                n++;
            }
            if (LoadModel(c, d.Springs[i].Model) is { } s) { d.Springs[i].Length = Math.Round(s.Max.Y - s.Min.Y, 2); n++; }
            if (LoadModel(c, d.Axles[i].Model) is { } a) { d.Axles[i].Length = Math.Round(a.Max.Z - a.Min.Z, 2); n++; }
        }
        return n;
    }

    /// <summary>Solid box inertia of the body. Same formula for every car, so it's a good start</summary>
    public static bool ComputeInertia(Car c)
    {
        if (LoadModel(c, c.Data.BodyModel) is not { } b || b.Verts.Length == 0) return false;
        var s = b.Max - b.Min;
        var m = (float)c.Data.BodyMass / 12f;
        c.Data.Inertia[0] = new Vector3(m * (s.Y * s.Y + s.Z * s.Z), 0, 0);
        c.Data.Inertia[1] = new Vector3(0, m * (s.X * s.X + s.Z * s.Z), 0);
        c.Data.Inertia[2] = new Vector3(0, 0, m * (s.X * s.X + s.Y * s.Y));
        return true;
    }

    /// <summary>Wheels on the ground of the body: fills the positions of a new model</summary>
    public static bool PlaceWheels(Car c)
    {
        if (LoadModel(c, c.Data.BodyModel) is not { } b || b.Verts.Length == 0) return false;
        var d = c.Data;
        float x = b.Max.X - (float)Math.Max(d.Wheels[0].SkidWidth, 8) / 2f;
        float y = b.Max.Y - (float)d.Wheels[0].Radius / 2f;
        float zf = b.Max.Z - (b.Max.Z - b.Min.Z) * 0.2f, zb = b.Min.Z + (b.Max.Z - b.Min.Z) * 0.2f;
        d.Wheels[0].Offset1 = new(-x, y, zf);
        d.Wheels[1].Offset1 = new(x, y, zf);
        d.Wheels[2].Offset1 = new(-x, y, zb);
        d.Wheels[3].Offset1 = new(x, y, zb);
        return true;
    }

    public static (int W, int H, Vector3 Min, Vector3 Max)? CarBounds(Car c)
    {
        var d = c.Data;
        var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
        bool any = false;
        if (LoadModel(c, d.BodyModel) is { } b && b.Verts.Length > 0)
        {
            min = b.Min + d.BodyOffset; max = b.Max + d.BodyOffset; any = true;
        }
        for (int i = 0; i < 4; i++)
            if (d.Wheels[i].Present && LoadModel(c, d.Wheels[i].Model) is { } w && w.Verts.Length > 0)
            {
                min = Vector3.Min(min, w.Min + d.Wheels[i].Offset1);
                max = Vector3.Max(max, w.Max + d.Wheels[i].Offset1);
                any = true;
            }
        return any ? (0, 0, min, max) : null;
    }

    // ---------------------------- Shadow ----------------------------

    /// <summary>Fill SHADOWTABLE around the body and wheels</summary>
    public static bool FitShadowTable(Car c)
    {
        if (CarBounds(c) is not { } b) return false;
        var d = c.Data;
        var h = d.HasShadowTable && d.ShadowTable[4] != 0 ? d.ShadowTable[4] : -5.0;
        d.ShadowTable = new[] { Math.Round((double)b.Min.X, 2), Math.Round((double)b.Max.X, 2), Math.Round((double)b.Max.Z, 2), Math.Round((double)b.Min.Z, 2), h };
        d.HasShadowTable = true;
        return true;
    }

    /// <summary>Top view of the car as a white shape on black, blurred at the edges, like stock shadows</summary>
    public static SKBitmap RenderShadow(Car c, int size = 64, float blur = 1.6f)
    {
        var d = c.Data;
        var bb = CarBounds(c) ?? (0, 0, new Vector3(-50), new Vector3(50));
        float sx = (size - 4) / Math.Max(bb.Max.X - bb.Min.X, 1), sz = (size - 4) / Math.Max(bb.Max.Z - bb.Min.Z, 1);
        var bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var cv = new SKCanvas(bmp);
        cv.Clear(SKColors.Black);
        using var paint = new SKPaint { Color = SKColors.White, IsAntialias = true, Style = SKPaintStyle.Fill, MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, blur) };

        void Draw(Prm? m, Vector3 off)
        {
            if (m == null) return;
            foreach (var f in m.Faces)
            {
                using var path = new SKPath();
                for (int k = 0; k < f.Corners; k++)
                {
                    var i = f.Index(k);
                    if (i < 0 || i >= m.Verts.Length) continue;
                    var p = m.Verts[i].Pos + off;
                    // texture up = front of the car
                    var pt = new SKPoint(2 + (p.X - bb.Min.X) * sx, 2 + (bb.Max.Z - p.Z) * sz);
                    if (k == 0) path.MoveTo(pt); else path.LineTo(pt);
                }
                path.Close();
                cv.DrawPath(path, paint);
            }
        }
        Draw(LoadModel(c, d.BodyModel), d.BodyOffset);
        for (int i = 0; i < 4; i++) if (d.Wheels[i].Present) Draw(LoadModel(c, d.Wheels[i].Model), d.Wheels[i].Offset1);
        return bmp;
    }

    /// <summary>Make shadow.bmp in the car's folder and point the car to it</summary>
    public static string MakeShadow(Car c)
    {
        var path = Path.Combine(c.Dir, "shadow.bmp");
        Files.Backup(path);
        using var b = RenderShadow(c);
        Images.SaveBmp24(b, path);
        c.Data.TShadow = "cars\\" + c.Folder + "\\shadow.bmp";
        FitShadowTable(c);
        return path;
    }

    // ---------------------------- Auto shade ----------------------------

    public sealed record ShadeOptions(float Ambient = 0.45f, float Sun = 0.55f, float Occlusion = 1f, Vector3? SunDir = null, int Rays = 48);

    static bool RayHits(Vector3 o, Vector3 d, Prm m, float maxDist)
    {
        foreach (var f in m.Faces)
        {
            if ((f.Flags & FaceFlags.Translucent) != 0) continue;
            for (int t = 0; t < (f.IsQuad ? 2 : 1); t++)
            {
                int ia = f.V0, ib = t == 0 ? f.V1 : f.V2, ic = t == 0 ? f.V2 : f.V3;
                if (ia >= m.Verts.Length || ib >= m.Verts.Length || ic >= m.Verts.Length) continue;
                Vector3 a = m.Verts[ia].Pos, e1 = m.Verts[ib].Pos - a, e2 = m.Verts[ic].Pos - a;
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
        }
        return false;
    }

    /// <summary>
    /// Bake soft shading into the vertex colours: sky light that the car's own shape blocks, plus sun.
    /// Keeps each polygon's alpha. Y goes down, so the sky is at -Y.
    /// </summary>
    public static void AutoShade(Prm m, ShadeOptions? options = null)
    {
        var o = options ?? new ShadeOptions();
        var sun = Vector3.Normalize(o.SunDir ?? new Vector3(0.35f, -1f, 0.25f));
        var dirs = new Vector3[o.Rays];
        // Fibonacci sphere: even directions, no randomness, same result each time
        for (int i = 0; i < o.Rays; i++)
        {
            float y = 1 - (i + 0.5f) * 2f / o.Rays, r = MathF.Sqrt(Math.Max(0, 1 - y * y)), a = i * 2.3999632f;
            dirs[i] = new Vector3(MathF.Cos(a) * r, y, MathF.Sin(a) * r);
        }
        var size = (m.Max - m.Min).Length();
        var cache = new float[m.Verts.Length];
        for (int v = 0; v < m.Verts.Length; v++)
        {
            var n = m.Verts[v].Normal;
            if (n.LengthSquared() < 1e-4f) n = -Vector3.UnitY;
            n = Vector3.Normalize(n);
            var start = m.Verts[v].Pos + n * 0.1f;
            float open = 0, total = 0;
            foreach (var d in dirs)
            {
                var c = Vector3.Dot(d, n);
                if (c <= 0) continue;
                // ground hides what comes from below
                var sky = 0.35f + 0.65f * Math.Max(0, -d.Y);
                total += c * sky;
                if (!RayHits(start, d, m, size)) open += c * sky;
            }
            float occl = total > 0 ? open / total : 1;
            occl = 1 - (1 - occl) * o.Occlusion;
            float lit = Math.Max(0, Vector3.Dot(n, sun));
            if (lit > 0 && RayHits(start, sun, m, size)) lit *= 0.15f;
            cache[v] = Math.Clamp(o.Ambient * occl * 1.4f + o.Sun * lit, 0.08f, 1f);
        }
        foreach (var f in m.Faces)
            for (int k = 0; k < f.Corners; k++)
            {
                var i = f.Index(k);
                if (i < 0 || i >= cache.Length) continue;
                var g = (uint)Math.Clamp((int)(cache[i] * 255), 0, 255);
                f.Colors[k] = (f.Colors[k] & 0xFF000000) | (g << 16) | (g << 8) | g;
            }
    }

    // ---------------------------- Collision hull ----------------------------

    static readonly Vector3[] DopDirs = MakeDirs();
    static Vector3[] MakeDirs()
    {
        var r = new List<Vector3>();
        for (int x = -1; x <= 1; x++) for (int y = -1; y <= 1; y++) for (int z = -1; z <= 1; z++)
            if (x != 0 || y != 0 || z != 0) r.Add(Vector3.Normalize(new(x, y, z)));
        return r.ToArray();
    }

    /// <summary>A simple hull from the body: its outer points as one convex part, spheres inside it</summary>
    public static Hull? MakeHull(Prm body, Vector3 offset)
    {
        if (body.Verts.Length < 4) return null;
        var pts = new List<Vector3>();
        foreach (var dir in DopDirs)
        {
            var best = body.Verts.MaxBy(v => Vector3.Dot(v.Pos, dir)).Pos + offset;
            if (!pts.Any(p => (p - best).LengthSquared() < 4f)) pts.Add(best);
        }

        // Planes through 3 points with all others behind them
        var planes = new List<(Vector3 N, float D)>();
        for (int a = 0; a < pts.Count; a++)
            for (int b = a + 1; b < pts.Count; b++)
                for (int c = b + 1; c < pts.Count; c++)
                {
                    var n = Vector3.Cross(pts[b] - pts[a], pts[c] - pts[a]);
                    if (n.LengthSquared() < 1e-3f) continue;
                    n = Vector3.Normalize(n);
                    var d = -Vector3.Dot(n, pts[a]);
                    bool pos = false, neg = false;
                    foreach (var p in pts)
                    {
                        var s = Vector3.Dot(n, p) + d;
                        if (s > 0.05f) pos = true;
                        if (s < -0.05f) neg = true;
                    }
                    if (pos && neg) continue;
                    if (pos) { n = -n; d = -d; }
                    if (!planes.Any(q => Vector3.Dot(q.N, n) > 0.999f && Math.Abs(q.D - d) < 0.1f)) planes.Add((n, d));
                }
        if (planes.Count < 4) return null;

        // Points on a plane, then edges: pairs of points shared by 2 planes
        var used = new List<Vector3>();
        var edges = new List<(short, short)>();
        bool On(Vector3 p, (Vector3 N, float D) q) => Math.Abs(Vector3.Dot(q.N, p) + q.D) < 0.1f;
        foreach (var p in pts) if (planes.Count(q => On(p, q)) >= 3) used.Add(p);
        for (short a = 0; a < used.Count; a++)
            for (short b = (short)(a + 1); b < used.Count; b++)
                if (planes.Count(q => On(used[a], q) && On(used[b], q)) >= 2) edges.Add((a, b));

        var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
        foreach (var p in used) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        var center = (min + max) / 2;

        var hull = new Hull();
        hull.Convex.Add(new ConvexHull
        {
            Points = used.ToArray(), Edges = edges.ToArray(), Faces = planes.ToArray(),
            Offset = center, Min = min - center, Max = max - center
        });

        // Spheres: a grid inside, each touching the nearest plane
        float Depth(Vector3 p) => planes.Min(q => -(Vector3.Dot(q.N, p) + q.D));
        for (int iz = 0; iz < 5; iz++)
            for (int ix = -1; ix <= 1; ix += 2)
            {
                var p = new Vector3(center.X + ix * (max.X - min.X) * 0.25f, center.Y, min.Z + (max.Z - min.Z) * (0.12f + 0.19f * iz));
                var r = Depth(p);
                if (r > 1) hull.Spheres.Add((p, r));
            }
        return hull;
    }

    // ---------------------------- Readme ----------------------------

    public sealed record ReadmeInfo(string Author = "", string Email = "", string Site = "", string Comments = "", string Thanks = "", string Tools = "RV Car Studio", string Links = "");

    public static string[] RatingNames = { "Rookie", "Amateur", "Advanced", "Semi-Pro", "Pro", "Super Pro" };
    public static string[] ClassNames = { "Electric", "Glow", "Special" };

    public static string FillReadme(string template, Car c, ReadmeInfo info)
    {
        var d = c.Data;
        int Polys(int slot) => LoadModel(c, slot)?.Faces.Length ?? 0;
        int body = Polys(d.BodyModel);
        var wp = d.Wheels.Select(w => Polys(w.Model)).ToArray();
        var powered = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Powered).ToArray();
        var drive = powered.Length == 4 ? "4WD" : powered.Length == 0 ? "none" : powered[0] < 2 ? "Front wheels" : "Rear wheels";
        var tags = new Dictionary<string, string>
        {
            ["[CARNAME]"] = d.Name, ["[AUTHOR]"] = info.Author, ["[FOLDER]"] = c.Folder,
            ["[TYPE]"] = GameFolder.IsStock(c.Folder) ? "Stock" : "Original",
            ["[CLASS]"] = d.Class is >= 0 and < 3 ? ClassNames[d.Class] : "Special",
            ["[RATING]"] = d.Rating is >= 0 and < 6 ? RatingNames[d.Rating] : "?",
            ["[ENGINE]"] = drive,
            ["[TOPSPEED]"] = $"{d.TopSpeed:0.0} mph ({d.TopSpeed * 1.609:0.0} km/h)",
            ["[POLYCOUNT]"] = (body + wp.Sum()).ToString(), ["[BODYPOLYCOUNT]"] = body.ToString(),
            ["[WHEELSPOLYCOUNT]"] = wp.Sum().ToString(), ["[WHEELONEPOLYCOUNT]"] = wp[0].ToString(),
            ["[EMAIL]"] = info.Email, ["[WEBSITE]"] = info.Site, ["[SITE]"] = info.Site,
            ["[COMMENTS]"] = info.Comments, ["[DESCRIPTION]"] = info.Comments, ["[THANKS]"] = info.Thanks,
            ["[TOOLS]"] = info.Tools, ["[LINKS]"] = info.Links, ["[COPYRIGHT]"] = info.Author,
            ["[DATE]"] = DateTime.Now.ToString("yyyy-MM-dd")
        };
        var o = new List<string>();
        foreach (var raw in template.Replace("\r\n", "\n").Split('\n'))
        {
            var line = raw;
            var tr = line.TrimEnd();
            if (tr.Length > 4 && tr.StartsWith("+ ") && tr.EndsWith('+') && tr.Contains('['))
            {
                // framed line: the frame stays where it is, whatever the text
                int width = tr.Length - 4;
                var inner = tr.Substring(2, width).Trim();
                bool right = inner.Contains("%right%");
                inner = inner.Replace("%right%", "").Replace("%keeplayout%", "").Trim();
                foreach (var kv in tags) inner = inner.Replace(kv.Key, kv.Value);
                foreach (var para in inner.Replace("\r\n", "\n").Split('\n'))
                {
                    var p = para;
                    do
                    {
                        var piece = p;
                        if (p.Length > width)
                        {
                            int cut = p.LastIndexOf(' ', width);
                            if (cut <= 0) cut = width;
                            piece = p[..cut];
                            p = p[cut..].TrimStart();
                        }
                        else p = "";
                        o.Add("+ " + (right ? piece.PadLeft(width) : piece.PadRight(width)) + " +");
                    } while (p != "");
                }
                continue;
            }
            foreach (var kv in tags) line = line.Replace(kv.Key, kv.Value);
            o.Add(line);
        }
        return string.Join("\r\n", o);
    }

    // ---------------------------- Carbox ----------------------------

    /// <summary>The carbox from a picture of the car: name on top, white background</summary>
    public static SKBitmap ComposeCarbox(SKBitmap car, string name, SKColor textColor, int size = 256)
    {
        var bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var cv = new SKCanvas(bmp);
        cv.Clear(SKColors.White);
        using var typeface = SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
        using var paint = new SKPaint { Typeface = typeface, IsAntialias = true, TextAlign = SKTextAlign.Center };
        var upper = name.ToUpperInvariant();
        paint.TextSize = size / 6f;
        var w = paint.MeasureText(upper);
        if (w > size * 0.92f) paint.TextSize *= size * 0.92f / w;
        for (int i = 3; i >= 0; i--)
        {
            paint.Color = textColor.WithAlpha((byte)(i == 0 ? 255 : 110 - i * 30));
            cv.DrawText(upper, size / 2f, size * 0.2f + i * paint.TextSize * 0.8f, paint);
        }
        // multiply: the white around the car lets the name show through
        using (var picture = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true, BlendMode = SKBlendMode.Multiply })
            cv.DrawBitmap(car, new SKRect(0, size * 0.18f, size, size), picture);
        return bmp;
    }

    public static string SaveCarbox(Car c, SKBitmap box)
    {
        var path = Path.Combine(c.Dir, "carbox.bmp");
        Files.Backup(path);
        Images.SaveBmp24(box, path);
        c.Data.TCarBox = "cars\\" + c.Folder + "\\carbox.bmp";
        return path;
    }

    // ---------------------------- Texture ----------------------------

    /// <summary>Brightness (-1..1), saturation (0..2), hue shift (degrees). Pure black stays black, the game shows it as see-through</summary>
    public static SKBitmap AdjustTexture(SKBitmap src, float brightness, float saturation, float hue)
    {
        var o = new SKBitmap(src.Width, src.Height, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                var c = src.GetPixel(x, y);
                if (c.Red == 0 && c.Green == 0 && c.Blue == 0) { o.SetPixel(x, y, c); continue; }
                c.ToHsv(out var h, out var s, out var v);
                h = (h + hue + 360) % 360;
                s = Math.Clamp(s * saturation, 0, 100);
                v = Math.Clamp(v * (1 + brightness), 1, 100);
                o.SetPixel(x, y, SKColor.FromHsv(h, s, v, c.Alpha));
            }
        return o;
    }

    public static string SameBodyAsStock(Car c)
    {
        if (c.Game.Find(c.Data.BodyModel is >= 0 and < 19 ? c.Data.Models[c.Data.BodyModel] : null) is not { } own) return "";
        try
        {
            var h = Convert.ToHexString(MD5.HashData(File.ReadAllBytes(own)));
            foreach (var dir in Directory.GetDirectories(c.Game.CarsDir))
            {
                var folder = Path.GetFileName(dir);
                if (!GameFolder.IsStock(folder) || folder.Equals(c.Folder, StringComparison.OrdinalIgnoreCase)) continue;
                var f = Path.Combine(dir, "body.prm");
                if (File.Exists(f) && Convert.ToHexString(MD5.HashData(File.ReadAllBytes(f))) == h) return folder;
            }
        }
        catch (Exception) { }
        return "";
    }
}
