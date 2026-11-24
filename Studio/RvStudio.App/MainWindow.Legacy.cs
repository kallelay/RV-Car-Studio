using System.Numerics;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// What the old VB.NET RV Car Studio had and the new one didn't yet, brought over: the car's details and readme beside
/// its name, a random car, centre of mass presets and the weapon's place, centring a model, the menu card as the game
/// draws it, the car read again when parameters.txt changes on disk, saving by itself, and a list of the messages.
/// </summary>
public sealed partial class MainWindow
{
    // ---------------------------- the open car's details, under its name ----------------------------

    readonly TextBlock carDetails = new() { FontSize = 11.5, Foreground = Page.Faint, TextWrapping = TextWrapping.Wrap };
    Button? readmeButton;

    /// <summary>Who made the car: Acclaim for the game's own, else from its readme ("Author:", "Made by"...), or empty</summary>
    public static string MadeBy(Car c)
    {
        if (GameFolder.IsStock(c.Folder)) return "Acclaim (stock car)";
        if (ReadmeOf(c) is not { } f) return "";
        try
        {
            foreach (var line in File.ReadLines(f, ParamFile.FileEncoding).Take(80))
            {
                var m = Regex.Match(line, @"(?i)\b(author|made by|created by|car by|creator|model by)\b\s*[:=\-]?\s*([^\r\n+|]{2,40})");
                if (m.Success)
                {
                    var who = m.Groups[2].Value.Trim(' ', '.', ':', '-', '+', '*', '=', '|');
                    if (who.Length >= 2 && !who.StartsWith("[")) return who;
                }
            }
        }
        catch (Exception) { }
        return "";
    }

