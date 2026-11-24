using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// Working with other programs and other cars: models out to .obj and back in (and back in again each time the .obj is
/// saved, so Blender and the Studio can stay open side by side), and comparing with another car to take its values.
/// </summary>
public sealed partial class MainWindow
{
    static readonly FilePickerFileType ObjFiles = new("Wavefront .obj") { Patterns = new[] { "*.obj" } };

    static bool HoldsModel(Selection s) => s.Kind is SelKind.Body or SelKind.Wheel or SelKind.Spring or SelKind.Axle or SelKind.Pin
        or SelKind.Spinner or SelKind.AerialBase or SelKind.AerialTip;

    string? TexturePath() => Doc.D is { } d ? Doc.Game?.Find(d.TPage) : null;

    /// <summary>The selected part's model as .obj</summary>
    public async Task ExportObjAsked()
    {
        if (Doc.D == null) return;
        var m = Doc.ModelsOf(Doc.Sel).FirstOrDefault();
        if (m == null) { Say("Select a part with a model first (the body, a wheel...)"); return; }
        var name = Names.Stem(Doc.ModelName(m) ?? "model");
        var f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save " + Doc.Sel.Label + " as .obj", SuggestedFileName = name + ".obj", DefaultExtension = "obj", FileTypeChoices = new[] { ObjFiles }
        });
        if (f?.TryGetLocalPath() is not { } path) return;
        ExportObj(Doc.Sel, path);
    }

    public bool ExportObj(Selection s, string path)
    {
        var m = Doc.ModelsOf(s).FirstOrDefault();
        if (m == null) return false;
        try
        {
            ObjModel.Export(m, path, TexturePath());
            Say($"Saved {Names.File(path)}: {m.Faces.Length} polygons. 1 metre there is 100 units here; bring it back with Import from .obj");
            return true;
        }
        catch (Exception ex) { Say("Can't save the .obj: " + ex.Message); return false; }
    }

    /// <summary>All the car's parts where they stand, one object each: something to model around</summary>
    public async Task ExportCarObjAsked()
    {
        if (Doc.Car == null) return;
        var f = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the whole car as .obj", SuggestedFileName = Doc.Car.Folder + "_car.obj", DefaultExtension = "obj", FileTypeChoices = new[] { ObjFiles }
        });
        if (f?.TryGetLocalPath() is { } path) ExportCarObj(path);
    }

    public int ExportCarObj(string path)
    {
        if (Doc.D is not { } d) return 0;
        var parts = new List<(string, Prm, System.Numerics.Matrix4x4)>();
        foreach (var p in CarPose.Build(d))
            if (Doc.Model(p.Slot) is { } m)
                parts.Add((p.Kind + (p.Kind is PartKind.Body or PartKind.Spinner ? "" : "_" + p.Index), m, p.World));
        try
        {
            ObjModel.Export(parts, path, TexturePath());
            Say($"Saved the car as {Names.File(path)}: {parts.Count} parts where they stand");
        }
        catch (Exception ex) { Say("Can't save the .obj: " + ex.Message); return 0; }
        return parts.Count;
    }

    public async Task ImportObjAsked()
    {
        if (Doc.Car == null) return;
        var s = HoldsModel(Doc.Sel) ? Doc.Sel : new Selection(SelKind.Body);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "A model for " + s.Label, AllowMultiple = false, FileTypeFilter = new[] { ObjFiles, FilePickerFileTypes.All }
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is { } path) ImportObj(s, path);
    }

    /// <summary>The .obj files brought in, each with the model file it became. Saving the .obj brings it in again</summary>
    readonly Dictionary<string, (string Rel, FileSystemWatcher Watch)> objLinks = new(StringComparer.OrdinalIgnoreCase);
    DispatcherTimer? objTimer;
    readonly HashSet<string> objChanged = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, (long, DateTime)> objStamps = new(StringComparer.OrdinalIgnoreCase);
    public int ObjReloads { get; private set; }
    public IReadOnlyCollection<string> ObjLinks => objLinks.Keys;

    /// <summary>
    /// Bring in an .obj as the part's model: it's written as a .prm in the car's folder, named after the .obj, and the
    /// part uses it. The .obj stays linked: each time it's saved, the model is read again.
    /// </summary>
    public bool ImportObj(Selection s, string objPath)
    {
        if (Doc.Car is not { } car) return false;
        Prm m;
        ObjModel.ImportReport rep;
        try { m = ObjModel.Import(objPath, out rep); }
        catch (Exception ex) { Say("Can't read the .obj: " + ex.Message); return false; }
        if (m.Faces.Length == 0) { Say("No polygons in " + Names.File(objPath)); return false; }
        var name = string.Concat(Names.Stem(objPath).Select(c => char.IsLetterOrDigit(c) || c is '_' or '-' ? c : '_'));
        // Never over another part's model: a file of that name that isn't this part's own gets a new name (name_2...)
        var own = Doc.ModelsOf(s).Select(x => Doc.ModelName(x)).Where(x => x != null).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rel = $"cars\\{car.Folder}\\{name}.prm";
        for (int k = 2; !own.Contains(rel) && car.Game.Find(rel) != null; k++) rel = $"cars\\{car.Folder}\\{name}_{k}.prm";
        name = Names.Stem(rel);
        if (!WriteModel(rel, m)) return false;
        Doc.SetPartModel(s, rel);
        Doc.Select(s);
        Link(objPath, rel);
        var notes = new List<string>();
        if (rep.SplitPolygons > 0) notes.Add($"{rep.SplitPolygons} polygons over 4 corners made into triangles");
        if (rep.Skipped > 0) notes.Add($"{rep.Skipped} left out");
        if (!rep.HadUvs) notes.Add("no UVs (the UV editor's Unwrap can make them)");
        if (!rep.HadColours) notes.Add("white: Tools > Auto shade lights it");
        Say($"{s.Label}: {name}.prm, {rep.Polygons} polygons. Saving the .obj brings it in again" + (notes.Count > 0 ? ". " + string.Join(", ", notes) : ""));
        Did("Import");
        return true;
    }

    bool WriteModel(string rel, Prm m)
    {
        try
        {
            var target = Doc.Car!.Game.WritePath(rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            Files.Backup(target);
            m.Save(target);
            return true;
        }
        catch (Exception ex) { Say("Can't write the model: " + ex.Message); return false; }
    }

    void Link(string objPath, string rel)
    {
        objPath = Path.GetFullPath(objPath);
        if (objLinks.Remove(objPath, out var old)) old.Watch.Dispose();
        var w = new FileSystemWatcher(Path.GetDirectoryName(objPath)!, Names.File(objPath))
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName
        };
        void Changed(object? _, FileSystemEventArgs e) => Dispatcher.UIThread.Post(() =>
        {
            objChanged.Add(objPath);
            // Programs write in pieces, or write elsewhere and rename: wait until it's quiet
            objTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(500), DispatcherPriority.Background, (_, _) => ReloadLinked());
            objTimer.Stop();
            objTimer.Start();
        });
        w.Changed += Changed;
        w.Created += Changed;
        w.Renamed += (o, e) => Changed(o, e);
        w.EnableRaisingEvents = true;
        objLinks[objPath] = (rel, w);
    }

    void ReloadLinked()
    {
        objTimer?.Stop();
        foreach (var path in objChanged.ToList())
        {
            if (!objLinks.TryGetValue(path, out var link) || Doc.Car == null) { objChanged.Remove(path); continue; }
            // Still being written? The size and time must stay the same over one wait, or a half file would become the model
            try
            {
                var fi = new FileInfo(path);
                var stamp = (fi.Length, fi.LastWriteTimeUtc);
                if (!objStamps.TryGetValue(path, out var was) || was != stamp) { objStamps[path] = stamp; objTimer?.Start(); continue; }
            }
            catch (IOException) { objTimer?.Start(); continue; }
            objStamps.Remove(path);
            Prm m;
            try { m = ObjModel.Import(path); }
            catch (IOException) { objTimer?.Start(); continue; } // still being written
            catch (Exception ex) { objChanged.Remove(path); Say("Can't read " + Names.File(path) + ": " + ex.Message); continue; }
            objChanged.Remove(path);
            if (m.Faces.Length == 0) continue;
            if (!WriteModel(link.Rel, m)) continue;
            Doc.ReloadModel(link.Rel);
            ObjReloads++;
            Say($"{Names.File(path)} changed: {Names.File(link.Rel)} read again, {m.Faces.Length} polygons");
        }
    }

    void DropObjLinks()
    {
        foreach (var l in objLinks.Values) l.Watch.Dispose();
        objLinks.Clear();
    }

    public CompareWindow? Compare { get; private set; }

    public void OpenCompare(string? with = null)
    {
        if (Doc.Car == null) return;
        if (Compare is { IsVisible: true }) { Compare.Activate(); if (with != null) Compare.Choose(with); return; }
        Compare = new CompareWindow(this);
        Compare.Closed += (_, _) => Compare = null;
        Compare.Show(this);
        if (with != null) Compare.Choose(with);
    }
}

