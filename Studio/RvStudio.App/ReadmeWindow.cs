using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// The readme of the open car: a few answers on the left, the readme as it will be on the right, changing as you type.
/// Write it, or (when packing) write it and go on to the zip. Your name, e-mail and site are remembered for every car,
/// the rest for this car.
/// </summary>
public sealed class ReadmeWindow : Window
{
    public enum Outcome { Closed, Written, Pack, PackWithout }
    public Outcome Result { get; private set; } = Outcome.Closed;
    /// <summary>A readme was written (the car's details show its Read button)</summary>
    public static event Action? Written;
    public string? WrittenPath { get; private set; }

    readonly Car car;
    readonly Hull? hull;
    readonly TextBox author = Box("Your name or nickname"), email = Box("E-mail (optional)"), site = Box("Website (optional)");
    readonly TextBox version = Box("1.0"), basedOn = Box("The car it started from, and its maker"), thanks = Box("People and tools to thank (optional)");
    readonly TextBox description = new() { Watermark = "A few words about the car: what it is, what inspired it, how it drives", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, Height = 96, FontSize = 12 };
    readonly ComboBox type = new() { ItemsSource = Readme.KindNames, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly ComboBox permission = new() { ItemsSource = Readme.PermissionNames, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    // The readme's look: RV Car Studio 3's, or one of the templates in data/readme (as the old Car Studio had them)
    readonly ComboBox layout = new() { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    readonly List<string> templates = Directory.Exists(Path.Combine(AppContext.BaseDirectory, "data", "readme"))
        ? Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "data", "readme"), "*.txt").OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList() : new();
    readonly TextBox preview = new() { IsReadOnly = true, FontFamily = new FontFamily("Consolas, Cascadia Mono, Courier New, monospace"), FontSize = 12, TextWrapping = TextWrapping.NoWrap, AcceptsReturn = true };
    readonly TextBlock note = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Look.Faint, VerticalAlignment = VerticalAlignment.Center };
    readonly DispatcherTimer redo;
    string text = "";

    static TextBox Box(string hint) => new() { Watermark = hint, FontSize = 12 };

    public ReadmeWindow(Car car, Hull? hull, bool packing)
    {
        this.car = car;
        this.hull = hull;
        Title = "Readme - " + (car.Data.Name.Trim() == "" ? car.Folder : car.Data.Name.Trim());
        Width = 1120; Height = 720; MinWidth = 820; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Look.Panel;
        Foreground = Look.Text;

        Load();

        var form = new StackPanel { Spacing = 4, Margin = new Thickness(14, 12, 10, 12) };
        void Row(string label, Control c, string? tip = null)
        {
            form.Children.Add(new TextBlock { Text = label, FontSize = 12, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 6, 0, 0) });
            if (tip != null) ToolTip.SetTip(c, tip);
            form.Children.Add(c);
        }
        form.Children.Add(new TextBlock { Text = "The readme travels with your car: players see who made it, how to install it and what they may do with it. The numbers come from the car itself.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Look.Faint });
        layout.ItemsSource = new[] { "RV Car Studio 3 (recommended)" }.Concat(templates.Select(f => "Template: " + Names.Stem(f))).ToList();
        layout.SelectedIndex = 0;
        Row("Layout", layout, "RV Car Studio 3's readme, or a template from data/readme (the old Car Studio's, or your own with its [CARNAME], [AUTHOR]... tags)");
        Row("Made by", author);
        Row("Contact", email);
        form.Children.Add(site);
        Row("About the car", description);
        Row("What it is", type, "Guessed from the car: a stock body means a repaint, a stock folder a changed stock car");
        Row("Based on", basedOn, "For a remodel or a repaint: the car it started from. Credit its maker too");
        Row("Thanks", thanks);
        Row("Others may", permission);
        Row("Version", version, "Raise it when you share a new version: 1.1, 2.0...");

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
        Button B(string t, Action a, bool primary = false)
        {
            var b = new Button { Content = t, FontSize = 12, MinHeight = 30, Padding = new Thickness(12, 4) };
            if (primary) Look.Primary(b);
            b.Click += (_, _) => a();
            buttons.Children.Add(b);
            return b;
        }
        if (packing)
        {
            B("Write the readme and pack", () => { if (Write()) { Result = Outcome.Pack; Close(); } }, true);
            B("Pack without it", () => { Save(); Result = Outcome.PackWithout; Close(); });
            B("Cancel", Close);
        }
        else
        {
            B("Write readme.txt", () => { if (Write()) { Result = Outcome.Written; Close(); } }, true);
            B("Write it and pack as zip...", () => { if (Write()) { Result = Outcome.Pack; Close(); } });
            B("Close", () => { Save(); Close(); });
        }

        var bottom = new DockPanel { Margin = new Thickness(12, 8), LastChildFill = true };
        DockPanel.SetDock(buttons, Dock.Right);
        bottom.Children.Add(buttons);
        bottom.Children.Add(note);

        var left = new ScrollViewer { Content = form, Width = 380 };
        var right = new DockPanel { Margin = new Thickness(4, 12, 14, 4) };
        var ph = new TextBlock { Text = "readme.txt, as it will be written", FontSize = 12, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 0, 0, 4) };
        DockPanel.SetDock(ph, Dock.Top);
        right.Children.Add(ph);
        right.Children.Add(preview);

        var root = new DockPanel();
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);
        DockPanel.SetDock(left, Dock.Left);
        root.Children.Add(left);
        root.Children.Add(right);
        Content = root;

