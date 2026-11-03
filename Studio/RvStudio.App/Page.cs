using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RvStudio.App;

/// <summary>A panel of rows connected to the car: labels, numbers, vectors, switches. Everything edits live with undo</summary>
public sealed class Page : ScrollViewer
{
    readonly StackPanel stack = new() { Spacing = 5, Margin = new Thickness(10, 8, 14, 12) };
    readonly List<Action> loaders = new();
    readonly Doc doc;
    bool loading;

    public Page(Doc d)
    {
        doc = d;
        Content = stack;
        HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled;
    }

    public void Refresh()
    {
        loading = true;
        foreach (var l in loaders) l();
        loading = false;
    }

    public static readonly IBrush Faint = new SolidColorBrush(Color.Parse("#9AA3B2"));
    public static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#4C9AFF"));

    public void Header(string text)
    {
        stack.Children.Add(new TextBlock
        {
            Text = text, FontWeight = FontWeight.SemiBold, FontSize = 13, Foreground = Accent, Margin = new Thickness(0, 10, 0, 2)
        });
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

    TextBox NumBox()
    {
        return new TextBox { MinHeight = 26, Height = 26, Padding = new Thickness(5, 2), FontSize = 12, VerticalContentAlignment = VerticalAlignment.Center };
    }

    static string Show(double v) => v.ToString("0.####", CultureInfo.InvariantCulture);

    static bool Parse(string? s, out double v) =>
        double.TryParse(s?.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out v);

    public void Num(string label, Func<double> get, Action<double> set, string? tip = null, double min = double.MinValue, double max = double.MaxValue)
    {
        var b = NumBox();
        loaders.Add(() => { if (!b.IsFocused) b.Text = Show(get()); });
        b.TextChanged += (_, _) =>
        {
            if (loading || !Parse(b.Text, out var v) || v < min || v > max) return;
            if (Math.Abs(v - get()) < 1e-9) return;
            doc.Edit(label, () => set(v));
        };
        b.LostFocus += (_, _) => b.Text = Show(get());
        Row(label, b, tip);
    }

    public void Int(string label, Func<int> get, Action<int> set, string? tip = null, int min = int.MinValue, int max = int.MaxValue) =>
        Num(label, () => get(), v => set((int)v), tip, min, max);

    public void Vec(string label, Func<Vector3> get, Action<Vector3> set, string? tip = null)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions("*,3,*,3,*") };
        var boxes = new[] { NumBox(), NumBox(), NumBox() };
        for (int i = 0; i < 3; i++)
        {
            int k = i;
            Grid.SetColumn(boxes[i], i * 2);
            g.Children.Add(boxes[i]);
            boxes[i].TextChanged += (_, _) =>
            {
                if (loading || !Parse(boxes[k].Text, out var v)) return;
                var cur = get();
                var nv = k == 0 ? cur with { X = (float)v } : k == 1 ? cur with { Y = (float)v } : cur with { Z = (float)v };
                if (nv == cur) return;
                doc.Edit(label, () => set(nv));
            };
            boxes[i].LostFocus += (_, _) => LoadVec();
        }
        void LoadVec()
        {
            var v = get();
            if (!boxes[0].IsFocused) boxes[0].Text = Show(Math.Round(v.X, 3));
            if (!boxes[1].IsFocused) boxes[1].Text = Show(Math.Round(v.Y, 3));
            if (!boxes[2].IsFocused) boxes[2].Text = Show(Math.Round(v.Z, 3));
        }
        loaders.Add(LoadVec);
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
        if (primary) b.Background = Accent;
        if (tip != null) ToolTip.SetTip(b, tip);
        b.Click += (_, _) => click();
        stack.Children.Add(b);
        return b;
    }

    public Slider Slide(string label, double min, double max, double start, Action<double>? changed = null, string? tip = null)
    {
        var s = new Slider { Minimum = min, Maximum = max, Value = start, Margin = new Thickness(0, -8, 0, -8) };
        if (changed != null) s.PropertyChanged += (_, e) => { if (e.Property == RangeBaseValue) changed(s.Value); };
        Row(label, s, tip);
        return s;
    }
    static readonly AvaloniaProperty RangeBaseValue = Avalonia.Controls.Primitives.RangeBase.ValueProperty;

    public void Add(Control c) => stack.Children.Add(c);
}
