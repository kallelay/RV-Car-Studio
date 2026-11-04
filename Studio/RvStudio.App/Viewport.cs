using System.Numerics;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using RvStudio.Core;
using Silk.NET.OpenGL;
using SkiaSharp;
using Vec2 = System.Numerics.Vector2;

namespace RvStudio.App;

public enum ViewMode { Perspective, Top, Front, Side }
public enum ShadeMode { Textured, VertexColours, Clay }

[StructLayout(LayoutKind.Sequential)]
struct MeshVertex
{
    public Vector3 Pos, Normal;
    public Vec2 Uv;
    public uint Color; // RGBA bytes
}

[StructLayout(LayoutKind.Sequential)]
struct LineVertex
{
    public Vector3 Pos;
    public uint Color;
}

// One model on the graphics card, grouped by what changes the GL state
sealed class GpuMesh
{
    public uint Vao, Vbo, LineVao, LineVbo;
    public int Version = -1;
    public int LineCount;
    public readonly List<(int Pass, bool Textured, bool Env, int First, int Count)> Batches = new();
}

sealed class GpuTex { public uint Id; public int Version = -1; }

/// <summary>What one frame draws. Made on the UI thread, read on the render thread, never changed in between</summary>
sealed class Frame
{
    public readonly List<(Prm Mesh, Matrix4x4 World, int Tex, bool Env, bool Selected)> Items = new();
    public readonly List<LineVertex> Lines = new();         // with depth
    public readonly List<LineVertex> Overlay = new();       // always on top
    public Vector3 Target, Eye;
    public float Distance, Yaw, Pitch;
    public bool Ortho;
    public ShadeMode Shade;
    public bool Env, Wire, Lit, Floor;
    public float FloorY;
    public Vector3 EnvColor;
    public (Vector3 A, Vector3 B, Vector3 C, Vector3 D, Vector4 Uv)? Shadow;
    public string? CarTexture, FxTexture, EnvTexture, ShadowTexture;
    public int TexVersion, ModelVersion;
    public byte[]? Preview;
    public int PreviewW, PreviewH, PreviewVersion;
    public System.Numerics.Vector3 Background = new(0.17f, 0.19f, 0.22f);
}

/// <summary>The 3D view, its camera and the mouse tools on it. Works in the game's coordinates: Y down, Z forward</summary>
public sealed class Viewport : OpenGlControlBase, Avalonia.Rendering.ICustomHitTest
{
    readonly Doc doc;
    GL? gl;
    uint meshProg, lineProg, floorVao, floorVbo, overlayVao, overlayVbo;
    int uMvp, uModel, uView, uTint, uMode, uEnvColor, lMvp, lOverride;
    bool es;
    readonly Dictionary<Prm, GpuMesh> meshes = new();
    readonly Dictionary<string, GpuTex> textures = new(StringComparer.OrdinalIgnoreCase);
    uint whiteTex;

    volatile Frame? frame;
    TaskCompletionSource<SKBitmap>? captureDone;
    int captureSize;
    bool captureClean;

    // Camera
    public Vector3 Target = new(0, 0, 0);
    public float Yaw = 0.7f, Pitch = 0.42f, Distance = 260;
    public ViewMode View = ViewMode.Perspective;

    // What to show
    public ShadeMode Shade = ShadeMode.Textured;
    public bool ShowEnv = true, ShowWire, ShowHull, ShowShadow = true, ShowGrid = true, ShowMarkers = true, ShowTravel, Lit;

    public event Action<string>? Status;
    public event Action? Picked;   // a click on the view chose something
    public event Action<Selection>? NeedsPermission;  // this edit would change files other cars need
    public int DrawnParts => Build(false).Items.Count;

    // GL draws nothing Avalonia knows of, so without this the mouse goes right through the view
    public bool HitTest(Point point) => true;

    public Viewport(Doc d)
    {
        doc = d;
        Focusable = true;
        ClipToBounds = true;
    }

    // --------------------------- Camera ---------------------------

    Vector3 Eye()
    {
        return Target + new Vector3(MathF.Sin(Yaw) * MathF.Cos(Pitch), -MathF.Sin(Pitch), MathF.Cos(Yaw) * MathF.Cos(Pitch)) * Distance;
    }

    bool OrthoOn => View != ViewMode.Perspective;

    Matrix4x4 ViewMatrix(Vector3 eye, Vector3 target)
    {
        var dir = eye - target;
        var up = Math.Abs(Vector3.Normalize(dir).Y) > 0.999f ? new Vector3(0, 0, 1) : new Vector3(0, -1, 0);
        return Matrix4x4.CreateLookAt(eye, target, up);
    }

    Matrix4x4 ProjMatrix(float aspect, float dist, bool ortho)
    {
        if (ortho)
        {
            float h = dist * 0.8f;
            return Matrix4x4.CreateOrthographic(h * aspect, h, 1f, Math.Max(5000, dist * 8));
        }
        return Matrix4x4.CreatePerspectiveFieldOfView(MathF.PI / 4, aspect, Math.Max(1f, dist / 100), Math.Max(6000, dist * 20));
    }

    public void SetView(ViewMode v)
    {
        View = v;
        switch (v)
        {
            case ViewMode.Top: Yaw = 0; Pitch = 1.5707f; break;
            case ViewMode.Front: Yaw = 0; Pitch = 0; break;
            case ViewMode.Side: Yaw = 1.5708f; Pitch = 0; break;
        }
        Redraw();
    }

    public void FrameCar()
    {
        if (doc.Car == null) return;
        var bb = CarTools.CarBounds(doc.Car);
        if (bb is { } b)
        {
            Target = (b.Min + b.Max) / 2;
            Distance = Math.Clamp((b.Max - b.Min).Length() * 1.55f, 60, 4000);
        }
        else { Target = Vector3.Zero; Distance = 260; }
        Redraw();
    }

    // Screen position (DIP) of a world point
    bool Project(Vector3 world, Matrix4x4 vp, out Vec2 screen)
    {
        var c = Vector4.Transform(new Vector4(world, 1), vp);
        screen = default;
        if (c.W <= 0.0001f) return false;
        screen = new Vec2((c.X / c.W * 0.5f + 0.5f) * (float)Bounds.Width, (0.5f - c.Y / c.W * 0.5f) * (float)Bounds.Height);
        return true;
    }

    Matrix4x4 UiViewProj()
    {
        float aspect = (float)Math.Max(Bounds.Width, 1) / (float)Math.Max(Bounds.Height, 1);
        var eye = Eye();
        return ViewMatrix(eye, Target) * ProjMatrix(aspect, Distance, OrthoOn);
    }

    (Vector3 Origin, Vector3 Dir) Ray(Point p)
    {
        var vp = UiViewProj();
        Matrix4x4.Invert(vp, out var inv);
        float x = (float)(p.X / Math.Max(Bounds.Width, 1)) * 2 - 1, y = 1 - (float)(p.Y / Math.Max(Bounds.Height, 1)) * 2;
        var a = Vector4.Transform(new Vector4(x, y, 0, 1), inv);
        var b = Vector4.Transform(new Vector4(x, y, 1, 1), inv);
        var o = new Vector3(a.X, a.Y, a.Z) / a.W;
        var e = new Vector3(b.X, b.Y, b.Z) / b.W;
        return (o, Vector3.Normalize(e - o));
    }

    // --------------------------- Picking ---------------------------

