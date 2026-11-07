using System.IO.Compression;
using System.Text;

namespace RvStudio.Core;

/// <summary>
/// The Re-Volt folder as RVGL sees it. A packlist (packs\default.txt) stacks folders: the root, then packs, later ones
/// win. Stock cars usually live in packs, and what's in the root's cars folder under the same name is then ignored.
/// Packs are read-only except one, normally "local", where edits to anything in a pack go.
/// </summary>
public sealed class GameFolder
{
    public readonly string Root;
    /// <summary>Folders the game reads, lowest first</summary>
    public readonly IReadOnlyList<string> Layers;
    /// <summary>Where edits of things in read-only packs go</summary>
    public readonly string WritableLayer;
    public string CarsDir => Path.Combine(Root, "cars");

    public GameFolder(string root)
    {
        Root = Path.GetFullPath(root);
        var layers = new List<string>();
        string? writable = null;
        var list = Path.Combine(Root, "packs", "default.txt");
        if (File.Exists(list))
            foreach (var raw in File.ReadAllLines(list))
            {
                var line = raw.Trim();
                if (line == "" || line.StartsWith(';')) continue;
                bool star = line.EndsWith('*');
                if (star) line = line[..^1].Trim();
                var name = line.Trim('"');
                var dir = name.Equals("default", StringComparison.OrdinalIgnoreCase) ? Root : Path.Combine(Root, "packs", name);
                layers.Add(dir);
                if (star) writable = dir;
            }
        if (layers.Count == 0) layers.Add(Root);
        Layers = layers;
        WritableLayer = writable ?? (layers.Count == 1 && layers[0] == Root ? Root : Path.Combine(Root, "packs", "local"));
    }

    public static bool IsGameFolder(string? p) => !string.IsNullOrEmpty(p) &&
        (Directory.Exists(Path.Combine(p, "cars")) || Directory.Exists(Path.Combine(p, "packs")));

    static readonly string[] Stock =
    {
        "adeon","amw","beatall","candy","cougar","dino","flag","fone","gencar","mite","moss","mouse","mud","panga","phat","q",
        "r5","rc","rotor","sgt","sugo","tc1","tc2","tc3","tc4","tc5","tc6","toyeca","trolley","ufo","volken","wincar","wincar2","wincar3","wincar4"
    };
    public static bool IsStock(string folder) => Stock.Contains(folder, StringComparer.OrdinalIgnoreCase);

    // A path in one folder, whatever the case: Linux needs that
    static string? FindIn(string layer, string[] parts)
    {
        var cur = layer;
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

    /// <summary>The file the game would read for a path written in parameters.txt, null if there's none</summary>
    public string? Find(string? relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) return null;
        var parts = relative.Trim().Split('\\', '/', StringSplitOptions.RemoveEmptyEntries);
        for (int i = Layers.Count - 1; i >= 0; i--)
            if (FindIn(Layers[i], parts) is { } f) return f;
        return null;
    }

