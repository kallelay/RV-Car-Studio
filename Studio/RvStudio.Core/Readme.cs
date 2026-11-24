using System.Text;
using SkiaSharp;

namespace RvStudio.Core;

/// <summary>
/// readme.txt for a car, worked out from the car itself: what it is, how it drives (from a simulated run), what's in
/// it, how to install it, who made it and what others may do with it. Plain text in the game's own encoding, 78
/// columns, so it reads the same in Notepad, on a website and in a zip viewer.
/// </summary>
public static class Readme
{
    public enum Kind { Original, Remodel, Repaint, StockEdit }
    public static readonly string[] KindNames = { "Original car", "Remodel (a new body on another car)", "Repaint (a new paint job)", "Changed stock car" };
    public static readonly string[] PermissionNames =
    {
        "Use it as a base for your own cars, if you credit me",
        "Ask me before you use any part of it",
        "Free to use and change, no credit needed",
    };

    public sealed record Info(
        string Author = "", string Email = "", string Site = "", string Description = "",
        Kind Type = Kind.Original, string BasedOn = "", string Thanks = "", int Permission = 0, string Version = "1.0");

    const int W = 78;

    /// <summary>What the car looks like it is: a changed stock car, a repaint (a stock body), or an original</summary>
    public static (Kind Type, string BasedOn) Guess(Car c)
    {
        if (GameFolder.IsStock(c.Folder)) return (Kind.StockEdit, c.Data.Name);
        var stock = CarTools.SameBodyAsStock(c);
        if (stock != "") return (Kind.Repaint, stock);
        return (Kind.Original, "");
    }

