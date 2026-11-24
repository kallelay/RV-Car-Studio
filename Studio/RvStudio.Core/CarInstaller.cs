using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace RvStudio.Core;

public enum InstallLevel { Note, Warning, Error }

public sealed record InstallIssue(InstallLevel Level, string Text);

/// <summary>One file of a zip or folder somebody gave us. Rel uses \ and has been checked: no "..", nothing absolute</summary>
public sealed class SourceFile
{
    public required string Rel { get; init; }
    public required Func<Stream> Open { get; init; }
    public long Length { get; init; }
}

/// <summary>A car found in a zip or folder</summary>
public sealed class FoundCar
{
    /// <summary>Where it sits in the zip, "" for the top</summary>
    public string SourceDir = "";
    /// <summary>Name from parameters.txt</summary>
    public string Name = "";
    /// <summary>The folder it asks for</summary>
    public string OriginalFolder = "";
    /// <summary>The folder it will get. Differs from OriginalFolder when that one is taken</summary>
    public string Folder = "";
    /// <summary>Its own files, path inside the car folder</summary>
    public readonly Dictionary<string, SourceFile> Files = new(StringComparer.OrdinalIgnoreCase);
    public string ParamText = "";

    // Filled by CarInstaller.Review
    /// <summary>OriginalFolder is taken: by a car of the game, a stock car, or another car of the same zip</summary>
    public bool Clash;
    public readonly List<InstallIssue> Issues = new();
    /// <summary>Paths of parameters.txt that will be written differently: key, old, new</summary>
    public readonly List<(string Key, string From, string To)> Rewrites = new();
    /// <summary>Files outside the car folder it needs, the game doesn't have, and the zip brings: game path, file</summary>
    public readonly Dictionary<string, SourceFile> Extras = new(StringComparer.OrdinalIgnoreCase);
    public bool CanInstall => !Issues.Any(i => i.Level == InstallLevel.Error);
}

/// <summary>What's in a zip or folder, read without writing anything</summary>
public sealed class InstallPlan : IDisposable
{
    public string Source = "";
    public readonly List<FoundCar> Cars = new();
    /// <summary>Files that belong to no car: readme, levels, sounds. They aren't installed</summary>
    public readonly List<SourceFile> Loose = new();
    /// <summary>Entries that would land outside the game folder, or have names Windows can't hold. Never written</summary>
    public readonly List<string> Rejected = new();
    public readonly List<InstallIssue> Issues = new();
    internal ZipArchive? Zip;

    public void Dispose() { Zip?.Dispose(); Zip = null; }

    /// <summary>The report shown before installing</summary>
    public string Describe()
    {
        var sb = new StringBuilder();
        sb.Append(Names.File(Source)).Append(": ");
        sb.AppendLine(Cars.Count == 0 ? "no car found" : Cars.Count == 1 ? "1 car" : Cars.Count + " cars");
        foreach (var c in Cars)
        {
            sb.AppendLine();
            sb.Append(c.Name == "" ? "(no name)" : c.Name).Append("  ->  cars\\").Append(c.Folder);
            sb.Append("   (").Append(c.Files.Count + c.Extras.Count).Append(" files, ").Append(Size(c.Files.Values.Concat(c.Extras.Values).Sum(f => f.Length))).AppendLine(")");
            foreach (var i in c.Issues.OrderByDescending(i => i.Level)) sb.Append("  ").Append(Mark(i.Level)).AppendLine(i.Text);
            if (c.Issues.Count == 0) sb.AppendLine("  Nothing wrong found");
        }
        if (Issues.Count > 0) sb.AppendLine();
        foreach (var i in Issues.OrderByDescending(i => i.Level)) sb.Append(Mark(i.Level)).AppendLine(i.Text);
        return sb.ToString();
    }

    static string Mark(InstallLevel l) => l switch { InstallLevel.Error => "Problem: ", InstallLevel.Warning => "Careful: ", _ => "" };
    static string Size(long bytes) => bytes < 1024 * 1024 ? Math.Max(1, bytes / 1024) + " KB" : (bytes / 1048576.0).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture) + " MB";
}

