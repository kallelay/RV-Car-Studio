using System.Numerics;

namespace RvStudio.Core;

public enum SimGround { Flat, Bumps, Ramp }

/// <summary>A surface, with the values of the game's material table</summary>
public sealed record Material(string Name, float Roughness, float Grip, float Hardness, float Bump, float BumpLength, byte R, byte G, byte B, Vector3 Moves = default)
{
    const float Mph = CarSim.MphToUnits;

    /// <summary>In the order of the numbers in a level's .ncp file</summary>
    public static readonly Material[] Game =
    {
        new("Default", 1, 1, 1, 0, 1, 66, 68, 74),
        new("Marble", 0.9f, 0.9f, 0.5f, 0, 1, 205, 200, 190),
        new("Stone", 0.9f, 0.9f, 0.5f, 0, 1, 125, 125, 120),
        new("Wood", 0.8f, 0.8f, 0.3f, 0, 1, 140, 90, 50),
        new("Sand", 0.5f, 0.6f, 0, 0, 1, 214, 190, 130),
        new("Plastic", 0.7f, 0.9f, 0.2f, 0, 1, 60, 120, 210),
        new("Carpet tile", 1, 0.7f, 0, 0, 1, 150, 40, 70),
        new("Carpet shag", 1, 0.7f, 0, 0, 1, 170, 70, 110),
        new("Boundary", 1, 1, 0, 0, 1, 90, 90, 90),
        new("Glass", 0.7f, 0.8f, 0.3f, 0, 1, 170, 215, 225),
        new("Ice", 0.4f, 0.4f, 0.2f, 0, 1, 215, 240, 255),
        new("Metal", 0.8f, 0.8f, 0.4f, 0, 1, 130, 135, 145),
        new("Grass", 0.7f, 0.5f, 0, 1, 40, 70, 140, 50),
        new("Bumpy metal", 0.8f, 0.8f, 0.4f, 1, 40, 95, 100, 110),
        new("Pebbles", 0.7f, 0.7f, 0.2f, 3, 70, 120, 112, 100),
        new("Gravel", 0.9f, 0.8f, 0.2f, 1, 40, 150, 140, 125),
        new("Conveyor 1", 1, 1, 0, 1, 40, 45, 45, 50, new Vector3(-287.38f, 128.745f, 388.38f)),
        new("Conveyor 2", 1, 1, 0, 1, 40, 45, 45, 50, new Vector3(287.38f, 128.745f, -388.38f)),
        new("Dirt 1", 1, 1, 0, 1, 80, 110, 75, 45),
        new("Dirt 2", 1, 1, 0, 1, 80, 120, 85, 50),
        new("Dirt 3", 1, 1, 0, 1, 80, 100, 70, 40),
        new("Ice 2", 0.45f, 0.45f, 0.2f, 0, 1, 200, 230, 250),
        new("Ice 3", 0.5f, 0.5f, 0.2f, 0, 1, 190, 225, 245),
        new("Wood 2", 0.8f, 0.8f, 0.4f, 0, 1, 155, 105, 60),
        new("Market conveyor 1", 1, 1, 0, 1, 40, 50, 50, 55, new Vector3(0, 0, 4 * Mph)),
        new("Market conveyor 2", 1, 1, 0, 1, 40, 50, 50, 55, new Vector3(-4 * Mph, 0, 0)),
        new("Paving", 0.9f, 0.9f, 0.5f, 0, 1, 150, 150, 145)
    };

    public static readonly Material Default = Game[0];

    /// <summary>The bands of the surfaces track, one of each kind</summary>
    public static readonly Material[] Track = new[] { 0, 1, 2, 3, 4, 5, 6, 9, 10, 11, 12, 13, 14, 15, 18, 24, 26 }.Select(i => Game[i]).ToArray();
}

/// <summary>
/// The drive simulation, a new implementation of the behaviour of the original game's car physics in Arcade mode:
/// fixed 8 ms steps, wheels as spheres on sliding suspension, tyre and spring impulses, hull spheres for the body,
/// spheres against polygons, with the game's constants and surfaces. Nothing is fitted. RVGL may have changed
/// things since, so the final check is still Test Run.
/// Units are the game's: 1 unit = 0.5 cm, Y down. Pos and Rot place the car's model, the physics runs at its centre.
/// </summary>
public sealed class CarSim
{
    public const float MphToUnits = 1f / 0.01118f;
    const float Gravity = 2200f;
    const float Dt = 0.008f;
    const float FrictionScale = 120f;
    const float ContactEpsilon = 2f;
    const float SmallImpulse = 0.005f;

    public readonly CarData Car;
    public SimGround Ground = SimGround.Flat;
    /// <summary>What it drives on. Without one it's made from Ground</summary>
    public CollWorld? World;
    /// <summary>A road's height, for drawing and placing: the same shape as its World</summary>
    public Func<float, float, float>? GroundFunc;
    public bool TouchesWall { get; private set; }
    public bool Stacked { get; private set; }
    public string SurfaceName { get; private set; } = Material.Default.Name;

