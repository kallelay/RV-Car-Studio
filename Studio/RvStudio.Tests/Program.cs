using System.Numerics;
using System.Text;
using RvStudio.Core;

// RvStudio.Tests read <Re-Volt folder>   writes nothing. Safe on the real game
// RvStudio.Tests write <copy of folder>  saving, new car, rename, tools: only on a copy
// RvStudio.Tests fit <Re-Volt folder>    the simulator against the stock cars' real TopEnd and Acc

int pass = 0, fail = 0;
void Check(bool ok, string what) { if (ok) pass++; else { fail++; Console.WriteLine("FAIL " + what); } }

var mode = args.Length > 0 ? args[0] : "read";
var game = new GameFolder(args.Length > 1 ? args[1] : @"D:\ReVolt");

void Parser()
{
    var t = "{\r\nName \"Air AI (BODY)\"\r\nAcc 1.5.2\r\n;)CPUSelectable FALSE\r\nBODY {\r\nModelNum 3 ; {comment}\r\nOffset 1.0, -2.0, 3.0\r\nInertia 1 2 3\r\n4 5 6\r\n7 8 9\r\n}\r\n}\r\nABCDEF12";
    var s = new ParamFile(t);
    Check(s.Text == t, "text kept");
    Check(s.Main.Str("Name") == "Air AI (BODY)", "name");
    Check(s.Main.Num("Acc") == 1.52, "double dot");
    Check(!s.Main.Bool("CPUSelectable", true), ";) line");
    var b = s.Section("BODY")!;
    Check(b.Int("ModelNum") == 3, "comment ignored");
    Check(b.Numbers("Offset", 3)[1] == -2, "commas");
    Check(b.Numbers("Inertia#2", 3)[2] == 9, "continuation");
    Check(s.Section("AI") == null, "name isn't a section");
    var lf = "Name \"x\"\nWHEEL 0 {\nRadius 2\n}\n";
    var s2 = new ParamFile(lf);
    Check(s2.Section("WHEEL 0")!.Num("Radius") == 2 && s2.Text == lf, "LF only");
}

void ReadPass()
{
    foreach (var e in game.ListCars())
    {
        try
        {
            var c = new Car(game, e.Folder);
            var bytes = File.ReadAllBytes(c.ParamPath);
            Check(CarPose.Build(c.Data).All(p => p.World.M44 == 1 && !float.IsNaN(p.World.M41 + p.World.M11 + p.World.M22 + p.World.M33)), e.Folder + ": parts have sane places");
            Check(!c.Modified, e.Folder + ": fresh isn't modified");
            Check(ParamFile.FileEncoding.GetBytes(c.CurrentText()).SequenceEqual(bytes), e.Folder + ": unchanged = byte identical");
            var r = CarChecker.Check(c);
            Console.WriteLine($"{e.Folder}: {r.Score} ({r.Errors.Count}E {r.Warnings.Count}W)");
            if (GameFolder.IsStock(e.Folder) && r.Errors.Count == 0)
                Check(c.Game.Find(c.Data.Coll) == null || Hull.Load(c.Game.Find(c.Data.Coll)!).Convex.Count >= 0, e.Folder + ": hull reads");
        }
        catch (Exception ex) { Check(false, e.Folder + ": " + ex); }
    }
    foreach (var f in Directory.GetFiles(game.CarsDir, "*.*", SearchOption.AllDirectories))
    {
        var x = Path.GetExtension(f).ToLowerInvariant();
        byte[] bytes;
        try { bytes = File.ReadAllBytes(f); } catch (UnauthorizedAccessException) { Console.WriteLine("no access " + f); continue; }
        if (bytes.Length == 0) continue;
        if (x is ".prm" or ".m")
        {
            var p = Prm.Read(new BinaryReader(new MemoryStream(bytes)));
            var tmp = Path.GetTempFileName();
            p.Save(tmp);
            Check(File.ReadAllBytes(tmp).SequenceEqual(bytes), f + ": model round trip");
            if (p.OtherLods != null)
            {
                var lods = p.OtherLods;
                int seen = 0;
                p.ForOtherLods(_ => seen++);
                Check(seen > 0 && p.OtherLods.SequenceEqual(lods), f + ": LODs read and written back the same");
            }
            File.Delete(tmp);
        }
        else if (x == ".hul")
        {
            var h = Hull.Load(f);
            var tmp = Path.GetTempFileName();
            h.Save(tmp);
            Check(File.ReadAllBytes(tmp).SequenceEqual(bytes), f + ": hull round trip");
            File.Delete(tmp);
        }
    }
}