    static float RayTri(Vector3 o, Vector3 d, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 e1 = b - a, e2 = c - a, p = Vector3.Cross(d, e2);
        var det = Vector3.Dot(e1, p);
        if (Math.Abs(det) < 1e-8f) return -1;
        var inv = 1 / det;
        var s = o - a;
        var u = Vector3.Dot(s, p) * inv;
        if (u < 0 || u > 1) return -1;
        var q = Vector3.Cross(s, e1);
        var v = Vector3.Dot(d, q) * inv;
        if (v < 0 || u + v > 1) return -1;
        return Vector3.Dot(e2, q) * inv;
    }

    Selection PickPart(Point pos)
    {
        if (doc.D is not { } d) return new Selection(SelKind.None);
        // Markers first, they're small
        var vp = UiViewProj();
        var mouse = new Vec2((float)pos.X, (float)pos.Y);
        foreach (var (s, p) in MarkerPoints())
            if (Project(p, vp, out var sc) && (sc - mouse).Length() < 11) return s;

        var (o, dir) = Ray(pos);
        float best = float.MaxValue;
        var found = new Selection(SelKind.None);
        foreach (var part in CarPose.Build(d, null))
        {
            if (doc.Hidden.Contains(SelOf(part))) continue;
            var m = doc.Model(part.Slot);
            if (m == null || !Matrix4x4.Invert(part.World, out var inv)) continue;
            var lo = Vector3.Transform(o, inv);
            var ld = Vector3.TransformNormal(dir, inv);
            float scale = ld.Length();
            if (scale < 1e-6f) continue;
            ld /= scale;
            foreach (var f in m.Faces)
                for (int t = 0; t < (f.IsQuad ? 2 : 1); t++)
                {
                    float hit = RayTri(lo, ld, m.Verts[f.V0].Pos, m.Verts[t == 0 ? f.V1 : f.V2].Pos, m.Verts[t == 0 ? f.V2 : f.V3].Pos);
                    if (hit <= 0) continue;
                    var world = Vector3.Transform(lo + ld * hit, part.World);
                    var dist = (world - o).Length();
                    if (dist < best)
                    {
                        best = dist;
                        found = SelOf(part);
                    }
                }
        }
        return found;
    }

    static Selection SelOf(PartPose part) => part.Kind switch
    {
        PartKind.Body => new(SelKind.Body),
        PartKind.Wheel => new(SelKind.Wheel, part.Index),
        PartKind.Spring => new(SelKind.Spring, part.Index),
        PartKind.Axle => new(SelKind.Axle, part.Index),
        PartKind.Pin => new(SelKind.Pin, part.Index),
        PartKind.Spinner => new(SelKind.Spinner),
        PartKind.AerialTop => new(SelKind.AerialTip),
        _ => new(SelKind.AerialBase)
    };

    IEnumerable<(Selection, Vector3)> MarkerPoints()
    {
        if (doc.D is not { } d || !ShowMarkers) yield break;
        yield return (new(SelKind.CoM), d.CoM);
        yield return (new(SelKind.Weapon1), d.Weapon);
        if (d.HasWeapon2) yield return (new(SelKind.Weapon2), d.Weapon2);
        if (d.Camera != null)
        {
            yield return (new(SelKind.HoodCam), d.Camera.HoodOffset);
            yield return (new(SelKind.RearCam), d.Camera.RearOffset);
        }
        if (ShowHull && doc.Hull is { } hull)
        {
            for (int i = 0; i < hull.Spheres.Count; i++) yield return (new(SelKind.HullSphere, i), hull.Spheres[i].Pos);
            for (int i = 0; i < hull.Convex.Count; i++) yield return (new(SelKind.HullPart, i), hull.Convex[i].Offset);
        }
        if (d.Aerial.SecModel >= 0)
        {
            yield return (new(SelKind.AerialBase), d.Aerial.Offset);
            yield return (new(SelKind.AerialTip), CarPose.AerialTip(d));
        }
        for (int i = 0; i < 4; i++)
        {
            if (d.Springs[i].Model >= 0) yield return (new(SelKind.Spring, i), d.Springs[i].Offset);
            if (d.Axles[i].Model >= 0) yield return (new(SelKind.Axle, i), d.Axles[i].Offset);
        }
    }

    // --------------------------- Mouse ---------------------------

    enum Drag { None, Orbit, Pan, Axis, Free, Turn, Size }
    Drag drag;
    Point dragLast, dragStart;
    Vector3 dragAxis, accumulated;
    float dragPxPerUnit;
    Vec2 dragAxisScreen;
    bool movedOnce;
    int dragIndex;
    float turned, ringAngle;
    public int LastHandle = -1;
    public Point LastPress;
    public string HandleDebug(Vector3 world) => $"press {LastPress} handle {LastHandle}; tip now at {ScreenOf(world)}, view {Bounds.Size}, dist {Distance}, yaw {Yaw}, pitch {Pitch}, sim {doc.Sim != null}, tool {doc.Tool}, sel {doc.Sel.Label}, scale {VisualRoot?.RenderScaling}";

    float HandleLength => Distance * 0.16f;

    Vector3[] GizmoAxes => new[] { Vector3.UnitX, Vector3.UnitY, Vector3.UnitZ };

    // Rings of the rotate tool: u x v is the axis, so going from u to v is a positive turn
    static (Vector3 U, Vector3 V) RingAxes(int i) => i switch
    {
        0 => (Vector3.UnitY, Vector3.UnitZ),
        1 => (Vector3.UnitZ, Vector3.UnitX),
        _ => (Vector3.UnitX, Vector3.UnitY)
    };

    int HitGizmo(Point pos)
    {
        if (doc.Sim != null || doc.Anchor(doc.Sel) is not { } a) return -1;
        var vp = UiViewProj();
        if (!Project(a, vp, out var s0)) return -1;
        var mouse = new Vec2((float)pos.X, (float)pos.Y);
        if (doc.Tool != GizmoTool.Move && !doc.CanTransform(doc.Sel)) return -1;
        if (doc.Tool == GizmoTool.Rotate)
        {
            int best = -1; float bestDist = 7;
            for (int i = 0; i < 3; i++)
            {
                var (u, v) = RingAxes(i);
                for (int k = 0; k < 48; k++)
                {
                    float t0 = k * MathF.Tau / 48, t1 = (k + 1) * MathF.Tau / 48;
                    if (!Project(a + (u * MathF.Cos(t0) + v * MathF.Sin(t0)) * HandleLength, vp, out var p0)) continue;
                    if (!Project(a + (u * MathF.Cos(t1) + v * MathF.Sin(t1)) * HandleLength, vp, out var p1)) continue;
                    var ab = p1 - p0;
                    var q = Math.Clamp(Vec2.Dot(mouse - p0, ab) / Math.Max(ab.LengthSquared(), 0.01f), 0, 1);
                    var dist = (p0 + ab * q - mouse).Length();
                    if (dist < bestDist)
                    {
                        bestDist = dist; best = i;
                        ringAngle = t0;
                    }
                }
            }
            return best;
        }
        if ((mouse - s0).Length() < 9) return 3;
        for (int i = 0; i < 3; i++)
        {
            if (!Project(a + GizmoAxes[i] * HandleLength, vp, out var s1)) continue;
            var ab = s1 - s0;
            var t = Math.Clamp(Vec2.Dot(mouse - s0, ab) / Math.Max(ab.LengthSquared(), 1), 0, 1);
            if ((s0 + ab * t - mouse).Length() < 7) return i;
        }
        return -1;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        Focus();
        var pt = e.GetCurrentPoint(this);
        var props = pt.Properties;
        e.Pointer.Capture(this);
        BeginGesture(pt.Position);

        if (props.IsRightButtonPressed) { drag = Drag.Orbit; return; }
        if (props.IsMiddleButtonPressed) { drag = Drag.Pan; return; }
        if (!props.IsLeftButtonPressed) return;
        if (e.KeyModifiers.HasFlag(KeyModifiers.Alt)) { drag = Drag.Orbit; return; }
        PressLeft(pt.Position);
    }