    public Quaternion Rot = Quaternion.Identity;
    public Vector3 Vel, AngVel;
    public readonly float[] Compression = new float[4];   // wheel up from rest
    public readonly float[] WheelSpin = new float[4];
    public readonly float[] SteerAngle = new float[4];    // as a rotation of the wheel model
    public readonly bool[] Touching = new bool[4];
    public float Throttle, Steer;
    public float Time;

    Vector3 centre, shift, impulse, angImpulse, lastDeltaV;
    float steerNow, engineNow, angRes, overlap, noContactTime;
    bool righting;
    Vector3 rightPos;
    Quaternion rightRot;
    int wheelContacts, bodyContacts, worldContacts, wheelFloorContacts;
    float noMoveTime;
    Vector3 movedBy;
    readonly List<CollPoly> nearby = new();
    readonly List<BodyHit> carHits = new();

    readonly float mass, invMass, topSpeed;
    readonly Matrix4x4 bodyInvInertia;
    readonly Vector3 comShift;
    readonly (Vector3 Pos, float Radius)[] spheres;
    readonly Vector3[] oldSphere;
    readonly Wheel[] wheels = new Wheel[4];

    sealed class Wheel
    {
        public WheelData Data = null!;
        public SpringData Spring = null!;
        public Vector3 Mount, Hub;        // on the car at rest: Offset1, and with Offset2 where the sphere sits
        public float Radius, InvMass, InvInertia, SpinImpulse;
        public float Travel, TravelVel, Spin, Angle;
        public Vector3 CentrePos, OldCentrePos, Right, Look;
        public bool Contact, Spinning;
    }

    sealed class WheelHit
    {
        public Wheel W = null!;
        public Vector3 Normal, Rel, Vel, World;
        public float Depth;
        public Material Mat = Material.Default;
        public CollPoly Poly = null!;
    }

    sealed class BodyHit
    {
        public Vector3 Normal, Rel, Vel, World;
        public float Depth, Static, Kinetic, Restitution, OtherInvMass;
    }

    public CarSim(CarData car, Hull? hull = null)
    {
        Car = car;
        mass = car.BodyMass > 0 ? (float)car.BodyMass : 1;
        invMass = 1 / mass;
        topSpeed = Math.Max((float)car.TopSpeed * MphToUnits, 1);
        comShift = car.CoM;

        var inertia = new Matrix4x4(
            car.Inertia[0].X, car.Inertia[0].Y, car.Inertia[0].Z, 0,
            car.Inertia[1].X, car.Inertia[1].Y, car.Inertia[1].Z, 0,
            car.Inertia[2].X, car.Inertia[2].Y, car.Inertia[2].Z, 0,
            0, 0, 0, 1);
        if (!Matrix4x4.Invert(inertia, out bodyInvInertia) || car.Inertia[0].X <= 0)
            bodyInvInertia = Matrix4x4.CreateScale(1 / 1000f);

        for (int i = 0; i < 4; i++)
        {
            var d = car.Wheels[i];
            float r = (float)Math.Max(d.Radius, 1), m = d.Mass > 0 ? (float)d.Mass : 0.1f;
            wheels[i] = new Wheel
            {
                Data = d,
                Spring = car.Springs[i],
                Mount = d.Offset1 + comShift,
                Hub = d.Offset1 + comShift + d.Offset2,
                Radius = r,
                InvMass = 1 / m,
                InvInertia = 1 / (m * r * r / 2),
                SpinImpulse = m * 15000f * r
            };
        }

        spheres = hull is { Spheres.Count: > 0 } ? hull.Spheres.Select(s => (s.Pos + comShift, s.Radius)).ToArray() : BoxSpheres();
        oldSphere = new Vector3[spheres.Length];
        Reset();
    }

    // A car without a hull still needs something to land on
    (Vector3, float)[] BoxSpheres()
    {
        var present = wheels.Where(w => w.Data.Present).ToArray();
        float x = present.Length > 0 ? present.Max(w => Math.Abs(w.Mount.X)) : 30;
        float z = present.Length > 0 ? present.Max(w => Math.Abs(w.Mount.Z)) : 60;
        const float R = 8;
        return new[] { (new Vector3(-x, -14, -z), R), (new Vector3(x, -14, -z), R), (new Vector3(-x, -14, z), R), (new Vector3(x, -14, z), R),
                       (new Vector3(-x, 0, -z), R), (new Vector3(x, 0, -z), R), (new Vector3(-x, 0, z), R), (new Vector3(x, 0, z), R) };
    }

    public void Reset()
    {
        Rot = Quaternion.Identity;
        Vel = AngVel = shift = impulse = angImpulse = lastDeltaV = Vector3.Zero;
        Time = overlap = noContactTime = noMoveTime = 0;
        Stacked = false;
        movedBy = Vector3.Zero;
        carHits.Clear();
        steerNow = engineNow = 0;
        angRes = (float)Car.AngRes;
        righting = false;
        float low = 0;
        bool any = false;
        foreach (var w in wheels)
        {
            w.Travel = w.TravelVel = w.Spin = w.Angle = 0;
            w.Contact = w.Spinning = false;
            if (!w.Data.Present) continue;
            low = any ? Math.Max(low, w.Hub.Y + w.Radius) : w.Hub.Y + w.Radius;
            any = true;
        }
        centre = new Vector3(0, any ? -low : -20, 0);
        foreach (var w in wheels) PlaceWheel(w);
        for (int i = 0; i < spheres.Length; i++) oldSphere[i] = SphereWorld(i);
    }

