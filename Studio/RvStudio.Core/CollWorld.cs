using System.Numerics;

namespace RvStudio.Core;

/// <summary>A collision polygon: its plane, one plane per edge facing outwards, its box and surface</summary>
public sealed class CollPoly
{
    public Vector3 Normal;
    public float D;
    public (Vector3 N, float D)[] Edges = Array.Empty<(Vector3, float)>();
    public Vector3 Min, Max;
    public Material Mat = Material.Default;
    internal int Stamp;

    public float Distance(Vector3 p) => Vector3.Dot(Normal, p) + D;

    /// <summary>A flat convex polygon facing the way of hint. Corners go in order around it</summary>
    public static CollPoly Make(Vector3[] corners, Vector3 hint, Material? mat = null)
    {
        var n = Vector3.Normalize(Vector3.Cross(corners[1] - corners[0], corners[2] - corners[0]));
        if (Vector3.Dot(n, hint) < 0) n = -n;
        var centre = corners.Aggregate(Vector3.Zero, (s, c) => s + c) / corners.Length;
        var p = new CollPoly { Normal = n, D = -Vector3.Dot(n, corners[0]), Mat = mat ?? Material.Default, Min = corners[0], Max = corners[0] };
        p.Edges = new (Vector3, float)[corners.Length];
        for (int i = 0; i < corners.Length; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % corners.Length];
            var e = Vector3.Normalize(Vector3.Cross(n, b - a));
            if (Vector3.Dot(e, centre - a) > 0) e = -e;
            p.Edges[i] = (e, -Vector3.Dot(e, a));
            p.Min = Vector3.Min(p.Min, a);
            p.Max = Vector3.Max(p.Max, a);
        }
        return p;
    }

    /// <summary>
    /// A moving sphere against this polygon, the game's way: on the face, or on one edge. A vertex (outside two edges)
    /// doesn't count. Normal is what pushes the sphere out, Rel goes from the sphere's centre to the contact
    /// </summary>
    public bool Touch(Vector3 from, Vector3 to, float radius, out Vector3 normal, out Vector3 rel, out Vector3 world, out float depth)
    {
        const float Epsilon = 2;
        normal = rel = world = default;
        depth = 0;
        float now = Distance(to);
        if (now - radius > Epsilon) return false;
        if (Distance(from) < -(radius + Epsilon)) return false;

        int outside = 0, edge = -1;
        Span<float> dist = stackalloc float[Edges.Length];
        for (int i = 0; i < Edges.Length; i++)
        {
            dist[i] = Vector3.Dot(Edges[i].N, to) + Edges[i].D;
            if (dist[i] >= 0) { outside++; edge = i; }
        }

        if (outside == 0)
        {
            depth = now - radius;
            normal = Normal;
            rel = -radius * normal;
            world = to - now * Normal;
            return true;
        }
        if (outside != 1) return false;

        var toEdge = -dist[edge] * Edges[edge].N - now * Normal;
        float len = toEdge.Length();
        if (len > radius) return false;
        world = toEdge + to;
        depth = len - radius;
        const float Margin = 0.01f; // the game's boxes are exact, floats here need a hair
        if (world.X < Min.X - Margin || world.X > Max.X + Margin || world.Y < Min.Y - Margin || world.Y > Max.Y + Margin ||
            world.Z < Min.Z - Margin || world.Z > Max.Z + Margin) return false;
        for (int i = 0; i < Edges.Length; i++)
            if (i != edge && Vector3.Dot(Edges[i].N, world) + Edges[i].D > 0) return false;
        normal = len > 1e-5f ? toEdge / -len : Normal;
        rel = -radius * normal;
        return true;
    }
}

/// <summary>What the cars collide with: fixed polygons, and ground made on demand from a height</summary>
public sealed class CollWorld
{
    const float CellSize = 400;
    readonly Dictionary<(int, int), List<CollPoly>> cells = new();
    readonly Dictionary<(int, int), CollPoly[]> tiles = new();
    readonly Func<float, float, float>? height;
    readonly Func<float, float, Material>? surface;
    readonly float tile;
    int stamp;

    public static readonly Vector3 Up = new(0, -1, 0);

    public CollWorld(Func<float, float, float>? height = null, float tile = 4000, Func<float, float, Material>? surface = null)
    {
        this.height = height;
        this.tile = tile;
        this.surface = surface;
    }

    public static CollWorld Flat(Material? mat = null, float y = 0) => new((_, _) => y, 4000, mat == null ? null : (_, _) => mat);

    public void Add(CollPoly p)
    {
        for (int x = Cell(p.Min.X); x <= Cell(p.Max.X); x++)
            for (int z = Cell(p.Min.Z); z <= Cell(p.Max.Z); z++)
            {
                if (!cells.TryGetValue((x, z), out var list)) cells[(x, z)] = list = new();
                list.Add(p);
            }
    }

    static int Cell(float v) => (int)MathF.Floor(v / CellSize);

    /// <summary>Everything whose box meets this box, each polygon once</summary>
    public void Query(Vector3 min, Vector3 max, List<CollPoly> into)
    {
        into.Clear();
        stamp++;
        void Take(CollPoly p)
        {
            if (p.Stamp == stamp) return;
            p.Stamp = stamp;
            if (p.Max.X < min.X || p.Min.X > max.X || p.Max.Y < min.Y || p.Min.Y > max.Y || p.Max.Z < min.Z || p.Min.Z > max.Z) return;
            into.Add(p);
        }
        for (int x = Cell(min.X); x <= Cell(max.X); x++)
            for (int z = Cell(min.Z); z <= Cell(max.Z); z++)
                if (cells.TryGetValue((x, z), out var list))
                    foreach (var p in list) Take(p);
        if (height == null) return;
        for (int x = (int)MathF.Floor(min.X / tile); x <= (int)MathF.Floor(max.X / tile); x++)
            for (int z = (int)MathF.Floor(min.Z / tile); z <= (int)MathF.Floor(max.Z / tile); z++)
                foreach (var p in Tile(x, z)) Take(p);
    }

    CollPoly[] Tile(int x, int z)
    {
        if (tiles.TryGetValue((x, z), out var made)) return made;
        if (tiles.Count > 40000) tiles.Clear();
        float x0 = x * tile, z0 = z * tile, x1 = x0 + tile, z1 = z0 + tile;
        var a = new Vector3(x0, height!(x0, z0), z0);
        var b = new Vector3(x1, height(x1, z0), z0);
        var c = new Vector3(x1, height(x1, z1), z1);
        var d = new Vector3(x0, height(x0, z1), z1);
        var mat = surface?.Invoke((x0 + x1) / 2, (z0 + z1) / 2);
        made = a.Y == b.Y && b.Y == c.Y && c.Y == d.Y
            ? new[] { CollPoly.Make(new[] { a, b, c, d }, Up, mat) }
            : new[] { CollPoly.Make(new[] { a, b, c }, Up, mat), CollPoly.Make(new[] { a, c, d }, Up, mat) };
        tiles[(x, z)] = made;
        return made;
    }
}