    /// <summary>A file as the game names it: from the top of whichever folder it's in. Null if it's outside them all</summary>
    public string? RelOf(string fullPath)
    {
        var full = Path.GetFullPath(fullPath);
        foreach (var layer in Layers.Reverse().Append(Root))
            if (full.StartsWith(layer + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                return Path.GetRelativePath(layer, full).Replace('/', '\\');
        return null;
    }

    static bool IsIn(string file, string dir) => file.StartsWith(dir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

    /// <summary>The folder a car is read from. Null if there's no such car</summary>
    public string? CarLayer(string folder)
    {
        for (int i = Layers.Count - 1; i >= 0; i--)
            if (FindIn(Layers[i], new[] { "cars", folder }) != null || Directory.Exists(Path.Combine(Layers[i], "cars", folder))) return Layers[i];
        return null;
    }

    /// <summary>Where a car's changes are written: where it lives, unless that's a read-only pack</summary>
    public string CarHome(string folder)
    {
        var layer = CarLayer(folder);
        if (layer == null || layer == Root || layer == WritableLayer) return Path.Combine(layer ?? Root, "cars", folder);
        return Path.Combine(WritableLayer, "cars", folder);
    }

    /// <summary>Where to write a file so the game reads it. Files in read-only packs are never touched</summary>
    public string WritePath(string relative)
    {
        var rel = relative.Trim().Replace('/', '\\');
        var cur = Find(rel);
        if (cur != null && (IsIn(cur, WritableLayer) || (IsIn(cur, Root) && !IsInPack(cur)))) return cur;
        var parts = rel.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        string path;
        if (parts.Length > 2 && parts[0].Equals("cars", StringComparison.OrdinalIgnoreCase))
            path = Path.Combine(new[] { CarHome(parts[1]) }.Concat(parts.Skip(2)).ToArray());
        else
            path = Path.Combine(new[] { WritableLayer }.Concat(parts).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    bool IsInPack(string file) => IsIn(file, Path.Combine(Root, "packs"));

    /// <summary>Make sure there's a copy that can be changed, and give it</summary>
    public string EnsureWritable(string relative)
    {
        var cur = Find(relative);
        var target = WritePath(relative);
        if (cur != null && !string.Equals(cur, target, StringComparison.OrdinalIgnoreCase)) File.Copy(cur, target, true);
        return target;
    }

    public IEnumerable<string> CarFolders()
    {
        var seen = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in Layers)
        {
            var dir = Path.Combine(layer, "cars");
            if (!Directory.Exists(dir)) continue;
            foreach (var d in Directory.GetDirectories(dir)) seen.Add(Path.GetFileName(d));
        }
        return seen;
    }

    /// <summary>Every file of a car folder as the game sees it: path as written in parameters.txt, then the real file</summary>
    public Dictionary<string, string> CarFiles(string folder)
    {
        var r = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in Layers)
        {
            var dir = Path.Combine(layer, "cars", folder);
            if (!Directory.Exists(dir)) continue;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                r["cars\\" + folder + "\\" + Path.GetRelativePath(dir, f).Replace('/', '\\')] = f;
        }
        return r;
    }

    public IEnumerable<CarEntry> ListCars()
    {
        foreach (var folder in CarFolders())
        {
            var p = Find("cars\\" + folder + "\\parameters.txt");
            if (p == null) continue;
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
    /// <summary>Where this car's changes are written</summary>
    public string Dir => Game.CarHome(Folder);
    string ParamRel => "cars\\" + Folder + "\\parameters.txt";
    /// <summary>The file it's read from, or will be written to</summary>
    public string ParamPath => Game.Find(ParamRel) ?? Path.Combine(Dir, "parameters.txt");
    /// <summary>Saved changes of a car that lives in a read-only pack</summary>
    public bool HasLocalChanges => !Game.WritableLayer.Equals(Game.Root, StringComparison.OrdinalIgnoreCase) && Directory.Exists(Path.Combine(Game.WritableLayer, "cars", Folder));

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
        var target = Game.WritePath(ParamRel);
        Files.Backup(target);
        var tmp = target + ".tmp";
        File.WriteAllText(tmp, content, ParamFile.FileEncoding);
        File.Move(tmp, target, true);
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
        if (Game.CarLayer(newFolder) != null) throw new IOException("There already is a car " + newFolder);
        if (Game.CarLayer(Folder) != Game.Root) throw new InvalidOperationException("This car lives in a pack. Make a copy instead");
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
        var dest = Path.Combine(game.CarsDir, newFolder);
        var files = game.CarFiles(templateFolder);
        if (files.Count == 0) throw new DirectoryNotFoundException(templateFolder);
        if (game.CarLayer(newFolder) != null) throw new IOException("There already is a car " + newFolder);
        Directory.CreateDirectory(dest);
        var prefix = "cars\\" + templateFolder + "\\";
        foreach (var (rel, f) in files)
        {
            if (f.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)) continue;
            var to = Path.Combine(dest, rel[prefix.Length..].Replace('\\', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(to)!);
            File.Copy(f, to);
        }

        var c = new Car(game, newFolder);
        c.Repoint(templateFolder, newFolder);
        c.Data.Name = newName;
        c.CurrentText();
        File.WriteAllText(c.ParamPath, c.File_.Text, ParamFile.FileEncoding);
        return c;
    }

    /// <summary>
    /// A copy of this car in a new folder, with the changes made so far. The first version stays as it is on disk.
    /// </summary>
    public Car Fork(string newFolder, string newName)
    {
        var c = CreateFrom(Game, Folder, newFolder, newName);
        var d = Data.Clone();
        var old = c.Data;
        c.Data = d;
        c.Repoint(Folder, newFolder);
        d.Name = newName;
        c.File_.Apply(old.Emit(), d.Emit());
        c.baseEntries = d.Emit();
        File.WriteAllText(c.ParamPath, c.File_.Text, ParamFile.FileEncoding);
        return c;
    }

    static string Norm(string p) => p.Trim().Replace('/', '\\');

    /// <summary>Is this file outside this car's own folder?</summary>
    public bool IsForeign(string rel) =>
        !Norm(rel).StartsWith("cars\\" + Folder + "\\", StringComparison.OrdinalIgnoreCase);

    /// <summary>Other cars whose parameters point to this file</summary>
    public List<string> OtherUsers(string rel)
    {
        var r = new List<string>();
        var want = Norm(rel);
        foreach (var name in Game.CarFolders())
        {
            if (name.Equals(Folder, StringComparison.OrdinalIgnoreCase)) continue;
            var p = Game.Find("cars\\" + name + "\\parameters.txt");
            if (p == null) continue;
            var f = ParamFile.Load(p);
            bool uses = f.Main.Str("COLL").Trim().Replace('/', '\\').Equals(want, StringComparison.OrdinalIgnoreCase);
            for (int i = 0; !uses && i < 19; i++)
                uses = f.Main.Str("MODEL " + i).Trim().Replace('/', '\\').Equals(want, StringComparison.OrdinalIgnoreCase);
            if (uses) r.Add(name);
        }
        return r;
    }

    /// <summary>Copy a file into this car's folder and give its new path</summary>
    public string CopyToOwn(string rel)
    {
        var src = Game.Find(rel) ?? throw new FileNotFoundException(rel);
        var name = Path.GetFileName(src);
        Directory.CreateDirectory(Dir);
        var dest = Path.Combine(Dir, name);
        if (File.Exists(dest) && !File.ReadAllBytes(dest).SequenceEqual(File.ReadAllBytes(src)))
        {
            var stem = Path.GetFileNameWithoutExtension(name);
            var ext = Path.GetExtension(name);
            int n = 1;
            do dest = Path.Combine(Dir, stem + "_" + n++ + ext); while (File.Exists(dest));
        }
        if (!File.Exists(dest)) File.Copy(src, dest);
        return "cars\\" + Folder + "\\" + Path.GetFileName(dest);
    }

    /// <summary>Forget saved changes of a car in a read-only pack: it goes back to what the pack has</summary>
    public void DropLocalChanges()
    {
        var dir = Path.Combine(Game.WritableLayer, "cars", Folder);
        if (Directory.Exists(dir)) Directory.Delete(dir, true);
    }

    /// <summary>Skins are found by name: car.bmp gives caror.bmp, car1.bmp, car-bleh.bmp (RVGL)</summary>
    public List<string> FindSkins()
    {
        var r = new List<string>();
        var tex = Game.Find(Data.TPage);
        if (tex == null) return r;
        var stem = Path.GetFileNameWithoutExtension(tex);
        var box = Data.TCarBox == "" ? "" : Path.GetFileNameWithoutExtension(Data.TCarBox.Replace('\\', '/'));
        var texDir = Path.GetDirectoryName((Game.RelOf(tex) ?? "").Replace('\\', '/'))?.Replace('/', '\\') + "\\";
        foreach (var f in Game.CarFiles(Folder).Where(kv => kv.Key.StartsWith(texDir, StringComparison.OrdinalIgnoreCase) && kv.Key.LastIndexOf('\\') == texDir.Length - 1)
                     .Select(kv => kv.Value).OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
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
        foreach (var (rel, f) in Game.CarFiles(Folder))
        {
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is ".bak" or ".tmp") continue;
            zip.CreateEntryFromFile(f, rel.Replace('\\', '/'));
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