    /// <summary>
    /// The game's self-righting: only when the car is nearly upside down and touching something. It lifts and turns
    /// over a moment, facing the way it was. False means it didn't apply
    /// </summary>
    public bool Flip()
    {
        if (righting) return true;
        if (Vector3.Transform(Vector3.UnitY, Rot).Y > 0.3f) return false;
        if (wheelContacts == 0 && bodyContacts == 0 && noContactTime >= 0.1f) return false;
        var look = Vector3.Transform(Vector3.UnitZ, Rot);
        look.Y = 0;
        var yaw = look.LengthSquared() > 1e-6f ? MathF.Atan2(look.X, look.Z) : MathF.PI / 2;
        rightRot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        rightPos = centre + new Vector3(0, -50, 0);
        righting = true;
        return true;
    }

    public bool Righting => righting;

    public void StepOnce() => Step();
    public float Mass => mass;
    public float SteerNow => steerNow;
    public Vector3 Centre => centre;

    /// <summary>How far the steering wheels can turn, in radians</summary>
    public float SteerLock => wheels.Where(w => w.Data.Present && w.Data.Turnable).Select(w => Math.Abs((float)w.Data.SteerRatio)).DefaultIfEmpty(0).Max();

    /// <summary>Put the car on the floor here, facing this way, as new</summary>
    public void Place(Vector3 at, float yaw)
    {
        var ground = Ground;
        Reset();
        Rot = Quaternion.CreateFromAxisAngle(Vector3.UnitY, yaw);
        var origin = Vector3.Transform(comShift, Rot);
        centre = new Vector3(at.X + origin.X, centre.Y + GroundY(at.X, at.Z), at.Z + origin.Z);
        foreach (var w in wheels) { PlaceWheel(w); w.OldCentrePos = w.CentrePos; }
        for (int i = 0; i < spheres.Length; i++) oldSphere[i] = SphereWorld(i);
        Ground = ground;
    }

    /// <summary>A push from outside, at a point in the world. It works on the next step</summary>
    public void AddImpulse(Vector3 push, Vector3 at)
    {
        impulse += push;
        angImpulse += Vector3.Cross(at - centre, push);
    }

    /// <summary>Move out of something without changing speed</summary>
    public void Push(Vector3 by) => shift = AddShift(shift, by);

    /// <summary>Sideways speed of the front and rear axles, what the game's CPU driver watches</summary>
    public (float Front, float Rear) SideSlip()
    {
        float At(Wheel w) => Vector3.Dot(w.Right, Vel + Vector3.Cross(AngVel, w.CentrePos - centre));
        return (At(wheels[0]), At(wheels[2]));
    }

    public float GroundY(float x, float z) => GroundFunc != null ? GroundFunc(x, z) : Ground switch
    {
        SimGround.Bumps => 6f * MathF.Sin(x * 0.012f) * MathF.Sin(z * 0.009f),
        SimGround.Ramp => z > 800 && z < 1400 ? -(z - 800) * 0.18f : 0,
        _ => 0
    };

    CollWorld ActiveWorld
    {
        get
        {
            if (World != null) return World;
            if (Ground == SimGround.Bumps) return World = new CollWorld(GroundY, 30);
            World = CollWorld.Flat();
            if (Ground == SimGround.Ramp)
            {
                const float From = 800, To = 1400, Top = (To - From) * 0.18f, Wide = 20000;
                World.Add(CollPoly.Make(new[] { new Vector3(-Wide, 0, From), new Vector3(Wide, 0, From), new Vector3(Wide, -Top, To), new Vector3(-Wide, -Top, To) }, CollWorld.Up));
                World.Add(CollPoly.Make(new[] { new Vector3(-Wide, 0, To), new Vector3(Wide, 0, To), new Vector3(Wide, -Top, To), new Vector3(-Wide, -Top, To) }, Vector3.UnitZ));
            }
            return World;
        }
    }

    /// <summary>The car's model origin in the world</summary>
    public Vector3 Pos
    {
        get => centre + Vector3.Transform(-comShift, Rot);
        set => centre += value - Pos;
    }

    public float SpeedForward => Vector3.Dot(Vel, Vector3.Transform(Vector3.UnitZ, Rot));
    public float SpeedMph => Vel.Length() / MphToUnits;
    public Vector3 Up => Vector3.Transform(-Vector3.UnitY, Rot);

    public Matrix4x4 Matrix => Matrix4x4.CreateTranslation(comShift) * Matrix4x4.CreateFromQuaternion(Rot) * Matrix4x4.CreateTranslation(centre);

    /// <summary>Move forward by real seconds</summary>
    public void Advance(float seconds)
    {
        overlap += seconds;
        int steps = Math.Min((int)(overlap / Dt), 10);
        overlap = steps == 10 ? 0 : overlap - steps * Dt;
        for (int i = 0; i < steps; i++) Step();
    }

