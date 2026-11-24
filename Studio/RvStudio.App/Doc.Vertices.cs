using System.Numerics;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// Editing vertices: the points of one part's model, or the corners of one hull part. Pick some, then the usual
/// move, turn and size tools (handles, arrows, W E R and a typed amount) act on them instead of on the whole part.
/// One undo step per gesture, as for everything else. A hull part is made again from its points after each change,
/// so it stays a closed convex solid (a point pulled inside it stops being a corner).
/// </summary>
public sealed partial class Doc
{
    public bool VertexMode { get; private set; }
    public Selection VertexPart { get; private set; }
    public readonly HashSet<int> PickedVerts = new();
    public event Action? VerticesChanged;

    /// <summary>Parts whose vertices can be edited: the ones with a model of their own, and hull parts</summary>
    public bool CanEditVertices(Selection s) =>
        s.Kind == SelKind.HullPart ? Hull != null && s.Index < Hull.Convex.Count
        : s.Kind is SelKind.Body or SelKind.Wheel or SelKind.Spring or SelKind.Axle or SelKind.Pin or SelKind.Spinner && VertexTarget(s).Model != null;

    /// <summary>Into vertex editing on what's selected; false (and why) when it has no vertices to edit</summary>
    public string? EnterVertexMode()
    {
        if (!CanEditVertices(Sel)) return Sel.Kind == SelKind.None ? "Click a part first" : Sel.Label + " has no vertices to edit";
        VertexMode = true;
        VertexPart = Sel;
        PickedVerts.Clear();
        VerticesChanged?.Invoke();
        Changed?.Invoke();
        return null;
    }

