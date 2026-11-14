using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace RvStudio.App;

/// <summary>A panel of rows connected to the car: labels, numbers, vectors, switches. Everything edits live with undo</summary>
public sealed class Page : ScrollViewer
{
    readonly StackPanel stack = new() { Spacing = 6, Margin = new Thickness(14, 8, 16, 14) };
    readonly List<Action> loaders = new();
    readonly Doc doc;
    bool loading;

    // Without this a class derived from ScrollViewer has no look, and so no scrollbar and no scrolling
    protected override Type StyleKeyOverride => typeof(ScrollViewer);

    public Page(Doc d)
    {
        doc = d;
        Content = stack;
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
        VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        AllowAutoHide = false;
    }

    public void Refresh()
    {
        if (doc.Car == null) return;
        loading = true;
        foreach (var l in loaders) l();
        loading = false;
    }

    // The look's own brushes: they change colour in place when the theme does
    public static IBrush Faint => Look.Faint;
    public static IBrush Accent => Look.Accent;

    public void Header(string text)
    {
        // A line between sections, none above the first
        if (stack.Children.Count > 0)
            stack.Children.Add(new Border { Height = 1, Background = Look.Line, Margin = new Thickness(0, 10, 0, 0) });
        stack.Children.Add(Look.Heading(text, new Thickness(0, 6, 0, 3)));
    }

