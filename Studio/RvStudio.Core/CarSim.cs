using System.Numerics;

namespace RvStudio.Core;

public enum SimGround { Flat, Bumps, Ramp }

/// <summary>
/// A drive simulation from the car's parameters, for quick trials inside the editor. It is not the game:
/// the car physics source isn't available, so forces are worked out from what each parameter means and
/// a few constants are fitted so stock cars reach their real TopEnd and Acc. The real check is Test Run in RVGL.
/// Units are the game's: 1 unit = 0.5 cm, Y down.
/// </summary>
public sealed class CarSim
{
    public const float MphToUnits = 89.408f;
    const float Gravity = 2200f;
    const float Dt = 1f / 240f;

    // Fitted on the stock cars, see the tests
    public static float EngineScale = 0.086f;
    public static float DragScale = 1f;
    public static float RollScale = 0.3f;
    const float CorneringSlip = 60f; // units/s of sideways slip for full grip

    public readonly CarData Car;
    public SimGround Ground = SimGround.Flat;

    public Vector3 Pos;
    public Quaternion Rot = Quaternion.Identity;
    public Vector3 Vel, AngVel;
    public readonly float[] Compression = new float[4];
    public readonly float[] WheelSpin = new float[4];
    public readonly float[] SteerAngle = new float[4];
    public readonly bool[] Touching = new bool[4];
    public float Throttle, Steer;
    float steerNow, engineNow;
    public float Time;

    readonly float mass;
    readonly Vector3 invInertia;

    public CarSim(CarData car)
    {
        Car = car;
        mass = (float)car.BodyMass + car.Wheels.Where(w => w.Present).Sum(w => (float)w.Mass);
        if (mass <= 0) mass = 1;
        float I(float v) => v > 1 ? 1f / v : 1f / 1000f;
        invInertia = new Vector3(I(car.Inertia[0].X), I(car.Inertia[1].Y), I(car.Inertia[2].Z));
        Reset();
    }

    public void Reset()
    {
        Rot = Quaternion.Identity;
        Vel = AngVel = Vector3.Zero;
        Time = 0;
        steerNow = engineNow = 0;
        // Wheels touching the ground at rest
        float low = 0;
        foreach (var w in Car.Wheels) if (w.Present) low = Math.Max(low, w.Offset1.Y + (float)w.Radius);
        if (low == 0) low = 20;
        Pos = new Vector3(0, -low, 0);
    }

    public float GroundY(float x, float z) => Ground switch
    {
        SimGround.Bumps => 6f * MathF.Sin(x * 0.012f) * MathF.Sin(z * 0.009f),
        SimGround.Ramp => z > 800 && z < 1400 ? -(z - 800) * 0.18f : 0,
        _ => 0
    };

    public float SpeedForward => Vector3.Dot(Vel, Vector3.Transform(Vector3.UnitZ, Rot));
    public float SpeedMph => Vel.Length() / MphToUnits;
    public Vector3 Up => Vector3.Transform(-Vector3.UnitY, Rot);

    public Matrix4x4 Matrix => Matrix4x4.CreateFromQuaternion(Rot) * Matrix4x4.CreateTranslation(Pos);

    /// <summary>Move forward by real seconds</summary>
    public void Advance(float seconds)
    {
        int steps = Math.Clamp((int)Math.Round(seconds / Dt), 1, 40);
        for (int i = 0; i < steps; i++) Step();
    }

