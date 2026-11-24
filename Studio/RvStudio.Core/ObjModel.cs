using System.Globalization;
using System.Numerics;
using System.Text;

namespace RvStudio.Core;

/// <summary>
/// Models to and from Wavefront .obj, the format every 3D program reads and writes (Blender, 3ds Max, Maya, Wings 3D...).
/// Axes: the game's Y points down and Z forward; the .obj has Y up, so a point (x, y, z) is written as (x, -y, -z), a half
/// turn that keeps left and right. Size: 1 metre in the .obj is 100 game units, as Blender's Re-Volt add-on does.
/// The game's polygons run clockwise, an .obj's anticlockwise, so the corners are written in the other order.
/// </summary>
public static class ObjModel
{
    public const float Scale = 0.01f;
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    static Vector3 ToObj(Vector3 p) => new(p.X, -p.Y, -p.Z);

    /// <summary>Write the model's first LOD as .obj, with a .mtl naming the texture when one is given.</summary>
    public static void Export(Prm m, string objPath, string? texturePath = null) =>
        Export(new[] { (Path.GetFileNameWithoutExtension(objPath), m, Matrix4x4.Identity) }, objPath, texturePath);

    /// <summary>Several models in one .obj, each its own object, placed by its matrix: the whole car to model around.</summary>
    public static void Export(IEnumerable<(string Name, Prm Model, Matrix4x4 World)> parts, string objPath, string? texturePath = null)
    {
        var sb = new StringBuilder();
        string name = Path.GetFileNameWithoutExtension(objPath);
        sb.AppendLine("# RV Car Studio: 1 unit = 100 game units, Y up, the car's front towards -Z");
        string? mtl = null;
        if (texturePath != null)
        {
            mtl = name + ".mtl";
            sb.AppendLine("mtllib " + mtl);
        }
        int vBase = 1, vtBase = 1;
        foreach (var (partName, m, world) in parts)
        {
            sb.AppendLine("o " + partName.Replace(' ', '_'));
            bool flip = world.GetDeterminant() < 0;
            // A vertex's colour: the average of the corners that use it (the game keeps one per corner)
            var sum = new Vector4[m.Verts.Length];
            var count = new int[m.Verts.Length];
            foreach (var f in m.Faces)
                for (int k = 0; k < f.Corners; k++)
                {
                    int i = f.Index(k);
                    if (i < 0 || i >= m.Verts.Length) continue;
                    uint c = f.Colors[k];
                    sum[i] += new Vector4((c >> 16) & 255, (c >> 8) & 255, c & 255, 0) / 255f;
                    count[i]++;
                }
            for (int i = 0; i < m.Verts.Length; i++)
            {
                var p = ToObj(Vector3.Transform(m.Verts[i].Pos, world)) * Scale;
                var c = count[i] > 0 ? sum[i] / count[i] : Vector4.One;
                sb.Append("v ").Append(F(p.X)).Append(' ').Append(F(p.Y)).Append(' ').Append(F(p.Z))
                  .Append(' ').Append(F(c.X, "0.###")).Append(' ').Append(F(c.Y, "0.###")).Append(' ').Append(F(c.Z, "0.###")).AppendLine();
            }
            foreach (var v in m.Verts)
            {
                var n = Vector3.TransformNormal(v.Normal, world);
                n = ToObj(n.LengthSquared() > 1e-12f ? Vector3.Normalize(n) : n);
                sb.Append("vn ").Append(F(n.X)).Append(' ').Append(F(n.Y)).Append(' ').Append(F(n.Z)).AppendLine();
            }
            foreach (var f in m.Faces)
                for (int k = 0; k < f.Corners; k++)
                    sb.Append("vt ").Append(F(f.U[k])).Append(' ').Append(F(1 - f.V[k])).AppendLine();
            if (mtl != null) sb.AppendLine("usemtl " + name);
            int vt = vtBase;
            foreach (var f in m.Faces)
            {
                int n = f.Corners;
                sb.Append('f');
                for (int jj = 0; jj < n; jj++)
                {
                    int j = flip ? jj : n - 1 - jj;
                    int i = f.Index(j) + vBase;
                    sb.Append(' ').Append(i).Append('/').Append(vt + j).Append('/').Append(i);
                }
                vt += n;
                sb.AppendLine();
            }
            vBase += m.Verts.Length;
            vtBase = vt;
        }
        File.WriteAllText(objPath, sb.ToString());
        if (mtl != null)
        {
            var dir = Path.GetDirectoryName(Path.GetFullPath(objPath))!;
            var tex = Path.GetFileName(texturePath!);
            try
            {
                var copy = Path.Combine(dir, tex);
                if (!File.Exists(copy) && File.Exists(texturePath)) File.Copy(texturePath!, copy);
            }
            catch (IOException) { }
            File.WriteAllText(Path.Combine(dir, mtl), $"newmtl {name}\nKd 1 1 1\nmap_Kd {tex}\n");
        }
    }