    public void Hint(Func<string> text)
    {
        var t = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 11, Foreground = Faint, Margin = new Thickness(112, -3, 0, 2) };
        loaders.Add(() => t.Text = text());
        stack.Children.Add(t);
    }

    /// <summary>A path with a button beside it</summary>
    public void TextWithButton(string label, Func<string> get, Action<string> set, string button, Action click, string? tip = null)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,3,Auto") };
        var b = new TextBox { MinHeight = 26, Height = 26, Padding = new Thickness(5, 2), FontSize = 12 };
        loaders.Add(() => { if (!b.IsFocused) b.Text = get(); });
        b.TextChanged += (_, _) =>
        {
            if (loading || b.Text == get()) return;
            doc.Edit(label, () => set(b.Text ?? ""));
        };
        var pick = new Button { Content = button, FontSize = 12, Padding = new Thickness(7, 3), VerticalAlignment = VerticalAlignment.Stretch };
        pick.Click += (_, _) => click();
        g.Children.Add(b);
        Grid.SetColumn(pick, 2);
        g.Children.Add(pick);
        Row(label, g, tip);
    }

    public void Note(string text) =>
        stack.Children.Add(new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Faint });

    public TextBlock Info(Func<string> text)
    {
        var t = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Faint };
        loaders.Add(() => t.Text = text());
        stack.Children.Add(t);
        return t;
    }

    void Row(string label, Control c, string? tip)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("112,*") };
        var l = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center, FontSize = 12, TextTrimming = TextTrimming.CharacterEllipsis };
        if (tip != null) { ToolTip.SetTip(l, tip); ToolTip.SetTip(c, tip); }
        g.Children.Add(l);
        Grid.SetColumn(c, 1);
        g.Children.Add(c);
        stack.Children.Add(g);
    }

    static string Show(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    public void Num(string label, Func<double> get, Action<double> set, string? tip = null, double min = double.MinValue, double max = double.MaxValue, bool whole = false)
    {
        var b = new NumberBox(min, max, whole);
        loaders.Add(() => b.Show(get()));
        b.Edited += v =>
        {
            if (loading || Math.Abs(v - get()) < 1e-9) return;
            doc.Edit(label, () => set(v));
        };
        b.Done += () => b.Show(get(), true);
        Row(label, b, tip);
    }

    public void Int(string label, Func<int> get, Action<int> set, string? tip = null, int min = int.MinValue, int max = int.MaxValue) =>
        Num(label, () => get(), v => set((int)v), tip, min, max, true);

    public void Vec(string label, Func<Vector3> get, Action<Vector3> set, string? tip = null)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,2,*,2,*") };
        var boxes = new[] { new NumberBox(), new NumberBox(), new NumberBox() };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            Grid.SetColumn(boxes[i], i * 2);
            g.Children.Add(boxes[i]);
            boxes[i].Edited += v =>
            {
                if (loading) return;
                var cur = get();
                var nv = k == 0 ? cur with { X = (float)v } : k == 1 ? cur with { Y = (float)v } : cur with { Z = (float)v };
                if (nv == cur) return;
                doc.Edit(label, () => set(nv));
            };
            boxes[i].Done += () => LoadVec(true);
        }
        void LoadVec(bool force)
        {
            var v = get();
            boxes[0].Show(Math.Round(v.X, 3), force);
            boxes[1].Show(Math.Round(v.Y, 3), force);
            boxes[2].Show(Math.Round(v.Z, 3), force);
        }
        loaders.Add(() => LoadVec(false));
        Row(label, g, tip);
    }

    public void Check(string label, Func<bool> get, Action<bool> set, string? tip = null)
    {
        var c = new CheckBox { Content = label, FontSize = 12, MinHeight = 24 };
        if (tip != null) ToolTip.SetTip(c, tip);
        loaders.Add(() => c.IsChecked = get());
        c.IsCheckedChanged += (_, _) =>
        {
            if (loading || c.IsChecked == get()) return;
            doc.Edit(label, () => set(c.IsChecked == true));
        };
        stack.Children.Add(c);
    }

    public TextBox Text(string label, Func<string> get, Action<string> set, string? tip = null)
    {
        var b = new TextBox { MinHeight = 26, Height = 26, Padding = new Thickness(5, 2), FontSize = 12 };
        loaders.Add(() => { if (!b.IsFocused) b.Text = get(); });
        b.TextChanged += (_, _) =>
        {
            if (loading || b.Text == get()) return;
            doc.Edit(label, () => set(b.Text ?? ""));
        };
        Row(label, b, tip);
        return b;
    }

    public ComboBox Choice(string label, string[] items, Func<int> get, Action<int> set, string? tip = null)
    {
        var c = new ComboBox { ItemsSource = items, FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch };
        loaders.Add(() => c.SelectedIndex = Math.Clamp(get(), -1, items.Length - 1));
        c.SelectionChanged += (_, _) =>
        {
            if (loading || c.SelectedIndex < 0 || c.SelectedIndex == get()) return;
            doc.Edit(label, () => set(c.SelectedIndex));
        };
        Row(label, c, tip);
        return c;
    }

    public Button Button(string text, Action click, string? tip = null, bool primary = false)
    {
        var b = new Button { Content = text, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center, Padding = new Thickness(8, 5) };
        if (primary) Look.Primary(b);
        if (tip != null) ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        stack.Children.Add(b);
        return b;
    }

    public Slider Slide(string label, double min, double max, double start, Action<double>? changed = null, string? tip = null)
    {
        var s = new Slider { Minimum = min, Maximum = max, Value = start, Margin = new Thickness(0, -8, 0, -8) };
        var shown = new TextBlock { FontSize = 11, Foreground = Faint, VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Right, Text = Show(Math.Round(start, 2)) };
        s.PropertyChanged += (_, e) =>
        {
            if (e.Property != RangeBaseValue) return;
            shown.Text = Show(Math.Round(s.Value, 2));
            changed?.Invoke(s.Value);
        };
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,38") };
        g.Children.Add(s);
        Grid.SetColumn(shown, 1);
        g.Children.Add(shown);
        Row(label, g, tip);
        return s;
    }
    static readonly AvaloniaProperty RangeBaseValue = Avalonia.Controls.Primitives.RangeBase.ValueProperty;

    public void Add(Control c) => stack.Children.Add(c);

    /// <summary>Any control beside a label, not connected to the car</summary>
    public void Labeled(string label, Control c, string? tip = null) => Row(label, c, tip);

    /// <summary>Something else to bring up to date when the car changes</summary>
    public void Live(Action load) => loaders.Add(load);

    /// <summary>A worked out value: label, result, and what it means when you point at it</summary>
    public TextBlock Value(string label, Func<string> text, string? tip = null)
    {
        var t = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        loaders.Add(() => t.Text = text());
        Row(label, t, tip);
        return t;
    }

    public ColourButton Colour(string label, Color start, Action<Color> changed, string? tip = null)
    {
        var b = new ColourButton(start);
        b.Changed += changed;
        Row(label, b, tip);
        return b;
    }

    /// <summary>A colour on the car, with undo</summary>
    public ColourButton ColourOf(string label, Func<Color> get, Action<Color> set, string? tip = null)
    {
        var b = new ColourButton(Colors.Black);
        loaders.Add(() => b.SetValue(get()));
        b.Changed += c =>
        {
            if (loading || c == get()) return;
            doc.Edit(label, () => set(c));
        };
        Row(label, b, tip);
        return b;
    }
}

