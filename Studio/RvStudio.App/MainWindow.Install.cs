using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using RvStudio.Core;

namespace RvStudio.App;

// Installing a downloaded car: File menu, or drop a zip or folder on the window
public sealed partial class MainWindow
{
    void HookInstallDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
        {
            e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        });
        AddHandler(DragDrop.DropEvent, async (_, e) =>
        {
            var path = e.Data.GetFiles()?.Select(f => f.TryGetLocalPath()).FirstOrDefault(p => p != null);
            e.Handled = true;
            if (path == null) return;
            if (!Directory.Exists(path) && !path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) { Say("To install a car, drop a .zip or a folder"); return; }
            await InstallFrom(path);
        });
    }

    async Task InstallAsked()
    {
        if (Doc.Game == null) { Say("Open your Re-Volt folder first"); return; }
        int kind = await Dialogs.Ask(this, "Install a car you downloaded into your game. Where is it?", "A zip file", "A folder", "Cancel");
        string? path = null;
        if (kind == 0)
        {
            var f = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "The car's zip", AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType("Zip files") { Patterns = new[] { "*.zip" } }, FilePickerFileTypes.All }
            });
            path = f.FirstOrDefault()?.TryGetLocalPath();
        }
        else if (kind == 1)
        {
            var f = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = "The folder with the car", AllowMultiple = false });
            path = f.FirstOrDefault()?.TryGetLocalPath();
        }
        if (path != null) await InstallFrom(path);
    }

    /// <summary>Look into a zip or folder, show what's there, and install on a yes</summary>
    public async Task InstallFrom(string path)
    {
        var game = Doc.Game;
        if (game == null) { Say("Open your Re-Volt folder first"); return; }
        Say("Looking into " + Path.GetFileName(path) + "...");
        InstallPlan plan;
        try { plan = await Task.Run(() => CarInstaller.Inspect(game, path)); }
        catch (Exception ex) { Say("Can't read it: " + ex.Message); return; }
        using var _ = plan;
        Say("");

        var report = plan.Describe() + "\nIt goes into " + Path.Combine(game.CarsDir, "<folder>") + ". Nothing that's there is replaced, and File, Undo the last install takes it out again.";
        if (plan.Cars.Count == 0)
        {
            await Report("Install a car", report, "Close");
            return;
        }
        if (await Report("Install a car", report, plan.Cars.Count == 1 ? "Install" : "Install " + plan.Cars.Count + " cars", "Cancel") != 0) return;

        // A folder that's taken: the new name is offered, and can be changed
        var chosen = new List<FoundCar>();
        foreach (var c in plan.Cars)
        {
            bool ask = c.Clash || !c.CanInstall;
            while (ask)
            {
                var who = c.Name == "" ? "this car" : c.Name;
                var why = c.CanInstall ? "cars\\" + c.OriginalFolder + " is taken. Folder for " + who + ":"
                    : c.Issues.First(i => i.Level == InstallLevel.Error).Text + "\nFolder for " + who + ":";
                var name = await Dialogs.Input(this, why, c.Folder);
                if (name == null) break; // leave this one out
                CarInstaller.SetFolder(game, plan, c, name);
                ask = !c.CanInstall;
            }
            if (!ask) chosen.Add(c);
        }
        chosen = chosen.Where(c => c.CanInstall).ToList();
        if (chosen.Count == 0) { Say("Nothing installed"); return; }
        if (!await AskSave()) return;

        try
        {
            var rec = await Task.Run(() => CarInstaller.Install(game, plan, chosen));
            LoadCarList();
            PickCar(chosen[0].Folder);
            Say("Installed " + string.Join(", ", rec.Cars.Select(c => "cars\\" + c)) + " (" + rec.Files.Count + " files). File, Undo the last install takes it out again");
        }
        catch (Exception ex) { await Report("Install a car", "Nothing was installed.\n\n" + ex.Message, "Close"); }
    }

    async Task UndoInstall()
    {
        var game = Doc.Game;
        if (game == null) return;
        var rec = CarInstaller.LastInstall(game);
        if (rec == null) { Say("There's no install to undo"); return; }
        var what = string.Join(", ", rec.Cars.Select(c => "cars\\" + c));
        bool evenChanged = false;
        var changed = CarInstaller.ChangedSince(game, rec);
        if (changed.Count > 0)
        {
            int r = await Dialogs.Ask(this, "Take out " + what + ", installed " + rec.When + "?\n" + changed.Count + " of its files were changed since.", "Delete all", "Keep changed files", "Cancel");
            if (r == 2) return;
            evenChanged = r == 0;
        }
        else if (await Dialogs.Ask(this, "Take out " + what + " (" + rec.Files.Count + " files), installed " + rec.When + "?", "Undo the install", "Cancel") != 0) return;

        try
        {
            var res = CarInstaller.UndoLast(game, evenChanged);
            if (res == null) return;
            LoadCarList();
            if (Doc.Car != null && game.Find("cars\\" + Doc.Car.Folder + "\\parameters.txt") == null)
            {
                // The open car is gone: show another one rather than a car with no files
                var other = allCars.FirstOrDefault(c => c.Stock)?.Folder ?? allCars.FirstOrDefault()?.Folder;
                if (other != null) PickCar(other);
                ChangeCar();
            }
            Say("Took out " + res.Deleted + " files of " + what +
                (res.Changed.Count > 0 ? ". Kept " + res.Changed.Count + " changed files" : "") +
                (res.DirsKept.Count > 0 ? ". " + string.Join(", ", res.DirsKept) + " still holds other files" : ""));
        }
        catch (Exception ex) { Say("Can't undo: " + ex.Message); }
    }

    // A report with room to read, and buttons. Gives the index of the button, the last one when closed
    async Task<int> Report(string title, string text, params string[] buttons)
    {
        var d = new Window
        {
            Title = title, Width = 640, Height = 420, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, Background = new SolidColorBrush(Color.Parse("#23262D"))
        };
        int result = buttons.Length - 1;
        var row = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 6, Margin = new Thickness(0, 10, 0, 0) };
        for (int i = 0; i < buttons.Length; i++)
        {
            int k = i;
            var b = new Button { Content = buttons[i], MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
            if (i == 0 && buttons.Length > 1) b.Background = Page.Accent;
            b.Click += (_, _) => { result = k; d.Close(); };
            row.Children.Add(b);
        }
        DockPanel.SetDock(row, Dock.Bottom);
        d.Content = new DockPanel
        {
            Margin = new Thickness(16),
            Children = { row, new ScrollViewer { Content = new SelectableTextBlock { Text = text, TextWrapping = TextWrapping.Wrap } } }
        };
        await d.ShowDialog(this);
        return result;
    }
}