    public void LeaveVertexMode()
    {
        if (!VertexMode) return;
        VertexMode = false;
        PickedVerts.Clear();
        VerticesChanged?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>Another part was clicked: its vertices if it has any, else back to whole parts</summary>
    void VertexFollowSelection(Selection s)
    {
        if (!VertexMode || s == VertexPart) return;
        if (CanEditVertices(s)) { VertexPart = s; PickedVerts.Clear(); VerticesChanged?.Invoke(); }
        else LeaveVertexMode();
    }

    /// <summary>The model a part draws and where it is on the car (null model: a hull part, its points are where they are)</summary>
    public (Prm? Model, Matrix4x4 World) VertexTarget(Selection s)
    {
        if (D is not { } d || s.Kind == SelKind.HullPart) return (null, Matrix4x4.Identity);
        var kind = s.Kind switch
        {
            SelKind.Body => PartKind.Body, SelKind.Wheel => PartKind.Wheel, SelKind.Spring => PartKind.Spring,
            SelKind.Axle => PartKind.Axle, SelKind.Pin => PartKind.Pin, SelKind.Spinner => PartKind.Spinner,
            _ => (PartKind?)null
        };
        if (kind == null) return (null, Matrix4x4.Identity);
        foreach (var p in CarPose.Build(d, null))
            if (p.Kind == kind && (kind is PartKind.Body or PartKind.Spinner || p.Index == s.Index) && Model(p.Slot) is { } m)
                return (m, p.World);
        return (null, Matrix4x4.Identity);
    }

    /// <summary>Every vertex of the part being edited, where it is on the car</summary>
    public Vector3[] VertexPositions()
    {
        if (!VertexMode) return Array.Empty<Vector3>();
        if (VertexPart.Kind == SelKind.HullPart)
            return Hull != null && VertexPart.Index < Hull.Convex.Count ? (Vector3[])Hull.Convex[VertexPart.Index].Points.Clone() : Array.Empty<Vector3>();
        var (m, w) = VertexTarget(VertexPart);
        return m == null ? Array.Empty<Vector3>() : m.Verts.Select(v => Vector3.Transform(v.Pos, w)).ToArray();
    }

    /// <summary>The model's edges, as pairs of vertex numbers (each once): what the view draws while editing</summary>
    public List<(int A, int B)> VertexEdges()
    {
        var r = new List<(int, int)>();
        if (!VertexMode) return r;
        if (VertexPart.Kind == SelKind.HullPart)
        {
            if (Hull != null && VertexPart.Index < Hull.Convex.Count) foreach (var (a, b) in Hull.Convex[VertexPart.Index].Edges) r.Add((a, b));
            return r;
        }
        var (m, _) = VertexTarget(VertexPart);
        if (m == null) return r;
        var seen = new HashSet<long>();
        foreach (var f in m.Faces)
        {
            int n = f.IsQuad ? 4 : 3;
            for (int k = 0; k < n; k++)
            {
                int a = f.Index(k), b = f.Index((k + 1) % n);
                long key = Math.Min(a, b) * 1_000_000L + Math.Max(a, b);
                if (seen.Add(key)) r.Add((a, b));
            }
        }
        return r;
    }

    public Vector3? VertexAnchor()
    {
        if (!VertexMode || PickedVerts.Count == 0) return null;
        var all = VertexPositions();
        var picked = PickedVerts.Where(i => i < all.Length).ToList();
        if (picked.Count == 0) return null;
        var sum = Vector3.Zero;
        foreach (var i in picked) sum += all[i];
        return sum / picked.Count;
    }

    public void PickVertices(IEnumerable<int> which, bool add, bool remove = false)
    {
        if (!add && !remove) PickedVerts.Clear();
        foreach (var i in which) { if (remove) PickedVerts.Remove(i); else PickedVerts.Add(i); }
        VerticesChanged?.Invoke();
        Changed?.Invoke();
    }

    bool EditingVertices(Selection s) => VertexMode && s == VertexPart && PickedVerts.Count > 0;

    /// <summary>The picked vertices through a change of where they are on the car: moved, turned or sized</summary>
    void MapVertices(string label, Func<Vector3, Vector3> map)
    {
        if (!VertexMode || PickedVerts.Count == 0) return;
        if (VertexPart.Kind == SelKind.HullPart)
        {
            int k = VertexPart.Index;
            Edit(label, () => HullChange(h =>
            {
                if (k >= h.Convex.Count) return;
                var pts = h.Convex[k].Points.ToArray();
                var moved = new List<Vector3>();
                foreach (var i in PickedVerts) if (i < pts.Length) { pts[i] = map(pts[i]); moved.Add(pts[i]); }
                // A convex solid again; the picked points found again by where they are now
                if (CarTools.ConvexFrom(pts) is not { } c) return;
                h.Convex[k] = c;
                PickedVerts.Clear();
                for (int i = 0; i < c.Points.Length; i++)
                    if (moved.Any(p => (p - c.Points[i]).LengthSquared() < 1e-3f)) PickedVerts.Add(i);
            }));
            VerticesChanged?.Invoke();
            return;
        }
        var (m, w) = VertexTarget(VertexPart);
        if (m == null || !Matrix4x4.Invert(w, out var inv)) return;
        Edit(label, () =>
        {
            Remember(label, m);
            var nv = (PrmVertex[])m.Verts.Clone();
            foreach (var i in PickedVerts)
                if (i < nv.Length) nv[i].Pos = Vector3.Transform(map(Vector3.Transform(nv[i].Pos, w)), inv);
            m.Verts = nv;
            m.ComputeBounds();
            DirtyModels.Add(m);
            ModelVersion++;
        });
    }

    public void MoveVertices(Vector3 delta) => MapVertices("Move vertices", p => p + delta);

    public void RotateVertices(Vector3 axis, float angle)
    {
        if (VertexAnchor() is not { } c) return;
        var r = Matrix4x4.CreateTranslation(-c) * Matrix4x4.CreateFromAxisAngle(axis, angle) * Matrix4x4.CreateTranslation(c);
        MapVertices("Turn vertices", p => Vector3.Transform(p, r));
    }

    public void ScaleVertices(Vector3 f)
    {
        if (VertexAnchor() is not { } c) return;
        MapVertices("Size vertices", p => c + (p - c) * f);
    }

    /// <summary>The model files a vertex edit changes (the hull is written on Save as always)</summary>
    public List<Prm> VertexModels() => VertexMode && VertexPart.Kind != SelKind.HullPart && VertexTarget(VertexPart).Model is { } m ? new() { m } : new();
}
