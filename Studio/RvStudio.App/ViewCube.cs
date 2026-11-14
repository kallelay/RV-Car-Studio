using System.Globalization;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Point = Avalonia.Point;
using Vector3 = System.Numerics.Vector3;

namespace RvStudio.App;

/// <summary>
/// A small view cube in the corner of the 3D view, as in 3ds Max. Click a face, an edge or a corner to look from
/// there; drag the cube to go round the car; drag the ring to turn about the up axis. Show or hide it in Show ▾.
/// </summary>
public sealed class ViewCube : Control
{
    public Viewport? View;

    const double Size = 84, CubeScale = 18;
    static readonly Typeface Face = new("Inter", FontStyle.Normal, FontWeight.SemiBold);

    // Car coordinates: Y down, the nose to +Z, as the Front view (3) looks from
    static readonly (Vector3 N, string Name)[] Faces =
    {
        (-Vector3.UnitY, "TOP"), (Vector3.UnitY, "BOTTOM"), (Vector3.UnitZ, "FRONT"),
        (-Vector3.UnitZ, "BACK"), (-Vector3.UnitX, "LEFT"), (Vector3.UnitX, "RIGHT"),
    };

    public ViewCube()
    {
        Width = Size;
        Height = Size;
        Cursor = new Cursor(StandardCursorType.Hand);
        ClipToBounds = false;
    }

    Point Centre => new(Bounds.Width / 2, Bounds.Height / 2 - 4);

    // The view's axes, as the view makes them with -Y up
    (Vector3 X, Vector3 Y, Vector3 Z) Basis()
    {
        var v = View!;
        float cp = MathF.Cos(v.Pitch);
        var z = Vector3.Normalize(new Vector3(MathF.Sin(v.Yaw) * cp, -MathF.Sin(v.Pitch), MathF.Cos(v.Yaw) * cp));
        var x = Vector3.Cross(-Vector3.UnitY, z);
        x = x.LengthSquared() > 1e-8f ? Vector3.Normalize(x) : Vector3.UnitX;
        return (x, Vector3.Cross(z, x), z);
    }

    Point Project(Vector3 w, (Vector3 X, Vector3 Y, Vector3 Z) b)
    {
        var c = Centre;
        return new Point(c.X + Vector3.Dot(w, b.X) * CubeScale, c.Y - Vector3.Dot(w, b.Y) * CubeScale);
    }

    static (Vector3 A, Vector3 B) FaceAxes(Vector3 n) =>
        MathF.Abs(n.Y) > 0.5f ? (Vector3.UnitX, Vector3.UnitZ) : MathF.Abs(n.X) > 0.5f ? (Vector3.UnitZ, Vector3.UnitY) : (Vector3.UnitX, Vector3.UnitY);

    // Which face, and where on it from -1 to 1, is under a point of the control
    (Vector3 N, float U, float V)? HitCube(Point p)
    {
        if (View == null) return null;
        var b = Basis();
        foreach (var (n, _) in Faces)
        {
            if (Vector3.Dot(n, b.Z) <= 0.02f) continue;
            var (a, bb) = FaceAxes(n);
            var c = Project(n, b); var pa = Project(n + a, b); var pb = Project(n + bb, b);
            double ax = pa.X - c.X, ay = pa.Y - c.Y, bx = pb.X - c.X, by = pb.Y - c.Y;
            double det = ax * by - ay * bx;
            if (Math.Abs(det) < 1e-6) continue;
            double px = p.X - c.X, py = p.Y - c.Y;
            float u = (float)((px * by - py * bx) / det), v = (float)((ax * py - ay * px) / det);
            if (MathF.Abs(u) <= 1.02f && MathF.Abs(v) <= 1.02f) return (n, u, v);
        }
        return null;
    }

    const float EdgeBand = 0.55f;

    // A face, an edge or a corner: the way to look from
    static Vector3 Region(Vector3 n, float u, float v)
    {
        var (a, b) = FaceAxes(n);
        var d = n;
        if (MathF.Abs(u) > EdgeBand) d += MathF.Sign(u) * a;
        if (MathF.Abs(v) > EdgeBand) d += MathF.Sign(v) * b;
        return Vector3.Normalize(d);
    }

    bool OnRing(Point p)
    {
        var c = Centre;
        double r = Math.Sqrt((p.X - c.X) * (p.X - c.X) + (p.Y - c.Y) * (p.Y - c.Y));
        return r < Size / 2 + 2;
    }

