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

void Fit()
{
    if (args.Length > 2) CarSim.EngineScale = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
    if (args.Length > 3) CarSim.RollScale = float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
    var rows = new List<(string Car, double TopReal, double AccReal, float Top, float Acc)>();
    foreach (var e in game.ListCars().Where(x => x.Stock))
    {
        var c = new Car(game, e.Folder);
        var d = c.Data;
        if (d.TopEnd <= 0 || d.Acc <= 0 || !d.Wheels.Any(w => w.Present)) continue;
        var r = CarSim.RunAcceleration(d);
        rows.Add((e.Folder, d.TopEnd, d.Acc, r.TopEnd, r.Acc));
        var bumps = CarSim.RunBumps(d);
        var circle = CarSim.RunCircle(d);
        Console.WriteLine($"{e.Folder,-9} TopEnd {d.TopEnd,6:0} sim {r.TopEnd,6:0}   Acc {d.Acc,5:0.0} sim {r.Acc,5:0.0}   {circle.Note}; {bumps.Note}");
    }
    rows = rows.Where(r => r.AccReal < 20 && r.TopReal > 1000).ToList();
    double MeanErr(Func<(string Car, double TopReal, double AccReal, float Top, float Acc), double> f) => rows.Average(f);
    Console.WriteLine($"{rows.Count} cars: top speed mean error {MeanErr(r => Math.Abs(r.Top - r.TopReal) / r.TopReal) * 100:0.0}%, acceleration mean error {MeanErr(r => Math.Abs(r.Acc - r.AccReal) / r.AccReal) * 100:0.0}%");
}

Parser();
switch (mode)
{
    case "read": ReadPass(); break;
    case "write": WritePass(); break;
    case "fit": Fit(); break;
    case "launch":
        {
            // Starts the real game for some seconds, with a settings folder given here
            var run = new TestRun(game, args[2]);
            var levels = run.Levels().ToList();
            Console.WriteLine(levels.Count + " levels, e.g. " + string.Join(", ", levels.Take(3).Select(l => l.Name)));
            Check(levels.Count > 0, "levels found");
            Check(run.FindExe() != null, "RVGL found: " + run.FindExe());
            var pr = run.Start("adeon", "nhood1", false);
            Thread.Sleep(9000);
            pr.Refresh();
            Check(!pr.HasExited, "RVGL still running");
            Console.WriteLine("window: " + pr.MainWindowTitle);
            if (!pr.HasExited) pr.Kill();
            var log = Path.Combine(args[2], "profiles", "re-volt.log");
            Check(File.Exists(Path.Combine(args[2], "profiles", TestRun.Profile, "profile.ini")), "own profile made");
            var ini = File.ReadAllText(Path.Combine(args[2], "profiles", TestRun.Profile, "profile.ini"));
            Check(ini.Contains("CarDir1 = \"adeon\"") && ini.Contains("LevelDir = \"nhood1\""), "car and level picked");
            Check(ini.Contains("[Controller1]"), "your controls came along");
            break;
        }
}
Console.WriteLine($"passed {pass}, failed {fail}");
return fail == 0 ? 0 : 1;