    static string F(float v, string format = "0.######") => v.ToString(format, Inv);

    public sealed record ImportReport(int Polygons, int Vertices, int SplitPolygons, int Skipped, bool HadNormals, bool HadColours, bool HadUvs);

    /// <summary>
    /// Read an .obj as a model. Triangles and quads stay as they are; bigger polygons become triangles. All objects in the
    /// file go into one model. Without normals they're made smooth; without colours the polygons are white, ready for Auto shade.
    /// </summary>
    public static Prm Import(string objPath) => Import(objPath, out _);

    public static Prm Import(string objPath, out ImportReport report)
    {
        var pos = new List<Vector3>();
        var col = new List<Vector3?>();
        var uv = new List<Vector2>();
        var nrm = new List<Vector3>();
        var polys = new List<(int P, int T, int N)[]>();
        int split = 0, skipped = 0;
        bool anyColour = false;

        foreach (var raw in File.ReadLines(objPath))
        {
            var line = raw.Trim();
            // A comment may also end a line ("f 1 2 3 # tri"): it would read as a corner, or as a black vertex colour
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line[..hash].TrimEnd();
            if (line.Length < 2) continue;
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            switch (parts[0])
            {
                case "v":
                    pos.Add(new Vector3(N(parts, 1), N(parts, 2), N(parts, 3)));
                    if (parts.Length >= 7) { col.Add(new Vector3(N(parts, 4), N(parts, 5), N(parts, 6))); anyColour = true; }
                    else col.Add(null);
                    break;
                case "vt": uv.Add(new Vector2(N(parts, 1), N(parts, 2))); break;
                case "vn": nrm.Add(new Vector3(N(parts, 1), N(parts, 2), N(parts, 3))); break;
                case "f":
                    var corners = new List<(int, int, int)>();
                    for (int i = 1; i < parts.Length; i++)
                    {
                        var s = parts[i].Split('/');
                        int p = Index(s, 0, pos.Count), t = Index(s, 1, uv.Count), n = Index(s, 2, nrm.Count);
                        if (p < 0) { corners.Clear(); break; }
                        corners.Add((p, t, n));
                    }
                    if (corners.Count < 3) { skipped++; break; }
                    if (corners.Count <= 4) polys.Add(corners.ToArray());
                    else
                    {
                        split++;
                        for (int i = 1; i + 1 < corners.Count; i++) polys.Add(new[] { corners[0], corners[i], corners[i + 1] });
                    }
                    break;
            }
        }

        // Normals: from the file, or smooth from the polygons around each point
        bool hadNormals = nrm.Count > 0 && polys.All(p => p.All(c => c.N >= 0));
        Vector3[]? smooth = null;
        if (!hadNormals)
        {
            smooth = new Vector3[pos.Count];
            foreach (var p in polys)
            {
                var n = Vector3.Cross(pos[p[1].P] - pos[p[0].P], pos[p[2].P] - pos[p[0].P]);
                if (p.Length == 4) n += Vector3.Cross(pos[p[2].P] - pos[p[0].P], pos[p[3].P] - pos[p[0].P]);
                foreach (var c in p) smooth[c.P] += n;
            }
        }

        // One game vertex per point and normal
        var verts = new List<PrmVertex>();
        var key = new Dictionary<(int, int), short>();
        var faces = new List<PrmFace>();
        foreach (var p in polys)
        {
            var f = new PrmFace { Flags = p.Length == 4 ? FaceFlags.Quad : 0, TPage = 0 };
            var idx = new short[4];
            int n = p.Length;
            bool full = false;
            for (int j = 0; j < n; j++)
            {
                var c = p[n - 1 - j]; // the other way round: clockwise, as the game has them
                var k = (c.P, hadNormals ? c.N : -1);
                if (!key.TryGetValue(k, out var vi))
                {
                    if (verts.Count >= 32767) { full = true; break; }
                    var normal = hadNormals ? nrm[c.N] : smooth![c.P];
                    normal = normal.LengthSquared() > 1e-12f ? Vector3.Normalize(normal) : Vector3.UnitY;
                    vi = (short)verts.Count;
                    verts.Add(new PrmVertex { Pos = new Vector3(pos[c.P].X, -pos[c.P].Y, -pos[c.P].Z) / Scale, Normal = new Vector3(normal.X, -normal.Y, -normal.Z) });
                    key[k] = vi;
                }
                idx[j] = vi;
                if (c.T >= 0) { f.U[j] = uv[c.T].X; f.V[j] = 1 - uv[c.T].Y; }
                var cc = col[c.P];
                f.Colors[j] = cc is { } v
                    ? 0xFF000000u | (B(v.X) << 16) | (B(v.Y) << 8) | B(v.Z)
                    : 0xFFFFFFFFu;
            }
            if (full) { skipped++; continue; }
            f.V0 = idx[0]; f.V1 = idx[1]; f.V2 = idx[2]; f.V3 = n == 4 ? idx[3] : (short)0;
            faces.Add(f);
            if (faces.Count >= 32767) break;
        }
        var m = new Prm { Faces = faces.ToArray(), Verts = verts.ToArray() };
        m.ComputeBounds();
        report = new ImportReport(faces.Count, verts.Count, split, skipped, hadNormals, anyColour, uv.Count > 0);
        return m;
    }

