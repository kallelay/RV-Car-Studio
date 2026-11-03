using System.IO.Compression;
using System.Text;

namespace RvStudio.Core;

/// <summary>The Re-Volt folder: finds files whatever the case in their path, which matters outside Windows</summary>
public sealed class GameFolder
{
    public readonly string Root;
    public GameFolder(string root) => Root = Path.GetFullPath(root);
    public string CarsDir => Path.Combine(Root, "cars");

    public static bool IsGameFolder(string? p) => !string.IsNullOrEmpty(p) && Directory.Exists(Path.Combine(p, "cars"));

    static readonly string[] Stock =
    {
        "adeon","amw","beatall","candy","cougar","dino","flag","fone","gencar","mite","moss","mouse","mud","panga","phat","q",
        "r5","rc","rotor","sgt","sugo","tc1","tc2","tc3","tc4","tc5","tc6","toyeca","trolley","ufo","volken","wincar","wincar2","wincar3","wincar4"
    };
    public static bool IsStock(string folder) => Stock.Contains(folder, StringComparer.OrdinalIgnoreCase);

    /// <summary>Full path of a path written in parameters.txt, null if there's no such file</summary>
    public string? Find(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = relative.Trim().Split('\\', '/', StringSplitOptions.RemoveEmptyEntries);
        var cur = Root;
        for (int i = 0; i < parts.Length; i++)
        {
            var next = Path.Combine(cur, parts[i]);
            bool last = i == parts.Length - 1;
            if (last ? File.Exists(next) : Directory.Exists(next)) { cur = next; continue; }
            if (!Directory.Exists(cur)) return null;
            var match = (last ? Directory.EnumerateFiles(cur) : Directory.EnumerateDirectories(cur))
                .FirstOrDefault(e => Path.GetFileName(e).Equals(parts[i], StringComparison.OrdinalIgnoreCase));
            if (match == null) return null;
            cur = match;
        }
        return cur;
    }

    public IEnumerable<CarEntry> ListCars()
    {
        if (!Directory.Exists(CarsDir)) yield break;
        foreach (var dir in Directory.GetDirectories(CarsDir).OrderBy(d => d, StringComparer.OrdinalIgnoreCase))
        {
            var p = Path.Combine(dir, "parameters.txt");
            if (!File.Exists(p)) continue;
            var folder = Path.GetFileName(dir);
            var name = ParamFile.Load(p).Main.Str("Name");
            yield return new CarEntry(folder, name == "" ? "(no name)" : name, IsStock(folder));
        }
    }
}

public sealed record CarEntry(string Folder, string Name, bool Stock);

public sealed class Car
{
    public readonly GameFolder Game;
    public string Folder { get; private set; }
    public string Dir => Path.Combine(Game.CarsDir, Folder);
    public string ParamPath => Path.Combine(Dir, "parameters.txt");

    public ParamFile File_ { get; private set; }
    public CarData Data { get; set; }
    List<ParamEntry> baseEntries;

    public Car(GameFolder game, string folder)
    {
        Game = game;
        Folder = folder;
        File_ = ParamFile.Load(ParamPath);
        Data = CarData.From(File_, GameFolder.IsStock(folder));
        baseEntries = Data.Emit();
    }

    public void LoadText(string text)
    {
        File_ = new ParamFile(text);
        Data = CarData.From(File_, GameFolder.IsStock(Folder));
        baseEntries = Data.Emit();
    }

    public bool Modified
    {
        get
        {
            var now = Data.Emit();
            if (now.Count != baseEntries.Count) return true;
            for (int i = 0; i < now.Count; i++) if (now[i].Value != baseEntries[i].Value) return true;
            return false;
        }
    }

    /// <summary>The file text with the current values. Unchanged values keep their exact text</summary>
    public string CurrentText()
    {
        if (Modified)
        {
            File_.Apply(baseEntries, Data.Emit());
            baseEntries = Data.Emit();
        }
        return File_.Text;
    }

    /// <summary>Save. The first version of the file stays next to it as .bak</summary>
    public void Save()
    {
        var content = CurrentText();
        Files.Backup(ParamPath);
        var tmp = ParamPath + ".tmp";
        File.WriteAllText(tmp, content, ParamFile.FileEncoding);
        File.Move(tmp, ParamPath, true);
    }

    // Paths from the old folder of the car to the new one
    void Repoint(string oldFolder, string newFolder)
    {
        var prefix = "cars\\" + oldFolder + "\\";
        string Fix(string p)
        {
            var q = p.Replace('/', '\\');
            return q.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? "cars\\" + newFolder + "\\" + q[prefix.Length..] : p;
        }
        var d = Data;
        for (int i = 0; i < d.Models.Length; i++) d.Models[i] = Fix(d.Models[i]);
        d.TPage = Fix(d.TPage); d.Coll = Fix(d.Coll); d.TCarBox = Fix(d.TCarBox); d.TShadow = Fix(d.TShadow);
        d.SfxEngine = Fix(d.SfxEngine); d.SfxHonk = Fix(d.SfxHonk); d.SfxServo = Fix(d.SfxServo);
    }