    void Step()
    {
        var c = Car;
        var com = Pos + Vector3.Transform(c.CoM, Rot);
        var force = new Vector3(0, Gravity * mass, 0);
        var torque = Vector3.Zero;

        // Input follows slowly, as SteerRate and EngineRate say
        steerNow += (Steer - steerNow) * Math.Min(1f, Dt * Math.Max(1f, (float)c.SteerRate * 3f));
        engineNow += (Throttle - engineNow) * Math.Min(1f, Dt * Math.Max(0.5f, (float)c.EngineRate));

        var right = Vector3.Transform(Vector3.UnitX, Rot);
        var fwd = Vector3.Transform(Vector3.UnitZ, Rot);
        float vmax = (float)c.TopSpeed * MphToUnits;

        void Apply(Vector3 f, Vector3 at)
        {
            force += f;
            torque += Vector3.Cross(at - com, f);
        }
        Vector3 VelAt(Vector3 at) => Vel + Vector3.Cross(AngVel, at - com);

        for (int i = 0; i < 4; i++)
        {
            var w = c.Wheels[i];
            Touching[i] = false;
            Compression[i] = 0;
            if (!w.Present) continue;

            var rel = Vector3.Transform(w.Offset1, Rot);
            var anchor = Pos + rel;
            float ground = GroundY(anchor.X, anchor.Z);
            float radius = (float)Math.Max(w.Radius, 1);
            float travel = (float)Math.Max(w.MaxPos, 0.5);
            float gap = ground - anchor.Y;     // distance from the wheel's rest centre to the floor
            float x = radius + travel - gap;   // how much the spring is pressed
            if (x <= 0) { WheelSpin[i] += engineNow * 60f * Dt * (w.Powered ? 1 : 0); continue; }

            var sp = c.Springs[i];
            var pv = VelAt(anchor);
            float k = (float)Math.Max(sp.Stiffness, 50), damp = (float)Math.Max(sp.Damping, 2);
            float n = k * Math.Min(x, travel * 2) + damp * pv.Y;
            // past the end of the travel the tyre takes the load
            if (x > travel * 2) n += 6000f * (x - travel * 2);
            n = Math.Max(n, 0);
            n *= 1 + (float)c.DownForceMod * 0.02f * Math.Abs(SpeedForward) / 1000f;
            Touching[i] = true;
            Compression[i] = Math.Min(x, travel * 2) - travel;

            var contact = anchor + new Vector3(0, radius, 0);
            Apply(new Vector3(0, -n, 0), anchor);

            float steerA = w.Turnable ? -steerNow * (float)w.SteerRatio * MathF.PI : 0;
            SteerAngle[i] = steerA;
            var wf = fwd * MathF.Cos(steerA) + right * MathF.Sin(steerA);
            wf.Y = 0;
            if (wf.LengthSquared() < 1e-6f) continue;
            wf = Vector3.Normalize(wf);
            var wr = new Vector3(wf.Z, 0, -wf.X);

            var v = VelAt(contact);
            float vf = Vector3.Dot(v, wf), vr = Vector3.Dot(v, wr);
            WheelSpin[i] += vf / radius * Dt;

            float limit = n * (float)Math.Max(w.StaticFriction, 0.1);
            float slideLimit = n * (float)Math.Max(w.KineticFriction, 0.1);

            // Engine pushes less as top speed nears
            float drive = 0;
            if (w.Powered)
            {
                float ratio = vmax > 1 ? Math.Clamp(1 - vf / vmax, -1, 1) : 0;
                drive = engineNow * (float)w.EngineRatio * EngineScale * (engineNow >= 0 ? ratio : Math.Clamp(1 + vf / (vmax * 0.5f), -1, 1));
            }
            // Rolling loss and braking when off the throttle
            drive -= vf * (float)w.AxleFriction * 1.5f * RollScale * n / 1000f;

            float side = -vr / CorneringSlip * limit;
            var total = new Vector2(drive, side);
            float len = total.Length();
            if (len > limit)
            {
                total *= slideLimit / len;
            }
            Apply(wf * total.X + wr * total.Y, contact);
        }

        // Body touching the floor when flipped
        foreach (var corner in BodyPoints())
        {
            var p = Pos + Vector3.Transform(corner, Rot);
            float g = GroundY(p.X, p.Z);
            float over = p.Y - g;
            if (over <= 0) continue;
            var v = VelAt(p);
            var fn = 4000f * over + 40f * Math.Max(v.Y, 0);
            Apply(new Vector3(0, -fn, 0), p);
            var tang = new Vector3(v.X, 0, v.Z);
            if (tang.LengthSquared() > 1)
                Apply(-Vector3.Normalize(tang) * fn * (float)Math.Max(c.BodyKinetic, 0.2), p);
        }

        // Air
        force -= Vel * Vel.Length() * (float)c.Resistance * mass * 0.001f * DragScale;

        Vel += force / mass * Dt;
        Pos += Vel * Dt;

        // Inertia is in the car's frame
        var inv = Quaternion.Inverse(Rot);
        var tl = Vector3.Transform(torque, inv);
        var wl = Vector3.Transform(AngVel, inv);
        wl += new Vector3(tl.X * invInertia.X, tl.Y * invInertia.Y, tl.Z * invInertia.Z) * Dt;
        wl *= MathF.Max(0, 1 - (float)c.AngRes * Dt * 60f);
        AngVel = Vector3.Transform(wl, Rot);
        var spin = AngVel * Dt;
        if (spin.LengthSquared() > 1e-10f)
            Rot = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.Normalize(spin), spin.Length()) * Rot);
        Time += Dt;
    }

    Vector3[]? bodyPoints;
    IEnumerable<Vector3> BodyPoints()
    {
        if (bodyPoints != null) return bodyPoints;
        float w = 30, h = 20, l = 60;
        var ws = Car.Wheels.Where(x => x.Present).ToArray();
        if (ws.Length > 0)
        {
            w = ws.Max(x => Math.Abs(x.Offset1.X));
            l = ws.Max(x => Math.Abs(x.Offset1.Z)) + 10;
        }
        return bodyPoints = new[]
        {
            new Vector3(-w, -h, -l), new Vector3(w, -h, -l), new Vector3(-w, -h, l), new Vector3(w, -h, l),
            new Vector3(-w, 5, -l), new Vector3(w, 5, -l), new Vector3(-w, 5, l), new Vector3(w, 5, l)
        };
    }

    // ---------------------------- Auto runs ----------------------------

    public sealed record RunResult(string Name, float TopEnd, float Acc, float TopMph, string Note);

    /// <summary>Full throttle: top speed reached and the time to get near it, what TopEnd and Acc mean</summary>
    public static RunResult RunAcceleration(CarData car, float maxTime = 40)
    {
        var s = new CarSim(car);
        s.Advance(0.5f);
        s.Time = 0;
        s.Throttle = 1;
        float top = 0, lastCheck = 0, lastTop = 0, reach = 0;
        var samples = new List<(float T, float V)>();
        while (s.Time < maxTime)
        {
            s.Step();
            var v = s.Vel.Length();
            samples.Add((s.Time, v));
            top = Math.Max(top, v);
            if (s.Time - lastCheck > 2)
            {
                if (top - lastTop < 2) break;
                lastTop = top; lastCheck = s.Time;
            }
        }
        foreach (var (t, v) in samples) if (v >= top * 0.97f) { reach = t; break; }
        return new RunResult("Acceleration", top, reach, top / MphToUnits, $"0 to {top / MphToUnits:0.0} mph in {reach:0.0} s");
    }

    /// <summary>Steady full lock circle at some speed: how much it grips, whether it rolls over</summary>
    public static RunResult RunCircle(CarData car)
    {
        var s = new CarSim(car) { Throttle = 0.6f };
        s.Advance(0.2f);
        while (s.Time < 3) s.Step();
        s.Steer = 1;
        float bestG = 0; bool rolled = false;
        while (s.Time < 9)
        {
            s.Step();
            var v = new Vector3(s.Vel.X, 0, s.Vel.Z);
            var turn = s.AngVel.Y;
            bestG = Math.Max(bestG, Math.Abs(turn) * v.Length() / Gravity);
            if (Vector3.Dot(s.Up, -Vector3.UnitY) < 0.2f) rolled = true;
        }
        return new RunResult("Circle", 0, 0, s.SpeedMph, rolled ? "Rolled over in a hard turn" : $"Held {bestG:0.00} g sideways without rolling");
    }

    /// <summary>Over bumps: does it stay on its wheels, how much it bounces</summary>
    public static RunResult RunBumps(CarData car)
    {
        var s = new CarSim(car) { Ground = SimGround.Bumps, Throttle = 0.7f };
        float maxPitch = 0; int airborne = 0, steps = 0;
        while (s.Time < 10)
        {
            s.Step();
            maxPitch = Math.Max(maxPitch, 1 - Vector3.Dot(s.Up, -Vector3.UnitY));
            steps++;
            if (s.Touching.All(t => !t)) airborne++;
        }
        return new RunResult("Bumps", 0, 0, s.SpeedMph, $"In the air {100f * airborne / steps:0}% of the time, max tilt {MathF.Acos(1 - Math.Min(maxPitch, 2)) * 57.3f:0} deg");
    }

    /// <summary>What's written in the frontend bars comes from here</summary>
    public static void FillStats(CarData car)
    {
        var a = RunAcceleration(car);
        car.TopEnd = a.TopEnd;
        car.Acc = a.Acc;
        car.Weight = car.BodyMass;
        var powered = Enumerable.Range(0, 4).Where(i => car.Wheels[i].Powered).ToArray();
        car.Trans = powered.Length == 4 ? 0 : powered.Length > 0 && powered[0] < 2 ? 1 : 2;
        car.Statistics = true;
    }
}