        // Typing changes the preview a moment later (the top speed is a simulated run: not on every key)
        redo = new DispatcherTimer(TimeSpan.FromMilliseconds(250), DispatcherPriority.Background, (_, _) => { redoTimerStop(); Refresh(); });
        void Later() { redo.Stop(); redo.Start(); }
        foreach (var t in new[] { author, email, site, version, basedOn, thanks, description }) t.TextChanged += (_, _) => Later();
        type.SelectionChanged += (_, _) => Later();
        layout.SelectionChanged += (_, _) => Later();
        permission.SelectionChanged += (_, _) => Later();
        Refresh();

        var target = Target();
        note.Text = File.Exists(target)
            ? (Readme.IsOurs(target) ? "Writes over the readme made before." : "The car already has its own readme: it's kept as readme.txt.bak.")
            : "The car has no readme yet.";
    }

    void redoTimerStop() => redo.Stop();

    public Readme.Info Info() => new(
        author.Text ?? "", email.Text ?? "", site.Text ?? "", description.Text ?? "",
        (Readme.Kind)Math.Max(0, type.SelectedIndex), basedOn.Text ?? "", thanks.Text ?? "", Math.Max(0, permission.SelectedIndex), version.Text ?? "1.0");

    public string Text => text;

    /// <summary>The layout: 0 RV Car Studio 3's, 1.. the templates (for the tests)</summary>
    public int Layouts => templates.Count + 1;
    public void UseLayout(int i) { layout.SelectedIndex = i; Refresh(); }

    void Refresh()
    {
        var i = Info();
        if (layout.SelectedIndex > 0 && layout.SelectedIndex - 1 < templates.Count)
        {
            try
            {
                var tpl = File.ReadAllText(templates[layout.SelectedIndex - 1], ParamFile.FileEncoding);
                text = CarTools.FillReadme(tpl, car, new CarTools.ReadmeInfo(i.Author, i.Email, i.Site, i.Description, i.Thanks)) + (tpl.Contains("RV Car Studio", StringComparison.OrdinalIgnoreCase) ? "" : Environment.NewLine + Environment.NewLine + "Made with RV Car Studio 3" + Environment.NewLine);
                preview.Text = text;
                return;
            }
            catch (Exception ex) { preview.Text = "Can't use that template: " + ex.Message; }
        }
        text = Readme.Make(car, hull, i);
        preview.Text = text;
    }

    string Target() => car.Game.WritePath("cars\\" + car.Folder + "\\readme.txt");

    /// <summary>Writes readme.txt into the car's folder (a readme someone else wrote stays as readme.txt.bak)</summary>
    public bool Write()
    {
        Save();
        Refresh();
        var file = Target();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            if (File.Exists(file) && !Readme.IsOurs(file)) File.Copy(file, file + ".bak", true);
            File.WriteAllText(file, text, ParamFile.FileEncoding);
            WrittenPath = file;
            Written?.Invoke();
            return true;
        }
        catch (Exception ex) { note.Text = "Couldn't write the readme: " + ex.Message; return false; }
    }

    // ---------------------------- remembered answers ----------------------------

    sealed record Kept(string Description, int Type, string BasedOn, string Thanks, int Permission, string Version);

    string Key => "readme:" + car.Folder.ToLowerInvariant();

    void Load()
    {
        author.Text = Settings.Author;
        email.Text = Settings.Email;
        site.Text = Settings.Site;
        Kept? k = null;
        try { if (Settings.Get(Key) is { } j) k = JsonSerializer.Deserialize<Kept>(j); } catch (Exception) { }
        if (k != null)
        {
            description.Text = k.Description; type.SelectedIndex = k.Type; basedOn.Text = k.BasedOn;
            thanks.Text = k.Thanks; permission.SelectedIndex = k.Permission; version.Text = k.Version;
        }
        else
        {
            var (kind, from) = Readme.Guess(car);
            type.SelectedIndex = (int)kind;
            basedOn.Text = from;
            permission.SelectedIndex = 0;
            version.Text = "1.0";
        }
    }

    void Save()
    {
        Settings.Author = author.Text ?? "";
        Settings.Email = email.Text ?? "";
        Settings.Site = site.Text ?? "";
        Settings.Put(Key, JsonSerializer.Serialize(new Kept(description.Text ?? "", Math.Max(0, type.SelectedIndex), basedOn.Text ?? "", thanks.Text ?? "", Math.Max(0, permission.SelectedIndex), version.Text ?? "1.0")));
    }

    /// <summary>For the tests: fill the answers</summary>
    public void Fill(string who, string about, Readme.Kind kind, string from = "")
    {
        author.Text = who; description.Text = about; type.SelectedIndex = (int)kind; basedOn.Text = from;
        Refresh();
    }
}