    void Step()
    {
        StepMove();
        StepCollide();
    }

    /// <summary>First half of a step: controls and movement. A race moves all cars, then finds who touches, then collides</summary>
    public void StepMove()
    {
        UpdateControls();
        if (righting) MoveRighting();
        else
        {
            CheckStacked();
            MoveBody();
        }
        foreach (var w in wheels) if (w.Data.Present) MoveWheel(w, !righting);
    }

    public void StepCollide()
    {
        Collide();
        Time += Dt;
    }

    // At rest on its body with no wheel on the floor and nothing asked of it, a car is held still. Not on its wheels
    void CheckStacked()
    {
        const float SlowVel = 20, SlowTurn = 0.5f;
        bool slow = Math.Abs(Vel.X) < SlowVel && Math.Abs(Vel.Y) < SlowVel && Math.Abs(Vel.Z) < SlowVel &&
                    Math.Abs(AngVel.X) < SlowTurn && Math.Abs(AngVel.Y) < SlowTurn && Math.Abs(AngVel.Z) < SlowTurn;
        noMoveTime = slow ? noMoveTime + Dt : 0;
        Stacked = engineNow == 0 && steerNow == 0 && Throttle == 0 && noMoveTime > 0.1f && wheelFloorContacts == 0 &&
                  carHits.Count == 0 && (bodyContacts > 1 || Stacked);
        if (!Stacked) return;
        impulse = angImpulse = Vel = AngVel = shift = Vector3.Zero;
        foreach (var w in wheels) w.TravelVel = 0;
    }

    static int Sign(float v) => v == 0 ? 0 : v < 0 ? -1 : 1;

    static float Approach(float now, float target, float step) =>
        target > now ? Math.Min(now + step, target) : Math.Max(now - step, target);

    void UpdateControls()
    {
        float dest = Math.Clamp(Steer, -1, 1);
        float step = (float)Car.SteerRate * Dt;
        // Straightening is much quicker than turning in
        step *= dest == 0 || Sign(dest) != Sign(steerNow) ? 4 : 0.5f;
        steerNow = Approach(steerNow, dest, step);

        dest = Math.Clamp(Throttle, -1, 1);
        if (dest * engineNow < 0) engineNow = 0;
        engineNow = Approach(engineNow, dest, (float)Car.EngineRate * Dt);
    }

    void MoveBody()
    {
        movedBy = shift;
        centre += shift;
        shift = Vector3.Zero;
        var before = Vel;
        Vel += impulse * invMass;
        Vel *= 1 - (float)Car.Resistance * FrictionScale * Dt;
        centre += Vel * Dt;
        lastDeltaV = Vel - before;
        impulse = Vector3.Zero;
        Turn();
        noContactTime = worldContacts == 0 ? noContactTime + Dt : 0;
    }

    void Turn()
    {
        AngVel += WorldInvInertia(angImpulse);
        angImpulse = Vector3.Zero;
        float keep = 1 - angRes * Dt * FrictionScale;
        AngVel = keep > 0 ? AngVel * keep : Vector3.Zero;
        var spin = AngVel * Dt;
        if (spin.LengthSquared() > 1e-12f)
            Rot = Quaternion.Normalize(Quaternion.CreateFromAxisAngle(Vector3.Normalize(spin), spin.Length()) * Rot);
    }

    void MoveRighting()
    {
        centre += shift;
        movedBy = shift = impulse = angImpulse = Vel = AngVel = lastDeltaV = Vector3.Zero;
        centre += (rightPos - centre) * (Dt * 10);
        Rot = Quaternion.Normalize(Quaternion.Slerp(Rot, rightRot, Dt * 8));
        if (Math.Abs(Quaternion.Dot(Rot, rightRot)) > 0.9999f) righting = false;
    }

    Vector3 WorldInvInertia(Vector3 v)
    {
        var local = Vector3.Transform(v, Quaternion.Inverse(Rot));
        return Vector3.Transform(Vector3.TransformNormal(local, bodyInvInertia), Rot);
    }

    Vector3 Ahead(Vector3 local) => centre + Vector3.Transform(local, Rot);

    static float SpringForce(SpringData s, float travel, float vel)
    {
        float f = -(float)s.Stiffness * travel - (float)s.Damping * vel;
        return Sign(f) == Sign(travel) ? 0 : f;
    }

