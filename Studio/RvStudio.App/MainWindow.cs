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
    public readonly ViewCube Cube = new() { HorizontalAlignment = HorizontalAlignment.Center };
    // The cube with Home under it, top right of the view, shown or hidden together
    readonly StackPanel cubeBox = new() { HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, 8, 10, 0), Spacing = 2 };
    public readonly Button HomeButton = new() { Content = "⌂ Home", HorizontalAlignment = HorizontalAlignment.Center, Padding = new Thickness(8, 1), FontSize = 11, MinHeight = 0 };

    /// <summary>Home: the view the studio starts with, the car from the front left and above, on its grid, framed.</summary>
    public void GoHome()
    {
        View.SetView(ViewMode.Perspective);
        View.FrameCar();
        Cube.InvalidateVisual();
    }
    readonly TextBox search = new() { Watermark = "Search cars   (#stock  #custom)", FontSize = 12, Margin = new Thickness(8, 8, 8, 4) };
    readonly ListBox carList = new() { FontSize = 12 };
    readonly Control pickerPanel;
    readonly Control lockedPanel;
    readonly TextBlock lockedName = new() { FontSize = 16, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock lockedInfo = new() { FontSize = 11.5, Foreground = Page.Faint, TextWrapping = TextWrapping.Wrap };
    readonly ListBox partsList = new() { FontSize = 12 };
    bool fillingParts;
    public bool CarLocked => lockedPanel.IsVisible;
    readonly TextBlock carCount = new() { FontSize = 11, Foreground = Page.Faint, Margin = new Thickness(10, 0, 0, 6) };
    public readonly TextBlock StatusText = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0) };
    readonly TextBlock titleText = new() { FontSize = 15, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 16, 0) };
    readonly TextBlock modifiedDot = new() { Foreground = Look.Modified, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
    readonly Button undoBtn = new(), redoBtn = new(), saveBtn = new();
    public readonly TabControl Tabs = new() { TabStripPlacement = Dock.Top };
    readonly Border selectedHost = new();
    readonly ItemsControl issues = new();
    readonly TextBlock issuesHead = new() { FontSize = 12, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
    readonly TextBlock hud = new() { FontSize = 15, Foreground = Look.Text, FontWeight = FontWeight.SemiBold };
    readonly Border hudBox = new()
    {
        IsHitTestVisible = false, IsVisible = false, Background = Look.TipBack, BorderBrush = Look.Line, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Look.BigRadius),
        Padding = new Thickness(12, 8), Margin = new Thickness(14), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top
    };
    readonly TextBox rawText = new() { AcceptsReturn = true, FontFamily = new FontFamily("Consolas,Menlo,monospace"), FontSize = 12, TextWrapping = TextWrapping.NoWrap };
    readonly List<Page> pages = new();
    readonly List<CarEntry> allCars = new();
    Page? selectedPage;
    readonly DispatcherTimer simTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    readonly RaceMap raceMap = new() { Width = 170, Height = 170, IsVisible = false, IsHitTestVisible = false, Margin = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
    readonly DispatcherTimer checkTimer = new() { Interval = TimeSpan.FromMilliseconds(600) };
    readonly DispatcherTimer rawTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    readonly HashSet<Key> keys = new();
    DateTime simLast;
    bool loadingList;

    // The look's own brushes: they change colour in place when the theme does
    static IBrush PanelBrush => Look.Panel;
    static IBrush BarBrush => Look.Bar;
    static IBrush LineBrush => Look.Line;

    public MainWindow(string? rvPath)
    {
        Title = "RV Car Studio";
        try { using var icon = typeof(MainWindow).Assembly.GetManifestResourceStream("app.ico"); if (icon != null) Icon = new WindowIcon(icon); } catch { }
        Width = 1500; Height = 900;
        MinWidth = 1000; MinHeight = 600;
        Background = BarBrush;
        View = new Viewport(Doc);
        pickerPanel = BuildPicker();
        lockedPanel = BuildLocked();

        Content = BuildLayout();

        Doc.Changed += OnDocChanged;
        Doc.SelectionChanged += OnSelectionChanged;
        Doc.CarOpened += OnCarOpened;
        Doc.CarOpened += () => { DropObjLinks(); Compare?.Close(); };
        View.Status += s => Say(s);
        View.TipChanged += ShowViewTip;
        View.ContextAsked += ShowPartMenu;
        Look.Changed += OnThemeChanged;
        View.Picked += () => Tabs.SelectedIndex = 0;
        View.Picked += EasyFollow;
        View.NeedsPermission += async s => await AskShared(s);
        Doc.StockTouched += () => stockBanner.IsVisible = true;
        BuildStockBanner();
        Doc.PartsChanged += FillParts;
        search.TextChanged += (_, _) => FillList();
        carList.SelectionChanged += async (_, _) => await OnCarPicked();
        simTimer.Tick += (_, _) => SimTick();
        checkTimer.Tick += (_, _) => { checkTimer.Stop(); RunChecks(); };
        shadeFinal.Tick += (_, _) => ShadeFinish();
        rawTimer.Tick += (_, _) => { rawTimer.Stop(); ApplyRaw(); };

        AddHandler(KeyDownEvent, OnKey, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, (_, e) => keys.Remove(e.Key), RoutingStrategies.Tunnel);
        Closing += OnClosing;
        HookInstallDrop();

        var startPath = rvPath ?? Settings.RvPath;
        if (GameFolder.IsGameFolder(startPath)) OpenGame(startPath!);
        else Opened += async (_, _) => await AskGameFolder();
        Opened += (_, _) => OfferToursOnce();
        if (AutoSave) SetAutoSave(true);
        ReadmeWindow.Written += ShowCarDetails;
    }

    // --------------------------- Layout ---------------------------

    Control BuildLayout()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,28") };
        rootGrid = root;

        var menu = BuildMenu();
        root.Children.Add(new Panel { Children = { menu, easyTitle } });

        toolBar = BuildToolbar();
        Grid.SetRow(toolBar, 1);
        root.Children.Add(toolBar);
        easyBar = BuildEasyBar();
        Grid.SetRow(easyBar, 1);
        root.Children.Add(easyBar);

        var main = new Grid { ColumnDefinitions = new ColumnDefinitions("230,Auto,*,Auto,370") };
        Grid.SetRow(main, 2);
        root.Children.Add(main);
        mainGrid = main;

        // The car: pick one, then it stays until you ask to change it
        var left = new Panel { Background = PanelBrush };
        left.Children.Add(pickerPanel);
        left.Children.Add(lockedPanel);
        main.Children.Add(left);
        leftSide = left;

        var split1 = new GridSplitter { Width = 4, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(split1, 1);
        main.Children.Add(split1);
        leftSplit = split1;

        // Middle: 3D and what's wrong with the car
        var mid = new Grid { RowDefinitions = new RowDefinitions("*,Auto,150,Auto") };
        midGrid = mid;
        Grid.SetColumn(mid, 2);
        main.Children.Add(mid);
        var overlay = new Grid();
        overlay.Children.Add(View);
        Cube.View = View;
        View.Redrawn += () => { if (Cube.IsVisible) Cube.InvalidateVisual(); };
        HomeButton.Click += (_, _) => GoHome();
        ToolTip.SetTip(HomeButton, "Back to the starting view: the car from the front left and above, on the grid, framed");
        cubeBox.Children.Add(Cube);
        cubeBox.Children.Add(HomeButton);
        cubeBox.IsVisible = Settings.ViewCube;
        overlay.Children.Add(cubeBox);
        hudBox.Child = hud;
        overlay.Children.Add(hudBox);
        overlay.Children.Add(viewTip);
        overlay.Children.Add(raceMap);
        overlay.Children.Add(stockBanner);
        overlay.Children.Add(TourBox());
        // The box dragged to pick vertices
        var boxCanvas = new Canvas { IsHitTestVisible = false };
        var boxRect = new Border { BorderBrush = Look.Accent, BorderThickness = new Thickness(1), Background = new SolidColorBrush(Colors.White, 0.07), IsVisible = false };
        boxCanvas.Children.Add(boxRect);
        overlay.Children.Add(boxCanvas);
        View.BoxChanged += r =>
        {
            if (r is not { } b) { boxRect.IsVisible = false; return; }
            Canvas.SetLeft(boxRect, b.X); Canvas.SetTop(boxRect, b.Y);
            boxRect.Width = b.Width; boxRect.Height = b.Height; boxRect.IsVisible = true;
        };
        viewFrame = new Border { Child = overlay };
        mid.Children.Add(viewFrame);
        var split2 = new GridSplitter { Height = 4, ResizeDirection = GridResizeDirection.Rows };
        Grid.SetRow(split2, 1);
        mid.Children.Add(split2);
        issuesSplit = split2;

        var issuesPanel = new DockPanel { Background = PanelBrush };
        var ih = new Border { Padding = new Thickness(12, 8, 12, 6), Child = issuesHead };
        DockPanel.SetDock(ih, Dock.Top);
        issuesPanel.Children.Add(ih);
        issuesPanel.Children.Add(new ScrollViewer { Content = issues, Margin = new Thickness(12, 0, 4, 8) });
        Grid.SetRow(issuesPanel, 2);
        mid.Children.Add(issuesPanel);
        issuesBox = issuesPanel;
        tipBox = BuildTipBox();
        Grid.SetRow(tipBox, 3);
        mid.Children.Add(tipBox);

        var split3 = new GridSplitter { Width = 4, ResizeDirection = GridResizeDirection.Columns };
        Grid.SetColumn(split3, 3);
        main.Children.Add(split3);

        // Right: everything about the car
        BuildTabs();
        easyHead = BuildEasyHead();
        easyFoot = BuildEasyFoot();
        var rightDock = new DockPanel();
        DockPanel.SetDock(easyHead, Dock.Top);
        DockPanel.SetDock(easyFoot, Dock.Bottom);
        rightDock.Children.Add(easyHead);
        rightDock.Children.Add(easyFoot);
        rightDock.Children.Add(Tabs);
        var right = new Border { Background = PanelBrush, Padding = new Thickness(0, 6, 0, 0), Child = rightDock };
        rightBox = right;
        Grid.SetColumn(right, 4);
        main.Children.Add(right);

        var status = new Border { Background = BarBrush, BorderBrush = LineBrush, BorderThickness = new Thickness(0, 1, 0, 0) };
        var statusRow = new DockPanel();
        DockPanel.SetDock(easyName, Dock.Left);
        DockPanel.SetDock(easyApp, Dock.Right);
        statusRow.Children.Add(easyName);
        statusRow.Children.Add(easyApp);
        statusRow.Children.Add(StatusText);
        status.Child = statusRow;
        Grid.SetRow(status, 3);
        root.Children.Add(status);
        ApplyMode();
        return root;
    }

    Control BuildPicker()
    {
        var left = new DockPanel { IsVisible = true };
        var head = new StackPanel();
        head.Children.Add(new TextBlock { Text = "Pick a car", FontSize = 15, FontWeight = FontWeight.SemiBold, Margin = new Thickness(12, 12, 10, 0) });
        head.Children.Add(search);
        head.Children.Add(carCount);
        DockPanel.SetDock(head, Dock.Top);
        left.Children.Add(head);
        var bottom = new StackPanel { Margin = new Thickness(8), Spacing = 4 };
        var newBtn = new Button { Content = "+  New car", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12 };
        newBtn.Click += async (_, _) => await NewCar();
        var keep = new Button { Content = "Keep the open car", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12 };
        keep.Click += (_, _) => SetLocked(Doc.Car != null);
        bottom.Children.Add(keep);
        var random = new Button { Content = "Random car", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12 };
        ToolTip.SetTip(random, "Any car of the list (as the search filters it)");
        random.Click += (_, _) => RandomCar();
        bottom.Children.Add(random);
        bottom.Children.Add(newBtn);
        DockPanel.SetDock(bottom, Dock.Bottom);
        left.Children.Add(bottom);
        left.Children.Add(carList);
        return left;
    }

    Control BuildLocked()
    {
        var left = new DockPanel { IsVisible = false };
        var head = new StackPanel { Margin = new Thickness(10), Spacing = 4 };
        head.Children.Add(lockedName);
        head.Children.Add(lockedInfo);
        head.Children.Add(carDetails);
        readmeButton = new Button { Content = "Read its readme", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12, Margin = new Thickness(0, 4, 0, 0), IsVisible = false };
        readmeButton.Click += async (_, _) => await ViewReadme();
        head.Children.Add(readmeButton);
        var change = new Button { Content = "Change car...", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12, Margin = new Thickness(0, 6, 0, 0) };
        ToolTip.SetTip(change, "Ctrl+O");
        change.Click += (_, _) => ChangeCar();
        head.Children.Add(change);
        head.Children.Add(Look.Heading("Parts", new Thickness(0, 12, 0, 2)));
        hullShow.IsCheckedChanged += (_, _) =>
        {
            View.ShowHull = hullShow.IsChecked == true;
            // Closed with a hull part selected: the body is selected instead, or the group would stay open for it
            if (!View.ShowHull && Doc.Sel.IsHull) { if (Doc.VertexMode) Doc.LeaveVertexMode(); Doc.Select(new Selection(SelKind.Body)); }
            FillParts();
            View.Redraw();
        };
        head.Children.Add(hullShow);
        var hullSolid = new CheckBox { Content = "Solid, smooth shaded", FontSize = 12, MinHeight = 22, Margin = new Thickness(18, 0, 0, 0) };
        ToolTip.SetTip(hullSolid, "Show the hull's spheres and parts solid instead of as wires");
        hullSolid.IsCheckedChanged += (_, _) => { View.HullSolid = hullSolid.IsChecked == true; View.Redraw(); };
        head.Children.Add(hullSolid);
        DockPanel.SetDock(head, Dock.Top);
        left.Children.Add(head);
        var add = new Button { Content = "+  Add a part", HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, FontSize = 12, Margin = new Thickness(8) };
        add.Flyout = addMenu;
        addMenu.Opening += (_, _) => FillAddMenu();
        DockPanel.SetDock(add, Dock.Bottom);
        left.Children.Add(add);
        left.Children.Add(partsList);
        partsList.SelectionChanged += (_, _) =>
        {
            if (fillingParts) return;
            if (partsList.SelectedItem is ListBoxItem { Tag: Selection s }) Doc.Select(s);
            // The hull's own row: opens or closes the group (and shows or hides the hull in the view)
            // after the click is done: the list can't be rebuilt in the middle of its own selection (it breaks)
            else if (partsList.SelectedItem is ListBoxItem { Tag: "hull" })
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { hullShow.IsChecked = !(hullShow.IsChecked == true); SyncParts(); });
        };
        return left;
    }

    // --------------------------- Stock cars and shared files ---------------------------

    readonly Border stockBanner = new()
    {
        IsVisible = false, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10),
        Background = Look.Banner, BorderBrush = Look.BannerLine, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Look.BigRadius), Padding = new Thickness(14, 8)
    };

    public bool StockBannerShown => stockBanner.IsVisible;

    void BuildStockBanner()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        row.Children.Add(new TextBlock { Text = "You're changing a stock car. Nothing is saved yet: make your own copy?", FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        ToolTip.SetTip(row, "If you keep editing, Save puts your changes in packs\\local on top of the game's files. RVGL shows them everywhere, and may mark the car as changed online.");
        var copy = Look.Primary(new Button { Content = "Make a copy", FontSize = 12, Padding = new Thickness(9, 3) });
        copy.Click += async (_, _) => await CopyCar();
        var keep = new Button { Content = "Keep editing the original", FontSize = 12, Padding = new Thickness(9, 3) };
        keep.Click += (_, _) => stockBanner.IsVisible = false;
        row.Children.Add(copy);
        row.Children.Add(keep);
        stockBanner.Child = row;
    }

    /// <summary>Carry on in a copy of this car, with what's been changed so far</summary>
    public async Task<bool> CopyCar()
    {
        if (Doc.Car is not { } c) return false;
        var folder = await Dialogs.Input(this, "Your copy of " + Doc.D!.Name + ". Its folder inside cars:", c.Folder + "_my");
        if (string.IsNullOrWhiteSpace(folder)) return false;
        var name = await Dialogs.Input(this, "Its name:", Doc.D.Name + " Mod");
        if (string.IsNullOrWhiteSpace(name)) return false;
        try
        {
            Doc.ForkCar(folder, name);
            stockBanner.IsVisible = false;
            LoadCarList();
            Settings.LastCar = folder;
            SetLocked(true);
            Say("Now working on your copy, " + name + ". " + c.Folder + " stays as it was");
            return true;
        }
        catch (Exception ex) { Say("Can't make the copy: " + ex.Message); return false; }
    }

    async Task DropLocal()
    {
        if (Doc.Car is not { } c) return;
        if (!c.HasLocalChanges) { Say("This car has no saved changes on top of the game's files"); return; }
        var r = await Dialogs.Ask(this, "Delete your saved changes of " + c.Folder + " in packs\\local? The car goes back to the game's own files.", "Delete them", "Keep them");
        if (r != 0) return;
        c.DropLocalChanges();
        PickCar(c.Folder);
        Say("Back to the game's version of " + c.Folder);
    }

    /// <summary>Saving a stock car with no answer yet</summary>
    public async Task SaveAsked()
    {
        if (Doc.Car == null) return;
        if (Doc.IsStockCar && Doc.Modified && stockBanner.IsVisible)
        {
            var r = await Dialogs.Ask(this, Doc.D!.Name + " is a stock car. Saving puts your changes in packs\\local, on top of the game's files, which stay as they are. RVGL may mark the car as changed online.", "Make a copy", "Save the changes", "Cancel");
            if (r == 2) return;
            stockBanner.IsVisible = false;
            if (r == 0) { await CopyCar(); return; }
        }
        Save();
    }

    public async Task<bool> AskShared(Selection s) => await AskSharedFiles(Doc.SharedFiles(s));

    /// <summary>Files others need: use copies, or change them for all. False means don't do it</summary>
    public async Task<bool> AskSharedFiles(List<(string Rel, string Why)> files)
    {
        if (files.Count == 0) return true;
        var r = await Dialogs.Ask(this, string.Join("\n", files.Select(f => f.Why)) + "\n\nChanging it would change the other cars too.", "Use my own copy", "Change it for all", "Cancel");
        if (r == 2) return false;
        foreach (var (rel, _) in files)
        {
            if (r == 0) Doc.MakeOwn(rel);
            else Doc.AllowShared(rel);
        }
        LoadCarDetails();
        Say(r == 0 ? "This car now has its own copy. Drag again" : "You can change it now. Drag again");
        return true;
    }

    void LoadCarDetails()
    {
        foreach (var p in pages) p.Refresh();
        selectedPage?.Refresh();
        View.Redraw();
    }

    readonly MenuFlyout addMenu = new();

    void FillAddMenu()
    {
        addMenu.Items.Clear();
        if (Doc.D is not { } d) return;
        void Offer(string text, Selection s) => addMenu.Items.Add(Item(text, async () => await PickModelFor(s)));
        if (d.BodyModel < 0) Offer("Body", new(SelKind.Body));
        for (int i = 0; i < 4; i++) if (!Doc.HasModel(new(SelKind.Wheel, i))) Offer("Wheel: " + Doc.WheelName(i), new(SelKind.Wheel, i));
        for (int i = 0; i < 4; i++) if (!Doc.HasModel(new(SelKind.Spring, i))) Offer("Spring: " + Doc.WheelName(i), new(SelKind.Spring, i));
        for (int i = 0; i < 4; i++) if (!Doc.HasModel(new(SelKind.Axle, i))) Offer("Axle: " + Doc.WheelName(i), new(SelKind.Axle, i));
        for (int i = 0; i < 4; i++) if (!Doc.HasModel(new(SelKind.Pin, i))) Offer("Pin: " + Doc.WheelName(i), new(SelKind.Pin, i));
        if (d.Spinner.Model < 0) Offer("Spinner", new(SelKind.Spinner));
        if (d.Aerial.SecModel < 0) addMenu.Items.Add(Item("Aerial (the stock one)", () => { Doc.AddAerial(); Say("Aerial added"); }));
    }

    /// <summary>Choose a model by seeing it. Null: nothing chosen. Empty: no model</summary>
    public async Task<string?> AskModel(string title)
    {
        if (Doc.Car == null) return null;
        var p = new ModelPicker(Doc.Car.Game, Doc.Car.Folder, title);
        await p.ShowDialog(this);
        if (p.FilePath != null)
        {
            // Picky: a model with polygons, or nothing changes and it says why
            if (FileChecks.Model(p.FilePath) is { } why)
            {
                Say("Can't use that model: " + why);
                await Dialogs.Ask(this, "Can't use that model:" + Environment.NewLine + why, "OK");
                return null;
            }
            try
            {
                if (!Path.GetExtension(p.FilePath).Equals(".obj", StringComparison.OrdinalIgnoreCase)) return Doc.BringIn(p.FilePath);
                // An .obj from Blender: made into a .prm in the car's folder
                var tmp = Path.Combine(Path.GetTempPath(), "rvcs_" + Guid.NewGuid().ToString("N") + ".prm");
                ObjModel.Import(p.FilePath).Save(tmp);
                try { return Doc.BringIn(tmp, Names.Stem(p.FilePath) + ".prm"); }
                finally { File.Delete(tmp); }
            }
            catch (Exception ex)
            {
                Say("Can't use that model: " + ex.Message);
                return null;
            }
        }
        return p.Result;
    }

    public async Task PickModelFor(Selection s)
    {
        var rel = await AskModel("Model for " + s.Label);
        if (rel == null) return;
        if (rel == "") { Doc.RemovePart(s); return; }
        Doc.SetPartModel(s, rel);
        Doc.Select(s);
        Say(s.Label + ": " + Names.File(rel));
    }

    public void SetLocked(bool locked)
    {
        lockedPanel.IsVisible = locked;
        pickerPanel.IsVisible = !locked;
        ShowLeft();
        if (locked) View.Focus();
    }

    /// <summary>Back to the list of cars. The open car stays until another one is picked</summary>
    public void ChangeCar()
    {
        search.Text = "";
        FillList();
        SetLocked(false);
        search.Focus();
    }

    /// <summary>The hull shown and edited, one state for the checkbox and the hull's row in the Parts list</summary>
    readonly CheckBox hullShow = new() { Content = "Show and edit the hull", FontSize = 12, MinHeight = 22 };

    public List<ListBoxItem> PartsItems() => partsList.Items.OfType<ListBoxItem>().ToList();
    /// <summary>Selects a row of the Parts list as a click does (tests)</summary>
    public void ClickPartsItem(ListBoxItem i) => partsList.SelectedItem = i;

    /// <summary>The hull shown (or not) in the view and opened in the Parts list: everything that shows it comes here</summary>
    public void ShowHull(bool on)
    {
        if (hullShow.IsChecked == on)
        {
            View.ShowHull = on;
            if (!on && Doc.Sel.IsHull) { if (Doc.VertexMode) Doc.LeaveVertexMode(); Doc.Select(new Selection(SelKind.Body)); }
            FillParts(); View.Redraw(); return;
        }
        hullShow.IsChecked = on; // shows it, fills the list, redraws
    }

    /// <summary>
    /// The Parts list in groups: the car's parts, its points (centre of mass, weapons, cameras), and the collision hull
    /// as one row that opens to its parts and spheres, indented, instead of a dozen rows among the rest.
    /// </summary>
    void FillParts()
    {
        if (Doc.D is not { } d) return;
        fillingParts = true;
        // Let go of the selection before the rows go: a selection left on rows being removed breaks the list
        partsList.SelectedItem = null;
        partsList.Items.Clear();
        int indent = 0;
        void Label(string text)
        {
            partsList.Items.Add(new ListBoxItem
            {
                Content = new TextBlock { Text = text, FontSize = 11, FontWeight = FontWeight.SemiBold, Foreground = Page.Faint },
                IsHitTestVisible = false, Focusable = false, Padding = new Thickness(8, 8, 4, 1), MinHeight = 0
            });
        }
        void Add(Selection s)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            row.Children.Add(new TextBlock { Text = s.Label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis });
            if (Doc.HasModel(s))
            {
                var eye = new CheckBox { IsChecked = !Doc.Hidden.Contains(s), MinWidth = 0, MinHeight = 0, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
                ToolTip.SetTip(eye, "Show or hide it in the view. The file doesn't change");
                eye.IsCheckedChanged += (_, _) => { if ((eye.IsChecked != true) != Doc.Hidden.Contains(s)) Doc.ToggleHidden(s); };
                Grid.SetColumn(eye, 1);
                row.Children.Add(eye);
            }
            partsList.Items.Add(new ListBoxItem { Content = row, Tag = s, Padding = new Thickness(8 + indent, 2, 4, 2), MinHeight = 24 });
        }
        Label("Car parts");
        if (d.BodyModel >= 0) Add(new(SelKind.Body));
        for (int i = 0; i < 4; i++) if (d.Wheels[i].Present) Add(new(SelKind.Wheel, i));
        for (int i = 0; i < 4; i++) if (d.Springs[i].Model >= 0) Add(new(SelKind.Spring, i));
        for (int i = 0; i < 4; i++) if (d.Axles[i].Model >= 0) Add(new(SelKind.Axle, i));
        for (int i = 0; i < 4; i++) if (d.Pins[i].Model >= 0) Add(new(SelKind.Pin, i));
        if (d.Spinner.Model >= 0) Add(new(SelKind.Spinner));
        if (d.Aerial.SecModel >= 0) { Add(new(SelKind.AerialBase)); Add(new(SelKind.AerialTip)); }
        Label("Points");
        Add(new(SelKind.CoM));
        Add(new(SelKind.Weapon1));
        if (d.HasWeapon2) Add(new(SelKind.Weapon2));
        if (d.Camera != null) { Add(new(SelKind.HoodCam)); Add(new(SelKind.RearCam)); }
        if (Doc.Hull is { } h)
        {
            bool open = View.ShowHull || Doc.Sel.IsHull;
            string Count(int n, string one) => n + " " + one + (n == 1 ? "" : "s");
            var head = new TextBlock
            {
                Text = (open ? "▾  " : "▸  ") + "Collision hull   " + Count(h.Convex.Count, "part") + ", " + Count(h.Spheres.Count, "sphere"),
                FontSize = 12, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center
            };
            var hi = new ListBoxItem { Content = head, Tag = "hull", Padding = new Thickness(8, 6, 4, 2), MinHeight = 26 };
            ToolTip.SetTip(hi, open ? "Click to close the hull and hide it in the view" : "Click to show the hull in the view and edit its parts and spheres");
            partsList.Items.Add(hi);
            if (open)
            {
                indent = 16;
                for (int i = 0; i < h.Convex.Count; i++) Add(new(SelKind.HullPart, i));
                for (int i = 0; i < h.Spheres.Count; i++) Add(new(SelKind.HullSphere, i));
                indent = 0;
            }
        }
        SyncParts();
        fillingParts = false;
    }

    void SyncParts()
    {
        bool was = fillingParts;
        fillingParts = true;
        partsList.SelectedItem = partsList.Items.OfType<ListBoxItem>().FirstOrDefault(i => i.Tag is Selection s && s == Doc.Sel);
        fillingParts = was;
    }

    static MenuItem Item(string text, Action act, string? gesture = null)
    {
        var m = new MenuItem { Header = text, FontSize = 13, Icon = Sym.Icon(text) };
        if (gesture != null) m.InputGesture = KeyGesture.Parse(gesture);
        m.Click += (_, _) => act();
        return m;
    }

    Menu BuildMenu()
    {
        var file = new MenuItem { Header = "File", FontSize = 13 };
        file.Items.Add(Item("Change car...", ChangeCar, "Ctrl+O"));
        file.Items.Add(Item("Open Re-Volt folder...", async () => await AskGameFolder()));
        file.Items.Add(Item("New car...", async () => await NewCar()));
        file.Items.Add(Item("Install a car from a zip or folder...", async () => await InstallAsked()));
        file.Items.Add(Item("Undo the last install", async () => await UndoInstall()));
        file.Items.Add(new Separator());
        file.Items.Add(Item("Save", async () => await SaveAsked(), "Ctrl+S"));
        file.Items.Add(Item("Make a copy of this car...", async () => await CopyCar()));
        file.Items.Add(Item("Back to the game's version of this car...", async () => await DropLocal()));
        file.Items.Add(Item("Revert to the saved car", Revert));
        file.Items.Add(new Separator());
        file.Items.Add(Item("Open car folder", OpenFolder));
        file.Items.Add(Item("Rename car folder...", async () => await RenameCar()));
        file.Items.Add(Item("Pack as zip...", async () => await PackZip()));
        var autoSave = new MenuItem { Header = "Save by itself", FontSize = 13, ToggleType = MenuItemToggleType.CheckBox, IsChecked = AutoSave };
        ToolTip.SetTip(autoSave, "Saves a few seconds after each change, as the old Car Studio's Auto save");
        autoSave.Click += (_, _) => SetAutoSave(autoSave.IsChecked);
        file.Items.Add(autoSave);
        file.Items.Add(new Separator());
        file.Items.Add(Item("Import a model from .obj for the selected part...", async () => await ImportObjAsked()));
        file.Items.Add(Item("Export the selected part as .obj...", async () => await ExportObjAsked()));
        file.Items.Add(Item("Export the whole car as .obj...", async () => await ExportCarObjAsked()));
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
        tools.Items.Add(Item("Fill frontend stats from a simulated run", () => Tool("Stats", () => CarSim.FillStats(Doc.D!, Doc.Hull))));
        tools.Items.Add(new Separator());
        tools.Items.Add(Item("Compare with another car and take its values...", () => OpenCompare()));

        var run = new MenuItem { Header = "Run", FontSize = 13 };
        run.Items.Add(Item("Test run in RVGL", () => TestRunGame(false), "F5"));
        run.Items.Add(Item("Test run in RVGL, dev mode (Ctrl+R reloads the car)", () => TestRunGame(true), "Shift+F5"));
        run.Items.Add(new Separator());
        run.Items.Add(Item("Drive here", () => StartDrive(SimGround.Flat), "F6"));
        run.Items.Add(Item("Race here", () => StartRace(), "F7"));
        run.Items.Add(Item("Automatic runs and report", AutoRuns));

        var view = new MenuItem { Header = "View", FontSize = 13 };
        var themes = new MenuItem { Header = "Theme", FontSize = 13 };
        foreach (var (kind, text) in new[] { (ThemeKind.Dark, "Dark: black"), (ThemeKind.Light, "Light: ghost white"), (ThemeKind.Classic, "Classic: as it was") })
        {
            var item = Item(text, () => SetTheme(kind));
            item.ToggleType = MenuItemToggleType.Radio;
            item.IsChecked = kind == Look.Kind;
            item.Tag = kind;
            themeItems.Add(item);
            themes.Items.Add(item);
        }
        view.Items.Add(themes);
        view.Items.Add(new Separator());
        view.Items.Add(Item("Frame the car", View.FrameCar));
        view.Items.Add(Item("Messages...", async () => await ShowMessages()));
        view.Items.Add(Item("Frame what's selected", () => View.FramePart(Doc.Sel)));

        var help = new MenuItem { Header = "Help", FontSize = 13 };
        var tours = new MenuItem { Header = "Tours", FontSize = 13 };
        for (int i = 0; i < TourNames.Length; i++) { int k = i; tours.Items.Add(Item(TourNames[i], () => StartTour(k))); }
        help.Items.Add(tours);
        help.Items.Add(new Separator());
        help.Items.Add(Item("Documentation", OpenDocs));
        help.Items.Add(Item("Guide and calculations", ShowGuide, "F1"));
        help.Items.Add(Item("Mouse and keys", () => Say("Click: select, double click: frame it, right click: its menu. Drag a handle, or W E R then type an amount (X Y Z: axis, Enter, Esc). Right drag: turn, middle drag: pan, wheel: zoom. Arrows + PgUp/PgDn: nudge. 1 2 3 4: views. F: frame the car.")));
        help.Items.Add(Item("About", () => Say("RV Car Studio 3 by Kallel A.Y (theKDL). GPL v3. Re-Volt is a trademark of its owners.")));

        var m = new Menu { Background = BarBrush, Height = 28, Padding = new Thickness(4, 0) };
        foreach (var x in new[] { file, edit, view, tools, run, help }) m.Items.Add(x);
        return m;
    }

    readonly List<RadioButton> shadeButtons = new();
    public void SetShadeView(ShadeMode m)
    {
        foreach (var b in shadeButtons) if (Equals(b.Tag, m)) b.IsChecked = true;
    }

    Button BarButton(string text, string tip, Action click, Button? use = null)
    {
        var b = use ?? new Button();
        b.Content = text; b.FontSize = 12; b.Padding = new Thickness(10, 5); b.Margin = new Thickness(2, 0); b.VerticalAlignment = VerticalAlignment.Center;
        ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        return b;
    }

    static Border Sep() => new() { Width = 1, Height = 18, Background = LineBrush, Margin = new Thickness(8, 0), VerticalAlignment = VerticalAlignment.Center };

    // In a group: no box of its own
    static Button Flat(Button b)
    {
        b.Classes.Add("flat");
        b.Margin = new Thickness(0);
        b.Padding = new Thickness(10, 3);
        b.MinHeight = 24;
        return b;
    }

    Control BuildToolbar()
    {
        // Wraps onto a second line when the window is too narrow for it all
        var p = new WrapPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 4) };
        p.Children.Add(titleText);
        p.Children.Add(modifiedDot);
        p.Children.Add(Sep());
        p.Children.Add(Sym.Set(BarButton("Save", "Save the car (Ctrl+S). The first version stays as .bak", async () => await SaveAsked(), saveBtn), Sym.Save, "Save"));
        p.Children.Add(Sym.Set(BarButton("Undo", "Ctrl+Z", Doc.Undo, undoBtn), Sym.Undo, "Undo"));
        p.Children.Add(Sym.Set(BarButton("Redo", "Ctrl+Y", Doc.Redo, redoBtn), Sym.Redo, "Redo"));
        p.Children.Add(Sep());

        var seg = new List<Control>();
        foreach (var (tool, text, tip) in new[]
        {
            (GizmoTool.Move, "Move", "Drag the arrows or the circle (W)"),
            (GizmoTool.Rotate, "Rotate", "Drag a ring: turns the model itself, or the aerial's direction (E). 5 degree steps with snap on"),
            (GizmoTool.Scale, "Scale", "Drag a box, or the circle for all axes: resizes the model and its radius or length (R)")
        })
        {
            var t = new RadioButton { Content = text, GroupName = "tool", IsChecked = tool == Doc.Tool, Tag = tool, Classes = { "seg" } };
            ToolTip.SetTip(t, tip);
            Sym.Set(t, tool switch { GizmoTool.Move => Sym.Move, GizmoTool.Rotate => Sym.Rotate, _ => Sym.Scale }, text);
            t.IsCheckedChanged += (_, _) => { if (t.IsChecked == true) { Doc.Tool = tool; View.Redraw(); } };
            toolButtons.Add(t);
            seg.Add(t);
        }
        p.Children.Add(Look.Group(seg.ToArray()));
        p.Children.Add(Sep());
        p.Children.Add(new TextBlock { Text = "Corners", VerticalAlignment = VerticalAlignment.Center, FontSize = 12, Foreground = Page.Faint, Margin = new Thickness(2, 0, 5, 0) });
        seg.Clear();
        foreach (var (mode, text, tip) in new[]
        {
            (MoveMode.Single, "Single", "Only this wheel or part"),
            (MoveMode.Mirror, "Mirror L/R", "The other side moves the opposite way"),
            (MoveMode.FrontBack, "Front/Back", "Front and back mirror each other: changes the wheelbase"),
            (MoveMode.All, "All four", "All four corners, mirrored both ways")
        })
        {
            var r = new RadioButton { Content = text, GroupName = "move", IsChecked = mode == Doc.MoveMode, Tag = mode, Classes = { "seg" } };
            moveButtons.Add(r);
            ToolTip.SetTip(r, tip);
            Sym.Set(r, mode switch { MoveMode.Single => Sym.One, MoveMode.Mirror => Sym.Mirror, MoveMode.FrontBack => Sym.FrontBack, _ => Sym.Four }, text);
            r.IsCheckedChanged += (_, _) => { if (r.IsChecked == true) { Doc.MoveMode = mode; View.Redraw(); } };
            seg.Add(r);
        }
        p.Children.Add(Look.Group(seg.ToArray()));
        var linked = new CheckBox { Content = "Suspension follows wheel", FontSize = 12, IsChecked = true, MinHeight = 24, Margin = new Thickness(8, 0, 6, 0) };
        linked.IsCheckedChanged += (_, _) => Doc.MoveLinked = linked.IsChecked == true;
        p.Children.Add(linked);

        var snap = new ComboBox { ItemsSource = new[] { "Snap off", "Snap 0.1", "Snap 0.5", "Snap 1", "Snap 5" }, SelectedIndex = 2, FontSize = 12, MinHeight = 28, Padding = new Thickness(10, 3, 0, 3), VerticalAlignment = VerticalAlignment.Center };
        snap.SelectionChanged += (_, _) => Doc.Snap = new[] { 0f, 0.1f, 0.5f, 1f, 5f }[Math.Max(snap.SelectedIndex, 0)];
        p.Children.Add(snap);
        p.Children.Add(Sep());

        seg.Clear();
        foreach (var (v, text) in new[] { (ViewMode.Perspective, "3D"), (ViewMode.Top, "Top"), (ViewMode.Front, "Front"), (ViewMode.Side, "Side") })
            seg.Add(Flat(BarButton(text, "View (" + ((int)v + 1) + ")", () => View.SetView(v))));
        seg.Add(Sym.Set(Flat(BarButton("Frame", "Frame the car (F)", View.FrameCar)), Sym.Frame, "Frame"));
        p.Children.Add(Look.Group(seg.ToArray()));
        p.Children.Add(Sym.Set(Flat(BarButton("UV / Texture", "The UV and texture editor: paint, generate, unwrap (U)", () => OpenUvEditor())), Sym.Palette, "UV and texture editor"));

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
        Toggle("Collision hull", false, v => { View.ShowHull = v; FillParts(); });
        Toggle("Suspension travel", false, v => View.ShowTravel = v);
        Toggle("Vertex normals", false, v => View.ShowNormals = v);
        Toggle("Markers", true, v => View.ShowMarkers = v);
        Toggle("Grid", true, v => View.ShowGrid = v);
        Toggle("Soft light", false, v => View.Lit = v);
        Toggle("View cube", Settings.ViewCube, v => { cubeBox.IsVisible = v; Settings.ViewCube = v; });
        if (Sym.Available) Toggle("Names on the buttons", Sym.ShowNames, Sym.SetShowNames);
        var showBtn = new Button { Content = "Show ▾", FontSize = 12, Padding = new Thickness(10, 5), Margin = new Thickness(2, 0), Flyout = viewMenu };
        Sym.Set(showBtn, Sym.Eye, "Show", "What the view shows: reflections, shadow, wires, hull, markers...");
        p.Children.Add(showBtn);
        seg.Clear();
        foreach (var (m, n, tip) in new[]
        {
            (ShadeMode.Textured, "Texture", "As in the game"),
            (ShadeMode.VertexColours, "Shading", "Texture off: only the vertex colours, to see shading work"),
            (ShadeMode.Clay, "Clay", "Only the shape")
        })
        {
            var r = new RadioButton { Content = n, GroupName = "shade", IsChecked = m == View.Shade, Tag = m, Classes = { "seg" } };
            ToolTip.SetTip(r, tip);
            Sym.Set(r, m switch { ShadeMode.Textured => Sym.Picture, ShadeMode.VertexColours => Sym.Shading, _ => Sym.Clay }, n);
            r.IsCheckedChanged += (_, _) => { if (r.IsChecked == true) { View.Shade = m; View.Redraw(); } };
            shadeButtons.Add(r);
            seg.Add(r);
        }
        p.Children.Add(Look.Group(seg.ToArray()));
        p.Children.Add(Sep());

        var runBtn = Sym.Set(BarButton("▶  Test run", "Run the car in RVGL (F5)", () => TestRunGame(false)), Sym.Play, "Test run", null, true);
        Look.Primary(runBtn);
        p.Children.Add(runBtn);
        var runMore = BarButton("▾", "The track, laps, opponents and the other settings for Test run and Race here", () => { });
        runMore.Flyout = RunFlyout;
        runMore.Padding = new Thickness(6, runMore.Padding.Top, 6, runMore.Padding.Bottom);
        p.Children.Add(runMore);
        p.Children.Add(Sym.Set(BarButton("Drive here", "Drive with the arrow keys: the original game's physics on a plain floor (F6)", () => StartDrive(SimGround.Flat)), Sym.Car, "Drive here"));
        p.Children.Add(Sym.Set(BarButton("Race here", "A race against CPU cars on a track made here, the original game's physics (F7)", () => StartRace()), Sym.Flag, "Race here"));
        p.Children.Add(Sep());
        p.Children.Add(ModeSwitch());
        p.Children.Add(BarButton(ThemeButtonText, "The look: dark, light or classic. Also in View, Theme", NextTheme, themeBtn));
        // Room between the lines when it wraps
        foreach (var c in p.Children)
            if (c.Margin.Top < 3) c.Margin = new Thickness(c.Margin.Left, 3, c.Margin.Right, 3);

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
        thumbs.Clear();
        thumbsComing.Clear();
        LoadCarList();
        BuildGameDependent();
        SetLocked(false);
        titleText.Text = "Pick a car";
        search.Focus();
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
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("56,*") };
            var image = new Image { Width = 48, Height = 48, Source = thumbs.GetValueOrDefault(c.Folder) };
            row.Children.Add(image);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(new TextBlock { Text = c.Name, FontSize = 13, TextTrimming = TextTrimming.CharacterEllipsis });
            text.Children.Add(new TextBlock { Text = c.Folder + (c.Stock ? "  ·  stock" : ""), FontSize = 10.5, Foreground = Page.Faint, TextTrimming = TextTrimming.CharacterEllipsis });
            Grid.SetColumn(text, 1);
            row.Children.Add(text);
            // Its picture is made when the row first comes into view, not for the whole list
            if (image.Source == null) image.AttachedToVisualTree += (_, _) => { if (image.Source == null) _ = LoadThumb(c.Folder, image); };
            carList.Items.Add(new ListBoxItem { Content = row, Padding = new Thickness(6, 3), Tag = c.Folder });
        }
        carCount.Text = show.Count == allCars.Count ? allCars.Count + " cars" : show.Count + " of " + allCars.Count + " cars";
        if (Doc.Car != null)
            carList.SelectedItem = carList.Items.Cast<ListBoxItem>().FirstOrDefault(i => (string)i.Tag! == Doc.Car.Folder);
        loadingList = false;
    }

    readonly Dictionary<string, Avalonia.Media.Imaging.Bitmap> thumbs = new(StringComparer.OrdinalIgnoreCase);

    readonly Dictionary<string, Task<byte[]?>> thumbsComing = new(StringComparer.OrdinalIgnoreCase);
    public int ThumbsAsked, ThumbsDone;
    public int CarsListed => carList.ItemCount;
    public void ScrollCars(int to) => carList.ScrollIntoView(to);
    public bool RowPicture(int i) => ((carList.Items[i] as ListBoxItem)?.Content as Grid)?.Children[0] is Image { Source: not null };

    async Task LoadThumb(string folder, Image image)
    {
        var game = Doc.Game;
        if (game == null) return;
        var back = Look.ThumbBack;
        var key = folder + back;
        if (!thumbsComing.TryGetValue(key, out var coming))
        {
            ThumbsAsked++;
            thumbsComing[key] = coming = PictureQueue.Make(() =>
            {
                using var b = Thumbnail.OfCar(new Car(game, folder), 96, back);
                return Thumbnail.Png(b);
            }, true);
        }
        var png = await coming;
        if (thumbsComing.Remove(key)) ThumbsDone++;
        if (png == null || Doc.Game != game) return;
        // The look may have changed meanwhile: then this one is of the old colour
        if (back != Look.ThumbBack) return;
        if (!thumbs.TryGetValue(folder, out var bmp)) thumbs[folder] = bmp = new Avalonia.Media.Imaging.Bitmap(new MemoryStream(png));
        image.Source = bmp;
    }

    public void PickCar(string folder)
    {
        if (Doc.Game == null) return;
        Doc.Open(Doc.Game, folder);
        Settings.LastCar = folder;
        SetLocked(true);
        loadingList = true;
        carList.SelectedItem = carList.Items.Cast<ListBoxItem>().FirstOrDefault(i => string.Equals((string)i.Tag!, folder, StringComparison.OrdinalIgnoreCase));
        loadingList = false;
    }

    async Task OnCarPicked()
    {
        if (loadingList || carList.SelectedItem is not ListBoxItem it || Doc.Game == null) return;
        var folder = (string)it.Tag!;
        if (Doc.Car != null && folder == Doc.Car.Folder) { SetLocked(true); return; }
        if (!await AskSave()) { FillList(); return; }
        PickCar(folder);
    }

    /// <summary>Unsaved changes? Ask. False means stay here</summary>
    async Task<bool> AskSave()
    {
        // The texture editor first: its painting and faces belong to this car, not the next one
        if (TextureEditor != null && !await TextureEditor.AskClose()) return false;
        if (!Doc.Modified) return true;
        var r = await Dialogs.Ask(this, "Save the changes of " + Doc.D!.Name + "?", "Save", "Don't save", "Cancel");
        if (r == 0) { Save(); return true; }
        return r == 1;
    }

    bool closingOk;
    async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (closingOk || (!Doc.Modified && TextureEditor?.TexDirty != true)) return;
        e.Cancel = true;
        if (await AskSave()) { closingOk = true; Close(); }
    }

    void OnCarOpened()
    {
        stockBanner.IsVisible = false;
        shadeOld = null;
        ResetPaint();
        View.FrameCar();
        titleText.Text = Doc.D!.Name == "" ? Doc.Car!.Folder : Doc.D.Name;
        lockedName.Text = titleText.Text;
        easyName.Text = easyTitle.Text = titleText.Text;
        ShowDrawing();
        if (!ProMode && easyModes.Count > 0) EasyGo(0);
        lockedInfo.Text = "cars\\" + Doc.Car!.Folder + (GameFolder.IsStock(Doc.Car.Folder) ? "  ·  stock" : "");
        ShowCarDetails();
        WatchParameters();
        FillParts();
        foreach (var p in pages) p.Refresh();
        RefreshRaw();
        RunChecks();
        Say("Opened " + Doc.Car!.Folder);
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
        easyName.Text = titleText.Text;
        easyApp.Text = modified ? "RV Car Studio   ● not saved" : "RV Car Studio";
        selectedPage?.Refresh();
        foreach (var p in pages) if (p.IsVisible) p.Refresh();
        FollowTexture();
        View.Redraw();
        checkTimer.Stop();
        checkTimer.Start();
        if (Doc.Sim == null && !rawText.IsFocused) rawDirty = true;
    }

    void OnSelectionChanged()
    {
        SyncParts();
        selectedPage = BuildSelectedPage();
        selectedHost.Child = selectedPage;
        selectedPage.Refresh();
        EasySyncParts();
    }

    public void Say(string s) { StatusText.Text = s; Remember(s); }

    /// <summary>Something a car maker does was done (a tool, a save, a pack): the tours follow it</summary>
    public event Action<string>? DidSomething;
    public readonly HashSet<string> Done = new();
    public void Did(string what) { Done.Add(what); DidSomething?.Invoke(what); }

    void Tool(string name, Action act)
    {
        if (Doc.Car == null) return;
        try
        {
            Doc.Edit(name, act);
            Doc.EndGesture();
            if (StatusText.Text is null || !StatusText.Text.StartsWith("Shadow")) Say(name + ": done");
            Did(name);
        }
        catch (Exception ex) { Say(name + " failed: " + ex.Message); }
    }

    public void Save()
    {
        if (Doc.Car == null) return;
        try
        {
            NoteOwnWrite();
            Doc.Save();
            RefreshRaw();
            ShowCarDetails();
            Say("Saved " + Doc.Car.ParamPath);
            Did("Save");
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
        issuesHead.Foreground = r.Errors.Count > 0 ? Look.Bad : r.Warnings.Count > 0 ? Look.Warn : Look.Good;
        var rows = new List<Control>();
        foreach (var e in r.Errors) rows.Add(new TextBlock { Text = "✖  " + e, Foreground = Look.Bad, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        foreach (var e in r.Warnings) rows.Add(new TextBlock { Text = "▲  " + e, Foreground = Look.Warn, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        foreach (var e in r.Infos) rows.Add(new TextBlock { Text = "·   " + e, Foreground = Page.Faint, FontSize = 12, TextWrapping = TextWrapping.Wrap });
        issues.ItemsSource = rows;
    }

    // --------------------------- Keys ---------------------------

    void OnKey(object? s, KeyEventArgs e)
    {
        keys.Add(e.Key);
        var typing = FocusManager?.GetFocusedElement() is TextBox;
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (ctrl && e.Key == Key.S) { _ = SaveAsked(); e.Handled = true; return; }
        if (typing && !ctrl) { if (e.Key == Key.Escape) View.Focus(); return; }
        if (ctrl && e.Key == Key.Z) { Doc.Undo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.Y) { Doc.Redo(); e.Handled = true; return; }
        if (ctrl && e.Key == Key.O) { ChangeCar(); e.Handled = true; return; }
        if (e.Key == Key.F5) { TestRunGame(e.KeyModifiers.HasFlag(KeyModifiers.Shift)); e.Handled = true; return; }
        if (e.Key == Key.F6) { if (Doc.Sim != null) StopDrive(); else StartDrive(SimGround.Flat); e.Handled = true; return; }
        if (e.Key == Key.F7) { if (Doc.Sim != null) StopDrive(); else StartRace(ctrl); e.Handled = true; return; }
        if (e.Key == Key.F1) { ShowGuide(); e.Handled = true; return; }
        // An exact amount typed during a drag, or after W, E or R
        if (!ctrl && Doc.Sim == null && View.KeyTyped(e.Key)) { e.Handled = true; return; }
        if (Doc.Sim != null)
        {
            if (e.Key == Key.Escape) StopDrive();
            if (e.Key == Key.R)
            {
                if (Doc.Race is { } race) race.Reposition(race.Player);
                else Doc.Sim.Reset();
            }
            if (e.Key == Key.I && Doc.Race is { } driven)
            {
                driven.AiDrivesPlayer = !driven.AiDrivesPlayer;
                Say(driven.AiDrivesPlayer ? "The AI drives your car, with your car's AI values. I to take it back" : "You drive");
            }
            if (e.Key == Key.Space && !e.KeyModifiers.HasFlag(KeyModifiers.Control)) Doc.Sim.Flip();
            e.Handled = true;
            return;
        }
        // Vertices: Tab in and out, A picks them all, Esc lets go of the picked ones, then of vertex editing
        if (e.Key == Key.Tab && !ctrl) { ToggleVertexMode(); e.Handled = true; return; }
        if (Doc.VertexMode && e.Key == Key.A && !ctrl) { Doc.PickVertices(Enumerable.Range(0, Doc.VertexPositions().Length), false); Say($"All {Doc.PickedVerts.Count} vertices picked"); e.Handled = true; return; }
        if (Doc.VertexMode && e.Key == Key.Escape) { if (Doc.PickedVerts.Count > 0) Doc.PickVertices(Array.Empty<int>(), false); else ToggleVertexMode(); e.Handled = true; return; }
        float amount = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 5 : Math.Max(Doc.Snap, 0.1f);
        switch (e.Key)
        {
            case Key.D1: View.SetView(ViewMode.Perspective); break;
            case Key.D2: View.SetView(ViewMode.Top); break;
            case Key.D3: View.SetView(ViewMode.Front); break;
            case Key.D4: View.SetView(ViewMode.Side); break;
            case Key.F: View.FrameCar(); break;
            case Key.U: OpenUvEditor(); break;
            case Key.W: SetTool(GizmoTool.Move); View.Arm(); break;
            case Key.E: SetTool(GizmoTool.Rotate); View.Arm(); break;
            case Key.R: SetTool(GizmoTool.Scale); View.Arm(); break;
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

    /// <summary>Vertex editing on the selected part, or back to whole parts (Tab)</summary>
    public void ToggleVertexMode()
    {
        if (Doc.VertexMode) { Doc.LeaveVertexMode(); Say("Back to whole parts"); View.Redraw(); return; }
        if (Doc.EnterVertexMode() is { } why) { Say(why + ". Vertices: click a part (or a hull part with Show and edit the hull), then Tab"); return; }
        if (Doc.VertexPart.Kind == SelKind.HullPart) ShowHull(true);
        Say($"Vertices of {Doc.VertexPart.Label}: click one (Shift adds, Ctrl takes away, drag a box), then move it with the handles, the arrows or W and a number. A: all. Tab or Esc: back to parts");
        View.Redraw();
    }

    readonly List<RadioButton> toolButtons = new();
    public void SetTool(GizmoTool t)
    {
        foreach (var b in toolButtons) if (Equals(b.Tag, t)) b.IsChecked = true;
    }

    // --------------------------- Drive ---------------------------

    public void StartDrive(SimGround ground)
    {
        if (Doc.Car == null) return;
        Doc.StartSim(ground);
        Did("Drive");
        hudBox.IsVisible = true;
        driveCamYaw = float.NaN;
        simLast = DateTime.UtcNow;
        simTimer.Start();
        View.Focus();
        Say("Driving: arrows or WASD, or a gamepad. R to restart, Esc to leave. Space flips it when it's nearly upside down, as in the game. The original game's physics on a plain floor: Test run has the real tracks.");
    }

    // ---- Race here ----
    public int RaceTrackIndex, RaceGroundIndex, RaceOpponents = 5, RaceLapsHere = 3;
    public float RaceSkill = 0.92f;
    public bool RaceSameCar, RaceAiDrives;

    /// <summary>Secret: Ctrl+F7 races on the game's surfaces</summary>
    public void StartRace(bool materials = false)
    {
        if (Doc.Car == null) return;
        Doc.StartRace(RaceTrackIndex, materials ? RaceGround.Materials : (RaceGround)RaceGroundIndex, RaceOpponents, RaceLapsHere, RaceSkill, RaceSameCar, RaceAiDrives);
        Did("Race");
        raceMap.Race = Doc.Race;
        raceMap.IsVisible = true;
        hudBox.IsVisible = true;
        driveCamYaw = float.NaN;
        simLast = DateTime.UtcNow;
        simTimer.Start();
        View.Focus();
        Say("Race: arrows or WASD, or a gamepad. Space or X flips a car on its roof, R puts you back on the road, I lets the AI drive, Esc leaves. The car physics and the CPU driver follow the game's; the road and walls are this editor's own.");
    }

    static string Clock(float seconds) => float.IsInfinity(seconds) ? "-" : $"{(int)(seconds / 60)}:{seconds % 60:00.00}";

    string RaceHud(Race r)
    {
        var me = r.Player;
        if (!r.Started) return r.CountdownLabel;
        var lines = new List<string>();
        int lap = Math.Clamp(me.Lap, 1, r.Laps);
        if (me.Finished) lines.Add($"FINISHED {Place(r.PlaceOf(me))} in {Clock(me.FinishTime)}");
        else lines.Add($"{Place(r.PlaceOf(me))} of {r.Cars.Count}    Lap {lap}/{r.Laps}    {Clock(r.Time)}");
        lines.Add($"{me.Sim.SpeedMph:0} mph    best lap {Clock(me.BestLap)}" + (r.AiDrivesPlayer ? "    AI driving" : ""));
        if (r.Track.Ground == RaceGround.Materials) lines.Add("On: " + me.Sim.SurfaceName);
        if (r.Time < 1) lines.Add("GO!");
        int n = 1;
        foreach (var c in r.Standings.Take(me.Finished ? r.Cars.Count : 4))
            lines.Add($"{n++}. {c.Name}" + (c.Finished ? "  " + Clock(c.FinishTime) : ""));
        return string.Join("\n", lines);
    }

    static string Place(int p) => p + (p % 10 == 1 && p != 11 ? "st" : p % 10 == 2 && p != 12 ? "nd" : p % 10 == 3 && p != 13 ? "rd" : "th");

    public string HudText => hud.Text ?? "";

    public void StopDrive()
    {
        simTimer.Stop();
        Doc.StopSim();
        raceMap.Race = null;
        raceMap.IsVisible = false;
        hudBox.IsVisible = false;
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
        if (GamePad.Read() is { } pad)
        {
            if (pad.Steer != 0) st = pad.Steer;
            if (pad.Throttle != 0) th = pad.Throttle;
            if (pad.Flip) s.Flip();
            if (pad.Back)
            {
                if (Doc.Race is { } padRace) padRace.Reposition(padRace.Player);
                else s.Reset();
            }
        }
        if (!float.IsNaN(AutoThrottle)) { th = AutoThrottle; st = AutoSteer; }
        s.Throttle = th;
        s.Steer = st;
        if (Doc.Race is { } race)
        {
            race.PlayerThrottle = th;
            race.PlayerSteer = st;
        }
        Doc.TickSim(dt);

        var fwd = Vector3.Transform(Vector3.UnitZ, s.Rot);
        var yaw = MathF.Atan2(fwd.X, fwd.Z);
        if (float.IsNaN(driveCamYaw)) driveCamYaw = yaw;
        var diff = MathF.IEEERemainder(yaw - driveCamYaw, MathF.Tau);
        driveCamYaw += diff * Math.Min(1, dt * 4); // as the game's camera: a quarter of a second behind
        View.View = ViewMode.Perspective;
        // The game's Behind camera: 460 back and 150 up, aimed 60 over the car
        View.Target = s.Centre + new Vector3(0, -60, 0);
        View.Yaw = driveCamYaw + MathF.PI;
        View.Pitch = MathF.Atan2(90, 460);
        View.Distance = MathF.Sqrt(90 * 90 + 460 * 460);
        hud.Text = Doc.Race is { } running ? RaceHud(running) : $"{s.SpeedMph:0} mph   {s.SpeedMph * 1.609f:0} km/h";
        raceMap.InvalidateVisual();
        View.Redraw();
    }

    void AutoRuns()
    {
        if (Doc.D == null) return;
        Say("Running...");
        var d = Doc.D.Clone();
        var rows = new List<string>();
        var a = CarSim.RunAcceleration(d, Doc.Hull);
        rows.Add($"Acceleration: {a.Note}");
        rows.Add($"   in the file: TopEnd {d.TopEnd:0}, Acc {d.Acc:0.0}. Simulated: TopEnd {a.TopEnd:0}, Acc {a.Acc:0.0}");
        rows.Add($"Hard turn: {CarSim.RunCircle(d, Doc.Hull).Note}");
        rows.Add($"Bumps: {CarSim.RunBumps(d, Doc.Hull).Note}");
        Dialogs.ShowText(this, "Automatic runs", string.Join("\n", rows) + "\n\nThis is a simulation from the parameters, not the game. Use Test run for the real thing.");
        Say("Automatic runs done");
    }

    // --------------------------- Tools with side effects ---------------------------

    async void AutoShade() => await ShadeAsked(0.45f, 0.55f, 0.85f);

    // ------- Realtime: sliders shade with fewer rays at once, the full quality follows when they stop -------

    Dictionary<Prm, (uint[][] Colors, byte[]? Lods)>? shadeOld;
    bool shadeBusy, shadePending, shadeAsking;
    float shadeA = 0.45f, shadeS = 0.55f, shadeO = 0.85f;
    readonly DispatcherTimer shadeFinal = new() { Interval = TimeSpan.FromMilliseconds(500) };
    public int ShadeRuns;

    List<Prm> ShadedModels()
    {
        var d = Doc.D!;
        var slots = new List<int> { d.BodyModel };
        slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
        if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
        return slots.Distinct().Select(s => Doc.Model(s)).Where(m => m != null).Select(m => m!).ToList();
    }

    public async Task ShadeLive(float ambient, float sun, float occlusion)
    {
        if (Doc.D == null || shadeAsking) return;
        (shadeA, shadeS, shadeO) = (ambient, sun, occlusion);
        if (shadeOld == null)
        {
            shadeAsking = true;
            var d = Doc.D;
            var files = new List<(string, string)>();
            var slots = new List<int> { d.BodyModel };
            slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
            if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
            foreach (var s in slots.Distinct())
                if (s is >= 0 and < 19 && Doc.SharedProblem(d.Models[s]) is { } why) files.Add((d.Models[s], why));
            var go = await AskSharedFiles(files);
            shadeAsking = false;
            if (!go) return;
            Doc.Edit("Shade", () => { });
            Doc.EndGesture();
            shadeOld = ShadedModels().ToDictionary(m => m, m => (m.Faces.Select(f => (uint[])f.Colors.Clone()).ToArray(), m.OtherLods));
        }
        shadeFinal.Stop();
        shadeFinal.Start();
        if (shadeBusy) { shadePending = true; return; }
        shadeBusy = true;
        do
        {
            shadePending = false;
            await ShadeRun(false);
        } while (shadePending);
        shadeBusy = false;
    }

    async Task ShadeRun(bool full)
    {
        var models = ShadedModels();
        foreach (var m in models) Doc.DirtyModels.Add(m);
        var opt = ShadeNow(full ? 160 : 24);
        var (scene, worlds) = ShadeSceneNow();
        await Task.Run(() =>
        {
            foreach (var m in models) Shader.Shade(m, opt, full, scene, worlds.TryGetValue(m, out var w) ? w : null);
        });
        ShadeRuns++;
        Doc.ModelVersion++;
        Doc.Touch();
    }

    async void ShadeFinish()
    {
        shadeFinal.Stop();
        if (shadeBusy || Doc.D == null || shadeOld == null) return;
        shadeBusy = true;
        await ShadeRun(true);
        shadeBusy = false;
        Say("Shaded. Save keeps it, the first version of each model stays as .bak");
        Did("Shade");
    }

    /// <summary>Back to the colours from before the sliders were touched</summary>
    public void ShadeUndo()
    {
        if (shadeOld == null) return;
        shadeFinal.Stop();
        foreach (var (m, old) in shadeOld)
        {
            for (int i = 0; i < m.Faces.Length; i++) Array.Copy(old.Colors[i], m.Faces[i].Colors, 4);
            m.OtherLods = old.Lods;
            Doc.DirtyModels.Remove(m);
        }
        shadeOld = null;
        Doc.ModelVersion++;
        Doc.Touch();
        Say("Shading as it was");
    }

    public async Task ShadeAsked(float ambient, float sun, float occlusion)
    {
        if (Doc.D is not { } d) return;
        var files = new List<(string, string)>();
        var slots = new List<int> { d.BodyModel };
        slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
        if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
        foreach (var s in slots.Distinct())
            if (s is >= 0 and < 19 && Doc.SharedProblem(d.Models[s]) is { } why) files.Add((d.Models[s], why));
        if (!await AskSharedFiles(files)) return;
        DoAutoShade(ambient, sun, occlusion);
    }

    // Everything the shading panel says: the light itself is fixed, as in a clay render
    public ShadeSmooth ShadeSmoothing = ShadeSmooth.ByAngle;
    public float ShadeAngle = 45;

    // The light, from the pad: degrees round from the front towards the right, up from the horizon, and its radius
    public double ShadeAround = LightPad.DefaultAround, ShadeUp = LightPad.DefaultHeight, ShadeRadius = LightPad.DefaultRadius;

    public ShadeOptions ShadeNow(int rays)
    {
        double a = ShadeAround * Math.PI / 180, up = ShadeUp * Math.PI / 180;
        // Towards the light; Y goes down, so up is -Y, the front is +Z and the right +X
        var toLight = new Vector3((float)(Math.Cos(up) * Math.Sin(a)), (float)-Math.Sin(up), (float)(Math.Cos(up) * Math.Cos(a)));
        return new(shadeA, shadeS, shadeO, toLight, rays, ShadeSmoothing, ShadeAngle, SunSoft: (float)(ShadeRadius * Math.PI / 180 / 0.35));
    }

    // The whole car standing together: wheels shade the body, the body shades the wheels
    (ShadeScene? Scene, Dictionary<Prm, Matrix4x4?> Worlds) ShadeSceneNow()
    {
        var worlds = new Dictionary<Prm, Matrix4x4?>();
        if (Doc.D == null) return (null, worlds);
        var parts = new List<(Prm, Matrix4x4)>();
        foreach (var part in CarPose.Build(Doc.D))
        {
            if (part.Kind is PartKind.AerialSection or PartKind.AerialTop) continue;
            if (Doc.Model(part.Slot) is not { } m) continue;
            parts.Add((m, part.World));
            worlds.TryAdd(m, part.World);
        }
        return (new ShadeScene(parts), worlds);
    }

    /// <summary>Rewrite the vertex normals of the car's main models. Undo takes it back</summary>
    public async Task RewriteNormals(ShadeSmooth mode, float angle)
    {
        if (Doc.D is not { } d) return;
        var files = new List<(string, string)>();
        var slots = new List<int> { d.BodyModel };
        slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
        if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
        foreach (var s in slots.Distinct())
            if (s is >= 0 and < 19 && Doc.SharedProblem(d.Models[s]) is { } why) files.Add((d.Models[s], why));
        if (!await AskSharedFiles(files)) return;
        DoRewriteNormals(mode, angle);
    }

    public void DoRewriteNormals(ShadeSmooth mode, float angle)
    {
        var models = ShadedModels();
        int before = models.Sum(m => m.Verts.Length);
        bool ok = true;
        Doc.EditModels("Vertex normals", models, () =>
        {
            foreach (var m in models) ok &= Shader.SetNormals(m, mode, angle);
        });
        int after = models.Sum(m => m.Verts.Length);
        Say(!ok ? "A model would need too many vertices: it stays as it was" : after == before ? $"Normals rewritten, {after} vertices. Save keeps it, undo takes it back" : $"Normals rewritten: {before} vertices became {after}, as sharp edges need two. Save keeps it, undo takes it back");
    }

    public void DoAutoShade(float ambient, float sun, float occlusion)
    {
        if (Doc.D is not { } d) return;
        (shadeA, shadeS, shadeO) = (ambient, sun, occlusion);
        var opt = ShadeNow(160);
        var (scene, worlds) = ShadeSceneNow();
        var slots = new List<int> { d.BodyModel };
        slots.AddRange(d.Wheels.Where(w => w.Present).Select(w => w.Model));
        if (d.Spinner.Model >= 0) slots.Add(d.Spinner.Model);
        int n = 0;
        foreach (var slot in slots.Distinct())
            if (Doc.Model(slot) is { } m)
            {
                Shader.Shade(m, opt, true, scene, worlds.TryGetValue(m, out var world) ? world : null);
                Doc.DirtyModels.Add(m);
                n++;
            }
        Doc.ModelVersion++;
        Doc.Touch();
        Say($"Shaded {n} models. See it, then Save to keep it (the first version stays as .bak)");
        Did("Shade");
    }

    public async Task<string?> MakeCarbox()
    {
        if (Doc.Car == null) return null;
        var (yaw, pitch, dist, tgt) = (View.Yaw, View.Pitch, View.Distance, View.Target);
        // The game's own look in one click: the car cut out of the view, big and angled, its name behind in the car's colour
        View.Yaw = Carbox.StockTurn * MathF.PI / 180; View.Pitch = Carbox.StockHeight * MathF.PI / 180;
        View.FrameCar();
        SKBitmap cut;
        try
        {
            using var white = await View.Capture(512, true, 0, new System.Numerics.Vector3(1, 1, 1));
            using var black = await View.Capture(512, true, 0, new System.Numerics.Vector3(0, 0, 0));
            cut = Carbox.Cutout(white, black);
        }
        catch (Exception ex) { Say("Couldn't take the car's picture: " + ex.Message); return null; }
        finally { (View.Yaw, View.Pitch, View.Distance, View.Target) = (yaw, pitch, dist, tgt); }
        SKColor color;
        using (var tex = Doc.Car.Game.Find(Doc.D!.TPage) is { } tf ? Images.Load(tf) : null) color = Carbox.NameColourFrom(tex);
        using var box = Carbox.Compose(cut, Carbox.Stock(Doc.D!.Name, color));
        cut.Dispose();
        string path = "";
        Tool("Carbox", () => path = CarTools.SaveCarbox(Doc.Car, box));
        Say("Carbox saved: " + path);
        View.Redraw();
        return path;
    }

    void MakeHull()
    {
        if (Doc.Car == null || Doc.Model(Doc.D!.BodyModel) is not { } body) { Say("The car has no body to make a hull from"); return; }
        if (Doc.Car.IsForeign(Doc.D.Coll) || Doc.Car.OtherUsers(Doc.D.Coll).Count > 0)
            Tool("Hull", () => Doc.D!.Coll = "cars\\" + Doc.Car.Folder + "\\hull.hul");
        if (!Doc.RebuildHull(HullDetail)) { Say("Couldn't make a hull from this body"); return; }
        ShowHull(true);
        Say("Hull made: " + HullReport() + ". Save keeps it");
        Did("Hull");
        View.Redraw();
    }

    /// <summary>How many spheres a made hull may have</summary>
    public HullMaker.Detail HullDetail = HullMaker.Detail.Normal;

    /// <summary>The hull in a line: how many spheres, and how well they stand for the body</summary>
    public string HullReport()
    {
        if (Doc.Hull is not { } h) return "no hull";
        static string N(int n, string what) => n + " " + what + (n == 1 ? "" : "s");
        var s = N(h.Convex.Count, "part") + ", " + N(h.Spheres.Count, "sphere");
        if (Doc.HullQuality() is not { } q) return s;
        // Against the stock cars: 1.6 on average, 5.9 for all but the worst twentieth
        var word = q.Gap95 <= 4.5f ? "follow the body closely" : q.Gap95 <= 6.5f ? "follow the body as the stock cars' do" : q.Gap95 <= 10 ? "leave parts of the body uncovered" : "leave much of the body uncovered";
        return FormattableString.Invariant($"{s} that {word} (gaps {q.Gap:0.0} on average, {q.Gap95:0.0} at the wider places; {q.OutMax:0.0} out at most)");
    }

    void RefillSpheres()
    {
        if (Doc.Hull == null) { MakeHull(); return; }
        if (!Doc.RefillSpheres(HullDetail)) { Say("Couldn't fill this body with spheres"); return; }
        ShowHull(true);
        Say("Spheres made again: " + HullReport() + ". Save keeps them");
        Did("Hull");
        View.Redraw();
    }

    /// <summary>Making the hull: how fine, the buttons, how good it is (Model, the hull's own page, Easy's Collision)</summary>
    void HullRows(Page p)
    {
        // A setting of the tool, not of the car: no undo step, nothing to save
        var box = new ComboBox { ItemsSource = HullMaker.DetailNames, SelectedIndex = (int)HullDetail, FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch };
        p.Live(() => box.SelectedIndex = (int)HullDetail);
        box.SelectionChanged += (_, _) => { if (box.SelectedIndex >= 0) HullDetail = (HullMaker.Detail)box.SelectedIndex; };
        p.Labeled("Spheres", box, "The stock cars have 10 to 34. More follow the body closer, few are enough for a plain shape");
        p.Button("Make a hull from the body", MakeHull, "Its convex part (two for a cabin on a flat base) and spheres with the body's outline, to edit from there. Save keeps it");
        p.Button("Fill with spheres again", RefillSpheres, "Only the spheres: the hull's parts stay as they are");
        p.Info(() => Doc.Hull == null ? "No hull yet" : char.ToUpper(HullReport()[0]) + HullReport()[1..]);
    }

    public async void TestRunGame(bool dev)
    {
        if (Doc.Car == null) return;
        try
        {
            await SaveAsked();
            var run = new TestRun(Doc.Car.Game);
            if (run.FindExe() == null) { Say("No RVGL found in " + Doc.Car.Game.Root); return; }
            var game = run.Start(Doc.Car.Folder, SelectedLevel, dev, RaceCars, RaceLaps);
            Did("RVGL");
            if (!RaceByItself || dev)
            {
                Say(dev ? "RVGL started in dev mode: Ctrl+R reloads the car, Start Race has Calc Car Stats" : "RVGL started with " + Doc.D!.Name + " picked: choose Start Race");
                return;
            }
            Say("RVGL starting. It goes to the race by itself: leave its window in front");
            var levelName = SelectedLevel == null ? null : run.Levels().FirstOrDefault(l => l.Folder == SelectedLevel).Name;
            await TestRun.DriveToRace(game, new RaceSetup(Doc.D!.Name, levelName, RaceCars, RaceLaps), s => Dispatcher.UIThread.Post(() => Say(s)));
        }
        catch (Exception ex) { Say("Couldn't start RVGL: " + ex.Message); }
    }

    public string? SelectedLevel;
    public bool RaceByItself = true;
    public int RaceCars = 8, RaceLaps = 3;

    /// <summary>docs/index.html beside the program, in the browser (a build without it: the one on GitHub)</summary>
    void OpenDocs()
    {
        var file = Path.Combine(AppContext.BaseDirectory, "docs", "index.html");
        var what = File.Exists(file) ? file : "https://github.com/kallelay/RV-Car-Studio";
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(what) { UseShellExecute = true }); }
        catch (Exception ex) { Say("Couldn't open " + what + ": " + ex.Message); }
    }

    void OpenFolder()
    {
        if (Doc.Car == null) return;
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(Doc.Car.Dir) { UseShellExecute = true }); }
        catch (Exception ex) { Say(ex.Message); }
    }

    async Task NewCar()
    {
        if (Doc.Game == null) return;
        var w = new NewCarWizard(Doc.Game, Doc.Car?.Folder);
        await w.ShowDialog(this);
        if (w.Result is not { } r) return;
        if (!await AskSave()) return;
        // Made in a new folder: if anything goes wrong, the half made car goes away again (no "already a car" next time)
        var dest = Path.Combine(Doc.Game.CarsDir, r.Folder);
        bool existed = Directory.Exists(dest);
        try
        {
            CreateCar(r);
        }
        catch (Exception ex)
        {
            // Not opened yet (it fails before that): the half made folder goes
            bool kept = true;
            if (!existed && Directory.Exists(dest) && Doc.Car?.Folder.Equals(r.Folder, StringComparison.OrdinalIgnoreCase) != true)
            {
                try { Directory.Delete(dest, true); kept = false; } catch (Exception) { }
                LoadCarList();
            }
            Say("Couldn't make the car: " + ex.Message);
            await Dialogs.Ask(this, "Couldn't make " + r.Name + ":" + Environment.NewLine + ex.Message + Environment.NewLine + Environment.NewLine +
                (kept ? "" : "Nothing was kept. ") + "Check the files and try again.", "OK");
        }
    }

    /// <summary>Make the car and open it where that kind of work starts</summary>
    public void CreateCar(NewCarChoice r)
    {
        var c = Car.CreateFrom(Doc.Game!, r.BaseCar, r.Folder, r.Name);
        if (r.Kind == NewCarKind.Original)
        {
            CarTools.SetupOriginal(c, r.BodyFile!, r.WheelFile, r.TextureFile);
            c.Save();
            if (File.Exists(c.ParamPath + ".bak")) File.Delete(c.ParamPath + ".bak");
        }
        LoadCarList();
        PickCar(r.Folder);
        Tabs.SelectedIndex = r.Kind == NewCarKind.Repaint ? PaintTabIndex : 0;
        View.ShowHull = false;
        Say(r.Kind switch
        {
            NewCarKind.Repaint => "Repaint: change the colours in the Paint tab, or open the texture in your image editor",
            NewCarKind.Remodel => "Remodel: click a part to change it. Replace, Remove and Add a part are on the left and on its page",
            _ => "Your model is in: wheels placed, sizes fitted, a first hull made. Check the positions, then Tools for shade, shadow and carbox"
        });
        Did("NewCar:" + r.Kind);
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

    /// <summary>The readme window; Write it and pack goes on to the zip</summary>
    public async Task OpenReadme()
    {
        if (Doc.Car == null) return;
        var w = new ReadmeWindow(Doc.Car, Doc.Hull, false);
        Readme_ = w;
        await w.ShowDialog(this);
        Readme_ = null;
        if (w.WrittenPath != null) { Say("Readme written: " + w.WrittenPath); Did("Readme"); }
        if (w.Result == ReadmeWindow.Outcome.Pack) await PackZip(readmeDone: true);
    }
    public ReadmeWindow? Readme_;

    /// <summary>
    /// Pack as zip: saved first, then the readme (asked when the car has none, or changed since it was written), then
    /// the zip to unzip on the game's folder.
    /// </summary>
    async Task PackZip(bool readmeDone = false)
    {
        if (Doc.Car == null) return;
        if (Doc.Modified) Save();
        if (!readmeDone)
        {
            var rm = Doc.Car.Game.Find("cars\\" + Doc.Car.Folder + "\\readme.txt");
            // No readme, or older than one of the car's files: the car changed since it was written
            bool stale = rm == null || File.GetLastWriteTimeUtc(rm) < Doc.Car.Game.CarFiles(Doc.Car.Folder)
                .Where(x => !x.Key.EndsWith("readme.txt", StringComparison.OrdinalIgnoreCase) && !x.Key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .Select(x => File.GetLastWriteTimeUtc(x.Value)).DefaultIfEmpty(DateTime.MinValue).Max();
            if (stale)
            {
                var w = new ReadmeWindow(Doc.Car, Doc.Hull, true);
                Readme_ = w;
                await w.ShowDialog(this);
                Readme_ = null;
                if (w.Result == ReadmeWindow.Outcome.Closed) { Say("Not packed"); return; }
                if (w.WrittenPath != null) Did("Readme");
            }
        }
        var f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions { SuggestedFileName = Doc.Car.Folder + ".zip", DefaultExtension = "zip" });
        var path = f?.TryGetLocalPath();
        if (path == null) return;
        try
        {
            Doc.Car.Pack(path, true);
            LastPack = path;
            Say("Packed: " + path + ". Unzip it on a game folder to install the car");
            Did("Pack");
        }
        catch (Exception ex) { Say("Couldn't pack: " + ex.Message); }
    }
    public string? LastPack;

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
