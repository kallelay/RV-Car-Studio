using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RvStudio.Core;

namespace RvStudio.App;

public enum NewCarKind { Repaint, Remodel, Original }

public sealed record NewCarChoice(NewCarKind Kind, string BaseCar, string Folder, string Name, string? BodyFile = null, string? WheelFile = null, string? TextureFile = null);

/// <summary>The three things car makers do: paint a car, change a car, or bring their own model</summary>
public sealed class NewCarWizard : Window
{
    public NewCarChoice? Result;

    readonly List<CarEntry> cars;
    readonly ComboBox baseBox = new() { HorizontalAlignment = HorizontalAlignment.Stretch, FontSize = 12 };
    readonly TextBox nameBox = new() { FontSize = 12, Watermark = "The name shown in the game" };
    readonly TextBox folderBox = new() { FontSize = 12, Watermark = "A folder inside cars" };
    readonly TextBlock problem = new() { Foreground = Look.Bad, FontSize = 12, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock kindNote = new() { FontSize = 12, Foreground = Page.Faint, TextWrapping = TextWrapping.Wrap };
    readonly StackPanel ownFiles = new() { Spacing = 5, IsVisible = false };
    readonly TextBox bodyBox = new() { FontSize = 12, Watermark = "Body model, .prm or .obj" };
    readonly TextBox wheelBox = new() { FontSize = 12, Watermark = "A wheel model, .prm or .obj (optional: keeps the base car's)" };
    readonly TextBox textureBox = new() { FontSize = 12, Watermark = "Texture, .bmp, .png or .jpg (optional)" };
    readonly GameFolder game;
    NewCarKind kind = NewCarKind.Repaint;
    bool folderTyped;

    static Control Card(string title, string text, RadioButton r)
    {
        r.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = title, FontSize = 14, FontWeight = FontWeight.SemiBold },
                new TextBlock { Text = text, FontSize = 12, Foreground = Page.Faint, TextWrapping = TextWrapping.Wrap }
            }
        };
        r.GroupName = "kind";
        r.HorizontalAlignment = HorizontalAlignment.Stretch;
        return r;
    }

    public NewCarWizard(GameFolder game, string? current)
    {
        this.game = game;
        Title = "New car";
        Width = 540; Height = 640;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Background = Look.Panel;

        // The game's own cars first (they drive well and every player has them), then the others; Toyeca to start from
        cars = game.ListCars().OrderBy(c => c.Stock ? 0 : 1).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        baseBox.ItemsSource = cars.Select(c => c.Name + "  (" + c.Folder + ")" + (c.Stock ? "" : "  -  yours")).ToList();
        int toyeca = cars.FindIndex(c => c.Folder.Equals("toyeca", StringComparison.OrdinalIgnoreCase));
        baseBox.SelectedIndex = Math.Max(0, toyeca);

        var repaint = new RadioButton { IsChecked = true };
        var remodel = new RadioButton();
        var original = new RadioButton();
        repaint.IsCheckedChanged += (_, _) => { if (repaint.IsChecked == true) SetKind(NewCarKind.Repaint); };
        remodel.IsCheckedChanged += (_, _) => { if (remodel.IsChecked == true) SetKind(NewCarKind.Remodel); };
        original.IsCheckedChanged += (_, _) => { if (original.IsChecked == true) SetKind(NewCarKind.Original); };

        nameBox.TextChanged += (_, _) => { if (!folderTyped) { filling = true; folderBox.Text = Suggest(nameBox.Text); filling = false; } Validate(); };
        folderBox.TextChanged += (_, _) => { if (!filling) folderTyped = true; Validate(); };

        foreach (var b in new[] { bodyBox, wheelBox, textureBox }) b.TextChanged += (_, _) => { if (tried) Validate(); };
        ownFiles.Children.Add(FileRow("Body", bodyBox));
        ownFiles.Children.Add(FileRow("Wheel", wheelBox));
        ownFiles.Children.Add(FileRow("Texture", textureBox));

        var ok = Look.Primary(new Button { Content = "Make the car", MinWidth = 110, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true });
        var cancel = new Button { Content = "Cancel", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };
        radios = new[] { repaint, remodel, original };
        ok.Click += (_, _) => Confirm();
        cancel.Click += (_, _) => Close();

        Content = new ScrollViewer
        {
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = "What are you making?", FontSize = 18, FontWeight = FontWeight.SemiBold },
                    Card("Repaint", "New colours on a car that exists. Same models, same handling: only the texture changes.", repaint),
                    Card("Remodel", "Start from a car and change its settings, parts or models.", remodel),
                    Card("Original car", "Your own model. Wheels are placed and sizes worked out for you, the handling comes from the base car.", original),
                    new TextBlock { Text = "Base car", FontSize = 12, Margin = new Thickness(0, 8, 0, 0) },
                    baseBox,
                    kindNote,
                    new TextBlock { Text = "Name", FontSize = 12 },
                    nameBox,
                    new TextBlock { Text = "Folder", FontSize = 12 },
                    folderBox,
                    ownFiles,
                    problem,
                    new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Children = { ok, cancel } }
                }
            }
        };
        SetKind(NewCarKind.Repaint);
    }

    bool filling;
    readonly RadioButton[] radios;

    /// <summary>What Make the car does</summary>
    public bool Confirm()
    {
        tried = true;
        if (!Validate() || !CheckFiles()) return false;
        Result = new NewCarChoice(kind, cars[baseBox.SelectedIndex].Folder, folderBox.Text!.Trim(), nameBox.Text!.Trim(),
            kind == NewCarKind.Original ? bodyBox.Text : null,
            kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(wheelBox.Text) ? wheelBox.Text : null,
            kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(textureBox.Text) ? textureBox.Text : null);
        Close();
        return true;
    }

    /// <summary>Fills the boxes as a person would, for the release test</summary>
    public void Fill(NewCarKind k, string baseFolder, string name, string? body = null, string? wheel = null, string? texture = null)
    {
        radios[(int)k].IsChecked = true;
        baseBox.SelectedIndex = Math.Max(0, cars.FindIndex(c => c.Folder.Equals(baseFolder, StringComparison.OrdinalIgnoreCase)));
        nameBox.Text = name;
        bodyBox.Text = body; wheelBox.Text = wheel; textureBox.Text = texture;
    }

    public string Problem => problem.Text ?? "";
    public string BaseFolder => baseBox.SelectedIndex >= 0 ? cars[baseBox.SelectedIndex].Folder : "";
    public IReadOnlyList<CarEntry> BaseList => cars;

    static string Suggest(string? name)
    {
        var s = new string((name ?? "").Trim().ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());
        return s;
    }

    Control FileRow(string label, TextBox box)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("60,*,4,Auto") };
        g.Children.Add(new TextBlock { Text = label, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(box, 1);
        g.Children.Add(box);
        var b = new Button { Content = "...", FontSize = 12 };
        Grid.SetColumn(b, 3);
        b.Click += async (_, _) =>
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions { Title = label, AllowMultiple = false });
            if (files.FirstOrDefault()?.TryGetLocalPath() is { } p) box.Text = p;
        };
        g.Children.Add(b);
        return g;
    }

    void SetKind(NewCarKind k)
    {
        kind = k;
        ownFiles.IsVisible = k == NewCarKind.Original;
        kindNote.Text = k switch
        {
            NewCarKind.Repaint => "Its files are copied, the original isn't touched.",
            NewCarKind.Remodel => "Its files are copied, the original isn't touched.",
            _ => "Only the handling and the sounds are kept from it."
        };
        Validate();
    }

    // Make the car was pressed: from then on every missing answer is said, not only the wrong ones
    bool tried;

    bool Validate()
    {
        problem.Text = "";
        var name = nameBox.Text?.Trim() ?? "";
        var folder = folderBox.Text?.Trim() ?? "";
        if (name == "" && folder == "" && !tried) return false;
        if (name == "") problem.Text = "Give the car a name";
        else if (!Car.ValidFolderName(folder)) problem.Text = "That isn't a good folder name: letters and numbers, no \\ / ; or \"";
        else if (game.CarLayer(folder) != null || Directory.Exists(Path.Combine(game.CarsDir, folder))) problem.Text = "There already is a car in cars\\" + folder + ": choose another folder";
        else if (baseBox.SelectedIndex < 0) problem.Text = "Choose the base car";
        else if (kind == NewCarKind.Original && string.IsNullOrWhiteSpace(bodyBox.Text)) problem.Text = "Choose the body model (.obj or .prm)";
        else if (kind == NewCarKind.Original && !File.Exists(bodyBox.Text!.Trim())) problem.Text = "There's no body model at " + bodyBox.Text;
        else if (kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(wheelBox.Text) && !File.Exists(wheelBox.Text.Trim())) problem.Text = "There's no wheel model at " + wheelBox.Text;
        else if (kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(textureBox.Text) && !File.Exists(textureBox.Text.Trim())) problem.Text = "There's no texture at " + textureBox.Text;
        return problem.Text == "";
    }

    /// <summary>The files read for real before anything is made: a model that isn't one, or a picture that isn't one, is said here</summary>
    bool CheckFiles()
    {
        if (kind != NewCarKind.Original) return true;
        string? Model(string what, string? path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            var ext = Path.GetExtension(path).ToLowerInvariant();
            if (ext is not (".obj" or ".prm")) return $"The {what} model must be an .obj or a .prm file, not {(ext == "" ? "a file without an ending" : ext)}";
            try
            {
                var m = CarTools.LoadModelFile(path.Trim());
                if (m.Faces.Length == 0) return $"The {what} model has no polygons in it: is it really a model? ({Names.File(path)})";
                if (!m.IsValid()) return $"The {what} model is damaged: some polygons point at corners it doesn't have ({Names.File(path)})";
                return null;
            }
            catch (Exception ex) { return $"The {what} model can't be read ({Names.File(path)}): {ex.Message}"; }
        }
        var bad = Model("body", bodyBox.Text) ?? Model("wheel", wheelBox.Text);
        if (bad == null && !string.IsNullOrWhiteSpace(textureBox.Text))
        {
            try { using var b = Images.Load(textureBox.Text.Trim()); if (b == null) bad = "The texture can't be read as a picture (" + Names.File(textureBox.Text) + "): use a .bmp, .png or .jpg"; }
            catch (Exception ex) { bad = "The texture can't be read (" + Names.File(textureBox.Text) + "): " + ex.Message; }
        }
        problem.Text = bad ?? "";
        return bad == null;
    }
}