    public static bool ValidFolderName(string name) =>
        name.Trim() != "" && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && name.IndexOfAny(new[] { ';', '"', '\\', '/' }) < 0;

    public void Rename(string newFolder)
    {
        if (!ValidFolderName(newFolder)) throw new ArgumentException("That isn't a good folder name");
        var dest = Path.Combine(Game.CarsDir, newFolder);
        if (Directory.Exists(dest)) throw new IOException("There already is a folder " + newFolder);
        if (Modified) Save();
        Directory.Move(Dir, dest);
        var old = Folder;
        Folder = newFolder;
        Repoint(old, newFolder);
        Save();
    }

    /// <summary>A new car: a copy of another one, in its own folder, pointing to its own files</summary>
    public static Car CreateFrom(GameFolder game, string templateFolder, string newFolder, string newName)
    {
        if (!ValidFolderName(newFolder)) throw new ArgumentException("That isn't a good folder name");
        var from = Path.Combine(game.CarsDir, templateFolder);
        var dest = Path.Combine(game.CarsDir, newFolder);
        if (!Directory.Exists(from)) throw new DirectoryNotFoundException(from);
        if (Directory.Exists(dest)) throw new IOException("There already is a folder " + newFolder);
        Directory.CreateDirectory(dest);
        foreach (var f in Directory.GetFiles(from))
            if (!f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
                File.Copy(f, Path.Combine(dest, Path.GetFileName(f)));

        var c = new Car(game, newFolder);
        c.Repoint(templateFolder, newFolder);
        c.Data.Name = newName;
        c.CurrentText();
        File.WriteAllText(c.ParamPath, c.File_.Text, ParamFile.FileEncoding);
        return c;
    }

    /// <summary>Skins are found by name: car.bmp gives caror.bmp, car1.bmp, car-bleh.bmp (RVGL)</summary>
    public List<string> FindSkins()
    {
        var r = new List<string>();
        var tex = Game.Find(Data.TPage);
        if (tex == null) return r;
        var stem = Path.GetFileNameWithoutExtension(tex);
        var box = Data.TCarBox == "" ? "" : Path.GetFileNameWithoutExtension(Data.TCarBox.Replace('\\', '/'));
        foreach (var f in Directory.GetFiles(Path.GetDirectoryName(tex)!).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            if (!Path.GetExtension(f).Equals(".bmp", StringComparison.OrdinalIgnoreCase)) continue;
            var n = Path.GetFileNameWithoutExtension(f);
            if (!n.StartsWith(stem, StringComparison.OrdinalIgnoreCase) || n.Length - stem.Length > 11) continue;
            if (box != "" && n.StartsWith(box, StringComparison.OrdinalIgnoreCase)) continue;
            r.Add(f);
        }
        return r;
    }

    /// <summary>Files of other folders this car needs: not in its own and not in cars\misc</summary>
    public List<string> ExternalFiles()
    {
        var r = new List<string>();
        var own = "cars\\" + Folder + "\\";
        var d = Data;
        foreach (var p in d.Models.Concat(new[] { d.TPage, d.Coll, d.TCarBox, d.TShadow, d.SfxEngine, d.SfxHonk, d.SfxServo }))
        {
            if (string.IsNullOrWhiteSpace(p) || p.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) continue;
            var rel = p.Trim().Replace('/', '\\');
            if (rel.StartsWith(own, StringComparison.OrdinalIgnoreCase) || rel.StartsWith("cars\\misc\\", StringComparison.OrdinalIgnoreCase)) continue;
            if (!r.Contains(rel, StringComparer.OrdinalIgnoreCase)) r.Add(rel);
        }
        return r;
    }

    /// <summary>Zip to unzip on the main Re-Volt folder</summary>
    public void Pack(string zipPath, bool withExternal)
    {
        if (File.Exists(zipPath)) File.Delete(zipPath);
        using var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create);
        foreach (var f in Directory.GetFiles(Dir, "*", SearchOption.AllDirectories))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is ".bak" or ".tmp") continue;
            zip.CreateEntryFromFile(f, "cars/" + Folder + "/" + Path.GetRelativePath(Dir, f).Replace('\\', '/'));
        }
        if (!withExternal) return;
        foreach (var rel in ExternalFiles())
            if (Game.Find(rel) is { } full)
                zip.CreateEntryFromFile(full, rel.Replace('\\', '/'));
    }
}

public static class Files
{
    public static void Backup(string path)
    {
        if (File.Exists(path) && !File.Exists(path + ".bak")) File.Copy(path, path + ".bak");
    }
}
