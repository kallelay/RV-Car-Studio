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
    readonly TextBlock problem = new() { Foreground = Brushes.OrangeRed, FontSize = 12, TextWrapping = TextWrapping.Wrap };
    readonly TextBlock kindNote = new() { FontSize = 12, Foreground = Page.Faint, TextWrapping = TextWrapping.Wrap };
    readonly StackPanel ownFiles = new() { Spacing = 5, IsVisible = false };
    readonly TextBox bodyBox = new() { FontSize = 12, Watermark = "Body model, .prm" };
    readonly TextBox wheelBox = new() { FontSize = 12, Watermark = "A wheel model, .prm (optional: keeps the base car's)" };
    readonly TextBox textureBox = new() { FontSize = 12, Watermark = "Texture, .bmp or .png (optional)" };
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
        Background = new SolidColorBrush(Color.Parse("#23262D"));

        cars = game.ListCars().OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase).ToList();
        baseBox.ItemsSource = cars.Select(c => c.Name + "  (" + c.Folder + ")").ToList();
        baseBox.SelectedIndex = Math.Max(0, cars.FindIndex(c => c.Folder.Equals(current, StringComparison.OrdinalIgnoreCase)));

        var repaint = new RadioButton { IsChecked = true };
        var remodel = new RadioButton();
        var original = new RadioButton();
        repaint.IsCheckedChanged += (_, _) => { if (repaint.IsChecked == true) SetKind(NewCarKind.Repaint); };
        remodel.IsCheckedChanged += (_, _) => { if (remodel.IsChecked == true) SetKind(NewCarKind.Remodel); };
        original.IsCheckedChanged += (_, _) => { if (original.IsChecked == true) SetKind(NewCarKind.Original); };

        nameBox.TextChanged += (_, _) => { if (!folderTyped) { filling = true; folderBox.Text = Suggest(nameBox.Text); filling = false; } Validate(); };
        folderBox.TextChanged += (_, _) => { if (!filling) folderTyped = true; Validate(); };

        ownFiles.Children.Add(FileRow("Body", bodyBox));
        ownFiles.Children.Add(FileRow("Wheel", wheelBox));
        ownFiles.Children.Add(FileRow("Texture", textureBox));

        var ok = new Button { Content = "Make the car", Background = Page.Accent, MinWidth = 110, HorizontalContentAlignment = HorizontalAlignment.Center, IsDefault = true };
        var cancel = new Button { Content = "Cancel", MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center, IsCancel = true };
        ok.Click += (_, _) =>
        {
            if (!Validate()) return;
            Result = new NewCarChoice(kind, cars[baseBox.SelectedIndex].Folder, folderBox.Text!.Trim(), nameBox.Text!.Trim(),
                kind == NewCarKind.Original ? bodyBox.Text : null,
                kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(wheelBox.Text) ? wheelBox.Text : null,
                kind == NewCarKind.Original && !string.IsNullOrWhiteSpace(textureBox.Text) ? textureBox.Text : null);
            Close();
        };
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

    bool Validate()
    {
        problem.Text = "";
        var name = nameBox.Text?.Trim() ?? "";
        var folder = folderBox.Text?.Trim() ?? "";
        if (name == "" && folder == "") return false;
        if (name == "") problem.Text = "It needs a name";
        else if (!Car.ValidFolderName(folder)) problem.Text = "That isn't a good folder name";
        else if (Directory.Exists(Path.Combine(game.CarsDir, folder))) problem.Text = "There already is a car in cars\\" + folder;
        else if (kind == NewCarKind.Original && !File.Exists(bodyBox.Text ?? "")) problem.Text = "Choose the body model";
        return problem.Text == "";
    }
}
