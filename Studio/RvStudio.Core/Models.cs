using System.Numerics;

namespace RvStudio.Core;

[Flags]
public enum FaceFlags : ushort
{
    Quad = 0x001,
    DoubleSided = 0x002,
    Translucent = 0x004,
    Mirror = 0x080,
    Additive = 0x100, // translucent type: adds instead of blends
    TexAnim = 0x200,
    NoEnv = 0x400,
    Env = 0x800,
    Cloth = 0x1000,
    Skip = 0x2000
}

public sealed class PrmFace
{
    public FaceFlags Flags;
    public short TPage;
    public short V0, V1, V2, V3;
    public readonly uint[] Colors = new uint[4]; // ARGB
    public readonly float[] U = new float[4];
    public readonly float[] V = new float[4];
    public bool IsQuad => (Flags & FaceFlags.Quad) != 0;
    public short Index(int corner) => corner switch { 0 => V0, 1 => V1, 2 => V2, _ => V3 };
    public int Corners => IsQuad ? 4 : 3;
}

public struct PrmVertex { public Vector3 Pos, Normal; }

/// <summary>A .prm or .m model: polygons of 60 bytes, vertices of 24. More LODs may follow the first one</summary>
public sealed class Prm
{
    public string? FilePath;
    public PrmFace[] Faces = Array.Empty<PrmFace>();
    public PrmVertex[] Verts = Array.Empty<PrmVertex>();
    public byte[]? OtherLods;
    public Vector3 Min, Max;

    /// <summary>Which vertices the polygons use. Rewriting normals can change them</summary>
    public short[] FaceIndices()
    {
        var r = new short[Faces.Length * 4];
        for (int i = 0; i < Faces.Length; i++)
        {
            var f = Faces[i];
            r[i * 4] = f.V0; r[i * 4 + 1] = f.V1; r[i * 4 + 2] = f.V2; r[i * 4 + 3] = f.V3;
        }
        return r;
    }

    public void SetFaceIndices(short[] r)
    {
        for (int i = 0; i < Faces.Length; i++)
        {
            var f = Faces[i];
            f.V0 = r[i * 4]; f.V1 = r[i * 4 + 1]; f.V2 = r[i * 4 + 2]; f.V3 = r[i * 4 + 3];
        }
    }