void WritePass()
{
    foreach (var e in game.ListCars())
    {
        var c = new Car(game, e.Folder);
        var bytes = File.ReadAllBytes(c.ParamPath);
        try
        {
            if (bytes.Length == 0)
            {
                c.Data.Name = "Made from nothing";
                c.Data.BodyMass = 3;
                var made = new ParamFile(c.CurrentText());
                Check(made.Main.Str("Name") == "Made from nothing" && made.Section("BODY")!.Num("Mass") == 3, e.Folder + ": empty file filled");
                continue;
            }
            var before = c.Data.Emit();
            c.Data.TopSpeed = 123.5;
            c.Data.BodyMass = 2.25;
            c.Data.Wheels[2].Offset1 = new Vector3(1, 2, 3);
            c.Data.Inertia[1] = new Vector3(7, 8, 9);
            c.Data.Models[5] = "cars\\x\\y.prm";
            c.Data.TCarBox = "cars\\x\\box.bmp";
            c.Data.Wheels[1].Camber = -5;
            c.Data.Spinner.Type = 6;
            c.Data.Camera = new CameraData { HoodLook = 0.1 };
            var text = c.CurrentText();
            Check(!c.Modified, e.Folder + ": clean after applying");
            var r = new Car(game, e.Folder);
            r.LoadText(text);
            var d = r.Data;
            Check(d.TopSpeed == 123.5 && d.BodyMass == 2.25 && d.Wheels[2].Offset1.Y == 2 && d.Inertia[1].Y == 8, e.Folder + ": values");
            Check(d.Models[5] == "cars\\x\\y.prm" && d.TCarBox == "cars\\x\\box.bmp" && d.Wheels[1].Camber == -5 && d.Spinner.Type == 6, e.Folder + ": values 2");
            Check(d.Camera != null && Math.Abs(d.Camera.HoodLook - 0.1) < 1e-5, e.Folder + ": new section");

            var old = ParamFile.FileEncoding.GetString(bytes).Split('\n');
            var neu = text.Split('\n');
            Check(old.Distinct().Count() - old.Intersect(neu).Count() <= 7, e.Folder + ": other lines kept");
            Check(old[^1] == neu[^1], e.Folder + ": tail kept");
            c.Save();
            Check(File.ReadAllBytes(c.ParamPath + ".bak").SequenceEqual(bytes), e.Folder + ": backup");
            Check(ParamFile.FileEncoding.GetString(File.ReadAllBytes(c.ParamPath)) == text, e.Folder + ": saved");
        }
        catch (Exception ex) { Check(false, e.Folder + ": " + ex); }
        finally
        {
            File.WriteAllBytes(c.ParamPath, bytes);
            if (File.Exists(c.ParamPath + ".bak")) File.Delete(c.ParamPath + ".bak");
        }
    }

    // A new car and every tool on it
    var folder = Path.Combine(game.CarsDir, "testnew");
    if (Directory.Exists(folder)) Directory.Delete(folder, true);
    var n = Car.CreateFrom(game, "adeon", "testnew", "My, New Car");
    Check(n.Data.Models[0].StartsWith("cars\\testnew\\", StringComparison.OrdinalIgnoreCase), "new car path " + n.Data.Models[0]);
    Check(n.Game.Find(n.Data.Models[0]) != null && n.ExternalFiles().Count == 0, "new car self contained");
    Check(n.Data.Models[17].Equals("cars\\misc\\Aerial.m", StringComparison.OrdinalIgnoreCase), "shared aerial");
    Check(!File.Exists(n.ParamPath + ".bak"), "no backup in new car");
    Check(CarChecker.Check(n).Errors.Count == 0, "new car checks");
    Check(CarTools.SameBodyAsStock(n).Equals("adeon", StringComparison.OrdinalIgnoreCase), "body seen as stock");

    Check(CarTools.ComputeInertia(n) && n.Data.Inertia[0].X > 100, "inertia");
    Check(CarTools.FitToModels(n) > 0 && n.Data.Wheels[0].Radius > 3, "fit");
    var shadowPath = CarTools.MakeShadow(n);
    Check(File.Exists(shadowPath) && n.Data.TShadow.EndsWith("shadow.bmp") && n.Data.HasShadowTable, "shadow");
    using (var sh = Images.Load(shadowPath)!)
    {
        Check(sh.Width == 64, "shadow size");
        Check(sh.GetPixel(32, 32).Red > 200 && sh.GetPixel(0, 0).Red < 40, "shadow is white on black");
    }

    var body = CarTools.LoadModel(n, n.Data.BodyModel)!;
    var old2 = body.Faces[0].Colors[0];
    var sw = System.Diagnostics.Stopwatch.StartNew();
    CarTools.AutoShade(body);
    Console.WriteLine($"auto shade {body.Faces.Length} faces: {sw.ElapsedMilliseconds} ms");
    var grays = body.Faces.SelectMany(f => f.Colors).Select(x => x & 255).Distinct().Count();
    Check(grays > 10, "shade has range: " + grays);
    var topFace = body.Faces.MinBy(f => body.Verts[f.V0].Pos.Y)!;
    var lowFace = body.Faces.MaxBy(f => body.Verts[f.V0].Pos.Y)!;
    Check((topFace.Colors[0] & 255) > (lowFace.Colors[0] & 255), "roof lighter than underside");

    var hull = CarTools.MakeHull(body, n.Data.BodyOffset)!;
    Check(hull != null, "hull made");
    var hp = Path.Combine(n.Dir, "made.hul");
    hull!.Save(hp);
    var hl = Hull.Load(hp);
    var ch = hl.Convex[0];
    Console.WriteLine($"made hull: {ch.Points.Length} pts {ch.Edges.Length} edges {ch.Faces.Length} faces {hl.Spheres.Count} spheres");
    Check(ch.Points.Length - ch.Edges.Length + ch.Faces.Length == 2, "hull is a closed solid (Euler)");
    foreach (var f in ch.Faces) foreach (var p in ch.Points) Check(Vector3.Dot(f.Normal, p) + f.D < 0.15f, "points behind planes");
    foreach (var s in hl.Spheres) Check(s.Radius > 0, "sphere");

    foreach (var tpl in Directory.Exists(args.Length > 2 ? args[2] : "") ? Directory.GetFiles(args[2], "*.txt") : Array.Empty<string>())
    {
        var txt = CarTools.FillReadme(File.ReadAllText(tpl, ParamFile.FileEncoding), n, new("Tester", "a@b.c", Comments: "line one\nline two"));
        Check(txt.Contains("My, New Car") && txt.Contains("Tester"), "readme " + tpl);
        Check(!System.Text.RegularExpressions.Regex.IsMatch(txt, "\\[[A-Z]+\\]") && !txt.Contains('%'), "readme tags " + tpl);
    }

    using (var tex = Images.Load(n.Game.Find(n.Data.TPage))!)
    using (var adj = CarTools.AdjustTexture(tex, 0.2f, 1f, 30f))
        Check(adj.Width == tex.Width, "texture adjust");

    n.Save();
    var zip = Path.GetTempFileName() + ".zip";
    n.Pack(zip, true);
    using (var a = System.IO.Compression.ZipFile.OpenRead(zip))
    {
        Check(a.Entries.Any(x => x.FullName == "cars/testnew/parameters.txt"), "zip params");
        Check(!a.Entries.Any(x => x.FullName.EndsWith(".bak")), "zip without backups");
    }
    File.Delete(zip);

    var ren = Path.Combine(game.CarsDir, "testren");
    if (Directory.Exists(ren)) Directory.Delete(ren, true);
    n.Rename("testren");
    var n2 = new Car(game, "testren");
    Check(n2.Data.TPage.StartsWith("cars\\testren\\", StringComparison.OrdinalIgnoreCase) && n2.Game.Find(n2.Data.Models[1]) != null, "rename");
    Directory.Delete(ren, true);

    // Moving by hand: symmetry
    var d2 = new Car(game, "rc").Data;
    d2.Wheels[0].Offset1 = new Vector3(-50, 1, 2);
    CarTools.Symmetrize(d2);
    Check(d2.Wheels[1].Offset1 == new Vector3(50, 1, 2), "symmetrize");
}