    void BeginGesture(Point p)
    {
        dragLast = dragStart = p;
        movedOnce = false;
    }

    /// <summary>Left button: a handle if there's one under it, else a click or a turn of the view</summary>
    public void PressLeft(Point pos)
    {
        BeginGesture(pos);
        int g = HitGizmo(pos);
        LastHandle = g;
        LastPress = pos;
        if (g < 0 && doc.Tool != GizmoTool.Move && doc.Anchor(doc.Sel) != null && !doc.CanTransform(doc.Sel))
            Status?.Invoke(doc.Sel.Label + " is a point: it can only be moved");
        if (g >= 0 && doc.SharedFiles(doc.Sel).Count > 0)
        {
            // Ask first, the handle is there again after the answer
            NeedsPermission?.Invoke(doc.Sel);
            return;
        }
        if (g >= 0 && doc.Anchor(doc.Sel) is { } a)
        {
            accumulated = Vector3.Zero;
            doc.EndGesture();
            doc.InGesture = true;
            dragIndex = g;
            turned = 0;
            if (doc.Tool == GizmoTool.Rotate)
            {
                drag = Drag.Turn;
                var (u, v) = RingAxes(g);
                dragAxis = GizmoAxes[g];
                var at = a + (u * MathF.Cos(ringAngle) + v * MathF.Sin(ringAngle)) * HandleLength;
                var tangent = -u * MathF.Sin(ringAngle) + v * MathF.Cos(ringAngle);
                var vp0 = UiViewProj();
                Project(at, vp0, out var p0);
                Project(at + tangent * HandleLength * 0.1f, vp0, out var p1);
                var sv0 = (p1 - p0) / 0.1f; // pixels for one radian
                dragPxPerUnit = sv0.Length();
                dragAxisScreen = sv0.LengthSquared() > 1 ? Vec2.Normalize(sv0) : Vec2.UnitX;
                return;
            }
            if (doc.Tool == GizmoTool.Scale)
            {
                drag = Drag.Size;
                var vp1 = UiViewProj();
                Project(a, vp1, out var s0a);
                var axis = g == 3 ? CameraAxes().Right : GizmoAxes[g];
                Project(a + axis * HandleLength, vp1, out var s1a);
                var sva = s1a - s0a;
                dragPxPerUnit = Math.Max(sva.Length(), 10);
                dragAxisScreen = sva.LengthSquared() > 1 ? Vec2.Normalize(sva) : Vec2.UnitX;
                return;
            }
            if (g == 3) { drag = Drag.Free; return; }
            drag = Drag.Axis;
            dragAxis = GizmoAxes[g];
            var vp = UiViewProj();
            Project(a, vp, out var s0);
            Project(a + dragAxis * HandleLength, vp, out var s1);
            var sv = s1 - s0;
            dragPxPerUnit = sv.Length() / HandleLength;
            dragAxisScreen = sv.LengthSquared() > 1 ? Vec2.Normalize(sv) : Vec2.UnitX;
            return;
        }
        drag = Drag.Orbit; // a click selects, a drag on nothing turns the view
    }

    protected override void OnPointerMoved(PointerEventArgs e) => MoveTo(e.GetPosition(this));

    public void MoveTo(Point p)
    {
        if (drag == Drag.None)
        {
            Hover(p);
            return;
        }
        var dx = (float)(p.X - dragLast.X);
        var dy = (float)(p.Y - dragLast.Y);
        if (Math.Abs(p.X - dragStart.X) + Math.Abs(p.Y - dragStart.Y) > 3) movedOnce = true;
        dragLast = p;

        switch (drag)
        {
            case Drag.Orbit:
                if (View != ViewMode.Perspective) View = ViewMode.Perspective;
                Yaw -= dx * 0.008f;
                Pitch = Math.Clamp(Pitch + dy * 0.008f, -1.55f, 1.55f);
                break;
            case Drag.Pan:
                {
                    var (r, u) = CameraAxes();
                    float unit = Distance * 0.0022f;
                    Target += (-r * dx + u * dy) * unit;
                    break;
                }
            case Drag.Axis:
                {
                    var along = Vec2.Dot(new Vec2(dx, dy), dragAxisScreen) / Math.Max(dragPxPerUnit, 0.001f);
                    ApplySnapped(dragAxis * along);
                    break;
                }
            case Drag.Turn:
                {
                    turned += Vec2.Dot(new Vec2(dx, dy), dragAxisScreen) / Math.Max(dragPxPerUnit, 1);
                    float step = doc.Snap > 0 ? MathF.PI / 36 : 0; // 5 degrees
                    float amount = step > 0 ? MathF.Round(turned / step) * step : turned;
                    if (amount != 0)
                    {
                        turned -= amount;
                        doc.Rotate(doc.Sel, dragAxis, amount);
                    }
                    break;
                }
            case Drag.Size:
                {
                    float f = 1 + Vec2.Dot(new Vec2(dx, dy), dragAxisScreen) / dragPxPerUnit;
                    f = Math.Clamp(f, 0.5f, 2f);
                    doc.Scale(doc.Sel, dragIndex switch
                    {
                        0 => new Vector3(f, 1, 1),
                        1 => new Vector3(1, f, 1),
                        2 => new Vector3(1, 1, f),
                        _ => new Vector3(f)
                    });
                    break;
                }
            case Drag.Free:
                {
                    var (r, u) = CameraAxes();
                    float unit = HandleLength / Math.Max(PixelsPerHandle(), 1);
                    ApplySnapped((r * dx - u * dy) * unit);
                    break;
                }
        }
        Redraw();
    }

    string lastHover = "";
    void Hover(Point p)
    {
        if (doc.Sim != null || doc.D == null) return;
        bool handle = HitGizmo(p) >= 0;
        var over = handle ? doc.Sel : PickPart(p);
        Cursor = handle || over.Kind != SelKind.None ? new Cursor(StandardCursorType.Hand) : Cursor.Default;
        var text = handle ? "Drag to " + doc.Tool.ToString().ToLowerInvariant() + ": " + doc.Sel.Label : over.Kind != SelKind.None ? "Click to select: " + over.Label : "";
        if (text != lastHover) { lastHover = text; if (text != "") Status?.Invoke(text); }
    }

    float PixelsPerHandle()
    {
        var vp = UiViewProj();
        var (r, _) = CameraAxes();
        if (Project(Target, vp, out var a) && Project(Target + r * HandleLength, vp, out var b)) return (b - a).Length();
        return 100;
    }

    (Vector3 Right, Vector3 Up) CameraAxes()
    {
        var f = Vector3.Normalize(Target - Eye());
        var up0 = Math.Abs(f.Y) > 0.999f ? new Vector3(0, 0, 1) : new Vector3(0, -1, 0);
        var r = Vector3.Normalize(Vector3.Cross(f, up0));
        return (r, Vector3.Cross(r, f));
    }

    void ApplySnapped(Vector3 delta)
    {
        accumulated += delta;
        var snap = doc.Snap;
        Vector3 step = snap > 0
            ? new Vector3(MathF.Round(accumulated.X / snap), MathF.Round(accumulated.Y / snap), MathF.Round(accumulated.Z / snap)) * snap
            : accumulated;
        if (step == Vector3.Zero) return;
        accumulated -= step;
        doc.Move(doc.Sel, step);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        ReleaseAt(e.GetPosition(this), e.InitialPressMouseButton == MouseButton.Left);
        e.Pointer.Capture(null);
    }