    void MoveWheel(Wheel w, bool bodyMoved)
    {
        var d = w.Data;
        var down = Vector3.Transform(Vector3.UnitY, Rot);
        float angImp = 0;
        if ((d.Powered && !w.Contact) || (w.Spinning && w.Contact))
            angImp = engineNow * w.SpinImpulse * Dt * (d.EngineRatio < 0 ? -1 : 1);

        // The wheel stays behind when the body accelerates along its travel, and is thrown out when the car spins
        if (bodyMoved)
        {
            var arm = Ahead(w.Mount + new Vector3(0, w.Travel, 0)) - centre;
            var outward = Vector3.Cross(AngVel, Vector3.Cross(arm, AngVel));
            w.TravelVel += Dt * Vector3.Dot(outward, down);
            w.TravelVel -= Vector3.Dot(lastDeltaV, down);
        }

        w.Spin += w.InvInertia * angImp;
        w.Spin *= 1 - FrictionScale * Dt * (float)d.AxleFriction;
        w.Travel = Math.Clamp(w.Travel + w.TravelVel * Dt, -(float)d.MaxPos, (float)d.MaxPos);
        w.Angle += w.Spin * Dt;

        float kick = w.InvMass * Dt * SpringForce(w.Spring, w.Travel, w.TravelVel);
        w.TravelVel += w.Contact ? kick / 2 : kick;

        // A shift out of the ground isn't movement: where it was comes along
        w.CentrePos += movedBy;
        PlaceWheel(w);
    }

    void PlaceWheel(Wheel w)
    {
        w.OldCentrePos = w.CentrePos;
        w.CentrePos = Ahead(w.Hub + new Vector3(0, w.Travel, 0));
        int index = Array.IndexOf(wheels, w);
        // Each axle turns as one, by its left wheel
        var lead = wheels[index & ~1];
        float a = lead.Data.Turnable ? steerNow * (float)lead.Data.SteerRatio : 0;
        w.Right = Vector3.Transform(new Vector3(MathF.Cos(a), 0, MathF.Sin(a)), Rot);
        w.Look = Vector3.Transform(new Vector3(-MathF.Sin(a), 0, MathF.Cos(a)), Rot);
        SteerAngle[index] = -a;
        Compression[index] = -w.Travel;
        WheelSpin[index] = w.Angle;
    }

    // Keep the biggest push on each axis: pushes the same way don't add up
    static Vector3 AddShift(Vector3 total, Vector3 push)
    {
        static float One(float t, float p)
        {
            if (t == 0) return p;
            if (Sign(t) != Sign(p)) return t + p;
            return t > 0 ? Math.Max(t, p) : Math.Min(t, p);
        }
        return new Vector3(One(total.X, push.X), One(total.Y, push.Y), One(total.Z, push.Z));
    }

    void Collide()
    {
        var down = Vector3.Transform(Vector3.UnitY, Rot);
        var world = ActiveWorld;
        TouchesWall = false;

        // ---- Wheels against polygons ----
        var hits = new List<WheelHit>(8);
        foreach (var w in wheels)
        {
            w.Contact = w.Spinning = false;
            if (!w.Data.Present) continue;
            var r = new Vector3(w.Radius);
            world.Query(Vector3.Min(w.OldCentrePos, w.CentrePos) - r, Vector3.Max(w.OldCentrePos, w.CentrePos) + r, nearby);
            foreach (var poly in nearby)
            {
                if (!poly.Touch(w.OldCentrePos, w.CentrePos, w.Radius, out var n, out var toContact, out var point, out var depth)) continue;
                var rel = toContact + w.CentrePos - centre;
                var vel = Vel + Vector3.Cross(AngVel, rel);
                // Already leaving the surface
                if (Vector3.Dot(vel + w.TravelVel * down, poly.Normal) > 0) continue;
                var mat = poly.Mat;
                if (mat.Bump > 0) depth += mat.Bump * MathF.Cos(MathF.Tau * point.X / mat.BumpLength) * MathF.Cos(MathF.Tau * point.Z / mat.BumpLength);
                hits.Insert(0, new WheelHit { W = w, Normal = n, Rel = rel, Vel = vel - mat.Moves, World = point, Depth = depth, Mat = mat, Poly = poly });
            }
        }
        wheelContacts = hits.Count;
        if (hits.Count > 0) SurfaceName = hits[0].Mat.Name;

        // Close contacts of a wheel count as one, the deepest, facing the way its polygon does. Then what each wheel
        // needs to get out of the ground adds up
        var wheelShift = new Vector3[4];
        for (int a = 0; a < hits.Count; a++)
        {
            bool kept = true;
            for (int b = a + 1; b < hits.Count; b++)
            {
                if (hits[a].W != hits[b].W) continue;
                if (Math.Abs(hits[a].Rel.Y - hits[b].Rel.Y) >= 3 && Math.Abs(hits[a].World.Y - hits[b].World.Y) >= 3) continue;
                if (hits[a].Depth > hits[b].Depth)
                {
                    hits[b].Normal = hits[b].Poly.Normal;
                    hits.RemoveAt(a);
                    a--;
                    kept = false;
                    break;
                }
                hits[a].Normal = hits[a].Poly.Normal;
                hits.RemoveAt(b);
                b--;
            }
            if (kept && hits[a].Depth < 0)
                wheelShift[Array.IndexOf(wheels, hits[a].W)] += -hits[a].Depth * hits[a].Normal;
        }
        if (!Stacked)
            for (int i = 0; i < 4; i++)
            {
                var w = wheels[i];
                float along = Vector3.Dot(wheelShift[i], down);
                float limit = (float)w.Data.MaxPos;
                float travel = w.Travel + along;
                if (travel > limit) { along -= travel - limit; travel = limit; }
                else if (travel < -limit) { along -= travel + limit; travel = -limit; }
                w.Travel = travel;
                shift = AddShift(shift, wheelShift[i] - along * down);
            }

        var total = Vector3.Zero;
        var totalTurn = Vector3.Zero;
        wheelFloorContacts = 0;
        foreach (var h in hits)
        {
            var imp = TyreImpulse(h);
            total += imp;
            totalTurn += Vector3.Cross(h.Rel, imp);
            if (down.Y > 0 && down.Y * h.Normal.Y < -0.15f) wheelFloorContacts++;
            if (Math.Abs(h.Normal.Y) < 0.5f) TouchesWall = true;
        }
        angImpulse += totalTurn;
        impulse += total;

        for (int i = 0; i < 4; i++) Touching[i] = wheels[i].Contact;

        // ---- Lift on two wheels: speed pushes a car on one side back down ----
        bool l = wheels[0].Contact && wheels[2].Contact && !wheels[1].Contact && !wheels[3].Contact;
        bool r2 = wheels[1].Contact && wheels[3].Contact && !wheels[0].Contact && !wheels[2].Contact;
        if (l || r2)
        {
            float tilt = Math.Min(1.5f - Math.Abs(down.Y), 1);
            impulse += (float)Car.DownForceMod * Dt * tilt * Vector3.Dot(Vel, Vector3.Transform(Vector3.UnitZ, Rot)) * down;
        }

        // ---- Gravity, then the body's hull spheres ----
        impulse += new Vector3(0, mass * Gravity * Dt, 0);
        BodyCollisions(world);

        // Spinning in the air slows down differently
        angRes = wheels.Any(w => w.Contact) ? (float)Car.AngRes : (float)(Car.ResMod * Car.AngRes);
    }