    static uint B(float v) => (uint)Math.Clamp((int)MathF.Round(v * 255), 0, 255);

    static float N(string[] p, int i) =>
        i < p.Length && float.TryParse(p[i], NumberStyles.Float, Inv, out var v) ? v : 0;

    // .obj counts from 1; negative numbers count back from the end
    static int Index(string[] s, int i, int count)
    {
        if (i >= s.Length || s[i] == "" || !int.TryParse(s[i], NumberStyles.Integer, Inv, out var v)) return -1;
        int r = v > 0 ? v - 1 : count + v;
        return r >= 0 && r < count ? r : -1;
    }
}

/// <summary>Values that differ between two cars, and taking some of them over</summary>
public static class CarCompare
{
    public sealed record Row(string Block, string Key, string Mine, string Theirs)
    {
        /// <summary>What the row is about, to group and pick rows by</summary>
        public string Group => Groups.Of(Block, Key);
        public string Label => (Block == "" ? "" : Block + " ") + Key.Replace("#", " row ");
    }

    public static class Groups
    {
        public const string Files = "Files and names", Engine = "Engine and speed", Steering = "Steering", Mass = "Mass and balance",
            Wheels = "Wheels and grip", Suspension = "Springs", Ai = "AI", Look = "Looks and parts", Menu = "Menu and stats", Other = "Other";
        public static readonly string[] All = { Engine, Steering, Mass, Wheels, Suspension, Ai, Look, Menu, Files, Other };

