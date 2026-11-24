using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace RvStudio.App;

/// <summary>
/// Where Auto shade's light is, and how big: two circles. The big one is the sky seen from above, the car in the
/// middle with its nose up; its edge is the horizon, its middle straight overhead. The small one is the light: drag it
/// to move the light, drag its rim (or turn the wheel over it) to make it bigger, for softer shadows, or smaller, for
/// sharper ones. A double click puts it back.
/// </summary>
public sealed class LightPad : Control
{
    /// <summary>Degrees round from the car's front towards its right side.</summary>
    public double Around { get; private set; } = 39;
    /// <summary>Degrees above the horizon: 90 is straight overhead.</summary>
    public double Elevation { get; private set; } = 55;
    /// <summary>The light's radius in degrees: 0 a point (sharp shadows), up to 40 (very soft).</summary>
    public double Radius { get; private set; } = 12;
    public const double DefaultAround = 39, DefaultHeight = 55, DefaultRadius = 12;
    public event Action? Changed;

    const double Side = 200;
    double R => Side / 2 - 14;
    Point Mid => new(Side / 2, Side / 2);

    public LightPad()
    {
        Width = Side; Height = Side;
        Cursor = new Cursor(StandardCursorType.Hand);
        ToolTip.SetTip(this, "The light, seen from above (the car's nose points up). Drag it to move it: the middle is straight overhead, the edge low on the horizon. Drag its rim or turn the wheel for its size: small, sharp shadows; large, soft ones. Double click: back to the start");
    }

    public void Set(double around, double height, double radius, bool tell = true)
    {
        Around = ((around % 360) + 360) % 360;
        Elevation = Math.Clamp(height, 5, 90);
        Radius = Math.Clamp(radius, 0, 40);
        InvalidateVisual();
        if (tell) Changed?.Invoke();
    }

    /// <summary>The light's middle on the pad.</summary>
    public Point LightAt()
    {
        double r = (90 - Elevation) / 90 * R, a = Around * Math.PI / 180;
        return new Point(Mid.X + Math.Sin(a) * r, Mid.Y - Math.Cos(a) * r);
    }

    /// <summary>The light's circle on the pad.</summary>
    public double LightSize() => Math.Max(4, Radius / 90 * R);

    public override void Render(DrawingContext dc)
    {
        var mid = Mid;
        dc.FillRectangle(Brushes.Transparent, new Rect(0, 0, Side, Side));
        var sky = new RadialGradientBrush
        {
            GradientStops = { new GradientStop(Color.FromRgb(120, 150, 190), 0), new GradientStop(Color.FromRgb(46, 56, 74), 1) }
        };
        dc.DrawEllipse(sky, new Pen(Look.Edge, 1), mid, R, R);
        var faint = new Pen(new SolidColorBrush(Colors.White, 0.18), 1);
        foreach (var h in new[] { 30.0, 60.0 }) dc.DrawEllipse(null, faint, mid, (90 - h) / 90 * R, (90 - h) / 90 * R);
        dc.DrawLine(faint, new Point(mid.X - R, mid.Y), new Point(mid.X + R, mid.Y));
        dc.DrawLine(faint, new Point(mid.X, mid.Y - R), new Point(mid.X, mid.Y + R));
        // The car from above, nose up
        var car = new StreamGeometry();
        using (var g = car.Open())
        {
            g.BeginFigure(new Point(mid.X, mid.Y - 16), true);
            g.LineTo(new Point(mid.X + 8, mid.Y - 6)); g.LineTo(new Point(mid.X + 8, mid.Y + 14));
            g.LineTo(new Point(mid.X - 8, mid.Y + 14)); g.LineTo(new Point(mid.X - 8, mid.Y - 6));
            g.EndFigure(true);
        }
        dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(200, 200, 205)), new Pen(Brushes.Black, 1), car);
        Label(dc, "front", new Point(mid.X, mid.Y - R - 12));
        // The light and its size
        var at = LightAt();
        double size = LightSize();
        var glow = new RadialGradientBrush
        {
            GradientStops = { new GradientStop(Color.FromArgb(255, 255, 244, 200), 0), new GradientStop(Color.FromArgb(120, 255, 210, 120), 0.7), new GradientStop(Color.FromArgb(0, 255, 210, 120), 1) }
        };
        dc.DrawEllipse(glow, new Pen(new SolidColorBrush(Color.FromRgb(255, 220, 120)), 1.5), at, size, size);
        dc.DrawEllipse(Brushes.White, null, at, 2.5, 2.5);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(255, 220, 120), 0.5), 1, new DashStyle(new double[] { 3, 3 }, 0)), mid, at);
        Label(dc, $"{Around:0}° round, {Elevation:0}° up, size {Radius:0}°", new Point(mid.X, Side - 6));
    }

    static void Label(DrawingContext dc, string text, Point centre)
    {
        var t = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Inter"), 10.5, Look.Faint);
        dc.DrawText(t, new Point(centre.X - t.Width / 2, centre.Y - t.Height / 2));
    }

    enum Drag { None, Move, Size }
    Drag drag;
    Point grab;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        e.Handled = true;
        var p = e.GetPosition(this);
        if (e.ClickCount >= 2) { Set(DefaultAround, DefaultHeight, DefaultRadius); return; }
        var at = LightAt();
        double d = Dist(p, at), size = LightSize();
        // The rim sizes it; anywhere else in the sky moves the light there
        if (Math.Abs(d - size) < 5 && size > 6) drag = Drag.Size;
        else { drag = Drag.Move; grab = d <= size ? new Point(at.X - p.X, at.Y - p.Y) : default; MoveTo(p); }
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        if (drag == Drag.Move) MoveTo(p);
        else if (drag == Drag.Size) Set(Around, Elevation, Dist(p, LightAt()) / R * 90);
        else
        {
            double d = Dist(p, LightAt());
            Cursor = new Cursor(Math.Abs(d - LightSize()) < 5 && LightSize() > 6 ? StandardCursorType.SizeAll : StandardCursorType.Hand);
        }
    }

    void MoveTo(Point p)
    {
        var q = new Point(p.X + grab.X - Mid.X, p.Y + grab.Y - Mid.Y);
        double r = Math.Min(Math.Sqrt(q.X * q.X + q.Y * q.Y), R);
        double around = Math.Atan2(q.X, -q.Y) * 180 / Math.PI;
        Set(r < 1 ? Around : around, 90 - r / R * 90, Radius);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        drag = Drag.None;
        e.Pointer.Capture(null);
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        e.Handled = true;
        Set(Around, Elevation, Radius + (e.Delta.Y > 0 ? 2 : -2));
    }

    static double Dist(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