// A game folder built here: the same car in the root and in a pack, the way RVGL installs it
void Packs()
{
    var dir = Path.Combine(Path.GetTempPath(), "rvs_packs_" + Guid.NewGuid().ToString("N"));
    void Put(string rel, string text)
    {
        var p = Path.Combine(dir, rel.Replace('\\', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(p)!);
        File.WriteAllText(p, text);
    }
    var param = "{\r\nName \"Pack Car\"\r\nMODEL 0 \"cars\\one\\body.prm\"\r\nTPAGE \"cars\\one\\car.bmp\"\r\nCOLL \"cars\\one\\hull.hul\"\r\nBODY {\r\nModelNum 0\r\nMass 1\r\n}\r\n}\r\n";
    Put("packs\\default.txt", "\"default\"\n\"game_files\"\n;comment\n\"local\" *\n");
    Put("cars\\one\\parameters.txt", "{\r\nName \"Old in the root\"\r\n}\r\n");
    Put("cars\\one\\body.prm", "");
    Put("packs\\game_files\\cars\\one\\parameters.txt", param);
    Put("packs\\game_files\\cars\\one\\car.bmp", "from the pack");
    Put("packs\\game_files\\cars\\only\\parameters.txt", "{\r\nName \"Only in a pack\"\r\n}\r\n");
    Put("cars\\mine\\parameters.txt", "{\r\nName \"Custom\"\r\n}\r\n");
    Put("cars\\mine\\hull.hul", "x");
    try
    {
        var g = new GameFolder(dir);
        Check(g.Layers.Count == 3 && g.WritableLayer.EndsWith("local"), "packlist read");
        var list = g.ListCars().ToList();
        Check(list.Count == 3 && list.Any(c => c.Folder == "only"), "cars from packs listed: " + list.Count);
        Check(list.First(c => c.Folder == "one").Name == "Pack Car", "the pack beats the root");
        var one = new Car(g, "one");
        Check(one.Data.Name == "Pack Car" && one.ParamPath.Contains("game_files"), "read from the pack");
        Check(g.WritePath("cars\\one\\car.bmp").Contains(Path.Combine("packs", "local")), "changes to a pack's file go to local");
        Check(g.WritePath("cars\\one\\body.prm") == Path.Combine(dir, "cars", "one", "body.prm"), "a file only the root has is the one the game reads");
        var packBytes = File.ReadAllBytes(one.ParamPath);
        one.Data.BodyMass = 9;
        one.Save();
        Check(File.ReadAllBytes(Path.Combine(dir, "packs", "game_files", "cars", "one", "parameters.txt")).SequenceEqual(packBytes), "the pack isn't touched");
        Check(new ParamFile(File.ReadAllText(Path.Combine(dir, "cars", "one", "parameters.txt"))).Main.Str("Name") == "Old in the root", "nor the root");
        var again = new Car(g, "one");
        Check(again.Data.BodyMass == 9 && again.ParamPath.Contains(Path.Combine("packs", "local")), "read back from local");
        Check(again.HasLocalChanges, "local changes known");
        again.DropLocalChanges();
        Check(new Car(g, "one").Data.BodyMass == 1, "dropped: back to the pack");
        var mine = new Car(g, "mine");
        mine.Data.BodyMass = 2;
        mine.Save();
        Check(File.Exists(Path.Combine(dir, "cars", "mine", "parameters.txt.bak")) && !Directory.Exists(Path.Combine(dir, "packs", "local", "cars", "mine")), "a root car stays in the root");
        var copy = Car.CreateFrom(g, "one", "fresh", "Fresh");
        Check(File.ReadAllText(Path.Combine(dir, "cars", "fresh", "car.bmp")) == "from the pack" && copy.Data.TPage == "cars\\fresh\\car.bmp", "a copy takes the files the game uses");
        Check(g.EnsureWritable("cars\\one\\car.bmp").Contains(Path.Combine("packs", "local")) && g.Find("cars\\one\\car.bmp")!.Contains(Path.Combine("packs", "local")), "paintable copy");
    }
    finally { Directory.Delete(dir, true); }
}

void NewTools(CarData rc, Hull? hull)
{
    // ---- The calculator against the physics ----
    var calc = CarCalc.Of(rc, hull);
    Console.WriteLine($"rc: front {calc.FrontShare * 100:0}%, sag {calc.FrontSag:0.0}/{calc.RearSag:0.0}, top {calc.TopSpeedReal:0} ({rc.TopEnd:0}), to top {calc.TimeToTop:0.0} s ({rc.Acc:0.0}), {calc.FrontHz:0.0} Hz, damping {calc.FrontDamping:0.00}, slide at {calc.SlideFront:0}");
    if (rc.TopEnd > 0) Check(Math.Abs(calc.TopSpeedReal - rc.TopEnd) / rc.TopEnd < 0.03, $"calculated top speed: {calc.TopSpeedReal:0} for {rc.TopEnd:0}");
    if (rc.Acc > 0) Check(Math.Abs(calc.TimeToTop - rc.Acc) / rc.Acc < 0.25, $"calculated time to top: {calc.TimeToTop:0.0} for {rc.Acc:0.0}");
    var standing = new CarSim(rc, hull);
    for (int i = 0; i < 400; i++) standing.Advance(1 / 60f);
    Check(Math.Abs(standing.Compression[0] - calc.FrontSag) < Math.Max(calc.FrontSag * 0.5f, 1.5f), $"calculated sag: {calc.FrontSag:0.0}, in the simulation {standing.Compression[0]:0.0}");
    var heavier = CarCalc.Of(CarCalc.Scaled(rc, engine: 2));
    Check(heavier.Accel0 > calc.Accel0 * 1.9f, "what-if: twice the engine");
    Check(CarCalc.Of(new CarData()).Warnings.Count > 0, "warns about a car without wheels");
    var soft = CarCalc.Scaled(rc, springs: 0.02f);
    Check(CarCalc.Of(soft).Warnings.Any(w => w.Contains("too soft")), "warns about springs that can't hold the car");

    // ---- Shading ----
    var quad = new Prm { Verts = new PrmVertex[] {
        new() { Pos = new(0, 0, 0), Normal = new(0, -1, 0) }, new() { Pos = new(10, 0, 0), Normal = new(0, -1, 0) },
        new() { Pos = new(10, 0, 10), Normal = new(0, -1, 0) }, new() { Pos = new(0, 0, 10), Normal = new(0, -1, 0) },
        new() { Pos = new(0, 10, 10), Normal = new(0, 0, 1) }, new() { Pos = new(10, 10, 10), Normal = new(0, 0, 1) } } };
    PrmFace Face(short a, short b, short c, short d) => new() { Flags = FaceFlags.Quad, V0 = a, V1 = b, V2 = c, V3 = d };
    quad.Faces = new[] { Face(0, 1, 2, 3), Face(3, 2, 5, 4) };
    var byFace = Shader.CornerNormals(quad, ShadeSmooth.Faces);
    var smooth = Shader.CornerNormals(quad, ShadeSmooth.Smooth);
    var angled = Shader.CornerNormals(quad, ShadeSmooth.ByAngle, 45);
    var wide = Shader.CornerNormals(quad, ShadeSmooth.ByAngle, 120);
    Check(byFace[0][2] != byFace[1][1], "by faces: a shared corner differs");
    Check(smooth[0][2] == smooth[1][1], "smooth: a shared corner agrees");
    Check(angled[0][2] == byFace[0][2], "by angle: a 90 degree edge stays sharp under 45");
    Check(wide[0][2] == smooth[0][2], "by angle: and goes smooth over 90");
    Check(byFace[0][0].Y < -0.9f, "faces know their outside from the model's normals: " + byFace[0][0]);

    var flat = new Prm { Verts = (PrmVertex[])quad.Verts.Clone(), Faces = new[] { Face(0, 1, 2, 3), Face(3, 2, 5, 4) } };
    Shader.Shade(flat, new ShadeOptions(Rays: 12, Smooth: ShadeSmooth.Faces));
    Check(flat.Faces.All(f => f.Colors.All(c => c == f.Colors[0])), "shading by faces: one colour per polygon");
    Check(flat.Faces[0].Colors[0] != flat.Faces[1].Colors[0], "and polygons facing away differ");
    var warm = new Prm { Verts = (PrmVertex[])quad.Verts.Clone(), Faces = new[] { Face(0, 1, 2, 3) } };
    warm.Faces[0].Colors[0] = 0x80FFFFFF;
    Shader.Shade(warm, new ShadeOptions(Rays: 12, SkyColour: new(1, 0.3f, 0.1f), SunColour: new(1, 0.5f, 0.2f)));
    uint wc = warm.Faces[0].Colors[0];
    Check(((wc >> 16) & 255) > (wc & 255) + 20, $"coloured light: {wc:X8}");
    Check((wc >> 24) == 0x80, "alpha stays");

    var verts = new Prm { Verts = (PrmVertex[])quad.Verts.Clone(), Faces = new[] { Face(0, 1, 2, 3), Face(3, 2, 5, 4) } };
    Check(Shader.SetNormals(verts, ShadeSmooth.Faces) && verts.Verts.Length == 8, "normals by faces split the shared vertices: " + verts.Verts.Length);
    Check(verts.Faces.All(f => Enumerable.Range(0, 4).All(k => f.Index(k) < verts.Verts.Length)), "polygons still point at their vertices");
    var together = new Prm { Verts = (PrmVertex[])quad.Verts.Clone(), Faces = new[] { Face(0, 1, 2, 3), Face(3, 2, 5, 4) } };
    Shader.SetNormals(together, ShadeSmooth.Smooth);
    Check(together.Verts.Length <= 6 && together.Verts.Length >= 6, "smooth normals keep the vertex count");
    var saved = Path.Combine(Path.GetTempPath(), "rvs_normals_" + Guid.NewGuid().ToString("N") + ".prm");
    try
    {
        verts.Save(saved);
        var back = Prm.Load(saved);
        Check(back.Verts.Length == 8 && back.Faces[1].V0 == verts.Faces[1].V0, "and it saves");
    }
    finally { File.Delete(saved); }

    // ---- Colours ----
    using (var bmp = new SkiaSharp.SKBitmap(4, 1, SkiaSharp.SKColorType.Bgra8888, SkiaSharp.SKAlphaType.Opaque))
    {
        bmp.SetPixel(0, 0, new SkiaSharp.SKColor(200, 30, 30));
        bmp.SetPixel(1, 0, new SkiaSharp.SKColor(100, 15, 15));
        bmp.SetPixel(2, 0, new SkiaSharp.SKColor(30, 30, 200));
        bmp.SetPixel(3, 0, new SkiaSharp.SKColor(0, 0, 0));
        using var same = CarTools.AdjustTexture(bmp, new PaintOptions());
        Check(Enumerable.Range(0, 4).All(x => { var a = same.GetPixel(x, 0); var b = bmp.GetPixel(x, 0); return Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue) <= 6; }), "no change, no change");
        using var swapped = CarTools.AdjustTexture(bmp, new PaintOptions(From: new SkiaSharp.SKColor(200, 30, 30), To: new SkiaSharp.SKColor(30, 200, 30), Tolerance: 0.3f));
        var px = swapped.GetPixel(0, 0);
        Check(px.Green > 170 && px.Red < 60, "a colour swaps for another: " + px);
        Check(swapped.GetPixel(2, 0).Blue > 170 && swapped.GetPixel(2, 0).Green < 60, "others stay");
        Check(swapped.GetPixel(3, 0) == new SkiaSharp.SKColor(0, 0, 0), "black stays see-through");
        using var tinted = CarTools.AdjustTexture(bmp, new PaintOptions(Colourise: new SkiaSharp.SKColor(30, 200, 30), ColouriseAmount: 1));
        Check(tinted.GetPixel(2, 0).Green > tinted.GetPixel(2, 0).Blue, "colourise turns everything to one colour");
        using var hard = CarTools.AdjustTexture(bmp, new PaintOptions(Contrast: 2));
        Check(hard.GetPixel(0, 0).Red >= bmp.GetPixel(0, 0).Red && hard.GetPixel(1, 0).Red <= bmp.GetPixel(1, 0).Red, "contrast");
        using var warmer = CarTools.AdjustTexture(bmp, new PaintOptions(Warmth: 1));
        Check(warmer.GetPixel(2, 0).Blue < bmp.GetPixel(2, 0).Blue, "warmth");
    }
    Check(new PaintOptions().IsPlain && !new PaintOptions(Contrast: 1.2f).IsPlain, "knows when nothing is asked");

    // ---- Race, with you driving ----
    {
        var track = RaceTrack.Make(0);
        var race = new Race(track, 1);
        var me = race.Add("Me", "rc", rc.Clone(), hull, true);
        race.Grid();
        race.PlayerThrottle = 1;
        // The game's countdown: five seconds of 8 ms steps, the last three shown, and nobody leaves the grid
        Check(Race.CountdownTime == 5 && Math.Abs(race.Countdown - 5) < 1e-4f && race.CountdownLabel == "", "the countdown starts at 5 s, nothing shown yet: " + race.Countdown);
        int waited = 0;
        var grid = me.Sim.Pos;
        string shown = "";
        race.Reposition(me);
        Check(!me.Repositioning, "no reposition before the green");
        while (!race.Started && waited < 2000)
        {
            race.Step();
            waited++;
            if (race.CountdownLabel != "" && !shown.EndsWith(race.CountdownLabel)) shown += race.CountdownLabel;
        }
        Check(waited == 625, $"the race starts after 625 steps of 8 ms: {waited}");
        Check(shown == "321GO", "the last three seconds are shown, then GO: " + shown);
        Check(race.Time == 0 && race.Countdown == 0, "race time starts at the green");
        var moved = me.Sim.Pos - grid;
        Check(Math.Abs(moved.X) < 1 && Math.Abs(moved.Z) < 1 && me.Sim.SpeedMph < 1, "full throttle on the grid goes nowhere: " + moved);
        var from = me.Sim.Pos;
        for (int i = 0; i < 200; i++) race.Step();
        Check((me.Sim.Pos - from).Length() > 100, "your throttle drives your car: " + (me.Sim.Pos - from).Length());
        Check(me.Run > -270, "and counts as progress: " + me.Run);

        // Full throttle with the wheel turned: the wall has to hold it, whatever happens
        race.PlayerSteer = 1;
        float worst = 0;
        for (int i = 0; i < 2500; i++)
        {
            race.Step();
            var side = RaceTrack.Side(track.Tangents[me.Hint]);
            worst = Math.Max(worst, Math.Abs(System.Numerics.Vector3.Dot(me.Sim.Pos - track.Points[me.Hint], side)));
        }
        Check(worst < track.Width / 2 + 40, $"walls hold a car in: {worst:0} of {track.Width / 2:0}");
        Check(!float.IsNaN(me.Sim.Pos.X) && me.Hits > 0, "after " + me.Hits + " hits");
        // R, as the game has it: two seconds, the car put back when one of them is over
        race.PlayerThrottle = race.PlayerSteer = 0;
        var node = me.Brain.Cur ?? me.Brain.LastValid;
        Check(node != null, "your car's node is followed too");
        race.Reposition(me);
        Check(me.Repositioning && Math.Abs(me.ReposTimer - Race.RepositionTime) < 1e-4f && Race.RepositionTime == 2, "R starts a reposition of 2 s");
        int steps = 0, placedAt = -1;
        float jump = 0;
        while (me.Repositioning && steps < 1000)
        {
            var was = me.Sim.Pos;
            race.Step();
            steps++;
            float d = (me.Sim.Pos - was).Length();
            if (d > jump) { jump = d; placedAt = steps; node = me.Brain.Cur; }
            if (steps == 60) race.Reposition(me); // a second press changes nothing
        }
        Check(steps == 250, $"it takes 250 steps: {steps}");
        Check(placedAt >= 125 && placedAt <= 127, $"the car is put back at the half: step {placedAt}, {jump:0} units");
        var wanted = System.Numerics.Vector3.Lerp(node!.Centre, node.Next.Centre, 100 / node.Dist);
        for (int i = 0; i < 150; i++) race.Step();
        var off = me.Sim.Pos - wanted;
        Check(Math.Sqrt(off.X * off.X + off.Z * off.Z) < 40 && me.Sim.Up.Y < -0.9f, $"on the racing line, 100 units on from its node, on its wheels: {off}");
        var nose = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, me.Sim.Rot);
        Check(System.Numerics.Vector3.Dot(nose, node.Forward) > 0.95f, "facing the way of the race");
    }

    // ---- The road's AI nodes ----
    for (int t = 0; t < RaceTrack.Names.Length; t++)
    {
        var track = RaceTrack.Make(t);
        var net = new AiNet(track);
        bool linked = true, across = true, inside = true, lines = true;
        float longest = 0, shortest = float.MaxValue, lean = 0;
        foreach (var n in net.Nodes)
        {
            if (n.Next.Prev != n || n.Prev.Next != n) linked = false;
            if (Math.Abs(n.Width - track.Width) > 1) across = false;
            if (n.Racing < 0.2f || n.Racing > 0.8f || n.Overtake < 0.2f || n.Overtake > 0.8f) lines = false;
            // The racing line's own point is on the road, and over this node's stretch
            if (!n.Over(System.Numerics.Vector3.Lerp(n.Centre, n.Next.Centre, 0.5f)) || n.EdgeLeft(n.Centre) < 150 || n.EdgeRight(n.Centre) < 150) inside = false;
            if (Math.Abs(n.EdgeLeft(n.Centre) + n.EdgeRight(n.Centre) - track.Width) > track.Width * 0.08f) inside = false;
            longest = Math.Max(longest, n.Dist);
            shortest = Math.Min(shortest, n.Dist);
            lean = Math.Max(lean, Math.Abs(n.Racing - 0.5f));
        }
        var walk = net.Nodes[0];
        int ring = 0;
        do { walk = walk.Next; ring++; } while (walk != net.Nodes[0] && ring < 10000);
        Check(linked && ring == net.Nodes.Count, $"{track.Name}: {net.Nodes.Count} AI nodes in one ring");
        Check(across && inside && lines, $"{track.Name}: nodes span the road, lines stay on it");
        Check(shortest > 150 && longest < 450, $"{track.Name}: nodes {shortest:0} to {longest:0} apart");
        Check(lean > 0.1f && net.Zones >= 4, $"{track.Name}: the racing line leans into bends by {lean:0.00}, {net.Zones} zones");
    }

    // ---- The CPU driver, as the game's ----
    {
        var track = RaceTrack.Make(0);
        var race = new Race(track, 1);
        var me = race.Add("Me", "rc", rc.Clone(), hull, true);
        var cpu = race.Add("Cpu", "rc", rc.Clone(), hull, false, 0.7f);
        race.Grid();
        Check(race.Difficulty == RaceDifficulty.VeryEasy && Race.DifficultyFor(1.05f) == RaceDifficulty.Hard, "skill picks the game's difficulty");
        Check(cpu.Sim.Pos != me.Sim.Pos && cpu.Run > me.Run, "you start at the back");
        while (!race.Started) race.Step();
        Check(cpu.Brain.State == CpuBrain.Mode.Race && cpu.Brain.Dy == -127 && cpu.Sim.SpeedMph < 1, "a CPU car holds full throttle through the countdown, and stays");
        // You stay on the grid. On the long straight the CPU car only ever asks for full throttle
        int thoughts = 0, lastDx = int.MinValue, changes = 0;
        bool eights = true, floored = true;
        for (int i = 0; i < 4 * 125; i++)
        {
            race.Step();
            thoughts++;
            var b = cpu.Brain;
            if (b.Dx != lastDx) { changes++; lastDx = b.Dx; }
            if (b.Dx % 8 != 0 && Math.Abs(b.Dx) != 127) eights = false;
            if (b.Dy != -127 && cpu.Run < 4000) floored = false;
        }
        Check(eights, "its steering moves in steps of 8");
        Check(changes < thoughts / 4, $"it thinks 30 times a second, not every step: {changes} changes in {thoughts} steps");
        Check(floored, "it never lifts on a straight");
        Check(cpu.Brain.Line < 0.05f, "alone, it's on the racing line: " + cpu.Brain.Line);
        // Far ahead of you it's held back: 2 mph less top speed for each 600 units, at the easiest level
        Check(cpu.Run - me.Run > 1300 && cpu.Sim.TopSpeedMph < (float)cpu.Data.TopSpeed - 1.5f && cpu.Sim.CatchFriction == 1, $"far ahead it loses top speed, not grip: {cpu.Sim.TopSpeedMph:0.0} of {cpu.Data.TopSpeed:0.0} mph");
    }
    {
        // Behind you by more than 2000 units, it's helped: more top speed and grip
        var track = RaceTrack.Make(0);
        var race = new Race(track, 3);
        var me = race.Add("Me", "rc", rc.Clone(), hull, true);
        var cpu = race.Add("Cpu", "rc", rc.Clone(), hull, false, 0.7f);
        race.Grid();
        while (!race.Started) race.Step();
        me.Run += 5000; // as if you were well ahead
        for (int i = 0; i < 20; i++) race.Step();
        Check(cpu.Sim.TopSpeedMph > (float)cpu.Data.TopSpeed + 0.9f && cpu.Sim.CatchFriction > 1.04f, $"far behind it's helped: {cpu.Sim.TopSpeedMph:0.0} mph, grip x{cpu.Sim.CatchFriction:0.000}");
        me.Run -= 5000;

        // A CPU car that goes nowhere is put back by the game's own watch: within 2 x 5 s at this level, then the 2 s reposition
        int n = 0, began = -1, ended = -1;
        while (n < 3000 && ended < 0)
        {
            race.Step();
            n++;
            if (cpu.Repositioning && began < 0) began = n;
            if (!cpu.Repositioning && began >= 0) ended = n;
            if (began < 0 && n > 300) cpu.Sim.Vel = cpu.Sim.AngVel = System.Numerics.Vector3.Zero; // held, as if wedged
        }
        Check(began > 300 && began <= 300 + 1260, $"a CPU car that goes nowhere is repositioned: after {(began - 300) * 0.008f:0.0} s");
        // It starts within a step, so the first of its 250 steps is over when it's first seen
        Check(ended - began + 1 == 250, $"which takes the game's 2 s: {(ended - began + 1) * 0.008f:0.00} s");
        for (int i = 0; i < 300; i++) race.Step();
        Check(cpu.Brain.Cur != null && cpu.Sim.SpeedMph > 5 && cpu.Brain.FaceDir > 0, $"and it drives on: {cpu.Sim.SpeedMph:0.0} mph");
    }

    // Turned round on the road, it works its way back with the game's three-point turn
    {
        var track = RaceTrack.Make(0);
        var race = new Race(track, 1) { AiDrivesPlayer = true };
        var me = race.Add("Me", "rc", rc.Clone(), hull, true);
        race.Grid();
        while (!race.Started) race.Step();
        me.Sim.Place(track.Points[10], MathF.Atan2(-track.Tangents[10].X, -track.Tangents[10].Z));
        var modes = new HashSet<CpuBrain.Mode>();
        int turned = -1;
        for (int i = 0; i < 2500 && turned < 0; i++)
        {
            race.Step();
            modes.Add(me.Brain.State);
            if (i > 50 && me.Brain.State == CpuBrain.Mode.Race && me.Brain.FaceDir > 0 && me.Sim.SpeedForward > 300) turned = i;
        }
        Check(modes.Contains(CpuBrain.Mode.Trouble) && modes.Any(m => m >= CpuBrain.Mode.ForwardLeft), "facing the wrong way is trouble: " + string.Join(", ", modes));
        Check(turned > 0 && !me.Repositioning, $"it turns round by itself in {turned * 0.008f:0.0} s");
    }

    // Close behind another car it takes the other line, as the game's overtaking does
    {
        var race = new Race(RaceTrack.Make(0), 1) { AiDrivesPlayer = true };
        race.Add("Me", "rc", rc.Clone(), hull, true);
        race.Add("A", "rc", rc.Clone(), hull, false);
        race.Add("B", "rc", rc.Clone(), hull, false);
        race.Grid();
        bool other = false;
        while (race.Time < 6)
        {
            race.Step();
            if (race.Cars.Any(c => c.Brain.Overtake && c.Brain.LineTarget == 1)) other = true;
        }
        Check(other, "a car close behind another goes for the overtaking line");
        Check(race.Cars.All(c => c.Brain.Line >= 0 && c.Brain.Line <= 1), "lines stay between the two");
    }

    // ---- Grounds ----
    foreach (var ground in new[] { RaceGround.Bumpy, RaceGround.Ramp, RaceGround.Materials })
    {
        var track = RaceTrack.Make(0, ground);
        var race = new Race(track, 1) { AiDrivesPlayer = true };
        var me = race.Add("Me", "rc", rc.Clone(), hull, true);
        race.Add("Other", "rc", rc.Clone(), hull, false, 0.9f);
        race.Grid();
        var seen = new HashSet<string>();
        bool flew = false, groundNan = false;
        float highest = 0;
        while (!race.AllDone && race.Time < 200)
        {
            race.Step();
            seen.Add(me.Sim.SurfaceName);
            if (race.Started && me.Sim.Touching.All(t => !t)) flew = true;
            highest = Math.Max(highest, -me.Sim.Pos.Y);
            if (race.Cars.Any(c => float.IsNaN(c.Sim.Pos.X) || float.IsNaN(c.Sim.Vel.X))) { groundNan = true; break; }
        }
        Check(!groundNan, $"{ground}: no NaN");
        Console.WriteLine($"  {ground}: {me.FinishTime:0.0} s, {me.Sim.SurfaceName}, surfaces {seen.Count}, highest {highest:0}, hits {me.Hits}");
        Check(race.AllDone, $"{ground}: the race finishes in {race.Time:0.0} s");
        if (ground == RaceGround.Ramp) Check(flew && highest > 60, $"the ramp makes a jump: {highest:0} high");
        if (ground == RaceGround.Materials) Check(seen.Count >= Material.Track.Length - 1, "it drives over all the surfaces: " + string.Join(", ", seen));
    }
    var plain = RaceTrack.Make(0);
    Check(!RaceTrack.GroundNames.Contains("Materials") && plain.SurfaceAt(0, 0) == Material.Default && plain.Height(0, 800) == 0, "Materials isn't offered, and plain stays plain");
    var hiddenTrack = RaceTrack.Make(0, RaceGround.Materials);
    Check(Enumerable.Range(0, hiddenTrack.Points.Length).Select(i => hiddenTrack.Band[i]).Distinct().Count() == Material.Track.Length, "each surface has its own part of the lap");

    // Ice has to be slippery: the same car on the same bend, with the game's numbers
    float SideG(Material m)
    {
        var s = new CarSim(rc, hull) { World = CollWorld.Flat(m), Throttle = 1 };
        for (int i = 0; i < 350; i++) s.StepOnce();
        s.Steer = 1;
        float best = 0;
        for (int i = 0; i < 500; i++)
        {
            s.StepOnce();
            best = Math.Max(best, Math.Abs(s.AngVel.Y) * s.Vel.Length() / CarCalc.Gravity);
        }
        return best;
    }
    float onRoad = SideG(Material.Default), onIce = SideG(Material.Track.First(m => m.Name == "Ice"));
    Console.WriteLine($"  sideways grip: road {onRoad:0.00} g, ice {onIce:0.00} g");
    Check(onIce < onRoad * 0.7f, $"ice holds less than road: {onIce:0.00} against {onRoad:0.00}");

    // ---- The game's collision rules ----
    var wall = CollPoly.Make(new[] { new Vector3(0, 0, 0), new Vector3(100, 0, 0), new Vector3(100, 0, 100), new Vector3(0, 0, 100) }, CollWorld.Up);
    Check(wall.Touch(new(50, -20, 50), new(50, -9, 50), 10, out var pn, out _, out _, out var pd) && pd < 0 && pn == wall.Normal, "a sphere sinking into a face: " + pd);
    Check(!wall.Touch(new(50, -20, 50), new(50, -13, 50), 10, out _, out _, out _, out _), "not when it's over 2 units clear");
    Check(wall.Touch(new(106, -5, 50), new(106, -5, 50), 10, out var en, out _, out var ep, out _) && Math.Abs(ep.X - 100) < 0.01f && en.X > 0.5f, "over an edge it pushes out from the edge: " + en);
    Check(!wall.Touch(new(106, -5, 106), new(106, -5, 106), 10, out _, out _, out _, out _), "a corner doesn't count, as in the game");
    Check(!wall.Touch(new(50, 60, 50), new(50, 60, 50), 10, out _, out _, out _, out _), "from far behind, nothing");
    Check(Material.Game.Length == 27 && Material.Game[10].Name == "Ice" && Material.Game[26].Name == "Paving" && Material.Game[8].Name == "Boundary", "materials in the order of an .ncp");

    // On its roof with no input, it comes to rest and is held: the game's Stacked. On its wheels it never is
    var roof = new CarSim(rc, hull);
    roof.Place(System.Numerics.Vector3.Zero, 0);
    roof.Rot = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, MathF.PI);
    for (int i = 0; i < 1500; i++) roof.StepOnce();
    Check(roof.Stacked && roof.SpeedMph == 0, $"on its roof it comes to rest: {roof.SpeedMph} mph");
    var parked = new CarSim(rc, hull);
    for (int i = 0; i < 1500; i++) parked.StepOnce();
    Check(!parked.Stacked, "on its wheels it never is");

    // The game has no skill for a driver: all of them drive alike, and a lap alone comes out the same
    float Solo(float skill)
    {
        var race = new Race(RaceTrack.Make(1), 1) { AiDrivesPlayer = true };
        race.Add("Solo", "rc", rc.Clone(), hull, true, skill);
        race.Grid();
        while (!race.AllDone && race.Time < 150) race.Step();
        return race.Player.FinishTime;
    }
    float quick = Solo(1f), slow = Solo(0.65f);
    Console.WriteLine($"  solo lap of Park: {quick:0.0} s");
    Check(quick == slow && quick < 60, $"alone, a lap is the same whatever the skill: {slow:0.0} and {quick:0.0}");

    // ---- Race ----
    for (int t = 0; t < RaceTrack.Names.Length; t++)
    {
        var track = RaceTrack.Make(t);
        Check(track.Length > 8000 && track.Points.Length > 50, $"{track.Name}: {track.Length:0} units long");
        // A road that crosses itself or folds up would be useless
        float tightest = track.Radius.Min();
        Check(tightest > track.Width * 0.45f, $"{track.Name}: tightest bend {tightest:0}");
        var race = new Race(track, 1);
        race.Add("Me", "rc", rc.Clone(), hull, true);
        race.Add("A", "rc", rc.Clone(), hull, false, 0.95f);
        race.Add("B", "rc", rc.Clone(), hull, false, 0.9f);
        race.AiDrivesPlayer = true;
        race.Grid();
        Check(race.Cars.Select(c => c.Sim.Pos).Distinct().Count() == 3, "each starts in its own place");
        var startGap = (race.Cars[0].Sim.Pos - race.Cars[1].Sim.Pos).Length();
        Check(startGap > race.Cars[0].HalfWidth, "with room: " + startGap);
        var last = race.Cars[1].Run;
        bool forward = true, nan = false;
        while (race.Countdown > 0) race.Step();
        Check(race.Cars[1].Sim.SpeedMph < 1, "nobody moves before the start");
        float limit = 200;
        while (!race.AllDone && race.Time < limit)
        {
            race.Step();
            // One step of the road is 90: backing out of a bump is fine, a jump along the road isn't, but for a reposition
            if (race.Cars[1].Run < last - 150 && !race.Cars[1].Repositioning) forward = false;
            last = race.Cars[1].Run;
            if (race.Cars.Any(c => float.IsNaN(c.Sim.Pos.X) || float.IsNaN(c.Sim.Vel.X) || float.IsNaN(c.Sim.Steer))) { nan = true; break; }
        }
        Check(!nan, $"{track.Name}: no NaN");
        foreach (var c in race.Standings) Console.WriteLine($"  {track.Name}: {c.Name} {c.FinishTime:0.0} s, best lap {c.BestLap:0.0}, hits {c.Hits}, ran {c.Run:0} of {track.Length * race.Laps:0}");
        Check(race.AllDone, $"{track.Name}: all CPU cars finish in {race.Time:0.0} s");
        Check(forward, $"{track.Name}: nobody jumps along the road");
        var best = race.Cars.Min(c => c.FinishTime);
        Check(best > track.Length / ((float)rc.TopSpeed * CarSim.MphToUnits), "and not faster than light: " + best);
    }
}