/// <summary>What an install put on disk, kept in the game folder so it can be taken out again</summary>
public sealed class InstallRecord
{
    public string File_ = "";
    public string When = "";
    public string Source = "";
    public readonly List<string> Cars = new();
    /// <summary>Folders it made, from the game folder, outermost first</summary>
    public readonly List<string> Dirs = new();
    /// <summary>Files it made, from the game folder, with their SHA-256</summary>
    public readonly List<(string Rel, string Hash)> Files = new();
}

public sealed class UndoResult
{
    public InstallRecord Record = new();
    public int Deleted;
    /// <summary>Files changed since the install: kept unless asked otherwise</summary>
    public readonly List<string> Changed = new();
    /// <summary>Folders that still hold something the install didn't make</summary>
    public readonly List<string> DirsKept = new();
}

/// <summary>
/// Puts a downloaded car into the game: looks first, then copies into the root's cars folder. It never replaces a
/// file, never writes outside the game folder, and notes what it made so it can be undone.
/// </summary>
public static class CarInstaller
{
    public const string RecordDir = ".rvcarstudio\\installs";
    const long MaxBytes = 1L << 30; // one car bigger than this isn't a car

    static readonly string[] PathKeys = Enumerable.Range(0, 19).Select(i => "MODEL " + i)
        .Concat(new[] { "TPAGE", "COLL", "TCARBOX", "TSHADOW", "SFXENGINE", "SFXHONK", "SFXSERVO" }).ToArray();

    static readonly string[] Devices = { "con", "prn", "aux", "nul", "com1", "com2", "com3", "com4", "com5", "com6", "com7", "com8", "com9", "lpt1", "lpt2", "lpt3", "lpt4", "lpt5", "lpt6", "lpt7", "lpt8", "lpt9" };

    // ------------------------------------------------------------ names

    static bool SafePart(string p) =>
        p != "" && p != "." && p != ".." && p.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 &&
        p.IndexOfAny(new[] { ':', '*', '?', '"', '<', '>', '|', '\\', '/' }) < 0 && !p.Any(char.IsControl) &&
        !p.EndsWith(' ') && !p.EndsWith('.') &&
        !Devices.Contains(p.Split('.')[0].Trim(), StringComparer.OrdinalIgnoreCase);

