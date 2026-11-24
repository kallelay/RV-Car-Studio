using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RvStudio.App;

/// <summary>
/// Symbols instead of words on the busy bars: each button shows a symbol from Windows' own icon font, its name and
/// what it does in the tooltip. View, Names on the buttons, puts the words back beside them. Where the font isn't
/// there (another system), the buttons keep their words.
/// </summary>
public static class Sym
{
    static readonly FontFamily Font = new("Segoe Fluent Icons, Segoe MDL2 Assets");

    /// <summary>Windows 10 and 11 have the font</summary>
    public static readonly bool Available = OperatingSystem.IsWindows() &&
        (File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "segmdl2.ttf")) ||
         File.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "SegoeIcons.ttf")));

    // Symbols beside the words unless you chose symbols alone
    public static bool ShowNames { get; private set; } = Settings.Get("names") != "0";

    // ---------------------------- a symbol for each word that has one ----------------------------

    /// <summary>The start of a button's, tab's or menu item's words, and its symbol. Longest first wins</summary>
    static readonly (string Start, string Glyph)[] Words = new (string, string)[]
    {
        // Tabs
        ("Selected", ""), ("Files", ""), ("Properties (RVGL)", ""), ("Properties", ""), ("Model", ""),
        ("Paint", ""), ("Shade", ""), ("Package", ""),
        // Files, Package
        ("Pick", ""), ("Open the car's folder", ""), ("Open car folder", ""), ("Open Re-Volt folder", ""),
        ("Import", ""), ("Install", ""), ("Export", ""), ("Readme", ""), ("Pack as zip", ""),
        ("Carbox maker", ""), ("Quick carbox", ""), ("A picture of my own", ""), ("Make carbox", ""),
        // Properties
        ("Take its handling", ""), ("Compare", ""), ("Use these numbers", ""), ("Back to 1", ""),
        // Model
        ("Edit the vertices", ""), ("Edit the hull", ""), ("Pick all", ""), ("Pick none", ""),
        ("Make a hull", ""), ("Make collision hull", ""), ("Show and edit the hull", ""),
        ("Radius, skid", ""), ("Fit", ""), ("Inertia", ""), ("Compute inertia", ""), ("Place wheels", ""),
        ("Make right side like the left", ""), ("Make left side like the right", ""),
        // Paint, Shade
        ("Open the UV and texture editor", ""), ("Open the texture and watch it", ""), ("Save as a new skin", ""),
        ("Change this car's texture", ""), ("Make a livery", ""), ("Generate", ""), ("Back to the texture", ""),
        ("Shade the car", ""), ("Auto shade", ""), ("Back to the shading", ""), ("Rewrite the normals", ""),
        ("Make shadow", ""), ("Fit shadow", ""), ("Fill frontend stats", ""),
        // Menus
        ("New car", ""), ("Change car", ""), ("Undo the last install", ""), ("Save", ""), ("Make a copy", ""),
        ("Back to the game's version", ""), ("Revert", ""), ("Rename", ""), ("Exit", ""),
        ("Undo", ""), ("Redo", ""), ("Test run", ""), ("Drive here", ""), ("Race here", ""),
        ("Automatic runs", ""), ("Frame", ""), ("Guide", ""), ("Tours", ""), ("Mouse and keys", ""),
        ("About", ""), ("Theme", ""),
    }.OrderByDescending(w => w.Item1.Length).ToArray();

    /// <summary>The symbol for these words, or null</summary>
    public static string? For(string? text)
    {
        if (!Available || string.IsNullOrEmpty(text)) return null;
        var t = text.TrimStart('+', ' ', '▶', '→');
        foreach (var (start, glyph) in Words) if (t.StartsWith(start, StringComparison.OrdinalIgnoreCase)) return glyph;
        return null;
    }

    /// <summary>The symbol, small, for a menu item's icon</summary>
    public static Control? Icon(string text) => For(text) is { } g ? new TextBlock { Text = g, FontFamily = Font, FontSize = 14, VerticalAlignment = VerticalAlignment.Center } : null;

    /// <summary>Shows words with their symbol before them; the words themselves stay the button's (tests and links find it by them)</summary>
    public static readonly Avalonia.Controls.Templates.IDataTemplate Beside = new Avalonia.Controls.Templates.FuncDataTemplate<string>((s, _) =>
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Center };
        if (For(s) is { } g) p.Children.Add(new TextBlock { Text = g, FontFamily = Font, FontSize = 13, VerticalAlignment = VerticalAlignment.Center });
        p.Children.Add(new TextBlock { Text = s, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis });
        return p;
    }, true);

    /// <summary>A symbol before the words of this button (or tab), when its words have one</summary>
    public static void Add(ContentControl c)
    {
        if (c.Content is string s && For(s) != null) c.ContentTemplate = Beside;
    }

    public static void AddTo(Avalonia.Controls.Primitives.HeaderedContentControl c)
    {
        if (c.Header is string s && For(s) != null) c.HeaderTemplate = Beside;
    }

    // Every button given a symbol, to show or hide the names on all of them at once
    static readonly List<(WeakReference<ContentControl> C, string Glyph, string Name, bool Always)> all = new();

    public const string Save = "", Undo = "", Redo = "", Move = "", Rotate = "", Scale = "",
        One = "", Mirror = "", FrontBack = "", Four = "", Frame = "", Palette = "", Eye = "",
        Picture = "", Shading = "", Clay = "", Play = "", Car = "", Flag = "",
        Pointer = "", Brush = "", Eraser = "", Drop = "", Dropper = "", Line = "", Shape = "",
        Waves = "", Focus = "", Sun = "", Flame = "";

    static object Make(string glyph, string name, bool always)
    {
        if (!Available) return name;
        var g = new TextBlock { Text = glyph, FontFamily = Font, FontSize = 15, VerticalAlignment = VerticalAlignment.Center };
        if (!ShowNames && !always) return g;
        var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        p.Children.Add(g);
        p.Children.Add(new TextBlock { Text = name, VerticalAlignment = VerticalAlignment.Center });
        return p;
    }

    /// <summary>A symbol on the button, its name and what it does in the tooltip. always: the name shows too (the main action)</summary>
    public static T Set<T>(T c, string glyph, string name, string? tip = null, bool always = false) where T : ContentControl
    {
        c.Content = Make(glyph, name, always);
        var t = tip ?? (ToolTip.GetTip(c) as string);
        ToolTip.SetTip(c, string.IsNullOrEmpty(t) || t.StartsWith(name) ? (t ?? name) : name + ": " + t);
        all.Add((new WeakReference<ContentControl>(c), glyph, name, always));
        return c;
    }

    /// <summary>Names beside the symbols, or symbols alone (remembered)</summary>
    public static void SetShowNames(bool on)
    {
        ShowNames = on;
        if (!AutoScenario.Running) Settings.Put("names", on ? "1" : "0");
        all.RemoveAll(x => !x.C.TryGetTarget(out _));
        foreach (var (w, g, n, a) in all) if (w.TryGetTarget(out var c)) c.Content = Make(g, n, a);
    }
}
