using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// Easy mode: RV Car Studio laid out as it was, in today's look. The row of buttons (Pro editor, save, other car), the
/// Edit Menu with its list of modes and the &lt; &gt; beside it, the Render Settings with the drawing of the car to click,
/// and the tips under the view. Pro is the window of RV Car Studio 3. The tab pages are the same ones in both; the old
/// modes that are a part of a longer page have a page of their own here.
/// </summary>
public sealed partial class MainWindow
{
    /// <summary>One line of the Edit Menu: the tab it shows, the parts it switches between, the tips it picks from</summary>
    sealed record EasyMode(string Name, int Tab, (string Text, Selection Sel)[]? Parts, params string[] Tips)
    {
        public bool Has(Selection s) => Parts?.Any(p => p.Sel == s) == true;
    }

    readonly List<EasyMode> easyModes = new();
    readonly HashSet<Page> easyPages = new();
    public readonly ComboBox EasyBox = new() { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 26 };
    readonly Avalonia.Controls.Primitives.UniformGrid easyParts = new() { Margin = new Thickness(12, 6, 10, 0) };
    readonly List<(RadioButton B, Selection S)> easyPartButtons = new();
    Grid rootGrid = null!, mainGrid = null!, midGrid = null!;
    Control toolBar = null!, easyBar = null!, easyHead = null!, easyFoot = null!, issuesBox = null!, issuesSplit = null!, leftSide = null!, leftSplit = null!, tipBox = null!;
    Border rightBox = null!, viewFrame = null!;
    readonly TextBlock easyTitle = new() { Text = "", FontSize = 13, Foreground = Look.Faint, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock easyName = new() { FontSize = 12, Margin = new Thickness(10, 0), VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeight.SemiBold };
    readonly TextBlock easyApp = new() { Text = "RV Car Studio", FontSize = 12, Margin = new Thickness(10, 0), VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock tipText = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock tipWho = new() { FontSize = 11, Foreground = Look.Faint, HorizontalAlignment = HorizontalAlignment.Right };
    readonly CheckBox easyAutoSave = new() { Content = "Auto save parameters", FontSize = 11, MinHeight = 20 };
    readonly TextBox easyConsole = new() { IsReadOnly = true, AcceptsReturn = true, FontSize = 11, TextWrapping = TextWrapping.Wrap, MinHeight = 320 };
    readonly Image easyCarbox = new() { Width = 192, Height = 192, Stretch = Stretch.Uniform };
    bool easyPicking, issuesAsked, devRun;
    int proTab;

    public bool EasyShown => easyHead.IsVisible;
    public string EasyTip => tipText.Text ?? "";
    public IReadOnlyList<string> EasyNames => easyModes.Select(m => m.Name).ToList();

    // ---------------------------- the pages of the old modes ----------------------------

    /// <summary>The Edit Menu's modes, in the old order. Called at the end of BuildTabs: the pages of their own come after the Pro tabs</summary>
    void BuildEasyTabs()
    {
        int Own(string name, Action<Page> fill)
        {
            var p = NewPage();
            easyPages.Add(p);
            fill(p);
            int at = Tabs.ItemCount;
            AddTab("Easy: " + name, p);
            return at;
        }
        CarData D() => Doc.D!;
        (string, Selection)[] Corners(SelKind k, string before = "") => (before == "" ? new[] { "Front left", "Front right", "Rear left", "Rear right" } : new[] { "FL", "FR", "RL", "RR" }).Select((t, i) => (before == "" ? t : before + " " + t, new Selection(k, i))).ToArray();

        void Mode(string name, int tab, (string, Selection)[]? parts = null, params string[] tips) => easyModes.Add(new EasyMode(name, tab, parts, tips));

        Mode("Main", Own("Main", p =>
        {
            CarPage(p, false);
            p.Check("ClothFX", () => D().ClothFx, v => D().ClothFx = v, "Mystery's cloth: needs cloth polygons flagged in the model");
            p.Check("Flying", () => D().Flying, v => D().Flying = v, "As the UFO car");
            p.Check("Flippable", () => D().Flippable, v => D().Flippable = v, "As Rotor");
            p.Header("Actions");
            p.Button("Open car folder", OpenFolder);
            p.Button("Reload car", async () => await ReloadCar(), "Reads the car again from its files");
            p.Button("Reload bitmap", () => { Doc.TextureVersion++; View.Redraw(); Say("Texture read again"); });
            p.Button("Check car", ToggleIssues, "What's wrong with the car, listed under the view");
            p.Button("Re-Volt", () => TestRunGame(devRun), "Runs the car in RVGL (F5)", true);
            var dev = new CheckBox { Content = "-dev", FontSize = 12, MinHeight = 22 };
            ToolTip.SetTip(dev, "Dev mode: Ctrl+R in the game reloads the car");
            dev.IsCheckedChanged += (_, _) => devRun = dev.IsChecked == true;
            p.Add(dev);
            p.Button("Drive here", () => StartDrive(SimGround.Flat), "The original game's physics, on a plain floor (F6)");
        }), null, "name", "clothfx", "flying", "flippable", "envcolor");
        Mode("Texture", PaintTabIndex);
        Mode("Handling", Own("Handling", p => HandlingPage(p)), null, "statfric", "kineticfric");
        Mode("Frontend Stuffs", Own("Frontend", p => FrontendPage(p)));
        Mode("Body", 0, new[] { ("Body", new Selection(SelKind.Body)), ("Centre of mass", new Selection(SelKind.CoM)), ("Weapon", new Selection(SelKind.Weapon1)) }, "mass", "inertia", "body", "bodydyn", "bodygeo");
        Mode("Wheels", 0, Corners(SelKind.Wheel), "statfric", "kineticfric");
        Mode("Suspensions", 0, Corners(SelKind.Spring, "Spring").Concat(Corners(SelKind.Axle, "Axle")).Concat(Corners(SelKind.Pin, "Pin")).ToArray());
        Mode("Spinner", 0, new[] { ("Spinner", new Selection(SelKind.Spinner)) });
        Mode("Aerial", 0, new[] { ("Base", new Selection(SelKind.AerialBase)), ("Tip", new Selection(SelKind.AerialTip)) });
        Mode("AI", Own("AI", p => AiPage(p)));
        Mode("Camera", Own("Camera", CameraRows));
        Mode("Collision", Own("Collision", p =>
        {
            p.Header("Collision hull");
            p.TextWithButton("Hull", () => D().Coll, v => { D().Coll = v; Doc.LoadHull(); }, "Pick",
                async () => await PickInto("Collision hull", new[] { "*.hul" }, null, (a, b) => File.Copy(a, b, true), rel => { D().Coll = rel; Doc.LoadHull(); }, FileChecks.Hull), "Collision hull, .hul: what the car bumps the world with");
            HullRows(p);
            p.Button("Show and edit the hull", () => { ShowHull(true); if (Doc.Hull is { } h && h.Convex.Count > 0) Doc.Select(new Selection(SelKind.HullPart, 0)); else if (Doc.Hull is { } h2 && h2.Spheres.Count > 0) Doc.Select(new Selection(SelKind.HullSphere, 0)); Tabs.SelectedIndex = 0; }, "Its parts and spheres, to drag in the view");
            p.Button("Hide the hull", () => ShowHull(false));
            p.Num("Hardness", () => D().Hardness, v => D().Hardness = v, "How much the body bounces when it hits something");
        }));
        Mode("Carbox", Own("Carbox", p =>
        {
            p.Header("Carbox");
            p.Add(new Border { Child = easyCarbox, BorderBrush = Look.Line, BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 4) });
            p.Info(() => { ShowCarbox(); easyCarbox.Source = carboxImage.Source; return carboxShown == null ? "No carbox yet" : Names.File(carboxShown); });
            p.Button("Carbox maker...", async () => { await OpenCarboxMaker(); p.Refresh(); }, "The car from the angle you like, its name as you want it", true);
            p.Button("Quick carbox", async () => { await MakeCarbox(); p.Refresh(); });
            p.Button("A picture of my own...", async () => { await CarboxFromPicture(); p.Refresh(); });
        }));
        Mode("Shade", ShadeTabIndex);
        Mode("Shadow", Own("Shadow", p =>
        {
            p.Header("Shadow");
            p.TextWithButton("Texture", () => D().TShadow, v => D().TShadow = v, "Pick", async () => await PickShadow(), "The dark patch under the car");
            ShadowRows(p);
            p.Button("Make shadow", () => Tool("Shadow", () => Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!))), "From the car's own shape", true);
        }));
        Mode("Misc.", Own("Misc", p =>
        {
            SoundRows(p);
            p.Header("Weapons");
            p.Vec("Shockwave", () => D().Weapon, v => D().Weapon = v);
            p.Vec("Water bomb", () => D().Weapon2, v => { D().Weapon2 = v; D().HasWeapon2 = true; });
            p.Header("Camber, degrees (RVGL)");
            for (int i = 0; i < 4; i++) { int k = i; p.Num(char.ToUpper(Doc.WheelName(k)[0]) + Doc.WheelName(k)[1..], () => D().Wheels[k].Camber, v => D().Wheels[k].Camber = v, null, -90, 90); }
            p.Header("Skins found");
            p.Info(() => Doc.Car?.FindSkins() is { Count: > 1 } s ? string.Join(", ", s.Select(Names.Stem)) : "No other skins");
        }));
        Mode("Readme", Own("Readme", p =>
        {
            p.Header("Readme generator");
            p.Note("Tells players who made the car, how it drives, how to install it and what they may do with it.");
            p.Button("Readme...", async () => await OpenReadme(), "Answer a few questions, see it as it will be, write it", true);
            p.Button("Read its readme", async () => await ViewReadme());
        }));
        Mode("Packing", Own("Packing", p =>
        {
            p.Header("Pack the car for sharing");
            p.Info(() => Doc.Car == null ? "" : Doc.Car.Folder + ": " + Doc.Car.Game.CarFiles(Doc.Car.Folder).Count() + " files");
            p.Button("Pack as zip...", async () => await PackZip(), "Saves the car, writes its readme and the zip players unzip on their game folder", true);
        }));
        Mode("About", Own("About", p =>
        {
            p.Header("RV Car Studio 3");
            p.Note("By Kallel A.Y (theKDL). GPL v3. Re-Volt is a trademark of its owners.");
            p.Note("This is the Easy mode: the Car Studio as it was. Pro editor, above, has everything.");
            p.Button("Guide and calculations", () => { SetMode(true); ShowGuide(); }, "Opens in Pro mode (F1)");
        }));
        Mode("Console", Own("Console", p =>
        {
            p.Header("Console");
            p.Live(() => easyConsole.Text = string.Join(Environment.NewLine, messages));
            p.Add(easyConsole);
        }));