    public void ReleaseAt(Point p, bool left = true)
    {
        if (drag is Drag.Orbit && !movedOnce && left && doc.Sim == null)
        {
            doc.Select(PickPart(p));
            Picked?.Invoke();
        }
        if (drag is Drag.Axis or Drag.Free or Drag.Turn or Drag.Size) doc.EndGesture();
        drag = Drag.None;
        Redraw();
    }

    /// <summary>Where a point of the car is on the view, in control units</summary>
    public Point? ScreenOf(Vector3 world)
    {
        if (!Project(world, UiViewProj(), out var s)) return null;
        return new Point(s.X, s.Y);
    }
    public Vector3 AxisTip(Vector3 anchor, int axis) => anchor + GizmoAxes[axis] * HandleLength;

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        Distance = Math.Clamp(Distance * (e.Delta.Y > 0 ? 0.9f : 1.11f), 8, 8000);
        Redraw();
    }

    /// <summary>Move what's selected from the keyboard, along the axes as they look from the camera</summary>
    public void Nudge(int dx, int dy, int dz, float amount)
    {
        var v = new Vector3(dx, dy, dz) * amount;
        doc.Move(doc.Sel, v);
        doc.EndGesture();
    }

    // --------------------------- Building a frame ---------------------------

    public void Redraw()
    {
        frame = Build(false);
        RequestNextFrameRendering();
    }

    static uint Rgba(byte r, byte g, byte b, byte a = 255) => (uint)(r | g << 8 | b << 16 | a << 24);
    static void Line(List<LineVertex> l, Vector3 a, Vector3 b, uint c)
    {
        l.Add(new LineVertex { Pos = a, Color = c });
        l.Add(new LineVertex { Pos = b, Color = c });
    }
    static void Cross(List<LineVertex> l, Vector3 p, float s, uint c)
    {
        Line(l, p - Vector3.UnitX * s, p + Vector3.UnitX * s, c);
        Line(l, p - Vector3.UnitY * s, p + Vector3.UnitY * s, c);
        Line(l, p - Vector3.UnitZ * s, p + Vector3.UnitZ * s, c);
    }
    static void Ring(List<LineVertex> l, Vector3 p, Vector3 a, Vector3 b, float r, uint c, int n = 24)
    {
        for (int i = 0; i < n; i++)
        {
            float t0 = i * MathF.Tau / n, t1 = (i + 1) * MathF.Tau / n;
            Line(l, p + (a * MathF.Cos(t0) + b * MathF.Sin(t0)) * r, p + (a * MathF.Cos(t1) + b * MathF.Sin(t1)) * r, c);
        }
    }

    Frame Build(bool clean)
    {
        var f = new Frame
        {
            Target = Target, Eye = Eye(), Distance = Distance, Ortho = OrthoOn,
            Yaw = Yaw, Pitch = Pitch, Shade = Shade, Env = ShowEnv, Wire = ShowWire, Lit = Lit,
            ModelVersion = doc.ModelVersion, TexVersion = doc.TextureVersion
        };
        if (doc.D is not { } d || doc.Car is not { } car) return f;

        var sim = doc.Sim;
        var root = sim != null ? doc.Dyn.Root : Matrix4x4.Identity;
        foreach (var part in CarPose.Build(d, sim != null ? doc.Dyn : null))
        {
            if (doc.Hidden.Contains(SelOf(part))) continue;
            var m = doc.Model(part.Slot);
            if (m == null) continue;
            bool sel = !clean && sim == null && part.Kind switch
            {
                PartKind.Body => doc.Sel.Kind == SelKind.Body,
                PartKind.Wheel => doc.Sel.Kind == SelKind.Wheel && doc.Sel.Index == part.Index,
                PartKind.Spring => doc.Sel.Kind == SelKind.Spring && doc.Sel.Index == part.Index,
                PartKind.Axle => doc.Sel.Kind == SelKind.Axle && doc.Sel.Index == part.Index,
                PartKind.Pin => doc.Sel.Kind == SelKind.Pin && doc.Sel.Index == part.Index,
                PartKind.Spinner => doc.Sel.Kind == SelKind.Spinner,
                PartKind.AerialTop => doc.Sel.Kind == SelKind.AerialTip,
                PartKind.AerialSection => doc.Sel.Kind == SelKind.AerialBase,
                _ => false
            };
            // the game reflects the environment on the body
            f.Items.Add((m, part.World, part.Tex, part.Kind == PartKind.Body, sel));
        }

        f.EnvColor = new Vector3(d.EnvR, d.EnvG, d.EnvB) / 255f;
        f.CarTexture = doc.SkinOverride ?? car.Game.Find(d.TPage);
        if (doc.PaintPreview != null) { f.Preview = doc.PaintPreview; f.PreviewW = doc.PaintW; f.PreviewH = doc.PaintH; f.PreviewVersion = doc.PaintVersion; }
        f.FxTexture = car.Game.Find("gfx\\fxpage1.bmp");
        f.EnvTexture = car.Game.Find("gfx\\envstill.bmp");

        float floor = CarPose.FloorY(d, car.Data.BodyModel >= 0 && doc.Model(d.BodyModel) is { } bm ? bm.Max.Y + d.BodyOffset.Y : 30);
        f.FloorY = floor;
        f.Floor = !clean;

        if (!clean && ShowShadow) AddShadow(f, d, car, floor, root);

        var groundCenter = sim != null ? new Vector3(sim.Pos.X, floor, sim.Pos.Z) : new Vector3(0, floor, 0);
        if (!clean && ShowGrid)
        {
            uint gc = Rgba(70, 76, 88, 255), g2 = Rgba(52, 58, 68, 255);
            float cell = 50;
            int n = 14;
            var cx = MathF.Round(groundCenter.X / cell) * cell;
            var cz = MathF.Round(groundCenter.Z / cell) * cell;
            float Y(float x, float z) => sim != null ? sim.GroundY(x, z) + (sim.Ground == SimGround.Flat ? 0.05f : 0) : floor - 0.2f;
            for (int i = -n; i <= n; i++)
            {
                uint col = (i % 4 == 0) ? gc : g2;
                for (int s = -n; s < n; s++)
                {
                    float a0 = s * cell, a1 = (s + 1) * cell, b = i * cell;
                    Line(f.Lines, new(cx + b, Y(cx + b, cz + a0), cz + a0), new(cx + b, Y(cx + b, cz + a1), cz + a1), col);
                    Line(f.Lines, new(cx + a0, Y(cx + a0, cz + b), cz + b), new(cx + a1, Y(cx + a1, cz + b), cz + b), col);
                }
            }
        }

        if (clean) return f;

        if ((ShowHull || doc.Sel.IsHull) && doc.Hull is { } hull)
        {
            uint hc = Rgba(90, 230, 150, 255), sc = Rgba(255, 200, 80, 255), selc = Rgba(255, 255, 255, 255);
            for (int k = 0; k < hull.Convex.Count; k++)
            {
                var c = hull.Convex[k];
                var col = doc.Sel == new Selection(SelKind.HullPart, k) ? selc : hc;
                foreach (var (a, b) in c.Edges)
                    if (a < c.Points.Length && b < c.Points.Length) Line(f.Overlay, Vector3.Transform(c.Points[a], root), Vector3.Transform(c.Points[b], root), col);
            }
            for (int k = 0; k < hull.Spheres.Count; k++)
            {
                var (pos, r) = hull.Spheres[k];
                var col = doc.Sel == new Selection(SelKind.HullSphere, k) ? selc : sc;
                var at = Vector3.Transform(pos, root);
                Ring(f.Overlay, at, Vector3.UnitX, Vector3.UnitZ, r, col, 16);
                if (col != selc) continue; // all three only on the selected one, or the car disappears behind them
                Ring(f.Overlay, at, Vector3.UnitX, Vector3.UnitY, r, col, 16);
                Ring(f.Overlay, at, Vector3.UnitY, Vector3.UnitZ, r, col, 16);
            }
        }

        if (ShowTravel)
        {
            uint up = Rgba(120, 200, 255), dn = Rgba(255, 140, 120);
            for (int i = 0; i < 4; i++)
            {
                var w = d.Wheels[i];
                if (!w.Present) continue;
                var c = w.Offset1;
                float t = (float)w.MaxPos, r = (float)w.Radius;
                Ring(f.Overlay, c - new Vector3(0, t, 0), Vector3.UnitY, Vector3.UnitZ, r, up);
                Ring(f.Overlay, c + new Vector3(0, t, 0), Vector3.UnitY, Vector3.UnitZ, r, dn);
            }
        }

        if (sim != null) return f;

        if (ShowMarkers)
        {
            float s = Distance * 0.012f;
            foreach (var (sel, p) in MarkerPoints())
            {
                uint c = sel.Kind switch
                {
                    SelKind.CoM => Rgba(255, 220, 60),
                    SelKind.Weapon1 or SelKind.Weapon2 => Rgba(255, 90, 90),
                    SelKind.HoodCam or SelKind.RearCam => Rgba(120, 220, 255),
                    SelKind.AerialBase or SelKind.AerialTip => Rgba(255, 150, 240),
                    SelKind.HullPart => Rgba(90, 230, 150),
                    SelKind.HullSphere => Rgba(255, 200, 80),
                    _ => Rgba(180, 190, 205)
                };
                Cross(f.Overlay, p, s, c);
            }
        }

        // Gizmo on what's selected
        if (doc.Anchor(doc.Sel) is { } a2)
        {
            float h = HandleLength;
            uint[] cols = { Rgba(240, 80, 80), Rgba(90, 220, 90), Rgba(90, 150, 255) };
            bool canTransform = doc.CanTransform(doc.Sel);
            for (int i = 0; i < 3; i++)
            {
                var tip = a2 + GizmoAxes[i] * h;
                if (doc.Tool == GizmoTool.Rotate)
                {
                    if (!canTransform) break;
                    var (ru, rv) = RingAxes(i);
                    Ring(f.Overlay, a2, ru, rv, h, cols[i], 48);
                    continue;
                }
                if (doc.Tool == GizmoTool.Scale)
                {
                    if (!canTransform) break;
                    Line(f.Overlay, a2, tip, cols[i]);
                    var (bu, bv) = RingAxes(i);
                    float bs = h * 0.05f;
                    Line(f.Overlay, tip + (bu + bv) * bs, tip + (bu - bv) * bs, cols[i]);
                    Line(f.Overlay, tip + (bu - bv) * bs, tip - (bu + bv) * bs, cols[i]);
                    Line(f.Overlay, tip - (bu + bv) * bs, tip - (bu - bv) * bs, cols[i]);
                    Line(f.Overlay, tip - (bu - bv) * bs, tip + (bu + bv) * bs, cols[i]);
                    continue;
                }
                Line(f.Overlay, a2, tip, cols[i]);
                var (u, v) = i == 1 ? (Vector3.UnitX, Vector3.UnitZ) : i == 0 ? (Vector3.UnitY, Vector3.UnitZ) : (Vector3.UnitX, Vector3.UnitY);
                Ring(f.Overlay, tip - GizmoAxes[i] * h * 0.08f, u, v, h * 0.05f, cols[i], 8);
                Line(f.Overlay, tip, tip - GizmoAxes[i] * h * 0.08f + u * h * 0.05f, cols[i]);
                Line(f.Overlay, tip, tip - GizmoAxes[i] * h * 0.08f - u * h * 0.05f, cols[i]);
                Line(f.Overlay, tip, tip - GizmoAxes[i] * h * 0.08f + v * h * 0.05f, cols[i]);
                Line(f.Overlay, tip, tip - GizmoAxes[i] * h * 0.08f - v * h * 0.05f, cols[i]);
            }
            var (rr, uu) = CameraAxes();
            Ring(f.Overlay, a2, rr, uu, h * 0.09f, Rgba(255, 255, 255), 16);

            // Where the same move lands on the other corners
            if (doc.Sel.HasFourCorners)
                foreach (var other in Ghosts(doc.Sel.Index, a2))
                    Ring(f.Overlay, other, rr, uu, h * 0.09f, Rgba(255, 210, 90), 16);
        }
        return f;
    }

    IEnumerable<Vector3> Ghosts(int i, Vector3 p)
    {
        Vector3 MX(Vector3 v) => new(-v.X, v.Y, v.Z);
        Vector3 MZ(Vector3 v) => new(v.X, v.Y, -v.Z);
        if (doc.D is not { } d) yield break;
        Vector3 At(int k) => doc.Sel.Kind switch
        {
            SelKind.Wheel => d.Wheels[k].Offset1,
            SelKind.Spring or SelKind.Pin => d.Springs[k].Offset,
            SelKind.Axle => d.Axles[k].Offset,
            _ => p
        };
        switch (doc.MoveMode)
        {
            case MoveMode.Mirror: yield return At(i ^ 1); break;
            case MoveMode.FrontBack: yield return At(i ^ 2); break;
            case MoveMode.All:
                yield return At(i ^ 1); yield return At(i ^ 2); yield return At(i ^ 3);
                break;
        }
    }

    void AddShadow(Frame f, CarData d, Car car, float floor, Matrix4x4 root)
    {
        double l, r, fr, bk;
        if (d.HasShadowTable && d.ShadowTable.Any(x => x != 0))
        {
            l = d.ShadowTable[0]; r = d.ShadowTable[1]; fr = d.ShadowTable[2]; bk = d.ShadowTable[3];
        }
        else if (CarTools.CarBounds(car) is { } b)
        {
            l = b.Min.X; r = b.Max.X; fr = b.Max.Z; bk = b.Min.Z;
        }
        else return;

        // Stock shadows sit on an 8 x 4 page, one 64 x 64 cell each
        var uv = new Vector4(0, 0, 1, 1);
        string? tex = car.Game.Find(d.TShadow);
        if (tex == null && d.HasShadowIndex && d.ShadowIndex >= 0)
        {
            tex = car.Game.Find("cars\\misc\\shadow.bmp");
            int cx = d.ShadowIndex % 8, cy = d.ShadowIndex / 8;
            uv = new Vector4(cx / 8f, cy / 4f, (cx + 1) / 8f, (cy + 1) / 4f);
        }
        f.ShadowTexture = tex;
        float y = doc.Sim != null ? 0.1f : floor - 0.15f;
        Vector3 W(double x, double z)
        {
            var v = new Vector3((float)x, doc.Sim != null ? 0 : y, (float)z);
            if (doc.Sim == null) return v;
            var q = Vector3.Transform(new Vector3((float)x, 0, (float)z), root);
            q.Y = doc.Sim.GroundY(q.X, q.Z) - 0.2f;
            return q;
        }
        f.Shadow = (W(l, fr), W(r, fr), W(r, bk), W(l, bk), uv);
    }

    // --------------------------- GL ---------------------------

    const string VsMesh = @"
