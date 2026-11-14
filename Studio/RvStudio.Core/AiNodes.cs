using System.Numerics;

namespace RvStudio.Core;

/// <summary>
/// One AI node as the game's CPU driver sees it: a line across the road from its left edge to its right, with the
/// place of the racing line and of the overtaking line along it. What is measured to the next node belongs to this one
/// </summary>
public sealed class AiNode
{
    public int Index, Type, Zone;
    public Vector3 Left, Right;
    /// <summary>Where the lines cross this node: 0 at the left edge, 1 at the right</summary>
    public float Racing = 0.5f, Overtake = 0.5f;
    /// <summary>The point on the racing line, not the middle of the road: all the driver's geometry starts there</summary>
    public Vector3 Centre;
    public AiNode Next = null!, Prev = null!;

    // The stretch from here to the next node
    public float Dist, Width;
    public Vector3 Forward, Side;
    Vector4 planeLeft, planeRight;
    Vector2 min, max;

    public Vector3 Line(float t) => Left + (Right - Left) * t;

    /// <summary>How far inside the left edge a point is. Below zero it's off the road</summary>
    public float EdgeLeft(Vector3 p) => planeLeft.X * p.X + planeLeft.Y * p.Y + planeLeft.Z * p.Z + planeLeft.W;
    public float EdgeRight(Vector3 p) => planeRight.X * p.X + planeRight.Y * p.Y + planeRight.Z * p.Z + planeRight.W;

    internal void Measure()
    {
        Width = Vector3.Distance(Left, Right);
        Dist = MathF.Max(Vector3.Distance(Centre, Next.Centre), 1);
        var f = Next.Centre - Centre;
        f.Y = 0;
        Forward = f.LengthSquared() > 1e-4f ? Vector3.Normalize(f) : Vector3.UnitZ;
        Side = RaceTrack.Side(Forward);
        if (Vector3.Dot(Side, Right - Left) < 0) Side = -Side;
        planeLeft = Wall(Left, Next.Left, Right);
        planeRight = Wall(Right, Next.Right, Left);
        min = new Vector2(MathF.Min(MathF.Min(Left.X, Right.X), MathF.Min(Next.Left.X, Next.Right.X)), MathF.Min(MathF.Min(Left.Z, Right.Z), MathF.Min(Next.Left.Z, Next.Right.Z)));
        max = new Vector2(MathF.Max(MathF.Max(Left.X, Right.X), MathF.Max(Next.Left.X, Next.Right.X)), MathF.Max(MathF.Max(Left.Z, Right.Z), MathF.Max(Next.Left.Z, Next.Right.Z)));
    }

    // An upright plane along one edge, facing the road
    static Vector4 Wall(Vector3 a, Vector3 b, Vector3 inside)
    {
        var normal = Vector3.Cross(b - a, Vector3.UnitY);
        normal = normal.LengthSquared() > 1e-6f ? Vector3.Normalize(normal) : Vector3.UnitX;
        if (Vector3.Dot(normal, inside - a) < 0) normal = -normal;
        return new Vector4(normal, -Vector3.Dot(normal, a));
    }

    /// <summary>Seen from above, is this point over the stretch from this node to the next? Height doesn't count</summary>
    public bool Over(Vector3 p)
    {
        if (p.X < min.X || p.X > max.X || p.Z < min.Y || p.Z > max.Y) return false;
        Span<Vector3> q = stackalloc Vector3[4] { Next.Left, Next.Right, Right, Left };
        bool inside = false;
        for (int i = 0, j = 3; i < 4; j = i++)
            if ((q[i].Z > p.Z) != (q[j].Z > p.Z) && p.X < (q[j].X - q[i].X) * (p.Z - q[i].Z) / (q[j].Z - q[i].Z) + q[i].X)
                inside = !inside;
        return inside;
    }
}

/// <summary>
/// The AI nodes of a built-in road, made the way a track maker lays them: a node every few car lengths, edge to edge,
/// the racing line leaning to the inside of each bend and the overtaking line beside it. One route, no forks.
/// The game's zones aren't on these roads: a run of nodes stands in for each
/// </summary>
public sealed class AiNet
{
    public readonly List<AiNode> Nodes = new();
    public int Zones { get; private set; }
    readonly int[] nodeOfPoint;