void Fit()
{
    var rows = new List<(string Car, double TopReal, double AccReal, float Top, float Acc)>();
    foreach (var e in game.ListCars().Where(x => x.Stock))
    {
        var c = new Car(game, e.Folder);
        var d = c.Data;
        if (d.TopEnd <= 0 || d.Acc <= 0 || !d.Wheels.Any(w => w.Present)) continue;
        Hull? hull = null;
        try { if (game.Find(d.Coll) is { } hullFile) hull = Hull.Load(hullFile); } catch (Exception) { }
        var r = CarSim.RunAcceleration(d, hull);
        rows.Add((e.Folder, d.TopEnd, d.Acc, r.TopEnd, r.Acc));
        var bumps = CarSim.RunBumps(d, hull);
        var circle = CarSim.RunCircle(d, hull);
        Console.WriteLine($"{e.Folder,-9} TopEnd {d.TopEnd,6:0} sim {r.TopEnd,6:0}   Acc {d.Acc,5:0.0} sim {r.Acc,5:0.0}   {circle.Note}; {bumps.Note}");
    }
    rows = rows.Where(r => r.AccReal < 20 && r.TopReal > 1000).ToList();
    double MeanErr(Func<(string Car, double TopReal, double AccReal, float Top, float Acc), double> f) => rows.Average(f);
    Console.WriteLine($"{rows.Count} cars: top speed mean error {MeanErr(r => Math.Abs(r.Top - r.TopReal) / r.TopReal) * 100:0.0}%, acceleration mean error {MeanErr(r => Math.Abs(r.Acc - r.AccReal) / r.AccReal) * 100:0.0}%");
}

