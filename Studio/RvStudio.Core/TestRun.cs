using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace RvStudio.Core;

public sealed record RaceSetup(string CarName, string? LevelName, int Cars = 8, int Laps = 3);

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
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var layer in Game.Layers.Reverse())
        {
            var dir = Path.Combine(layer, "levels");
            if (!Directory.Exists(dir)) continue;
            foreach (var d in Directory.GetDirectories(dir))
            {
                var folder = Names.File(d);
                var inf = Path.Combine(d, folder + ".inf");
                if (!File.Exists(inf) || !seen.Add(folder)) continue;
                string name = folder;
                foreach (var l in File.ReadLines(inf, ParamFile.FileEncoding))
                {
                    var m = Regex.Match(l, "^\\s*NAME\\s+'([^']*)'", RegexOptions.IgnoreCase);
                    if (m.Success) { name = m.Groups[1].Value; break; }
                }
                yield return (folder, name);
            }
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
    public string PrepareProfile(string carFolder, string? level, int cars = 8, int laps = 3)
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
        text = SetIni(text, "NCars", cars.ToString());
        text = SetIni(text, "NLaps", laps.ToString());
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

    public Process Start(string carFolder, string? level, bool dev, int cars = 8, int laps = 3)
    {
        PrepareProfile(carFolder, level, cars, laps);
        return Process.Start(Command(dev)) ?? throw new InvalidOperationException("RVGL didn't start");
    }

    // --------------------------- Into the race ---------------------------

    /// <summary>
    /// RVGL has no command to start a race, so this goes through its menus with the keyboard, only while its window has
    /// the focus: Start Race, Single Race, difficulty, the name (where the CARNIVAL code unlocks every car), then the
    /// search that works by typing on the car and track screens. False means the focus went elsewhere and it stopped.
    /// </summary>
    public static async Task<bool> DriveToRace(Process game, RaceSetup race, Action<string>? progress = null)
    {
        if (!OperatingSystem.IsWindows())
        {
            progress?.Invoke("Starting the race by itself only works on Windows so far: choose Start Race");
            return false;
        }
        // The window comes up a little after the process
        for (int i = 0; i < 100 && game.MainWindowHandle == IntPtr.Zero && !game.HasExited; i++)
        {
            await Task.Delay(200);
            game.Refresh();
        }
        if (game.HasExited) return false;
        var win = game.MainWindowHandle;
        await Task.Delay(5500);
        SetForegroundWindow(win);
        await Task.Delay(800);

        async Task<bool> Enter(int wait = 1800)
        {
            if (!Press(win, ScanEnter)) return false;
            await Task.Delay(wait);
            return true;
        }
        progress?.Invoke("Going through RVGL's menus. Please leave its window in front");
        if (!await Enter() || !await Enter() || !await Enter()) return Lost(progress);
        for (int i = 0; i < 12; i++) if (!Press(win, ScanBackspace)) return Lost(progress);
        if (!Type(win, "CARNIVAL")) return Lost(progress);
        await Task.Delay(400);
        if (!await Enter()) return Lost(progress);
        if (!Type(win, race.CarName)) return Lost(progress);
        await Task.Delay(1300);
        if (!await Enter() || !await Enter()) return Lost(progress);
        if (!string.IsNullOrEmpty(race.LevelName))
        {
            if (!Type(win, race.LevelName)) return Lost(progress);
            await Task.Delay(1300);
        }
        if (!await Enter() || !await Enter(500)) return Lost(progress);
        progress?.Invoke("Race started");
        return true;
    }

    static bool Lost(Action<string>? progress)
    {
        progress?.Invoke("RVGL lost the focus, so the menus stopped there: carry on by hand");
        return false;
    }

    /// <summary>RVGL's own screenshot key: the picture goes in its profiles folder</summary>
    public static bool Screenshot(Process game)
    {
        game.Refresh();
        return OperatingSystem.IsWindows() && Press(game.MainWindowHandle, 0x42);
    }

    const ushort ScanEnter = 0x1C, ScanBackspace = 0x0E;

    [StructLayout(LayoutKind.Sequential)] struct KeyInput { public ushort Vk, Scan; public uint Flags, Time; public IntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] struct MouseFiller { public int A, B; public uint C, D, E; public IntPtr F; }
    [StructLayout(LayoutKind.Explicit)] struct InputUnion { [FieldOffset(0)] public KeyInput Key; [FieldOffset(0)] public MouseFiller Mouse; }
    [StructLayout(LayoutKind.Sequential)] struct Input { public uint Type; public InputUnion U; }
    [DllImport("user32.dll")] static extern uint SendInput(uint n, Input[] i, int size);
    [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);

    static void Send(ushort code, uint flags)
    {
        var i = new Input[1];
        i[0].Type = 1;
        i[0].U.Key.Scan = code;
        i[0].U.Key.Flags = flags;
        SendInput(1, i, Marshal.SizeOf<Input>());
    }

    // Only ever to the game: anywhere else these keys would land in whatever you're doing
    static bool Press(IntPtr win, ushort scan)
    {
        if (GetForegroundWindow() != win) return false;
        Send(scan, 8);
        Thread.Sleep(70);
        Send(scan, 8 | 2);
        Thread.Sleep(50);
        return true;
    }

    // By character, so it doesn't matter which keyboard layout is on
    static bool Type(IntPtr win, string text)
    {
        foreach (var c in text)
        {
            if (GetForegroundWindow() != win) return false;
            Send(c, 4);
            Thread.Sleep(50);
            Send(c, 4 | 2);
            Thread.Sleep(50);
        }
        return true;
    }
}
