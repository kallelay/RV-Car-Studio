using System.Numerics;

namespace RvStudio.Core;

public enum RaceGround { Plain, Bumpy, Ramp, Materials }

/// <summary>A built-in closed road, walled on both sides</summary>
public sealed class RaceTrack
{
    public static readonly string[] Names = { "Oval", "Park", "Hairpins" };
    /// <summary>What the list offers. Materials isn't in it</summary>
    public static readonly string[] GroundNames = { "Plain", "Bumpy", "Ramp" };

    public string Name = "";
    public RaceGround Ground;
    /// <summary>The road, floor and walls as collision polygons</summary>
    public CollWorld World = CollWorld.Flat();
    public int[] Band = Array.Empty<int>();
    readonly Dictionary<(int, int), int> bandAt = new();
    const float BandCell = 150;
    public Vector3[] Points = Array.Empty<Vector3>();
    public Vector3[] Tangents = Array.Empty<Vector3>();
    public float[] Distance = Array.Empty<float>();
    public float[] Radius = Array.Empty<float>();
    public float Length, Width = 820;
    const float Spacing = 90, Scale = 1.3f;

    // The jump, straight ahead after the start on every road: a slope that ends in a drop
    const float RampFrom = 500, RampTo = 1050, RampSlope = 0.13f;

    public float Height(float x, float z) => Ground switch
    {
        RaceGround.Bumpy => 7f * MathF.Sin(x * 0.012f) * MathF.Sin(z * 0.009f),
        RaceGround.Ramp when Math.Abs(x) < Width && z > RampFrom && z < RampTo => -(z - RampFrom) * RampSlope,
        _ => 0
    };

    void BuildWorld()
    {
        int n = Points.Length;
        Vector3 At(int i, float off, float y = 0)
        {
            var p = Points[i % n] + Side(Tangents[i % n]) * off;
            p.Y = y;
            return p;
        }
        float h = Width / 2;
        switch (Ground)
        {
            case RaceGround.Bumpy:
                World = new CollWorld(Height, 30);
                break;
            case RaceGround.Materials:
                // Each band is its own road, just over the floor: where both are near, the deepest contact wins
                World = CollWorld.Flat(null, 1);
                for (int i = 0; i < n; i++)
                    World.Add(CollPoly.Make(new[] { At(i, -h), At(i + 1, -h), At(i + 1, h), At(i, h) }, CollWorld.Up, Material.Track[Band[i]]));
                break;
            default:
                World = CollWorld.Flat();
                break;
        }
        if (Ground == RaceGround.Ramp)
        {
            float top = (RampTo - RampFrom) * RampSlope;
            World.Add(CollPoly.Make(new[] { new Vector3(-Width, 0, RampFrom), new Vector3(Width, 0, RampFrom), new Vector3(Width, -top, RampTo), new Vector3(-Width, -top, RampTo) }, CollWorld.Up));
            World.Add(CollPoly.Make(new[] { new Vector3(-Width, 0, RampTo), new Vector3(Width, 0, RampTo), new Vector3(Width, -top, RampTo), new Vector3(-Width, -top, RampTo) }, Vector3.UnitZ));
        }
        // Walls, facing the road and tall enough for the jump
        const float Tall = 400;
        for (int i = 0; i < n; i++)
            foreach (var (off, sign) in new[] { (h, -1f), (-h, 1f) })
            {
                var inward = Side(Tangents[i]) * sign;
                World.Add(CollPoly.Make(new[] { At(i, off, 20), At(i + 1, off, 20), At(i + 1, off, -Tall), At(i, off, -Tall) }, inward, Material.Game[8]));
            }
    }

    public Material SurfaceAt(float x, float z)
    {
        if (Ground != RaceGround.Materials) return Material.Default;
        return bandAt.TryGetValue(((int)MathF.Floor(x / BandCell), (int)MathF.Floor(z / BandCell)), out var b) ? Material.Track[b] : Material.Default;
    }

