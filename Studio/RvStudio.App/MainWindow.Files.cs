using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

public sealed partial class MainWindow
{
    // ---------------------------- Files: each file of the car, with Pick; the carbox, readme and zip ----------------------------

    /// <summary>The Files tab: the car's files one by one (texture, hull, shadow, carbox, each model slot), each with
    /// Pick; the carbox as it looks and its maker; the readme and the zip; parameters.txt as text</summary>
    Page FilesPage(Control rawHost)
    {
        var p = NewPage();
        CarData D() => Doc.D!;
        p.Header("The car's files");
        p.Info(() => Doc.Car == null ? "" : "Folder: cars\\" + Doc.Car.Folder + (GameFolder.IsStock(Doc.Car.Folder) ? "   (stock: your changes go in packs\\local, on top)" : ""));
        p.Button("Open the car's folder", OpenFolder);

        p.TextWithButton("Texture", () => D().TPage, v => { D().TPage = v; Doc.TextureVersion++; }, "Pick",
            async () => await PickInto("Texture", new[] { "*.bmp", "*.png", "*.jpg", "*.jpeg" }, "car.bmp", CarTools.CopyTexture, rel => { D().TPage = rel; Doc.TextureVersion++; }, FileChecks.Texture),
            "The car's texture page. Pick one from anywhere: a .png or .jpg is made into a .bmp in the car's folder");
        p.Info(() =>
        {
            if (Doc.Car == null) return "";
            var skins = Doc.Car.FindSkins();
            return skins.Count > 1 ? "Skins: " + string.Join(", ", skins.Select(Path.GetFileNameWithoutExtension)) : "No other skins";
        });
        p.TextWithButton("Hull", () => D().Coll, v => { D().Coll = v; Doc.LoadHull(); }, "Pick",
            async () => await PickInto("Collision hull", new[] { "*.hul" }, null, (a, b) => File.Copy(a, b, true), rel => { D().Coll = rel; Doc.LoadHull(); }, FileChecks.Hull),
            "Collision hull, .hul: what the car bumps the world with");
        p.TextWithButton("Shadow", () => D().TShadow, v => D().TShadow = v, "Pick",
            async () => await PickShadow(),
            "shadow.bmp: the dark patch under the car (Shade tab, Make shadow)");
        p.TextWithButton("Carbox", () => D().TCarBox, v => D().TCarBox = v, "Pick",
            async () => await CarboxFromPicture(), "The picture on the car selection screen. Pick any picture: it's fitted into a square");

        SoundRows(p);

        p.ProStart();
        p.Header("Models");
        p.Note("The models the car uses, and what each is for. Pick takes a model of this car, of another car (copied in), or a file from anywhere.");
        // Only the slots in use: the free ones are one click away
        bool showFree = false;
        for (int i = 0; i < 19; i++)
        {
            int k = i;
            var row = p.TextWithButton("Model " + k, () => D().Models[k], v => { D().Models[k] = v; Doc.ReloadModels(); }, "Pick", async () =>
            {
                var rel = await AskModel("Model " + k + RoleOf(k));
                if (rel == null) return;
                Doc.Edit("Model " + k, () => { D().Models[k] = rel == "" ? "NONE" : rel; Doc.ReloadModels(); });
                Doc.EndGesture();
                filesPage.Refresh();
            });
            var hint = p.Hint(() => RoleOf(k).TrimStart(' ', ':'));
            p.Live(() => row.IsVisible = hint.IsVisible = showFree || RoleOf(k) != ": free");
        }
        var free = new CheckBox { Content = "Show the free slots too", FontSize = 12, MinHeight = 24 };
        ToolTip.SetTip(free, "Empty slots, for a part you add by hand (Add a part, on the left, does it for you)");
        free.IsCheckedChanged += (_, _) => { showFree = free.IsChecked == true; filesPage.Refresh(); };
        p.Add(free);
        p.ProEnd();
        p.Button("Import a model from .obj for the selected part...", async () => await ImportObjAsked(), "Made in Blender: metres, Y up, front towards -Z. Saving the .obj again brings it in again");
        p.Button("Export the selected part as .obj...", async () => await ExportObjAsked());
        p.Button("Export the whole car as .obj...", async () => await ExportCarObjAsked());

        p.Note("The carbox, the readme and the zip are on the Package tab.");

        p.ProStart();
        p.Header("Everything in the folder");
        p.Info(() =>
        {
            if (Doc.Car is not { } c) return "";
            var files = c.Game.CarFiles(c.Folder).Where(f => !f.Key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => f.Key, StringComparer.OrdinalIgnoreCase).ToList();
            string Size(string f) { try { var n = new FileInfo(f).Length; return n >= 1024 ? $"{n / 1024.0:0} KB" : $"{n} B"; } catch (Exception) { return "?"; } }
            string Where(string f) => f.Contains(Path.Combine("packs", "local"), StringComparison.OrdinalIgnoreCase) ? "  (your change)" : "";
            return string.Join(Environment.NewLine, files.Select(f => Path.GetFileName(f.Key) + "   " + Size(f.Value) + Where(f.Value)));
        });

        p.Header("parameters.txt, as text");
        p.Add(rawHost);
        p.ProEnd();
        return p;
    }

