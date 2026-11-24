using Avalonia.Controls;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

// RvCarStudio -rvpath <copy> --docs <car folder> <folder for pictures>: the pictures of the README and the documentation
public static partial class AutoScenario
{
    public static async Task RunDocs(MainWindow w, string car, string shotDir)
    {
        Running = true;
        shots = shotDir;
        Directory.CreateDirectory(shots);
        log = new StreamWriter(Path.Combine(shots, "docs.txt"));
        var d = w.Doc;
        try
        {
            w.Width = 1440; w.Height = 860;
            await Wait(1500);
            w.SetTheme(ThemeKind.Dark);
            async Task Shot(string name, int wait = 500) { await Wait(wait); await FullShot(w, name); Log("shot " + name); }
            async Task Page(int tab, string name, string? section = null)
            {
                w.Tabs.SelectedIndex = tab;
                await Wait(350);
                if (section != null && (w.Tabs.SelectedItem as TabItem)?.Content is Page p) { p.JumpTo(section); await Wait(250); }
                await FullShot(w, name);
                Log("shot " + name);
            }
            void Turn(float yaw, float pitch, float closer = 1)
            {
                w.View.SetView(ViewMode.Perspective);
                w.View.Yaw = yaw * MathF.PI / 180; w.View.Pitch = pitch * MathF.PI / 180;
                w.View.FrameCar();
                w.View.Distance *= closer;
                w.View.Redraw();
            }

            await Shot("cars", 2500);
            w.PickCar(car);
            w.SetLocked(true);
            await Wait(900);
            w.View.ShowMarkers = false;
            d.Select(new Selection(SelKind.None));
            Turn(-38, 16, 0.8f);
            await Page(w.PropertiesTabIndex, "pro", "Handling");
            w.SetTheme(ThemeKind.Light);
            await Shot("light");
            w.SetTheme(ThemeKind.Classic);
            await Shot("classic");
            w.SetTheme(ThemeKind.Dark);
            w.View.ShowMarkers = true;
            d.Select(new Selection(SelKind.Wheel, 0));
            Turn(-38, 16, 0.75f);
            await Page(0, "selected");
            await Page(w.FilesTabIndex, "files");
            await Page(w.PropertiesTabIndex, "properties");
            await Page(w.PropertiesTabIndex, "menu", "Menu: the car selection screen");
            await Page(w.PropertiesTabIndex + 1, "rvgl");

            // Vertices
            d.Select(new Selection(SelKind.Body));
            Turn(-38, 22, 0.62f);
            w.Tabs.SelectedIndex = w.ModelTabIndex;
            w.ToggleVertexMode();
            d.PickVertices(Enumerable.Range(0, Math.Min(24, d.VertexPositions().Length)), false);
            await Shot("vertices");
            w.ToggleVertexMode();

            // Hull
            w.ShowHull(true);
            w.View.HullSolid = true;
            d.RebuildHull();
            d.Select(new Selection(SelKind.None));
            w.View.ShowMarkers = false;
            Turn(-38, 18, 0.72f);
            await Page(w.ModelTabIndex, "hull", "Collision");
            w.View.HullSolid = false;
            w.ShowHull(false);
            d.Undo();

            // Paint
            w.View.ShowMarkers = false;
            Turn(-38, 16, 0.75f);
            w.Tabs.SelectedIndex = w.PaintTabIndex;
            w.PaintHue.Value = 200;
            await Shot("paint", 1400);
            w.PaintHue.Value = 0;
            w.ResetPaint();
            await Wait(500);
            if (w.OpenUvEditor() is { } tx)
            {
                await Wait(900);
                SaveWin(tx, "texture");
                tx.CloseDiscarding();
                await Wait(400);
            }

            // Shade
            w.SetShadeView(ShadeMode.Clay);
            await Page(w.ShadeTabIndex, "shade");
            w.SetShadeView(ShadeMode.Textured);

            // Package, carbox, readme
            await Page(w.PackageTabIndex, "package");
            var cb = new CarboxWindow(w, d);
            cb.Show(w);
            for (int i = 0; i < 120 && !cb.VariationsReady; i++) await Wait(100);
            SaveWin(cb, "carbox");
            cb.UseLook(4);
            await Wait(300);
            SaveWin(cb, "carbox_side");
            cb.Close();
            var rm = new ReadmeWindow(d.Car!, d.Hull, false);
            rm.Show(w);
            rm.Fill("Your name", "A wedge of the nineties with more grip than manners.", Readme.Kind.Original);
            await Wait(600);
            SaveWin(rm, "readme");
            rm.Close();

            // New car, compare, tour
            var nw = new NewCarWizard(d.Game!, null);
            nw.Show(w);
            await Wait(700);
            SaveWin(nw, "newcar");
            nw.Close();
            w.OpenCompare("toyeca");
            await Wait(900);
            if (w.Compare is { } cmp) { SaveWin(cmp, "compare"); cmp.Close(); }
            w.StartTour(0);
            await Shot("tour", 700);
            w.EndTour();

            // Race
            w.Tabs.SelectedIndex = w.PropertiesTabIndex;
            w.RaceOpponents = 3;
            w.RaceAiDrives = true;
            w.StartRace();
            await Shot("race", 9000);
            w.StopDrive();
            await Wait(300);

            // Easy
            w.View.ShowMarkers = false;
            w.SetMode(false);
            Turn(-38, 16, 0.75f);
            w.EasyGo("Main");
            await Shot("easy", 700);
            w.NextPartView(new Selection(SelKind.Body));
            w.EasyGo("Wheels");
            await Shot("easy_wire");
            w.SetMode(true);
        }
        catch (Exception ex) { Log("FAIL " + ex); }
        finally { log!.Flush(); Environment.Exit(0); }
    }
}
