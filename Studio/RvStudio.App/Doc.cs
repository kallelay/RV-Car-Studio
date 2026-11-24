using System.Numerics;
using System.Runtime.InteropServices;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

/// <summary>How a part is drawn in the view (Easy's drawing of the car): nothing to do with the file</summary>
public enum PartView { Solid, Wire, Points, None, Half }

public enum SelKind { None, Body, Wheel, Spring, Axle, Pin, Spinner, AerialBase, AerialTip, CoM, Weapon1, Weapon2, HoodCam, RearCam, HullPart, HullSphere }

public readonly record struct Selection(SelKind Kind, int Index = 0)
{
    public string Label => Kind switch
    {
        SelKind.None => "Nothing selected",
        SelKind.Body => "Body",
        SelKind.Wheel => "Wheel: " + Doc.WheelName(Index),
        SelKind.Spring => "Spring: " + Doc.WheelName(Index),
        SelKind.Axle => "Axle: " + Doc.WheelName(Index),
        SelKind.Pin => "Pin: " + Doc.WheelName(Index),
        SelKind.Spinner => "Spinner",
        SelKind.AerialBase => "Aerial base",
        SelKind.AerialTip => "Aerial tip",
        SelKind.CoM => "Centre of mass",
        SelKind.Weapon1 => "Weapon: shockwave, firework",
        SelKind.Weapon2 => "Weapon: water bomb",
        SelKind.HoodCam => "Hood camera",
        SelKind.RearCam => "Rear camera",
        SelKind.HullPart => "Hull part " + (Index + 1),
        SelKind.HullSphere => "Hull sphere " + (Index + 1),
        _ => ""
    };
    public bool HasFourCorners => Kind is SelKind.Wheel or SelKind.Spring or SelKind.Axle or SelKind.Pin;
    public bool IsHull => Kind is SelKind.HullPart or SelKind.HullSphere;
}

/// <summary>How a move on one wheel or suspension part carries to the others</summary>
public enum MoveMode { Single, Mirror, FrontBack, All }

/// <summary>What the handles on the selected part do</summary>
public enum GizmoTool { Move, Rotate, Scale }

/// <summary>The open car and everything that can be done to it. The window and the 3D view only look at this</summary>
public sealed partial class Doc
{
    public GameFolder? Game;
    public Car? Car;
    public CarData? D => Car?.Data;

    public Selection Sel { get; private set; }
    public MoveMode MoveMode = MoveMode.Mirror;
    public bool MoveLinked = true;  // spring and axle mount follow their wheel
    public float Snap = 0.5f;
    public GizmoTool Tool = GizmoTool.Move;

    sealed record UndoState(string Label, CarData Data, List<(Prm Model, PrmVertex[] Verts, byte[]? Lods, short[] Faces, float[] Uvs)> Models, Hull? Hull, bool HullDirty);

    /// <summary>The collision hull, as it will be saved</summary>
    public Hull? Hull;
    public bool HullDirty;
    public bool HullFollowsBody = true;

    /// <summary>Editing a stock car: ask once whether a copy is wanted</summary>
    public bool StockAnswered;
    public event Action? StockTouched;
    public bool IsStockCar => Car != null && GameFolder.IsStock(Car.Folder);

    readonly HashSet<string> sharedOk = new(StringComparer.OrdinalIgnoreCase);

    // Copies of the car's current hull are small, so undo keeps them whole
    Hull? HullCopy() => Hull?.Clone();

    void NoteEdit()
    {
        if (IsStockCar && !StockAnswered)
        {
            StockAnswered = true;
            StockTouched?.Invoke();
        }
    }

    public void LoadHull()
    {
        Hull = null;
        HullDirty = false;
        if (Car == null) return;
        try
        {
            if (Car.Game.Find(D!.Coll) is { } f && new FileInfo(f).Length > 0) Hull = Hull.Load(f);
        }
        catch (Exception) { }
    }