    /// <summary>What a model slot is for, from the car's values: ": body", ": wheel, front left"...</summary>
    string RoleOf(int slot)
    {
        if (Doc.D is not { } d) return "";
        var r = new List<string>();
        if (d.BodyModel == slot) r.Add("body");
        for (int i = 0; i < 4; i++)
        {
            if (d.Wheels[i].Present && d.Wheels[i].Model == slot) r.Add("wheel, " + Doc.WheelName(i));
            if (d.Springs[i].Model == slot) r.Add("spring, " + Doc.WheelName(i));
            if (d.Axles[i].Model == slot) r.Add("axle, " + Doc.WheelName(i));
            if (d.Pins[i].Model == slot) r.Add("pin, " + Doc.WheelName(i));
        }
        if (d.Spinner.Model == slot) r.Add("spinner");
        if (d.Aerial.SecModel == slot) r.Add("aerial");
        if (d.Aerial.TopModel == slot) r.Add("aerial top");
        var m = d.Models[slot].Trim();
        if (r.Count == 0) return m == "" || m.Equals("NONE", StringComparison.OrdinalIgnoreCase) ? ": free" : ": not used by a part";
        // Four of one kind: said once
        if (r.Count == 4 && r.Select(x => x.Split(',')[0]).Distinct().Count() == 1) return ": " + r[0].Split(',')[0] + ", all four";
        return ": " + string.Join("; ", r);
    }

