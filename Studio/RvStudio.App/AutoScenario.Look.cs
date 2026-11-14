using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

// The themes, and working in the 3D view: names under the mouse, a part's menu, amounts shown and typed
public static partial class AutoScenario
{
    const uint KEYDOWN = 0x100, KEYUP = 0x101;
    const int VK_BACK = 0x08, VK_ENTER = 0x0D, VK_ESC = 0x1B, VK_PERIOD = 0xBE, VK_MINUS = 0xBD;

    /// <summary>Keys as Windows delivers them: to the window, so through the same path as typing</summary>
    static async Task Keys(MainWindow w, params int[] vks)
    {
        var h = w.TryGetPlatformHandle()!.Handle;
        foreach (var vk in vks)
        {
            PostMessage(h, KEYDOWN, (IntPtr)vk, (IntPtr)1);
            PostMessage(h, KEYUP, (IntPtr)vk, unchecked((IntPtr)0xC0000001L));
            await Wait(40);
        }
    }

    static SKBitmap Picture(Visual v)
    {
        var size = new PixelSize(Math.Max(1, (int)v.Bounds.Width), Math.Max(1, (int)v.Bounds.Height));
        using var rtb = new RenderTargetBitmap(size);
        rtb.Render(v);
        using var ms = new MemoryStream();
        rtb.Save(ms);
        ms.Position = 0;
        return SKBitmap.Decode(ms);
    }

    static bool Near(SKColor c, Color want, int by = 6) =>
        Math.Abs(c.Red - want.R) <= by && Math.Abs(c.Green - want.G) <= by && Math.Abs(c.Blue - want.B) <= by;

    static bool Near(SKColor c, Vector3 want, int by = 6) =>
        Near(c, Color.FromRgb((byte)(want.X * 255), (byte)(want.Y * 255), (byte)(want.Z * 255)), by);

    static int Differing(SKBitmap a, SKBitmap b)
    {
        int n = 0;
        for (int y = 0; y < a.Height; y += 2)
            for (int x = 0; x < a.Width; x += 2)
                if (a.GetPixel(x, y) != b.GetPixel(x, y)) n++;
        return n;
    }