        public static string Of(string block, string key)
        {
            if (block == "AI") return Ai;
            if (block.StartsWith("SPRING")) return Suspension;
            if (block.StartsWith("WHEEL"))
                return key is "Offset1" or "Offset2" or "IsPresent" or "ModelNum" or "Radius" or "SkidWidth" ? Look : Wheels;
            if (block is "AXLE 0" or "AXLE 1" or "AXLE 2" or "AXLE 3" or "PIN 0" or "PIN 1" or "PIN 2" or "PIN 3" or "SPINNER" or "AERIAL" or "CAMATTACHED") return Look;
            if (block == "BODY")
                return key is "ModelNum" or "Offset" ? Look : Mass;
            if (key == "Name" || key.StartsWith("MODEL") || key is "TPAGE" or "COLL" or "TCARBOX" or "TSHADOW" or "SFXENGINE" or "SFXHONK" or "SFXSERVO") return Files;
            if (key is "TopSpeed" or "EngineRate" or "DownForceMod" or "MaxRevs") return Engine;
            if (key is "Rating" or "Class" or "Obtain" or "TopEnd" or "Acc" or "Weight" or "Trans" or "Selectable" or "CPUSelectable" or "Statistics" or "BestTime") return Menu;
            if (key is "SteerRate" or "SteerMod") return Steering;
            if (key is "CoM" or "Weapon" or "WeaponOffset1" or "WeaponOffset2") return Mass;
            if (key is "EnvRGB" or "ShadowIndex" or "ShadowTable") return Look;
            return Other;
        }
    }

    public static List<Row> Diff(CarData mine, CarData theirs)
    {
        var a = mine.Emit();
        var b = theirs.Emit().GroupBy(e => e.Block + "|" + e.Key).ToDictionary(g => g.Key, g => g.First().Value);
        var r = new List<Row>();
        var seen = new HashSet<string>();
        foreach (var e in a)
        {
            var k = e.Block + "|" + e.Key;
            if (!seen.Add(k)) continue;
            b.TryGetValue(k, out var t);
            t ??= "";
            if (!Same(e.Value, t)) r.Add(new Row(e.Block, e.Key, e.Value, t));
        }
        foreach (var e in theirs.Emit())
        {
            var k = e.Block + "|" + e.Key;
            if (seen.Add(k) && e.Value != "") r.Add(new Row(e.Block, e.Key, "", e.Value));
        }
        return r;
    }

    // 1.0 and 1.000000 are the same value
    static bool Same(string x, string y)
    {
        if (x == y) return true;
        var xs = x.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var ys = y.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (xs.Length != ys.Length) return false;
        for (int i = 0; i < xs.Length; i++)
        {
            if (string.Equals(xs[i], ys[i], StringComparison.OrdinalIgnoreCase)) continue;
            if (!double.TryParse(xs[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var p) ||
                !double.TryParse(ys[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var q) || Math.Abs(p - q) > 1e-6) return false;
        }
        return true;
    }

    /// <summary>
    /// The car's values with the chosen rows' values taken from the other car. Goes through the car's file text (as
    /// Car.CurrentText gives it), so every value is read exactly as the game would read it. The car itself isn't changed.
    /// </summary>
    public static CarData Take(string currentText, IEnumerable<Row> rows, bool stock)
    {
        var before = CarData.From(new ParamFile(currentText), stock).Emit();
        var now = new List<ParamEntry>(before);
        // A value the other car doesn't have ("(none)") is nothing to take: it would blank this car's line
        var list = rows.Where(r => !string.IsNullOrWhiteSpace(r.Theirs)).ToList();
        var pick = list.GroupBy(r => r.Block + "|" + r.Key).ToDictionary(g => g.Key, g => g.First().Theirs);
        for (int i = 0; i < now.Count; i++)
            if (pick.TryGetValue(now[i].Block + "|" + now[i].Key, out var v)) now[i] = now[i] with { Value = v };
        var have = now.Select(e => e.Block + "|" + e.Key).ToHashSet();
        foreach (var r in list) if (have.Add(r.Block + "|" + r.Key)) now.Add(new ParamEntry(r.Block, r.Key, r.Theirs));
        var f = new ParamFile(currentText);
        f.Apply(before, now);
        return CarData.From(f, stock);
    }
}