    /// <summary>An entry name as a path under the place it's unpacked to. Null if it would leave it</summary>
    public static string? SafeRel(string entryName)
    {
        var n = entryName.Replace('/', '\\');
        if (n.StartsWith('\\') || n.Contains(':')) return null; // absolute, a drive, or a stream of another file
        var parts = n.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || !parts.All(SafePart)) return null;
        return string.Join('\\', parts);
    }

    public static bool ValidFolder(string name) => Car.ValidFolderName(name) && SafePart(name) && name == name.Trim();

    static bool Junk(string rel)
    {
        var parts = rel.Split('\\');
        var file = parts[^1];
        return parts.Contains("__MACOSX", StringComparer.OrdinalIgnoreCase) || file.StartsWith("._") ||
               file.Equals(".DS_Store", StringComparison.OrdinalIgnoreCase) || file.Equals("Thumbs.db", StringComparison.OrdinalIgnoreCase) ||
               file.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase);
    }

    static string Norm(string p) => string.Join('\\', p.Trim().Replace('/', '\\').Split('\\', StringSplitOptions.RemoveEmptyEntries));

    static bool Taken(GameFolder game, string folder) =>
        GameFolder.IsStock(folder) || game.CarLayer(folder) != null ||
        game.Layers.Append(game.Root).Any(l => Directory.Exists(Path.Combine(l, "cars")) &&
            Directory.EnumerateFileSystemEntries(Path.Combine(l, "cars")).Any(e => Names.File(e).Equals(folder, StringComparison.OrdinalIgnoreCase)));

    /// <summary>A folder name nothing uses yet: name, name2, name3...</summary>
    public static string FreeFolder(GameFolder game, string wanted, IEnumerable<string>? alsoTaken = null)
    {
        var others = new HashSet<string>(alsoTaken ?? Array.Empty<string>(), StringComparer.OrdinalIgnoreCase);
        var stem = wanted.Trim();
        if (!ValidFolder(stem)) stem = new string(stem.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-').ToArray());
        if (!ValidFolder(stem)) stem = "newcar";
        if (!Taken(game, stem) && !others.Contains(stem)) return stem;
        for (int n = 2; ; n++)
            if (!Taken(game, stem + n) && !others.Contains(stem + n)) return stem + n;
    }

    // ------------------------------------------------------------ looking

    /// <summary>Read a zip or a folder. Nothing is written. Dispose the plan when done: it keeps the zip open</summary>
    public static InstallPlan Inspect(GameFolder game, string path)
    {
        var plan = new InstallPlan { Source = Path.GetFullPath(path) };
        var files = new List<SourceFile>();
        try
        {
            if (Directory.Exists(path))
            {
                var root = Path.GetFullPath(path);
                foreach (var f in Directory.EnumerateFiles(root, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = FileAttributes.ReparsePoint }))
                {
                    var rel = SafeRel(Path.GetRelativePath(root, f));
                    if (rel == null) { plan.Rejected.Add(Path.GetRelativePath(root, f)); continue; }
                    var full = f;
                    files.Add(new SourceFile { Rel = rel, Open = () => File.OpenRead(full), Length = new FileInfo(f).Length });
                }
            }
            else if (File.Exists(path))
            {
                plan.Zip = ZipFile.OpenRead(path);
                foreach (var e in plan.Zip.Entries)
                {
                    if (e.FullName.EndsWith('/') || e.FullName.EndsWith('\\')) continue; // a folder
                    var rel = SafeRel(e.FullName);
                    if (rel == null) { plan.Rejected.Add(e.FullName); continue; }
                    var entry = e;
                    files.Add(new SourceFile { Rel = rel, Open = () => entry.Open(), Length = e.Length });
                }
            }
            else
            {
                plan.Issues.Add(new(InstallLevel.Error, "There's no such file or folder"));
                return plan;
            }
        }
        catch (Exception ex) when (ex is InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            plan.Dispose();
            plan.Issues.Add(new(InstallLevel.Error, "Can't read it: " + ex.Message + (Directory.Exists(path) ? "" : " Only zip files can be read: unpack other kinds first and give the folder")));
            return plan;
        }

        // The same path twice (a zip can do that): the first one counts
        files = files.Where(f => !Junk(f.Rel)).GroupBy(f => f.Rel, StringComparer.OrdinalIgnoreCase).Select(g => g.First()).ToList();

        // Every folder with a parameters.txt is a car. A file belongs to the deepest car folder above it
        var carDirs = files.Where(f => Names.File(f.Rel).Equals("parameters.txt", StringComparison.OrdinalIgnoreCase))
            .Select(f => f.Rel.Contains('\\') ? f.Rel[..f.Rel.LastIndexOf('\\')] : "")
            .Distinct(StringComparer.OrdinalIgnoreCase).OrderByDescending(d => d.Length).ToList();
        var cars = carDirs.ToDictionary(d => d, d => new FoundCar { SourceDir = d }, StringComparer.OrdinalIgnoreCase);
        foreach (var f in files)
        {
            var dir = carDirs.FirstOrDefault(d => d == "" || f.Rel.StartsWith(d + "\\", StringComparison.OrdinalIgnoreCase));
            if (dir == null) plan.Loose.Add(f);
            else cars[dir].Files[dir == "" ? f.Rel : f.Rel[(dir.Length + 1)..]] = f;
        }

        foreach (var c in cars.Values.OrderBy(c => c.SourceDir, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var pf = c.Files["parameters.txt"];
                if (pf.Length > 4 << 20) throw new InvalidDataException("parameters.txt is far too big");
                using var s = pf.Open();
                using var r = new StreamReader(s, ParamFile.FileEncoding);
                c.ParamText = r.ReadToEnd();
            }
            catch (Exception ex) when (ex is InvalidDataException or IOException)
            {
                plan.Issues.Add(new(InstallLevel.Error, "Can't read " + (c.SourceDir == "" ? "" : c.SourceDir + "\\") + "parameters.txt: " + ex.Message));
                continue;
            }
            var main = new ParamFile(c.ParamText).Main;
            c.Name = main.Str("Name");
            c.OriginalFolder = WantedFolder(c, main, plan.Source);
            plan.Cars.Add(c);
        }

        Review(game, plan);
        return plan;
    }

    // The folder the car means to live in
    static string WantedFolder(FoundCar c, ParamSection main, string source)
    {
        var parts = c.SourceDir.Split('\\', StringSplitOptions.RemoveEmptyEntries);
        // cars\<name>\ in the zip says it outright
        if (parts.Length >= 2 && parts[^2].Equals("cars", StringComparison.OrdinalIgnoreCase) && ValidFolder(parts[^1])) return parts[^1];
        // Otherwise the folder its own paths name, when the files they name are here
        var declared = PathKeys.Select(k => Norm(main.Str(k)).Split('\\'))
            .Where(p => p.Length >= 3 && p[0].Equals("cars", StringComparison.OrdinalIgnoreCase) && !p[1].Equals("misc", StringComparison.OrdinalIgnoreCase)
                        && ValidFolder(p[1]) && c.Files.ContainsKey(string.Join('\\', p.Skip(2))))
            .GroupBy(p => p[1], StringComparer.OrdinalIgnoreCase).OrderByDescending(g => g.Count()).FirstOrDefault()?.Key;
        if (declared != null) return declared;
        if (parts.Length > 0 && ValidFolder(parts[^1])) return parts[^1];
        var stem = Names.Stem(source.TrimEnd('\\', '/'));
        if (Directory.Exists(source)) stem = Names.File(source.TrimEnd('\\', '/'));
        return ValidFolder(stem) ? stem : "newcar";
    }

    /// <summary>Change the folder a car will get, and look at it again</summary>
    public static void SetFolder(GameFolder game, InstallPlan plan, FoundCar car, string folder)
    {
        car.Folder = folder.Trim();
        Review(game, plan, keepFolders: true);
    }

    /// <summary>Work out folder names, path changes and problems of every car in the plan</summary>
    public static void Review(GameFolder game, InstallPlan plan, bool keepFolders = false)
    {
        plan.Issues.RemoveAll(i => i.Level != InstallLevel.Error || i.Text.StartsWith("Left out"));
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var extrasUsed = new HashSet<SourceFile>();

        foreach (var c in plan.Cars)
        {
            c.Issues.Clear(); c.Rewrites.Clear(); c.Extras.Clear();
            c.Clash = Taken(game, c.OriginalFolder) || used.Contains(c.OriginalFolder);
            if (!keepFolders || c.Folder == "") c.Folder = FreeFolder(game, c.OriginalFolder, used);

            if (!ValidFolder(c.Folder)) c.Issues.Add(new(InstallLevel.Error, "'" + c.Folder + "' can't be a folder name"));
            else if (Taken(game, c.Folder)) c.Issues.Add(new(InstallLevel.Error, "There already is a car in cars\\" + c.Folder + (GameFolder.IsStock(c.Folder) ? " (a stock car)" : "") + ". Give it another folder name"));
            else if (!used.Add(c.Folder)) c.Issues.Add(new(InstallLevel.Error, "Two cars here want the folder " + c.Folder));

            if (c.Clash && !c.Folder.Equals(c.OriginalFolder, StringComparison.OrdinalIgnoreCase))
                c.Issues.Add(new(InstallLevel.Warning, "cars\\" + c.OriginalFolder + " is " + (GameFolder.IsStock(c.OriginalFolder) ? "a stock car" : "taken") +
                    ". It goes into cars\\" + c.Folder + " instead and its paths are changed to match. The car that's there isn't touched"));

            if (c.Files.Values.Sum(f => f.Length) > MaxBytes) c.Issues.Add(new(InstallLevel.Error, "Over 1 GB: that isn't a car"));
            if (c.Name == "") c.Issues.Add(new(InstallLevel.Warning, "parameters.txt has no Name"));

            var main = new ParamFile(c.ParamText).Main;
            var own = "cars\\" + c.Folder + "\\";
            var wrongFolders = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
            var missing = new List<string>();
            var shared = new List<string>();
            var foreign = new List<string>();
            bool anyModel = false;

            foreach (var key in PathKeys)
            {
                var raw = main.Str(key);
                if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) continue;
                var p = Norm(raw);
                var parts = p.Split('\\');
                if (key.StartsWith("MODEL")) anyModel = true;
                if (parts.Contains("..") || p.Contains(':')) { missing.Add(key + " " + raw + " (not a path inside the game)"); continue; }
                bool inCars = parts.Length >= 3 && parts[0].Equals("cars", StringComparison.OrdinalIgnoreCase);
                var rest = inCars ? string.Join('\\', parts.Skip(2)) : "";

                if (inCars && c.Files.ContainsKey(rest) && !parts[1].Equals("misc", StringComparison.OrdinalIgnoreCase))
                {
                    // Its own file. The path has to name the folder the car really gets
                    if (!parts[1].Equals(c.Folder, StringComparison.OrdinalIgnoreCase))
                    {
                        c.Rewrites.Add((key, raw, own + rest));
                        if (!parts[1].Equals(c.OriginalFolder, StringComparison.OrdinalIgnoreCase)) wrongFolders.Add(parts[1]);
                    }
                    continue;
                }
                if (game.Find(p) != null)
                {
                    if (inCars && parts[1].Equals("misc", StringComparison.OrdinalIgnoreCase)) shared.Add(parts[^1]);
                    else if (inCars && !parts[1].Equals(c.Folder, StringComparison.OrdinalIgnoreCase)) foreign.Add(p);
                    continue;
                }
                // Not the car's, not in the game: does the zip bring it along, outside the car folder?
                var extra = inCars ? plan.Loose.FirstOrDefault(f => f.Rel.Equals(p, StringComparison.OrdinalIgnoreCase) || f.Rel.EndsWith("\\" + p, StringComparison.OrdinalIgnoreCase)) : null;
                if (extra != null && SafeRel(p) != null && !parts[1].Equals(c.Folder, StringComparison.OrdinalIgnoreCase)) { c.Extras[p] = extra; extrasUsed.Add(extra); }
                else missing.Add(key + " " + raw);
            }

            if (wrongFolders.Count > 0)
                c.Issues.Add(new(InstallLevel.Warning, "Its paths point to cars\\" + string.Join(", cars\\", wrongFolders) + " but it goes into cars\\" + c.Folder +
                    ": the game wouldn't find its files. " + c.Rewrites.Count + " paths in parameters.txt are changed to match"));
            else if (c.Rewrites.Count > 0 && !c.Clash)
                c.Issues.Add(new(InstallLevel.Note, c.Rewrites.Count + " paths in parameters.txt are changed to cars\\" + c.Folder));
            if (!anyModel) c.Issues.Add(new(InstallLevel.Warning, "parameters.txt names no model: this may not be a car"));
            if (missing.Count > 0)
                c.Issues.Add(new(InstallLevel.Warning, missing.Count + (missing.Count == 1 ? " file it names is missing: " : " files it names are missing: ") + Few(missing, 6)));
            if (shared.Count > 0)
                c.Issues.Add(new(InstallLevel.Note, "Uses shared models from cars\\misc: " + Few(shared.Distinct(StringComparer.OrdinalIgnoreCase), 6)));
            if (foreign.Count > 0)
                c.Issues.Add(new(InstallLevel.Note, "Uses files of other cars, which your game has: " + Few(foreign.Distinct(StringComparer.OrdinalIgnoreCase), 6)));
            if (c.Extras.Count > 0)
                c.Issues.Add(new(InstallLevel.Note, "Also installs files it needs that your game doesn't have: " + Few(c.Extras.Keys, 6)));
            foreach (var (rel, _) in c.Extras)
                if (File.Exists(Path.Combine(game.Root, rel))) c.Issues.Add(new(InstallLevel.Error, rel + " exists already"));
        }

        if (plan.Cars.Count == 0 && !plan.Issues.Any(i => i.Level == InstallLevel.Error))
            plan.Issues.Add(new(InstallLevel.Error, "No car in here: no folder has a parameters.txt"));
        if (plan.Rejected.Count > 0)
            plan.Issues.Add(new(InstallLevel.Error, "Left out, because " + (plan.Rejected.Count == 1 ? "it" : "they") + " would be written outside the game folder or can't be a file name: " + Few(plan.Rejected, 6)));
        var loose = plan.Loose.Where(f => !extrasUsed.Contains(f)).ToList();
        if (loose.Count > 0 && plan.Cars.Count > 0)
            plan.Issues.Add(new(InstallLevel.Note, loose.Count + (loose.Count == 1 ? " file isn't part of a car and isn't installed: " : " files aren't part of a car and aren't installed: ") + LooseSummary(loose)));
    }

    static string Few(IEnumerable<string> items, int max)
    {
        var l = items.ToList();
        return string.Join(", ", l.Take(max)) + (l.Count > max ? " and " + (l.Count - max) + " more" : "");
    }

    // readme.txt, levels\ (40 files), wavs\ (2 files)
    static string LooseSummary(List<SourceFile> loose)
    {
        // A folder wrapping everything says nothing: look below it
        string Strip(string rel) { var i = rel.IndexOf('\\'); return i < 0 ? rel : rel[(i + 1)..]; }
        var rels = loose.Select(f => f.Rel).ToList();
        while (rels.Count > 1 && rels.All(r => r.Contains('\\')) && rels.Select(r => r[..r.IndexOf('\\')]).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1)
            rels = rels.Select(Strip).ToList();
        var groups = rels.GroupBy(r => r.Contains('\\') ? r[..r.IndexOf('\\')] + "\\" : r, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Key.EndsWith('\\') ? g.Key + " (" + g.Count() + (g.Count() == 1 ? " file)" : " files)") : g.Key);
        return Few(groups, 8);
    }

    // ------------------------------------------------------------ installing

    /// <summary>parameters.txt as it will be written: only the paths that have to change are touched</summary>
    public static string RewrittenParams(FoundCar c)
    {
        if (c.Rewrites.Count == 0) return c.ParamText;
        var file = new ParamFile(c.ParamText);
        var before = new List<ParamEntry>();
        var now = new List<ParamEntry>();
        foreach (var (key, from, to) in c.Rewrites)
        {
            var line = file.Main.Find(key);
            if (line == null) continue;
            int at = line.Value.IndexOf(from, StringComparison.Ordinal);
            var value = at < 0 ? "\"" + to + "\"" : line.Value[..at] + to + line.Value[(at + from.Length)..];
            before.Add(new ParamEntry("", key, line.Value, line.Ext));
            now.Add(new ParamEntry("", key, value, line.Ext));
        }
        file.Apply(before, now);
        return file.Text;
    }

    static string Inside(string root, string rel)
    {
        if (SafeRel(rel) == null) throw new InvalidOperationException("Not a path inside the game folder: " + rel);
        var full = Path.GetFullPath(Path.Combine(root, rel.Replace('\\', Path.DirectorySeparatorChar)));
        if (!full.StartsWith(Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Not a path inside the game folder: " + rel);
        return full;
    }

    /// <summary>
    /// Copy the cars of a plan (or some of them) into the game's cars folder. Nothing that exists is replaced: if any
    /// file is in the way nothing at all is written. If it fails half way, what it made is taken out again.
    /// </summary>
    public static InstallRecord Install(GameFolder game, InstallPlan plan, IEnumerable<FoundCar>? which = null)
    {
        var cars = (which ?? plan.Cars).ToList();
        if (cars.Count == 0) throw new InvalidOperationException("No car to install");
        Review(game, plan, keepFolders: true);
        foreach (var c in cars)
            if (!c.CanInstall) throw new InvalidOperationException((c.Name == "" ? c.OriginalFolder : c.Name) + ": " + c.Issues.First(i => i.Level == InstallLevel.Error).Text);

        // Everything that will be written, checked before the first byte
        var todo = new List<(string Rel, SourceFile? File, string? Text)>();
        foreach (var c in cars)
        {
            foreach (var (rel, f) in c.Files)
            {
                var to = "cars\\" + c.Folder + "\\" + rel;
                bool isParams = rel.Equals("parameters.txt", StringComparison.OrdinalIgnoreCase);
                todo.Add((to, isParams ? null : f, isParams ? RewrittenParams(c) : null));
            }
            foreach (var (rel, f) in c.Extras)
                if (!todo.Any(t => t.Rel.Equals(rel, StringComparison.OrdinalIgnoreCase))) todo.Add((Norm(rel), f, null));
        }
        if (todo.GroupBy(t => t.Rel, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1)) throw new InvalidOperationException("Two files would get the same place");
        foreach (var t in todo)
        {
            var full = Inside(game.Root, t.Rel);
            if (File.Exists(full) || Directory.Exists(full)) throw new IOException(t.Rel + " exists already. Nothing was installed");
        }

        var rec = new InstallRecord { When = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), Source = plan.Source };
        rec.Cars.AddRange(cars.Select(c => c.Folder));
        try
        {
            foreach (var t in todo)
            {
                var full = Inside(game.Root, t.Rel);
                MakeDirs(game.Root, Path.GetDirectoryName(t.Rel.Replace('\\', Path.DirectorySeparatorChar))!, rec);
                // CreateNew: if something appeared there meanwhile this fails instead of replacing it
                using (var o = new FileStream(full, FileMode.CreateNew, FileAccess.Write))
                {
                    rec.Files.Add((t.Rel, ""));
                    if (t.Text != null) o.Write(ParamFile.FileEncoding.GetBytes(t.Text));
                    else { using var i = t.File!.Open(); i.CopyTo(o); }
                }
                rec.Files[^1] = (t.Rel, Hash(full));
            }
            SaveRecord(game, rec);
        }
        catch
        {
            try { Remove(game, rec, true, new UndoResult()); } catch (Exception) { }
            throw;
        }
        return rec;
    }

    // Make the folders of a path one by one, noting the ones that weren't there
    static void MakeDirs(string root, string relDir, InstallRecord rec)
    {
        var cur = "";
        foreach (var part in relDir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            cur = cur == "" ? part : cur + "\\" + part;
            var full = Inside(root, cur);
            if (Directory.Exists(full)) continue;
            Directory.CreateDirectory(full);
            rec.Dirs.Add(cur);
        }
    }

    static string Hash(string file)
    {
        using var s = File.OpenRead(file);
        return Convert.ToHexString(SHA256.HashData(s));
    }

    // ------------------------------------------------------------ the record, and undo

    static string RecordFolder(GameFolder game) => Path.Combine(game.Root, RecordDir.Replace('\\', Path.DirectorySeparatorChar));

    static void SaveRecord(GameFolder game, InstallRecord rec)
    {
        var dir = RecordFolder(game);
        Directory.CreateDirectory(dir);
        // Numbered, so the newest is always the last by name
        int next = Directory.GetFiles(dir, "*.txt").Select(f => int.TryParse(Names.Stem(f), out var n) ? n : 0).DefaultIfEmpty(0).Max() + 1;
        var file = Path.Combine(dir, next.ToString("D6") + ".txt");
        var sb = new StringBuilder("RV Car Studio install\n");
        sb.Append("when\t").Append(rec.When).Append('\n');
        sb.Append("source\t").Append(rec.Source).Append('\n');
        foreach (var c in rec.Cars) sb.Append("car\t").Append(c).Append('\n');
        foreach (var d in rec.Dirs) sb.Append("dir\t").Append(d).Append('\n');
        foreach (var (rel, hash) in rec.Files) sb.Append("file\t").Append(hash).Append('\t').Append(rel).Append('\n');
        File.WriteAllText(file, sb.ToString(), Encoding.UTF8);
        rec.File_ = file;
    }

    /// <summary>The newest install that can still be undone. Null if there's none</summary>
    public static InstallRecord? LastInstall(GameFolder game)
    {
        var dir = RecordFolder(game);
        if (!Directory.Exists(dir)) return null;
        foreach (var file in Directory.GetFiles(dir, "*.txt").OrderByDescending(Names.File, StringComparer.Ordinal))
        {
            string[] lines;
            try { lines = File.ReadAllLines(file, Encoding.UTF8); } catch (IOException) { continue; }
            if (lines.Length == 0 || lines[0] != "RV Car Studio install") continue;
            var rec = new InstallRecord { File_ = file };
            foreach (var l in lines.Skip(1))
            {
                var p = l.Split('\t');
                if (p.Length < 2) continue;
                switch (p[0])
                {
                    case "when": rec.When = p[1]; break;
                    case "source": rec.Source = p[1]; break;
                    case "car": rec.Cars.Add(p[1]); break;
                    case "dir": rec.Dirs.Add(p[1]); break;
                    case "file" when p.Length >= 3: rec.Files.Add((p[2], p[1])); break;
                }
            }
            return rec;
        }
        return null;
    }

    /// <summary>
    /// Take the last install out again: exactly the files it made, and the folders it made if they're empty after
    /// that. A file changed since stays, unless evenChanged.
    /// </summary>
    public static UndoResult? UndoLast(GameFolder game, bool evenChanged = false)
    {
        var rec = LastInstall(game);
        if (rec == null) return null;
        var r = new UndoResult { Record = rec };
        Remove(game, rec, evenChanged, r);
        if (File.Exists(rec.File_)) File.Delete(rec.File_);
        // The folder of the records goes too once it's empty
        for (var d = RecordFolder(game); d.Length > game.Root.Length && Directory.Exists(d) && !Directory.EnumerateFileSystemEntries(d).Any(); d = Path.GetDirectoryName(d)!)
            Directory.Delete(d);
        return r;
    }

    /// <summary>Files of an install that aren't what it wrote any more: the car was edited since</summary>
    public static List<string> ChangedSince(GameFolder game, InstallRecord rec)
    {
        var r = new List<string>();
        foreach (var (rel, hash) in rec.Files)
        {
            string full;
            try { full = Inside(game.Root, rel); } catch (InvalidOperationException) { continue; }
            if (File.Exists(full) && hash != "" && Hash(full) != hash) r.Add(rel);
        }
        return r;
    }

    static void Remove(GameFolder game, InstallRecord rec, bool evenChanged, UndoResult r)
    {
        foreach (var (rel, hash) in rec.Files)
        {
            string full;
            try { full = Inside(game.Root, rel); } catch (InvalidOperationException) { continue; } // a record somebody edited
            if (!File.Exists(full)) continue;
            if (!evenChanged && hash != "" && Hash(full) != hash) { r.Changed.Add(rel); continue; }
            File.SetAttributes(full, FileAttributes.Normal);
            File.Delete(full);
            r.Deleted++;
        }
        foreach (var rel in rec.Dirs.OrderByDescending(d => d.Length))
        {
            string full;
            try { full = Inside(game.Root, rel); } catch (InvalidOperationException) { continue; }
            if (!Directory.Exists(full)) continue;
            if (Directory.EnumerateFileSystemEntries(full).Any()) r.DirsKept.Add(rel);
            else Directory.Delete(full);
        }
    }
}
