using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

public sealed partial class MainWindow : Window
{
    public readonly Doc Doc = new();
    public readonly Viewport View;
    readonly TextBox search = new() { Watermark = "Search cars   (#stock  #custom)", FontSize = 12, Margin = new Thickness(8, 8, 8, 4) };
    readonly ListBox carList = new() { FontSize = 12 };
    readonly TextBlock carCount = new() { FontSize = 11, Foreground = Page.Faint, Margin = new Thickness(10, 0, 0, 6) };
    public readonly TextBlock StatusText = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0) };
    readonly TextBlock titleText = new() { FontSize = 15, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 16, 0) };
    readonly TextBlock modifiedDot = new() { Foreground = Brushes.Orange, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    readonly Button undoBtn = new(), redoBtn = new(), saveBtn = new();
    public readonly TabControl Tabs = new() { TabStripPlacement = Dock.Top };
    readonly Border selectedHost = new();
    readonly ItemsControl issues = new();
    readonly TextBlock issuesHead = new() { FontSize = 12, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock hud = new() { FontSize = 15, Foreground = Brushes.White, FontWeight = FontWeight.SemiBold, IsVisible = false, Margin = new Thickness(14), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
    readonly TextBox rawText = new() { AcceptsReturn = true, FontFamily = new FontFamily("Consolas,Menlo,monospace"), FontSize = 12, TextWrapping = TextWrapping.NoWrap };
    readonly List<Page> pages = new();
    readonly List<CarEntry> allCars = new();
    Page? selectedPage;
    readonly DispatcherTimer simTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    readonly DispatcherTimer checkTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    readonly DispatcherTimer rawTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    readonly HashSet<Key> keys = new();
    DateTime simLast;
    bool loadingList;

    static readonly IBrush PanelBrush = new SolidColorBrush(Color.Parse("#23262D"));
    static readonly IBrush BarBrush = new SolidColorBrush(Color.Parse("#1B1E23"));
    static readonly IBrush LineBrush = new SolidColorBrush(Color.Parse("#343842"));

    public MainWindow(string? rvPath)
    {
        Title = "RV Car Studio";
        Width = 1500; Height = 900;
        MinWidth = 1000; MinHeight = 600;
        Background = BarBrush;
        View = new Viewport(Doc);

        Content = BuildLayout();

        Doc.Changed += OnDocChanged;
        Doc.SelectionChanged += OnSelectionChanged;
        Doc.CarOpened += OnCarOpened;
        View.Status += s => Say(s);
        search.TextChanged += (_, _) => FillList();
        carList.SelectionChanged += async (_, _) => await OnCarPicked();
        simTimer.Tick += (_, _) => SimTick();
        checkTimer.Tick += (_, _) => { checkTimer.Stop(); RunChecks(); };
        rawTimer.Tick += (_, _) => { rawTimer.Stop(); ApplyRaw(); };

        AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => keys.Remove(e.Key), RoutingStrategies.Tunnel);
        Closing += OnClosing;

        var startPath = rvPath ?? Settings.RvPath;
        if (GameFolder.IsGameFolder(startPath)) OpenGame(startPath!);
        else Opened += async (_, _) => await AskGameFolder();
    }

    // --------------------------- Layout ---------------------------

    Control BuildLayout()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,26") };

        var menu = BuildMenu();
        root.Children.Add(menu);

        var bar = BuildToolbar();
        Grid.SetRow(bar, 1);
        root.Children.Add(bar);

        var main = new Grid { ColumnDefinitions = new ColumnDefinitions("230,Auto,*,Auto,370") };
        Grid.SetRow(main, 2);
        root.Children.Add(main);

        // Cars
        var left = new DockPanel { Background = PanelBrush };
        var head = new StackPanel();
        head.Children.Add(search);
        head.Children.Add(carCount);
        DockPanel.SetDock(head, Dock.Top);
        left.Children.Add(head);
        var newBtn = new Button { Content = "+  New car", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Margin = new Thickness(8), FontSize = 12 };
        newBtn.Click += async (_, _) => await NewCar();
        DockPanel.SetDock(newBtn, Dock.Bottom);
        left.Children.Add(newBtn);
        left.Children.Add(carList);
        main.Children.Add(left);

        var split1 = new GridSplitter { Width = 3, Background = LineBrush, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(split1, 1);
        main.Children.Add(split1);

        // Middle: 3D and what's wrong with the car
        var mid = new Grid { RowDefinitions = new RowDefinitions("*,Auto,150") };
        Grid.SetColumn(mid, 2);
        main.Children.Add(mid);
        var overlay = new Grid();
        overlay.Children.Add(View);
        overlay.Children.Add(hud);
        mid.Children.Add(overlay);
        var split2 = new GridSplitter { Height = 3, Background = LineBrush, ResizeDirection = GridResizeDirection.Rows };
        Grid.SetRow(split2, 1);
        mid.Children.Add(split2);

        var issuesPanel = new DockPanel { Background = PanelBrush };
        var ih = new Border { Padding = new Thickness(10, 5), Child = issuesHead };
        DockPanel.SetDock(ih, Dock.Top);
        issuesPanel.Children.Add(ih);
        issuesPanel.Children.Add(new ScrollViewer { Content = issues, Margin = new Thickness(10, 0, 4, 6) });
        Grid.SetRow(issuesPanel, 2);
        mid.Children.Add(issuesPanel);

        var split3 = new GridSplitter { Width = 3, Background = LineBrush, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(split3, 3);
        main.Children.Add(split3);

        // Right: everything about the car
        BuildTabs();
        var right = new Border { Background = PanelBrush, Child = Tabs };
        Grid.SetColumn(right, 4);
        main.Children.Add(right);

        var status = new Border { Background = BarBrush, Child = StatusText };
        Grid.SetRow(status, 3);
        root.Children.Add(status);
        return root;
    }

    static MenuItem Item(string text, Action act, string? gesture = null)
    {
        var m = new MenuItem { Header = text, FontSize = 13 };
        if (gesture != null) m.InputGesture = KeyGesture.Parse(gesture);
        m.Click += (_, _) => act();
        return m;
    }

    Menu BuildMenu()
    {
        var file = new MenuItem { Header = "File", FontSize = 13 };
        file.Items.Add(Item("Open Re-Volt folder...", async () => await AskGameFolder()));
        file.Items.Add(Item("New car...", async () => await NewCar()));
        file.Items.Add(new Separator());
        file.Items.Add(Item("Save", Save, "Ctrl+S"));
        file.Items.Add(Item("Revert to the saved car", Revert));
        file.Items.Add(new Separator());
        file.Items.Add(Item("Open car folder", OpenFolder));
        file.Items.Add(Item("Rename car folder...", async () => await RenameCar()));
        file.Items.Add(Item("Pack as zip...", async () => await PackZip()));
        file.Items.Add(new Separator());
        file.Items.Add(Item("Exit", Close));

        var edit = new MenuItem { Header = "Edit", FontSize = 13 };
        edit.Items.Add(Item("Undo", Doc.Undo, "Ctrl+Z"));
        edit.Items.Add(Item("Redo", Doc.Redo, "Ctrl+Y"));
        edit.Items.Add(new Separator());
        edit.Items.Add(Item("Make right side like the left", () => Tool("Symmetry", () => CarTools.Symmetrize(Doc.D!))));
        edit.Items.Add(Item("Make left side like the right", () => Tool("Symmetry", () => CarTools.Symmetrize(Doc.D!, 1))));

        var tools = new MenuItem { Header = "Tools", FontSize = 13 };
        tools.Items.Add(Item("Auto shade the car", AutoShade));
        tools.Items.Add(Item("Make shadow", () => Tool("Shadow", () => Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!)))));
        tools.Items.Add(Item("Make carbox", async () => await MakeCarbox()));
        tools.Items.Add(Item("Make collision hull", MakeHull));
        tools.Items.Add(new Separator());
        tools.Items.Add(Item("Fit radius, skid and lengths to the models", () => Tool("Fit", () => CarTools.FitToModels(Doc.Car!))));
        tools.Items.Add(Item("Compute inertia from the body", () => Tool("Inertia", () => CarTools.ComputeInertia(Doc.Car!))));
        tools.Items.Add(Item("Place wheels on a new body", () => Tool("Place wheels", () => CarTools.PlaceWheels(Doc.Car!))));
        tools.Items.Add(Item("Fit shadow to the car", () => Tool("Shadow table", () => CarTools.FitShadowTable(Doc.Car!))));
        tools.Items.Add(Item("Fill frontend stats from a simulated run", () => Tool("Stats", () => CarSim.FillStats(Doc.D!))));

        var run = new MenuItem { Header = "Run", FontSize = 13 };
        run.Items.Add(Item("Test run in RVGL", () => TestRunGame(false), "F5"));
        run.Items.Add(Item("Test run in RVGL, dev mode (Ctrl+R reloads the car)", () => TestRunGame(true), "Shift+F5"));
        run.Items.Add(new Separator());
        run.Items.Add(Item("Drive here", () => StartDrive(SimGround.Flat), "F6"));
        run.Items.Add(Item("Automatic runs and report", AutoRuns));

        var help = new MenuItem { Header = "Help", FontSize = 13 };
        help.Items.Add(Item("Mouse and keys", () => Say("Left drag: turn the view, click: select, arrows on the handle: move. Right drag: turn, middle drag: pan, wheel: zoom. Arrows + PgUp/PgDn: nudge. 1 2 3 4: views. F: frame the car.")));
        help.Items.Add(Item("About", () => Say("RV Car Studio 3 by Kallel A.Y (theKDL). GPL v3. Re-Volt is a trademark of its owners.")));

        var m = new Menu { Background = BarBrush, Height = 28 };
        foreach (var x in new[] { file, edit, tools, run, help }) m.Items.Add(x);
        return m;
    }

    Button BarButton(string text, string tip, Action click, Button? use = null)
    {
        var b = use ?? new Button();
        b.Content = text; b.FontSize = 12; b.Padding = new Thickness(9, 4); b.Margin = new Thickness(2, 0);
        ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        return b;
    }

    static Border Sep() => new() { Width = 1, Background = LineBrush, Margin = new Thickness(7, 5) };

    Control BuildToolbar()
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(4, 4) };
        p.Children.Add(titleText);
        p.Children.Add(modifiedDot);
        p.Children.Add(Sep());
        p.Children.Add(BarButton("Save", "Save the car (Ctrl+S). The first version stays as .bak", Save, saveBtn));
        p.Children.Add(BarButton("Undo", "Ctrl+Z", Doc.Undo, undoBtn));
        p.Children.Add(BarButton("Redo", "Ctrl+Y", Doc.Redo, redoBtn));
        p.Children.Add(Sep());

        p.Children.Add(new TextBlock { Text = "Move:", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Foreground = Page.Faint, Margin = new Thickness(0, 0, 5, 0) });
        foreach (var (mode, text, tip) in new[]
        {
            (MoveMode.Single, "Single", "Only this wheel or part"),
            (MoveMode.Mirror, "Mirror L/R", "The other side moves the opposite way"),
            (MoveMode.FrontBack, "Front/Back", "Front and back mirror each other: changes the wheelbase"),
            (MoveMode.All, "All four", "All four corners, mirrored both ways")
        })
        {
            var r = new RadioButton { Content = text, GroupName = "move", FontSize = 12, IsChecked = mode == Doc.MoveMode, Margin = new Thickness(0, 0, 4, 0), MinHeight = 24 };
            ToolTip.SetTip(r, tip);
            r.IsCheckedChanged += (_, _) => { if (r.IsChecked == true) { Doc.MoveMode = mode; View.Redraw(); } };
            p.Children.Add(r);
        }
        var linked = new CheckBox { Content = "Suspension follows wheel", FontSize = 12, IsChecked = true, MinHeight = 24, Margin = new Thickness(4, 0) };
        linked.IsCheckedChanged += (_, _) => Doc.MoveLinked = linked.IsChecked == true;
        p.Children.Add(linked);

        var snap = new ComboBox { ItemsSource = new[] { "Snap off", "Snap 0.1", "Snap 0.5", "Snap 1", "Snap 5" }, SelectedIndex = 2, FontSize = 12, MinHeight = 24, VerticalAlignment = VerticalAlignment.Center };
        snap.SelectionChanged += (_, _) => Doc.Snap = new[] { 0f, 0.1f, 0.5f, 1f, 5f }[Math.Max(snap.SelectedIndex, 0)];
        p.Children.Add(snap);
        p.Children.Add(Sep());

        foreach (var (v, text) in new[] { (ViewMode.Perspective, "3D"), (ViewMode.Top, "Top"), (ViewMode.Front, "Front"), (ViewMode.Side, "Side") })
            p.Children.Add(BarButton(text, "View (" + ((int)v + 1) + ")", () => View.SetView(v)));
        p.Children.Add(BarButton("Frame", "Frame the car (F)", View.FrameCar));

        var viewMenu = new MenuFlyout();
        void Toggle(string name, bool start, Action<bool> set)
        {
            var c = new ToggleSwitch { IsChecked = start };
            var item = new MenuItem { Header = name, ToggleType = MenuItemToggleType.CheckBox, IsChecked = start };
            item.Click += (_, _) => { set(item.IsChecked); View.Redraw(); };
            viewMenu.Items.Add(item);
        }
        Toggle("Reflections (EnvRGB)", true, v => View.ShowEnv = v);
        Toggle("Shadow", true, v => View.ShowShadow = v);
        Toggle("Wireframe", false, v => View.ShowWire = v);
        Toggle("Collision hull", false, v => { View.ShowHull = v; LoadHull(); });
        Toggle("Suspension travel", false, v => View.ShowTravel = v);
        Toggle("Markers", true, v => View.ShowMarkers = v);
        Toggle("Grid", true, v => View.ShowGrid = v);
        Toggle("Soft light", false, v => View.Lit = v);
        viewMenu.Items.Add(new Separator());
        foreach (var (m, n) in new[] { (ShadeMode.Textured, "Textured"), (ShadeMode.VertexColours, "Only the shading"), (ShadeMode.Clay, "Clay") })
            viewMenu.Items.Add(Item(n, () => { View.Shade = m; View.Redraw(); }));
        var showBtn = new Button { Content = "Show ▾", FontSize = 12, Padding = new Thickness(9, 4), Margin = new Thickness(2, 0), Flyout = viewMenu };
        p.Children.Add(showBtn);
        p.Children.Add(Sep());

        var runBtn = BarButton("▶  Test run", "Run the car in RVGL (F5)", () => TestRunGame(false));
        runBtn.Background = Page.Accent;
        p.Children.Add(runBtn);
        p.Children.Add(BarButton("Drive here", "Drive with the arrow keys, a quick simulation (F6)", () => StartDrive(SimGround.Flat)));

        return new Border { Background = PanelBrush, BorderBrush = LineBrush, BorderThickness = new Thickness(0, 0, 0, 1), Child = p };
    }

    // --------------------------- Opening ---------------------------

    async Task AskGameFolder()
    {
        var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "Your Re-Volt folder, the one holding 'cars'", AllowMultiple = false });
        var path = picked.FirstOrDefault()?.TryGetLocalPath();
        if (path == null) return;
        if (!GameFolder.IsGameFolder(path)) { Say("That folder has no 'cars' folder inside"); return; }
        Settings.RvPath = path;
        OpenGame(path);
    }

    public void OpenGame(string path)
    {
        Doc.Game = new GameFolder(path);
        LoadCarList();
        BuildGameDependent();
        if (allCars.Count > 0)
        {
            var last = allCars.FirstOrDefault(c => c.Folder.Equals(Settings.LastCar, StringComparison.OrdinalIgnoreCase));
            PickCar(last?.Folder ?? allCars[0].Folder);
        }
    }

    void LoadCarList()
    {
        allCars.Clear();
        if (Doc.Game != null) allCars.AddRange(Doc.Game.ListCars().OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase));
        FillList();
    }

    void FillList()
    {
        loadingList = true;
        var q = search.Text ?? "";
        var words = q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        bool stock = words.Contains("#stock", StringComparer.OrdinalIgnoreCase), custom = words.Contains("#custom", StringComparer.OrdinalIgnoreCase);
        var names = words.Where(w => !w.StartsWith('#')).ToArray();
        var show = allCars.Where(c =>
            (!stock || c.Stock) && (!custom || !c.Stock) &&
            names.All(n => c.Name.Contains(n, StringComparison.OrdinalIgnoreCase) || c.Folder.Contains(n, StringComparison.OrdinalIgnoreCase))).ToList();
        carList.Items.Clear();
        foreach (var c in show)
        {
            var row = new StackPanel { Tag = c.Folder };
            row.Children.Add(new TextBlock { Text = c.Name, FontSize = 13 });
            row.Children.Add(new TextBlock { Text = c.Folder + (c.Stock ? "  ·  stock" : ""), FontSize = 10.5, Foreground = Page.Faint });
            carList.Items.Add(new ListBoxItem { Content = row, Padding = new Thickness(10, 4), Tag = c.Folder });
        }
        carCount.Text = show.Count == allCars.Count ? allCars.Count + " cars" : show.Count + " of " + allCars.Count + " cars";
        if (Doc.Car != null)
            carList.SelectedItem = carList.Items.Cast<ListBoxItem>().FirstOrDefault(i => (string)i.Tag! == Doc.Car.Folder);
        loadingList = false;
    }

    public void PickCar(string folder)
    {
        if (Doc.Game == null) return;
        Doc.Open(Doc.Game, folder);
        Settings.LastCar = folder;
        loadingList = true;
        carList.SelectedItem = carList.Items.Cast<ListBoxItem>().FirstOrDefault(i => string.Equals((string)i.Tag!, folder, StringComparison.OrdinalIgnoreCase));
        loadingList = false;
    }

    async Task OnCarPicked()
    {
        if (loadingList || carList.SelectedItem is not ListBoxItem it || Doc.Game == null) return;
        var folder = (string)it.Tag!;
        if (Doc.Car != null && folder == Doc.Car.Folder) return;
        if (!await AskSave()) { FillList(); return; }
        PickCar(folder);
    }

    /// <summary>Unsaved changes? Ask. False means stay here</summary>
    async Task<bool> AskSave()
    {
        if (!Doc.Modified) return true;
        var r = await Dialogs.Ask(this, "Save the changes of " + Doc.D!.Name + "?", "Save", "Don't save", "Cancel");
        if (r == 0) { Save(); return true; }
        return r == 1;
    }

    bool closingOk;
    async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (closingOk || !Doc.Modified) return;
        e.Cancel = true;
        if (await AskSave()) { closingOk = true; Close(); }
    }

    void OnCarOpened()
    {
        View.HullData = null;
        LoadHull();
        View.FrameCar();
        titleText.Text = Doc.D!.Name == "" ? Doc.Car!.Folder : Doc.D.Name;
        foreach (var p in pages) p.Refresh();
        RefreshRaw();
        RunChecks();
        Say("Opened " + Doc.Car!.Folder);
    }

    void LoadHull()
    {
        View.HullData = null;
        if (Doc.Car == null) return;
        try
        {
            if (Doc.Car.Game.Find(Doc.D!.Coll) is { } f && new FileInfo(f).Length > 0) View.HullData = Hull.Load(f);
        }
        catch (Exception) { }
    }

    void OnDocChanged()
    {
        undoBtn.IsEnabled = Doc.CanUndo;
        redoBtn.IsEnabled = Doc.CanRedo;
        if (Doc.Car == null) return;
        var modified = Doc.Modified;
        modifiedDot.Text = modified ? "● not saved" : "";
        titleText.Text = Doc.D!.Name == "" ? Doc.Car.Folder : Doc.D.Name;
        Title = "RV Car Studio - " + titleText.Text + (modified ? " *" : "");
        selectedPage?.Refresh();
        foreach (var p in pages) if (p.IsVisible) p.Refresh();
        View.Redraw();
        checkTimer.Stop();
        checkTimer.Start();
        if (Doc.Sim == null && !rawText.IsFocused) rawDirty = true;
    }

    void OnSelectionChanged()
    {
        selectedPage = BuildSelectedPage();
        selectedHost.Child = selectedPage;
        selectedPage.Refresh();
    }

    public void Say(string s) => StatusText.Text = s;

    void Tool(string name, Action act)
    {
        if (Doc.Car == null) return;
        try
        {
            Doc.Edit(name, act);
            Doc.EndGesture();
            if (StatusText.Text is null || !StatusText.Text.StartsWith("Shadow")) Say(name + ": done");
        }
        catch (Exception ex) { Say(name + " failed: " + ex.Message); }
    }

    public void Save()
    {
        if (Doc.Car == null) return;
        try
        {
            Doc.Save();
            RefreshRaw();
            Say("Saved " + Doc.Car.ParamPath);
        }
        catch (Exception ex) { Say("Couldn't save: " + ex.Message); }
    }

    void Revert()
    {
        if (Doc.Car != null) PickCar(Doc.Car.Folder);
    }

    // --------------------------- Checks ---------------------------

    void RunChecks()
    {
        if (Doc.Car == null) return;
        var r = CarChecker.Check(Doc.Car);
        issuesHead.Text = $"Car check: {r.Score}/100    {r.Errors.Count} errors, {r.Warnings.Count} warnings";
        issuesHead.Foreground = r.Errors.Count > 0 ? Brushes.OrangeRed : r.Warnings.Count > 0 ? Brushes.Gold : Brushes.LightGreen;
        var rows = new List<Control>();
        foreach (var e in r.Errors) rows.Add(new TextBlock { Text = "✖  " + e, Foreground = Brushes.OrangeRed, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        foreach (var e in r.Warnings) rows.Add(new TextBlock { Text = "▲  " + e, Foreground = Brushes.Gold, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        foreach (var e in r.Infos) rows.Add(new TextBlock { Text = "·   " + e, Foreground = Page.Faint, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        issues.ItemsSource = rows;
    }

    // --------------------------- Keys ---------------------------

    void OnKey(object? s, KeyEventArgs e)
    {
        keys.Add(e.Key);
        var typing = FocusManager?.GetFocusedElement() is TextBox;
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (ctrl && e.Key == Key.S) { Save(); e.Handled = true; return; }
        if (typing && !ctrl) { if (e.Key == Key.Escape) View.Focus(); return; }
        if (ctrl && e.Key == Key.Z) { Doc.Undo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.Y) { Doc.Redo(); e.Handled = true; return; }
        if (e.Key == Key.F5) { TestRunGame(e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; return; }
        if (e.Key == Key.F6) { if (Doc.Sim != null) StopDrive(); else StartDrive(SimGround.Flat); e.Handled = true; return; }
        if (Doc.Sim != null)
        {
            if (e.Key == Key.Escape) StopDrive();
            if (e.Key == Key.R) { Doc.Sim.Reset(); }
            e.Handled = true;
            return;
        }
        float amount = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 5 : Math.Max(Doc.Snap, 0.1f);
        switch (e.Key)
        {
            case Key.D1: View.SetView(ViewMode.Perspective); break;
            case Key.D2: View.SetView(ViewMode.Top); break;
            case Key.D3: View.SetView(ViewMode.Front); break;
            case Key.D4: View.SetView(ViewMode.Side); break;
            case Key.F: View.FrameCar(); break;
            case Key.Left: View.Nudge(-1, 0, 0, amount); break;
            case Key.Right: View.Nudge(1, 0, 0, amount); break;
            case Key.Up: View.Nudge(0, 0, 1, amount); break;
            case Key.Down: View.Nudge(0, 0, -1, amount); break;
            case Key.PageUp: View.Nudge(0, -1, 0, amount); break;
            case Key.PageDown: View.Nudge(0, 1, 0, amount); break;
            default: return;
        }
        e.Handled = true;
    }

    // --------------------------- Drive ---------------------------

    public void StartDrive(SimGround ground)
    {
        if (Doc.Car == null) return;
        Doc.StartSim(ground);
        hud.IsVisible = true;
        driveCamYaw = float.NaN;
        simLast = DateTime.UtcNow;
        simTimer.Start();
        View.Focus();
        Say("Driving: arrows or WASD, R to restart, Esc to leave. A quick simulation: the real test is Test run.");
    }

    public void StopDrive()
    {
        simTimer.Stop();
        Doc.StopSim();
        hud.IsVisible = false;
        View.SetView(ViewMode.Perspective);
        View.FrameCar();
        Say("Back to editing");
    }

    float driveCamYaw = float.NaN;
    public float AutoThrottle = float.NaN, AutoSteer;

    void SimTick()
    {
        if (Doc.Sim is not { } s) return;
        var now = DateTime.UtcNow;
        var dt = Math.Clamp((float)(now - simLast).TotalSeconds, 0.001f, 0.05f);
        simLast = now;
        float th = 0, st = 0;
        if (keys.Contains(Key.Up) || keys.Contains(Key.W)) th += 1;
        if (keys.Contains(Key.Down) || keys.Contains(Key.S)) th -= 1;
        if (keys.Contains(Key.Left) || keys.Contains(Key.A)) st -= 1;
        if (keys.Contains(Key.Right) || keys.Contains(Key.D)) st += 1;
        if (!float.IsNaN(AutoThrottle)) { th = AutoThrottle; st = AutoSteer; }
        s.Throttle = th;
        s.Steer = st;
        Doc.TickSim(dt);

        var fwd = Vector3.Transform(Vector3.UnitZ, s.Rot);
        var yaw = MathF.Atan2(fwd.X, fwd.Z);
        if (float.IsNaN(driveCamYaw)) driveCamYaw = yaw;
        var diff = MathF.IEEERemainder(yaw - driveCamYaw, MathF.Tau);
        driveCamYaw += diff * Math.Min(1, dt * 4);
        View.View = ViewMode.Perspective;
        View.Target = s.Pos + new Vector3(0, -15, 0);
        View.Yaw = driveCamYaw + MathF.PI;
        View.Pitch = 0.3f;
        View.Distance = 300;
        hud.Text = $"{s.SpeedMph:0} mph   {s.SpeedMph * 1.609f:0} km/h";
        View.Redraw();
    }

    void AutoRuns()
    {
        if (Doc.D == null) return;
        Say("Running...");
        var d = Doc.D.Clone();
        var rows = new List<string>();
        var a = CarSim.RunAcceleration(d);
        rows.Add($"Acceleration: {a.Note}");
        rows.Add($"   in the file: TopEnd {d.TopEnd:0}, Acc {d.Acc:0.0}. Simulated: TopEnd {a.TopEnd:0}, Acc {a.Acc:0.0}");
        rows.Add($"Hard turn: {CarSim.RunCircle(d).Note}");
        rows.Add($"Bumps: {CarSim.RunBumps(d).Note}");
        Dialogs.ShowText(this, "Automatic runs", string.Join("\n", rows) + "\n\nThis is a simulation from the parameters, not the game. Use Test run for the real thing.");
        Say("Automatic runs done");
    }

    // --------------------------- Tools with side effects ---------------------------

    void AutoShade() => DoAutoShade(0.45f, 0.55f, 1f);

    public void DoAutoShade(float ambient, float sun, float occlusion)
    {
        if (Doc.D is not { } d) return;
        var opt = new CarTools.ShadeOptions(ambient, sun, occlusion);
        var slots = new List<int> { d.BodyModel };
        slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
        if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
        int n = 0;
        foreach (var slot in slots.Distinct())
            if (Doc.Model(slot) is { } m)
            {
                CarTools.AutoShade(m, opt);
                Doc.DirtyModels.Add(m);
                n++;
            }
        Doc.ModelVersion++;
        Doc.Touch();
        Say($"Shaded {n} models. See it, then Save to keep it (the first version stays as .bak)");
    }

    public async Task<string?> MakeCarbox()
    {
        if (Doc.Car == null) return null;
        var (yaw, pitch, dist, tgt) = (View.Yaw, View.Pitch, View.Distance, View.Target);
        View.Yaw = 0.75f; View.Pitch = 0.3f;
        View.FrameCar();
        View.Distance *= 0.82f;
        using var shot = await View.Capture(512, true);
        (View.Yaw, View.Pitch, View.Distance, View.Target) = (yaw, pitch, dist, tgt);
        var color = SKColor.FromHsv((DateTime.Now.Millisecond * 0.36f) % 360, 75, 80);
        using var box = CarTools.ComposeCarbox(shot, Doc.D!.Name, color);
        string path = "";
        Tool("Carbox", () => path = CarTools.SaveCarbox(Doc.Car, box));
        Say("Carbox saved: " + path);
        View.Redraw();
        return path;
    }

    void MakeHull()
    {
        if (Doc.Car == null || Doc.Model(Doc.D!.BodyModel) is not { } body) { Say("The car has no body to make a hull from"); return; }
        var hull = CarTools.MakeHull(body, Doc.D.BodyOffset);
        if (hull == null) { Say("Couldn't make a hull from this body"); return; }
        var path = Path.Combine(Doc.Car.Dir, "hull.hul");
        Files.Backup(path);
        hull.Save(path);
        Tool("Hull", () => Doc.D!.Coll = "cars\\" + Doc.Car.Folder + "\\hull.hul");
        View.ShowHull = true;
        LoadHull();
        Say("Hull saved: " + path + " (shown in green)");
        View.Redraw();
    }

    public void TestRunGame(bool dev)
    {
        if (Doc.Car == null) return;
        try
        {
            Save();
            var run = new TestRun(Doc.Car.Game);
            if (run.FindExe() == null) { Say("No RVGL found in " + Doc.Car.Game.Root); return; }
            run.Start(Doc.Car.Folder, SelectedLevel, dev);
            Say(dev ? "RVGL started in dev mode: Ctrl+R reloads the car, Start Race has Calc Car Stats" : "RVGL started with " + Doc.D!.Name + " picked: choose Start Race");
        }
        catch (Exception ex) { Say("Couldn't start RVGL: " + ex.Message); }
    }

    public string? SelectedLevel;

    void OpenFolder()
    {
        if (Doc.Car == null) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Doc.Car.Dir) { UseShellExecute = true }); }
        catch (Exception ex) { Say(ex.Message); }
    }

    async Task NewCar()
    {
        if (Doc.Game == null) return;
        var template = Doc.Car?.Folder ?? "gencar";
        var folder = await Dialogs.Input(this, "New car, starting as a copy of '" + template + "'. Its folder inside cars:", "");
        if (string.IsNullOrWhiteSpace(folder)) return;
        var name = await Dialogs.Input(this, "Its name:", folder);
        if (string.IsNullOrWhiteSpace(name)) return;
        if (!await AskSave()) return;
        try
        {
            Car.CreateFrom(Doc.Game, template, folder, name);
            LoadCarList();
            PickCar(folder);
        }
        catch (Exception ex) { Say("Can't make the car: " + ex.Message); }
    }

    async Task RenameCar()
    {
        if (Doc.Car == null) return;
        var folder = await Dialogs.Input(this, "New folder for " + Doc.Car.Folder + ":", Doc.Car.Folder);
        if (string.IsNullOrWhiteSpace(folder) || folder == Doc.Car.Folder) return;
        try
        {
            Doc.Car.Rename(folder);
            LoadCarList();
            Settings.LastCar = folder;
            Say("Renamed to " + folder);
        }
        catch (Exception ex) { Say("Can't rename: " + ex.Message); }
    }

    async Task PackZip()
    {
        if (Doc.Car == null) return;
        var f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = Doc.Car.Folder + ".zip", DefaultExtension = "zip" });
        var path = f?.TryGetLocalPath();
        if (path == null) return;
        Save();
        Doc.Car.Pack(path, true);
        Say("Packed: " + path);
    }

    // --------------------------- Raw file ---------------------------

    bool rawDirty;
    string? rawLast;  // what we filled in: only something else is an edit
    void RefreshRaw()
    {
        if (Doc.Car == null) return;
        rawTimer.Stop();
        rawLast = Doc.Car.CurrentText().Replace("\0", "");
        rawText.Text = rawLast;
        rawDirty = false;
    }

    void ApplyRaw()
    {
        if (Doc.Car == null || rawText.Text == null || rawText.Text == rawLast) return;
        rawLast = rawText.Text;
        Doc.Edit("Edit the file", () => { });
        Doc.Car.LoadText(rawText.Text);
        Doc.ReloadModels();
        Doc.EndGesture();
        Doc.Touch();
    }
}