    /// <summary>The car's readme: readme.txt, else any .txt with "read" in its name, one that isn't the original's first</summary>
    public static string? ReadmeOf(Car c)
    {
        var files = c.Game.CarFiles(c.Folder).Values.Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) && Names.File(f).Contains("read", StringComparison.OrdinalIgnoreCase)).ToList();
        return files.FirstOrDefault(f => !Names.File(f).Contains("original", StringComparison.OrdinalIgnoreCase)) ?? files.FirstOrDefault();
    }

    void ShowCarDetails()
    {
        if (Doc.Car is not { } c || Doc.D is not { } d) { carDetails.Text = ""; return; }
        var parts = new List<string>();
        var who = MadeBy(c);
        if (who != "") parts.Add("Made by " + who);
        parts.Add((d.Class is >= 0 and < 3 ? CarTools.ClassNames[d.Class] : "Special") + ", " + (d.Rating is >= 0 and < 6 ? CarTools.RatingNames[d.Rating] : "?"));
        try { parts.Add($"{CarCalc.ToMph(CarCalc.Of(d, Doc.Hull).TopSpeedReal):0} mph"); } catch (Exception) { }
        int polys = new[] { d.BodyModel }.Concat(d.Wheels.Where(w => w.Present).Select(w => w.Model)).Distinct().Sum(s => Doc.Model(s)?.Faces.Length ?? 0);
        parts.Add(polys + " polygons");
        carDetails.Text = string.Join("  ·  ", parts);
        if (readmeButton != null) readmeButton.IsVisible = ReadmeOf(c) != null;
    }

    /// <summary>The car's readme, to read</summary>
    public async Task ViewReadme()
    {
        if (Doc.Car is not { } c || ReadmeOf(c) is not { } f) { Say("This car has no readme"); return; }
        string text;
        try { text = File.ReadAllText(f, ParamFile.FileEncoding); } catch (Exception ex) { Say("Can't read the readme: " + ex.Message); return; }
        var box = new TextBox
        {
            Text = text, IsReadOnly = true, AcceptsReturn = true, TextWrapping = TextWrapping.NoWrap, FontSize = 12,
            FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New, monospace")
        };
        var w = new Window { Title = Names.File(f) + " - " + c.Data.Name, Width = 720, Height = 640, Content = box, Background = Look.Panel, Foreground = Look.Text, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        await w.ShowDialog(this);
    }

    /// <summary>Any car of the list as it's filtered now</summary>
    public void RandomCar()
    {
        var items = carList.Items.OfType<ListBoxItem>().ToList();
        if (items.Count == 0) return;
        carList.SelectedItem = items[Random.Shared.Next(items.Count)];
    }

    // ---------------------------- centre of mass and weapon, worked out ----------------------------

    public static readonly string[] ComPresets =
    {
        "In the middle of the wheels", "Balanced front and back (50/50)", "From the body and wheels, by their weight", "Low and in the middle (stable)"
    };

    /// <summary>The centre of mass from the car's parts, as the old Body mode's buttons did (cleaned up)</summary>
    public void SetComPreset(int which)
    {
        if (Doc.D is not { } d) return;
        var ws = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Present).ToList();
        if (ws.Count == 0) { Say("The car has no wheels to work it out from"); return; }
        Vector3 mid = ws.Aggregate(Vector3.Zero, (a, i) => a + d.Wheels[i].Offset1) / ws.Count;
        var front = ws.Where(i => i < 2).ToList();
        var rear = ws.Where(i => i >= 2).ToList();
        float zFront = front.Count > 0 ? front.Average(i => d.Wheels[i].Offset1.Z) : mid.Z, zRear = rear.Count > 0 ? rear.Average(i => d.Wheels[i].Offset1.Z) : mid.Z;
        var com = d.CoM;
        switch (which)
        {
            case 0: com = new Vector3(0, mid.Y, mid.Z); break;
            case 1: com = new Vector3(com.X, com.Y, (zFront + zRear) / 2); break;
            case 2:
                {
                    // Each part's centre, weighted by its mass
                    var body = Doc.Model(d.BodyModel);
                    var bc = body != null ? (body.Min + body.Max) / 2 + d.BodyOffset : Vector3.Zero;
                    double total = d.BodyMass;
                    Vector3 sum = bc * (float)d.BodyMass;
                    foreach (var i in ws) { sum += d.Wheels[i].Offset1 * (float)d.Wheels[i].Mass; total += d.Wheels[i].Mass; }
                    com = total > 0 ? sum / (float)total : bc;
                    com.X = 0;
                    break;
                }
            case 3: com = new Vector3(0, mid.Y + (float)ws.Average(i => d.Wheels[i].Radius) * 0.3f, (zFront + zRear) / 2); break;
        }
        Doc.Edit("Centre of mass: " + ComPresets[which], () => d.CoM = com);
        Doc.EndGesture();
        Say($"Centre of mass {ComPresets[which].ToLowerInvariant()}: {com.X:0.0} {com.Y:0.0} {com.Z:0.0}. Undo takes it back");
    }

    /// <summary>Where weapons start: at the front of the body, near its top, as the stock cars have it</summary>
    public void WeaponAuto()
    {
        if (Doc.D is not { } d || Doc.Model(d.BodyModel) is not { } b) { Say("The car has no body to place it on"); return; }
        var at = new Vector3(0, b.Min.Y + (b.Max.Y - b.Min.Y) * 0.12f, b.Max.Z) + d.BodyOffset;
        Doc.Edit("Weapon place", () => d.Weapon = at);
        Doc.EndGesture();
        Say($"Weapons start at the front, near the top: {at.X:0} {at.Y:0} {at.Z:0}");
    }

    // ---------------------------- centring a model ----------------------------

    /// <summary>
    /// A part's model moved so its middle is on its origin: sideways and lengthways (heightways too when asked), as the
    /// old body helper's Center XZ / Center All. The model changes (undo takes it back, Save writes it)
    /// </summary>
    public void CentreModel(Selection s, bool height = false)
    {
        var models = Doc.ModelsOf(s);
        if (models.Count == 0) { Say(s.Label + " has no model"); return; }
        var m = models[0];
        var c = (m.Min + m.Max) / 2;
        var shift = new Vector3(-c.X, height ? -c.Y : 0, -c.Z);
        if (shift.LengthSquared() < 1e-6f) { Say(s.Label + " is already centred"); return; }
        Doc.EditModels("Centre " + s.Label, models, () =>
        {
            foreach (var x in models)
            {
                var nv = (PrmVertex[])x.Verts.Clone();
                for (int i = 0; i < nv.Length; i++) nv[i].Pos += shift;
                x.Verts = nv;
                x.ComputeBounds();
            }
        });
        Say($"{s.Label} centred: moved {shift.X:0.0} {shift.Y:0.0} {shift.Z:0.0}. Save writes the model");
    }

    // ---------------------------- the menu card, as the game draws it ----------------------------

    // The game's ranges for the three bars
    const float TopEndMin = 26, TopEndMax = 42, AccMin = 4, AccMax = 12, WeightMin = 0.6f, WeightMax = 2.8f;

    /// <summary>Name, class, rating, drive and the speed, acceleration and weight bars, as on the car selection screen</summary>
    Control FrontendCard(Page p)
    {
        var name = new TextBlock { FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Brushes.White };
        var info = new TextBlock { FontSize = 11, Foreground = new SolidColorBrush(Color.FromRgb(200, 210, 230)) };
        Border Bar(out Border fill)
        {
            fill = new Border { Background = new SolidColorBrush(Color.FromRgb(255, 190, 40)), HorizontalAlignment = HorizontalAlignment.Left, Height = 10, CornerRadius = new CornerRadius(2) };
            return new Border { Background = new SolidColorBrush(Color.FromRgb(40, 50, 80)), Height = 10, Width = 180, CornerRadius = new CornerRadius(2), Child = fill, HorizontalAlignment = HorizontalAlignment.Left };
        }
        var speed = Bar(out var sf); var acc = Bar(out var af); var weight = Bar(out var wf);
        var noBars = new TextBlock { Text = "No stat bars (Show stat bars is off)", FontSize = 11, Foreground = Brushes.White, IsVisible = false };
        Grid Row(string label, Control bar)
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("80,*"), Margin = new Thickness(0, 2) };
            g.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = Brushes.White, VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(bar, 1);
            g.Children.Add(bar);
            return g;
        }
        var rows = new StackPanel { Children = { Row("Speed", speed), Row("Acceleration", acc), Row("Weight", weight) } };
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(16, 24, 48)), CornerRadius = new CornerRadius(8), Padding = new Thickness(12, 10), Margin = new Thickness(0, 4, 0, 6),
            HorizontalAlignment = HorizontalAlignment.Left, MinWidth = 300,
            Child = new StackPanel { Spacing = 3, Children = { name, info, rows, noBars } }
        };
        ToolTip.SetTip(card, "As the car selection screen draws it, from the values below (Fill the stat bars from a simulated run sets them)");
        p.Live(() =>
        {
            if (Doc.D is not { } d) return;
            name.Text = d.Name;
            var powered = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Present && d.Wheels[i].Powered).ToArray();
            string drive = powered.Length == 4 ? "4WD" : powered.All(i => i < 2) ? "FWD" : "RWD";
            info.Text = (d.Class is >= 0 and < 3 ? CarTools.ClassNames[d.Class] : "Special") + "  ·  " + (d.Rating is >= 0 and < 6 ? CarTools.RatingNames[d.Rating] : "?") + "  ·  " + drive;
            double F(double v, double lo, double hi) => Math.Clamp((v - lo) / (hi - lo), 0, 1);
            sf.Width = 180 * F(CarCalc.ToMph((float)d.TopEnd), TopEndMin, TopEndMax);
            af.Width = 180 * F(AccMax - d.Acc, 0, AccMax - AccMin);
            wf.Width = 180 * F(d.Weight, WeightMin, WeightMax);
            rows.IsVisible = d.Statistics;
            noBars.IsVisible = !d.Statistics;
        });
        return card;
    }

    // ---------------------------- parameters.txt changed on disk, saving by itself ----------------------------

    FileSystemWatcher? paramWatcher;
    DateTime ownWrite;

    /// <summary>Watches the open car's parameters.txt: an edit in another program comes in (asked first when you have unsaved changes here)</summary>
    void WatchParameters()
    {
        paramWatcher?.Dispose();
        paramWatcher = null;
        if (Doc.Car is not { } c || !File.Exists(c.ParamPath)) return;
        var path = c.ParamPath;
        paramWatcher = new FileSystemWatcher(Path.GetDirectoryName(path)!, Names.File(path)) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size };
        paramWatcher.Changed += (_, _) => Dispatcher.UIThread.Post(async () =>
        {
            // Our own Save is not an outside edit
            if ((DateTime.UtcNow - ownWrite).TotalSeconds < 3 || Doc.Car == null || !Doc.Car.ParamPath.Equals(path, StringComparison.OrdinalIgnoreCase)) return;
            await Task.Delay(300);
            if (Doc.Modified && await Dialogs.Ask(this, "parameters.txt was changed in another program. Read it again? Your unsaved changes here would go.", "Read it again", "Keep mine") != 0) return;
            PickCar(Doc.Car.Folder);
            Say("parameters.txt changed on disk: read again");
        });
        paramWatcher.EnableRaisingEvents = true;
    }

    /// <summary>Called by Save: what's written now isn't an outside edit</summary>
    void NoteOwnWrite() => ownWrite = DateTime.UtcNow;

    public bool AutoSave { get; private set; } = Settings.Get("autosave") == "1";
    DispatcherTimer? autoSaveTimer;

    /// <summary>Save by itself, a few seconds after each change (as the old Studio's Auto save)</summary>
    public void SetAutoSave(bool on)
    {
        AutoSave = on;
        easyAutoSave.IsChecked = on;
        if (!AutoScenario.Running) Settings.Put("autosave", on ? "1" : "0");
        autoSaveTimer ??= new DispatcherTimer(TimeSpan.FromSeconds(5), DispatcherPriority.Background, (_, _) =>
        {
            if (AutoSave && Doc.Car != null && Doc.Modified && Doc.Sim == null && !View.Entering) { Save(); Say("Saved by itself at " + DateTime.Now.ToString("HH:mm")); }
        });
        if (on) autoSaveTimer.Start(); else autoSaveTimer.Stop();
        Say(on ? "Saves by itself a few seconds after each change" : "Saves only when you ask");
    }

    /// <summary>-load from the command line: the car with that folder or name</summary>
    public void LoadFromCommandLine(string which)
    {
        var c = allCars.FirstOrDefault(x => x.Folder.Equals(which, StringComparison.OrdinalIgnoreCase))
            ?? allCars.FirstOrDefault(x => x.Name.Equals(which, StringComparison.OrdinalIgnoreCase))
            ?? allCars.FirstOrDefault(x => x.Name.Contains(which, StringComparison.OrdinalIgnoreCase));
        if (c == null) { Say("No car " + which + " in this game"); search.Text = which; return; }
        PickCar(c.Folder);
        Say("Opened " + c.Name + " (from the command line)");
    }

    // ---------------------------- the messages so far ----------------------------

    readonly List<string> messages = new();

    void Remember(string s)
    {
        if (string.IsNullOrWhiteSpace(s) || (messages.Count > 0 && messages[^1].EndsWith(s))) return;
        messages.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + s);
        if (messages.Count > 500) messages.RemoveAt(0);
    }

    public IReadOnlyList<string> Messages => messages;
    public string CarDetailsText => carDetails.Text ?? "";
    public bool ReadmeButtonShown => readmeButton?.IsVisible == true;

    /// <summary>Every message of the status line so far, newest last (the old Studio's console)</summary>
    public async Task ShowMessages()
    {
        var box = new TextBox { Text = string.Join(Environment.NewLine, messages), IsReadOnly = true, AcceptsReturn = true, FontSize = 12, TextWrapping = TextWrapping.Wrap };
        var w = new Window { Title = "Messages", Width = 760, Height = 520, Content = box, Background = Look.Panel, Foreground = Look.Text, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        w.Opened += (_, _) => { box.CaretIndex = box.Text?.Length ?? 0; };
        await w.ShowDialog(this);
    }
}