    void BuildBands()
    {
        int n = Points.Length, count = Material.Track.Length;
        Band = new int[n];
        for (int i = 0; i < n; i++)
        {
            Band[i] = Math.Min((int)(Distance[i] / Length * count), count - 1);
            var side = Side(Tangents[i]);
            for (float off = -Width * 0.6f; off <= Width * 0.6f; off += BandCell * 0.5f)
            {
                var p = Points[i] + side * off;
                bandAt[((int)MathF.Floor(p.X / BandCell), (int)MathF.Floor(p.Z / BandCell))] = Band[i];
            }
        }
    }

    public static RaceTrack Make(int which, RaceGround ground = RaceGround.Plain)
    {
        (float X, float Z)[] p = which switch
        {
            1 => new[] { (0f, -3000f), (0f, 3000f), (800f, 5200f), (2800f, 5600f), (4200f, 4000f), (3200f, 2000f), (4000f, 0f), (5600f, -1200f), (5000f, -3800f), (2600f, -4800f), (800f, -4400f) },
            2 => new[] { (0f, -3000f), (0f, 4000f), (1300f, 5400f), (2600f, 4000f), (2600f, -800f), (3900f, -2200f), (5200f, -800f), (5200f, 4400f), (6500f, 5800f), (7800f, 4400f), (7800f, -4000f), (6200f, -5600f), (1400f, -5600f) },
            _ => new[] { (0f, -4000f), (0f, 0f), (0f, 4000f), (700f, 5700f), (2200f, 6400f), (3700f, 5700f), (4400f, 4000f), (4400f, 0f), (4400f, -4000f), (3700f, -5700f), (2200f, -6400f), (700f, -5700f) }
        };
        var t = new RaceTrack { Name = Names[Math.Clamp(which, 0, Names.Length - 1)], Ground = ground };
        t.Build(p.Select(c => (c.X * Scale, c.Z * Scale)).ToArray());
        if (ground == RaceGround.Materials) t.BuildBands();
        t.BuildWorld();
        return t;
    }

    // A smooth curve through the points
    static Vector2 Curve(Vector2 a, Vector2 b, Vector2 c, Vector2 d, float t) =>
        0.5f * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * (t * t) + (3 * b - a - 3 * c + d) * (t * t * t));

    void Build((float X, float Z)[] controls)
    {
        Vector2 C(int i) { var c = controls[(i + controls.Length) % controls.Length]; return new Vector2(c.X, c.Z); }
        var fine = new List<Vector2>();
        for (int i = 0; i < controls.Length; i++)
            for (int s = 0; s < 40; s++)
                fine.Add(Curve(C(i - 1), C(i), C(i + 1), C(i + 2), s / 40f));

        // Evenly spaced points along the curve
        var even = new List<Vector2> { fine[0] };
        float since = 0;
        for (int i = 1; i <= fine.Count; i++)
        {
            var a = fine[i - 1];
            var b = fine[i % fine.Count];
            float seg = (b - a).Length();
            while (since + seg >= Spacing && seg > 0)
            {
                float k = (Spacing - since) / seg;
                a += (b - a) * k;
                seg = (b - a).Length();
                even.Add(a);
                since = 0;
            }
            since += seg;
        }
        // The last one would sit right on the first
        if ((even[^1] - even[0]).Length() < Spacing * 0.5f) even.RemoveAt(even.Count - 1);

        int n = even.Count;
        Points = even.Select(v => new Vector3(v.X, 0, v.Y)).ToArray();
        Tangents = new Vector3[n];
        Distance = new float[n];
        Radius = new float[n];
        for (int i = 0; i < n; i++)
        {
            Tangents[i] = Vector3.Normalize(Points[(i + 1) % n] - Points[(i - 1 + n) % n]);
            if (i > 0) Distance[i] = Distance[i - 1] + (Points[i] - Points[i - 1]).Length();
        }
        Length = Distance[^1] + (Points[0] - Points[^1]).Length();

        // Radius of each bend, smoothed over a car's length or two
        const int Reach = 4;
        for (int i = 0; i < n; i++)
        {
            var a = Tangents[(i - Reach + n) % n];
            var b = Tangents[(i + Reach) % n];
            float turn = MathF.Acos(Math.Clamp(Vector3.Dot(a, b), -1, 1));
            Radius[i] = turn > 1e-3f ? Reach * 2 * Spacing / turn : 1e5f;
        }
    }

    /// <summary>The nearest point of the road, looking only around a known one</summary>
    public int Nearest(Vector3 pos, int hint, int reach = 30)
    {
        int n = Points.Length, best = hint;
        float bestD = float.MaxValue;
        for (int k = -reach; k <= reach; k++)
        {
            int i = ((hint + k) % n + n) % n;
            var d = pos - Points[i];
            float dd = d.X * d.X + d.Z * d.Z;
            if (dd < bestD) { bestD = dd; best = i; }
        }
        return best;
    }

    public int Index(float distance)
    {
        distance = (distance % Length + Length) % Length;
        return Math.Clamp((int)(distance / Length * Points.Length), 0, Points.Length - 1);
    }

    public static Vector3 Side(Vector3 tangent) => new(tangent.Z, 0, -tangent.X);
}