    /// <summary>The readme's text</summary>
    public static string Make(Car c, Hull? hull, Info i)
    {
        var d = c.Data;
        var o = new List<string>();
        var name = d.Name.Trim() == "" ? c.Folder : d.Name.Trim();

        // Title
        o.Add(new string('=', W));
        o.Add(Centre(name));
        if (i.Author.Trim() != "") o.Add(Centre("by " + i.Author.Trim()));
        o.Add(Centre("Version " + (i.Version.Trim() == "" ? "1.0" : i.Version.Trim()) + "  -  " + DateTime.Now.ToString("d MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture)));
        o.Add(new string('=', W));

        if (i.Description.Trim() != "") { Section(o, "About"); Para(o, i.Description); }

        Section(o, "The car");
        var kind = i.Type switch
        {
            Kind.Remodel => "Remodel" + (i.BasedOn.Trim() != "" ? " of " + i.BasedOn.Trim() : ""),
            Kind.Repaint => "Repaint" + (i.BasedOn.Trim() != "" ? " of " + i.BasedOn.Trim() : ""),
            Kind.StockEdit => "Changed stock car" + (i.BasedOn.Trim() != "" ? " (" + i.BasedOn.Trim() + ")" : ""),
            _ => "Original" + (i.BasedOn.Trim() != "" ? ", based on " + i.BasedOn.Trim() : ""),
        };
        Field(o, "Name", name);
        Field(o, "Folder", "cars\\" + c.Folder);
        Field(o, "Type", kind);
        Field(o, "Class", d.Class is >= 0 and < 3 ? CarTools.ClassNames[d.Class] : "Special");
        Field(o, "Rating", d.Rating is >= 0 and < 6 ? CarTools.RatingNames[d.Rating] : "?");
        var powered = Enumerable.Range(0, 4).Where(k => d.Wheels[k].Present && d.Wheels[k].Powered).ToArray();
        Field(o, "Drive", powered.Length == 4 ? "Four wheel drive" : powered.Length == 0 ? "None" : powered.All(k => k < 2) ? "Front wheel drive" : powered.All(k => k >= 2) ? "Rear wheel drive" : "Mixed");
        try
        {
            var run = CarSim.RunAcceleration(d.Clone(), hull);
            Field(o, "Top speed", $"{run.TopMph:0} mph ({run.TopMph * 1.609f:0} km/h), measured in a simulated run");
            Field(o, "0 to top", $"{run.Acc:0.0} s");
        }
        catch (Exception) { }
        Field(o, "Weight", $"{d.BodyMass:0.0#} (the game's mass units)");

        Section(o, "What's in it");
        int Polys(int slot) => CarTools.LoadModel(c, slot)?.Faces.Length ?? 0;
        int body = Polys(d.BodyModel);
        int wheels = d.Wheels.Where(w => w.Present).Sum(w => Polys(w.Model));
        int other = Enumerable.Range(0, 19).Where(s => s != d.BodyModel && !d.Wheels.Any(w => w.Model == s) && s != d.Aerial.SecModel && s != d.Aerial.TopModel).Sum(Polys);
        Field(o, "Polygons", $"{body + wheels + other} (body {body}, wheels {wheels}" + (other > 0 ? $", other parts {other}" : "") + ")");
        var files = c.Game.CarFiles(c.Folder).Where(f => !f.Key.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) && !f.Key.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)).ToList();
        if (c.Game.Find(d.TPage) is { } tex)
        {
            var size = ImageSize(tex);
            Field(o, "Texture", Names.File(tex) + (size != null ? $", {size.Value.W} x {size.Value.H}" : "") + (size is { W: > 256 } ? " (RVGL only)" : ""));
        }
        // RVGL's skins: the texture's name, a dash and the skin's (car-alt1.bmp next to car.bmp)
        var texBase = Names.Stem(d.TPage.Trim().Replace('\\', '/').Split('/').Last());
        var skins = texBase == "" ? new List<string>() : files.Select(f => Names.File(f.Key))
            .Where(f => f.StartsWith(texBase + "-", StringComparison.OrdinalIgnoreCase) && f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)).ToList();
        if (skins.Count > 0) Field(o, "Skins", string.Join(", ", skins));
        var extras = new List<string>();
        if (!string.IsNullOrWhiteSpace(d.TShadow) && c.Game.Find(d.TShadow) != null) extras.Add("its own shadow");
        if (!string.IsNullOrWhiteSpace(d.TCarBox) && c.Game.Find(d.TCarBox) != null) extras.Add("a carbox picture");
        if (!string.IsNullOrWhiteSpace(d.Coll) && d.Coll.Trim().StartsWith("cars\\" + c.Folder + "\\", StringComparison.OrdinalIgnoreCase)) extras.Add("its own collision hull");
        if (new[] { d.SfxEngine, d.SfxHonk, d.SfxServo }.Any(s => !string.IsNullOrWhiteSpace(s) && s.Trim().StartsWith("cars\\" + c.Folder + "\\", StringComparison.OrdinalIgnoreCase))) extras.Add("its own sounds");
        if (d.Ai != null) extras.Add("settings for CPU drivers");
        if (extras.Count > 0) Field(o, "Has", string.Join(", ", extras));
        Field(o, "Files", $"{files.Count} files, {files.Sum(f => SafeLength(f.Value)) / 1024.0:0} KB");

        Section(o, "Install");
        Para(o, $"Unzip into your RVGL (or Re-Volt) folder, the one with rvgl.exe in it, keeping the folders. The files go to cars\\{c.Folder}\\. Start the game and pick {name} in the car list.");
        var ext = c.ExternalFiles();
        if (ext.Count > 0) Para(o, "It also uses these files from other folders, which the zip includes: " + string.Join(", ", ext) + ".");

        Section(o, "Credits");
        if (i.Author.Trim() != "") Field(o, "Made by", i.Author.Trim());
        if (i.Email.Trim() != "") Field(o, "E-mail", i.Email.Trim());
        if (i.Site.Trim() != "") Field(o, "Website", i.Site.Trim());
        if (i.BasedOn.Trim() != "" && i.Type != Kind.StockEdit) Field(o, "Based on", i.BasedOn.Trim());
        if (i.Thanks.Trim() != "") Field(o, "Thanks to", i.Thanks.Trim());
        Field(o, "Made with", "RV Car Studio 3");

        Section(o, "Permissions");
        Para(o, PermissionNames[Math.Clamp(i.Permission, 0, PermissionNames.Length - 1)] + ".");
        Para(o, "Re-Volt and its cars belong to their owners. Don't sell this car or put it in a paid product.");

        o.Add("");
        o.Add(new string('=', W));
        return Latin1Safe(string.Join("\r\n", o) + "\r\n");
    }