/// <summary>This car beside another: every value that differs, ticks to take the other car's</summary>
public sealed class CompareWindow : Window
{
    readonly MainWindow main;
    readonly ComboBox other = new() { MinWidth = 260, FontSize = 12 };
    readonly ComboBox group = new() { MinWidth = 160, FontSize = 12 };
    readonly StackPanel rowsPanel = new() { Spacing = 0 };
    readonly TextBlock status = new() { FontSize = 12, Foreground = Look.Faint, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
    readonly List<CarEntry> cars;
    Car? theirs;
    public List<CarCompare.Row> Rows { get; private set; } = new();
    public readonly HashSet<CarCompare.Row> Ticked = new();

    public CompareWindow(MainWindow main)
    {
        this.main = main;
        Title = "Compare with another car";
        Width = 760; Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Look.Panel;
        Foreground = Look.Text;

        var game = main.Doc.Game!;
        cars = game.ListCars().Where(c => !c.Folder.Equals(main.Doc.Car!.Folder, StringComparison.OrdinalIgnoreCase)).OrderBy(c => c.Name).ToList();
        other.ItemsSource = cars.Select(c => $"{c.Name}  ({c.Folder})").ToList();
        other.SelectionChanged += (_, _) => Load();
        group.ItemsSource = new[] { "Every value" }.Concat(CarCompare.Groups.All).ToList();
        group.SelectedIndex = 0;
        group.SelectionChanged += (_, _) => Fill();

        var tickGroup = new Button { Content = "Tick what's shown", FontSize = 12 };
        tickGroup.Click += (_, _) => { foreach (var r in Shown()) Ticked.Add(r); Fill(); };
        var untick = new Button { Content = "Untick all", FontSize = 12 };
        untick.Click += (_, _) => { Ticked.Clear(); Fill(); };
        var take = Look.Primary(new Button { Content = "Take the ticked values", FontSize = 12 });
        take.Click += (_, _) => TakeTicked();
        var close = new Button { Content = "Close", FontSize = 12, IsCancel = true };
        close.Click += (_, _) => Close();

        var top = new WrapPanel { Margin = new Thickness(12, 12, 12, 6) };
        foreach (var c in new Control[] { Text("Compare with"), other, Text("Show"), group, tickGroup, untick })
        {
            c.Margin = new Thickness(0, 0, 8, 4);
            top.Children.Add(c);
        }
        var head = Row("", "Value", "This car", "The other car", true);
        head.Margin = new Thickness(12, 0, 12, 2);
        var bottom = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { take, close } };
        DockPanel.SetDock(buttons, Dock.Right);
        bottom.Children.Add(buttons);
        bottom.Children.Add(status);

        var dock = new DockPanel();
        foreach (var (c, d) in new (Control, Dock)[] { (top, Dock.Top), (head, Dock.Top), (bottom, Dock.Bottom) })
        {
            DockPanel.SetDock(c, d);
            dock.Children.Add(c);
        }
        dock.Children.Add(new ScrollViewer { Content = rowsPanel, Margin = new Thickness(12, 0), AllowAutoHide = false });
        Content = dock;
        main.Doc.Changed += Refresh;
        Closed += (_, _) => main.Doc.Changed -= Refresh;
        status.Text = "Choose a car: every value that differs shows, grouped. Tick the ones to take over, then Take. Undo takes it back.";
    }

