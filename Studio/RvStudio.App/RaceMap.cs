using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>The road from above, with everyone on it</summary>
public sealed class RaceMap : Control
{
    public Race? Race;

    // The look's own brushes: they change colour in place when the theme does
    static IBrush Back => Look.MapBack;
    static IBrush Me => Look.MapMe;
    static IBrush Others => Look.MapOther;
    static readonly Pen Road = new(Look.MapRoad, 2.5);

    public override void Render(DrawingContext g)
    {
        if (Race is not { } race) return;
        g.DrawRectangle(Back, new Pen(Look.Line, 1), new Rect(Bounds.Size).Deflate(0.5), Look.BigRadius, Look.BigRadius);
        var pts = race.Track.Points;
        float minX = pts.Min(p => p.X), maxX = pts.Max(p => p.X), minZ = pts.Min(p => p.Z), maxZ = pts.Max(p => p.Z);
        double pad = 10, scale = Math.Min((Bounds.Width - 2 * pad) / Math.Max(maxX - minX, 1), (Bounds.Height - 2 * pad) / Math.Max(maxZ - minZ, 1));
        double ox = (Bounds.Width - (maxX - minX) * scale) / 2, oy = (Bounds.Height - (maxZ - minZ) * scale) / 2;
        Point At(float x, float z) => new(ox + (x - minX) * scale, Bounds.Height - oy - (z - minZ) * scale);

        var line = new StreamGeometry();
        using (var c = line.Open())
        {
            c.BeginFigure(At(pts[0].X, pts[0].Z), false);
            for (int i = 1; i < pts.Length; i += 2) c.LineTo(At(pts[i].X, pts[i].Z));
            c.EndFigure(true);
        }
        g.DrawGeometry(null, Road, line);
        foreach (var car in race.Cars.OrderBy(c => c.IsPlayer ? 1 : 0))
        {
            var p = car.Sim.Pos;
            g.DrawEllipse(car.IsPlayer ? Me : Others, null, At(p.X, p.Z), car.IsPlayer ? 5 : 3.5, car.IsPlayer ? 5 : 3.5);
        }
    }
}