    public static Prm Load(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path));
        var m = Read(r);
        m.FilePath = path;
        return m;
    }

    public static Prm Read(BinaryReader r)
    {
        var m = ReadOne(r);
        var rest = r.BaseStream.Length - r.BaseStream.Position;
        if (rest > 0) m.OtherLods = r.ReadBytes((int)rest);
        return m;
    }

    /// <summary>
    /// Do the same to the farther LODs. Most stock wheels have them, and the game shows them from a distance,
    /// so a change only on the first one would make a wheel jump back as it goes away.
    /// </summary>
    public void ForOtherLods(Action<Prm> change)
    {
        if (OtherLods == null) return;
        var lods = new List<Prm>();
        using (var r = new BinaryReader(new MemoryStream(OtherLods)))
        {
            try
            {
                while (r.BaseStream.Position + 4 <= r.BaseStream.Length) lods.Add(ReadOne(r));
                if (r.BaseStream.Position != r.BaseStream.Length) return; // not LODs: don't touch
            }
            catch (EndOfStreamException) { return; }
        }
        using var o = new MemoryStream();
        using var w = new BinaryWriter(o);
        foreach (var l in lods)
        {
            change(l);
            l.Write(w);
        }
        w.Flush();
        OtherLods = o.ToArray();
    }

    static Prm ReadOne(BinaryReader r)
    {
        var m = new Prm();
        int nf = r.ReadUInt16(), nv = r.ReadUInt16();
        m.Faces = new PrmFace[nf];
        for (int i = 0; i < nf; i++)
        {
            var f = new PrmFace { Flags = (FaceFlags)r.ReadUInt16(), TPage = r.ReadInt16() };
            f.V0 = r.ReadInt16(); f.V1 = r.ReadInt16(); f.V2 = r.ReadInt16(); f.V3 = r.ReadInt16();
            for (int k = 0; k < 4; k++) f.Colors[k] = r.ReadUInt32();
            for (int k = 0; k < 4; k++) { f.U[k] = r.ReadSingle(); f.V[k] = r.ReadSingle(); }
            m.Faces[i] = f;
        }
        m.Verts = new PrmVertex[nv];
        for (int i = 0; i < nv; i++)
        {
            m.Verts[i].Pos = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
            m.Verts[i].Normal = new Vector3(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
        }
        m.ComputeBounds();
        return m;
    }

    public void ComputeBounds()
    {
        if (Verts.Length == 0) { Min = Max = Vector3.Zero; return; }
        Min = new Vector3(float.MaxValue); Max = new Vector3(float.MinValue);
        foreach (var v in Verts) { Min = Vector3.Min(Min, v.Pos); Max = Vector3.Max(Max, v.Pos); }
    }

    public void Save(string path)
    {
        using var w = new BinaryWriter(File.Create(path));
        Write(w);
        if (OtherLods != null) w.Write(OtherLods);
    }

    void Write(BinaryWriter w)
    {
        w.Write((ushort)Faces.Length);
        w.Write((ushort)Verts.Length);
        foreach (var f in Faces)
        {
            w.Write((ushort)f.Flags); w.Write(f.TPage);
            w.Write(f.V0); w.Write(f.V1); w.Write(f.V2); w.Write(f.V3);
            for (int k = 0; k < 4; k++) w.Write(f.Colors[k]);
            for (int k = 0; k < 4; k++) { w.Write(f.U[k]); w.Write(f.V[k]); }
        }
        foreach (var v in Verts)
        {
            w.Write(v.Pos.X); w.Write(v.Pos.Y); w.Write(v.Pos.Z);
            w.Write(v.Normal.X); w.Write(v.Normal.Y); w.Write(v.Normal.Z);
        }
    }

    /// <summary>The same model for the other side: X turned around, polygons kept facing outwards</summary>
    public void MirrorX()
    {
        void One(Prm m)
        {
            for (int i = 0; i < m.Verts.Length; i++)
            {
                m.Verts[i].Pos.X = -m.Verts[i].Pos.X;
                m.Verts[i].Normal.X = -m.Verts[i].Normal.X;
            }
            foreach (var f in m.Faces)
            {
                // swapping two opposite corners keeps the side that shows, for triangles and quads
                (f.V0, f.V2) = (f.V2, f.V0);
                Swap(f, 0, 2);
            }
            m.ComputeBounds();
        }
        static void Swap(PrmFace f, int a, int b)
        {
            (f.U[a], f.U[b]) = (f.U[b], f.U[a]);
            (f.V[a], f.V[b]) = (f.V[b], f.V[a]);
            (f.Colors[a], f.Colors[b]) = (f.Colors[b], f.Colors[a]);
        }
        One(this);
        ForOtherLods(One);
    }

    /// <summary>Is every polygon using vertices that exist?</summary>
    public bool IsValid()
    {
        foreach (var f in Faces)
            for (int k = 0; k < f.Corners; k++)
            {
                var i = f.Index(k);
                if (i < 0 || i >= Verts.Length) return false;
            }
        return true;
    }
}

public sealed class ConvexHull
{
    public Vector3 Min, Max, Offset;
    public Vector3[] Points = Array.Empty<Vector3>();
    public (short A, short B)[] Edges = Array.Empty<(short, short)>();
    public (Vector3 Normal, float D)[] Faces = Array.Empty<(Vector3, float)>();

    public ConvexHull Clone()
    {
        var c = (ConvexHull)MemberwiseClone();
        c.Points = (Vector3[])Points.Clone();
        c.Edges = ((short, short)[])Edges.Clone();
        c.Faces = ((Vector3, float)[])Faces.Clone();
        return c;
    }

    /// <summary>Move every point and keep the planes true. Offset is the middle, Min and Max are from it, as in the game's files</summary>
    public void Apply(Matrix4x4 m)
    {
        if (!Matrix4x4.Invert(m, out var inv)) return;
        var nm = Matrix4x4.Transpose(inv);
        for (int i = 0; i < Points.Length; i++) Points[i] = Vector3.Transform(Points[i], m);
        for (int i = 0; i < Faces.Length; i++)
        {
            var (n, d) = Faces[i];
            var on = Vector3.Transform(n * -d, m);
            var nn = Vector3.Normalize(Vector3.TransformNormal(n, nm));
            Faces[i] = (nn, -Vector3.Dot(nn, on));
        }
        if (Points.Length == 0) return;
        var lo = new Vector3(float.MaxValue); var hi = new Vector3(float.MinValue);
        foreach (var p in Points) { lo = Vector3.Min(lo, p); hi = Vector3.Max(hi, p); }
        Offset = (lo + hi) / 2;
        Min = lo - Offset;
        Max = hi - Offset;
    }
}