/// <summary>A colour: click for a palette, the colours of this texture, sliders and hex</summary>
public sealed class ColourButton : Button
{
    protected override Type StyleKeyOverride => typeof(Button);

    static readonly string[] Palette =
    {
        "#000000", "#FFFFFF", "#808080", "#C0C0C0", "#D7263D", "#FF5A1F", "#FF9F1C", "#FFD60A", "#A2D729", "#2EC4B6", "#1B98E0", "#3A5BFF",
        "#7B2CBF", "#E0219C", "#6B3E26", "#F2D0A4", "#0B3D2E", "#14213D", "#8A0F1E", "#FFB3C1", "#BDE0FE", "#CAFFBF", "#FDFFB6", "#FFD6A5",
        "#FFF4E0", "#FFE0B3", "#C7D6FF", "#9DB8FF"
    };

    public Color Value { get; private set; }
    public event Action<Color>? Changed;
    readonly Border swatch = new() { Height = 18, MinWidth = 60, CornerRadius = new CornerRadius(4), BorderBrush = Look.Edge, BorderThickness = new Thickness(1) };
    readonly WrapPanel own = new();
    readonly Slider red = Part(), green = Part(), blue = Part();
    readonly TextBox hex = new() { FontSize = 12, Width = 90, MinHeight = 26, Height = 26, Padding = new Thickness(5, 2), Watermark = "#RRGGBB" };
    bool filling;
    string? shownHex;

    static Slider Part() => new() { Minimum = 0, Maximum = 255, Width = 170, Margin = new Thickness(0, -10, 0, -10) };

    public ColourButton(Color start)
    {
        Padding = new Thickness(4);
        HorizontalAlignment = HorizontalAlignment.Stretch;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        Content = swatch;

        var palette = new WrapPanel { Width = 196 };
        foreach (var c in Palette) palette.Children.Add(Chip(Color.Parse(c)));
        var panel = new StackPanel { Spacing = 6, Width = 200 };
        panel.Children.Add(palette);
        panel.Children.Add(own);
        foreach (var s in new[] { red, green, blue })
        {
            s.PropertyChanged += (_, e) =>
            {
                if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty && !filling)
                    Pick(Color.FromRgb((byte)red.Value, (byte)green.Value, (byte)blue.Value));
            };
            panel.Children.Add(s);
        }
        panel.Children.Add(hex);
        hex.TextChanged += (_, _) =>
        {
            // TextChanged arrives late: what we put there ourselves isn't typing
            if (hex.Text != shownHex && hex.Text is { Length: 7 } t && Color.TryParse(t, out var c)) Pick(c);
        };
        Flyout = new Flyout { Content = panel };
        SetValue(start);
    }

    Button Chip(Color c)
    {
        var b = new Button
        {
            Width = 24, Height = 22, MinWidth = 0, MinHeight = 0, Padding = new Thickness(0), Margin = new Thickness(0, 0, 4, 4), Focusable = false,
            Background = new SolidColorBrush(c), BorderBrush = Look.Edge, CornerRadius = new CornerRadius(4), BorderThickness = new Thickness(1)
        };
        ToolTip.SetTip(b, $"#{c.R:X2}{c.G:X2}{c.B:X2}");
        b.Click += (_, _) => Pick(c);
        return b;
    }

    /// <summary>More colours to choose from, the texture's own for example</summary>
    public void SetOwn(IEnumerable<Color> colours)
    {
        own.Children.Clear();
        foreach (var c in colours) own.Children.Add(Chip(c));
    }

    void Pick(Color c)
    {
        SetValue(c);
        Changed?.Invoke(c);
    }

    public void SetValue(Color c)
    {
        Value = c;
        swatch.Background = new SolidColorBrush(c);
        filling = true;
        red.Value = c.R; green.Value = c.G; blue.Value = c.B;
        shownHex = $"#{c.R:X2}{c.G:X2}{c.B:X2}";
        hex.Text = shownHex;
        filling = false;
    }
}