    static MenuItem? Entry(IEnumerable<Control> items, string header) =>
        items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == header);

    static void Click(MenuItem? m)
    {
        Check(m != null, "the menu has the entry");
        m?.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
    }

    /// <summary>The whole window with the 3D view in it: the view is OpenGL, so it's pictured apart and put in its place</summary>
    static async Task FullShot(MainWindow w, string name)
    {
        using var win = Picture(w);
        var at = w.View.TranslatePoint(default, w) ?? default;
        int wide = Math.Max(1, (int)w.View.Bounds.Width), high = Math.Max(1, (int)w.View.Bounds.Height);
        using var view = await w.View.Capture(wide, false, high);
        using (var canvas = new SKCanvas(win))
        {
            canvas.DrawBitmap(view, (float)at.X, (float)at.Y);
            // What lies over the view: the label, the speed, the map, the banner
            if (w.View.Parent is Panel over)
                foreach (var c in over.Children)
                {
                    if (c == w.View || !c.IsVisible || c.Bounds.Width < 1 || c.Bounds.Height < 1) continue;
                    using var top = Picture(c);
                    var p = c.TranslatePoint(default, w) ?? default;
                    canvas.DrawBitmap(top, (float)p.X, (float)p.Y);
                }
        }
        SavePng(win, name);
    }

    /// <summary>RvCarStudio -rvpath FOLDER --look SHOTS   Only the looks: pictures of every page in each, and the checks on them. Writes nothing into the folder</summary>
    public static async Task RunLook(MainWindow w, string shotDir)
    {
        Running = true;
        shots = shotDir;
        Directory.CreateDirectory(shots);
        log = new StreamWriter(Path.Combine(shots, "look.txt"));
        try
        {
            await Wait(1500);
            await LookTest(w);
        }
        catch (Exception ex) { Check(false, ex.ToString()); }
        Log($"passed {pass}, failed {fail}");
        log.Dispose();
        Environment.Exit(fail == 0 ? 0 : 1);
    }

    // ------------------------------------------------------------------ in the view

    static async Task InViewTest(MainWindow w)
    {
        var d = w.Doc;
        d.MoveMode = MoveMode.Mirror;
        d.Snap = 0;
        w.PickCar("rc");
        w.SetTool(GizmoTool.Move);
        w.View.SetView(ViewMode.Perspective);
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
        w.View.FrameCar();
        d.Select(new Selection(SelKind.None));
        w.View.Focus();
        await Wait(200);
        var corner = new Point(30, 30);
        var wheelPos = d.D!.Wheels[0].Offset1;
        var at = w.View.ScreenOf(wheelPos)!.Value;

        // Under the mouse: outlined and named
        // A window that the real mouse isn't over is told so right after each move, and the label goes: so these moves are made directly
        w.View.MoveTo(corner);
        Check(w.View.Hovered.Kind == SelKind.None && w.ViewTipText == "", "nothing under the mouse, no label: '" + w.ViewTipText + "'");
        using (var plain = await w.View.Capture(480, false))
        {
            w.View.MoveTo(at);
            var over = w.View.Hovered;
            Check(over.Kind is SelKind.Wheel or SelKind.Axle or SelKind.Spring or SelKind.Pin, "the part under the mouse is the hovered one: " + over.Label);
            Check(w.ViewTipText == over.Label && over.Label != "", "its name is beside the mouse: '" + w.ViewTipText + "'");
            Check(d.Sel.Kind == SelKind.None, "hovering selects nothing");
            using var lit = await w.View.Capture(480, false);
            Check(Differing(plain, lit) > 20, "the hovered part is outlined: " + Differing(plain, lit));
            SavePng(lit, "inview_hover");
            WindowShot(w, "inview_hover_window");
        }
        // Somewhere on the body: its middle may be behind a wheel or under the roof from here, so a few places are tried
        var shape = d.Model(d.D.BodyModel)!;
        var mid = d.D.BodyOffset + (shape.Min + shape.Max) / 2;
        foreach (var lift in new[] { 0f, -4f, -8f, -12f, 4f })
            foreach (var along in new[] { 0f, 12f, -12f, 24f, -24f })
            {
                if (w.View.Hovered.Kind == SelKind.Body) break;
                if (w.View.ScreenOf(mid + new Vector3(0, lift, along)) is { } on) w.View.MoveTo(on);
            }
        Check(w.View.Hovered.Kind == SelKind.Body && w.ViewTipText == "Body", "moving on names the next part: " + w.ViewTipText);
        w.View.MoveTo(corner);
        Check(w.View.Hovered.Kind == SelKind.None && w.ViewTipText == "", "and off the car the label goes");
        await Send(w, MOVE, at);
        await Wait(100);
        Check(w.View.Hovered.Kind == SelKind.None && w.ViewTipText == "", "when the mouse leaves the view the label goes too");

        // Double click frames the part
        var dist = w.View.Distance;
        await Send(w, MOVE, at);
        await Send(w, LDOWN, at, 1);
        await Send(w, LUP, at);
        await Send(w, LDOWN, at, 1);
        await Send(w, LUP, at);
        await Wait(60);
        Check(d.Sel.Kind is SelKind.Wheel or SelKind.Axle or SelKind.Spring or SelKind.Pin, "double click selects: " + d.Sel.Label);
        Check(w.View.Distance < dist * 0.7f, $"double click frames the part: distance {dist:0} to {w.View.Distance:0}");
        Check((w.View.Target - wheelPos).Length() < 40, "and looks at it: " + (w.View.Target - wheelPos).Length());
        await GlShot(w, "inview_framed", 480);
        w.View.DoubleClickAt(corner);
        Check(Math.Abs(w.View.Distance - dist) < 1, "double click on nothing frames the whole car: " + w.View.Distance);
        d.Select(new Selection(SelKind.CoM));
        Check(w.View.FramePart(d.Sel) && (w.View.Target - d.D.CoM).Length() < 0.01f, "a point can be framed too");
        w.View.FrameCar();
        d.Select(new Selection(SelKind.None));
        await Wait(100);

        // Right click: the part's menu. A right drag still only turns the view
        at = w.View.ScreenOf(wheelPos)!.Value;
        var middle = new Point(w.View.Bounds.Width / 2, w.View.Bounds.Height / 2);
        var yaw = w.View.Yaw;
        await DragWith(w, RDOWN, RUP, 2, middle, middle + new Point(80, 0));
        await Wait(100);
        Check(!w.PartMenuOpen && Math.Abs(w.View.Yaw - yaw) > 0.2f, "a right drag turns the view and opens no menu");
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
        w.View.FrameCar();
        await Wait(100);
        await Send(w, MOVE, at);
        await Send(w, RDOWN, at, 2);
        await Send(w, RUP, at);
        await Wait(300);
        Check(w.PartMenuOpen, "right click on a part opens its menu");
        Check(d.Sel.Kind is SelKind.Wheel or SelKind.Axle or SelKind.Spring or SelKind.Pin, "and selects it: " + d.Sel.Label);
        w.ClosePartMenu();
        await Wait(200);
        Check(!w.PartMenuOpen, "the menu closes");
        await Send(w, MOVE, corner);
        await Send(w, RDOWN, corner, 2);
        await Send(w, RUP, corner);
        await Wait(200);
        Check(!w.PartMenuOpen, "right click on nothing: no menu");

        var wheel = new Selection(SelKind.Wheel, 0);
        d.Select(wheel);
        int all = w.View.DrawnParts;
        var names = w.PartMenuItems(wheel).OfType<MenuItem>().Select(m => m.Header as string).ToList();
        foreach (var want in new[] { "Hide", "Replace model...", "Remove", "Frame", "Move", "Rotate", "Scale", "Mirror mode" })
            Check(names.Contains(want), "a part's menu has " + want + ": " + string.Join(", ", names));
        Click(Entry(w.PartMenuItems(wheel), "Hide"));
        Check(d.Hidden.Contains(wheel) && w.View.DrawnParts == all - 1 && !d.Modified, "Hide in the menu hides it in the view only");
        Click(Entry(w.PartMenuItems(wheel), "Show"));
        Check(!d.Hidden.Contains(wheel) && w.View.DrawnParts == all, "and the menu then offers Show");
        dist = w.View.Distance;
        Click(Entry(w.PartMenuItems(wheel), "Frame"));
        Check(w.View.Distance < dist * 0.7f, "Frame in the menu");
        w.View.FrameCar();
        Click(Entry(w.PartMenuItems(wheel), "Rotate"));
        Check(d.Tool == GizmoTool.Rotate, "Rotate in the menu");
        Click(Entry(w.PartMenuItems(wheel), "Scale"));
        Check(d.Tool == GizmoTool.Scale, "Scale in the menu");
        Click(Entry(w.PartMenuItems(wheel), "Move"));
        Check(d.Tool == GizmoTool.Move, "Move in the menu");
        var mirror = Entry(w.PartMenuItems(wheel), "Mirror mode");
        Click(Entry(mirror?.Items.OfType<Control>() ?? Array.Empty<Control>(), "All four"));
        Check(d.MoveMode == MoveMode.All, "mirror mode from the menu: " + d.MoveMode);
        mirror = Entry(w.PartMenuItems(wheel), "Mirror mode");
        Check(mirror?.Items.OfType<MenuItem>().Single(m => m.IsChecked).Header as string == "All four", "the menu marks the mode in use");
        Click(Entry(mirror?.Items.OfType<Control>() ?? Array.Empty<Control>(), "Mirror L/R"));
        Check(d.MoveMode == MoveMode.Mirror, "and back");
        Check(Entry(w.PartMenuItems(new Selection(SelKind.CoM)), "Remove") == null && Entry(w.PartMenuItems(new Selection(SelKind.CoM)), "Rotate") == null,
            "a point's menu has no Remove or Rotate");
        Click(Entry(w.PartMenuItems(wheel), "Remove"));
        Check(!d.D!.Wheels[0].Present && !d.D.Wheels[1].Present && d.D.Wheels[2].Present, "Remove in the menu takes the wheel and its mirror off");
        d.Undo();
        Check(d.D!.Wheels[0].Present && d.D.Wheels[1].Present, "undo brings them back");
        d.Select(wheel);
        Click(Entry(w.PartMenuItems(wheel), "Replace model..."));
        await Wait(500);
        var picker = w.OwnedWindows.OfType<ModelPicker>().FirstOrDefault();
        Check(picker != null, "Replace model... opens the model picker");
        picker?.Close();
        await Wait(200);

        // The amount of a drag, beside the mouse
        d.Select(wheel);
        w.SetTool(GizmoTool.Move);
        w.View.Focus();
        await Wait(80);
        wheelPos = d.D!.Wheels[0].Offset1;
        var otherPos = d.D.Wheels[1].Offset1;
        int depth = d.UndoDepth;
        var from = w.View.ScreenOf(w.View.AxisTip(wheelPos, 2))!.Value;
        var far = w.View.ScreenOf(w.View.AxisTip(wheelPos, 2) + new Vector3(0, 0, 8))!.Value;
        w.View.PressLeft(from);
        for (int i = 1; i <= 5; i++) w.View.MoveTo(new Point(from.X + (far.X - from.X) * i / 5, from.Y + (far.Y - from.Y) * i / 5));
        var moved = d.D.Wheels[0].Offset1.Z - wheelPos.Z;
        var tip = w.ViewTipText;
        Check(tip.StartsWith("Move Z: +") && tip.Contains("units") && tip.Contains("cm"), "a drag shows its amount: '" + tip + "'");
        Check(tip.Contains(moved.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)) && tip.Contains((moved / 2).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + " cm"),
            $"in units and in cm: moved {moved}, '{tip}'");
        WindowShot(w, "inview_drag_amount");

        // Typing during the drag sets it exactly
        await Keys(w, '1', '2', VK_PERIOD, '5');
        Check(w.View.Entering, "typing a number during a drag starts an exact amount");
        Check(Math.Abs(d.D.Wheels[0].Offset1.Z - wheelPos.Z - 12.5f) < 1e-3f && d.D.Wheels[0].Offset1.X == wheelPos.X, "12.5 typed: moved exactly that: " + (d.D.Wheels[0].Offset1 - wheelPos));
        Check(Math.Abs(d.D.Wheels[1].Offset1.Z - otherPos.Z - 12.5f) < 1e-3f, "the mirrored wheel too");
        Check(w.ViewTipText.Contains("12.5") && w.ViewTipText.Contains("6.25 cm") && w.ViewTipText.Contains("Enter"), "the label shows what's typed: '" + w.ViewTipText + "'");
        WindowShot(w, "inview_typed_amount");
        w.View.MoveTo(far + new Point(40, 10));
        Check(Math.Abs(d.D.Wheels[0].Offset1.Z - wheelPos.Z - 12.5f) < 1e-3f, "the mouse no longer changes a typed amount");
        await Keys(w, VK_BACK);
        Check(Math.Abs(d.D!.Wheels[0].Offset1.Z - wheelPos.Z - 12f) < 1e-3f, "backspace: 12.");
        await Keys(w, VK_ENTER);
        Check(!w.View.Entering && w.ViewTipText == "", "Enter ends it");
        w.View.ReleaseAt(far);
        Check(Math.Abs(d.D!.Wheels[0].Offset1.Z - wheelPos.Z - 12f) < 1e-3f, "and the amount stays after the button is let go");
        Check(d.UndoDepth == depth + 1, "all of it is one undo step: " + (d.UndoDepth - depth));
        d.Undo();
        Check(d.D!.Wheels[0].Offset1 == wheelPos && d.D.Wheels[1].Offset1 == otherPos, "which takes it all back");

        // Esc takes a drag back
        w.View.PressLeft(from);
        for (int i = 1; i <= 5; i++) w.View.MoveTo(new Point(from.X + (far.X - from.X) * i / 5, from.Y + (far.Y - from.Y) * i / 5));
        Check(d.D.Wheels[0].Offset1 != wheelPos, "dragging again");
        await Keys(w, VK_ESC);
        Check(d.D!.Wheels[0].Offset1 == wheelPos && d.UndoDepth == depth && !d.CanRedo, "Esc during a drag puts it back, with nothing left to undo or redo");
        w.View.MoveTo(far);
        w.View.ReleaseAt(far);
        Check(d.D!.Wheels[0].Offset1 == wheelPos, "and the rest of that drag does nothing");

        // From the keyboard: W, an amount, Enter
        d.Select(new Selection(SelKind.Body));
        w.View.Focus();
        await Wait(60);
        var bodyAt = d.D.BodyOffset;
        await Keys(w, 'W');
        Check(d.Tool == GizmoTool.Move && w.ViewTipText.StartsWith("Move X"), "W offers an amount to type: '" + w.ViewTipText + "'");
        await Keys(w, '5');
        Check(d.D!.BodyOffset == bodyAt + new Vector3(5, 0, 0), "W 5: five units along X: " + (d.D.BodyOffset - bodyAt));
        await Keys(w, 'Y');
        Check(d.D!.BodyOffset == bodyAt + new Vector3(0, 5, 0), "Y: the same amount along Y instead: " + (d.D.BodyOffset - bodyAt));
        await Keys(w, VK_BACK, VK_MINUS, '3');
        Check(d.D!.BodyOffset == bodyAt + new Vector3(0, -3, 0), "backspace, -3: " + (d.D.BodyOffset - bodyAt));
        Check(w.View.View == ViewMode.Perspective, "the digits didn't change the view");
        await Keys(w, VK_ENTER);
        Check(!w.View.Entering && d.UndoDepth == depth + 1, "Enter: one step to undo");
        d.Undo();
        Check(d.D!.BodyOffset == bodyAt, "undo of a typed move");

        await Keys(w, 'W', '7');
        Check(d.D!.BodyOffset == bodyAt + new Vector3(7, 0, 0), "W 7");
        await Keys(w, VK_ESC);
        Check(d.D!.BodyOffset == bodyAt && d.UndoDepth == depth && !w.View.Entering, "Esc cancels what was typed");

        // E: degrees
        var model = d.Model(d.D.BodyModel)!;
        var v0 = model.Verts[0].Pos;
        var size0 = model.Max - model.Min;
        await Keys(w, 'E', '9', '0');
        Check(d.Tool == GizmoTool.Rotate && w.ViewTipText.StartsWith("Rotate Y: 90"), "E 90: '" + w.ViewTipText + "'");
        await Keys(w, VK_ENTER);
        var v1 = model.Verts[0].Pos;
        Check((v1 - new Vector3(v0.Z, v0.Y, -v0.X)).Length() < 0.01f, $"turned a quarter around Y exactly: {v0} to {v1}");
        d.Undo();
        Check(model.Verts[0].Pos == v0, "undo of a typed turn");

        // R: times the size
        await Keys(w, 'R', '1', VK_PERIOD, '5');
        Check(d.Tool == GizmoTool.Scale && w.ViewTipText.Contains("150%"), "R 1.5: '" + w.ViewTipText + "'");
        await Keys(w, VK_ENTER);
        Check(((model.Max - model.Min) - size0 * 1.5f).Length() < 0.01f, "one and a half times the size, all ways");
        d.Undo();
        Check(((model.Max - model.Min) - size0).Length() < 0.001f, "undo of a typed size");
        await Keys(w, 'R', 'Z', '2', VK_ENTER);
        var size2 = model.Max - model.Min;
        Check(Math.Abs(size2.Z - size0.Z * 2) < 0.01f && Math.Abs(size2.X - size0.X) < 0.001f, "R Z 2: twice as long only");
        d.Undo();

        // Any other key ends the offer, so 2 is the top view again
        await Keys(w, 'W', 'F', '2');
        Check(w.View.View == ViewMode.Top && d.D!.BodyOffset == bodyAt && !w.View.Entering, "after another key the digits are views again");
        w.View.SetView(ViewMode.Perspective);
        // A point only moves: E offers nothing
        d.Select(new Selection(SelKind.CoM));
        var com = d.D!.CoM;
        await Keys(w, 'E', '4');
        Check(!w.View.Entering && d.D!.CoM == com, "a point can't be turned by typing either");
        w.View.SetView(ViewMode.Perspective);

        w.SetTool(GizmoTool.Move);
        d.Select(new Selection(SelKind.None));
        d.Snap = 0.5f;
        d.DirtyModels.Clear();
        w.PickCar("rc");
        await Wait(100);
    }

    // ------------------------------------------------------------------ themes

    static async Task LookTest(MainWindow w)
    {
        var d = w.Doc;
        w.PickCar("rc");
        w.View.SetView(ViewMode.Perspective);
        w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
        w.View.FrameCar();
        d.Select(new Selection(SelKind.Wheel, 0));
        w.Tabs.SelectedIndex = 0;
        await Wait(300);
        Check(Look.Kind == ThemeKind.Classic, "a test run starts in the classic look");
        var toolbar = new SKPointI(2, 31);
        SKColor classicPanel, classicView;
        using (var p = Picture(w)) classicPanel = p.GetPixel(toolbar.X, toolbar.Y);
        using (var v = await w.View.Capture(320, false)) classicView = v.GetPixel(2, 2);
        Check(Near(classicPanel, Color.Parse("#23262D")), "classic panels as they were: " + classicPanel);
        Check(Near(classicView, new Vector3(0.17f, 0.19f, 0.22f)), "classic view background as it was: " + classicView);

        foreach (var kind in new[] { ThemeKind.Dark, ThemeKind.Light })
        {
            w.SetTheme(kind);
            await Wait(400);
            bool light = kind == ThemeKind.Light;
            Check(Look.Kind == kind, "theme set: " + kind);
            Check(Application.Current!.ActualThemeVariant == (light ? ThemeVariant.Light : ThemeVariant.Dark), "the controls follow: " + Application.Current.ActualThemeVariant);
            var want = Look.Panel.Color;
            Check(light ? Look.Contrast(want, Colors.White) < 1.15 : Look.Contrast(want, Colors.Black) < 1.15, kind + " panels: ghost white or all but black: " + want);

            // Colours on screen, not just in the table
            using (var p = Picture(w))
            {
                var bar = p.GetPixel(toolbar.X, toolbar.Y);
                var status = p.GetPixel(p.Width - 4, p.Height - 4);
                var menu = p.GetPixel(p.Width - 4, 4);
                Check(Near(bar, want) && bar != classicPanel, $"{kind}: the toolbar is drawn in it: {bar}");
                Check(Near(status, Look.Bar.Color), $"{kind}: the status bar: {status}");
                Check(Near(menu, Look.Bar.Color), $"{kind}: the menu bar: {menu}");
                // Text is there, and reads: the darkest and lightest pixels of the toolbar are far apart
                double lo = 1, hi = 0;
                for (int x = 0; x < p.Width; x++)
                    for (int y = 30; y < 58; y++)
                    {
                        var c = p.GetPixel(x, y);
                        double l = (0.2126 * c.Red + 0.7152 * c.Green + 0.0722 * c.Blue) / 255;
                        lo = Math.Min(lo, l); hi = Math.Max(hi, l);
                    }
                Check(hi - lo > 0.6, $"{kind}: toolbar text stands out: {lo:0.00} to {hi:0.00}");
            }
            await FullShot(w, "theme_" + kind + "_window");

            // The 3D view: background, floor and grid
            using (var v = await w.View.Capture(640, false))
            {
                var back = v.GetPixel(2, 2);
                Check(Near(back, Look.ViewBack) && back != classicView, $"{kind}: the 3D view's background: {back}");
                Check(Near(back, want, 20), $"{kind}: which is the theme's own: {back}");
                var floor = v.GetPixel(4, v.Height - 4);
                var grid = new HashSet<SKColor>();
                for (int x = 0; x < v.Width; x++) grid.Add(v.GetPixel(x, v.Height - 40));
                Check(Near(floor, Look.Floor, 10) || grid.Any(c => Near(c, Look.Floor, 10)), $"{kind}: the floor: {floor}");
                Check(grid.Count >= 2, $"{kind}: the grid shows on the floor: {grid.Count} colours");
                SavePng(v, "theme_" + kind + "_view");
            }
            w.View.ShowHull = true; w.View.ShowTravel = true; w.View.Redraw();
            await GlShot(w, "theme_" + kind + "_helpers", 640);
            w.View.ShowHull = false; w.View.ShowTravel = false; w.View.Redraw();

            // Every colour of text against the panel it's on
            foreach (var (name, brush) in new (string, SolidColorBrush)[]
                     {
                         ("text", Look.Text), ("faint text", Look.Faint), ("headers", Look.Accent), ("good", Look.Good),
                         ("warnings", Look.Warn), ("errors", Look.Bad), ("not saved", Look.Modified)
                     })
            {
                var c = Look.Contrast(brush.Color, Look.Panel.Color);
                Check(c >= 4.5, $"{kind}: {name} reads on the panels: contrast {c:0.0}");
                Log($"{kind}: {name} contrast {c:0.0}");
            }
            foreach (var fill in new[] { Look.FillFrom, Look.FillTo })
                Check(Look.Contrast(Look.OnAccent.Color, fill) >= 4.5, $"{kind}: text on the accent colour reads: {Look.Contrast(Look.OnAccent.Color, fill):0.0}");
            Check(Look.Contrast(Look.Text.Color, Look.Banner.Color) >= 4.5, $"{kind}: the stock car banner reads");

            // Every tab
            for (int i = 0; i < w.Tabs.ItemCount; i++)
            {
                w.Tabs.SelectedIndex = i;
                await Wait(150);
                await FullShot(w, "theme_" + kind + "_tab_" + i);
            }
            w.Tabs.SelectedIndex = 0;

            // The list of cars
            w.ChangeCar();
            await Wait(2500);
            await FullShot(w, "theme_" + kind + "_cars");
            w.SetLocked(true);

            // Dialogs
            var asking = Dialogs.Ask(w, "Save the changes of RC Bandit?", "Save", "Don't save", "Cancel");
            await Wait(400);
            if (w.OwnedWindows.FirstOrDefault() is { } dlg)
            {
                using var p = Picture(dlg);
                Check(Near(p.GetPixel(3, 3), want), $"{kind}: a question is in it too: {p.GetPixel(3, 3)}");
                SavePng(p, "theme_" + kind + "_dialog");
                dlg.Close();
            }
            else Check(false, kind + ": the question opened");
            await asking;
            var picker = new ModelPicker(d.Game!, "rc", "Model for the test");
            picker.Show(w);
            await Wait(1200);
            using (var p = Picture(picker))
            {
                Check(Near(p.GetPixel(3, 3), want), $"{kind}: the model picker: {p.GetPixel(3, 3)}");
                SavePng(p, "theme_" + kind + "_picker");
            }
            picker.Close();
            var wizard = new NewCarWizard(d.Game!, "rc");
            wizard.Show(w);
            await Wait(500);
            using (var p = Picture(wizard))
            {
                Check(Near(p.GetPixel(3, 3), want), $"{kind}: the new car window: {p.GetPixel(3, 3)}");
                SavePng(p, "theme_" + kind + "_wizard");
            }
            wizard.Close();
            await Wait(200);

            // A popup: the part's menu
            w.View.ContextAt(w.View.ScreenOf(d.D!.Wheels[0].Offset1)!.Value);
            await Wait(400);
            Check(w.PartMenuOpen, kind + ": the part's menu opens");
            if (w.PartMenuRoot is { } root)
            {
                using var p = Picture(root);
                var inside = p.GetPixel(p.Width / 2, 5);
                Check(light ? inside.Red > 225 : inside.Red < 30, $"{kind}: menus are in it: {inside}");
                SavePng(p, "theme_" + kind + "_menu");
            }
            else Check(false, kind + ": the menu can be pictured");
            w.ClosePartMenu();
            await Wait(200);

            // Driving: the floor and the speed
            w.StartDrive(SimGround.Flat);
            w.AutoThrottle = 1; w.AutoSteer = 0;
            await Wait(900);
            await FullShot(w, "theme_" + kind + "_drive_window");
            await GlShot(w, "theme_" + kind + "_drive", 480);
            w.AutoThrottle = float.NaN;
            w.StopDrive();
            await Wait(150);
            w.View.Yaw = 0.8f; w.View.Pitch = 0.35f;
            w.View.FrameCar();
        }

        // The toolbar button goes around: Light, Classic, Dark
        Check(Look.Kind == ThemeKind.Light, "light was last");
        Check(w.ThemeButton.Content as string == "◐  Light", "the toolbar button names the look: " + w.ThemeButton.Content);
        w.ThemeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Wait(300);
        Check(Look.Kind == ThemeKind.Classic, "the toolbar button changes the look: " + Look.Kind);
        using (var p = Picture(w)) Check(p.GetPixel(toolbar.X, toolbar.Y) == classicPanel, "classic is back as it was: " + p.GetPixel(toolbar.X, toolbar.Y));
        using (var v = await w.View.Capture(320, false)) Check(v.GetPixel(2, 2) == classicView, "and its view: " + v.GetPixel(2, 2));
        w.ThemeButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        await Wait(200);
        Check(Look.Kind == ThemeKind.Dark, "and on to dark");
        Check(w.ThemeMenuChecked == ThemeKind.Dark, "the View menu marks it: " + w.ThemeMenuChecked);
        w.SetTheme(ThemeKind.Classic);
        await Wait(300);
        d.Select(new Selection(SelKind.None));
    }
}
