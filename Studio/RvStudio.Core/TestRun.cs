using System.Diagnostics;
using System.Text.RegularExpressions;

namespace RvStudio.Core;

/// <summary>
/// Runs the car in the real game. RVGL gets its own preferences folder, so your profile, best times
/// and settings aren't touched: the controls are copied once from your last used profile.
/// </summary>
public sealed class TestRun
{
    public readonly GameFolder Game;
    public readonly string PrefPath;
    public const string Profile = "rvstudio";

    public TestRun(GameFolder game, string? prefPath = null)
    {
        Game = game;
        PrefPath = prefPath ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RvCarStudio", "rvgl-test");
    }

    public string? FindExe()
    {
        foreach (var n in new[] { "rvgl.exe", "rvgl", "rvgl.64", "rvgl.32", "rvgl.arm64", "revolt.exe" })
        {
            var p = Path.Combine(Game.Root, n);
            if (File.Exists(p)) return p;
        }
        var app = Path.Combine(Game.Root, "RVGL.app", "Contents", "MacOS", "RVGL");
        return File.Exists(app) ? app : null;
    }

    public IEnumerable<(string Folder, string Name)> Levels()
    {
        var dir = Path.Combine(Game.Root, "levels");
        if (!Directory.Exists(dir)) yield break;
        foreach (var d in Directory.GetDirectories(dir).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var folder = Path.GetFileName(d);
            var inf = Path.Combine(d, folder + ".inf");
            if (!File.Exists(inf)) continue;
            string name = folder;
            foreach (var l in File.ReadLines(inf, ParamFile.FileEncoding))
            {
                var m = Regex.Match(l, "^\\s*NAME\\s+'([^']*)'", RegexOptions.IgnoreCase);
                if (m.Success) { name = m.Groups[1].Value; break; }
            }
            yield return (folder, name);
        }
    }

    string? LastProfileDir()
    {
        var dir = Path.Combine(Game.Root, "profiles");
        if (!Directory.Exists(dir)) return null;
        return Directory.GetDirectories(dir)
            .Where(d => File.Exists(Path.Combine(d, "profile.ini")))
            .OrderByDescending(d => File.GetLastWriteTimeUtc(Path.Combine(d, "profile.ini")))
            .FirstOrDefault();
    }

    /// <summary>The profile for the test: a copy of yours with this car and level picked</summary>
    public string PrepareProfile(string carFolder, string? level)
    {
        var profiles = Path.Combine(PrefPath, "profiles");
        var mine = Path.Combine(profiles, Profile);
        Directory.CreateDirectory(mine);
        var src = LastProfileDir();
        var ini = Path.Combine(mine, "profile.ini");
        if (!File.Exists(ini) && src != null) File.Copy(Path.Combine(src, "profile.ini"), ini);
        var global = Path.Combine(Game.Root, "profiles", "rvgl.ini");
        var myGlobal = Path.Combine(profiles, "rvgl.ini");
        if (!File.Exists(myGlobal) && File.Exists(global)) File.Copy(global, myGlobal);

        var text = File.Exists(ini) ? File.ReadAllText(ini) : "[Game]\r\n";
        text = SetIni(text, "CarDir1", "\"" + carFolder + "\"");
        if (!string.IsNullOrEmpty(level)) text = SetIni(text, "LevelDir", "\"" + level + "\"");
        File.WriteAllText(ini, text);
        return ini;
    }

    static string SetIni(string text, string key, string value)
    {
        var rx = new Regex("(?m)^(" + key + "\\s*=\\s*).*?(\\r?)$");
        if (rx.IsMatch(text)) return rx.Replace(text, m => m.Groups[1].Value + value + m.Groups[2].Value, 1);
        return Regex.Replace(text, "(?m)^\\[Game\\]\\s*$", m => m.Value + "\r\n" + key + " = " + value, RegexOptions.None, TimeSpan.FromSeconds(1));
    }

    public ProcessStartInfo Command(bool dev, bool window = true, int w = 1280, int h = 720)
    {
        var exe = FindExe() ?? throw new FileNotFoundException("No RVGL in " + Game.Root);
        var info = new ProcessStartInfo(exe) { WorkingDirectory = Game.Root, UseShellExecute = false };
        foreach (var a in new[] { "-prefpath", PrefPath, "-profile", Profile, "-nointro", "-sload" }) info.ArgumentList.Add(a);
        if (window) { info.ArgumentList.Add("-window"); info.ArgumentList.Add(w.ToString()); info.ArgumentList.Add(h.ToString()); }
        if (dev) info.ArgumentList.Add("-dev"); // Ctrl+R reloads the car, Calc Car Stats in Start Race
        return info;
    }

    public Process Start(string carFolder, string? level, bool dev)
    {
        PrepareProfile(carFolder, level);
        return Process.Start(Command(dev)) ?? throw new InvalidOperationException("RVGL didn't start");
    }
}