public sealed class RaceCar
{
    public string Name = "", Folder = "";
    public CarData Data = null!;
    public Hull? Hull;
    public CarSim Sim = null!;
    public readonly CarDynamics Dyn = new();
    public bool IsPlayer;
    public float Skill = 1;

    public int Hint;
    public float Run;              // how far along the race, can start below zero on the grid
    public int Lap;
    public float LapStart, BestLap = float.PositiveInfinity, LastLap;
    public float FinishTime = float.PositiveInfinity;
    public bool Finished => !float.IsPositiveInfinity(FinishTime);
    public int Hits;
    internal bool OnWall;

    internal float[] Speeds = Array.Empty<float>();
    internal float Lane, StuckFor, UpsideFor, BackUp, BackSteer;
    public float HalfWidth, HalfLength;
}

/// <summary>
/// A race in the editor: all cars use the same physics as Drive here. The CPU driver is simpler than the game's, which
/// follows the AI nodes of a real track: it reads this road's bends and plans its speed. Steering and throttle
/// corrections for sliding work as the game's do, from the car's own AI values, so those can be seen at work.
/// Road and walls are collision polygons, and cars meet the way they do in Arcade mode.
/// </summary>
public sealed class Race
{
    public readonly RaceTrack Track;
    public readonly List<RaceCar> Cars = new();
    public readonly int Laps;
    public RaceCar Player = null!;
    public float PlayerThrottle, PlayerSteer;
    public bool AiDrivesPlayer;
    public float Time, Countdown = 3;
    float overlap;
    const float Dt = 0.008f;

    public bool Started => Countdown <= 0;
    public bool AllDone => Cars.All(c => c.Finished);
    public bool PlayerDone => Player.Finished;
    public IEnumerable<RaceCar> Standings =>
        Cars.OrderBy(c => c.FinishTime).ThenByDescending(c => c.Run);
    public int PlaceOf(RaceCar car) => Standings.ToList().IndexOf(car) + 1;

    public Race(RaceTrack track, int laps)
    {
        Track = track;
        Laps = Math.Max(laps, 1);
    }

    public RaceCar Add(string name, string folder, CarData data, Hull? hull, bool player, float skill = 1)
    {
        var car = new RaceCar { Name = name, Folder = folder, Data = data, Hull = hull, IsPlayer = player, Skill = skill, Sim = new CarSim(data, hull) };
        car.Sim.GroundFunc = Track.Height;
        car.Sim.World = Track.World;
        var wheels = data.Wheels.Where(w => w.Present).ToArray();
        car.HalfWidth = wheels.Length > 0 ? wheels.Max(w => Math.Abs(w.Offset1.X + w.Offset2.X) + (float)w.Radius * 0.4f) : 30;
        car.HalfLength = wheels.Length > 0 ? wheels.Max(w => Math.Abs(w.Offset1.Z) + (float)w.Radius) : 60;
        if (player) Player = car;
        Cars.Add(car);
        return car;
    }