in vec3 aPos; in vec3 aNormal; in vec2 aUv; in vec4 aColor;
uniform mat4 uMvp; uniform mat4 uModel; uniform mat4 uView;
out vec2 vUv; out vec4 vColor; out vec3 vN; out vec2 vEnv;
void main() {
  gl_Position = uMvp * vec4(aPos, 1.0);
  vUv = aUv; vColor = aColor;
  vec3 wn = normalize(mat3(uModel) * aNormal);
  vN = wn;
  vec3 vn = normalize(mat3(uView) * wn);
  vEnv = vn.xy * 0.5 + 0.5;
}";

    const string FsMesh = @"
in vec2 vUv; in vec4 vColor; in vec3 vN; in vec2 vEnv;
uniform sampler2D uTex; uniform int uMode; uniform vec4 uTint; uniform vec3 uEnvColor;
out vec4 fragColor;
void main() {
  if (uMode == 3) { // env pass: added on top
    vec4 e = texture(uTex, vEnv);
    fragColor = vec4(e.rgb * uEnvColor * 0.5, 1.0);
    return;
  }
  if (uMode == 4) { // shadow: the texture says how dark
    float a = texture(uTex, vUv).r;
    fragColor = vec4(0.0, 0.0, 0.0, a * 0.5);
    return;
  }
  if (uMode == 5) { // floor
    fragColor = uTint;
    return;
  }
  vec4 t = texture(uTex, vUv);
  if (t.a <= 0.01) discard;
  vec4 c = uMode == 1 ? vec4(vColor.rgb, vColor.a) : uMode == 2 ? vec4(0.72, 0.74, 0.78, 1.0) : t * vColor;
  if (uMode == 1 || uMode == 2) c.a = vColor.a;
  if (uMode == 2 || uTint.a < 0.0) { float l = 0.55 + 0.45 * max(0.0, dot(normalize(vN), normalize(vec3(0.3, -0.8, 0.4)))); c.rgb *= l; }
  c.rgb = mix(c.rgb, abs(uTint.rgb), abs(uTint.a) > 0.5 ? 0.0 : abs(uTint.a));
  fragColor = c;
}";

    const string VsLine = @"
