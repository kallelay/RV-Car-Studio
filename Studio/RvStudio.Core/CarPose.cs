using System.Numerics;

namespace RvStudio.Core;

public enum PartKind { Body, Wheel, Spring, Axle, Pin, Spinner, AerialSection, AerialTop }

/// <summary>Where one model of the car goes. Tex 0 is the car's texture, 1 the game's fx page</summary>
public readonly record struct PartPose(PartKind Kind, int Index, int Slot, Matrix4x4 World, int Tex);

/// <summary>What moves when the car is alive. Without it the car stands still at rest</summary>
public sealed class CarDynamics
{
    public readonly float[] Compression = new float[4]; // wheel up from rest
    public readonly float[] Spin = new float[4];
    public readonly float[] Steer = new float[4];
    public float SpinnerAngle;
    public Vector3 AerialBend;   // how far the tip is pushed from straight, in car space
    public Matrix4x4 Root = Matrix4x4.Identity;
}

/// <summary>How the game assembles a car from its parameters</summary>
public static class CarPose
{
    // aerial.h has 5 points along the aerial, 1/4 apart. In the game (compared with RVGL itself) Length is the
    // whole aerial from base to tip: 3 sections, then the top model as the last quarter
    public const int AerialPoints = 5;

    // Y goes where yAxis says, X stays as sideways as it can
    static Matrix4x4 Basis(Vector3 yAxis, Vector3 origin, float scaleY = 1)
    {
        var y = yAxis.LengthSquared() < 1e-8f ? Vector3.UnitY : Vector3.Normalize(yAxis);
        var refZ = Math.Abs(y.Z) > 0.99f ? Vector3.UnitY : Vector3.UnitZ;
        var x = Vector3.Normalize(Vector3.Cross(y, refZ));
        var z = Vector3.Cross(x, y);
        return Row(x, y * scaleY, z, origin);
    }

    // Z goes where zAxis says
    static Matrix4x4 BasisZ(Vector3 zAxis, Vector3 origin, float scaleZ = 1)
    {
        var z = zAxis.LengthSquared() < 1e-8f ? Vector3.UnitZ : Vector3.Normalize(zAxis);
        var refY = Math.Abs(z.Y) > 0.99f ? Vector3.UnitZ : Vector3.UnitY;
        var x = Vector3.Normalize(Vector3.Cross(refY, z));
        var y = Vector3.Cross(z, x);
        return Row(x, y, z * scaleZ, origin);
    }

    static Matrix4x4 Row(Vector3 x, Vector3 y, Vector3 z, Vector3 t) => new(
        x.X, x.Y, x.Z, 0,
        y.X, y.Y, y.Z, 0,
        z.X, z.Y, z.Z, 0,
        t.X, t.Y, t.Z, 1);

    /// <summary>Where the visible wheel is now</summary>
    public static Vector3 WheelPos(CarData d, int i, CarDynamics? dyn) =>
        d.Wheels[i].Offset1 - new Vector3(0, dyn?.Compression[i] ?? 0, 0);

    public static List<PartPose> Build(CarData d, CarDynamics? dyn = null)
    {
        var r = new List<PartPose>(24);
        var root = dyn?.Root ?? Matrix4x4.Identity;
        void Add(PartKind k, int i, int slot, Matrix4x4 m, int tex = 0)
        {
            if (slot >= 0) r.Add(new PartPose(k, i, slot, m * root, tex));
        }

        Add(PartKind.Body, 0, d.BodyModel, Matrix4x4.CreateTranslation(d.BodyOffset));

        for (int i = 0; i < 4; i++)
        {
            var w = d.Wheels[i];
            var pos = WheelPos(d, i, dyn);

            if (w.Present)
            {
                var m = Matrix4x4.CreateRotationX(dyn?.Spin[i] ?? 0);
                // Camber: its sign is the same for left and right, negative puts the bottom further out
                if (w.Camber != 0) m *= Matrix4x4.CreateRotationZ((float)(w.Camber * Math.PI / 180 * (w.Offset1.X > 0 ? 1 : -1)));
                if (dyn != null && dyn.Steer[i] != 0) m *= Matrix4x4.CreateRotationY(dyn.Steer[i]);
                m *= Matrix4x4.CreateTranslation(pos);
                Add(PartKind.Wheel, i, w.Model, m);
            }

            // Springs hang from their mount and stretch to the wheel: distance / Length
            var sp = d.Springs[i];
            if (sp.Model >= 0)
            {
                var dist = (pos - sp.Offset).Length();
                Add(PartKind.Spring, i, sp.Model, Basis(pos - sp.Offset, sp.Offset, sp.Length != 0 ? dist / (float)sp.Length : 1));
            }

            // Axles go from their mount to the wheel, stretched the same way
            var ax = d.Axles[i];
            if (ax.Model >= 0)
            {
                var dist = (pos - ax.Offset).Length();
                Add(PartKind.Axle, i, ax.Model, BasisZ(pos - ax.Offset, ax.Offset, ax.Length != 0 ? dist / (float)ax.Length : 1));
            }

            // Pins are stiff: they start at the spring's mount and point at the wheel.
            // Their models go towards -Y and Length is the usual negative multiplier
            var pin = d.Pins[i];
            if (pin.Model >= 0)
                Add(PartKind.Pin, i, pin.Model, Basis(pos - sp.Offset, sp.Offset, (float)pin.Length));
        }

        var s = d.Spinner;
        if (s.Model >= 0)
        {
            var m = Matrix4x4.Identity;
            if (dyn != null && dyn.SpinnerAngle != 0 && s.Axis.LengthSquared() > 1e-6f)
                m = Matrix4x4.CreateFromAxisAngle(Vector3.Normalize(s.Axis), dyn.SpinnerAngle);
            Add(PartKind.Spinner, 0, s.Model, m * Matrix4x4.CreateTranslation(s.Offset));
        }

        var a = d.Aerial;
        if (a.SecModel >= 0 || a.TopModel >= 0)
        {
            var dir = a.Direction.LengthSquared() < 1e-6f ? -Vector3.UnitY : Vector3.Normalize(a.Direction);
            float part = (float)a.Length / (AerialPoints - 1);
            var bend = dyn?.AerialBend ?? Vector3.Zero;
            Vector3 Node(int n)
            {
                float t = n / (float)(AerialPoints - 1);
                return a.Offset + dir * (part * n) + bend * (t * t);
            }
            // Models are 1 unit long: y = 0 is their far end, y = -1 the end nearer the base.
            // On the top model that puts the red box at the very tip, as in the game
            for (int n = 0; n < AerialPoints - 1; n++)
            {
                bool top = n == AerialPoints - 2;
                var from = Node(n);
                var to = Node(n + 1);
                var along = to - from;
                Add(top ? PartKind.AerialTop : PartKind.AerialSection, n, top ? a.TopModel : a.SecModel,
                    Basis(along, to, along.Length()), 1);
            }
        }
        return r;
    }

    /// <summary>Tip of the aerial</summary>
    public static Vector3 AerialTip(CarData d)
    {
        var a = d.Aerial;
        var dir = a.Direction.LengthSquared() < 1e-6f ? -Vector3.UnitY : Vector3.Normalize(a.Direction);
        return a.Offset + dir * (float)a.Length;
    }

    /// <summary>Where the wheels meet the floor (Y goes down)</summary>
    public static float FloorY(CarData d, float fallback = 30)
    {
        float low = float.MinValue;
        foreach (var w in d.Wheels) if (w.Present) low = Math.Max(low, w.Offset1.Y + (float)w.Radius);
        return low == float.MinValue ? fallback : low;
    }
}