    /// <summary>Line up on the grid behind the start, you at the back, and work out each driver's speeds</summary>
    public void Grid()
    {
        var order = Cars.OrderBy(c => c.IsPlayer ? 1 : 0).ToList();
        for (int i = 0; i < order.Count; i++)
        {
            var car = order[i];
            float back = 280 + (i / 2) * 340;
            float along = -back;
            int idx = Track.Index(along);
            var side = RaceTrack.Side(Track.Tangents[idx]);
            float lane = (i % 2 == 0 ? -1 : 1) * Track.Width * 0.2f;
            car.Sim.Place(Track.Points[idx] + side * lane, MathF.Atan2(Track.Tangents[idx].X, Track.Tangents[idx].Z));
            car.Hint = idx;
            car.Run = along;
            car.Lap = 0;
            car.Lane = lane;
            car.Speeds = PlanSpeeds(car);
        }
    }

    float[] PlanSpeeds(RaceCar car)
    {
        var d = car.Data;
        var wheels = d.Wheels.Where(w => w.Present).ToArray();
        float mu = wheels.Length > 0 ? (float)wheels.Average(w => Math.Min(w.StaticFriction, w.KineticFriction * 1.3)) : 1;
        // Downhill from the grip: stays a margin under it, keeps some for the throttle
        float corner = mu * CarCalc.Gravity * 0.62f * car.Skill;
        float brake = mu * CarCalc.Gravity * 0.42f * car.Skill;
        int n = Track.Points.Length;
        var v = new float[n];
        float top = Math.Max((float)d.TopSpeed * CarSim.MphToUnits, 300);
        for (int i = 0; i < n; i++) v[i] = Math.Min(top, MathF.Sqrt(corner * Rough(i) * Track.Radius[i]));
        for (int pass = 0; pass < 2; pass++)
            for (int k = n - 1; k >= 0; k--)
            {
                int next = (k + 1) % n;
                float ds = (Track.Points[next] - Track.Points[k]).Length();
                v[k] = Math.Min(v[k], MathF.Sqrt(v[next] * v[next] + 2 * brake * Rough(k) * ds));
            }
        return v;
    }

    // A driver knows the surfaces of the road ahead
    float Rough(int i) => Track.Band.Length > 0 ? Material.Track[Track.Band[i]].Roughness : 1;

    public void Advance(float seconds)
    {
        overlap += seconds;
        int steps = Math.Min((int)(overlap / Dt), 10);
        overlap = steps == 10 ? 0 : overlap - steps * Dt;
        for (int s = 0; s < steps; s++) Step();
    }

    public void Step()
    {
        if (Countdown > 0) Countdown -= Dt;
        else Time += Dt;

        foreach (var car in Cars)
        {
            if (!Started)
            {
                car.Sim.Throttle = 0;
                car.Sim.Steer = 0;
            }
            else if (car.Finished)
            {
                car.Sim.Throttle = 0;
                Drive(car, true);
                car.Sim.Throttle = Math.Min(car.Sim.Throttle, 0);
            }
            else if (car.IsPlayer && !AiDrivesPlayer)
            {
                car.Sim.Throttle = PlayerThrottle;
                car.Sim.Steer = PlayerSteer;
            }
            else Drive(car, false);
            car.Sim.StepMove();
        }

        // As the game: everyone moves, then contacts are found, then everyone answers them
        for (int a = 0; a < Cars.Count; a++)
            for (int b = a + 1; b < Cars.Count; b++)
                Touch(Cars[a], Cars[b]);
        foreach (var car in Cars)
        {
            car.Sim.StepCollide();
            if (car.Sim.TouchesWall && !car.OnWall) car.Hits++;
            car.OnWall = car.Sim.TouchesWall;
        }
        foreach (var car in Cars) Follow(car);
    }

    // ---------------------------- The CPU driver ----------------------------

