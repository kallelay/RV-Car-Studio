using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace RvStudio.App;

public static class Program
{
    public static string[] Args = Array.Empty<string>();

    [STAThread]
    public static void Main(string[] args)
    {
        Args = args;
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<StudioApp>()
        .UsePlatformDetect()
        .WithInterFont()
        .LogToTrace();
}

public sealed class StudioApp : Application
{
    public override void Initialize()
    {
        RequestedThemeVariant = ThemeVariant.Dark;
        // Fluent's own styles come with the look: Look.Apply puts them first
        foreach (var s in Look.Styles()) Styles.Add(s);
        Look.Resources(Resources);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime d)
        {
            string? path = null;
            string? auto = null, look = null;
            var a = Program.Args;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == "-rvpath" && i + 1 < a.Length) path = a[++i];
                else if (a[i] == "--auto" && i + 1 < a.Length) auto = a[++i];
                else if (a[i] == "--look" && i + 1 < a.Length) look = a[++i];
            }
            AutoScenario.Running = auto != null || look != null;
            // A test run starts as it always looked, whatever you chose
            Look.Apply(AutoScenario.Running ? ThemeKind.Classic : Settings.Theme);
            var w = new MainWindow(path);
            d.MainWindow = w;
            if (auto != null) w.Opened += async (_, _) => await AutoScenario.Run(w, auto);
            if (look != null) w.Opened += async (_, _) => await AutoScenario.RunLook(w, look);
        }
        base.OnFrameworkInitializationCompleted();
    }
}

/// <summary>Small things to remember between runs</summary>
public static class Settings
{
    static readonly string File_ = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RvCarStudio", "settings.json");
    static Dictionary<string, string>? values;
    static Dictionary<string, string> V
    {
        get
        {
            if (values != null) return values;
            try { values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(File_)); } catch (Exception) { }
            return values ??= new();
        }
    }
    static void Set(string k, string v)
    {
        if (AutoScenario.Running) return; // a test run leaves your settings alone
        V[k] = v;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(File_)!);
            File.WriteAllText(File_, JsonSerializer.Serialize(V));
        }
        catch (Exception) { }
    }
    public static string? RvPath { get => V.GetValueOrDefault("rvpath"); set => Set("rvpath", value ?? ""); }
    public static string? LastCar { get => V.GetValueOrDefault("lastcar"); set => Set("lastcar", value ?? ""); }
    /// <summary>The look: Dark unless you chose another</summary>
    public static ThemeKind Theme
    {
        get => Enum.TryParse<ThemeKind>(V.GetValueOrDefault("theme"), out var t) ? t : ThemeKind.Dark;
        set => Set("theme", value.ToString());
    }
    /// <summary>The small view cube in the corner of the 3D view: on unless you hid it</summary>
    public static bool ViewCube { get => V.GetValueOrDefault("viewcube") != "0"; set => Set("viewcube", value ? "1" : "0"); }
    public static string Author { get => V.GetValueOrDefault("author") ?? ""; set => Set("author", value); }
}

public static class Dialogs
{
    static Window Frame(string title, double w, double h) => new()
    {
        Title = title, Width = w, Height = h, WindowStartupLocation = WindowStartupLocation.CenterOwner,
        CanResize = false, ShowInTaskbar = false, Background = Look.Panel, Foreground = Look.Text
    };

    /// <summary>Index of the button pressed. Closing the window counts as the last one</summary>
    public static async Task<int> Ask(Window owner, string text, params string[] buttons)
    {
        var d = Frame("RV Car Studio", 420, 150);
        int result = buttons.Length - 1;
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6 };
        for (int i = 0; i < buttons.Length; i++)
        {
            int k = i;
            var b = new Button { Content = buttons[i], MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            if (i == 0) Look.Primary(b);
            b.Click += (_, _) => { result = k; d.Close(); };
            row.Children.Add(b);
        }
        d.Content = new DockPanel
        {
            Margin = new Thickness(16),
            Children = { Bottom(row), new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap } }
        };
        await d.ShowDialog(owner);
        return result;
    }

    static Control Bottom(Control c) { DockPanel.SetDock(c, Dock.Bottom); return c; }

    public static async Task<string?> Input(Window owner, string text, string start)
    {
        var d = Frame("RV Car Studio", 440, 170);
        string? result = null;
        var box = new TextBox { Text = start, Margin = new Thickness(0, 8) };
        var ok = Look.Primary(new Button { Content = "OK", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true });
        var cancel = new Button { Content = "Cancel", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };
        ok.Click += (_, _) => { result = box.Text; d.Close(); };
        cancel.Click += (_, _) => d.Close();
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Children = { ok, cancel } };
        d.Content = new DockPanel
        {
            Margin = new Thickness(16),
            Children = { Bottom(row), new StackPanel { Children = { new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap }, box } } }
        };
        await d.ShowDialog(owner);
        return result;
    }

    public static void ShowText(Window owner, string title, string text)
    {
        var d = Frame(title, 560, 300);
        d.CanResize = true;
        d.Content = new ScrollViewer { Content = new SelectableTextBlock { Text = text, Margin = new Thickness(16), TextWrapping = TextWrapping.Wrap } };
        d.Show(owner);
    }
}
