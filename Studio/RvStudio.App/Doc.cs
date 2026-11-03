using System.Numerics;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

public enum SelKind { None, Body, Wheel, Spring, Axle, Pin, Spinner, AerialBase, AerialTip, CoM, Weapon1, Weapon2, HoodCam, RearCam }

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
        _ => ""
    };
    public bool HasFourCorners => Kind is SelKind.Wheel or SelKind.Spring or SelKind.Axle or SelKind.Pin;
}

/// <summary>How a move on one wheel or suspension part carries to the others</summary>
public enum MoveMode { Single, Mirror, FrontBack, All }

/// <summary>The open car and everything that can be done to it. The window and the 3D view only look at this</summary>
public sealed class Doc
{
    public GameFolder? Game;
    public Car? Car;
    public CarData? D => Car?.Data;

    public Selection Sel { get; private set; }
    public MoveMode MoveMode = MoveMode.Mirror;
    public bool MoveLinked = true;  // spring and axle mount follow their wheel
    public float Snap = 0.5f;

    readonly Stack<(string Label, CarData Data)> undo = new(), redo = new();
    string? openLabel;
    DateTime openTime;

    public readonly Dictionary<string, Prm> Models = new(StringComparer.OrdinalIgnoreCase);
    public readonly HashSet<Prm> DirtyModels = new();
    public int ModelVersion;
    public int TextureVersion;
    public string? SkinOverride;  // preview of another skin

    public CarSim? Sim;
    public readonly CarDynamics Dyn = new();
    public Vector3 AerialBend, AerialBendVel;

    public event Action? Changed;       // values changed: redraw, refresh
    public event Action? CarOpened;
    public event Action? SelectionChanged;

    public static string WheelName(int i) => i switch { 0 => "front left", 1 => "front right", 2 => "back left", _ => "back right" };

    public void Open(GameFolder game, string folder)
    {
        Game = game;
        Car = new Car(game, folder);
        ReloadModels();
        undo.Clear(); redo.Clear();
        Sel = new Selection(SelKind.Body);
        StopSim();
        SkinOverride = null;
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
        SelectionChanged?.Invoke();
        Changed?.Invoke();
    }

    // --------------------------- Undo ---------------------------

    public bool CanUndo => undo.Count > 0;
    public bool CanRedo => redo.Count > 0;
    public bool Modified => (Car?.Modified ?? false) || DirtyModels.Count > 0;

    /// <summary>A change. Changes with the same label close in time undo together</summary>
    public void Edit(string label, Action change)
    {
        if (Car == null) return;
        var now = DateTime.UtcNow;
        if (label != openLabel || (now - openTime).TotalSeconds > 1.5)
        {
            undo.Push((label, Car.Data.Clone()));
            redo.Clear();
        }
        openLabel = label; openTime = now;
        change();
        Changed?.Invoke();
    }
    public void EndGesture() => openLabel = null;

    public void Undo()
    {
        if (Car == null || undo.Count == 0) return;
        var (l, d) = undo.Pop();
        redo.Push((l, Car.Data));
        Car.Data = d;
        openLabel = null;
        Changed?.Invoke();
        SelectionChanged?.Invoke();
    }
    public void Redo()
    {
        if (Car == null || redo.Count == 0) return;
        var (l, d) = redo.Pop();
        undo.Push((l, Car.Data));
        Car.Data = d;
        openLabel = null;
        Changed?.Invoke();
        SelectionChanged?.Invoke();
    }

    public void Touch() => Changed?.Invoke();

    public void Save()
    {
        if (Car == null) return;
        Car.Save();
        foreach (var m in DirtyModels)
            if (m.FilePath != null)
            {
                Files.Backup(m.FilePath);
                m.Save(m.FilePath);
            }
        DirtyModels.Clear();
        Changed?.Invoke();
    }

    // --------------------------- Moving ---------------------------

    public Vector3? Anchor(Selection s)
    {
        if (D is not { } d) return null;
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
        Edit("Move " + s.Label, () =>
        {
            switch (s.Kind)
            {
                case SelKind.Body: d.BodyOffset += delta; break;
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
                        a.Length = v.Length() / 2;
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

    // --------------------------- Drive ---------------------------

    public void StartSim(SimGround ground)
    {
        if (D == null) return;
        Sim = new CarSim(D.Clone()) { Ground = ground };
        Sim.Reset();
        AerialBend = AerialBendVel = Vector3.Zero;
    }
    public void StopSim()
    {
        Sim = null;
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
        s.Advance(dt);
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
        int n = 0;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var c = b.GetPixel(x, y);
                o[n++] = c.Red; o[n++] = c.Green; o[n++] = c.Blue;
                o[n++] = key && c.Red == 0 && c.Green == 0 && c.Blue == 0 ? (byte)0 : c.Alpha;
            }
        return o;
    }
}