/// <summary>A number with decimals. Type it, or use the arrows, the Up and Down keys or the wheel. Shift: bigger steps, Ctrl: finer</summary>
public sealed class NumberBox : Grid
{
    readonly TextBox box = new()
    {
        MinHeight = 26, Height = 26, MinWidth = 0, Padding = new Thickness(5, 2, 1, 2), FontSize = 12,
        VerticalContentAlignment = VerticalAlignment.Center, BorderThickness = new Thickness(1, 1, 0, 1),
        CornerRadius = new CornerRadius(Look.Radius, 0, 0, Look.Radius)
    };
    readonly double min, max;
    readonly bool whole;
    string? shown;   // text we put there: TextChanged arrives late, this tells it from typing

    public event Action<double>? Edited;
    public event Action? Done;
    public double Value { get; private set; }

    public NumberBox(double min = double.MinValue, double max = double.MaxValue, bool whole = false)
    {
        this.min = min; this.max = max; this.whole = whole;
        ColumnDefinitions = new ColumnDefinitions("*,15");
        Children.Add(box);

        var arrows = new Grid { RowDefinitions = new RowDefinitions("*,*"), Height = 26 };
        Grid.SetColumn(arrows, 1);
        arrows.Children.Add(Arrow("▲", 1, 0));
        arrows.Children.Add(Arrow("▼", -1, 1));
        Children.Add(arrows);

        box.TextChanged += (_, _) =>
        {
            if (box.Text == null || box.Text == shown) return;
            if (Parse(box.Text, out var v) && v >= min && v <= max)
            {
                Value = whole ? Math.Round(v) : v;
                Edited?.Invoke(Value);
            }
        };
        box.LostFocus += (_, _) => Done?.Invoke();
        box.AddHandler(KeyDownEvent, (_, e) =>
        {
            if (e.Key == Key.Up) { Step(1, e.KeyModifiers); e.Handled = true; }
            else if (e.Key == Key.Down) { Step(-1, e.KeyModifiers); e.Handled = true; }
            else if (e.Key == Key.Enter) { Done?.Invoke(); box.SelectAll(); e.Handled = true; }
        }, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        PointerWheelChanged += (_, e) =>
        {
            if (!box.IsFocused) return;
            Step(Math.Sign(e.Delta.Y), e.KeyModifiers);
            e.Handled = true;
        };
    }

    RepeatButton Arrow(string text, int dir, int row)
    {
        var b = new RepeatButton
        {
            Content = new TextBlock { Text = text, FontSize = 7, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center },
            Padding = new Thickness(0), MinHeight = 0, MinWidth = 0, CornerRadius = row == 0 ? new CornerRadius(0, Look.Radius, 0, 0) : new CornerRadius(0, 0, Look.Radius, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center, Focusable = false
        };
        Grid.SetRow(b, row);
        b.Click += (_, _) => Step(dir, KeyModifiers.None);
        return b;
    }

    // Steps that suit the size of the number: 1500 goes by 10, 40 by 1, 0.8 by 0.01
    double StepSize()
    {
        if (whole) return 1;
        var a = Math.Abs(Value);
        return a >= 1000 ? 10 : a >= 100 ? 1 : a >= 10 ? 0.5 : a >= 1 ? 0.1 : a >= 0.1 ? 0.01 : 0.001;
    }

    public void Step(int dir, KeyModifiers mods)
    {
        var step = StepSize();
        if (mods.HasFlag(KeyModifiers.Shift)) step *= 10;
        if (mods.HasFlag(KeyModifiers.Control) && !whole) step /= 10;
        var v = Math.Round(Math.Clamp(Value + dir * step, min, max), 6);
        Value = v;
        shown = Format(v);
        box.Text = shown;
        Edited?.Invoke(v);
    }

    static string Format(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);
    static bool Parse(string s, out double v) => double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    public void Show(double v, bool force = false)
    {
        Value = v;
        if (box.IsFocused && !force) return;
        shown = Format(v);
        if (box.Text != shown) box.Text = shown;
    }
}
