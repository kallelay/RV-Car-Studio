using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

/// <summary>
/// RvCarStudio -rvpath COPY --auto SHOTS   Runs the real window on a copy of a Re-Volt folder, writes pictures
/// from the 3D view and the window itself, never from the screen. Saving tests write into the copy.
/// </summary>
public static partial class AutoScenario
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

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    static extern bool PostMessage(IntPtr h, uint msg, IntPtr w, IntPtr l);

    const uint MOVE = 0x200, LDOWN = 0x201, LUP = 0x202, RDOWN = 0x204, RUP = 0x205, MDOWN = 0x207, MUP = 0x208, WHEEL = 0x20A;

    static async Task Send(MainWindow w, uint msg, Point p, int buttons = 0, int wheel = 0)
    {
        var h = w.TryGetPlatformHandle()!.Handle;
        var c = w.View.TranslatePoint(p, w) ?? p;
        var s = w.RenderScaling;
        int x = (int)(c.X * s), y = (int)(c.Y * s);
        if (msg == WHEEL)
        {
            var sc = w.PointToScreen(c);
            x = sc.X; y = sc.Y;
        }
        PostMessage(h, msg, (IntPtr)(buttons | (wheel << 16)), (IntPtr)((y << 16) | (x & 0xFFFF)));
        await Wait(40);
    }

    static async Task DragWith(MainWindow w, uint down, uint up, int flag, Point a, Point b)
    {
        await Send(w, MOVE, a);
        await Send(w, down, a, flag);
        for (int i = 1; i <= 6; i++)
            await Send(w, MOVE, new Point(a.X + (b.X - a.X) * i / 6, a.Y + (b.Y - a.Y) * i / 6), flag);
        await Send(w, up, b);
    }

    static async Task RealMouse(MainWindow w)
    {
        var d = w.Doc;
        d.MoveMode = MoveMode.Mirror;
        d.Snap = 0;
        w.PickCar("rc");
        w.View.SetView(ViewMode.Perspective);
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
        w.View.FrameCar();
        await Wait(200);

        var middle = new Point(w.View.Bounds.Width / 2, w.View.Bounds.Height / 2);
        var hit = w.InputHitTest(w.View.TranslatePoint(middle, w)!.Value);
        Check(hit == w.View, "the mouse reaches the 3D view: " + hit);

        // Turn with the right button, and with the left on nothing
        var yaw = w.View.Yaw; var pitch = w.View.Pitch;
        await DragWith(w, RDOWN, RUP, 2, middle, middle + new Point(120, 40));
        Check(Math.Abs(w.View.Yaw - yaw) > 0.3f && Math.Abs(w.View.Pitch - pitch) > 0.1f, "right drag turns the view: " + w.View.Yaw);
        yaw = w.View.Yaw;
        d.Select(new Selection(SelKind.None));
        var corner = new Point(30, 30);
        await DragWith(w, LDOWN, LUP, 1, corner, corner + new Point(90, 0));
        Check(Math.Abs(w.View.Yaw - yaw) > 0.3f, "left drag on nothing turns the view");
        Check(d.Sel.Kind == SelKind.None, "a drag isn't a click");

        // Zoom
        var dist = w.View.Distance;
        await Send(w, MOVE, middle);
        await Send(w, WHEEL, middle, 0, 120);
        Check(w.View.Distance < dist, "wheel zooms in: " + w.View.Distance);

        // Pan
        var target = w.View.Target;
        await DragWith(w, MDOWN, MUP, 16, middle, middle + new Point(60, 0));
        Check((w.View.Target - target).Length() > 1, "middle drag pans");

        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
        w.View.FrameCar();
        await Wait(100);

        // Click selects
        var wheelPos = d.D!.Wheels[0].Offset1;
        var at = w.View.ScreenOf(wheelPos)!.Value;
        await Send(w, MOVE, at);
        await Send(w, LDOWN, at, 1);
        await Send(w, LUP, at);
        Check(d.Sel.Kind is SelKind.Wheel or SelKind.Axle or SelKind.Spring or SelKind.Pin, "click selects what's under it: " + d.Sel.Label);

        // Move handle
        d.Select(new Selection(SelKind.Wheel, 0));
        w.SetTool(GizmoTool.Move);
        await Wait(60);
        var pz = w.View.AxisTip(wheelPos, 2);
        var aimed = w.View.ScreenOf(pz)!.Value;
        var aimedView = (w.View.Yaw, w.View.Pitch, w.View.Distance, w.View.Bounds.Size, w.View.TranslatePoint(aimed, w));
        await DragWith(w, LDOWN, LUP, 1, aimed, w.View.ScreenOf(pz + new Vector3(0, 0, 8))!.Value);
        var moved = d.D.Wheels[0].Offset1 - wheelPos;
        Check(Math.Abs(moved.Z - 8) < 1.5f, "the Z arrow moves the wheel: " + moved + " | aimed " + aimed + " " + aimedView + " | " + w.View.HandleDebug(pz) + " | window " + w.Position + " " + w.ClientSize + " " + w.WindowState);
        d.Undo();
        Check(d.D!.Wheels[0].Offset1 == wheelPos, "one undo takes back a whole drag: " + d.D.Wheels[0].Offset1);

        // Rotate ring
        d.Select(new Selection(SelKind.Body));
        w.SetTool(GizmoTool.Rotate);
        await Wait(60);
        var body = d.Model(d.D!.BodyModel)!;
        var before = body.Verts[0].Pos;
        var anchor = d.D.BodyOffset;
        var ringPoint = anchor + new Vector3(0, 0, 1) * w.View.Distance * 0.16f; // Y ring, Z side
        var ringNext = anchor + Vector3.Normalize(new Vector3(0.4f, 0, 1)) * w.View.Distance * 0.16f;
        await DragWith(w, LDOWN, LUP, 1, w.View.ScreenOf(ringPoint)!.Value, w.View.ScreenOf(ringNext)!.Value);
        Check((body.Verts[0].Pos - before).Length() > 0.1f, "a ring turns the body");
        Check(Math.Abs(body.Verts[0].Pos.Length() - before.Length()) < 0.01f, "turning keeps the size");
        Check(d.Modified && d.DirtyModels.Contains(body), "turned model waits for Save");
        await GlShot(w, "tool_rotate", 640);
        d.Undo();
        Check(body.Verts[0].Pos == before, "undo brings the body back");

        // Scale box
        d.Select(new Selection(SelKind.Wheel, 0));
        w.SetTool(GizmoTool.Scale);
        await Wait(60);
        var wheelModel = d.Model(d.D!.Wheels[0].Model)!;
        var r0 = d.D.Wheels[0].Radius;
        var h0 = wheelModel.Max.Y - wheelModel.Min.Y;
        var py = w.View.AxisTip(wheelPos, 1);
        await DragWith(w, LDOWN, LUP, 1, w.View.ScreenOf(py)!.Value, w.View.ScreenOf(py + new Vector3(0, 8, 0))!.Value);
        Check(wheelModel.Max.Y - wheelModel.Min.Y > h0 * 1.02f, "a box stretches the wheel");
        Check(d.D.Wheels[0].Radius > r0, "and its radius follows");
        float lodHeight = 0;
        wheelModel.ForOtherLods(l => lodHeight = Math.Max(lodHeight, l.Max.Y - l.Min.Y));
        Check(wheelModel.OtherLods == null || lodHeight > h0 * 1.02f, "the farther LODs stretch too: " + lodHeight + " vs " + h0);
        await GlShot(w, "tool_scale", 640);
        d.Undo();
        Check(Math.Abs(wheelModel.Max.Y - wheelModel.Min.Y - h0) < 0.01f && d.D!.Wheels[0].Radius == r0, "undo of a stretch");

        // A point has no rotate or scale
        d.Select(new Selection(SelKind.CoM));
        await Wait(30);
        Check(!d.CanTransform(d.Sel), "centre of mass only moves");

        w.SetTool(GizmoTool.Move);
        d.Snap = 0.5f;
        await Wait(50);
        Check(!d.Modified || d.DirtyModels.Count > 0, "state after undo");
        d.DirtyModels.Clear();
        w.PickCar("rc");
    }

    // How much colour a picture of the car has, against the same car without reflections
    static async Task FadeTest(MainWindow w)
    {
        static double Saturation(SKBitmap b)
        {
            double sum = 0; int n = 0;
            for (int y = 0; y < b.Height; y += 2)
                for (int x = 0; x < b.Width; x += 2)
                {
                    var c = b.GetPixel(x, y);
                    if (c.Red > 245 && c.Green > 245 && c.Blue > 245) continue;
                    c.ToHsv(out _, out var s, out var v);
                    if (v > 15) { sum += s; n++; }
                }
            return n == 0 ? 0 : sum / n;
        }
        foreach (var car in new[] { "adeon", "tc4", "jg1jg7" })
        {
            w.PickCar(car);
            w.Doc.Select(new Selection(SelKind.Body));
            w.View.Yaw = 0.75f + MathF.PI; w.View.Pitch = 0.32f;
            w.View.ShowEnv = true;
            w.View.Redraw();
            await Wait(60);
            using var on = await w.View.Capture(360, true);
            w.View.ShowEnv = false;
            w.View.Redraw();
            using var off = await w.View.Capture(360, true);
            w.View.ShowEnv = true;
            var (a, b) = (Saturation(on), Saturation(off));
            Log($"colour of {car}: {a:0.0} with reflections, {b:0.0} without");
            Check(a > b * 0.8, $"{car} keeps its colour with reflections: {a:0.0} vs {b:0.0}");
            SavePng(on, "fade_" + car);
        }
    }

    static async Task PartsTest(MainWindow w)
    {
        var d = w.Doc;
        d.MoveMode = MoveMode.Mirror;
        w.PickCar("rc");
        await Wait(100);
        int all = w.View.DrawnParts;

        // Hide: only the view
        d.ToggleHidden(new Selection(SelKind.Body));
        Check(w.View.DrawnParts == all - 1 && !d.Modified, "hiding only changes the view");
        var bodyAt = w.View.ScreenOf(new Vector3(0, -8, 0))!.Value;
        d.ToggleHidden(new Selection(SelKind.Body));
        Check(w.View.DrawnParts == all, "shown again");

        // Remove with the mirror mode: both sides go, undo brings them back
        var slot = d.D!.Springs[0].Model;
        d.Select(new Selection(SelKind.Spring, 0));
        d.RemovePart(new Selection(SelKind.Spring, 0));
        Check(d.D.Springs[0].Model == -1 && d.D.Springs[1].Model == -1 && d.D.Springs[2].Model != -1, "remove a spring and its mirror");
        Check(w.View.DrawnParts == all - 2, "they're gone from the view");
        d.Undo();
        Check(d.D!.Springs[0].Model == slot && d.D.Springs[1].Model == slot, "undo brings them back");

        // Remove the aerial, then add the stock one
        d.RemovePart(new Selection(SelKind.AerialBase));
        Check(d.D!.Aerial.SecModel == -1 && d.D.Aerial.TopModel == -1, "aerial removed");
        d.AddAerial();
        Check(d.D.Aerial.SecModel >= 0 && d.Model(d.D.Aerial.TopModel) != null, "stock aerial added");
        d.Undo(); d.Undo();

        // Add a new part to a car without one: the file comes from another car
        w.PickCar("adeon");
        await Wait(50);
        var before = w.View.DrawnParts;
        Check(d.D!.Springs[0].Model < 0, "adeon has no springs");
        d.SetPartModel(new Selection(SelKind.Spring, 0), "cars\\rc\\spring.prm");
        Check(d.D.Springs[0].Model >= 0 && d.D.Springs[1].Model == d.D.Springs[0].Model && d.D.Springs[0].Length > 0, "added a spring with sensible values, both sides");
        Check(d.D.Models[d.D.Springs[0].Model].EndsWith("spring.prm"), "in a free slot");
        Check(w.View.DrawnParts == before + 2, "drawn");
        d.Undo();
        Check(d.D!.Springs[0].Model < 0, "undo removes it");

        // A file from outside the game folder is copied into the car's
        var outside = Path.Combine(Path.GetTempPath(), "rvs_outside.prm");
        File.Copy(d.Car!.Game.Find("cars\\rc\\spring.prm")!, outside, true);
        var rel = d.BringIn(outside);
        Check(rel == "cars\\" + d.Car.Folder + "\\rvs_outside.prm" && d.Car.Game.Find(rel) != null, "brought in: " + rel);
        File.Delete(d.Car.Game.Find(rel)!);
        File.Delete(outside);
        Check(!d.Modified, "nothing left over");

        // The aerial as it is in the game: Length from base to tip
        w.PickCar("cougar");
        var tipPart = CarPose.Build(d.D!).Single(p => p.Kind == PartKind.AerialTop);
        var tip = Vector3.Transform(Vector3.Zero, tipPart.World);
        var want = d.D!.Aerial.Offset + Vector3.Normalize(d.D.Aerial.Direction) * (float)d.D.Aerial.Length;
        Check((tip - want).Length() < 0.01f, $"the aerial ends Length from its base: {tip} vs {want}");
        w.View.Yaw = MathF.PI; w.View.Pitch = 0.25f; w.View.FrameCar();
        await Wait(60);
        await GlShot(w, "aerial_cougar", 800);
        d.Select(new Selection(SelKind.None));
        await GlShot(w, "aerial_cougar_clean", 800, true);
    }

    static byte[] Bytes(Doc d, string rel) => File.ReadAllBytes(d.Car!.Game.Find(rel)!);

    static async Task CopyAndHullTest(MainWindow w)
    {
        var d = w.Doc;
        d.MoveMode = MoveMode.Single;
        d.Snap = 0;
        foreach (var f in new[] { "forked", "ownparts" })
        {
            var p = Path.Combine(d.Game!.CarsDir, f);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }

        // A stock car: the first change asks, once
        w.PickCar("tc1");
        int asked = 0;
        Action count = () => asked++;
        d.StockTouched += count;
        var stockParam = File.ReadAllBytes(d.Car!.ParamPath);
        var bodyRel = d.D!.Models[d.D.BodyModel];
        var stockBody = Bytes(d, bodyRel);
        var hullRel = d.D.Coll;
        var stockHull = Bytes(d, hullRel);
        d.Edit("Top speed", () => d.D!.TopSpeed = 44);
        d.EndGesture();
        d.Edit("Top speed 2", () => d.D!.TopSpeed = 45);
        d.EndGesture();
        Check(asked == 1, "a stock car asks once: " + asked);
        Check(w.StockBannerShown, "with the banner on screen");
        d.StockTouched -= count;

        // Scaling the body takes the hull along
        var h0 = d.Hull!.Convex[0].Max.Z;
        var r0 = d.Hull.Spheres[0].Radius;
        d.Tool = GizmoTool.Scale;
        d.Scale(new Selection(SelKind.Body), new Vector3(1.5f));
        d.EndGesture();
        Check(Math.Abs(d.Hull!.Convex[0].Max.Z - h0 * 1.5f) < 0.01f, "the hull grows with the body");
        Check(Math.Abs(d.Hull.Spheres[0].Radius - r0 * 1.5f) < 0.01f, "and its spheres");
        foreach (var f in d.Hull.Convex[0].Faces)
            foreach (var pt in d.Hull.Convex[0].Points)
                Check(Vector3.Dot(f.Normal, pt) + f.D < 0.2f, "scaled hull stays a solid");
        d.Undo();
        Check(Math.Abs(d.Hull!.Convex[0].Max.Z - h0) < 0.001f, "undo takes the hull back too");
        d.Scale(new Selection(SelKind.Body), new Vector3(1.2f));
        d.EndGesture();
        d.Tool = GizmoTool.Move;
        var movedHull = d.Hull!.Convex[0].Offset;
        d.Move(new Selection(SelKind.Body), new Vector3(0, 0, 5));
        d.EndGesture();
        Check((d.Hull.Convex[0].Offset - movedHull - new Vector3(0, 0, 5)).Length() < 0.01f, "the hull moves with the body");
        d.HullFollowsBody = false;
        var still = d.Hull.Convex[0].Offset;
        d.Move(new Selection(SelKind.Body), new Vector3(0, 0, 5));
        d.EndGesture();
        Check(d.Hull.Convex[0].Offset == still, "unless told not to");
        d.HullFollowsBody = true;

        // The copy has it all, the stock car has nothing
        var scaled = d.Model(d.D!.BodyModel)!.Max;
        d.ForkCar("forked", "Forked Car");
        Check(d.Car!.Folder == "forked" && d.D!.Name == "Forked Car", "working on the copy");
        Check(d.D.TopSpeed == 45, "with the settings changed so far");
        Check((d.Model(d.D.BodyModel)!.Max - scaled).Length() < 0.01f, "and the scaled body");
        Check(d.D.Models[d.D.BodyModel].StartsWith("cars\\forked\\"), "in its own folder");
        Check(Math.Abs(d.Hull!.Convex[0].Max.Z - h0 * 1.2f) < 0.01f, "and the scaled hull");
        var game = d.Game!;
        var orig = new Car(game, "tc1");
        Check(File.ReadAllBytes(orig.ParamPath).SequenceEqual(stockParam), "stock parameters untouched");
        Check(File.ReadAllBytes(game.Find(bodyRel)!).SequenceEqual(stockBody), "stock body untouched");
        Check(File.ReadAllBytes(game.Find(hullRel)!).SequenceEqual(stockHull), "stock hull untouched");
        Check(!d.Modified, "the copy is saved");

        // A file of another car: permission first, then a copy of its own
        w.PickCar("adeon");
        Car.CreateFrom(game, "adeon", "ownparts", "Own Parts");
        w.PickCar("ownparts");
        d.Edit("spring", () => { });
        d.SetPartModel(new Selection(SelKind.Spring, 0), "cars\\rc\\spring.prm");
        w.Save();
        d.Tool = GizmoTool.Scale;
        d.Select(new Selection(SelKind.Spring, 0));
        var shared = d.SharedFiles(d.Sel);
        Check(shared.Count == 1 && shared[0].Why.Contains("rc"), "the spring is rc's: " + string.Join(";", shared.Select(s => s.Why)));
        var rcFile = game.Find("cars\\rc\\spring.prm")!;
        var rcBytes = File.ReadAllBytes(rcFile);

        // The real way there: a drag on the handle brings up the question, and nothing changes until it's answered
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f; w.View.FrameCar();
        await Wait(100);
        var mount = d.Anchor(d.Sel)!.Value;
        var lengthBefore = d.D!.Springs[0].Length;
        var tipAt = w.View.AxisTip(mount, 1);
        await DragWith(w, LDOWN, LUP, 1, w.View.ScreenOf(tipAt)!.Value, w.View.ScreenOf(tipAt + new Vector3(0, 8, 0))!.Value);
        await Wait(300);
        var asking = w.OwnedWindows.FirstOrDefault();
        Check(asking != null, "dragging a shared model's handle asks first");
        Check(d.D.Springs[0].Length == lengthBefore, "nothing changed while it asks");
        asking?.Close();
        await Wait(200);
        Check(d.SharedFiles(d.Sel).Count == 1, "closing it changes nothing");
        d.MakeOwn("cars\\rc\\spring.prm");
        Check(d.D!.Models[d.D.Springs[0].Model].StartsWith("cars\\ownparts\\"), "made its own copy");
        Check(d.SharedFiles(d.Sel).Count == 0, "nothing to ask any more");
        d.Scale(new Selection(SelKind.Spring, 0), new Vector3(1, 2, 1));
        d.EndGesture();
        w.Save();
        Check(File.ReadAllBytes(rcFile).SequenceEqual(rcBytes), "rc's spring untouched");
        Check(!File.ReadAllBytes(game.Find(d.D.Models[d.D.Springs[0].Model])!).SequenceEqual(rcBytes), "ours changed");
        d.AllowShared("cars\\misc\\Aerial.m");

        // Hull: spheres and parts, with the real mouse
        w.View.ShowHull = true;
        d.Tool = GizmoTool.Move;
        d.MoveMode = MoveMode.Mirror;
        d.Select(new Selection(SelKind.HullSphere, 1));
        await Wait(50);
        var hs = d.Hull!;
        var s1 = hs.Spheres[1];
        var target = d.Anchor(d.Sel)!.Value;
        var from = w.View.AxisTip(target, 2);
        await DragWith(w, LDOWN, LUP, 1, w.View.ScreenOf(from)!.Value, w.View.ScreenOf(from + new Vector3(0, 0, 5))!.Value);
        Check((d.Hull!.Spheres[1].Pos - s1.Pos).Length() > 2, "dragging a hull sphere: " + (d.Hull.Spheres[1].Pos - s1.Pos));
        d.Undo();
        d.AddSphere(new Vector3(0, -10, 0), 6);
        Check(d.Hull!.Spheres[^1].Radius == 6 && d.Sel.Kind == SelKind.HullSphere, "add a sphere");
        int count0 = d.Hull.Spheres.Count;
        d.RemoveHullItem(d.Sel);
        Check(d.Hull!.Spheres.Count == count0 - 1, "remove a sphere");
        d.Select(new Selection(SelKind.HullPart, 0));
        d.Tool = GizmoTool.Rotate;
        d.Rotate(d.Sel, Vector3.UnitY, 0.5f);
        d.EndGesture();
        foreach (var f in d.Hull!.Convex[0].Faces)
            foreach (var pt in d.Hull.Convex[0].Points)
                Check(Vector3.Dot(f.Normal, pt) + f.D < 0.2f, "turned hull stays a solid");
        await GlShot(w, "tool_hull", 640);
        Check(d.RebuildHull() && d.HullDirty, "make the hull again");
        w.Save();
        var saved = Hull.Load(d.Car!.Game.Find(d.D!.Coll)!);
        Check(saved.Convex.Count == 1 && saved.Spheres.Count > 0, "saved hull reads back");
        Check(File.Exists(d.Car.Game.Find(d.D.Coll) + ".bak"), "with the first version kept");
        w.View.ShowHull = false;
        d.Tool = GizmoTool.Move;
        d.Snap = 0.5f;
        w.PickCar("rc");
        Directory.Delete(Path.Combine(game.CarsDir, "forked"), true);
    }

    static async Task PaintTest(MainWindow w)
    {
        var d = w.Doc;
        w.PickCar("ownparts");
        d.Select(new Selection(SelKind.None));
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f; w.View.FrameCar();
        w.Tabs.SelectedIndex = w.PaintTabIndex;
        await Wait(200);
        WindowShot(w, "tab_paint");
        using var before = await w.View.Capture(320, true);

        // Moving the slider is enough to see it
        w.PaintHue.Value = 120;
        await Wait(800);
        using var after = await w.View.Capture(320, true);
        SavePng(after, "paint_hue");
        long diff = 0;
        for (int y = 0; y < 320; y += 4)
            for (int x = 0; x < 320; x += 4)
            {
                var a = before.GetPixel(x, y); var b = after.GetPixel(x, y);
                diff += Math.Abs(a.Red - b.Red) + Math.Abs(a.Green - b.Green) + Math.Abs(a.Blue - b.Blue);
            }
        Check(diff > 20000, "the hue shows on the car at once: " + diff);
        var tex = d.Car!.Game.Find(d.D!.TPage)!;
        var texBytes = File.ReadAllBytes(tex);
        Check(d.PaintPreview != null && !d.Modified, "nothing written yet");

        // As a new skin: the original stays, RVGL finds it by name
        var skin = w.PaintSaveSkin();
        Check(skin != null && File.Exists(skin) && File.ReadAllBytes(tex).SequenceEqual(texBytes), "a new skin, original kept");
        Check(d.Car.FindSkins().Any(s => string.Equals(s, skin, StringComparison.OrdinalIgnoreCase)), "listed as a skin");
        w.ResetPaint();
        Check(d.SkinOverride == null && d.PaintPreview == null, "back to the texture");

        // Only one colour
        using (var src = Images.Load(tex)!)
        {
            var main = CarTools.MainColours(src);
            Check(main.Count > 0, "main colours found: " + main.Count);
            using var one = CarTools.AdjustTexture(src, 0, 1, 90, new[] { main[0].Bucket });
            int changed = 0, kept = 0;
            for (int y = 0; y < src.Height; y += 2)
                for (int x = 0; x < src.Width; x += 2)
                {
                    var a = src.GetPixel(x, y); var b = one.GetPixel(x, y);
                    a.ToHsv(out var h, out var sat, out _);
                    if (a == b) kept++; else changed++;
                }
            Check(changed > 0 && kept > 0, $"only that colour changes: {changed} changed, {kept} kept");
        }

        // Save over the texture
        w.PaintBright.Value = 0.2;
        await Wait(500);
        w.WritePaint(tex);
        Check(File.Exists(tex + ".bak") && File.ReadAllBytes(tex + ".bak").SequenceEqual(texBytes), "texture changed, first version kept");
        Check(!File.ReadAllBytes(tex).SequenceEqual(texBytes), "and it's different");

        // Your image editor: saving there shows here
        w.WatchTexture(tex);
        var v0 = d.TextureVersion;
        using (var b = Images.Load(tex)!) Images.SaveBmp24(b, tex);
        await Wait(1500);
        Check(d.TextureVersion > v0, "saving in an editor reloads it");
        w.ResetPaint();
        w.Tabs.SelectedIndex = 0;
    }

    static async Task WizardWindowTest(MainWindow w)
    {
        var wiz = new NewCarWizard(w.Doc.Game!, "rc");
        _ = wiz.ShowDialog(w);
        await Wait(500);
        var size = new PixelSize((int)wiz.Bounds.Width, (int)wiz.Bounds.Height);
        using (var rtb = new RenderTargetBitmap(size))
        {
            rtb.Render(wiz);
            rtb.Save(Path.Combine(shots, "wizard.png"));
        }
        Check(wiz.IsVisible, "the wizard opens");
        wiz.Close();
        await Wait(200);
        Check(wiz.Result == null, "closing it makes nothing");
    }

    static void WizardTest(MainWindow w)
    {
        var d = w.Doc;
        var game = d.Game!;
        foreach (var f in new[] { "wizrepaint", "wizorig" })
        {
            var p = Path.Combine(game.CarsDir, f);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }
        w.CreateCar(new NewCarChoice(NewCarKind.Repaint, "adeon", "wizrepaint", "Wiz Repaint"));
        Check(d.Car!.Folder == "wizrepaint" && w.Tabs.SelectedIndex == w.PaintTabIndex, "repaint opens on Paint");
        Check(d.Car.ExternalFiles().Count == 0, "repaint has its own files");

        var body = game.Find("cars\\rc\\body.prm")!;
        var wheel = game.Find("cars\\rc\\wheelfl.prm")!;
        var tex = game.Find("cars\\rc\\car.bmp")!;
        w.CreateCar(new NewCarChoice(NewCarKind.Original, "adeon", "wizorig", "Wiz Original", body, wheel, tex));
        Check(d.Car!.Folder == "wizorig", "original opens");
        var dd = d.D!;
        Check(dd.Models[dd.BodyModel].StartsWith("cars\\wizorig\\") && d.Model(dd.BodyModel) != null, "own body");
        Check(dd.Wheels.All(x => x.Present && d.Model(x.Model) != null), "four wheels");
        Check(dd.Wheels[0].Offset1.X < 0 && dd.Wheels[1].Offset1.X > 0 && dd.Wheels[0].Offset1.Z > dd.Wheels[2].Offset1.Z, "placed at the corners");
        Check(dd.Wheels[0].Radius > 3, "radius from the model");
        Check(dd.Springs.All(s => s.Model < 0), "base car's suspension gone");
        Check(d.Car.Game.Find(dd.Coll) != null && d.Hull != null && d.Hull.Convex.Count == 1, "a first hull");
        var r = d.Model(dd.Wheels[1].Model)!; var l = d.Model(dd.Wheels[0].Model)!;
        Check(Math.Abs(r.Max.X + l.Min.X) < 0.01f, "right wheel is the left one mirrored");
        var report = CarChecker.Check(d.Car);
        Check(report.Errors.Count == 0, "original car checks: " + string.Join("; ", report.Errors));
        Check(!d.Car.ExternalFiles().Any(), "needs nothing from other cars: " + string.Join(",", d.Car.ExternalFiles()));
        w.Tabs.SelectedIndex = 0;
        w.PickCar("rc");
        foreach (var f in new[] { "wizrepaint", "wizorig", "ownparts" })
        {
            var p = Path.Combine(game.CarsDir, f);
            if (Directory.Exists(p)) Directory.Delete(p, true);
        }
    }

    // The things from the last round of feedback
    static async Task ViewAndToolsTest(MainWindow w)
    {
        var d = w.Doc;
        w.PickCar("rc");
        w.View.SetView(ViewMode.Perspective);
        w.View.Yaw = 0.3f; w.View.Pitch = 0.35f; w.View.FrameCar();
        await Wait(150);

        // Space flips the car while driving, through the real key
        w.StartDrive(SimGround.Flat);
        await Wait(100);
        d.Sim!.Rot = System.Numerics.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI);
        Check(d.Sim.Up.Y > 0.9f, "driving, on its roof");
        PostMessage(w.TryGetPlatformHandle()!.Handle, 0x100, (IntPtr)0x20, (IntPtr)0x00390001);
        await Wait(30);
        PostMessage(w.TryGetPlatformHandle()!.Handle, 0x101, (IntPtr)0x20, unchecked((IntPtr)0xC0390001));
        await Wait(700);
        Check(d.Sim.Up.Y < -0.9f, "Space flips it: " + d.Sim.Up);
        w.StopDrive();
        await Wait(50);

        // The 3D button is a view from an angle, also after a flat one
        w.View.SetView(ViewMode.Side);
        w.View.SetView(ViewMode.Perspective);
        Check(w.View.Yaw > 0.1f && w.View.Yaw < 1.4f && w.View.Pitch > 0.1f, "3D after Side looks from an angle");
        w.View.Yaw = 0.3f; w.View.Pitch = 0.35f;
        await Wait(60);

        // Camera: the car turns with the mouse
        var nose = d.D!.Wheels[0].Offset1;
        var noseBefore = w.View.ScreenOf(nose)!.Value.X;
        var mid = new Point(w.View.Bounds.Width / 2, w.View.Bounds.Height / 2);
        await DragWith(w, RDOWN, RUP, 2, mid, mid + new Point(40, 0));
        Check(w.View.ScreenOf(nose)!.Value.X > noseBefore, "dragging right moves the near side of the car right");

        // From below
        w.View.Pitch = 0.35f;
        Check(w.View.FloorShown, "floor from above");
        w.View.Pitch = -1f; w.View.Redraw();
        d.Select(new Selection(SelKind.None));
        Check(!w.View.FloorShown, "no floor from below");
        await Wait(60);
        await GlShot(w, "from_below", 500);
        using (var shot = await w.View.Capture(200, false))
        {
            int car = 0, x0 = 50;
            for (int y = 50; y < 150; y += 4)
                for (int x = x0; x < 150; x += 4)
                {
                    var c = shot.GetPixel(x, y);
                    if (Math.Abs(c.Red - 0.17 * 255) + Math.Abs(c.Green - 0.19 * 255) + Math.Abs(c.Blue - 0.22 * 255) > 25) car++;
                }
            Check(car > 20, "the underside can be seen: " + car);
        }
        w.View.Pitch = 0.35f;

        // Pages scroll
        w.Tabs.SelectedIndex = w.PaintTabIndex + 1; // Car
        await Wait(300);
        var page = ((TabItem)w.Tabs.Items[w.Tabs.SelectedIndex]!).Content as ScrollViewer;
        Check(page != null && page.Extent.Height > page.Viewport.Height && page.Viewport.Height > 50, $"a long page scrolls: {page?.Extent.Height} in {page?.Viewport.Height}");
        WindowShot(w, "tab_car_scroll");
        w.Tabs.SelectedIndex = 0;

        // Picture lists
        var picker = new ModelPicker(d.Game!, "rc", "Test");
        _ = picker.ShowDialog(w);
        await Wait(1200);
        Check(picker.TileCount > 4, "models of this car to pick from: " + picker.TileCount);
        using (var rtb = new RenderTargetBitmap(new PixelSize((int)picker.Bounds.Width, (int)picker.Bounds.Height)))
        {
            rtb.Render(picker);
            rtb.Save(Path.Combine(shots, "model_picker.png"));
        }
        picker.Close();
        await Wait(100);
        Check(picker.Result == null, "closing the picker chooses nothing");
        using (var thumb = Thumbnail.OfCar(d.Car!, 96)) SavePng(thumb, "thumb_rc");

        // Shadow takes every part
        using (var all = CarTools.RenderShadow(d.Car!))
        {
            var only = new Car(d.Game!, "rc");
            for (int i = 0; i < 4; i++) { only.Data.Springs[i].Model = -1; only.Data.Axles[i].Model = -1; only.Data.Pins[i].Model = -1; }
            using var some = CarTools.RenderShadow(only);
            SavePng(all, "shadow_all");
            Func<SKBitmap, long> ink = b => { long s = 0; for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) s += b.GetPixel(x, y).Red; return s; };
            Check(ink(all) > ink(some), "suspension casts shadow too");
        }

        // Shading while the slider moves, with the texture off
        var body = d.Model(d.D!.BodyModel)!;
        var c0 = body.Faces.Select(f => f.Colors[0]).ToArray();
        w.SetShadeView(ShadeMode.VertexColours);
        Check(w.View.Shade == ShadeMode.VertexColours, "texture off");
        int runs = w.ShadeRuns;
        w.Tabs.SelectedIndex = w.PaintTabIndex + 6;
        await Wait(100);
        w.ShadeSliderSun.Value = 0.9;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (w.ShadeRuns == runs && sw.ElapsedMilliseconds < 3000) await Wait(10);
        Log($"first live shade after {sw.ElapsedMilliseconds} ms");
        Check(sw.ElapsedMilliseconds < 1000, "moving a slider shades at once: " + sw.ElapsedMilliseconds + " ms");
        Check(!body.Faces.Select(f => f.Colors[0]).SequenceEqual(c0), "the colours changed");
        Check(d.DirtyModels.Contains(body), "waiting for Save");
        await Wait(900);
        Check(w.ShadeRuns >= runs + 2, "the full quality follows");
        await GlShot(w, "shade_live", 500);
        w.ShadeUndo();
        Check(body.Faces.Select(f => f.Colors[0]).SequenceEqual(c0) && !d.DirtyModels.Contains(body), "and back");
        w.ShadeSliderSun.Value = 0.55;
        await Wait(30);
        w.ShadeUndo();
        w.SetShadeView(ShadeMode.Textured);
        w.Tabs.SelectedIndex = 0;

        // Solid hull
        w.View.ShowHull = true;
        w.View.HullSolid = true;
        d.Select(new Selection(SelKind.HullSphere, 0));
        w.View.Redraw();
        await Wait(60);
        await GlShot(w, "hull_solid", 500);
        w.View.HullSolid = false;
        w.View.ShowHull = false;
        d.Select(new Selection(SelKind.Body));
        d.EndGesture();
        w.PickCar("rc");
    }

    // Guide, shading, normals, colours, race
    static async Task NewToolsTest(MainWindow w)
    {
        var d = w.Doc;
        w.PickCar("rc");
        await Wait(200);

        w.ShowGuide();
        await Wait(300);
        Check(((TabItem)w.Tabs.SelectedItem!).Content == w.GuideTab, "F1's Guide tab opens");
        w.GuideTab.Refresh();
        var speed = w.GuideSpeed.Text ?? "";
        Check(speed.Contains("mph"), "the Guide works out top speed: " + speed);
        WindowShot(w, "guide");
        var top = d.D!.TopSpeed;
        d.Edit("Guide test", () => d.D!.TopSpeed = top * 0.5);
        d.EndGesture();
        await Wait(80);
        Check((w.GuideSpeed.Text ?? "") != speed, "and follows the car: " + w.GuideSpeed.Text);
        d.Undo();
        var ratio = d.D!.Wheels[2].EngineRatio;
        w.TryEngine.Value = 2;
        await Wait(30);
        Check((w.GuideTryResult.Text ?? "").Contains("Then"), "Try it shows the result: " + w.GuideTryResult.Text);
        Check(d.D!.Wheels[2].EngineRatio == ratio, "without changing the car");
        w.ApplyTry();
        Check(Math.Abs(d.D!.Wheels[2].EngineRatio - ratio * 2) < 1e-6, "Use these numbers does");
        d.Undo();
        Check(d.D!.Wheels[2].EngineRatio == ratio, "and undo takes it back");

        // Shading by faces, as the slider moves
        var body = d.Model(d.D!.BodyModel)!;
        var old = body.Faces.Select(f => f.Colors[0]).ToArray();
        w.Tabs.SelectedIndex = w.PaintTabIndex + 6;
        await Wait(100);
        w.ShadeSmoothBox.SelectedIndex = 3;
        await Wait(1500);
        Check(body.Faces.All(f => Enumerable.Range(0, f.Corners).All(k => f.Colors[k] == f.Colors[0])), "shading by faces: flat polygons");
        WindowShot(w, "tools_shading");
        w.SetShadeView(ShadeMode.VertexColours);
        await GlShot(w, "shade_faces", 500);
        w.ShadeSmoothBox.SelectedIndex = 2;
        await Wait(1500);
        Check(body.Faces.Any(f => Enumerable.Range(0, f.Corners).Any(k => f.Colors[k] != f.Colors[0])), "by angle: blended where it's smooth");
        await GlShot(w, "shade_angle", 500);
        w.ShadeSky = Avalonia.Media.Color.Parse("#FF7030");
        w.ShadeSunColour = Avalonia.Media.Color.Parse("#FFC080");
        w.ShadeSmoothBox.SelectedIndex = 1;
        await Wait(1500);
        uint coloured = body.Faces[0].Colors[0];
        Check(((coloured >> 16) & 255) > (coloured & 255), $"sky and sun colours: {coloured:X8}");
        await GlShot(w, "shade_colour", 500);
        w.ShadeUndo();
        w.ShadeSky = w.ShadeSunColour = Avalonia.Media.Colors.White;
        w.ShadeSmoothBox.SelectedIndex = 0;
        await Wait(60);
        w.ShadeUndo();
        Check(body.Faces.Select(f => f.Colors[0]).SequenceEqual(old), "and back to the old shading");
        w.SetShadeView(ShadeMode.Textured);

        // Normals change vertices: undo must bring them back
        int verts = body.Verts.Length;
        w.DoRewriteNormals(ShadeSmooth.Faces, 45);
        Check(body.Verts.Length > verts, $"normals by faces: {verts} vertices became {body.Verts.Length}");
        Check(body.Faces.All(f => Enumerable.Range(0, f.Corners).All(k => f.Index(k) < body.Verts.Length)), "polygons fit them");
        w.View.ShowNormals = true;
        w.View.Redraw();
        await Wait(60);
        await GlShot(w, "normals", 500);
        w.View.ShowNormals = false;
        d.Undo();
        Check(body.Verts.Length == verts && body.Faces.All(f => Enumerable.Range(0, f.Corners).All(k => f.Index(k) < verts)), "undo brings the vertices back");
        d.DirtyModels.Remove(body);

        // Colours
        w.Tabs.SelectedIndex = w.PaintTabIndex;
        await Wait(100);
        w.PaintToneAmount.Value = 0.8;
        await Wait(250);
        Check(d.PaintPreview != null, "one colour over everything shows at once");
        await GlShot(w, "paint_tone", 500);
        w.PaintToneAmount.Value = 0;
        w.PaintSwap.IsChecked = true;
        w.PaintTo.SetValue(Avalonia.Media.Color.Parse("#2EC4B6"));
        w.PaintChanged();
        await Wait(250);
        Check(d.PaintPreview != null, "swapping a colour too");
        await GlShot(w, "paint_swap", 500);
        WindowShot(w, "tab_paint");
        w.PaintSwap.IsChecked = false;
        w.ResetPaint();
        await Wait(50);
        Check(d.PaintPreview == null, "and back");

        // A race, with the AI driving everyone
        w.RaceOpponents = 3;
        w.RaceLapsHere = 1;
        w.RaceAiDrives = true;
        w.StartRace();
        Check(d.Race != null && d.Race.Cars.Count == 4, "race: 3 CPU cars and you: " + d.Race?.Cars.Count);
        Check(d.Race!.Cars.Where(c => !c.IsPlayer).Select(c => c.Folder).Distinct().Count() == 3, "stock cars: " + string.Join(", ", d.Race.Cars.Select(c => c.Folder)));
        await Wait(300);
        await GlShot(w, "race_grid", 900);
        await Wait(5200); // the game's countdown is 5 s
        Check(d.Race.Started, "the countdown ends");
        float raceFrom = d.Race.Time;
        var raceClock = System.Diagnostics.Stopwatch.StartNew();
        await Wait(3500);
        float pace = (d.Race.Time - raceFrom) / (float)raceClock.Elapsed.TotalSeconds;
        Log($"race runs at {pace:0.00} of real time");
        Check(pace > 0.9f, $"the race runs at real speed: {pace:0.00}");
        Check(d.Sim!.SpeedMph > 5, "the race is on: " + d.Sim.SpeedMph);
        Check(w.HudText.Contains("Lap"), "with lap and place: " + w.HudText.Replace('\n', '|'));
        Check(w.View.DrawnParts > 20, "all the cars drawn: " + w.View.DrawnParts);
        await GlShot(w, "race_on", 900);
        w.StopDrive();
        await Wait(50);

        // The other grounds, and the one that isn't in the list
        w.RaceGroundIndex = 2;
        w.RaceOpponents = 1;
        w.StartRace();
        Check(d.Race!.Track.Ground == RaceGround.Ramp, "Ramp in the list");
        await Wait(6400);
        await GlShot(w, "race_ramp", 700);
        w.StopDrive();
        w.RaceGroundIndex = 1;
        w.StartRace();
        Check(d.Race!.Track.Ground == RaceGround.Bumpy, "Bumpy in the list");
        await Wait(6400);
        await GlShot(w, "race_bumpy", 700);
        w.StopDrive();
        w.RaceGroundIndex = 0;
        w.StartRace(true);
        Check(d.Race!.Track.Ground == RaceGround.Materials, "the secret surfaces track");
        await Wait(5500);
        Check(w.HudText.Contains("On: "), "says what you drive on: " + w.HudText.Replace('\n', '|'));
        await GlShot(w, "race_materials", 700);
        w.StopDrive();
        w.RaceOpponents = 3;
        w.StartRace();
        await Wait(4000);
        WindowShot(w, "race_window");
        w.StopDrive();
        Check(d.Race == null && d.Sim == null, "leaving the race");
        w.RaceAiDrives = false;
        w.PickCar("rc");
    }

    static void LockTest(MainWindow w)
    {
        w.PickCar("adeon");
        Check(w.CarLocked, "a picked car is locked");
        Check(w.Doc.Car!.Folder == "adeon", "adeon open");
        w.ChangeCar();
        Check(!w.CarLocked && w.Doc.Car!.Folder == "adeon", "change car keeps it open until another is picked");
        w.SetLocked(true);
        Check(w.CarLocked, "keep the open car");
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
            Check(d.Car == null && !w.CarLocked, "it starts on the list of cars, nothing open");
            for (int i = 0; i < w.Tabs.ItemCount; i++)
            {
                w.Tabs.SelectedIndex = i;
                await Wait(30);
            }
            w.Tabs.SelectedIndex = 0;
            WindowShot(w, "start_screen");
            LockTest(w);
            await ViewAndToolsTest(w);
            await NewToolsTest(w);
            await FadeTest(w);
            await PartsTest(w);
            await CopyAndHullTest(w);
            await PaintTest(w);
            await WizardWindowTest(w);
            WizardTest(w);

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

            // The whole way, as Windows delivers it: messages to the window
            await RealMouse(w);
            await InViewTest(w);
            await LookTest(w);

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