    Vector3 TyreImpulse(WheelHit h)
    {
        var w = h.W;
        var d = w.Data;
        var down = Vector3.Transform(Vector3.UnitY, Rot);
        float timeScale = FrictionScale * Dt;
        float velN = Vector3.Dot(h.Vel, h.Normal);
        float downN = Vector3.Dot(h.Normal, down);

        // A wheel pushed into the ground travels with it
        if (h.Depth < 0)
        {
            w.TravelVel = -downN * velN;
            w.TravelVel += Gravity * Dt * down.Y;
        }

        var look = Vector3.Cross(h.Normal, w.Right);
        float lookLen = look.Length();
        float wallGrip = 1;
        if (lookLen > 0.7f) look /= lookLen;
        else if (Math.Abs(h.Normal.Y) <= 0.5f) wallGrip = 0.1f + lookLen / 4;
        w.Contact = true;

        // The bump: what stops this point moving into the ground, kept by the damper's Restitution
        float normal = 0;
        if (downN < 0.9f)
        {
            normal = -velN / ImpulseResponse(h.Rel, h.Normal);
            normal += (float)w.Spring.Restitution * (normal * downN * downN);
        }

        // The spring, when it's the compressed side that touches
        float springImp = 0;
        float hubSide = Vector3.Dot(w.CentrePos - centre - h.Rel, down);
        if (Sign(w.Travel) == Sign(hubSide))
        {
            springImp = Dt * SpringForce(w.Spring, w.Travel, w.TravelVel) * downN;
            normal -= springImp;
        }
        if (normal < 0) return Vector3.Zero;

        // Sliding: grip works from the spring's load, not from the whole impulse
        var slide = h.Vel - velN * h.Normal;
        slide -= Vector3.Dot(look, slide) * look;
        var tangent = (float)d.Grip * h.Mat.Grip * Math.Abs(springImp) * timeScale * -0.35f * slide;

        // Engine: full at rest, nothing left at TopSpeed
        float torque = 0;
        if (d.Powered)
        {
            torque = Dt * engineNow * (float)d.EngineRatio;
            float left = torque - Math.Abs(torque) * (w.Spin * w.Radius / topSpeed);
            torque = Sign(torque) != Sign(left) ? 0 : left;
        }

        // Rolling loss when not driving, or driving against the wheel
        if (Math.Abs(engineNow) < 0.01f || Sign(engineNow) == -Sign(w.Spin))
            torque -= (float)d.AxleFriction * w.Spin * w.Radius * timeScale;

        float staticF = (float)d.StaticFriction * h.Mat.Roughness, kinetic = (float)d.KineticFriction * h.Mat.Roughness;
        if (Math.Abs(torque) > Math.Abs(w.Radius * wallGrip * staticF * normal)) w.Spinning = true;

        tangent += torque / w.Radius * look;

        float len = tangent.Length();
        if (len == 0) len = 1;
        float cap = 4 * Dt * mass * Gravity;
        if (len > cap)
        {
            tangent *= cap / len;
            len = cap;
        }
        if (len > staticF * wallGrip * normal)
            tangent *= kinetic * wallGrip * normal / len;

        if (!w.Spinning)
            w.Spin += (Vector3.Dot(h.Vel, look) / w.Radius - w.Spin) * (timeScale / 4);

        var result = normal * h.Normal + tangent;
        if (Math.Abs(result.X) < SmallImpulse) result.X = 0;
        if (Math.Abs(result.Y) < SmallImpulse) result.Y = 0;
        if (Math.Abs(result.Z) < SmallImpulse) result.Z = 0;
        return result;
    }

