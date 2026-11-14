using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace RvStudio.App;

public enum ThemeKind { Classic, Dark, Light }

/// <summary>
/// The colours of everything, for the look you chose. The brushes are shared and change in place, so whatever
/// uses one follows at once. The 3D view reads its colours here when it builds a frame.
/// </summary>
public static class Look
{
    public static ThemeKind Kind { get; private set; } = ThemeKind.Classic;
    public static event Action? Changed;

    /// <summary>Corners of buttons and boxes, and of menus, tips and cards</summary>
    public const double Radius = 6, BigRadius = 8;

    // Bar: the window itself, menu and status line. Panel: what lies on it. Card: buttons, boxes and popups on a panel.
    // Line: quiet separation. Edge: the outline of something you can use. Hover: under the mouse
    public static readonly SolidColorBrush
        Panel = new(), Bar = new(), Card = new(), Hover = new(), Pressed = new(), Line = new(), Edge = new(),
        Text = new(), Faint = new(), Accent = new(), AccentSoft = new(), OnAccent = new(),
        Good = new(), Warn = new(), Bad = new(), Modified = new(), Banner = new(), BannerLine = new(),
        TipBack = new(), MapBack = new(), MapRoad = new(), MapMe = new(), MapOther = new();

    /// <summary>The accent as a filled surface: orange to coral, as on the app's icon</summary>
    public static readonly LinearGradientBrush
        AccentFill = new() { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative) },
        AccentFillHover = new() { StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative), EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative) };
    public static Color FillFrom { get; private set; }
    public static Color FillTo { get; private set; }

    // The 3D view
    public static Vector3 ViewBack, Floor;
    public static (byte R, byte G, byte B) GridMajor, GridMinor, ViewLine;
    public static Vector4 SelectLine, HoverLine;
    public static bool LightView;
    /// <summary>Behind a small picture of a car or a model</summary>
    public static SkiaSharp.SKColor ThumbBack;

    sealed class Palette
    {
        public required string Panel, Bar, Card, Hover, Pressed, Line, Edge, Text, Faint;
        public required string Accent, AccentSoft, OnAccent, FillFrom, FillTo;
        public required string Good, Warn, Bad, Modified, Banner, BannerLine, TipBack;
        public required string MapBack, MapRoad, MapMe, MapOther;
        public required string ViewBack, Floor, GridMajor, GridMinor, ViewLine, SelectLine, HoverLine;
        /// <summary>Inside a text box</summary>
        public required string Field;
        public required string Thumb;
    }

    static readonly Dictionary<ThemeKind, Palette> Looks = new()
    {
        // The colours it always had: blue on slate
        [ThemeKind.Classic] = new()
        {
            Panel = "#23262D", Bar = "#1B1E23", Card = "#30343D", Hover = "#3B404B", Pressed = "#2A2E36", Line = "#343842", Edge = "#474D5A",
            Text = "#FFFFFF", Faint = "#9AA3B2",
            Accent = "#4C9AFF", AccentSoft = "#334C9AFF", OnAccent = "#FFFFFF", FillFrom = "#2F7DE0", FillTo = "#2F7DE0",
            Good = "#90EE90", Warn = "#FFD700", Bad = "#FF4500", Modified = "#FFA500", Banner = "#3A3320", BannerLine = "#DAA520", TipBack = "#E01B1E23",
            MapBack = "#780F1218", MapRoad = "#E6E1E6F0", MapMe = "#FFD83D", MapOther = "#FF6B5B",
            ViewBack = "#2B3038", Floor = "#4D5461", GridMajor = "#464C58", GridMinor = "#343A44", ViewLine = "#FFFFFF", SelectLine = "#8C59B3FF", HoverLine = "#99FFD966",
            Field = "#1B1E23", Thumb = "#2B2F36"
        },
        // Black, with the icon's orange
        [ThemeKind.Dark] = new()
        {
            Panel = "#0D0D10", Bar = "#050506", Card = "#19191E", Hover = "#25252C", Pressed = "#131317", Line = "#222228", Edge = "#33333C",
            Text = "#F5F5F7", Faint = "#A3A3AF",
            Accent = "#FF9440", AccentSoft = "#3AFF8A3D", OnAccent = "#1A0C04", FillFrom = "#FFA82E", FillTo = "#FF485C",
            Good = "#7CE38B", Warn = "#FFD23F", Bad = "#FF7B6E", Modified = "#FFB454", Banner = "#2A1D0A", BannerLine = "#B8741A", TipBack = "#F0141418",
            MapBack = "#B40A0A0C", MapRoad = "#E6E6E6F0", MapMe = "#FFA82E", MapOther = "#9FB4FF",
            ViewBack = "#000000", Floor = "#131316", GridMajor = "#4A4A54", GridMinor = "#26262C", ViewLine = "#FFFFFF", SelectLine = "#994DA3FF", HoverLine = "#99FFD966",
            Field = "#060608", Thumb = "#24242B"
        },
        // Ghost white, the same orange made dark enough to read
        [ThemeKind.Light] = new()
        {
            Panel = "#F8F8FF", Bar = "#ECECF5", Card = "#FFFFFF", Hover = "#E9E9F3", Pressed = "#DDDDEA", Line = "#DADAE6", Edge = "#C2C2D0",
            Text = "#15151C", Faint = "#565C6E",
            Accent = "#C2410C", AccentSoft = "#2EF2600C", OnAccent = "#1A0C04", FillFrom = "#FFA82E", FillTo = "#FF6A5C",
            Good = "#17703A", Warn = "#7A5200", Bad = "#C02626", Modified = "#A84A00", Banner = "#FFF1D6", BannerLine = "#D9902B", TipBack = "#F5FFFFFF",
            MapBack = "#C8FFFFFF", MapRoad = "#E6303442", MapMe = "#E8590C", MapOther = "#3355CC",
            ViewBack = "#F8F8FF", Floor = "#E2E2EC", GridMajor = "#9A9CAE", GridMinor = "#C4C6D4", ViewLine = "#15151C", SelectLine = "#B30F52BA", HoverLine = "#B3C25A00",
            Field = "#FFFFFF", Thumb = "#E4E4EE"
        },
    };

    static Look() => SetColours(Looks[ThemeKind.Classic]);

    static Vector3 V3(string s) { var c = Color.Parse(s); return new Vector3(c.R, c.G, c.B) / 255f; }
    static Vector4 V4(string s) { var c = Color.Parse(s); return new Vector4(c.R, c.G, c.B, c.A) / 255f; }
    static (byte, byte, byte) B3(string s) { var c = Color.Parse(s); return (c.R, c.G, c.B); }

    static Color Lighter(Color c, double by) =>
        Color.FromRgb((byte)(c.R + (255 - c.R) * by), (byte)(c.G + (255 - c.G) * by), (byte)(c.B + (255 - c.B) * by));

    static void SetColours(Palette l)
    {
        void Set(SolidColorBrush b, string s) => b.Color = Color.Parse(s);
        Set(Panel, l.Panel); Set(Bar, l.Bar); Set(Card, l.Card); Set(Hover, l.Hover); Set(Pressed, l.Pressed); Set(Line, l.Line); Set(Edge, l.Edge);
        Set(Text, l.Text); Set(Faint, l.Faint); Set(Accent, l.Accent); Set(AccentSoft, l.AccentSoft); Set(OnAccent, l.OnAccent);
        Set(Good, l.Good); Set(Warn, l.Warn); Set(Bad, l.Bad); Set(Modified, l.Modified); Set(Banner, l.Banner); Set(BannerLine, l.BannerLine);
        Set(TipBack, l.TipBack); Set(MapBack, l.MapBack); Set(MapRoad, l.MapRoad); Set(MapMe, l.MapMe); Set(MapOther, l.MapOther);
        FillFrom = Color.Parse(l.FillFrom); FillTo = Color.Parse(l.FillTo);
        AccentFill.GradientStops = new GradientStops { new GradientStop(FillFrom, 0), new GradientStop(FillTo, 1) };
        AccentFillHover.GradientStops = new GradientStops { new GradientStop(Lighter(FillFrom, 0.18), 0), new GradientStop(Lighter(FillTo, 0.18), 1) };
        ViewBack = V3(l.ViewBack); Floor = V3(l.Floor);
        GridMajor = B3(l.GridMajor); GridMinor = B3(l.GridMinor); ViewLine = B3(l.ViewLine);
        SelectLine = V4(l.SelectLine); HoverLine = V4(l.HoverLine);
        ThumbBack = SkiaSharp.SKColor.Parse(l.Thumb);
    }

    /// <summary>A helper's colour in the view: the bright ones made for a dark floor are darkened on a light one</summary>
    public static (byte R, byte G, byte B) Mark(byte r, byte g, byte b) =>
        LightView ? ((byte)(r * 0.55f), (byte)(g * 0.55f), (byte)(b * 0.55f)) : (r, g, b);

    static FluentTheme? fluent;

    // Fluent's lists, sliders, check boxes and popups take their colours from its palette when it's made,
    // so each look has a Fluent theme of its own, put in the place of the last one
    static FluentTheme Fluent(ThemeKind kind, Palette l)
    {
        var f = new FluentTheme();
        f.Palettes[kind == ThemeKind.Light ? ThemeVariant.Light : ThemeVariant.Dark] = new ColorPaletteResources
        {
            Accent = Color.Parse(kind == ThemeKind.Light ? "#E8590C" : l.Accent), RegionColor = Color.Parse(l.Panel),
            AltHigh = Color.Parse(l.Field), ChromeMediumLow = Color.Parse(l.Card), ChromeMedium = Color.Parse(l.Card)
        };
        return f;
    }

    public static void Apply(ThemeKind kind)
    {
        Kind = kind;
        var l = Looks[kind];
        LightView = kind == ThemeKind.Light;
        SetColours(l);
        if (Application.Current is { } app)
        {
            app.RequestedThemeVariant = kind == ThemeKind.Light ? ThemeVariant.Light : ThemeVariant.Dark;
            var made = Fluent(kind, l);
            int at = fluent == null ? -1 : app.Styles.IndexOf(fluent);
            if (at >= 0) app.Styles[at] = made;
            else app.Styles.Insert(0, made);
            fluent = made;
        }
        Changed?.Invoke();
    }

    /// <summary>A button in the accent colour, with text that reads on it</summary>
    public static T Primary<T>(T b) where T : TemplatedControl
    {
        b.Classes.Add("primary");
        return b;
    }

    /// <summary>A section's heading: a small accent mark and the name</summary>
    public static Control Heading(string text, Thickness margin) => new StackPanel
    {
        Orientation = Orientation.Horizontal, Spacing = 7, Margin = margin,
        Children =
        {
            new Border { Width = 3, Height = 13, CornerRadius = new CornerRadius(1.5), Background = AccentFill, VerticalAlignment = VerticalAlignment.Center },
            new TextBlock { Text = text, FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Text, VerticalAlignment = VerticalAlignment.Center }
        }
    };

    /// <summary>Choices side by side in one rounded box: put RadioButtons with the class "seg", or flat Buttons, in it</summary>
    public static Border Group(params Control[] items)
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        foreach (var i in items) row.Children.Add(i);
        return new Border
        {
            Background = Card, BorderBrush = Edge, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Radius + 1),
            Padding = new Thickness(2), Margin = new Thickness(2, 0), VerticalAlignment = VerticalAlignment.Center, Child = row
        };
    }

    // --------------------------- How the controls look ---------------------------

    static Style On<T>(Func<Selector?, Selector> more, params (AvaloniaProperty, object?)[] set) where T : StyledElement
    {
        var s = new Style(x => more(x.OfType<T>()));
        foreach (var (p, v) in set) s.Setters.Add(new Setter(p, v));
        return s;
    }

    static Selector Inside(Selector s) => s.Template().OfType<ContentPresenter>().Name("PART_ContentPresenter");

    // A plain rounded box around the content: for tabs and for choices in a group, in place of Fluent's underline and circle
    static IControlTemplate Pill<T>(AvaloniaProperty content) where T : TemplatedControl =>
        new FuncControlTemplate<T>((c, _) => new Border
        {
            [!Border.BackgroundProperty] = c[!TemplatedControl.BackgroundProperty],
            [!Border.CornerRadiusProperty] = c[!TemplatedControl.CornerRadiusProperty],
            [!Decorator.PaddingProperty] = c[!TemplatedControl.PaddingProperty],
            Child = new ContentPresenter
            {
                Name = "PART_ContentPresenter", HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center,
                [!ContentPresenter.ContentProperty] = c[!content]
            }
        });

    /// <summary>The app's own styles, on top of Fluent's. Made once: they hold the shared brushes, so they follow the look</summary>
    public static IEnumerable<IStyle> Styles()
    {
        var radius = new CornerRadius(Radius);
        var clear = Brushes.Transparent;

        yield return On<ScrollViewer>(x => x!, (ScrollViewer.AllowAutoHideProperty, false));
        yield return On<GridSplitter>(x => x!, (TemplatedControl.BackgroundProperty, Bar));
        yield return On<ListBox>(x => x!, (TemplatedControl.BackgroundProperty, clear));
        yield return On<ListBoxItem>(x => x!, (TemplatedControl.CornerRadiusProperty, radius), (Layoutable.MarginProperty, new Thickness(4, 1)));
        yield return On<ListBoxItem>(x => Inside(x.Class(":pointerover")), (ContentPresenter.BackgroundProperty, Hover));
        yield return On<ListBoxItem>(x => Inside(x.Class(":selected")), (ContentPresenter.BackgroundProperty, AccentSoft));
        yield return On<ToolTip>(x => x!,
            (TemplatedControl.BackgroundProperty, Card), (TemplatedControl.ForegroundProperty, Text), (TemplatedControl.BorderBrushProperty, Edge),
            (TemplatedControl.CornerRadiusProperty, radius), (TemplatedControl.PaddingProperty, new Thickness(10, 6)), (TemplatedControl.FontSizeProperty, 12.0));

        // The accent button
        yield return On<Button>(x => x.Class("primary"),
            (TemplatedControl.BackgroundProperty, AccentFill), (TemplatedControl.ForegroundProperty, OnAccent),
            (TemplatedControl.BorderBrushProperty, clear), (TemplatedControl.FontWeightProperty, FontWeight.SemiBold));
        foreach (var state in new[] { ":pointerover", ":pressed" })
            yield return On<Button>(x => Inside(x.Class("primary").Class(state)),
                (ContentPresenter.BackgroundProperty, AccentFillHover), (ContentPresenter.ForegroundProperty, OnAccent), (ContentPresenter.BorderBrushProperty, clear));

        // A button with no box of its own: in a group on the toolbar
        yield return On<Button>(x => x.Class("flat"), (TemplatedControl.BackgroundProperty, clear), (TemplatedControl.BorderBrushProperty, clear));
        yield return On<Button>(x => Inside(x.Class("flat").Class(":pointerover")), (ContentPresenter.BackgroundProperty, Hover), (ContentPresenter.BorderBrushProperty, clear));
        yield return On<Button>(x => Inside(x.Class("flat").Class(":pressed")), (ContentPresenter.BackgroundProperty, Pressed), (ContentPresenter.BorderBrushProperty, clear));

        // One of a few choices, side by side
        yield return On<RadioButton>(x => x.Class("seg"),
            (TemplatedControl.TemplateProperty, Pill<RadioButton>(ContentControl.ContentProperty)),
            (TemplatedControl.BackgroundProperty, clear), (TemplatedControl.ForegroundProperty, Text), (TemplatedControl.CornerRadiusProperty, new CornerRadius(Radius - 1)),
            (TemplatedControl.PaddingProperty, new Thickness(10, 3)), (Layoutable.MinHeightProperty, 24.0), (Layoutable.MinWidthProperty, 0.0),
            (Layoutable.MarginProperty, new Thickness(0)), (TemplatedControl.FontSizeProperty, 12.0));
        yield return On<RadioButton>(x => x.Class("seg").Class(":pointerover"), (TemplatedControl.BackgroundProperty, Hover));
        yield return On<RadioButton>(x => x.Class("seg").Class(":checked"),
            (TemplatedControl.BackgroundProperty, AccentSoft), (TemplatedControl.ForegroundProperty, Accent));

        // Tabs
        yield return On<TabItem>(x => x!,
            (TemplatedControl.TemplateProperty, Pill<TabItem>(HeaderedContentControl.HeaderProperty)),
            (TemplatedControl.FontSizeProperty, 12.5), (Layoutable.MinHeightProperty, 0.0), (TemplatedControl.PaddingProperty, new Thickness(10, 5)),
            (Layoutable.MarginProperty, new Thickness(0, 0, 2, 2)), (TemplatedControl.CornerRadiusProperty, radius),
            (TemplatedControl.BackgroundProperty, clear), (TemplatedControl.ForegroundProperty, Faint));
        yield return On<TabItem>(x => x.Class(":pointerover"), (TemplatedControl.BackgroundProperty, Hover), (TemplatedControl.ForegroundProperty, Text));
        yield return On<TabItem>(x => x.Class(":selected"), (TemplatedControl.BackgroundProperty, AccentSoft), (TemplatedControl.ForegroundProperty, Accent));
    }

    /// <summary>Fluent's own colours for buttons and boxes, pointed at the look's brushes, and its corners made rounder</summary>
    public static void Resources(IResourceDictionary r)
    {
        r["ControlCornerRadius"] = new CornerRadius(Radius);
        r["OverlayCornerRadius"] = new CornerRadius(BigRadius);
        foreach (var (key, brush) in new (string, IBrush)[]
                 {
                     ("ButtonBackground", Card), ("ButtonBackgroundPointerOver", Hover), ("ButtonBackgroundPressed", Pressed),
                     ("ButtonBorderBrush", Edge), ("ButtonBorderBrushPointerOver", Edge), ("ButtonBorderBrushPressed", Edge),
                     ("ButtonForeground", Text), ("ButtonForegroundPointerOver", Text), ("ButtonForegroundPressed", Text),
                     ("RepeatButtonBackground", Card), ("RepeatButtonBackgroundPointerOver", Hover), ("RepeatButtonBackgroundPressed", Pressed),
                     ("RepeatButtonBorderBrush", Edge), ("RepeatButtonBorderBrushPointerOver", Edge), ("RepeatButtonBorderBrushPressed", Edge),
                     ("RepeatButtonForeground", Text), ("RepeatButtonForegroundPointerOver", Text), ("RepeatButtonForegroundPressed", Text),
                     ("ToggleButtonBackground", Card), ("ToggleButtonBackgroundPointerOver", Hover), ("ToggleButtonBorderBrush", Edge), ("ToggleButtonBorderBrushPointerOver", Edge),
                     ("ToggleButtonForeground", Text), ("ToggleButtonForegroundPointerOver", Text),
                     ("TextControlBorderBrush", Edge), ("TextControlBorderBrushPointerOver", Faint), ("TextControlBorderBrushFocused", Accent),
                     ("TextControlForeground", Text), ("TextControlForegroundPointerOver", Text), ("TextControlForegroundFocused", Text),
                     ("TextControlPlaceholderForeground", Faint), ("TextControlPlaceholderForegroundPointerOver", Faint), ("TextControlPlaceholderForegroundFocused", Faint),
                     ("ComboBoxBackground", Card), ("ComboBoxBackgroundPointerOver", Hover), ("ComboBoxBorderBrush", Edge), ("ComboBoxBorderBrushPointerOver", Edge),
                     ("ComboBoxForeground", Text), ("ComboBoxDropDownBackground", Card), ("ComboBoxDropDownBorderBrush", Edge),
                     ("MenuFlyoutPresenterBackground", Card), ("MenuFlyoutPresenterBorderBrush", Edge),
                     ("MenuFlyoutItemBackgroundPointerOver", Hover), ("MenuFlyoutItemBackgroundPressed", Pressed),
                     ("MenuFlyoutSubItemBackgroundPointerOver", Hover), ("MenuFlyoutSubItemBackgroundSubMenuOpened", Hover),
                     ("MenuBarItemBackgroundPointerOver", Hover), ("MenuBarItemBackgroundSelected", Hover), ("MenuBarItemBackgroundPressed", Pressed),
                 })
            r[key] = brush;
    }

    static double Lum(Color c)
    {
        static double Ch(byte v) { var x = v / 255.0; return x <= 0.03928 ? x / 12.92 : Math.Pow((x + 0.055) / 1.055, 2.4); }
        return 0.2126 * Ch(c.R) + 0.7152 * Ch(c.G) + 0.0722 * Ch(c.B);
    }

    /// <summary>How well one colour reads on another, as WCAG counts it: 4.5 and up is good for text</summary>
    public static double Contrast(Color a, Color b)
    {
        double la = Lum(a), lb = Lum(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }
}
