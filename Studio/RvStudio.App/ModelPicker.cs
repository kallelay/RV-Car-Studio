using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>Choose a model by seeing it: from this car, a shared one, or any other car</summary>
public sealed class ModelPicker : Window
{
    /// <summary>The path to put in the parameters. Empty string means no model</summary>
    public string? Result;

    readonly GameFolder game;
    readonly string carFolder;
    readonly WrapPanel tiles = new();
    readonly TextBox search = new() { Watermark = "Search by file or car", FontSize = 12, MinWidth = 200 };
    readonly ComboBox scope = new() { FontSize = 12, ItemsSource = new[] { "This car", "Shared (misc)", "All cars" }, SelectedIndex = 0 };
    readonly TextBlock count = new() { FontSize = 12, Foreground = Page.Faint, VerticalAlignment = VerticalAlignment.Center };
    int fillRun;

    public int TileCount => tiles.Children.Count;

    public ModelPicker(GameFolder game, string carFolder, string title, string? hint = null)
    {
        this.game = game;
        this.carFolder = carFolder;
        Title = title;
        Width = 860; Height = 620;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.Parse("#23262D"));

        var fromFile = new Button { Content = "From a file...", FontSize = 12 };
        fromFile.Click += async (_, _) =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title, AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Re-Volt models") { Patterns = new[] { "*.prm", "*.m" } } }
            });
            if (files.FirstOrDefault()?.TryGetLocalPath() is { } p) { FilePath = p; Close(); }
        };
        var none = new Button { Content = "No model", FontSize = 12 };
        none.Click += (_, _) => { Result = ""; Close(); };
        var cancel = new Button { Content = "Cancel", FontSize = 12, IsCancel = true };
        cancel.Click += (_, _) => Close();

        var top = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(12) };
        top.Children.Add(scope);
        top.Children.Add(search);
        top.Children.Add(count);
        var bottom = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(12), HorizontalAlignment = HorizontalAlignment.Right };
        bottom.Children.Add(fromFile);
        bottom.Children.Add(none);
        bottom.Children.Add(cancel);

        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top);
        DockPanel.SetDock(bottom, Dock.Bottom);
        dock.Children.Add(top);
        dock.Children.Add(bottom);
        dock.Children.Add(new ScrollViewer { Content = tiles, Margin = new Thickness(12, 0), AllowAutoHide = false });
        Content = dock;

        search.Text = hint ?? "";
        scope.SelectionChanged += (_, _) => Fill();
        search.TextChanged += (_, _) => Fill();
        Opened += (_, _) => Fill();
    }

    /// <summary>A file from outside the game, chosen by the button</summary>
    public string? FilePath;

    IEnumerable<string> Models()
    {
        IEnumerable<string> folders = scope.SelectedIndex switch
        {
            0 => new[] { carFolder },
            1 => new[] { "misc" },
            _ => game.CarFolders().Append("misc").Distinct(StringComparer.OrdinalIgnoreCase)
        };
        foreach (var folder in folders)
            foreach (var rel in game.CarFiles(folder).Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase))
            {
                var e = Path.GetExtension(rel).ToLowerInvariant();
                if (e is ".prm" or ".m") yield return rel;
            }
    }

    public void Fill()
    {
        int run = ++fillRun;
        tiles.Children.Clear();
        var words = (search.Text ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var list = Models().Where(r => words.All(w => r.Contains(w, StringComparison.OrdinalIgnoreCase))).Take(400).ToList();
        count.Text = list.Count + " models";
        foreach (var rel in list)
        {
            var image = new Image { Width = 112, Height = 112 };
            var parts = rel.Split('\\');
            var stack = new StackPanel { Spacing = 2 };
            stack.Children.Add(image);
            stack.Children.Add(new TextBlock { Text = parts[^1], FontSize = 11, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 112 });
            var info = new TextBlock { Text = parts.Length > 1 ? parts[^2] : "", FontSize = 10, Foreground = Page.Faint, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 112 };
            stack.Children.Add(info);
            var b = new Button { Content = stack, Margin = new Thickness(0, 0, 6, 6), Padding = new Thickness(5), Tag = rel };
            ToolTip.SetTip(b, rel);
            b.Click += (_, _) => { Result = rel; Close(); };
            tiles.Children.Add(b);
            _ = LoadThumb(run, rel, image, info);
        }
    }

    async Task LoadThumb(int run, string rel, Image image, TextBlock info)
    {
        var made = await Task.Run(() =>
        {
            try
            {
                var file = game.Find(rel);
                if (file == null || run != fillRun) return ((byte[]?)null, 0);
                var m = Prm.Load(file);
                var dir = rel[..rel.LastIndexOf('\\')];
                var tex = game.Find(dir + "\\car.bmp") ?? (dir.EndsWith("misc", StringComparison.OrdinalIgnoreCase) ? game.Find("gfx\\fxpage1.bmp") : null);
                using var b = Thumbnail.OfModel(m, tex, 112);
                return (Thumbnail.Png(b), m.Faces.Length);
            }
            catch (Exception) { return ((byte[]?)null, 0); }
        });
        if (run != fillRun) return;
        if (made.Item1 == null) { info.Text += "  (can't be read)"; return; }
        image.Source = new Bitmap(new MemoryStream(made.Item1));
        info.Text += "  " + made.Item2 + " polys";
    }
}
