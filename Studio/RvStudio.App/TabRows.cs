using Avalonia;
using Avalonia.Controls;

namespace RvStudio.App;

/// <summary>
/// The tab headers in fixed rows: a new row starts at each given tab. The right panel's tabs read as groups,
/// Selected / Files, Properties, Properties (RVGL) / Model, Paint, Shade, whatever the panel's width.
/// A row too wide for the panel goes on wrapping by itself.
/// </summary>
public sealed class TabRows : Panel
{
    readonly HashSet<int> starts;

    public TabRows(params int[] rowStarts) => starts = new HashSet<int>(rowStarts);

    IEnumerable<List<(Control C, Size S)>> Rows(double width)
    {
        var row = new List<(Control, Size)>();
        double x = 0;
        for (int i = 0; i < Children.Count; i++)
        {
            var c = Children[i];
            var s = c.DesiredSize;
            if (row.Count > 0 && (starts.Contains(i) || x + s.Width > width + 0.5))
            {
                yield return row;
                row = new List<(Control, Size)>();
                x = 0;
            }
            row.Add((c, s));
            x += s.Width;
        }
        if (row.Count > 0) yield return row;
    }

    protected override Size MeasureOverride(Size available)
    {
        foreach (var c in Children) c.Measure(new Size(double.PositiveInfinity, available.Height));
        double w = 0, h = 0;
        foreach (var row in Rows(available.Width))
        {
            w = Math.Max(w, row.Sum(r => r.S.Width));
            h += row.Max(r => r.S.Height);
        }
        return new Size(double.IsInfinity(available.Width) ? w : Math.Min(w, available.Width), h);
    }

    protected override Size ArrangeOverride(Size final)
    {
        double y = 0;
        foreach (var row in Rows(final.Width))
        {
            double x = 0, h = row.Max(r => r.S.Height);
            foreach (var (c, s) in row)
            {
                c.Arrange(new Rect(x, y, s.Width, h));
                x += s.Width;
            }
            y += h;
        }
        return final;
    }
}