    // Turning by hand works in the 3D view: Top, Front and Side are flat views
    void ToPerspective()
    {
        if (View is { } v && v.View != ViewMode.Perspective) v.View = ViewMode.Perspective;
    }

    // ---------------------------------------- moving the view, with a short glide to clicked views

    readonly DispatcherTimer glide = new() { Interval = TimeSpan.FromMilliseconds(15) };
    float fromYaw, fromPitch, toYaw, toPitch;
    DateTime glideStart;

    public void LookFrom(Vector3 dir)
    {
        if (View == null) return;
        ToPerspective();
        toPitch = Math.Clamp(MathF.Asin(Math.Clamp(-dir.Y, -1, 1)), -1.55f, 1.55f);
        float flat = MathF.Sqrt(dir.X * dir.X + dir.Z * dir.Z);
        // Straight down or up keeps the nearest square heading
        toYaw = flat > 0.05f ? MathF.Atan2(dir.X, dir.Z) : MathF.Round(View.Yaw / (MathF.PI / 2)) * (MathF.PI / 2);
        fromYaw = View.Yaw; fromPitch = View.Pitch;
        float turn = toYaw - fromYaw;
        turn -= MathF.Round(turn / (MathF.PI * 2)) * MathF.PI * 2;
        toYaw = fromYaw + turn;
        glideStart = DateTime.UtcNow;
        if (!glide.IsEnabled)
        {
            glide.Tick -= Step;
            glide.Tick += Step;
            glide.Start();
        }
    }

    void Step(object? sender, EventArgs e)
    {
        if (View == null) { glide.Stop(); return; }
        float t = Math.Clamp((float)(DateTime.UtcNow - glideStart).TotalSeconds / 0.28f, 0, 1);
        float s = t * t * (3 - 2 * t);
        View.Yaw = fromYaw + (toYaw - fromYaw) * s;
        View.Pitch = fromPitch + (toPitch - fromPitch) * s;
        if (t >= 1) glide.Stop();
        View.Redraw();
        InvalidateVisual();
    }

    enum Grab { None, Cube, Ring }
    Grab grab;
    Point pressAt, last;
    bool dragged;
    (Vector3 N, float U, float V)? hover;
    bool hoverRing;

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        e.Handled = true;
        if (View == null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        var p = e.GetPosition(this);
        pressAt = last = p;
        dragged = false;
        glide.Stop();
        grab = HitCube(p) != null ? Grab.Cube : OnRing(p) ? Grab.Ring : Grab.None;
        e.Pointer.Capture(this);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var p = e.GetPosition(this);
        if (View == null) return;
        if (grab == Grab.None)
        {
            var h = HitCube(p);
            bool ring = h == null && OnRing(p);
            if (!Equals(h, hover) || ring != hoverRing)
            {
                hover = h; hoverRing = ring;
                ToolTip.SetTip(this, h is { } f ? $"Look from {Describe(Region(f.N, f.U, f.V))}. Drag to go round" : ring ? "Drag to turn about the up axis" : null);
                InvalidateVisual();
            }
            return;
        }
        float dx = (float)(p.X - last.X), dy = (float)(p.Y - last.Y);
        var lastAt = last;
        last = p;
        if (Math.Abs(p.X - pressAt.X) + Math.Abs(p.Y - pressAt.Y) > 3) dragged = true;
        if (!dragged) return;
        ToPerspective();
        if (grab == Grab.Cube)
        {
            View.Yaw -= dx * 0.01f;
            View.Pitch = Math.Clamp(View.Pitch + dy * 0.01f, -1.55f, 1.55f);
        }
        else
        {
            var c = Centre;
            double a0 = Math.Atan2(lastAt.Y - c.Y, lastAt.X - c.X), a1 = Math.Atan2(p.Y - c.Y, p.X - c.X);
            double turn = a1 - a0;
            if (turn > Math.PI) turn -= Math.PI * 2;
            if (turn < -Math.PI) turn += Math.PI * 2;
            View.Yaw += (float)turn;
        }
        View.Redraw();
        InvalidateVisual();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        e.Handled = true;
        var p = e.GetPosition(this);
        if (!dragged && grab == Grab.Cube && HitCube(p) is { } f) LookFrom(Region(f.N, f.U, f.V));
        grab = Grab.None;
        e.Pointer.Capture(null);
        InvalidateVisual();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        hover = null; hoverRing = false;
        InvalidateVisual();
    }