    void Drive(RaceCar car, bool easing)
    {
        var s = car.Sim;
        float speed = s.Vel.Length();
        var nose = Vector3.Transform(Vector3.UnitZ, s.Rot);
        var right = Vector3.Transform(Vector3.UnitX, s.Rot);

        // On its roof: right it, as a player would
        if (s.Up.Y > -0.3f)
        {
            car.UpsideFor += Dt;
            if (car.UpsideFor > 0.6f) s.Flip();
        }
        else car.UpsideFor = 0;

        // Stuck against something: back out with the wheels turned
        if (car.BackUp > 0)
        {
            car.BackUp -= Dt;
            s.Throttle = -1;
            s.Steer = car.BackSteer;
            return;
        }
        if (speed < 60 && !easing) car.StuckFor += Dt;
        else car.StuckFor = 0;
        if (car.StuckFor > 1.2f)
        {
            car.StuckFor = 0;
            var ahead = Track.Tangents[car.Hint];
            float wrong = Vector3.Dot(right, ahead);
            car.BackSteer = wrong > 0 ? -1 : 1;
            car.BackUp = 1.1f;
            return;
        }

        // Pick a place on the road a little ahead, in a lane that keeps clear of the car in front
        car.Lane += (LaneFor(car) - car.Lane) * Math.Min(1, Dt * 2.5f);
        float look = 170 + speed * 0.22f;
        int i = Track.Index(Track.Distance[car.Hint] + look);
        var target = Track.Points[i] + RaceTrack.Side(Track.Tangents[i]) * car.Lane;
        var to = target - s.Pos;
        float angle = MathF.Atan2(Vector3.Dot(to, right), Vector3.Dot(to, nose));
        float lockAngle = Math.Max(s.SteerLock, 0.05f);
        float steer = Math.Clamp(angle / lockAngle, -1, 1);

        // Slow for what's coming
        int soon = Track.Index(Track.Distance[car.Hint] + speed * 0.1f);
        float wanted = car.Speeds[soon];
        float throttle = speed > wanted * 1.03f ? -1 : speed > wanted * 0.97f ? 0.3f : 1;
        if (Vector3.Dot(s.Vel, nose) < -40) throttle = 1; // rolling backwards: the engine stops that first

        (steer, throttle) = CorrectSlide(car, steer, throttle);
        s.Steer = steer;
        s.Throttle = easing ? Math.Min(throttle, speed > 200 ? -1 : 0) : throttle;
    }

    float LaneFor(RaceCar car)
    {
        float keep = Track.Width * 0.22f;
        foreach (var other in Cars)
        {
            if (other == car) continue;
            float gap = other.Run - car.Run;
            if (gap < 0 || gap > 420) continue;
            var side = RaceTrack.Side(Track.Tangents[car.Hint]);
            float theirs = Vector3.Dot(other.Sim.Pos - Track.Points[other.Hint], side);
            return theirs > 0 ? -keep : keep;
        }
        return Math.Clamp(car.Lane * 0.995f, -keep, keep);
    }

    /// <summary>The game's correction for sliding, from the car's AI values</summary>
    (float Steer, float Throttle) CorrectSlide(RaceCar car, float steer, float throttle)
    {
        if (car.Data.Ai is not { } ai) return (steer, throttle);
        var (front, rear) = car.Sim.SideSlip();
        float amount = 0;
        if (MathF.Sign(front) == MathF.Sign(rear))
        {
            float over = Math.Abs(rear) - Math.Abs(front) - (float)ai.OverThresh;
            if (over > 0 && ai.OverRange > 0)
                amount = Math.Min(over / (float)ai.OverRange, (float)ai.OverMax);
            else
            {
                float under = Math.Abs(front + rear) - (float)ai.UnderThresh;
                if (under > 0 && ai.UnderRange > 0)
                    amount = -Math.Min(under / (float)ai.UnderRange, (float)ai.UnderMax);
            }
        }
        else
        {
            float snap = Math.Abs(front - rear) - (float)ai.OverThresh;
            if (snap > 0 && ai.OverRange > 0)
                amount = Math.Min(snap / (float)ai.OverRange, (float)ai.OverMax);
        }

        if (amount < 0)
        {
            float a = -amount;
            steer = Math.Clamp(steer + (steer < 0 ? -a : a), -1, 1);
            if (!(Math.Abs(front) > ai.UnderFront && Math.Abs(rear) > ai.UnderRear)) throttle = Math.Min(throttle, 1 - a);
        }
        else if (amount > 0)
        {
            steer = Math.Clamp(car.Sim.SteerNow + (front < 0 ? -amount : amount), -1, 1);
            float most = Math.Max(Math.Abs(front), Math.Abs(rear)) - (float)ai.OverAccThresh;
            if (most > 0 && ai.OverAccRange > 0)
                throttle = Math.Min(throttle, 1 - Math.Min(most / (float)ai.OverAccRange, 1.5f));
        }
        return (steer, throttle);
    }