    static TextBlock Text(string s) => new() { Text = s, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };

    static Grid Row(string groupName, string label, string mine, string theirs, bool head = false)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("28,130,170,*,*"), MinHeight = 22 };
        void Cell(string t, int col, IBrush? brush = null)
        {
            var tb = new TextBlock
            {
                Text = t, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = brush ?? (head ? Look.Faint : Look.Text), Margin = new Thickness(0, 0, 8, 0)
            };
            ToolTip.SetTip(tb, t);
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }
        Cell(groupName, 1, Look.Faint);
        Cell(label, 2);
        Cell(mine, 3);
        Cell(theirs, 4);
        return g;
    }

    /// <summary>Compare with this car, by folder</summary>
    public bool Choose(string folder)
    {
        int i = cars.FindIndex(c => c.Folder.Equals(folder, StringComparison.OrdinalIgnoreCase));
        if (i < 0) return false;
        other.SelectedIndex = i;
        return true;
    }

    void Load()
    {
        if (other.SelectedIndex < 0) return;
        try { theirs = new Car(main.Doc.Game!, cars[other.SelectedIndex].Folder); }
        catch (Exception ex) { status.Text = "Can't read that car: " + ex.Message; theirs = null; }
        Ticked.Clear();
        Refresh();
    }

    void Refresh()
    {
        if (theirs == null || main.Doc.D == null) return;
        var keep = Ticked.Select(r => r.Block + "|" + r.Key).ToHashSet();
        Rows = CarCompare.Diff(main.Doc.D, theirs.Data);
        Ticked.Clear();
        foreach (var r in Rows) if (keep.Contains(r.Block + "|" + r.Key)) Ticked.Add(r);
        Fill();
    }

    IEnumerable<CarCompare.Row> Shown() =>
        group.SelectedIndex <= 0 ? Rows : Rows.Where(r => r.Group == CarCompare.Groups.All[group.SelectedIndex - 1]);

    /// <summary>Show only this group's rows (null: all)</summary>
    public void ShowGroup(string? g) => group.SelectedIndex = g == null ? 0 : Array.IndexOf(CarCompare.Groups.All, g) + 1;

    public void TickShown() { foreach (var r in Shown()) Ticked.Add(r); Fill(); }

    void Fill()
    {
        rowsPanel.Children.Clear();
        var shown = Shown().OrderBy(r => Array.IndexOf(CarCompare.Groups.All, r.Group)).ToList();
        foreach (var r in shown)
        {
            var g = Row(r.Group, r.Label, r.Mine == "" ? "(none)" : r.Mine, r.Theirs == "" ? "(none)" : r.Theirs);
            var box = new CheckBox { IsChecked = Ticked.Contains(r), MinWidth = 0, MinHeight = 0, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center };
            var row = r;
            box.IsCheckedChanged += (_, _) => { if (box.IsChecked == true) Ticked.Add(row); else Ticked.Remove(row); Count(); };
            g.Children.Add(box);
            rowsPanel.Children.Add(g);
        }
        if (theirs != null) Count();
    }

    void Count()
    {
        if (theirs == null) return;
        status.Text = Rows.Count == 0
            ? "Every value is the same as " + theirs.Data.Name
            : $"{Rows.Count} values differ from {theirs.Data.Name}, {Shown().Count()} shown, {Ticked.Count} ticked";
    }

    /// <summary>Put the ticked values into this car, as one step undo takes back</summary>
    public int TakeTicked()
    {
        var doc = main.Doc;
        if (theirs == null || doc.Car is not { } car || Ticked.Count == 0) { status.Text = "Tick the values to take first"; return 0; }
        var rows = Ticked.ToList();
        var data = CarCompare.Take(car.PreviewText(), rows, GameFolder.IsStock(car.Folder));
        doc.ReplaceData("Values from " + theirs.Data.Name, data);
        int n = rows.Count;
        main.Say($"Took {n} values from {theirs.Data.Name}. Undo takes them back");
        Ticked.Clear();
        Refresh();
        return n;
    }
}