    // Speed along the normal per unit of impulse there
    float ImpulseResponse(Vector3 rel, Vector3 normal) =>
        invMass + Vector3.Dot(Vector3.Cross(WorldInvInertia(Vector3.Cross(rel, normal)), rel), normal);

    Vector3 SphereWorld(int i) => Ahead(spheres[i].Pos);

    /// <summary>
    /// The sphere that stands for this car against other cars in Arcade mode: one or two from its size, through its centre
    /// </summary>
    public IEnumerable<(Vector3 Pos, float Radius)> CarSpheres()
    {
        if (carSpheres == null)
        {
            var min = new Vector3(float.MaxValue);
            var max = new Vector3(float.MinValue);
            void Grow(Vector3 p, float r) { min = Vector3.Min(min, p - new Vector3(r)); max = Vector3.Max(max, p + new Vector3(r)); }
            foreach (var w in wheels) if (w.Data.Present) Grow(w.Hub, w.Radius);
            foreach (var (p, r) in spheres) Grow(p, r);
            float y = (min.Y + max.Y) / 2, wide = max.X - min.X, longest = max.Z - min.Z;
            if (wide > 0 && longest / wide > 1.2f)
            {
                float r = 1.2f * wide / 2;
                carSpheres = new[] { (new Vector3(0, y, max.Z - r), r), (new Vector3(0, y, min.Z + r), r) };
            }
            else carSpheres = new[] { (new Vector3(0, y, (max.Z + min.Z) / 2), 1.4f * longest / 2) };
        }
        return carSpheres.Select(s => (Ahead(s.Item1), s.Item2));
    }
    (Vector3, float)[]? carSpheres;

    /// <summary>Another car touches this one, through the centres. Normal pushes this car away, depth is below zero</summary>
    public void AddCarContact(Vector3 normal, float depth, Vector3 closing, float otherInvMass)
    {
        Stacked = false;
        carHits.Add(new BodyHit { Normal = normal, Depth = depth, Vel = closing, OtherInvMass = otherInvMass, World = centre });
    }

    public float InvMass => invMass;