Parser();
Packs();
if (game.Find("cars\\rc\\parameters.txt") != null)
{
    var flipped = new CarSim(new Car(game, "rc").Data);
    flipped.Advance(0.5f);
    flipped.Pos += new System.Numerics.Vector3(300, 0, 200);
    var heading = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY, 0.6f);
    flipped.Rot = System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ, MathF.PI) * heading;
    Check(flipped.Up.Y > 0.9f, "on its roof");
    var noseBefore = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, flipped.Rot);
    Check(flipped.Flip(), "it flips when upside down");
    for (int i = 0; i < 120; i++) flipped.Advance(1 / 60f);
    var nose = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, flipped.Rot);
    Check(flipped.Up.Y < -0.95f, "Space puts it on its wheels: " + flipped.Up);
    Check(Math.Abs(MathF.Atan2(nose.X, nose.Z) - MathF.Atan2(noseBefore.X, noseBefore.Z)) < 0.1f && Math.Abs(noseBefore.X) > 0.3f, "still facing the same way: " + nose);
    Check(Math.Abs(flipped.Pos.X - 300) < 30 && flipped.Touching.Any(t => t), "and stays where it was, on the floor: " + flipped.Pos);
    Check(!flipped.Righting && !flipped.Flip(), "not when it's on its wheels, as in the game");

    // The physics against what's known of the real car, with nothing fitted
    var rcData = new Car(game, "rc").Data;
    Hull? rcHull = null;
    try { if (game.Find(rcData.Coll) is { } rcFile) rcHull = Hull.Load(rcFile); } catch (Exception) { }
    var still = new CarSim(rcData, rcHull);
    for (int i = 0; i < 250; i++) still.Advance(1 / 60f);
    Check(still.Touching.All(t => t) && still.SpeedMph < 0.5f, "stands still on its four wheels: " + still.SpeedMph);
    var settled = still.Pos;
    for (int i = 0; i < 300; i++) still.Advance(1 / 60f);
    Check((still.Pos - settled).Length() < 3f, "practically still, under 0.01 mph: " + (still.Pos - settled));
    var run = CarSim.RunAcceleration(rcData, rcHull);
    if (rcData.TopEnd > 0) Check(Math.Abs(run.TopEnd - rcData.TopEnd) / rcData.TopEnd < 0.02, $"top speed like the real RC: {run.TopEnd:0} for {rcData.TopEnd:0}");
    var turning = new CarSim(rcData, rcHull) { Throttle = 1 };
    for (int i = 0; i < 180; i++) turning.Advance(1 / 60f);
    turning.Steer = 1;
    for (int i = 0; i < 60; i++) turning.Advance(1 / 60f);
    var noseAfter = System.Numerics.Vector3.Transform(System.Numerics.Vector3.UnitZ, turning.Rot);
    Check(noseAfter.X > 0.2f, "steering right turns it right: " + noseAfter);
    NewTools(rcData, rcHull);
}
switch (mode)
{
    case "read": ReadPass(); break;
    case "write": WritePass(); break;
    case "fit": Fit(); break;
    case "curve":
        {
            // The schedule of the scripted RVGL run: throttle, coast, throttle, then full lock
            var d = new Car(game, args.Length > 2 ? args[2] : "rc").Data;
            Hull? h = null;
            try { if (game.Find(d.Coll) is { } hf) h = Hull.Load(hf); } catch (Exception) { }
            var s = new CarSim(d, h);
            for (int i = 0; i < 125; i++) s.StepOnce();
            var parts = (args.Length > 3 ? args[3] : "throttle:5,coast:2.2,throttle:1.5,turn:2.6").Split(',').Select(x =>
            {
                var bits = x.Split(':');
                return (Name: bits[0], Seconds: float.Parse(bits[1], System.Globalization.CultureInfo.InvariantCulture), Throttle: bits[0] == "coast" ? 0f : 1f, Steer: bits[0] == "turn" ? 1f : 0f);
            }).ToArray();
            float clock = 0, yaw = 0;
            foreach (var (name, seconds, throttle, steer) in parts)
            {
                s.Throttle = throttle;
                s.Steer = steer;
                for (float t = 0; t < seconds; t += 0.008f)
                {
                    s.StepOnce();
                    clock += 0.008f;
                    yaw += s.AngVel.Y * 0.008f;
                    if ((int)(clock / 0.008f) % 25 == 0)
                    {
                        var (front, rear) = s.SideSlip();
                        Console.WriteLine($"{name,-10} {clock:0.00} s  {s.SpeedMph,5:0.0} mph  turned {yaw * 57.3f,7:0} deg  slide {CarCalc.ToMph(rear),5:0.0} mph");
                    }
                }
            }
            break;
        }
    case "launch":
        {
            // Starts the real game for some seconds, with a settings folder given here
            var run = new TestRun(game, args[2]);
            var levels = run.Levels().ToList();
            Console.WriteLine(levels.Count + " levels, e.g. " + string.Join(", ", levels.Take(3).Select(l => l.Name)));
            Check(levels.Count > 0, "levels found");
            Check(run.FindExe() != null, "RVGL found: " + run.FindExe());
            var track = levels.First(l => l.Folder.Equals("nhood1", StringComparison.OrdinalIgnoreCase));
            Console.WriteLine("track: " + track.Name);
            var pr = run.Start("adeon", track.Folder, false, 4, 1);
            var reached = TestRun.DriveToRace(pr, new RaceSetup("Adeon", track.Name, 4, 1), Console.WriteLine).GetAwaiter().GetResult();
            Check(reached, "went through the menus");
            Thread.Sleep(9000);
            pr.Refresh();
            Check(!pr.HasExited, "RVGL still running");
            Console.WriteLine("window: " + pr.MainWindowTitle);
            TestRun.Screenshot(pr);
            Thread.Sleep(1500);
            if (!pr.HasExited) pr.Kill();
            var pics = Directory.GetFiles(Path.Combine(args[2], "profiles"), "*.png", SearchOption.AllDirectories);
            Console.WriteLine("pictures: " + string.Join(" ", pics));
            var log = Path.Combine(args[2], "profiles", "re-volt.log");
            Check(File.Exists(Path.Combine(args[2], "profiles", TestRun.Profile, "profile.ini")), "own profile made");
            var ini = File.ReadAllText(Path.Combine(args[2], "profiles", TestRun.Profile, "profile.ini"));
            Check(ini.Contains("[Controller1]"), "your controls came along");
            break;
        }
}
Console.WriteLine($"passed {pass}, failed {fail}");
return fail == 0 ? 0 : 1;
