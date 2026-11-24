using System.Numerics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// RvCarStudio -rvpath COPY --release MODELS SHOTS: the two release tests, through the menus a car maker uses.
/// 1. A new car from scratch: body.obj, wheel.obj and texture.png from another program, New car, Original, then the
///    tools, Save. 2. An existing car changed: a stock car's engine, body and paint, saved on top of the game's files.
/// RVGL races both afterwards (a script does that, on the same copy).
/// </summary>
public static partial class AutoScenario
{
    public const string ReleaseFolder = "siennagt", ReleaseName = "Sienna GT", EditedCar = "cougar";

    static MenuItem Menu(MainWindow w, string header) =>
        w.GetLogicalDescendants().OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == header)
        ?? throw new InvalidOperationException("No menu item " + header);

    static async Task Click(MainWindow w, string header, int wait = 300)
    {
        Menu(w, header).RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
        await Wait(wait);
        Log("menu: " + header + " -> " + w.StatusText.Text);
    }

    /// <summary>Clicks a button of the dialog in front, as a person would</summary>
    static async Task<bool> Answer(MainWindow w, string button)
    {
        for (int i = 0; i < 20; i++)
        {
            var b = w.OwnedWindows.SelectMany(o => o.GetLogicalDescendants().OfType<Button>())
                .FirstOrDefault(x => (x.Content as string) == button);
            if (b != null) { b.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Wait(300); return true; }
            await Wait(150);
        }
        return false;
    }

    static async Task Turnaround(MainWindow w, string name)
    {
        w.View.SetView(ViewMode.Perspective);
        foreach (var (yaw, tag) in new[] { (0.75f, "a"), (0.75f + MathF.PI, "b"), (MathF.PI / 2, "side"), (0f, "front") })
        {
            w.View.Yaw = yaw; w.View.Pitch = tag == "side" || tag == "front" ? 0.08f : 0.32f; w.View.FrameCar();
            w.View.Redraw();
            await Wait(120);
            await GlShot(w, name + "_" + tag, 720, true);
        }
    }

    public static async Task RunRelease(MainWindow w, string models, string shotDir)
    {
        Running = true;
        shots = shotDir;
        Directory.CreateDirectory(shots);
        log = new StreamWriter(Path.Combine(shots, "release.txt"));
        var d = w.Doc;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("CRASH " + e.ExceptionObject);
        try
        {
            await Wait(1500);
            var game = d.Game!;

            // ---------------- 1. From scratch
            var newCar = Click(w, "New car...", 0);
            await Wait(600);
            var wiz = w.OwnedWindows.OfType<NewCarWizard>().FirstOrDefault();
            Check(wiz != null, "File, New car opens the wizard");
            wiz!.Fill(NewCarKind.Original, "toyeca", ReleaseName,
                Path.Combine(models, "body.obj"), Path.Combine(models, "wheel.obj"), Path.Combine(models, "texture.png"));
            await Wait(200);
            Check(wiz.Problem == "", "the wizard takes .obj models and a .png: " + wiz.Problem);
            wiz.Confirm();
            await newCar;
            await Wait(800);
            Check(d.Car?.Folder == ReleaseFolder, "the new car is open: " + d.Car?.Folder + " / " + w.StatusText.Text);
            if (d.Car?.Folder != ReleaseFolder) return;
            var dd = d.D!;
            Check(d.Model(dd.BodyModel) is { } bm && bm.Faces.Length > 500, "body from the .obj");
            Check(dd.Wheels.All(x => x.Present && d.Model(x.Model) != null), "four wheels from the .obj");
            var tex = game.Find(dd.TPage);
            Check(tex != null && File.ReadAllBytes(tex).Take(2).SequenceEqual("BM"u8.ToArray()), "texture is a real .bmp: " + tex);
            await Turnaround(w, "new_imported");

            // The tools a car maker runs, from the Tools menu
            await Click(w, "Fit radius, skid and lengths to the models");
            await Click(w, "Compute inertia from the body");
            await Click(w, "Make collision hull", 600);
            Check(d.Hull != null && d.Hull.Convex.Count >= 1 && d.Hull.Spheres.Count >= 1, "a hull");
            await Click(w, "Auto shade the car", 0);
            for (int i = 0; i < 100 && !(w.StatusText.Text ?? "").StartsWith("Shaded"); i++) await Wait(100);
            Check((w.StatusText.Text ?? "").StartsWith("Shaded"), "Auto shade ran: " + w.StatusText.Text);
            await Click(w, "Make shadow", 800);
            await Click(w, "Fit shadow to the car");
            await Click(w, "Make carbox", 1500);
            await Click(w, "Fill frontend stats from a simulated run", 3000);
            await Click(w, "Save", 1200);
            Check(!d.Modified, "saved");
            dd = d.D!;
            Check(!string.IsNullOrEmpty(dd.TShadow) && game.Find(dd.TShadow) != null, "shadow file: " + dd.TShadow);
            Check(!string.IsNullOrEmpty(dd.TCarBox) && game.Find(dd.TCarBox) != null, "carbox file: " + dd.TCarBox);
            var used = new HashSet<string>(dd.Models.Concat(new[] { dd.TPage, dd.Coll, dd.TShadow, dd.TCarBox })
                .Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => Names.File(x.Trim())), StringComparer.OrdinalIgnoreCase);
            var spare = Directory.GetFiles(d.Car!.Dir).Select(Names.File)
                .Where(f => f!.EndsWith(".prm", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hul", StringComparison.OrdinalIgnoreCase)
                    || (f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase) && !used.Contains(f))).Where(f => !used.Contains(f!)).ToList();
            Check(spare.Count == 0, "no files left from the base car: " + string.Join(", ", spare));
            var report = CarChecker.Check(d.Car!);
            Check(report.Errors.Count == 0, "the check finds no errors: " + string.Join("; ", report.Errors));
            Check(!report.Warnings.Any(x => x.StartsWith("Low hull")), "the hull clears kerbs: " + string.Join("; ", report.Warnings));
            Log("check score " + report.Score + ", warnings: " + string.Join("; ", report.Warnings));
            Check(!d.Car!.ExternalFiles().Any(), "needs nothing from other cars: " + string.Join(",", d.Car.ExternalFiles()));
            var acc = CarSim.RunAcceleration(dd.Clone(), d.Hull);
            Log("sim: " + acc.Note + " / turn: " + CarSim.RunCircle(dd.Clone(), d.Hull).Note + " / bumps: " + CarSim.RunBumps(dd.Clone(), d.Hull).Note);
            Log("params: TopEnd " + dd.TopEnd + " Acc " + dd.Acc + " Mass " + dd.BodyMass);
            await Turnaround(w, "new_done");
            WindowShot(w, "new_window");

            // ---------------- 2. An existing car changed
            w.PickCar(EditedCar);
            await Wait(500);
            var c2 = d.Car!;
            var before = c2.Data.Clone();
            var top = before.TopSpeed;
            d.Edit("Faster", () => d.D!.TopSpeed = top * 1.08);
            d.EndGesture();
            // Paint: a new colour over the texture, written where the game reads it on top of its own files
            w.Tabs.SelectedIndex = w.PaintTabIndex;
            w.PaintHue.Value = 150;
            await Wait(800);
            var texRel = d.D!.TPage;
            var texOut = game.WritePath(texRel);
            Check(texOut.StartsWith(Path.Combine(game.Root, "packs", "local"), StringComparison.OrdinalIgnoreCase), "paint goes to packs\\local: " + texOut);
            w.WritePaint(texOut);
            w.ResetPaint();
            w.Tabs.SelectedIndex = 0;
            await Click(w, "Save", 600);
            Check(await Answer(w, "Save the changes"), "Save asks how to keep a stock car's changes");
            await Wait(1200);
            Check(!d.Modified, "edited car saved");
            var saved = game.Find("cars\\" + EditedCar + "\\parameters.txt");
            Check(saved != null && saved.StartsWith(Path.Combine(game.Root, "packs", "local"), StringComparison.OrdinalIgnoreCase), "its parameters in packs\\local: " + saved);
            var again = new Car(game, EditedCar);
            Check(Math.Abs(again.Data.TopSpeed - top * 1.08) < 0.01, "the change reads back");
            var diff = CarCompare.Diff(before, again.Data).ToList();
            Check(diff.Count == 1 && diff[0].Key == "TopSpeed", "only TopSpeed changed: " + string.Join(", ", diff.Select(r => r.Key)));
            Check(CarChecker.Check(again).Errors.Count == 0, "edited car checks");
            await Turnaround(w, "edited");
        }
        catch (Exception ex) { Check(false, "release test crashed: " + ex); }
        finally
        {
            Log($"RELEASE {pass} passed, {fail} failed");
            log?.Flush();
            Environment.Exit(fail == 0 ? 0 : 1);
        }
    }
}
