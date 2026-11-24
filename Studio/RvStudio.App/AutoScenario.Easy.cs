using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

// Easy mode: the old Car Studio's window, every mode of its Edit Menu
public static partial class AutoScenario
{
    static async Task EasyLook(MainWindow w, Doc d)
    {
        await Wait(400);
        Check(w.EasyShown && !w.ProMode, "Easy: the Edit Menu and the Render Settings show");
        Check(string.Join(",", w.EasyNames) == "Main,Texture,Handling,Frontend Stuffs,Body,Wheels,Suspensions,Spinner,Aerial,AI,Camera,Collision,Carbox,Shade,Shadow,Misc.,Readme,Packing,About,Console", "the old modes, in the old order");
        int n = 0;
        foreach (var name in w.EasyNames)
        {
            w.EasyGo(name);
            await Wait(250);
            var page = (w.Tabs.SelectedItem as TabItem)?.Content as Control;
            var shown = page?.GetVisualDescendants().OfType<TextBlock>().Count(t => t.IsEffectivelyVisible && !string.IsNullOrWhiteSpace(t.Text)) ?? 0;
            Check(shown >= 1 && page!.Bounds.Height > 60, $"Easy {name}: its page shows ({shown} texts, {page?.Bounds.Height:0} high)");
            Check(w.EasyTip.Length > 10, $"Easy {name}: a tip: {w.EasyTip}");
            WindowShot(w, $"easy_{n++:00}_{name.Replace(" ", "").Replace(".", "")}");
        }
        // < and > go round
        w.EasyGo(-1);
        Check(w.EasyBox.SelectedItem as string == "Console", "< from Main goes to the last: " + w.EasyBox.SelectedItem);
        w.EasyGo(w.EasyBox.SelectedIndex + 1);
        Check(w.EasyBox.SelectedItem as string == "Main", "> from the last goes to Main");
        // Values typed here reach the car, with undo
        w.EasyGo("Handling");
        var was = d.D!.TopSpeed;
        d.Edit("Top speed", () => d.D!.TopSpeed = was + 1);
        d.EndGesture();
        d.Undo();
        Check(Math.Abs(d.D!.TopSpeed - was) < 0.001, "undo works in Easy");
        // The modes choose their part
        w.EasyGo("Wheels");
        Check(d.Sel.Kind == SelKind.Wheel, "Wheels chooses a wheel: " + d.Sel.Label);
        w.EasyGo("Body");
        Check(d.Sel.Kind == SelKind.Body, "Body chooses the body: " + d.Sel.Label);
        // The drawing: solid, wire, points, nothing, half, solid again; each draws differently
        d.Select(new Selection(SelKind.None));
        var looks = new HashSet<int>();
        for (int i = 0; i < 5; i++)
        {
            using var b = await w.View.Capture(320, false);
            int h = 17;
            for (int y = 0; y < b.Height; y += 2) for (int x = 0; x < b.Width; x += 2) h = h * 31 + b.GetPixel(x, y).GetHashCode();
            looks.Add(h);
            SavePng(b, "easy_body_" + i);
            w.NextPartView(new Selection(SelKind.Body));
            await Wait(150);
        }
        Check(looks.Count == 5, "the body is drawn in five ways: " + looks.Count);
        Check(d.PartViews.Count == 0 && d.Hidden.Count == 0, "and is solid again after them");
        // A picture for the carbox isn't taken with parts in wire
        w.NextPartView(new Selection(SelKind.Body));
        w.EasyGo("Main");
        w.ChangeCar();
        await Wait(600);
        WindowShot(w, "easy_cars");
        w.SetLocked(true);
        await Wait(300);
        w.PickCar("cougar");
        await Wait(700);
        Check(w.EasyBox.SelectedItem as string == "Main" && d.PartViews.Count == 0, "another car starts at Main, solid");
        WindowShot(w, "easy_cougar");
        foreach (var kind in new[] { ThemeKind.Dark, ThemeKind.Light, ThemeKind.Classic })
        {
            w.SetTheme(kind);
            await Wait(400);
            WindowShot(w, "easy_theme_" + kind);
        }
        w.PickCar("tourcar");
        await Wait(500);
        // A link or a tour leads to Pro's tabs
        w.GoTo(w.FilesTabIndex);
        Check(w.ProMode && w.Tabs.SelectedIndex == w.FilesTabIndex, "a link out of Easy opens in Pro");
        w.SetMode(false);
        w.StartTour(0);
        Check(w.ProMode, "a tour opens in Pro");
        w.EndTour();
        w.SetMode(false);
        await Wait(200);
    }
}
