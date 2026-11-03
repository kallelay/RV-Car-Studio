using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

/// <summary>
/// RvCarStudio -rvpath COPY --auto SHOTS   Runs the real window on a copy of a Re-Volt folder, writes pictures
/// from the 3D view and the window itself, never from the screen. Saving tests write into the copy.
/// </summary>
public static class AutoScenario
{
    public static bool Running;
    static int pass, fail;
    static string shots = "";
    static StreamWriter? log;

    static void Log(string s) { log?.WriteLine(s); log?.Flush(); }
    static void Check(bool ok, string what) { if (ok) pass++; else { fail++; Log("FAIL " + what); } }

    static async Task Wait(int ms)
    {
        await Task.Delay(ms);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background);
    }

    static void SavePng(SKBitmap b, string name)
    {
        using var img = SKImage.FromBitmap(b);
        using var data = img.Encode(SKEncodedImageFormat.Png, 90);
        File.WriteAllBytes(Path.Combine(shots, name + ".png"), data.ToArray());
    }

    static async Task GlShot(MainWindow w, string name, int size = 640, bool clean = false)
    {
        using var b = await w.View.Capture(size, clean);
        SavePng(b, name);
    }

    static void WindowShot(MainWindow w, string name)
    {
        var size = new PixelSize((int)w.Bounds.Width, (int)w.Bounds.Height);
        using var rtb = new RenderTargetBitmap(size);
        rtb.Render(w);
        rtb.Save(Path.Combine(shots, name + ".png"));
    }

    public static async Task Run(MainWindow w, string shotDir)
    {
        Running = true;
        shots = shotDir;
        Directory.CreateDirectory(shots);
        log = new StreamWriter(Path.Combine(shots, "auto.txt"));
        var d = w.Doc;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("CRASH " + e.ExceptionObject);
        try
        {
            await Wait(1500);
            Check(d.Car != null, "a car opened");

            // Every car, from two sides
            foreach (var e in d.Game!.ListCars().ToList())
            {
                try
                {
                    w.PickCar(e.Folder);
                    w.View.Yaw = 0.75f + MathF.PI; w.View.Pitch = 0.32f;
                    w.View.Redraw();
                    await Wait(60);
                    await GlShot(w, "car_" + e.Folder + "_a", 360);
                    w.View.Yaw = 0.75f;
                    w.View.Redraw();
                    await GlShot(w, "car_" + e.Folder + "_b", 360);
                }
                catch (Exception ex) { Check(false, e.Folder + ": " + ex.Message); }
            }

            w.PickCar("rc");
            await Wait(200);
            w.View.Yaw = 0.8f; w.View.Pitch = 0.35f; w.View.Redraw();
            await GlShot(w, "rc_big", 900);
            await Wait(300);
            WindowShot(w, "window_rc");

            // Every tab, with something selected
            d.Select(new Selection(SelKind.Wheel, 0));
            for (int i = 0; i < w.Tabs.ItemCount; i++)
            {
                w.Tabs.SelectedIndex = i;
                await Wait(200);
                WindowShot(w, "tab_" + i);
            }
            w.Tabs.SelectedIndex = 0;

            // Moving: modes
            var a = d.D!.Wheels[0].Offset1; var b = d.D.Wheels[1].Offset1; var c2 = d.D.Wheels[2].Offset1; var e2 = d.D.Wheels[3].Offset1;
            d.MoveMode = MoveMode.Single;
            d.Move(new(SelKind.Wheel, 0), new Vector3(1, 0, 0));
            Check(d.D.Wheels[0].Offset1.X == a.X + 1 && d.D.Wheels[1].Offset1 == b, "single move");
            d.Undo();
            Check(d.D!.Wheels[0].Offset1 == a, "undo");
            d.MoveMode = MoveMode.Mirror;
            d.Move(new(SelKind.Wheel, 0), new Vector3(1, 0, 2));
            Check(d.D.Wheels[1].Offset1 == b + new Vector3(-1, 0, 2), "mirror: other side opposite in X, same in Z");
            d.Undo();
            d.MoveMode = MoveMode.FrontBack;
            d.Move(new(SelKind.Wheel, 0), new Vector3(0, 0, 3));
            Check(d.D!.Wheels[2].Offset1 == c2 + new Vector3(0, 0, -3) && d.D.Wheels[1].Offset1 == b, "front and back");
            d.Undo();
            d.MoveMode = MoveMode.All;
            d.Move(new(SelKind.Wheel, 0), new Vector3(1, 2, 3));
            Check(d.D!.Wheels[3].Offset1 == e2 + new Vector3(-1, 2, -3), "all four");
            d.Redo();
            d.Undo();
            d.MoveMode = MoveMode.Mirror;
            var sp = d.D!.Springs[0].Offset;
            d.Move(new(SelKind.Wheel, 0), new Vector3(0, 0, 4));
            Check(d.D.Springs[0].Offset == sp + new Vector3(0, 0, 4), "spring follows its wheel");
            d.Undo();

            // The mouse on the 3D view: click selects, a handle moves, the other side mirrors
            d.MoveMode = MoveMode.Mirror;
            d.Snap = 0;
            w.View.SetView(ViewMode.Top);
            w.View.FrameCar();
            await Wait(100);
            var wheelPos = d.D!.Wheels[1].Offset1;
            var click = w.View.ScreenOf(wheelPos)!.Value;
            w.View.PressLeft(click);
            w.View.ReleaseAt(click);
            Check(d.Sel.Kind == SelKind.Wheel && d.Sel.Index == 1, "click selects the wheel under the mouse: " + d.Sel.Label);
            var bodyClick = w.View.ScreenOf(new Vector3(0, 0, 10))!.Value;
            w.View.PressLeft(bodyClick);
            w.View.ReleaseAt(bodyClick);
            Check(d.Sel.Kind == SelKind.Body, "click selects the body: " + d.Sel.Label);
            d.Select(new Selection(SelKind.Wheel, 1));
            await Wait(50);
            var other = d.D.Wheels[0].Offset1;
            var from = w.View.ScreenOf(w.View.AxisTip(wheelPos, 2))!.Value;
            var far = w.View.ScreenOf(w.View.AxisTip(wheelPos, 2) + new Vector3(0, 0, 10))!.Value;
            w.View.PressLeft(from);
            for (int i = 1; i <= 5; i++) w.View.MoveTo(new Point(from.X + (far.X - from.X) * i / 5, from.Y + (far.Y - from.Y) * i / 5));
            w.View.ReleaseAt(far);
            var moved = d.D.Wheels[1].Offset1 - wheelPos;
            Check(Math.Abs(moved.Z - 10) < 0.5f && Math.Abs(moved.X) < 0.5f, "dragging the Z handle by 10 units moved it " + moved);
            Check(Math.Abs(d.D.Wheels[0].Offset1.Z - other.Z - moved.Z) < 0.01f, "the other side followed");
            d.Undo();
            Check(d.D!.Wheels[1].Offset1 == wheelPos, "undo of a drag");
            w.View.SetView(ViewMode.Perspective);
            d.Snap = 0.5f;

            // A selected part in the view, with its handle
            d.Select(new Selection(SelKind.Wheel, 0));
            await Wait(100);
            await GlShot(w, "rc_selected", 640);

            // View options
            w.View.ShowHull = true; w.View.ShowTravel = true; w.View.ShowWire = true; w.View.Redraw();
            await GlShot(w, "rc_helpers", 640);
            w.View.ShowHull = false; w.View.ShowTravel = false; w.View.ShowWire = false;
            w.View.Shade = ShadeMode.VertexColours; w.View.Redraw();
            await GlShot(w, "rc_shading", 640);
            w.View.Shade = ShadeMode.Textured; w.View.Redraw();

            foreach (var v in new[] { ViewMode.Top, ViewMode.Front, ViewMode.Side })
            {
                w.View.SetView(v);
                await GlShot(w, "view_" + v, 480);
            }
            w.View.SetView(ViewMode.Perspective);
            w.View.Yaw = 0.8f; w.View.Pitch = 0.35f; w.View.Redraw();

            // Tools on a copy of a car
            var folder = Path.Combine(d.Game.CarsDir, "autotest");
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            Car.CreateFrom(d.Game, "rc", "autotest", "Auto Test");
            w.PickCar("autotest");
            await Wait(200);
            w.DoAutoShade(0.45f, 0.55f, 1f);
            await Wait(100);
            await GlShot(w, "tool_shade", 640);
            var bodyFile = d.Car!.Game.Find(d.D!.Models[d.D.BodyModel])!;
            var before = File.ReadAllBytes(bodyFile);
            w.Save();
            Check(File.Exists(bodyFile + ".bak") && !File.ReadAllBytes(bodyFile).SequenceEqual(before), "shading saved, with a backup");
            d.Edit("Shadow", () => CarTools.MakeShadow(d.Car));
            await Wait(100);
            await GlShot(w, "tool_shadow", 640);
            var box = await w.MakeCarbox();
            Check(box != null && File.Exists(box), "carbox");
            if (box != null)
                using (var bb = Images.Load(box)!) SavePng(bb, "tool_carbox");
            w.Save();
            var reread = new Car(d.Game, "autotest");
            Check(reread.Data.TShadow.Contains("shadow.bmp") && reread.Data.TCarBox.Contains("carbox.bmp"), "shadow and carbox in the file: '" + reread.Data.TShadow + "' '" + reread.Data.TCarBox + "' now '" + d.D!.TShadow + "' '" + d.D.TCarBox + "' text " + (File.ReadAllText(reread.ParamPath).Contains("TSHADOW")));

            // Drive
            w.PickCar("adeon");
            await Wait(150);
            w.StartDrive(SimGround.Flat);
            w.AutoThrottle = 1; w.AutoSteer = 0;
            await Wait(2500);
            var speed = d.Sim?.SpeedMph ?? 0;
            Check(speed > 5, "drive: speed " + speed);
            Log($"drive after 2.5 s: {speed:0.0} mph");
            w.AutoSteer = 1;
            await Wait(1200);
            await GlShot(w, "drive_turn", 640);
            WindowShot(w, "window_drive");
            w.AutoThrottle = float.NaN;
            w.StopDrive();
            await Wait(100);
            Check(d.Sim == null, "drive stopped");

            // Test run: the profile only, the game is started apart
            var run = new TestRun(d.Game, Path.Combine(shots, "prefs"));
            run.PrepareProfile("adeon", "nhood1");
            await Wait(1200);
            var ini = File.ReadAllText(Path.Combine(shots, "prefs", "profiles", TestRun.Profile, "profile.ini"));
            Check(ini.Contains("CarDir1 = \"adeon\""), "test profile has the car");
            Log("levels: " + run.Levels().Count());

            Directory.Delete(folder, true);
            w.PickCar("adeon");
        }
        catch (Exception ex) { Check(false, ex.ToString()); }
        Log($"passed {pass}, failed {fail}");
        log.Dispose();
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