    // ---------------------------- layout ----------------------------

    static string Centre(string s)
    {
        s = s.Trim();
        if (s.Length >= W) return s;
        return new string(' ', (W - s.Length) / 2) + s;
    }

    static void Section(List<string> o, string title)
    {
        o.Add("");
        o.Add(title);
        o.Add(new string('-', title.Length));
    }

    const int Label = 12;

    static void Field(List<string> o, string label, string value)
    {
        var lines = Wrap(value, W - Label - 2);
        for (int k = 0; k < lines.Count; k++)
            o.Add((k == 0 ? label.PadRight(Label) + ": " : new string(' ', Label + 2)) + lines[k]);
    }

    static void Para(List<string> o, string text)
    {
        bool first = true;
        foreach (var p in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (!first && p.Trim() == "") { o.Add(""); continue; }
            first = false;
            foreach (var l in Wrap(p, W)) o.Add(l);
        }
    }

    /// <summary>Words onto lines of at most `width`; a word longer than that is cut</summary>
    public static List<string> Wrap(string text, int width)
    {
        var r = new List<string>();
        var line = new StringBuilder();
        foreach (var word0 in text.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            var word = word0;
            while (word.Length > width)
            {
                if (line.Length > 0) { r.Add(line.ToString()); line.Clear(); }
                r.Add(word[..width]);
                word = word[width..];
            }
            if (line.Length > 0 && line.Length + 1 + word.Length > width) { r.Add(line.ToString()); line.Clear(); }
            if (line.Length > 0) line.Append(' ');
            line.Append(word);
        }
        if (line.Length > 0 || r.Count == 0) r.Add(line.ToString());
        return r;
    }

    /// <summary>The game's files are Latin-1: anything else becomes its nearest plain letter, or ?</summary>
    public static string Latin1Safe(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.Normalize(NormalizationForm.FormC))
        {
            if (ch <= 0xFF) { sb.Append(ch); continue; }
            // Latin Extended-A (Polish, Czech, Turkish...) by table: the app runs without the system's Unicode data,
            // so taking the accent off by decomposing doesn't work
            if (ch is >= 'Ā' and <= 'ſ') { sb.Append(LatinA[ch - 0x100]); continue; }
            var plain = ch.ToString().Normalize(NormalizationForm.FormD).FirstOrDefault(x => x <= 0x7F);
            sb.Append(ch switch
            {
                '‘' or '’' => '\'', '“' or '”' => '"', '–' or '—' => '-', '…' => '.',
                _ => plain != default && char.IsLetterOrDigit(plain) ? plain : '?'
            });
        }
        return sb.ToString();
    }

    const string LatinA = "AaAaAaCcCcCcCcDdDdEeEeEeEeEeGgGgGgGgHhHhIiIiIiIiIiIiJjKkkLlLlLlLlLlNnNnNnnNnOoOoOoOoRrRrRrSsSsSsSsTtTtTtUuUuUuUuUuUuWwYyYZzZzZzs";

    static long SafeLength(string f) { try { return new FileInfo(f).Length; } catch (Exception) { return 0; } }

    static (int W, int H)? ImageSize(string f)
    {
        try
        {
            using var codec = SKCodec.Create(f);
            if (codec != null) return (codec.Info.Width, codec.Info.Height);
            using var b = Images.Load(f);
            return b == null ? null : (b.Width, b.Height);
        }
        catch (Exception) { return null; }
    }

    /// <summary>Written by us (or nothing there): safe to write over. Someone's own readme is kept as readme.bak first</summary>
    public static bool IsOurs(string path)
    {
        if (!File.Exists(path)) return true;
        try { return File.ReadAllText(path, ParamFile.FileEncoding).Contains("RV Car Studio"); } catch (Exception) { return false; }
    }
}