    /// <summary>What's wrong with changing this file: who else needs it. Null when it's only this car's</summary>
    public string? SharedProblem(string? rel)
    {
        if (Car == null || string.IsNullOrWhiteSpace(rel) || rel.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase) || sharedOk.Contains(rel)) return null;
        var users = Car.OtherUsers(rel);
        if (users.Count > 0) return rel + " is also used by " + string.Join(", ", users.Take(6)) + (users.Count > 6 ? " and others" : "");
        if (Car.IsForeign(rel)) return rel + " is outside this car's folder";
        return null;
    }

    /// <summary>The files this edit would change that other cars may need</summary>
    public List<(string Rel, string Why)> SharedFiles(Selection s)
    {
        var r = new List<(string, string)>();
        if (D is not { } d) return r;
        void Look(string? rel)
        {
            if (SharedProblem(rel) is { } why && !r.Any(x => x.Item1.Equals(rel, StringComparison.OrdinalIgnoreCase))) r.Add((rel!, why));
        }
        if (s.IsHull || (s.Kind == SelKind.Body && HullFollowsBody && Hull != null)) Look(d.Coll);
        if (Tool != GizmoTool.Move && !s.IsHull)
            foreach (var (i, _, _) in s.HasFourCorners ? Corners2(s.Index) : new[] { (s.Index, false, false) })
            {
                int slot = SlotOf(s, i);
                if (slot is >= 0 and < 19) Look(d.Models[slot]);
            }
        return r;
    }

    public void AllowShared(string rel) => sharedOk.Add(rel);

    /// <summary>Copy the file into this car's folder and use the copy</summary>
    public void MakeOwn(string rel)
    {
        if (Car is not { } c) return;
        var own = c.CopyToOwn(rel);
        Edit("Copy " + Names.File(rel), () =>
        {
            for (int i = 0; i < 19; i++)
                if (c.Data.Models[i].Trim().Equals(rel.Trim(), StringComparison.OrdinalIgnoreCase)) c.Data.Models[i] = own;
            if (c.Data.Coll.Trim().Equals(rel.Trim(), StringComparison.OrdinalIgnoreCase)) c.Data.Coll = own;
        });
        EndGesture();
        if (Models.Remove(rel, out var m) && m != null)
        {
            m.FilePath = c.Game.Find(own);
            Models[own] = m;
        }
    }

    /// <summary>Carry on in a copy of the car, with everything changed so far. The original stays as it is on disk</summary>
    public void ForkCar(string folder, string name)
    {
        if (Car is not { } c) return;
        var old = c.Data.Clone();
        var dirty = DirtyModels.ToList();
        var hull = HullDirty ? Hull : null;
        var sel = Sel;
        var fork = c.Fork(folder, name);
        // Files changed in memory go to the copy's files, the original's aren't touched
        foreach (var m in dirty)
        {
            var rel = Models.FirstOrDefault(kv => kv.Value == m).Key;
            if (rel == null) continue;
            var target = rel;
            if (c.IsForeign(rel))
            {
                target = fork.CopyToOwn(rel);
                for (int i = 0; i < 19; i++) if (string.Equals(fork.Data.Models[i], rel, StringComparison.OrdinalIgnoreCase)) fork.Data.Models[i] = target;
            }
            else target = "cars\\" + folder + "\\" + rel.Replace('/', '\\')[("cars\\" + c.Folder + "\\").Length..];
            if (fork.Game.Find(target) is { } file) m.Save(file);
        }
        if (hull != null)
        {
            var rel = fork.Data.Coll;
            if (fork.IsForeign(rel)) { rel = "cars\\" + folder + "\\hull.hul"; fork.Data.Coll = rel; }
            hull.Save(fork.Game.WritePath(rel));
        }
        fork.Save();
        if (File.Exists(fork.ParamPath + ".bak")) File.Delete(fork.ParamPath + ".bak");
        Open(fork.Game, folder);
        Select(sel);
    }

    readonly Stack<UndoState> undo = new(), redo = new();
    string? openLabel;
    DateTime openTime;

    public readonly Dictionary<string, Prm> Models = new(StringComparer.OrdinalIgnoreCase);
    public readonly HashSet<Prm> DirtyModels = new();
    public int ModelVersion;
    public int TextureVersion;
    public string? SkinOverride;  // preview of another skin
    public byte[]? PaintPreview;  // the texture with the Paint tab's sliders on it
    public int PaintW, PaintH, PaintVersion;

    public CarSim? Sim;
    public readonly CarDynamics Dyn = new();
    public Vector3 AerialBend, AerialBendVel;

    public event Action? Changed;       // values changed: redraw, refresh
    public event Action? CarOpened;
    public event Action? SelectionChanged;
    public event Action? PartsChanged;  // a part came or went

    public readonly Dictionary<Selection, PartView> PartViews = new();
    public readonly HashSet<Selection> Hidden = new();  // only in the view, nothing to do with the file

    public static string WheelName(int i) => i switch { 0 => "front left", 1 => "front right", 2 => "back left", _ => "back right" };

    public void Open(GameFolder game, string folder)
    {
        Game = game;
        Car = new Car(game, folder);
        VertexMode = false;
        PickedVerts.Clear();
        ReloadModels();
        undo.Clear(); redo.Clear();
        Sel = new Selection(SelKind.Body);
        Hidden.Clear();
        PartViews.Clear();
        sharedOk.Clear();
        StockAnswered = false;
        LoadHull();
        StopSim();
        SkinOverride = null;
        PaintPreview = null;
        CarOpened?.Invoke();
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void ReloadModels()
    {
        Models.Clear();
        DirtyModels.Clear();
        ModelVersion++;
        TextureVersion++;
    }

    /// <summary>Read a model file again: it was changed on disk</summary>
    public void ReloadModel(string rel)
    {
        foreach (var k in Models.Keys.Where(k => k.Equals(rel, StringComparison.OrdinalIgnoreCase)).ToList())
        {
            if (Models[k] is { } old) DirtyModels.Remove(old);
            Models.Remove(k);
        }
        ModelVersion++;
        TextureVersion++;
        PartsChanged?.Invoke();
        Changed?.Invoke();
    }

    /// <summary>All the car's values at once, as one step undo takes back</summary>
    public void ReplaceData(string label, CarData data)
    {
        if (Car == null) return;
        Edit(label, () => { Car.Data = data; ModelVersion++; });
        EndGesture();
        PartsChanged?.Invoke();
        SelectionChanged?.Invoke();
    }

    public Prm? Model(int slot)
    {
        if (Car == null || slot < 0 || slot > 18) return null;
        var rel = Car.Data.Models[slot];
        if (string.IsNullOrWhiteSpace(rel) || rel.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) return null;
        if (Models.TryGetValue(rel, out var m)) return m;
        var f = Car.Game.Find(rel);
        if (f != null)
        {
            try { var p = Prm.Load(f); if (p.IsValid() && p.Faces.Length > 0) m = p; }
            catch (Exception) { m = null; }
        }
        Models[rel] = m!;
        return m;
    }

    public void Select(Selection s)
    {
        if (s == Sel) return;
        Sel = s;
        VertexFollowSelection(s);
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    // --------------------------- Undo ---------------------------

    /// <summary>The newest undo (or redo) step changed one of these models: what the UV editor's own undo may take back</summary>
    public bool UndoTopHas(IEnumerable<Prm> ms) => undo.Count > 0 && undo.Peek().Models.Any(x => ms.Contains(x.Model));
    public bool RedoTopHas(IEnumerable<Prm> ms) => redo.Count > 0 && redo.Peek().Models.Any(x => ms.Contains(x.Model));

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public bool Modified => (Car?.Modified ?? false) || DirtyModels.Count > 0 || HullDirty;

    /// <summary>A change. Changes with the same label close in time undo together</summary>
    public void Edit(string label, Action change)
    {
        if (Car == null) return;
        var now = DateTime.UtcNow;
        // A drag is one step however long it takes. Typing joins up while it goes on
        if (label != openLabel || (!InGesture && (now - openTime).TotalSeconds > 1.5))
        {
            undo.Push(new UndoState(label, Car.Data.Clone(), new(), HullCopy(), HullDirty));
            redo.Clear();
            NoteEdit();
        }
        openLabel = label; openTime = now;
        change();
        Changed?.Invoke();
    }
    public bool InGesture;
    public void EndGesture() { openLabel = null; InGesture = false; }

    /// <summary>How many steps undo holds: note it before a gesture to be able to take the gesture back</summary>
    public int UndoDepth => undo.Count;

    /// <summary>Take back what was done since undo held this many steps, leaving nothing to redo: Esc during a drag</summary>
    public void CancelTo(int depth)
    {
        EndGesture();
        while (Car != null && undo.Count > depth)
        {
            Step(undo, redo);
            redo.Pop();
        }
    }

    public void Undo()
    {
        if (Car == null || undo.Count == 0) return;
        Step(undo, redo);
    }

    void Step(Stack<UndoState> from, Stack<UndoState> to)
    {
        var s = from.Pop();
        to.Push(new UndoState(s.Label, Car!.Data, s.Models.Select(m => (m.Model, m.Model.Verts, m.Model.OtherLods, m.Model.FaceIndices(), UvKit.Copy(m.Model))).ToList(), HullCopy(), HullDirty));
        Car.Data = s.Data;
        Hull = s.Hull;
        HullDirty = s.HullDirty;
        foreach (var (m, v, l, faces, uvs) in s.Models)
        {
            m.Verts = v;
            m.OtherLods = l;
            m.SetFaceIndices(faces);
            UvKit.Paste(m, uvs);
            m.ComputeBounds();
        }
        if (s.Models.Count > 0) ModelVersion++;
        openLabel = null;
        PartsChanged?.Invoke();
        Changed?.Invoke();
        SelectionChanged?.Invoke();
    }
    public void Redo()
    {
        if (Car == null || redo.Count == 0) return;
        Step(redo, undo);
    }

    public void Touch() => Changed?.Invoke();

    public void Save()
    {
        if (Car == null) return;
        Car.Save();
        foreach (var m in DirtyModels)
        {
            var rel = Models.FirstOrDefault(kv => kv.Value == m).Key;
            if (rel == null) continue;
            var target = Car.Game.WritePath(rel);
            Files.Backup(target);
            m.Save(target);
            m.FilePath = target;
        }
        DirtyModels.Clear();
        if (HullDirty && Hull != null)
        {
            if (Car.Game.Find(Car.Data.Coll) == null)
            {
                Car.Data.Coll = "cars\\" + Car.Folder + "\\hull.hul";
                Car.Save();
            }
            var path = Car.Game.WritePath(Car.Data.Coll);
            Files.Backup(path);
            Hull.Save(path);
        }
        HullDirty = false;
        Changed?.Invoke();
    }

    // --------------------------- Collision hull ---------------------------

    void HullChange(Action<Hull> change)
    {
        if (Hull == null) return;
        change(Hull);
        HullDirty = true;
    }

    /// <summary>Hull from the body, kept in memory until Save</summary>
    public bool RebuildHull(HullMaker.Detail detail = HullMaker.Detail.Normal)
    {
        if (D is not { } d || Model(d.BodyModel) is not { } body) return false;
        var h = CarTools.MakeHull(body, d.BodyOffset, CarTools.HullFloor(d), detail);
        if (h == null) return false;
        Edit("Make hull", () =>
        {
            Hull = h;
            HullDirty = true;
        });
        EndGesture();
        PartsChanged?.Invoke();
        return true;
    }

    /// <summary>The spheres again from the body, the hull's parts as they are</summary>
    public bool RefillSpheres(HullMaker.Detail detail)
    {
        if (Hull == null || D is not { } d || Model(d.BodyModel) is not { } body) return false;
        if (HullMaker.Spheres(body, d.BodyOffset, CarTools.HullFloor(d), detail) is not { } made) return false;
        if (Sel.Kind == SelKind.HullSphere) Select(new Selection(SelKind.Body));
        Edit("Fill with spheres", () => HullChange(h => { h.Spheres.Clear(); h.Spheres.AddRange(made.Spheres); }));
        EndGesture();
        PartsChanged?.Invoke();
        return true;
    }

    HullMaker.Quality? quality;
    int qualityKey;

    /// <summary>How well the hull's spheres stand for the body</summary>
    public HullMaker.Quality? HullQuality()
    {
        if (Hull is not { Spheres.Count: > 0 } h || D is not { } d || Model(d.BodyModel) is not { } body) return null;
        // Pages ask at each change of anything: measured again only when the hull, the body or the wheels changed
        var key = new HashCode();
        foreach (var (p, r) in h.Spheres) { key.Add(p); key.Add(r); }
        key.Add(d.BodyOffset); key.Add(ModelVersion); key.Add(CarTools.HullFloor(d)); key.Add(body.Verts.Length);
        int k = key.ToHashCode();
        if (k != qualityKey) { quality = HullMaker.Measure(body, d.BodyOffset, CarTools.HullFloor(d), h.Spheres); qualityKey = k; }
        return quality;
    }

    public void AddSphere(Vector3 pos, float radius)
    {
        Edit("Add sphere", () => HullChange(h => h.Spheres.Add((pos, radius))));
        EndGesture();
        PartsChanged?.Invoke();
        Select(new Selection(SelKind.HullSphere, Hull!.Spheres.Count - 1));
    }

    public void RemoveHullItem(Selection s)
    {
        Edit("Remove " + s.Label, () => HullChange(h =>
        {
            if (s.Kind == SelKind.HullSphere && s.Index < h.Spheres.Count) h.Spheres.RemoveAt(s.Index);
            if (s.Kind == SelKind.HullPart && s.Index < h.Convex.Count) h.Convex.RemoveAt(s.Index);
        }));
        EndGesture();
        Sel = new Selection(SelKind.None);
        PartsChanged?.Invoke();
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    public void SetSphere(int i, Vector3 pos, float r)
    {
        Edit("Sphere " + (i + 1), () => HullChange(h => { if (i < h.Spheres.Count) h.Spheres[i] = (pos, r); }));
    }

    // The sphere mirroring this one, if there is one
    int MirrorSphere(Hull h, int i)
    {
        var want = h.Spheres[i].Pos * new Vector3(-1, 1, 1);
        for (int k = 0; k < h.Spheres.Count; k++)
            if (k != i && (h.Spheres[k].Pos - want).Length() < 2.5f) return k;
        return -1;
    }

    // --------------------------- Moving ---------------------------

    public Vector3? Anchor(Selection s)
    {
        if (D is not { } d) return null;
        if (VertexMode && s == VertexPart) return VertexAnchor();   // editing vertices: the handles sit on the picked ones
        return s.Kind switch
        {
            SelKind.Body => d.BodyOffset,
            SelKind.Wheel => d.Wheels[s.Index].Offset1,
            SelKind.Spring => d.Springs[s.Index].Offset,
            SelKind.Axle => d.Axles[s.Index].Offset,
            SelKind.Pin => d.Pins[s.Index].Model >= 0 ? d.Springs[s.Index].Offset : null,
            SelKind.Spinner => d.Spinner.Offset,
            SelKind.AerialBase => d.Aerial.Offset,
            SelKind.AerialTip => CarPose.AerialTip(d),
            SelKind.CoM => d.CoM,
            SelKind.Weapon1 => d.Weapon,
            SelKind.Weapon2 => d.Weapon2,
            SelKind.HoodCam => d.Camera?.HoodOffset,
            SelKind.RearCam => d.Camera?.RearOffset,
            SelKind.HullPart => Hull != null && s.Index < Hull.Convex.Count ? Hull.Convex[s.Index].Offset : null,
            SelKind.HullSphere => Hull != null && s.Index < Hull.Spheres.Count ? Hull.Spheres[s.Index].Pos : null,
            _ => null
        };
    }

    static Vector3 MirrorX(Vector3 v) => new(-v.X, v.Y, v.Z);
    static Vector3 MirrorZ(Vector3 v) => new(v.X, v.Y, -v.Z);

    /// <summary>The corners a move reaches, and how the move looks at each</summary>
    IEnumerable<(int Index, Func<Vector3, Vector3> Map)> Corners(int i)
    {
        yield return (i, v => v);
        switch (MoveMode)
        {
            case MoveMode.Mirror:
                yield return (i ^ 1, MirrorX);
                break;
            case MoveMode.FrontBack:
                yield return (i ^ 2, MirrorZ);
                break;
            case MoveMode.All:
                yield return (i ^ 1, MirrorX);
                yield return (i ^ 2, MirrorZ);
                yield return (i ^ 3, v => MirrorX(MirrorZ(v)));
                break;
        }
    }

    public void Move(Selection s, Vector3 delta)
    {
        if (D is not { } d) return;
        if (EditingVertices(s)) { MoveVertices(delta); return; }
        Edit("Move " + s.Label, () =>
        {
            switch (s.Kind)
            {
                case SelKind.Body:
                    d.BodyOffset += delta;
                    if (HullFollowsBody) HullChange(h => h.Apply(Matrix4x4.CreateTranslation(delta)));
                    break;
                case SelKind.HullPart:
                    HullChange(h => h.Convex[s.Index].Apply(Matrix4x4.CreateTranslation(delta)));
                    break;
                case SelKind.HullSphere:
                    HullChange(h =>
                    {
                        int other = MoveMode is MoveMode.Mirror or MoveMode.All ? MirrorSphere(h, s.Index) : -1;
                        h.Spheres[s.Index] = (h.Spheres[s.Index].Pos + delta, h.Spheres[s.Index].Radius);
                        if (other >= 0) h.Spheres[other] = (h.Spheres[other].Pos + delta * new Vector3(-1, 1, 1), h.Spheres[other].Radius);
                    });
                    break;
                case SelKind.Wheel:
                    foreach (var (i, map) in Corners(s.Index))
                    {
                        var m = map(delta);
                        d.Wheels[i].Offset1 += m;
                        if (MoveLinked)
                        {
                            if (d.Springs[i].Model >= 0) d.Springs[i].Offset += m;
                            if (d.Axles[i].Model >= 0) d.Axles[i].Offset += m;
                        }
                    }
                    break;
                case SelKind.Spring:
                case SelKind.Pin:
                    foreach (var (i, map) in Corners(s.Index)) d.Springs[i].Offset += map(delta);
                    break;
                case SelKind.Axle:
                    foreach (var (i, map) in Corners(s.Index)) d.Axles[i].Offset += map(delta);
                    break;
                case SelKind.Spinner: d.Spinner.Offset += delta; break;
                case SelKind.AerialBase: d.Aerial.Offset += delta; break;
                case SelKind.AerialTip:
                    var a = d.Aerial;
                    var v = CarPose.AerialTip(d) + delta - a.Offset;
                    if (v.LengthSquared() > 1)
                    {
                        a.Direction = Vector3.Normalize(v);
                        a.Length = v.Length();
                    }
                    break;
                case SelKind.CoM: d.CoM += delta; break;
                case SelKind.Weapon1: d.Weapon += delta; break;
                case SelKind.Weapon2: d.Weapon2 += delta; d.HasWeapon2 = true; break;
                case SelKind.HoodCam: d.Camera ??= new CameraData(); d.Camera.HoodOffset += delta; break;
                case SelKind.RearCam: d.Camera ??= new CameraData(); d.Camera.RearOffset += delta; break;
            }
        });
    }

    // --------------------------- Adding and removing parts ---------------------------

    public bool HasModel(Selection s) => D is { } d && s.Kind switch
    {
        SelKind.Body => d.BodyModel >= 0,
        SelKind.Wheel => d.Wheels[s.Index].Present && d.Wheels[s.Index].Model >= 0,
        SelKind.Spring => d.Springs[s.Index].Model >= 0,
        SelKind.Axle => d.Axles[s.Index].Model >= 0,
        SelKind.Pin => d.Pins[s.Index].Model >= 0,
        SelKind.Spinner => d.Spinner.Model >= 0,
        SelKind.AerialBase => d.Aerial.SecModel >= 0,
        SelKind.AerialTip => d.Aerial.TopModel >= 0,
        _ => false
    };

    public void ToggleHidden(Selection s)
    {
        if (!Hidden.Remove(s)) Hidden.Add(s);
        Changed?.Invoke();
    }

    IEnumerable<int> UsedSlots(CarData d)
    {
        yield return d.BodyModel;
        foreach (var w in d.Wheels) yield return w.Model;
        foreach (var w in d.Springs) yield return w.Model;
        foreach (var w in d.Axles) yield return w.Model;
        foreach (var w in d.Pins) yield return w.Model;
        yield return d.Spinner.Model;
        yield return d.Aerial.SecModel;
        yield return d.Aerial.TopModel;
    }

    /// <summary>The slot holding this file, or a free one</summary>
    int SlotFor(CarData d, string rel)
    {
        for (int i = 0; i < 19; i++) if (d.Models[i].Equals(rel, StringComparison.OrdinalIgnoreCase)) return i;
        var used = UsedSlots(d).ToHashSet();
        for (int i = 0; i < 19; i++)
            if (!used.Contains(i) && d.Models[i].Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase)) return i;
        for (int i = 0; i < 19; i++) if (!used.Contains(i)) return i;
        throw new InvalidOperationException("All 19 model slots are in use");
    }

    void FreeUnusedSlots(CarData d)
    {
        var used = UsedSlots(d).ToHashSet();
        for (int i = 0; i < 19; i++) if (!used.Contains(i)) d.Models[i] = "NONE";
    }

    /// <summary>The file as the game wants it: from the Re-Volt folder. A file from elsewhere is copied into the car's folder</summary>
    public string BringIn(string file, string? asName = null)
    {
        if (asName == null && Car!.Game.RelOf(file) is { } rel) return rel;
        Directory.CreateDirectory(Car!.Dir);
        // The file picked, always: a different file of the same name already there gets a new name (name_2.prm)
        var name = asName ?? Names.File(file);
        var dest = Path.Combine(Car.Dir, name);
        for (int k = 2; File.Exists(dest) && !File.ReadAllBytes(dest).AsSpan().SequenceEqual(File.ReadAllBytes(file)); k++)
            dest = Path.Combine(Car.Dir, Names.Stem(name) + "_" + k + Path.GetExtension(name));
        if (!File.Exists(dest)) File.Copy(file, dest);
        return "cars\\" + Car.Folder + "\\" + Names.File(dest);
    }

    /// <summary>Give a part a model. With nothing there yet it gets sensible values to start from. Follows the corner mode</summary>
    public void SetPartModel(Selection s, string rel)
    {
        if (D is not { } d) return;
        Edit("Model of " + s.Label, () =>
        {
            int slot = SlotFor(d, rel);
            d.Models[slot] = rel;
            Models.Remove(rel);
            var m = Model(slot);
            foreach (var (i, _, _) in s.HasFourCorners ? Corners2(s.Index) : new[] { (s.Index, false, false) })
                switch (s.Kind)
                {
                    case SelKind.Body: d.BodyModel = slot; break;
                    case SelKind.Wheel:
                        d.Wheels[i].Model = slot;
                        d.Wheels[i].Present = true;
                        if (d.Wheels[i].Radius <= 0 && m != null) d.Wheels[i].Radius = Math.Round((m.Max.Y - m.Min.Y) / 2, 2);
                        break;
                    case SelKind.Spring:
                        if (d.Springs[i].Model < 0 && d.Springs[i].Offset == Vector3.Zero)
                            d.Springs[i].Offset = d.Wheels[i].Offset1 - new Vector3(d.Wheels[i].Offset1.X * 0.2f, 22, 0);
                        d.Springs[i].Model = slot;
                        if (m != null) d.Springs[i].Length = Math.Round(m.Max.Y - m.Min.Y, 2);
                        break;
                    case SelKind.Axle:
                        if (d.Axles[i].Model < 0 && d.Axles[i].Offset == Vector3.Zero)
                            d.Axles[i].Offset = d.Wheels[i].Offset1 * new Vector3(0.3f, 1, 1);
                        d.Axles[i].Model = slot;
                        if (m != null) d.Axles[i].Length = Math.Round(m.Max.Z - m.Min.Z, 2);
                        break;
                    case SelKind.Pin:
                        d.Pins[i].Model = slot;
                        if (d.Pins[i].Length == 0) d.Pins[i].Length = -1;
                        break;
                    case SelKind.Spinner:
                        d.Spinner.Model = slot;
                        if (d.Spinner.Axis == Vector3.Zero) d.Spinner.Axis = new Vector3(0, -1, 0);
                        break;
                    case SelKind.AerialBase: d.Aerial.SecModel = slot; break;
                    case SelKind.AerialTip: d.Aerial.TopModel = slot; break;
                }
            FreeUnusedSlots(d);
        });
        EndGesture();
        PartsChanged?.Invoke();
    }

    /// <summary>The stock aerial, on the back of the car</summary>
    public void AddAerial()
    {
        if (D is not { } d) return;
        Edit("Add aerial", () =>
        {
            int sec = SlotFor(d, "cars\\misc\\Aerial.m");
            d.Models[sec] = "cars\\misc\\Aerial.m";
            d.Aerial.SecModel = sec;
            int top = SlotFor(d, "cars\\misc\\AerialT.m");
            d.Models[top] = "cars\\misc\\AerialT.m";
            d.Aerial.TopModel = top;
            if (d.Aerial.Length <= 0) d.Aerial.Length = 17;
            if (d.Aerial.Direction == Vector3.Zero) d.Aerial.Direction = new Vector3(0, -1, 0);
            if (d.Aerial.Stiffness <= 0) d.Aerial.Stiffness = 2000;
            if (d.Aerial.Damping <= 0) d.Aerial.Damping = 5.5;
            if (d.Aerial.Offset == Vector3.Zero && Model(d.BodyModel) is { } b)
                d.Aerial.Offset = new Vector3(b.Max.X * 0.7f, b.Min.Y * 0.6f, b.Min.Z * 0.75f) + d.BodyOffset;
        });
        EndGesture();
        PartsChanged?.Invoke();
    }

    /// <summary>Take a part off the car. Follows the corner mode. Undo brings it back</summary>
    public void RemovePart(Selection s)
    {
        if (D is not { } d) return;
        Edit("Remove " + s.Label, () =>
        {
            foreach (var (i, _, _) in s.HasFourCorners ? Corners2(s.Index) : new[] { (s.Index, false, false) })
                switch (s.Kind)
                {
                    case SelKind.Body: d.BodyModel = -1; break;
                    case SelKind.Wheel: d.Wheels[i].Model = -1; d.Wheels[i].Present = false; break;
                    case SelKind.Spring: d.Springs[i].Model = -1; break;
                    case SelKind.Axle: d.Axles[i].Model = -1; break;
                    case SelKind.Pin: d.Pins[i].Model = -1; break;
                    case SelKind.Spinner: d.Spinner.Model = -1; break;
                    case SelKind.AerialBase:
                    case SelKind.AerialTip:
                        d.Aerial.SecModel = -1;
                        d.Aerial.TopModel = -1;
                        break;
                }
            FreeUnusedSlots(d);
        });
        EndGesture();
        Sel = new Selection(SelKind.None);
        PartsChanged?.Invoke();
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    // --------------------------- Rotating and scaling ---------------------------

    /// <summary>Can this be rotated or scaled? Points like the centre of mass only move</summary>
    public bool CanTransform(Selection s) => EditingVertices(s) || s.Kind is SelKind.Body or SelKind.Wheel or SelKind.Spring or SelKind.Axle
        or SelKind.Pin or SelKind.Spinner or SelKind.AerialBase or SelKind.AerialTip or SelKind.HullPart
        || (s.Kind == SelKind.HullSphere && Tool == GizmoTool.Scale);

    /// <summary>The models a part uses (the four wheels may share one), each once.</summary>
    public List<Prm> ModelsOf(Selection s)
    {
        var r = new List<Prm>();
        if (D == null) return r;
        int n = s.HasFourCorners ? 4 : 1;
        for (int i = 0; i < n; i++)
        {
            int k = s.HasFourCorners ? i : s.Index;
            int slot = s.Kind switch { SelKind.AerialBase => D.Aerial.SecModel, SelKind.AerialTip => D.Aerial.TopModel, _ => SlotOf(s, k) };
            if (Model(slot) is { } m && !r.Contains(m)) r.Add(m);
        }
        return r;
    }

    /// <summary>The name of the file a model came from, as the car's parameters give it.</summary>
    public string? ModelName(Prm m) => Models.FirstOrDefault(kv => kv.Value == m).Key;

    int SlotOf(Selection s, int i) => s.Kind switch
    {
        SelKind.Body => D!.BodyModel,
        SelKind.Wheel => D!.Wheels[i].Model,
        SelKind.Spring => D!.Springs[i].Model,
        SelKind.Axle => D!.Axles[i].Model,
        SelKind.Pin => D!.Pins[i].Model,
        SelKind.Spinner => D!.Spinner.Model,
        _ => -1
    };

    // A mirrored corner turns the other way around every axis but the mirror's own
    static Vector3 MirrorAxis(Vector3 n, bool x, bool z)
    {
        if (x) n = new Vector3(n.X, -n.Y, -n.Z);
        if (z) n = new Vector3(-n.X, -n.Y, n.Z);
        return n;
    }

    IEnumerable<(int Index, bool X, bool Z)> Corners2(int i)
    {
        yield return (i, false, false);
        if (MoveMode is MoveMode.Mirror or MoveMode.All) yield return (i ^ 1, true, false);
        if (MoveMode is MoveMode.FrontBack or MoveMode.All) yield return (i ^ 2, false, true);
        if (MoveMode == MoveMode.All) yield return (i ^ 3, true, true);
    }

    /// <summary>A change to whole models that undo can take back</summary>
    public void EditModels(string label, IEnumerable<Prm> models, Action change)
    {
        Edit(label, () =>
        {
            foreach (var m in models)
            {
                Remember(label, m);
                DirtyModels.Add(m);
            }
            change();
            ModelVersion++;
        });
        EndGesture();
    }

    // Vertices are shared by the parts that use a model: keep what undo needs, once per gesture
    void Remember(string label, Prm m)
    {
        var top = undo.Count > 0 ? undo.Peek() : null;
        if (top == null || top.Label != label) return;
        if (!top.Models.Any(x => x.Model == m)) top.Models.Add((m, m.Verts, m.OtherLods, m.FaceIndices(), UvKit.Copy(m)));
    }

    /// <summary>Turn the model of what's selected around its origin. Stays in memory until Save</summary>
    public void Rotate(Selection s, Vector3 axis, float angle)
    {
        if (EditingVertices(s)) { RotateVertices(axis, angle); return; }
        if (D is not { } d || !CanTransform(s)) return;
        var label = "Rotate " + s.Label;
        Edit(label, () =>
        {
            if (s.Kind is SelKind.AerialBase or SelKind.AerialTip)
            {
                var v = d.Aerial.Direction.LengthSquared() < 1e-6f ? -Vector3.UnitY : d.Aerial.Direction;
                d.Aerial.Direction = Vector3.Normalize(Vector3.Transform(v, Matrix4x4.CreateFromAxisAngle(axis, angle)));
                return;
            }
            if (s.Kind == SelKind.HullPart)
            {
                HullChange(h =>
                {
                    var c = h.Convex[s.Index].Offset;
                    h.Convex[s.Index].Apply(Matrix4x4.CreateTranslation(-c) * Matrix4x4.CreateFromAxisAngle(axis, angle) * Matrix4x4.CreateTranslation(c));
                });
                return;
            }
            if (s.Kind == SelKind.Body && HullFollowsBody)
                HullChange(h => h.Apply(Matrix4x4.CreateTranslation(-d.BodyOffset) * Matrix4x4.CreateFromAxisAngle(axis, angle) * Matrix4x4.CreateTranslation(d.BodyOffset)));
            var done = new HashSet<Prm>();
            foreach (var (i, mx, mz) in s.HasFourCorners ? Corners2(s.Index) : new[] { (s.Index, false, false) })
                if (Model(SlotOf(s, i)) is { } m && done.Add(m))
                {
                    Remember(label, m);
                    var r = Matrix4x4.CreateFromAxisAngle(MirrorAxis(axis, mx, mz), angle);
                    void Turn(Prm x)
                    {
                        var nv = (PrmVertex[])x.Verts.Clone();
                        for (int k = 0; k < nv.Length; k++)
                        {
                            nv[k].Pos = Vector3.Transform(nv[k].Pos, r);
                            nv[k].Normal = Vector3.TransformNormal(nv[k].Normal, r);
                        }
                        x.Verts = nv;
                    }
                    Turn(m);
                    m.ForOtherLods(Turn);
                    m.ComputeBounds();
                    DirtyModels.Add(m);
                }
            if (s.Kind == SelKind.Spinner && d.Spinner.Axis.LengthSquared() > 1e-6f)
                d.Spinner.Axis = Vector3.Transform(d.Spinner.Axis, Matrix4x4.CreateFromAxisAngle(axis, angle));
            ModelVersion++;
        });
    }

    /// <summary>Resize the model of what's selected from its origin, and the values that go with its size</summary>
    public void Scale(Selection s, Vector3 f)
    {
        if (EditingVertices(s)) { if (f.X > 0 && f.Y > 0 && f.Z > 0) ScaleVertices(f); return; }
        if (D is not { } d || !CanTransform(s) || f.X <= 0 || f.Y <= 0 || f.Z <= 0) return;
        var label = "Scale " + s.Label;
        Edit(label, () =>
        {
            if (s.Kind is SelKind.AerialBase or SelKind.AerialTip)
            {
                d.Aerial.Length *= f.Y;
                return;
            }
            if (s.Kind == SelKind.HullPart)
            {
                HullChange(h =>
                {
                    var c = h.Convex[s.Index].Offset;
                    h.Convex[s.Index].Apply(Matrix4x4.CreateTranslation(-c) * Matrix4x4.CreateScale(f) * Matrix4x4.CreateTranslation(c));
                });
                return;
            }
            if (s.Kind == SelKind.HullSphere)
            {
                HullChange(h =>
                {
                    float k = (f.X + f.Y + f.Z - 2);
                    int other = MoveMode is MoveMode.Mirror or MoveMode.All ? MirrorSphere(h, s.Index) : -1;
                    h.Spheres[s.Index] = (h.Spheres[s.Index].Pos, h.Spheres[s.Index].Radius * k);
                    if (other >= 0) h.Spheres[other] = (h.Spheres[other].Pos, h.Spheres[other].Radius * k);
                });
                return;
            }
            if (s.Kind == SelKind.Body && HullFollowsBody)
                HullChange(h => h.Apply(Matrix4x4.CreateTranslation(-d.BodyOffset) * Matrix4x4.CreateScale(f) * Matrix4x4.CreateTranslation(d.BodyOffset)));
            var done = new HashSet<Prm>();
            foreach (var (i, _, _) in s.HasFourCorners ? Corners2(s.Index) : new[] { (s.Index, false, false) })
            {
                if (Model(SlotOf(s, i)) is { } m && done.Add(m))
                {
                    Remember(label, m);
                    void Resize(Prm x)
                    {
                        var nv = (PrmVertex[])x.Verts.Clone();
                        for (int k = 0; k < nv.Length; k++)
                        {
                            nv[k].Pos *= f;
                            var n = nv[k].Normal / f;
                            nv[k].Normal = n.LengthSquared() > 1e-10f ? Vector3.Normalize(n) : nv[k].Normal;
                        }
                        x.Verts = nv;
                    }
                    Resize(m);
                    m.ForOtherLods(Resize);
                    m.ComputeBounds();
                    DirtyModels.Add(m);
                }
                switch (s.Kind)
                {
                    case SelKind.Wheel:
                        d.Wheels[i].Radius *= (f.Y + f.Z) / 2;
                        d.Wheels[i].SkidWidth *= f.X;
                        break;
                    case SelKind.Spring: d.Springs[i].Length *= f.Y; break;
                    case SelKind.Axle: d.Axles[i].Length *= f.Z; break;
                }
            }
            ModelVersion++;
        });
    }

    // --------------------------- Drive ---------------------------

    public void StartSim(SimGround ground)
    {
        if (D == null) return;
        Sim = new CarSim(D.Clone(), Hull?.Clone()) { Ground = ground };
        Sim.Reset();
        AerialBend = AerialBendVel = Vector3.Zero;
    }
    // ---- A race: the player's car is the Sim above, everyone else's parts come from here ----

    public Race? Race;
    readonly Dictionary<(string Folder, int Slot), Prm?> raceModels = new();

    public void StartRace(int track, RaceGround ground, int opponents, int laps, float skill, bool sameCar, bool aiDrives)
    {
        if (D == null || Car == null) return;
        var race = new Race(RaceTrack.Make(track, ground), laps) { AiDrivesPlayer = aiDrives };
        var clean = D.Clone();
        race.Add(D.Name, Car.Folder, clean, Hull?.Clone(), true);
        raceModels.Clear();
        if (sameCar)
        {
            for (int i = 0; i < opponents; i++)
                race.Add(D.Name + " " + (i + 2), Car.Folder, D.Clone(), Hull?.Clone(), false, skill * (1 - i * 0.012f));
        }
        else if (Game != null)
        {
            var picks = RvStudio.Core.Race.PickOpponents(Game, D, Car.Folder, opponents);
            for (int i = 0; i < picks.Count; i++)
            {
                var other = new Car(Game, picks[i]);
                Hull? hull = null;
                try { if (Game.Find(other.Data.Coll) is { } file) hull = Hull.Load(file); }
                catch (Exception) { }
                race.Add(other.Data.Name, picks[i], other.Data, hull, false, skill * (1 - i * 0.012f));
            }
        }
        race.Grid();
        Race = race;
        Sim = race.Player.Sim;
        AerialBend = AerialBendVel = Vector3.Zero;
    }

    /// <summary>A model of a car in the race, without touching the open one's models</summary>
    public Prm? RaceModel(RaceCar car, int slot)
    {
        if (Car != null && car.Folder.Equals(Car.Folder, StringComparison.OrdinalIgnoreCase)) return Model(slot);
        if (Game == null) return null;
        if (!raceModels.TryGetValue((car.Folder, slot), out var m))
        {
            try { m = CarTools.LoadModel(new Car(Game, car.Folder), slot); }
            catch (Exception) { m = null; }
            raceModels[(car.Folder, slot)] = m;
        }
        return m;
    }

    public void StopSim()
    {
        Sim = null;
        Race = null;
        raceModels.Clear();
        Array.Clear(Dyn.Compression);
        Array.Clear(Dyn.Spin);
        Array.Clear(Dyn.Steer);
        Dyn.Root = Matrix4x4.Identity;
        Dyn.AerialBend = Vector3.Zero;
    }

    Vector3 lastVel;
    public void TickSim(float dt)
    {
        if (Sim is not { } s) return;
        if (Race is { } race)
        {
            race.Advance(dt);
            foreach (var car in race.Cars)
            {
                if (car.IsPlayer) continue;
                Array.Copy(car.Sim.Compression, car.Dyn.Compression, 4);
                for (int i = 0; i < 4; i++) { car.Dyn.Spin[i] = car.Sim.WheelSpin[i]; car.Dyn.Steer[i] = car.Sim.SteerAngle[i]; }
                car.Dyn.Root = car.Sim.Matrix;
            }
        }
        else s.Advance(dt);
        Array.Copy(s.Compression, Dyn.Compression, 4);
        for (int i = 0; i < 4; i++) { Dyn.Spin[i] = s.WheelSpin[i]; Dyn.Steer[i] = s.SteerAngle[i]; }
        Dyn.SpinnerAngle += (float)s.Car.Spinner.AngVel * dt;
        Dyn.Root = s.Matrix;

        // Aerial: a tip on a spring, pushed around by the car's acceleration
        var acc = (s.Vel - lastVel) / Math.Max(dt, 0.001f);
        lastVel = s.Vel;
        var localAcc = Vector3.Transform(acc, Quaternion.Inverse(s.Rot));
        float k = (float)Math.Max(s.Car.Aerial.Stiffness, 100) / 8f, c = (float)Math.Max(s.Car.Aerial.Damping, 1) * 2f;
        var tipAcc = -localAcc * 0.03f - AerialBend * k - AerialBendVel * c;
        AerialBendVel += tipAcc * dt;
        AerialBend += AerialBendVel * dt;
        var limit = (float)s.Car.Aerial.Length * 1.2f;
        if (AerialBend.Length() > limit && limit > 0) AerialBend = Vector3.Normalize(AerialBend) * limit;
        Dyn.AerialBend = AerialBend;
    }

    // --------------------------- Texture ---------------------------

    public static byte[]? LoadTexture(string? path, out int w, out int h)
    {
        w = h = 0;
        using var b = Images.Load(path);
        if (b == null) return null;
        return ToRgba(b, out w, out h);
    }

    /// <summary>RGBA bytes. Without an alpha channel black is what the game shows as see-through</summary>
    public static byte[] ToRgba(SKBitmap b, out int w, out int h)
    {
        w = b.Width; h = b.Height;
        bool key = b.AlphaType == SKAlphaType.Opaque || b.ColorType == SKColorType.Rgb888x || b.ColorType == SKColorType.Rgb565;
        var o = new byte[w * h * 4];
        // On this thread alone: the window's own thread comes here, and Parallel.For would make it wait for the shared
        // threads, behind whatever they're busy with
        var to = MemoryMarshal.Cast<byte, uint>(o.AsSpan());
        var px = MemoryMarshal.Cast<SKColor, uint>(b.Pixels.AsSpan()); // ARGB
        for (int i = 0; i < to.Length; i++)
        {
            uint c = px[i];
            to[i] = key && (c & 0xFFFFFF) == 0 ? 0 : (c & 0xFF00FF00) | (c >> 16 & 255) | (c & 255) << 16;
        }
        return o;
    }
}