    /// <summary>
    /// A file picked for the car: one of the game's own is used where it is; one from elsewhere is copied into the car's
    /// folder (keeping the first version as .bak), under `name` or its own name. Undo takes the choice back.
    /// </summary>
    /// <summary>Picky: only the right kind of file is offered, and the one picked is checked before the car uses it</summary>
    async Task PickInto(string what, string[] patterns, string? name, Action<string, string> copy, Action<string> use, Func<string, string?> check, string? startIn = null)
    {
        if (Doc.Car is not { } c) return;
        IStorageFolder? start = null;
        if (startIn != null && Directory.Exists(startIn)) start = await StorageProvider.TryGetFolderFromPathAsync(startIn);
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = what, AllowMultiple = false, SuggestedStartLocation = start,
            FileTypeFilter = new[] { new FilePickerFileType(what) { Patterns = patterns } }
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } path) return;
        if (check(path) is { } why)
        {
            Say("Not used: " + why);
            await Dialogs.Ask(this, what + " not changed:" + Environment.NewLine + why, "OK");
            return;
        }
        try
        {
            string rel;
            if (c.Game.RelOf(path) is { } inGame && Path.GetExtension(path).Equals(Path.GetExtension(name ?? path), StringComparison.OrdinalIgnoreCase)) rel = inGame;
            else
            {
                var file = name ?? Path.GetFileName(path);
                rel = "cars\\" + c.Folder + "\\" + file;
                var target = c.Game.WritePath(rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                Files.Backup(target);
                copy(path, target);
            }
            Doc.Edit(what, () => use(rel));
            Doc.EndGesture();
            filesPage.Refresh();
            Say(what + ": " + rel);
        }
        catch (Exception ex) { Say("Couldn't use " + Path.GetFileName(path) + ": " + ex.Message); }
    }

    async Task PickShadow() =>
        await PickInto("Shadow", new[] { "*.bmp", "*.png" }, "shadow.bmp", CarTools.CopyTexture, rel => { Doc.D!.TShadow = rel; Doc.TextureVersion++; }, FileChecks.Picture);

    /// <summary>A sound for the car: a plain .wav, from the game's own sounds or anywhere (copied in)</summary>
    async Task PickSound(string what, Action<string> use)
    {
        var wavs = Doc.Car == null ? null : Doc.Car.Game.Find("wavs\\moto.wav") is { } w ? Path.GetDirectoryName(w) : Path.Combine(Doc.Car.Game.Root, "wavs");
        await PickInto(what + " sound", new[] { "*.wav" }, null, (a, b) => File.Copy(a, b, true), use, FileChecks.Wav, wavs);
    }

    /// <summary>The car's sounds, each with Pick (also on Properties (RVGL))</summary>
    void SoundRows(Page p)
    {
        CarData D() => Doc.D!;
        p.Header("Sounds");
        p.TextWithButton("Engine", () => D().SfxEngine, v => D().SfxEngine = v, "Pick", async () => await PickSound("Engine", rel => D().SfxEngine = rel), "A .wav. How it's played depends on the car's class (electric, glow)");
        p.TextWithButton("Horn", () => D().SfxHonk, v => D().SfxHonk = v, "Pick", async () => await PickSound("Horn", rel => D().SfxHonk = rel));
        p.TextWithButton("Servo", () => D().SfxServo, v => D().SfxServo = v, "Pick", async () => await PickSound("Servo", rel => D().SfxServo = rel), "The steering's whine");
    }

    // ---------------------------- carbox ----------------------------

    readonly Image carboxImage = new() { Width = 192, Height = 192, Stretch = Stretch.Uniform, HorizontalAlignment = HorizontalAlignment.Left };
    string? carboxShown;
    DateTime carboxTime;

    /// <summary>The carbox as it is now, on the Files tab</summary>
    void ShowCarbox()
    {
        var f = Doc.D != null && !string.IsNullOrWhiteSpace(Doc.D.TCarBox) ? Doc.Car?.Game.Find(Doc.D.TCarBox) : null;
        var time = f != null && File.Exists(f) ? File.GetLastWriteTimeUtc(f) : DateTime.MinValue;
        if (f == carboxShown && time == carboxTime) return;
        carboxShown = f; carboxTime = time;
        if (f == null) { carboxImage.Source = null; return; }
        try
        {
            using var b = Images.Load(f);
            if (b == null) { carboxImage.Source = null; return; }
            using var data = b.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            carboxImage.Source = new Bitmap(ms);
        }
        catch (Exception) { carboxImage.Source = null; }
    }

    /// <summary>carbox.bmp written into the car's folder, from a picture made here. Null if it couldn't be</summary>
    public string? SaveCarboxPicture(SKBitmap box)
    {
        if (Doc.Car == null) return null;
        string path = "";
        Tool("Carbox", () => path = CarTools.SaveCarbox(Doc.Car, box));
        if (path == "") return null;
        Say("Carbox saved: " + path);
        carboxShown = null;
        ShowCarbox();
        View.Redraw();
        return path;
    }

    public CarboxWindow? CarboxMaker;

    public async Task OpenCarboxMaker()
    {
        if (Doc.Car == null) return;
        var w = new CarboxWindow(this, Doc);
        CarboxMaker = w;
        await w.ShowDialog(this);
        CarboxMaker = null;
        filesPage.Refresh();
    }

    /// <summary>Any picture as the carbox: fitted into a 256 square on white</summary>
    async Task CarboxFromPicture()
    {
        if (Doc.Car == null) return;
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "A picture for the carbox", AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Pictures") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } } }
        });
        if (files.FirstOrDefault()?.TryGetLocalPath() is not { } path) return;
        if (FileChecks.Picture(path) is { } why) { await Dialogs.Ask(this, "Carbox not changed:" + Environment.NewLine + why, "OK"); return; }
        using var pic = Images.Load(path);
        if (pic == null) { Say("That isn't a picture: " + Path.GetFileName(path)); return; }
        using var box = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using (var cv = new SKCanvas(box))
        {
            cv.Clear(SKColors.White);
            float k = Math.Min(256f / pic.Width, 256f / pic.Height);
            float w = pic.Width * k, h = pic.Height * k;
            using var pp = new SKPaint { FilterQuality = SKFilterQuality.High };
            cv.DrawBitmap(pic, new SKRect((256 - w) / 2, (256 - h) / 2, (256 + w) / 2, (256 + h) / 2), pp);
        }
        SaveCarboxPicture(box);
        filesPage.Refresh();
    }

    /// <summary>
    /// The Package tab: everything that goes out with the car, as the last step. Is it ready (the check, the carbox,
    /// the readme up to date), its carbox, its readme, and the zip players unzip on their game folder.
    /// </summary>
    Page PackagePage()
    {
        var p = NewPage();
        p.Header("Ready to share?");
        p.Info(() =>
        {
            if (Doc.Car is not { } c) return "";
            var lines = new List<string>();
            var report = CarChecker.Check(c);
            lines.Add(report.Errors.Count == 0 ? $"✓ The car check: {report.Score}/100, no errors" : $"✗ The car check: {report.Errors.Count} errors to fix first (listed under the 3D view)");
            foreach (var w in report.Warnings.Where(w => !w.StartsWith("No readme"))) lines.Add("   ▲ " + w);
            bool box = !string.IsNullOrWhiteSpace(c.Data.TCarBox) && c.Game.Find(c.Data.TCarBox) != null;
            lines.Add(box ? "✓ A carbox" : "✗ No carbox yet: the selection screen shows an empty box (below: Carbox maker or Quick carbox)");
            var rm = c.Game.Find("cars\\" + c.Folder + "\\readme.txt");
            var newest = c.Game.CarFiles(c.Folder).Where(x => !x.Key.EndsWith("readme.txt", StringComparison.OrdinalIgnoreCase) && !x.Key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                .Select(x => File.GetLastWriteTimeUtc(x.Value)).DefaultIfEmpty(DateTime.MinValue).Max();
            lines.Add(rm == null ? "✗ No readme yet (Pack as zip asks for it)" : File.GetLastWriteTimeUtc(rm) < newest ? "▲ The readme is older than the car's last change (Pack as zip offers to write it again)" : "✓ A readme, up to date");
            if (Doc.Modified) lines.Add("▲ Not saved: Pack as zip saves first");
            return string.Join(Environment.NewLine, lines);
        });
        PackageSections(p);
        return p;
    }

    void PackageSections(Page p)
    {
        p.Header("Carbox");
        p.Note("The car's picture on the selection screen.");
        p.Add(new Border { Child = carboxImage, BorderBrush = Look.Line, BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 2, 0, 4) });
        p.Info(() => { ShowCarbox(); return carboxShown == null ? "No carbox yet" : Path.GetFileName(carboxShown); });
        p.Button("Carbox maker...", async () => await OpenCarboxMaker(), "The car from the angle you like, your background, its name as you want it, seen as you change it", true);
        p.Button("Quick carbox", async () => await MakeCarbox(), "In one click: the car from the front left, its name above, on white");
        p.Button("A picture of my own...", async () => await CarboxFromPicture(), "Any picture, fitted into the square");

        p.Header("Readme and sharing");
        p.Note("The readme tells players who made the car, how it drives, how to install it and what they may do with it. Packing writes it first.");
        p.Button("Readme...", async () => await OpenReadme(), "Answer a few questions, see the readme as it will be, write it");
        p.Button("Pack as zip...", async () => await PackZip());
    }
}