    void BodyCollisions(CollWorld world)
    {
        var hits = new List<BodyHit>(spheres.Length + carHits.Count);
        for (int i = 0; i < spheres.Length; i++)
        {
            var at = SphereWorld(i);
            float radius = spheres[i].Radius;
            var old = oldSphere[i];
            oldSphere[i] = at;
            var reach = new Vector3(radius + 2);
            world.Query(Vector3.Min(old, at) - reach, Vector3.Max(old, at) + reach, nearby);
            foreach (var poly in nearby)
            {
                if (!poly.Touch(old, at, radius, out _, out var toContact, out var point, out var depth)) continue;
                var rel = toContact + at - centre;
                var vel = Vel + Vector3.Cross(AngVel, rel);
                if (Vector3.Dot(vel, poly.Normal) >= 0) continue;
                var hit = new BodyHit
                {
                    Normal = poly.Normal, Rel = rel, Vel = vel - poly.Mat.Moves, World = point, Depth = depth,
                    Static = (float)Car.BodyStatic * poly.Mat.Roughness, Kinetic = (float)Car.BodyKinetic * poly.Mat.Roughness,
                    Restitution = (float)Car.Hardness * poly.Mat.Hardness
                };
                // Walls: it slides along and comes off a little harder
                if (Math.Abs(poly.Normal.Y) < 0.15f)
                {
                    hit.Static *= 0.1f;
                    hit.Kinetic *= 0.1f;
                    hit.Restitution += 0.1f;
                    TouchesWall = true;
                }
                hits.Insert(0, hit);
            }
        }
        worldContacts = hits.Count;
        hits.InsertRange(0, carHits);
        carHits.Clear();

        // What the shift already clears isn't a contact any more
        if (shift.X != 0 && shift.Y != 0 && shift.Z != 0)
            hits.RemoveAll(h => Vector3.Dot(shift, h.Normal) > -h.Depth);

        // Points close together count as one: the deepest
        for (int a = 0; a < hits.Count; a++)
        {
            if (hits[a].Depth < 0)
            {
                float share = invMass / (invMass + hits[a].OtherInvMass);
                shift = AddShift(shift, -share * hits[a].Depth * hits[a].Normal);
            }
            for (int b = a + 1; b < hits.Count; b++)
            {
                var dr = hits[a].Rel - hits[b].Rel;
                var dw = hits[a].World - hits[b].World;
                bool close = (Math.Abs(dr.X) < 30 && Math.Abs(dr.Y) < 30 && Math.Abs(dr.Z) < 30) ||
                             (Math.Abs(dw.Y) < 2 && Math.Abs(dw.X) < 30 && Math.Abs(dw.Z) < 30);
                if (!close) continue;
                if (hits[a].Depth < hits[b].Depth) { hits.RemoveAt(b); b--; }
                else { hits.RemoveAt(a); a--; break; }
            }
        }

        bodyContacts = hits.Count;
        if (hits.Count == 0) return;

        // All points answer together, each one's push changes the others' speed
        int count = hits.Count;
        var matrix = new float[count, count];
        var rhs = new float[count];
        for (int i = 0; i < count; i++)
        {
            var hi = hits[i];
            for (int j = 0; j < count; j++)
            {
                var hj = hits[j];
                var change = invMass * hj.Normal + Vector3.Cross(WorldInvInertia(Vector3.Cross(hj.Rel, hj.Normal)), hi.Rel);
                if (i == j) change += hj.OtherInvMass * hj.Normal;
                matrix[i, j] = Vector3.Dot(hi.Normal, change);
            }
            float wanted = -(1 + hi.Restitution) * Vector3.Dot(hi.Vel, hi.Normal);
            var already = WorldInvInertia(Vector3.Cross(angImpulse, hi.Rel)) + invMass * impulse;
            float have = Vector3.Dot(hi.Normal, already);
            rhs[i] = have < wanted ? wanted - have : 0;
        }
        var solved = count == 1 ? new[] { rhs[0] / matrix[0, 0] } : Solve(matrix, rhs);
        if (solved == null) return;

        var total = Vector3.Zero;
        var turn = Vector3.Zero;
        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            var imp = solved[i] * h.Normal;
            if (solved[i] >= 0) imp += BodyFriction(h, solved[i]);
            total += imp;
            turn += Vector3.Cross(h.Rel, imp);
        }
        angImpulse += turn;
        impulse += total;
    }

    Vector3 BodyFriction(BodyHit h, float normal)
    {
        var slide = h.Vel - Vector3.Dot(h.Vel, h.Normal) * h.Normal;
        float k = (float)Car.BodyGrip * normal * Dt * FrictionScale;
        var tan = -k * slide;
        float len = k * slide.Length();
        float cap = 0.5f * Dt * mass * Gravity;
        if (len > cap)
        {
            tan *= cap / len;
            len = cap;
        }
        if (len > h.Static * normal && len > 0)
            tan *= h.Kinetic * normal / len;
        return tan;
    }

    // Conjugate gradients. Null if it didn't settle: the impulses of this step are then skipped
    static float[]? Solve(float[,] a, float[] b)
    {
        int n = b.Length;
        var x = new float[n];
        var r = (float[])b.Clone();
        var p = (float[])b.Clone();
        var ap = new float[n];
        float rr = r.Sum(v => v * v);
        for (int it = 0; it < 2 * n && rr > 1e-6f; it++)
        {
            float pap = 0;
            for (int i = 0; i < n; i++)
            {
                ap[i] = 0;
                for (int j = 0; j < n; j++) ap[i] += a[i, j] * p[j];
                pap += p[i] * ap[i];
            }
            if (Math.Abs(pap) < 1e-12f) break;
            float alpha = rr / pap, next = 0;
            for (int i = 0; i < n; i++)
            {
                x[i] += alpha * p[i];
                r[i] -= alpha * ap[i];
                next += r[i] * r[i];
            }
            for (int i = 0; i < n; i++) p[i] = r[i] + next / rr * p[i];
            rr = next;
        }
        return MathF.Sqrt(rr) > 1 ? null : x;
    }

    // ---------------------------- Auto runs ----------------------------

    public sealed record RunResult(string Name, float TopEnd, float Acc, float TopMph, string Note);

    /// <summary>Full throttle: top speed reached and the time to get there, what TopEnd and Acc mean</summary>
    public static RunResult RunAcceleration(CarData car, Hull? hull = null, float maxTime = 40)
    {
        var s = new CarSim(car, hull);
        for (int i = 0; i < 60; i++) s.Step();
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
        // The code doesn't say how Acc was measured: 99.5% of top speed is what agrees best with the stock cars
        foreach (var (t, v) in samples) if (v >= top * 0.995f) { reach = t; break; }
        return new RunResult("Acceleration", top, reach, top / MphToUnits, $"0 to {top / MphToUnits:0.0} mph in {reach:0.0} s");
    }

    /// <summary>Steady full lock circle at some speed: how much it grips, whether it rolls over</summary>
    public static RunResult RunCircle(CarData car, Hull? hull = null)
    {
        var s = new CarSim(car, hull) { Throttle = 0.6f };
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
    public static RunResult RunBumps(CarData car, Hull? hull = null)
    {
        var s = new CarSim(car, hull) { Ground = SimGround.Bumps, Throttle = 0.7f };
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
    public static void FillStats(CarData car, Hull? hull = null)
    {
        var a = RunAcceleration(car, hull);
        car.TopEnd = a.TopEnd;
        car.Acc = a.Acc;
        car.Weight = car.BodyMass;
        var powered = Enumerable.Range(0, 4).Where(i => car.Wheels[i].Powered).ToArray();
        car.Trans = powered.Length == 4 ? 0 : powered.Length > 0 && powered[0] < 2 ? 1 : 2;
        car.Statistics = true;
    }
}
