using System.Numerics;

namespace RvStudio.Core;

/// <summary>
/// What a car's numbers add up to, worked out directly from the formulas of the car physics, without running anything.
/// Forces are in the game's own units, mass x units per second squared. Everything is for standing on a flat floor.
/// </summary>
public sealed class CarCalc
{
    public const float Gravity = 2200f;
    public const float UnitsPerMph = CarSim.MphToUnits;
    public const float CmPerUnit = 0.5f;

    public float Mass, FrontShare, Wheelbase, Track, Height;
    public float FrontSag, RearSag, FrontTravel, RearTravel;
    public float FrontHz, RearHz, FrontDamping, RearDamping;
    public float Accel0, TopSpeedSet, TopSpeedReal, TimeConstant, TimeToTop, EngineResponse;
    public float GripFront, GripRear, SlideFront, SlideRear;
    public float LockDeg, TurnInTime, StraightenTime, TurnRadius, CornerSpeed;
    public float RollIndex, DragPerSecond, SpinDecayPerSecond;
    public bool WheelSpinFront, WheelSpinRear;
    public readonly List<string> Warnings = new();

    public static float ToMph(float unitsPerSecond) => unitsPerSecond / UnitsPerMph;

    public static CarCalc Of(CarData d, Hull? hull = null)
    {
        var c = new CarCalc { Mass = d.BodyMass > 0 ? (float)d.BodyMass : 1 };
        var present = d.Wheels.Where(w => w.Present).ToArray();
        if (present.Length == 0)
        {
            c.Warnings.Add("No wheels: the car can't drive.");
            return c;
        }

        // Physics works around the centre of mass: everything on the car moves by CoM
        Vector3 Local(WheelData w) => w.Offset1 + d.CoM;
        var front = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Present && Local(d.Wheels[i]).Z >= 0).ToArray();
        var rear = Enumerable.Range(0, 4).Where(i => d.Wheels[i].Present && Local(d.Wheels[i]).Z < 0).ToArray();
        float zf = front.Length > 0 ? front.Average(i => Local(d.Wheels[i]).Z) : 0;
        float zr = rear.Length > 0 ? rear.Average(i => Local(d.Wheels[i]).Z) : 0;
        c.Wheelbase = zf - zr;
        c.FrontShare = c.Wheelbase > 0 ? -zr / c.Wheelbase : 0.5f;
        c.Track = present.Average(w => Math.Abs(w.Offset1.X + w.Offset2.X)) * 2;

        float weight = c.Mass * Gravity;
        float loadF = front.Length > 0 ? weight * c.FrontShare / front.Length : 0;
        float loadR = rear.Length > 0 ? weight * (1 - c.FrontShare) / rear.Length : 0;

        float Sag(int[] ax, float load) => ax.Length > 0 ? load / Math.Max((float)ax.Average(i => d.Springs[i].Stiffness), 1) : 0;
        c.FrontSag = Sag(front, loadF);
        c.RearSag = Sag(rear, loadR);
        c.FrontTravel = front.Length > 0 ? (float)front.Average(i => d.Wheels[i].MaxPos) : 0;
        c.RearTravel = rear.Length > 0 ? (float)rear.Average(i => d.Wheels[i].MaxPos) : 0;

        (float Hz, float Ratio) Bounce(int[] ax, float load)
        {
            if (ax.Length == 0 || load <= 0) return (0, 0);
            float k = (float)ax.Average(i => d.Springs[i].Stiffness), damp = (float)ax.Average(i => d.Springs[i].Damping);
            float corner = load / Gravity;
            return (MathF.Sqrt(k / corner) / MathF.Tau, damp / (2 * MathF.Sqrt(k * corner)));
        }
        (c.FrontHz, c.FrontDamping) = Bounce(front, loadF);
        (c.RearHz, c.RearDamping) = Bounce(rear, loadR);

        // Engine: full push at rest, none left at TopSpeed, air takes a share of the speed each step
        float drive = present.Where(w => w.Powered).Sum(w => (float)w.EngineRatio / (float)Math.Max(w.Radius, 1));
        c.Accel0 = drive / c.Mass;
        c.TopSpeedSet = (float)d.TopSpeed * UnitsPerMph;
        float air = (float)d.Resistance * 120f;
        c.DragPerSecond = air;
        // The engine eases off by how fast the wheel spins, and that stays behind the road speed: the axle slows it,
        // the road brings it back up by a quarter of the gap each step. So the car gets past what the sum alone says
        var driven = present.Where(w => w.Powered).ToArray();
        float axle = driven.Length > 0 ? (float)driven.Average(w => w.AxleFriction) * 0.96f : 0;
        float wheelLag = 0.24f / (1 - 0.76f * (1 - axle));
        float slow = (c.TopSpeedSet > 0 ? c.Accel0 * wheelLag / c.TopSpeedSet : 0) + air;
        c.TopSpeedReal = slow > 0 ? c.Accel0 / slow : 0;
        c.TimeConstant = slow > 0 ? 1 / slow : 0;
        c.EngineResponse = d.EngineRate > 0 ? 1 / (float)d.EngineRate : 0;
        c.TimeToTop = c.TimeConstant * MathF.Log(200) + c.EngineResponse;