    static string Describe(Vector3 d)
    {
        var parts = new List<string>();
        if (d.Y < -0.3f) parts.Add("top"); else if (d.Y > 0.3f) parts.Add("bottom");
        if (d.Z > 0.3f) parts.Add("front"); else if (d.Z < -0.3f) parts.Add("back");
        if (d.X < -0.3f) parts.Add("left"); else if (d.X > 0.3f) parts.Add("right");
        return string.Join(" ", parts);
    }

    /// <summary>The way the view looks from, snapped to the cube: for tests.</summary>
    public string Facing() => View == null ? "" : Describe(Basis().Z);

    // ---------------------------------------- drawing, in the theme's colours

    static Color Mix(Color a, Color b, double t) => Color.FromArgb(
        (byte)(a.A + (b.A - a.A) * t), (byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    public override void Render(DrawingContext ctx)
    {
        if (View == null) return;
        var b = Basis();
        var panel = Look.Panel.Color; var line = Look.Line.Color; var textC = Look.Text.Color; var acc = Look.Accent.Color;
        var faceFill = new SolidColorBrush(Color.FromArgb(225, panel.R, panel.G, panel.B));
        var faceEdge = new Pen(new SolidColorBrush(Mix(line, textC, 0.25)), 1);
        var text = new SolidColorBrush(Mix(textC, panel, 0.15));
        var accent = new SolidColorBrush(Color.FromArgb(210, acc.R, acc.G, acc.B));
        var ringPen = new Pen(new SolidColorBrush(hoverRing || grab == Grab.Ring ? acc : Color.FromArgb(150, line.R, line.G, line.B)), hoverRing ? 3 : 2);

        // The ring round the bottom, with a mark to the front of the car
        var ring = new StreamGeometry();
        using (var g = ring.Open())
        {
            for (int i = 0; i <= 48; i++)
            {
                float a = i / 48f * MathF.PI * 2;
                var q = Project(new Vector3(MathF.Sin(a) * 1.75f, 1, MathF.Cos(a) * 1.75f), b);
                if (i == 0) g.BeginFigure(q, false); else g.LineTo(q);
            }
        }
        ctx.DrawGeometry(null, ringPen, ring);
        ctx.DrawEllipse(accent, null, Project(new Vector3(0, 1, 1.75f), b), 3, 3);

        // Faces, far first
        foreach (var (n, name) in Faces.OrderBy(f => Vector3.Dot(f.N, b.Z)))
        {
            float facing = Vector3.Dot(n, b.Z);
            if (facing <= 0.02f) continue;
            var (a, bb) = FaceAxes(n);
            var poly = new StreamGeometry();
            using (var g = poly.Open())
            {
                g.BeginFigure(Project(n - a - bb, b), true);
                g.LineTo(Project(n + a - bb, b));
                g.LineTo(Project(n + a + bb, b));
                g.LineTo(Project(n - a + bb, b));
                g.EndFigure(true);
            }
            ctx.DrawGeometry(faceFill, faceEdge, poly);
            if (hover is { } h && h.N == n)
            {
                // The part a click looks from: the middle, an edge strip or a corner square
                (float lo, float hi) Range(float t) => MathF.Abs(t) > EdgeBand ? (t > 0 ? (EdgeBand, 1f) : (-1f, -EdgeBand)) : (-EdgeBand, EdgeBand);
                var (u0, u1) = Range(h.U); var (v0, v1) = Range(h.V);
                var part = new StreamGeometry();
                using (var g = part.Open())
                {
                    g.BeginFigure(Project(n + a * u0 + bb * v0, b), true);
                    g.LineTo(Project(n + a * u1 + bb * v0, b));
                    g.LineTo(Project(n + a * u1 + bb * v1, b));
                    g.LineTo(Project(n + a * u0 + bb * v1, b));
                    g.EndFigure(true);
                }
                ctx.DrawGeometry(accent, null, part);
            }
            if (facing > 0.45f)
            {
                var t = new FormattedText(name, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Face, 7.5 * Math.Sqrt(facing), text);
                var c = Project(n, b);
                ctx.DrawText(t, new Point(c.X - t.Width / 2, c.Y - t.Height / 2));
            }
        }
    }
}
