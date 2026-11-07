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

    /// <summary>32 bit BMP keeps see-through parts, for RVGL</summary>
    public static void SaveBmp32(SKBitmap b, string path)
    {
        int w = b.Width, h = b.Height;
        using var o = new BinaryWriter(File.Create(path));
        o.Write((byte)'B'); o.Write((byte)'M');
        o.Write(54 + w * h * 4); o.Write(0); o.Write(54);
        o.Write(40); o.Write(w); o.Write(h); o.Write((short)1); o.Write((short)32);
        o.Write(0); o.Write(w * h * 4); o.Write(2835); o.Write(2835); o.Write(0); o.Write(0);
        for (int y = h - 1; y >= 0; y--)
            for (int x = 0; x < w; x++)
            {
                var c = b.GetPixel(x, y);
                o.Write(c.Blue); o.Write(c.Green); o.Write(c.Red); o.Write(c.Alpha);
            }
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

        if (!GameFolder.IsStock(c.Folder) &&
            !c.Game.CarFiles(c.Folder).Values.Any(f => Path.GetFileName(f).Contains("read", StringComparison.OrdinalIgnoreCase) && Path.GetFileName(f).Contains("me", StringComparison.OrdinalIgnoreCase)))
            r.Warnings.Add("No readme (try the readme tool)");
        return r;
    }
}