        EasyBox.ItemsSource = EasyNames;
        EasyBox.SelectionChanged += (_, _) => { if (!easyPicking && EasyBox.SelectedIndex >= 0) EasyGo(EasyBox.SelectedIndex); };
        foreach (var t in Tabs.Items.OfType<TabItem>().Where(t => (t.Header as string)?.StartsWith("Easy: ") == true)) t.IsVisible = false;
    }

    async Task ReloadCar()
    {
        if (Doc.Car == null) return;
        if (Doc.Modified && await Dialogs.Ask(this, "Read " + Doc.D!.Name + " again from its files? Your unsaved changes would go.", "Read it again", "Cancel") != 0) return;
        Revert();
    }

    void ToggleIssues()
    {
        issuesAsked = !issuesAsked;
        RunChecks();
        ShowIssues();
    }

    void ShowIssues()
    {
        bool on = ProMode || issuesAsked;
        issuesBox.IsVisible = issuesSplit.IsVisible = on;
        midGrid.RowDefinitions[2].Height = new GridLength(on ? 150 : 0);
    }

    // ---------------------------- going from mode to mode ----------------------------

    /// <summary>Shows a mode of the Edit Menu, by its place in the list</summary>
    public void EasyGo(int index)
    {
        index = (index + easyModes.Count) % easyModes.Count;
        var m = easyModes[index];
        easyPicking = true;
        EasyBox.SelectedIndex = index;
        easyPicking = false;
        // The parts it switches between, only the ones this car has (or all its points)
        easyParts.Children.Clear();
        easyPartButtons.Clear();
        if (m.Parts is { Length: > 1 } parts)
            foreach (var (text, sel) in parts)
            {
                var b = new RadioButton { Content = text, GroupName = "easypart", Classes = { "seg" }, Margin = new Thickness(0, 0, 3, 3), Padding = new Thickness(2, 3), FontSize = 11, HorizontalAlignment = HorizontalAlignment.Stretch };
                ToolTip.SetTip(b, sel.Label);
                var s = sel;
                b.Click += (_, _) => Doc.Select(s);
                easyPartButtons.Add((b, s));
                easyParts.Children.Add(b);
            }
        easyParts.IsVisible = easyPartButtons.Count > 0;
        easyParts.Columns = Math.Clamp(easyPartButtons.Count, 1, 4);
        if (m.Parts is { Length: > 0 } ps && Doc.Car != null && !m.Has(Doc.Sel)) Doc.Select(ps[0].Sel);
        if (Tabs.SelectedIndex != m.Tab) Tabs.SelectedIndex = m.Tab;
        else if ((Tabs.SelectedItem as TabItem)?.Content is Page p) p.Refresh();
        EasySyncParts();
        EasyTipFor(m);
    }

    public void EasyGo(string name) => EasyGo(Math.Max(0, easyModes.FindIndex(m => m.Name == name)));

    void EasySyncParts()
    {
        foreach (var (b, s) in easyPartButtons) b.IsChecked = s == Doc.Sel;
        if (!ProMode) ShowDrawing();
    }

    /// <summary>A part was chosen in the view (or the hull): its mode comes up</summary>
    void EasyFollow()
    {
        if (ProMode) return;
        int i = easyModes.FindIndex(m => m.Has(Doc.Sel));
        if (i < 0 && Doc.Sel.IsHull)
        {
            easyPicking = true;
            EasyBox.SelectedIndex = easyModes.FindIndex(m => m.Name == "Collision");
            easyPicking = false;
            easyParts.IsVisible = false;
            Tabs.SelectedIndex = 0;
            return;
        }
        if (i < 0) return;
        if (i != EasyBox.SelectedIndex || Tabs.SelectedIndex != 0) EasyGo(i);
        else EasySyncParts();
    }

    // ---------------------------- the look ----------------------------

    /// <summary>The old window's row of buttons</summary>
    Control BuildEasyBar()
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(12, 6, 12, 4) };
        Button B(string text, string? glyph, string tip, Action act)
        {
            var b = new Button { Content = text, FontSize = 12, MinHeight = 30, Padding = new Thickness(12, 4), Margin = new Thickness(0, 0, 6, 4) };
            ToolTip.SetTip(b, tip);
            if (glyph != null) Sym.Set(b, glyph, text, null, true); else Sym.Add(b);
            b.Click += (_, _) => act();
            row.Children.Add(b);
            return b;
        }
        Look.Primary(B("Pro editor", Sym.Scale, "The whole of RV Car Studio 3: every value, tool and tab, RVGL's own too", () => SetMode(true)));
        B("Save", Sym.Save, "Ctrl+S. The first version stays as .bak", async () => await SaveAsked());
        B("Undo", Sym.Undo, "Ctrl+Z", Doc.Undo);
        B("Redo", Sym.Redo, "Ctrl+Y", Doc.Redo);
        B("Other car", Sym.Car, "Back to the list of cars (Ctrl+O)", ChangeCar);
        B("New car", null, "A new car, a remodel or a repaint", async () => await NewCar());
        B("Check car", Sym.Eye, "What's wrong with the car, listed under the view", ToggleIssues);
        B("Re-Volt", Sym.Play, "Runs the car in RVGL (F5)", () => TestRunGame(devRun));
        B("Help", null, "The tours, step by step (in Pro mode)", () => StartTour(0));
        return row;
    }

    /// <summary>A panel's title, as the old panels had on top</summary>
    static Border EasyStrip(string text) => new()
    {
        Padding = new Thickness(12, 7), Background = Look.Bar, BorderBrush = Look.Line, BorderThickness = new Thickness(0, 0, 0, 1),
        Child = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 7, HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new Border { Width = 3, Height = 13, CornerRadius = new CornerRadius(1.5), Background = Look.AccentFill, VerticalAlignment = VerticalAlignment.Center },
                new TextBlock { Text = text, FontSize = 12.5, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center }
            }
        }
    };

    Control BuildEasyHead()
    {
        var pick = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,4,*,4,Auto"), Margin = new Thickness(12, 8, 12, 0) };
        Button Arrow(string t, int step, string tip)
        {
            var b = new Button { Content = t, FontSize = 12, Padding = new Thickness(9, 2), VerticalAlignment = VerticalAlignment.Stretch };
            ToolTip.SetTip(b, tip);
            b.Click += (_, _) => EasyGo(EasyBox.SelectedIndex + step);
            return b;
        }
        var prev = Arrow("<", -1, "The mode before");
        var next = Arrow(">", 1, "The next mode");
        Grid.SetColumn(EasyBox, 2);
        Grid.SetColumn(next, 4);
        pick.Children.Add(prev);
        pick.Children.Add(EasyBox);
        pick.Children.Add(next);
        return new StackPanel { IsVisible = false, Children = { EasyStrip("Edit Menu"), pick, easyParts } };
    }

    // The drawing of the car seen from above, its front to the left: each part's outline, in lines that are always sharp
    static readonly (string Shape, Selection Sel)[] Drawing = MakeDrawing();

    static (string, Selection)[] MakeDrawing()
    {
        var all = new List<(string, Selection)>
        {
            // The body: nose, flanks, tail, and the two windows cut out of it
            ("F0 M 96,68 C 96,58 104,51 118,50 L 150,46 C 176,44 220,44 238,48 C 250,50 254,58 254,68 C 254,78 250,86 238,88 C 220,92 176,92 150,90 L 118,86 C 104,85 96,78 96,68 Z M 152,56 C 148,64 148,72 152,80 L 166,78 C 164,72 164,64 166,58 Z M 214,57 L 214,79 C 222,77 226,74 226,68 C 226,62 222,59 214,57 Z", new(SelKind.Body)),
            // The aerial: bent back, its tip a dot
            ("M 258,66 C 276,64 288,54 296,36 L 299,37 C 291,57 278,68 258,70 Z M 298,29 A 4,4 0 1 1 297.9,29 Z", new(SelKind.AerialBase)),
        };
        // x of the axles, and each wheel's side: 1 and 3 above (right), 0 and 2 below (left)
        foreach (var (i, x, up) in new[] { (1, 128, true), (0, 128, false), (3, 226, true), (2, 226, false) })
        {
            double s = up ? -1 : 1, c = 68;
            string P(double dx, double dy) => FormattableString.Invariant($"{x + dx},{c + s * dy}");
            string Box(double x0, double y0, double x1, double y1) => $"M {P(x0, y0)} L {P(x1, y0)} L {P(x1, y1)} L {P(x0, y1)} Z";
            var wheel = $"M {P(-17, 42)} L {P(17, 42)} Q {P(21, 42)} {P(21, 46)} L {P(21, 55)} Q {P(21, 59)} {P(17, 59)} L {P(-17, 59)} Q {P(-21, 59)} {P(-21, 55)} L {P(-21, 46)} Q {P(-21, 42)} {P(-17, 42)} Z";
            // A spring: a zigzag with some thickness
            var spring = $"M {P(6, 24)} L {P(14, 26)} L {P(6, 29)} L {P(14, 32)} L {P(6, 35)} L {P(14, 38)} L {P(14, 40)} L {P(5, 36)} L {P(12, 32.5)} L {P(5, 29.5)} L {P(12, 26.5)} L {P(6, 26)} Z";
            all.Add((wheel, new(SelKind.Wheel, i)));
            all.Add((Box(-3, 24, 2, 42), new(SelKind.Axle, i)));
            all.Add((spring, new(SelKind.Spring, i)));
            all.Add((Box(-12, 26, -8, 40), new(SelKind.Pin, i)));
        }
        return all.ToArray();
    }

    readonly List<(Avalonia.Controls.Shapes.Path Shape, Selection Sel)> drawing = new();

    Control BuildEasyFoot()
    {
        var canvas = new Canvas { Width = 312, Height = 138, HorizontalAlignment = HorizontalAlignment.Center };
        void Tick(string text, double y, Action<bool> set, string tip)
        {
            var c = new CheckBox { Content = text, IsChecked = true, FontSize = 11, MinHeight = 20, Padding = new Thickness(5, 0, 0, 0) };
            ToolTip.SetTip(c, tip);
            c.IsCheckedChanged += (_, _) => { set(c.IsChecked == true); View.Redraw(); };
            Canvas.SetLeft(c, 0); Canvas.SetTop(c, y);
            canvas.Children.Add(c);
        }
        Tick("Textured", 4, v => SetShadeView(v ? ShadeMode.Textured : ShadeMode.VertexColours), "Off: only the shading");
        Tick("Shiny", 29, v => View.ShowEnv = v, "The reflections (EnvRGB)");
        Tick("Shadow", 54, v => View.ShowShadow = v, "The shadow under the car");
        Tick("Markers", 79, v => View.ShowMarkers = v, "Centre of mass, weapons, mounts");
        var all = new Button { Content = "All solid", FontSize = 11, Padding = new Thickness(8, 2), MinHeight = 0 };
        ToolTip.SetTip(all, "Every part as it is again");
        all.Click += (_, _) => { Doc.PartViews.Clear(); Doc.Hidden.Clear(); FillParts(); ShowDrawing(); View.Redraw(); };
        Canvas.SetLeft(all, 0); Canvas.SetTop(all, 108);
        canvas.Children.Add(all);
        foreach (var (shape, sel) in Drawing)
        {
            var p = new Avalonia.Controls.Shapes.Path { Data = Geometry.Parse(shape), StrokeThickness = 1.4, StrokeJoin = PenLineJoin.Round, Cursor = new Cursor(StandardCursorType.Hand) };
            var s = sel;
            ToolTip.SetTip(p, sel.Label + ": click for solid, wire, points, not shown, half see-through. Right click: choose it");
            p.PointerPressed += (_, e) =>
            {
                if (e.GetCurrentPoint(p).Properties.IsRightButtonPressed) { if (Doc.Car != null) { Doc.Select(s); EasyFollow(); } }
                else NextPartView(s);
                e.Handled = true;
            };
            p.PointerEntered += (_, _) => p.StrokeThickness = 2.4;
            p.PointerExited += (_, _) => p.StrokeThickness = 1.4;
            canvas.Children.Add(p);
            drawing.Add((p, sel));
        }
        easyAutoSave.IsChecked = AutoSave;
        easyAutoSave.IsCheckedChanged += (_, _) => { if ((easyAutoSave.IsChecked == true) != AutoSave) SetAutoSave(easyAutoSave.IsChecked == true); };
        var save = new Border { Padding = new Thickness(12, 2), Child = easyAutoSave, HorizontalAlignment = HorizontalAlignment.Right };
        var strip = EasyStrip("Render Settings");
        strip.BorderThickness = new Thickness(0, 1, 0, 1);
        return new StackPanel { IsVisible = false, Children = { save, strip, new Border { Padding = new Thickness(10, 4), Child = canvas } } };
    }

    /// <summary>The next way to draw a part: solid, wire, points, not at all, half see-through</summary>
    public void NextPartView(Selection s)
    {
        if (Doc.Car == null) return;
        var now = Doc.Hidden.Contains(s) ? PartView.None : Doc.PartViews.GetValueOrDefault(s);
        var next = (PartView)(((int)now + 1) % 5);
        Doc.Hidden.Remove(s);
        Doc.PartViews.Remove(s);
        if (next == PartView.None) Doc.Hidden.Add(s);
        else if (next != PartView.Solid) Doc.PartViews[s] = next;
        FillParts();
        ShowDrawing();
        View.Redraw();
        Say(s.Label + ": " + next switch { PartView.Solid => "solid", PartView.Wire => "wire", PartView.Points => "points", PartView.None => "not shown", _ => "half see-through" });
    }

    /// <summary>The drawing of the car as its parts are shown: filled, outlined, dotted, faint, half filled; the chosen one in the accent colour</summary>
    void ShowDrawing()
    {
        foreach (var (p, s) in drawing)
        {
            var v = Doc.Hidden.Contains(s) ? PartView.None : Doc.PartViews.GetValueOrDefault(s);
            p.IsVisible = Doc.Car == null ? s.Kind is SelKind.Body or SelKind.Wheel : Doc.HasModel(s);
            bool chosen = Doc.Car != null && Doc.Sel == s;
            p.Stroke = chosen ? Look.Accent : v is PartView.Wire or PartView.Points ? Look.Text : Look.Faint;
            // A part not filled still takes the click
            p.Fill = v switch { PartView.Solid => s.Kind == SelKind.Body ? Look.Edge : Look.Faint, PartView.Half => Look.Edge, _ => Brushes.Transparent };
            p.StrokeDashArray = v switch { PartView.Points => new Avalonia.Collections.AvaloniaList<double> { 0.5, 2.5 }, PartView.None => new Avalonia.Collections.AvaloniaList<double> { 3, 3 }, _ => null };
            p.Opacity = v switch { PartView.None => 0.35, PartView.Half => 0.5, _ => 1 };
        }
    }

    Control BuildTipBox()
    {
        var g = new DockPanel();
        DockPanel.SetDock(tipWho, Dock.Right);
        tipWho.VerticalAlignment = VerticalAlignment.Bottom;
        tipWho.Margin = new Thickness(14, 0, 0, 0);
        g.Children.Add(tipWho);
        g.Children.Add(tipText);
        return new Border { IsVisible = false, Background = Look.Panel, BorderBrush = Look.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Look.BigRadius), Padding = new Thickness(14, 8), MinHeight = 36, Child = g };
    }

    // ---------------------------- tips ----------------------------

    // Region name, then who said it and what (the old data\Tips file). "" : the ones for any time
    Dictionary<string, List<(string Who, string Text)>>? tips;

    static Dictionary<string, List<(string Who, string Text)>> ReadTips()
    {
        var all = new Dictionary<string, List<(string, string)>>();
        using var s = typeof(MainWindow).Assembly.GetManifestResourceStream("easy.tips.txt");
        if (s == null) return all;
        var names = new Dictionary<string, string>();
        string region = "";
        foreach (var raw in new StreamReader(s).ReadToEnd().Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("#exit")) break;
            if (line.StartsWith("#Const")) { var kv = line[6..].Split('='); if (kv.Length == 2) names[kv[0].Trim()] = kv[1].Trim().TrimEnd(':'); continue; }
            if (line == "" || line[0] == '#') continue;
            if (line.StartsWith("Region ")) { region = line[7..].Trim().ToLowerInvariant(); continue; }
            if (line.StartsWith("End Region", StringComparison.OrdinalIgnoreCase)) { region = ""; continue; }
            string who = "", text = line;
            if (line[0] == '(' && line.IndexOf(')') is var e and > 0) { who = names.GetValueOrDefault(line[..(e + 1)], line[1..e]); text = line[(e + 1)..].Trim(); }
            else if (line.StartsWith("Note:")) { who = "Note"; text = line[5..].Trim(); }
            if (text.Length < 15) continue;
            if (!all.TryGetValue(region, out var list)) all[region] = list = new();
            list.Add((who, text));
        }
        return all;
    }

    void EasyTipFor(EasyMode m)
    {
        tips ??= ReadTips();
        var from = m.Tips.SelectMany(t => tips.GetValueOrDefault(t) ?? new()).ToList();
        // A mode with no tip of its own, and now and then any mode: one for any time, or how the drawing works
        if (from.Count == 0) from = (tips.GetValueOrDefault("") ?? new()).Concat(tips.GetValueOrDefault("rendersett") ?? new()).ToList();
        if (from.Count == 0) return;
        var (who, text) = from[Random.Shared.Next(from.Count)];
        tipText.Text = text;
        tipWho.Text = who;
    }

    // ---------------------------- the window, one way or the other ----------------------------

    /// <summary>The old window in Easy, RV Car Studio 3's in Pro. The same controls: only what shows, and where</summary>
    void ApplyLayout()
    {
        bool easy = !ProMode;
        toolBar.IsVisible = !easy;
        easyBar.IsVisible = easyTitle.IsVisible = easyHead.IsVisible = easyFoot.IsVisible = tipBox.IsVisible = easyName.IsVisible = easyApp.IsVisible = easy;
        // Panels with rounded corners, a little apart, as the old window's; in Pro edge to edge
        mainGrid.Margin = easy ? new Thickness(8, 0, 8, 6) : default;
        viewFrame.Margin = easy ? new Thickness(4, 0, 4, 0) : default;
        viewFrame.CornerRadius = new CornerRadius(easy ? Look.BigRadius : 0);
        viewFrame.ClipToBounds = easy;
        rightBox.Margin = easy ? new Thickness(4, 0, 4, 0) : default;
        rightBox.Padding = easy ? default : new Thickness(0, 6, 0, 0);
        rightBox.CornerRadius = new CornerRadius(easy ? Look.BigRadius : 0);
        rightBox.ClipToBounds = easy;
        rightBox.BorderThickness = new Thickness(easy ? 1 : 0);
        rightBox.BorderBrush = Look.Line;
        tipBox.Margin = new Thickness(4, 8, 4, 0);
        ShowLeft();
        ShowIssues();

        foreach (var t in Tabs.Items.OfType<TabItem>()) t.IsVisible = !easy && (t.Header as string)?.StartsWith("Easy: ") != true;
        if (easy)
        {
            if (EasyBox.SelectedIndex < 0) EasyBox.SelectedIndex = 0; else EasyGo(EasyBox.SelectedIndex);
            ShowDrawing();
        }
    }

    /// <summary>The panel on the left: the list of cars always has it; in Easy the open car's parts don't (the Edit Menu has them)</summary>
    void ShowLeft()
    {
        if (mainGrid == null) return;
        bool on = ProMode || !lockedPanel.IsVisible;
        leftSide.IsVisible = leftSplit.IsVisible = on;
        mainGrid.ColumnDefinitions[0].Width = new GridLength(on ? 230 : 0);
    }
}