        bool Spin(int[] ax, float load) => ax.Any(i => d.Wheels[i].Powered && (float)d.Wheels[i].EngineRatio / (float)Math.Max(d.Wheels[i].Radius, 1) > (float)d.Wheels[i].StaticFriction * load);
        c.WheelSpinFront = Spin(front, loadF);
        c.WheelSpinRear = Spin(rear, loadR);

        // Grip: the sideways push grows with sliding speed until it reaches StaticFriction x load
        float Fric(int[] ax) => ax.Length > 0 ? (float)ax.Average(i => d.Wheels[i].StaticFriction) : 0;
        float SlideAt(int[] ax)
        {
            if (ax.Length == 0) return 0;
            float grip = (float)ax.Average(i => d.Wheels[i].Grip);
            return grip > 0 ? Fric(ax) / (grip * 0.35f * 0.96f) : 0;
        }
        c.GripFront = Fric(front);
        c.GripRear = Fric(rear);
        c.SlideFront = SlideAt(front);
        c.SlideRear = SlideAt(rear);

        // Steering
        var steer = present.Where(w => w.Turnable).ToArray();
        float lock_ = steer.Length > 0 ? steer.Max(w => Math.Abs((float)w.SteerRatio)) : 0;
        c.LockDeg = lock_ * 180 / MathF.PI;
        if (d.SteerRate > 0)
        {
            c.TurnInTime = 1 / ((float)d.SteerRate * 0.5f);
            c.StraightenTime = 1 / ((float)d.SteerRate * 4);
        }
        c.TurnRadius = lock_ > 0.01f && c.Wheelbase > 0 ? c.Wheelbase / MathF.Tan(Math.Min(lock_, 1.4f)) : 0;
        float mu = front.Length > 0 && rear.Length > 0 ? Math.Min(c.GripFront, c.GripRear) : Math.Max(c.GripFront, c.GripRear);
        c.CornerSpeed = MathF.Sqrt(Math.Max(mu, 0) * Gravity * c.TurnRadius);

        // Roll: a car rolls before it slides when grip beats half its track over its height
        c.Height = present.Max(w => w.Offset1.Y + d.CoM.Y + w.Offset2.Y + (float)w.Radius);
        c.RollIndex = c.Height > 0 ? c.Track / (2 * c.Height) : 0;
        c.SpinDecayPerSecond = (float)d.AngRes * 120f;

        // ---- Things to look at ----
        if (!present.Any(w => w.Powered)) c.Warnings.Add("No powered wheel: the engine does nothing.");
        if (steer.Length == 0) c.Warnings.Add("No wheel can steer.");
        if (present.Any(w => w.Powered && w.EngineRatio == 0)) c.Warnings.Add("A powered wheel has EngineRatio 0: it pushes nothing.");
        if (c.TopSpeedSet <= 0) c.Warnings.Add("TopSpeed is 0: the engine can't push at all.");
        if (d.SteerRate <= 0) c.Warnings.Add("SteerRate is 0: the wheels never turn.");
        if (d.EngineRate <= 0) c.Warnings.Add("EngineRate is 0: the engine never starts.");
        foreach (var (name, sag, travel) in new[] { ("front", c.FrontSag, c.FrontTravel), ("rear", c.RearSag, c.RearTravel) })
        {
            if (travel <= 0) continue;
            if (sag >= travel) c.Warnings.Add($"The {name} springs are too soft: the car's weight presses them {sag:0.0} but they have only {travel:0.0} of travel. It sits on its stops.");
            else if (sag < travel * 0.05f) c.Warnings.Add($"The {name} springs barely move under the car's weight ({sag:0.00} of {travel:0.0}). It will ride very hard.");
        }
        if (c.FrontShare is < 0.25f or > 0.75f) c.Warnings.Add($"{c.FrontShare * 100:0}% of the weight is on the front wheels: move CoM to balance it.");
        if (mu > c.RollIndex && c.RollIndex > 0) c.Warnings.Add("Grip beats the width of the car over its height: it may roll in hard turns.");
        if (c.Accel0 * c.Mass > 0 && c.Accel0 < 400) c.Warnings.Add("Very weak engine: it needs more than 7 seconds per 1000 units of speed.");

        // The hull holding the car up keeps the wheels off the floor: then it doesn't drive at all
        if (hull is { Spheres.Count: > 0 })
        {
            float lowest = hull.Spheres.Max(s => s.Pos.Y + d.CoM.Y + s.Radius);
            if (lowest > c.Height) c.Warnings.Add($"A hull sphere reaches {lowest - c.Height:0.0} below the wheels: the body rests on the floor and the wheels can't touch it.");
        }
        return c;
    }

    /// <summary>A copy of the car with the main numbers scaled, to try without changing anything</summary>
    public static CarData Scaled(CarData d, float engine = 1, float topSpeed = 1, float mass = 1, float springs = 1, float grip = 1, float steer = 1)
    {
        var o = d.Clone();
        o.TopSpeed *= topSpeed;
        o.BodyMass *= mass;
        foreach (var w in o.Wheels)
        {
            w.EngineRatio *= engine;
            w.Grip *= grip;
            w.StaticFriction *= grip;
            w.KineticFriction *= grip;
            if (w.Turnable) w.SteerRatio *= steer;
        }
        foreach (var s in o.Springs) s.Stiffness *= springs;
        return o;
    }
}