/// <summary>Things people do by hand with a calculator, one click each</summary>
public sealed record PaintOptions(
    float Brightness = 0, float Saturation = 1, float Hue = 0, ICollection<int>? OnlyColours = null,
    float Contrast = 1, float Warmth = 0, SKColor? Colourise = null, float ColouriseAmount = 0,
    SKColor? From = null, SKColor? To = null, float Tolerance = 0.2f)
{
    public bool IsPlain => Brightness == 0 && Saturation == 1 && Hue == 0 && Contrast == 1 && Warmth == 0 &&
                           (Colourise == null || ColouriseAmount == 0) && (From == null || To == null);
}

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

    /// <summary>All the car's models where they stand, without the thin aerial</summary>
    public static IEnumerable<(Prm Model, Matrix4x4 World)> SolidParts(Car c)
    {
        foreach (var part in CarPose.Build(c.Data))
        {
            if (part.Kind is PartKind.AerialSection or PartKind.AerialTop) continue;
            if (LoadModel(c, part.Slot) is { } m && m.Verts.Length > 0) yield return (m, part.World);
        }
    }

    public static (int W, int H, Vector3 Min, Vector3 Max)? CarBounds(Car c)
    {
        var min = new Vector3(float.MaxValue); var max = new Vector3(float.MinValue);
        bool any = false;
        foreach (var (m, world) in SolidParts(c))
            foreach (var v in m.Verts)
            {
                var p = Vector3.Transform(v.Pos, world);
                min = Vector3.Min(min, p);
                max = Vector3.Max(max, p);
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

        void Draw(Prm m, Matrix4x4 world)
        {
            foreach (var f in m.Faces)
            {
                using var path = new SKPath();
                for (int k = 0; k < f.Corners; k++)
                {
                    var i = f.Index(k);
                    if (i < 0 || i >= m.Verts.Length) continue;
                    var p = Vector3.Transform(m.Verts[i].Pos, world);
                    // texture up = front of the car
                    var pt = new SKPoint(2 + (p.X - bb.Min.X) * sx, 2 + (bb.Max.Z - p.Z) * sz);
                    if (k == 0) path.MoveTo(pt); else path.LineTo(pt);
                }
                path.Close();
                cv.DrawPath(path, paint);
            }
        }
        foreach (var (m, world) in SolidParts(c)) Draw(m, world);
        return bmp;
    }

    /// <summary>Make shadow.bmp in the car's folder and point the car to it</summary>
    public static string MakeShadow(Car c)
    {
        var path = c.Game.WritePath("cars\\" + c.Folder + "\\shadow.bmp");
        Files.Backup(path);
        using var b = RenderShadow(c);
        Images.SaveBmp24(b, path);
        c.Data.TShadow = "cars\\" + c.Folder + "\\shadow.bmp";
        FitShadowTable(c);
        return path;
    }

    // ---------------------------- Auto shade ----------------------------

    /// <summary>Bake soft light into the vertex colours: see Shader</summary>
    public static void AutoShade(Prm m, ShadeOptions? options = null, bool withLods = true) => Shader.Shade(m, options, withLods);

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
        var path = c.Game.WritePath("cars\\" + c.Folder + "\\carbox.bmp");
        Files.Backup(path);
        Images.SaveBmp24(box, path);
        c.Data.TCarBox = "cars\\" + c.Folder + "\\carbox.bmp";
        return path;
    }

    // ---------------------------- Texture ----------------------------

    /// <summary>Brightness (-1..1), saturation (0..2), hue shift (degrees). Pure black stays black, the game shows it as see-through</summary>
    public static SKBitmap AdjustTexture(SKBitmap src, float brightness, float saturation, float hue, ICollection<int>? onlyColours = null) =>
        AdjustTexture(src, new PaintOptions(brightness, saturation, hue, onlyColours));

    /// <summary>All the colour changes on a texture. Black stays black: the game shows it as see-through</summary>
    public static SKBitmap AdjustTexture(SKBitmap src, PaintOptions p)
    {
        // Same kind of alpha as the source: without one, black has to stay what the game shows as see-through
        var o = new SKBitmap(src.Width, src.Height, SKColorType.Bgra8888, src.AlphaType == SKAlphaType.Opaque ? SKAlphaType.Opaque : SKAlphaType.Unpremul);
        var inPx = src.Pixels;
        var outPx = new SKColor[inPx.Length];
        var only = p.OnlyColours is { Count: > 0 } ? p.OnlyColours.ToHashSet() : null;
        float toneHue = 0, toneSat = 0;
        p.Colourise?.ToHsv(out toneHue, out toneSat, out _);
        float fromHue = 0, fromSat = 0, fromVal = 0, toHue = 0, toSat = 0, toVal = 0;
        p.From?.ToHsv(out fromHue, out fromSat, out fromVal);
        p.To?.ToHsv(out toHue, out toSat, out toVal);
        bool plainTone = p.Contrast == 1 && p.Warmth == 0;
        Parallel.For(0, inPx.Length, i =>
        {
            var c = inPx[i];
            if (c.Red == 0 && c.Green == 0 && c.Blue == 0) { outPx[i] = c; return; }
            c.ToHsv(out var h, out var s, out var v);

            // Swap one colour for another, keeping the light and dark of the painting
            if (p.From is { } from && p.To != null)
            {
                float gap = ColourGap(c, from);
                float reach = Math.Max(p.Tolerance, 0.001f);
                if (gap < reach)
                {
                    float w = gap < reach * 0.6f ? 1 : 1 - (gap - reach * 0.6f) / (reach * 0.4f);
                    float dHue = MathF.IEEERemainder(toHue - h, 360);
                    h = (h + dHue * w + 360) % 360;
                    s += (toSat - s) * w;
                    float vv = fromVal > 1 ? v / fromVal * toVal : toVal;
                    v = Math.Clamp(v + (vv - v) * w, 1, 100);
                }
            }

            if (only == null || (s >= GreySaturation && only.Contains(ColourBucket(h))))
            {
                h = (h + p.Hue + 360) % 360;
                s = Math.Clamp(s * p.Saturation, 0, 100);
                v = Math.Clamp(v * (1 + p.Brightness), 1, 100);
                if (p.Colourise != null && p.ColouriseAmount > 0)
                {
                    float dHue = MathF.IEEERemainder(toneHue - h, 360);
                    h = (h + dHue * p.ColouriseAmount + 360) % 360;
                    s += (toneSat - s) * p.ColouriseAmount;
                }
            }
            var result = SKColor.FromHsv(h, s, v, c.Alpha);
            if (!plainTone && (only == null || only.Contains(ColourBucket(h))))
            {
                float T(byte b, float shift)
                {
                    float x = (b / 255f - 0.5f) * p.Contrast + 0.5f + shift;
                    return Math.Clamp(x, 0, 1) * 255;
                }
                float warm = p.Warmth * 0.2f;
                result = new SKColor((byte)T(result.Red, warm), (byte)T(result.Green, warm * 0.15f), (byte)T(result.Blue, -warm), c.Alpha);
                // Tones must not turn into the see-through black
                if (result.Red == 0 && result.Green == 0 && result.Blue == 0) result = new SKColor(1, 1, 1, c.Alpha);
            }
            outPx[i] = result;
        });
        o.Pixels = outPx;
        return o;
    }

    // 0 for the same colour, 1 for black against white
    static float ColourGap(SKColor a, SKColor b)
    {
        float dr = a.Red - b.Red, dg = a.Green - b.Green, db = a.Blue - b.Blue;
        return MathF.Sqrt((dr * dr + dg * dg + db * db) / 3) / 255;
    }

    const float GreySaturation = 18;
    public const int ColourBuckets = 12;
    static int ColourBucket(float hue) => (int)((hue + 15) % 360 / (360f / ColourBuckets));

    /// <summary>The main colours of a texture, biggest first: which group, how much of it, and an average to show</summary>
    public static List<(int Bucket, int Count, SKColor Sample)> MainColours(SKBitmap src)
    {
        var counts = new int[ColourBuckets];
        var r = new long[ColourBuckets]; var g = new long[ColourBuckets]; var b = new long[ColourBuckets];
        for (int y = 0; y < src.Height; y += 2)
            for (int x = 0; x < src.Width; x += 2)
            {
                var c = src.GetPixel(x, y);
                c.ToHsv(out var h, out var s, out var v);
                if (s < GreySaturation || v < 12) continue;
                int i = ColourBucket(h);
                counts[i]++; r[i] += c.Red; g[i] += c.Green; b[i] += c.Blue;
            }
        return Enumerable.Range(0, ColourBuckets).Where(i => counts[i] > 40).OrderByDescending(i => counts[i])
            .Select(i => (i, counts[i], new SKColor((byte)(r[i] / counts[i]), (byte)(g[i] / counts[i]), (byte)(b[i] / counts[i])))).ToList();
    }

    public static void SaveTexture(SKBitmap b, string path, bool keepAlpha)
    {
        if (keepAlpha) Images.SaveBmp32(b, path); else Images.SaveBmp24(b, path);
    }

    /// <summary>
    /// A new car around your own model: its body, one wheel for all four (the right side mirrored) and its texture.
    /// Suspension of the base car wouldn't fit, so it goes. Places the wheels and fills what can be worked out.
    /// </summary>
    public static void SetupOriginal(Car c, string bodyFile, string? wheelFile, string? textureFile)
    {
        var d = c.Data;
        var body = Prm.Load(bodyFile);
        if (!body.IsValid() || body.Faces.Length == 0) throw new InvalidDataException("That body model is damaged");
        string Rel(string name) => "cars\\" + c.Folder + "\\" + name;

        body.Save(Path.Combine(c.Dir, "body.prm"));
        int bodySlot = d.BodyModel >= 0 ? d.BodyModel : 0;
        d.Models[bodySlot] = Rel("body.prm");
        d.BodyModel = bodySlot;
        d.BodyOffset = Vector3.Zero;

        for (int i = 0; i < 4; i++)
        {
            d.Springs[i].Model = -1; d.Springs[i].Offset = Vector3.Zero;
            d.Axles[i].Model = -1; d.Axles[i].Offset = Vector3.Zero;
            d.Pins[i].Model = -1;
        }
        d.Spinner.Model = -1;

        if (wheelFile != null)
        {
            var names = new[] { "wheelfl.prm", "wheelfr.prm", "wheelbl.prm", "wheelbr.prm" };
            for (int i = 0; i < 4; i++)
            {
                var w = Prm.Load(wheelFile);
                if (i % 2 == 1) w.MirrorX();
                w.Save(Path.Combine(c.Dir, names[i]));
                int slot = i + 1 == bodySlot ? 5 + i : i + 1;
                d.Models[slot] = Rel(names[i]);
                d.Wheels[i].Model = slot;
                d.Wheels[i].Present = true;
            }
        }
        if (textureFile != null)
        {
            File.Copy(textureFile, Path.Combine(c.Dir, "car.bmp"), true);
            d.TPage = Rel("car.bmp");
        }

        // Slots nothing uses any more
        var used = new HashSet<int> { d.BodyModel, d.Aerial.SecModel, d.Aerial.TopModel };
        foreach (var w in d.Wheels) used.Add(w.Model);
        for (int i = 0; i < 19; i++) if (!used.Contains(i)) d.Models[i] = "NONE";

        FitToModels(c);
        PlaceWheels(c);
        ComputeInertia(c);
        if (LoadModel(c, bodySlot) is { } b)
        {
            var dir = d.Aerial.Direction.LengthSquared() < 1e-6f ? -Vector3.UnitY : d.Aerial.Direction;
            d.Aerial.Direction = dir;
            d.Aerial.Offset = new Vector3(b.Max.X * 0.7f, b.Min.Y * 0.6f, b.Min.Z * 0.75f);
            if (MakeHull(b, Vector3.Zero) is { } h)
            {
                h.Save(Path.Combine(c.Dir, "hull.hul"));
                d.Coll = Rel("hull.hul");
            }
        }
        d.TShadow = "";
        d.HasShadowIndex = false;
        FitShadowTable(c);
        d.TCarBox = "";
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