/// <summary>A .hul collision hull: convex parts, then spheres</summary>
public sealed class Hull
{
    public List<ConvexHull> Convex = new();
    public List<(Vector3 Pos, float Radius)> Spheres = new();
    public byte[]? Tail; // anything after the spheres, kept as it is

    public Hull Clone() => new() { Convex = Convex.Select(c => c.Clone()).ToList(), Spheres = new(Spheres), Tail = Tail };

    /// <summary>Everything in the hull. A sphere stays a sphere: its radius follows the smallest stretch</summary>
    public void Apply(Matrix4x4 m)
    {
        foreach (var c in Convex) c.Apply(m);
        float s = Math.Min(new Vector3(m.M11, m.M12, m.M13).Length(), Math.Min(new Vector3(m.M21, m.M22, m.M23).Length(), new Vector3(m.M31, m.M32, m.M33).Length()));
        for (int i = 0; i < Spheres.Count; i++) Spheres[i] = (Vector3.Transform(Spheres[i].Pos, m), Spheres[i].Radius * s);
    }

    static Vector3 V(BinaryReader r) => new(r.ReadSingle(), r.ReadSingle(), r.ReadSingle());
    static void W(BinaryWriter w, Vector3 v) { w.Write(v.X); w.Write(v.Y); w.Write(v.Z); }

    public static Hull Load(string path)
    {
        using var r = new BinaryReader(File.OpenRead(path));
        var h = new Hull();
        int n = r.ReadInt16();
        for (int i = 0; i < n; i++)
        {
            int np = r.ReadInt16(), ne = r.ReadInt16(), nf = r.ReadInt16();
            var c = new ConvexHull();
            float x0 = r.ReadSingle(), x1 = r.ReadSingle(), y0 = r.ReadSingle(), y1 = r.ReadSingle(), z0 = r.ReadSingle(), z1 = r.ReadSingle();
            c.Min = new Vector3(x0, y0, z0); c.Max = new Vector3(x1, y1, z1);
            c.Offset = V(r);
            c.Points = new Vector3[np];
            for (int k = 0; k < np; k++) c.Points[k] = V(r);
            c.Edges = new (short, short)[ne];
            for (int k = 0; k < ne; k++) c.Edges[k] = (r.ReadInt16(), r.ReadInt16());
            c.Faces = new (Vector3, float)[nf];
            for (int k = 0; k < nf; k++) { var nm = V(r); c.Faces[k] = (nm, r.ReadSingle()); }
            h.Convex.Add(c);
        }
        int ns = r.ReadInt16();
        for (int i = 0; i < ns; i++) { var p = V(r); h.Spheres.Add((p, r.ReadSingle())); }
        var rest = r.BaseStream.Length - r.BaseStream.Position;
        if (rest > 0) h.Tail = r.ReadBytes((int)rest);
        return h;
    }

    public void Save(string path)
    {
        using var w = new BinaryWriter(File.Create(path));
        w.Write((short)Convex.Count);
        foreach (var c in Convex)
        {
            w.Write((short)c.Points.Length); w.Write((short)c.Edges.Length); w.Write((short)c.Faces.Length);
            w.Write(c.Min.X); w.Write(c.Max.X); w.Write(c.Min.Y); w.Write(c.Max.Y); w.Write(c.Min.Z); w.Write(c.Max.Z);
            W(w, c.Offset);
            foreach (var p in c.Points) W(w, p);
            foreach (var e in c.Edges) { w.Write(e.A); w.Write(e.B); }
            foreach (var f in c.Faces) { W(w, f.Normal); w.Write(f.D); }
        }
        w.Write((short)Spheres.Count);
        foreach (var s in Spheres) { W(w, s.Pos); w.Write(s.Radius); }
        if (Tail != null) w.Write(Tail);
    }
}