    // Node types the driver acts on. A generated road has only the first
    public const int RacingLine = 0, Slow25 = 4, JumpWall = 6, TitleSlow = 7, OffThrottle = 13, OffThrottlePetrol = 14, Slow15 = 16, Slow20 = 17, Slow30 = 18;

    const int Every = 3;             // road points to a node: 270 units, as far apart as a stock track's
    const float ZoneLength = 2500;   // a zone of a stock track covers about this much road
    const float Lean = 300, MostLean = 0.28f, Beside = 0.27f;

    public AiNet(RaceTrack track)
    {
        int points = track.Points.Length;
        int count = Math.Max(points / Every, 4);
        nodeOfPoint = new int[points];

        // How hard the road turns at each point, right as positive, evened out over the length of a bend's entry
        var turn = new float[points];
        for (int i = 0; i < points; i++)
        {
            var bend = track.Tangents[(i + 1) % points] - track.Tangents[(i - 1 + points) % points];
            float way = Vector3.Dot(RaceTrack.Side(track.Tangents[i]), bend) >= 0 ? 1 : -1;
            turn[i] = way / track.Radius[i];
        }
        float Turn(int at)
        {
            const int Reach = 7;
            float sum = 0;
            for (int k = -Reach; k <= Reach; k++) sum += turn[((at + k) % points + points) % points];
            return sum / (Reach * 2 + 1);
        }

        float half = track.Width / 2;
        var racing = new float[count];
        var beside = new float[count];
        var at = new int[count];
        for (int k = 0; k < count; k++)
        {
            at[k] = (int)((long)k * points / count);
            racing[k] = 0.5f + Math.Clamp(Turn(at[k]) * Lean, -MostLean, MostLean);
            // The other line keeps to the outside of a bend, and to the right on a straight
            beside[k] = racing[k] > 0.52f ? -Beside : Beside;
        }
        for (int k = 0; k < count; k++)
        {
            int i = at[k];
            var side = RaceTrack.Side(track.Tangents[i]);
            Vector3 Edge(float off)
            {
                var p = track.Points[i] + side * off;
                p.Y = track.Height(p.X, p.Z);
                return p;
            }
            // The side the overtaking line is on changes over a few nodes, never at once
            float shift = 0;
            for (int j = -3; j <= 3; j++) shift += beside[((k + j) % count + count) % count];
            var node = new AiNode
            {
                Index = k, Left = Edge(-half), Right = Edge(half), Racing = racing[k],
                Overtake = Math.Clamp(racing[k] + shift / 7, 0.2f, 0.8f),
            };
            node.Centre = node.Line(node.Racing);
            Nodes.Add(node);
        }
        for (int k = 0; k < count; k++)
        {
            Nodes[k].Next = Nodes[(k + 1) % count];
            Nodes[k].Prev = Nodes[(k - 1 + count) % count];
        }
        foreach (var n in Nodes) n.Measure();

        int perZone = Math.Max((int)(ZoneLength / (track.Length / count)), 1);
        foreach (var n in Nodes) n.Zone = n.Index / perZone;
        Zones = Nodes[^1].Zone + 1;

        for (int i = 0, k = 0; i < points; i++)
        {
            while (k + 1 < count && at[k + 1] <= i) k++;
            nodeOfPoint[i] = k;
        }
    }

    /// <summary>The node whose stretch a point of the road belongs to</summary>
    public AiNode NodeAt(int point) => Nodes[nodeOfPoint[Math.Clamp(point, 0, nodeOfPoint.Length - 1)]];

    /// <summary>The game's last resort for a car that lost its node: the nodes of its zone, the next one, the one before</summary>
    public AiNode? FindInZones(Vector3 p, int zone)
    {
        foreach (int z in new[] { zone, (zone + 1) % Zones, (zone - 1 + Zones) % Zones })
            foreach (var n in Nodes)
                if (n.Zone == z && n.Over(p)) return n;
        return null;
    }
}