    // ---------------------------- Contact ----------------------------

    // Arcade mode: the two closest spheres meet, and the cars push through their centres without friction
    static void Touch(RaceCar a, RaceCar b)
    {
        var sa = a.Sim.CarSpheres().ToArray();
        var sb = b.Sim.CarSpheres().ToArray();
        float best = float.MaxValue;
        (Vector3 Pos, float Radius) one = sa[0], two = sb[0];
        foreach (var p in sa)
            foreach (var q in sb)
            {
                var d = p.Pos - q.Pos;
                float far = Math.Max(Math.Max(Math.Abs(d.X), Math.Abs(d.Y)), Math.Abs(d.Z));
                if (far < best) { best = far; one = p; two = q; }
            }
        float reach = one.Radius + two.Radius;
        if (best > reach) return;
        var gap = one.Pos - two.Pos;
        float len = gap.Length();
        if (len > reach) return;
        var n = len > 1e-5f ? gap / len : Vector3.UnitX;
        float depth = (len - reach) / 2;
        var closing = a.Sim.Vel - b.Sim.Vel;
        a.Sim.AddCarContact(n, depth, closing, b.Sim.InvMass);
        b.Sim.AddCarContact(-n, depth, -closing, a.Sim.InvMass);
        a.Hits++;
        b.Hits++;
    }

    void Follow(RaceCar car)
    {
        int before = car.Hint;
        car.Hint = Track.Nearest(car.Sim.Pos, before);
        float delta = Track.Distance[car.Hint] - Track.Distance[before];
        if (delta < -Track.Length / 2) delta += Track.Length;
        else if (delta > Track.Length / 2) delta -= Track.Length;
        car.Run += delta;

        if (!Started || car.Finished) return;
        int lap = car.Run < 0 ? 0 : (int)(car.Run / Track.Length) + 1;
        if (lap <= car.Lap) return;
        // Each new lap, and only once for each
        if (car.Lap >= 1)
        {
            car.LastLap = Time - car.LapStart;
            car.BestLap = Math.Min(car.BestLap, car.LastLap);
        }
        car.LapStart = Time;
        car.Lap = lap;
        if (lap > Laps) car.FinishTime = Time;
    }

    /// <summary>The game's reposition: back on the road, facing the way of the race</summary>
    public void Reposition(RaceCar car)
    {
        int i = car.Hint;
        car.Sim.Place(Track.Points[i], MathF.Atan2(Track.Tangents[i].X, Track.Tangents[i].Z));
        car.Lane = 0;
    }

    /// <summary>Cars to race against: stock cars closest in speed to this one, the odd ones left out</summary>
    public static List<string> PickOpponents(GameFolder game, CarData mine, string myFolder, int count)
    {
        var odd = new[] { "ufo", "trolley", "panga", "wincar", "wincar2", "wincar3", "wincar4", "misc" };
        var found = new List<(string Folder, double Gap)>();
        foreach (var folder in game.CarFolders().Where(GameFolder.IsStock))
        {
            if (odd.Contains(folder, StringComparer.OrdinalIgnoreCase) || folder.Equals(myFolder, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var d = new Car(game, folder).Data;
                if (d.Wheels.Count(w => w.Present && w.Powered) == 0) continue;
                if (d.BodyModel < 0 || game.Find(d.TPage) == null) continue;
                found.Add((folder, Math.Abs(d.TopSpeed - mine.TopSpeed)));
            }
            catch (Exception) { }
        }
        return found.OrderBy(f => f.Gap).ThenBy(f => f.Folder).Take(count).Select(f => f.Folder).ToList();
    }
}