in vec3 aPos; in vec4 aColor; uniform mat4 uMvp; out vec4 vColor;
void main() { gl_Position = uMvp * vec4(aPos, 1.0); vColor = aColor; }";
    const string FsLine = @"
in vec4 vColor; uniform vec4 uOverride; out vec4 fragColor;
void main() { fragColor = uOverride.a > 0.0 ? uOverride : vColor; }";

    protected override void OnOpenGlInit(GlInterface gi)
    {
        gl = GL.GetApi(gi.GetProcAddress);
        es = gi.ContextInfo.Version.Type == GlProfileType.OpenGLES;
        meshProg = Program(VsMesh, FsMesh, "aPos", "aNormal", "aUv", "aColor");
        lineProg = Program(VsLine, FsLine, "aPos", "", "", "aColor");
        uMvp = gl.GetUniformLocation(meshProg, "uMvp");
        uModel = gl.GetUniformLocation(meshProg, "uModel");
        uView = gl.GetUniformLocation(meshProg, "uView");
        uTint = gl.GetUniformLocation(meshProg, "uTint");
        uMode = gl.GetUniformLocation(meshProg, "uMode");
        uEnvColor = gl.GetUniformLocation(meshProg, "uEnvColor");
        lMvp = gl.GetUniformLocation(lineProg, "uMvp");
        lOverride = gl.GetUniformLocation(lineProg, "uOverride");

        overlayVao = gl.GenVertexArray();
        overlayVbo = gl.GenBuffer();
        floorVao = gl.GenVertexArray();
        floorVbo = gl.GenBuffer();
        whiteTex = MakeTexture(new byte[] { 255, 255, 255, 255 }, 1, 1);
        Redraw();
    }

    protected override void OnOpenGlDeinit(GlInterface gi)
    {
        meshes.Clear();
        textures.Clear();
        gl = null;
    }

    uint Program(string vs, string fs, params string[] attribs)
    {
        var head = es ? "#version 300 es\nprecision highp float;\n" : "#version 330 core\n";
        uint Compile(ShaderType t, string src)
        {
            var s = gl!.CreateShader(t);
            gl.ShaderSource(s, head + src);
            gl.CompileShader(s);
            gl.GetShader(s, ShaderParameterName.CompileStatus, out int ok);
            if (ok == 0) throw new Exception("Shader: " + gl.GetShaderInfoLog(s));
            return s;
        }
        var p = gl!.CreateProgram();
        var a = Compile(ShaderType.VertexShader, vs);
        var b = Compile(ShaderType.FragmentShader, fs);
        gl.AttachShader(p, a);
        gl.AttachShader(p, b);
        for (uint i = 0; i < attribs.Length; i++) if (attribs[i] != "") gl.BindAttribLocation(p, i, attribs[i]);
        gl.LinkProgram(p);
        gl.GetProgram(p, ProgramPropertyARB.LinkStatus, out int linked);
        if (linked == 0) throw new Exception("Link: " + gl.GetProgramInfoLog(p));
        return p;
    }

    unsafe uint MakeTexture(byte[] rgba, int w, int h)
    {
        var t = gl!.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, t);
        fixed (byte* d = rgba)
            gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)w, (uint)h, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
        gl.GenerateMipmap(TextureTarget.Texture2D);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        return t;
    }

    uint previewId;
    int previewVersion = -1;
    uint PreviewTex(Frame f)
    {
        if (previewId != 0 && previewVersion == f.PreviewVersion) return previewId;
        if (previewId != 0) gl!.DeleteTexture(previewId);
        previewId = MakeTexture(f.Preview!, f.PreviewW, f.PreviewH);
        previewVersion = f.PreviewVersion;
        return previewId;
    }

    uint Tex(string? path, int version)
    {
        if (path == null) return whiteTex;
        if (textures.TryGetValue(path, out var t) && t.Version == version) return t.Id;
        var rgba = Doc.LoadTexture(path, out int w, out int h);
        if (t != null && t.Id != 0) gl!.DeleteTexture(t.Id);
        var id = rgba != null ? MakeTexture(rgba, w, h) : whiteTex;
        textures[path] = new GpuTex { Id = id == whiteTex ? 0 : id, Version = version };
        return id;
    }

    static uint ToRgba(uint argb, bool translucent)
    {
        uint a = translucent ? argb >> 24 : 255;
        uint r = (argb >> 16) & 255, g = (argb >> 8) & 255, b = argb & 255;
        return r | g << 8 | b << 16 | a << 24;
    }

    unsafe GpuMesh Mesh(Prm m, int version)
    {
        if (!meshes.TryGetValue(m, out var g)) meshes[m] = g = new GpuMesh();
        if (g.Version == version) return g;
        g.Version = version;
        g.Batches.Clear();

        var groups = new SortedDictionary<int, List<MeshVertex>>();
        var lines = new List<LineVertex>();
        void Corner(List<MeshVertex> l, PrmFace f, int c)
        {
            var i = f.Index(c);
            bool tr = (f.Flags & (FaceFlags.Translucent | FaceFlags.Additive)) != 0;
            l.Add(new MeshVertex { Pos = m.Verts[i].Pos, Normal = m.Verts[i].Normal, Uv = new Vec2(f.U[c], f.V[c]), Color = ToRgba(f.Colors[c], tr) });
        }
        foreach (var f in m.Faces)
        {
            int pass = (f.Flags & FaceFlags.Additive) != 0 && (f.Flags & FaceFlags.Translucent) != 0 ? 2 : (f.Flags & FaceFlags.Translucent) != 0 ? 1 : 0;
            bool textured = f.TPage >= 0;
            bool env = (f.Flags & FaceFlags.NoEnv) == 0;
            int key = pass * 4 + (textured ? 2 : 0) + (env ? 1 : 0);
            if (!groups.TryGetValue(key, out var l)) groups[key] = l = new List<MeshVertex>();
            // Polygons turn clockwise from the front
            Corner(l, f, 0); Corner(l, f, 1); Corner(l, f, 2);
            if (f.IsQuad) { Corner(l, f, 0); Corner(l, f, 2); Corner(l, f, 3); }
            if ((f.Flags & FaceFlags.DoubleSided) != 0)
            {
                Corner(l, f, 2); Corner(l, f, 1); Corner(l, f, 0);
                if (f.IsQuad) { Corner(l, f, 3); Corner(l, f, 2); Corner(l, f, 0); }
            }
            uint wc = Rgba(20, 24, 30);
            for (int c = 0; c < f.Corners; c++)
            {
                Line(lines, m.Verts[f.Index(c)].Pos, m.Verts[f.Index((c + 1) % f.Corners)].Pos, wc);
            }
        }
        var all = new List<MeshVertex>();
        foreach (var (key, l) in groups)
        {
            g.Batches.Add((key / 4, (key & 2) != 0, (key & 1) != 0, all.Count, l.Count));
            all.AddRange(l);
        }
        if (g.Vao == 0) { g.Vao = gl!.GenVertexArray(); g.Vbo = gl.GenBuffer(); g.LineVao = gl.GenVertexArray(); g.LineVbo = gl.GenBuffer(); }
        gl!.BindVertexArray(g.Vao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, g.Vbo);
        var arr = all.ToArray();
        fixed (MeshVertex* p = arr) gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(arr.Length * sizeof(MeshVertex)), p, BufferUsageARB.StaticDraw);
        uint st = (uint)sizeof(MeshVertex);
        gl.EnableVertexAttribArray(0); gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, st, (void*)0);
        gl.EnableVertexAttribArray(1); gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, st, (void*)12);
        gl.EnableVertexAttribArray(2); gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, st, (void*)24);
        gl.EnableVertexAttribArray(3); gl.VertexAttribPointer(3, 4, VertexAttribPointerType.UnsignedByte, true, st, (void*)32);

        gl.BindVertexArray(g.LineVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, g.LineVbo);
        var la = lines.ToArray();
        g.LineCount = la.Length;
        fixed (LineVertex* p = la) gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(la.Length * sizeof(LineVertex)), p, BufferUsageARB.StaticDraw);
        SetLineAttribs();
        gl.BindVertexArray(0);
        return g;
    }

    unsafe void SetLineAttribs()
    {
        uint st = (uint)sizeof(LineVertex);
        gl!.EnableVertexAttribArray(0); gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, st, (void*)0);
        gl.EnableVertexAttribArray(3); gl.VertexAttribPointer(3, 4, VertexAttribPointerType.UnsignedByte, true, st, (void*)12);
    }

    unsafe void Mat(int loc, Matrix4x4 m) => gl!.UniformMatrix4(loc, 1, false, (float*)&m);

    unsafe void DrawLines(List<LineVertex> l, Matrix4x4 vp)
    {
        if (l.Count == 0) return;
        gl!.UseProgram(lineProg);
        Mat(lMvp, vp);
        gl.BindVertexArray(overlayVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, overlayVbo);
        var arr = l.ToArray();
        fixed (LineVertex* p = arr) gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(arr.Length * sizeof(LineVertex)), p, BufferUsageARB.StreamDraw);
        SetLineAttribs();
        gl.DrawArrays(PrimitiveType.Lines, 0, (uint)arr.Length);
    }

    unsafe void DrawQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector4 uv)
    {
        var v = new[]
        {
            new MeshVertex { Pos = a, Uv = new(uv.X, uv.Y), Color = 0xffffffff }, new MeshVertex { Pos = b, Uv = new(uv.Z, uv.Y), Color = 0xffffffff },
            new MeshVertex { Pos = c, Uv = new(uv.Z, uv.W), Color = 0xffffffff }, new MeshVertex { Pos = a, Uv = new(uv.X, uv.Y), Color = 0xffffffff },
            new MeshVertex { Pos = c, Uv = new(uv.Z, uv.W), Color = 0xffffffff }, new MeshVertex { Pos = d, Uv = new(uv.X, uv.W), Color = 0xffffffff },
        };
        gl!.BindVertexArray(floorVao);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, floorVbo);
        fixed (MeshVertex* p = v) gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(6 * sizeof(MeshVertex)), p, BufferUsageARB.StreamDraw);
        uint st = (uint)sizeof(MeshVertex);
        gl.EnableVertexAttribArray(0); gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, st, (void*)0);
        gl.EnableVertexAttribArray(1); gl.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, st, (void*)12);
        gl.EnableVertexAttribArray(2); gl.VertexAttribPointer(2, 2, VertexAttribPointerType.Float, false, st, (void*)24);
        gl.EnableVertexAttribArray(3); gl.VertexAttribPointer(3, 4, VertexAttribPointerType.UnsignedByte, true, st, (void*)32);
        gl.DrawArrays(PrimitiveType.Triangles, 0, 6);
    }

    void Draw(Frame f, int w, int h, Vector3 bg, bool floor)
    {
        var g = gl!;
        var aspect = w / (float)Math.Max(h, 1);
        var view = ViewMatrix(f.Eye, f.Target);
        var vp = view * ProjMatrix(aspect, f.Distance, f.Ortho);

        g.Viewport(0, 0, (uint)w, (uint)h);
        g.ClearColor(bg.X, bg.Y, bg.Z, 1);
        g.ClearDepth(1f);
        g.DepthMask(true);
        g.Enable(EnableCap.DepthTest);
        g.DepthFunc(DepthFunction.Lequal);
        g.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        g.Enable(EnableCap.CullFace);
        g.CullFace(TriangleFace.Back);
        g.FrontFace(FrontFaceDirection.CW);
        g.Disable(EnableCap.Blend);

        g.UseProgram(meshProg);
        g.ActiveTexture(TextureUnit.Texture0);
        g.Uniform1(g.GetUniformLocation(meshProg, "uTex"), 0);
        Mat(uView, view);

        if (floor && f.Floor && doc.Sim == null)
        {
            g.Disable(EnableCap.CullFace);
            g.Uniform1(uMode, 5);
            g.Uniform4(uTint, 0.30f, 0.33f, 0.38f, 1f);
            Mat(uMvp, vp);
            Mat(uModel, Matrix4x4.Identity);
            float s = 6000;
            DrawQuad(new(-s, f.FloorY, s), new(s, f.FloorY, s), new(s, f.FloorY, -s), new(-s, f.FloorY, -s), new(0, 0, 1, 1));
            g.Enable(EnableCap.CullFace);
        }
        else if (floor && f.Floor && doc.Sim != null)
        {
            g.Disable(EnableCap.CullFace);
            g.Uniform1(uMode, 5);
            g.Uniform4(uTint, 0.30f, 0.33f, 0.38f, 1f);
            Mat(uMvp, vp);
            Mat(uModel, Matrix4x4.Identity);
            if (doc.Sim.Ground == SimGround.Flat)
            {
                float s = 30000;
                DrawQuad(new(-s, 0.2f, s), new(s, 0.2f, s), new(s, 0.2f, -s), new(-s, 0.2f, -s), new(0, 0, 1, 1));
            }
            g.Enable(EnableCap.CullFace);
        }

        // lines of the floor under the car
        g.Enable(EnableCap.Blend);
        g.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        DrawLines(f.Lines, vp);

        if (f.Shadow is { } s2)
        {
            g.UseProgram(meshProg);
            g.Disable(EnableCap.CullFace);
            g.DepthMask(false);
            g.Uniform1(uMode, 4);
            Mat(uMvp, vp);
            Mat(uModel, Matrix4x4.Identity);
            g.BindTexture(TextureTarget.Texture2D, Tex(f.ShadowTexture, 0));
            DrawQuad(s2.A, s2.B, s2.C, s2.D, s2.Uv);
            g.DepthMask(true);
            g.Enable(EnableCap.CullFace);
        }

        g.UseProgram(meshProg);
        // Opaque, env on top, then see-through
        uint carTex = f.Preview != null ? PreviewTex(f) : Tex(f.CarTexture, f.TexVersion);
        uint fxTex = Tex(f.FxTexture, 0);
        uint envTex = Tex(f.EnvTexture, 0);

        void Pass(int pass)
        {
            g.Uniform1(uMode, f.Shade switch { ShadeMode.VertexColours => 1, ShadeMode.Clay => 2, _ => 0 });
            foreach (var it in f.Items)
            {
                var mesh = Mesh(it.Mesh, f.ModelVersion);
                g.BindVertexArray(mesh.Vao);
                g.UseProgram(meshProg);
                Mat(uMvp, it.World * vp);
                Mat(uModel, it.World);
                g.Uniform4(uTint, 0f, 0f, 0f, f.Lit ? -0.001f : 0f);
                foreach (var b in mesh.Batches)
                {
                    if (b.Pass != pass) continue;
                    g.BindTexture(TextureTarget.Texture2D, b.Textured && f.Shade == ShadeMode.Textured ? (it.Tex == 1 ? fxTex : carTex) : whiteTex);
                    g.DrawArrays(PrimitiveType.Triangles, b.First, (uint)b.Count);
                }
            }
        }

        Pass(0);

        // Reflections add the game's dark environment picture. Without the picture there's nothing to add
        if (f.Env && f.EnvTexture != null && f.Shade == ShadeMode.Textured && (f.EnvColor.X + f.EnvColor.Y + f.EnvColor.Z) > 0)
        {
            g.Enable(EnableCap.Blend);
            g.BlendFunc(BlendingFactor.One, BlendingFactor.One);
            g.DepthMask(false);
            g.Uniform1(uMode, 3);
            g.Uniform3(uEnvColor, f.EnvColor.X, f.EnvColor.Y, f.EnvColor.Z);
            g.BindTexture(TextureTarget.Texture2D, envTex);
            foreach (var it in f.Items)
            {
                if (!it.Env) continue;
                var mesh = Mesh(it.Mesh, f.ModelVersion);
                g.BindVertexArray(mesh.Vao);
                Mat(uMvp, it.World * vp);
                Mat(uModel, it.World);
                foreach (var b in mesh.Batches)
                    if (b.Env && b.Pass == 0) g.DrawArrays(PrimitiveType.Triangles, b.First, (uint)b.Count);
            }
        }

        g.Enable(EnableCap.Blend);
        g.DepthMask(false);
        g.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        Pass(1);
        g.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
        Pass(2);
        g.DepthMask(true);
        g.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        // Wireframe, and what's selected is outlined so its colours stay what they are
        g.UseProgram(lineProg);
        foreach (var it in f.Items)
        {
            if (!f.Wire && !it.Selected) continue;
            var mesh = Mesh(it.Mesh, f.ModelVersion);
            Mat(lMvp, it.World * vp);
            if (it.Selected) g.Uniform4(lOverride, 0.35f, 0.7f, 1f, 0.55f);
            else g.Uniform4(lOverride, 0f, 0f, 0f, 0f);
            g.BindVertexArray(mesh.LineVao);
            g.DrawArrays(PrimitiveType.Lines, 0, (uint)mesh.LineCount);
        }
        g.Uniform4(lOverride, 0f, 0f, 0f, 0f);

        g.Disable(EnableCap.DepthTest);
        g.Disable(EnableCap.CullFace);
        DrawLines(f.Overlay, vp);
        g.BindVertexArray(0);
    }

    protected override unsafe void OnOpenGlRender(GlInterface gi, int fb)
    {
        if (gl == null) return;
        var f = frame ?? Build(false);
        float scale = (float)(VisualRoot?.RenderScaling ?? 1);
        int w = Math.Max(1, (int)(Bounds.Width * scale)), h = Math.Max(1, (int)(Bounds.Height * scale));
        try
        {
            if (captureDone != null) RunCapture();
            gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
            Draw(f, w, h, f.Background, true);
            gl.Disable(EnableCap.DepthTest);
            gl.Disable(EnableCap.CullFace);
            gl.Disable(EnableCap.Blend);
            gl.FrontFace(FrontFaceDirection.Ccw);
            gl.UseProgram(0);
        }
        catch (Exception ex)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => Status?.Invoke("3D error: " + ex.Message));
        }
        if (doc.Sim != null) RequestNextFrameRendering();
    }

    unsafe void RunCapture()
    {
        var done = captureDone!;
        captureDone = null;
        int size = captureSize;
        var g = gl!;
        var fbo = g.GenFramebuffer();
        var col = g.GenRenderbuffer();
        var dep = g.GenRenderbuffer();
        g.BindRenderbuffer(RenderbufferTarget.Renderbuffer, col);
        g.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.Rgba8, (uint)size, (uint)size);
        g.BindRenderbuffer(RenderbufferTarget.Renderbuffer, dep);
        g.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent24, (uint)size, (uint)size);
        g.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
        g.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, RenderbufferTarget.Renderbuffer, col);
        g.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, dep);
        var cf = captureFrame ?? frame!;
        try
        {
            Draw(cf, size, size, captureClean ? new Vector3(1, 1, 1) : cf.Background, !captureClean);
            var px = new byte[size * size * 4];
            fixed (byte* p = px) g.ReadPixels(0, 0, (uint)size, (uint)size, PixelFormat.Rgba, PixelType.UnsignedByte, p);
            var bmp = new SKBitmap(size, size, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            for (int y = 0; y < size; y++)
                System.Runtime.InteropServices.Marshal.Copy(px, (size - 1 - y) * size * 4, bmp.GetPixels() + y * bmp.RowBytes, size * 4);
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++) { var c = bmp.GetPixel(x, y); bmp.SetPixel(x, y, c.WithAlpha(255)); }
            done.SetResult(bmp);
        }
        catch (Exception ex) { done.SetException(ex); }
        g.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        g.DeleteFramebuffer(fbo);
        g.DeleteRenderbuffer(col);
        g.DeleteRenderbuffer(dep);
    }

    Frame? captureFrame;

    /// <summary>A picture of the view, from OpenGL. Clean: white background, no helpers, for the carbox</summary>
    public Task<SKBitmap> Capture(int size, bool clean)
    {
        var tcs = new TaskCompletionSource<SKBitmap>(TaskCreationOptions.RunContinuationsAsynchronously);
        captureFrame = Build(clean);
        captureFrame.Floor = !clean;
        captureSize = size;
        captureClean = clean;
        captureDone = tcs;
        RequestNextFrameRendering();
        return tcs.Task;
    }
}
