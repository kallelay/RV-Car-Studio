using System.Numerics;

namespace RvStudio.Core;

/// <summary>The game's four levels of help for CPU cars. A single race there is always the first</summary>
public enum RaceDifficulty { VeryEasy, Easy, Medium, Hard }

/// <summary>What one CPU driver remembers between its thoughts</summary>
public sealed class CpuBrain
{
    public enum Mode { Grid, Green, Race, Trouble, Air, ForwardLeft, ForwardRight, ReverseLeft, ReverseRight }
    public Mode State = Mode.Grid;
    internal Mode? LastState;
    internal float StateCount;

    /// <summary>The node whose stretch the car is over, if any, and the last one it was over</summary>
    public AiNode? Cur, LastValid;
    public float DistAlong, LineSide, FaceDir, Speed, LookAhead;
    internal Vector3 Look = Vector3.UnitZ;

    /// <summary>Between the racing line, 0, and the overtaking line, 1</summary>
    public float Line = 0.5f, LineTarget = 0.5f;
    public bool Overtake;
    internal int OvertakeLast = -1;
    internal float TimeOvertake;

    /// <summary>The point it steers for, the band around it where it leaves the wheel alone, and where it points</summary>
    public Vector3 Target;
    public float BiasSize = 50, Cos = 1, CosAbs = 1, Side;
    internal float EdgeLeft, EdgeRight;

    /// <summary>The controls as the game has them: -127 to 127, right and brake positive</summary>
    public int Dx, Dy;
    internal int CatchMode, CatchLevel;
    internal bool CatchUp;

    internal float OutNode, StuckFor, BeachCount;
    internal Vector3 BeachPos;
    internal RaceCar? Front;
    public int Zone, Place = -1;
}

// The CPU's hands, as the original game's driver has them. It gives a steering and a throttle value and nothing else,
// so CPU cars obey the same physics as yours. It thinks about 30 times a second, all cars on the same step, and holds
// its controls in between. It never brakes for a bend it sees coming: it keeps the throttle down and reacts to sliding,
// to a wall close ahead, and to where its car points.
public sealed partial class Race
{
    const float AiDt = 1f / 30;
    const float Mph = CarSim.MphToUnits;
    const int Full = 127;

    sealed record Help(float SpeedUpFrom, float SlowDownFrom, float PlaceAdjust, bool KeepSpeedUp, bool KeepSlowDown,
        float[] SpeedUpMph, float[] SlowDownMph, float BeachTime, float BeachDist2, float FlipTime);

    static readonly Help[] Helps =
    {
        new(2000, 600, 600, false, true, new[] { 1, 1.5f, 2, 3 }, new[] { -2f, -3, -4 }, 5, 200 * 200, 3),
        new(2000, 1400, 1000, false, true, new[] { 1, 1.5f, 2, 3 }, new[] { -1f, -2, -3 }, 4, 100 * 100, 2),
        new(1400, 2000, 1000, true, false, new[] { 1f, 2, 3, 4 }, new[] { 0f, -1, -2 }, 2.5f, 50 * 50, 2),
        new(1000, 2600, 1000, true, false, new[] { 1f, 2, 3, 4 }, new[] { 0f, 0, -1 }, 1, 20 * 20, 1),
    };

    /// <summary>How much the CPU cars are helped or held back against you, and how soon a stuck one is put back</summary>
    public RaceDifficulty Difficulty = RaceDifficulty.VeryEasy;
    Help help => Helps[(int)Difficulty];

    /// <summary>The slider of the race page, as the nearest thing the game has: its difficulty</summary>
    public static RaceDifficulty DifficultyFor(float skill) =>
        skill < 0.75f ? RaceDifficulty.VeryEasy : skill < 0.9f ? RaceDifficulty.Easy : skill < 1 ? RaceDifficulty.Medium : RaceDifficulty.Hard;

    public AiNet Net { get; private set; } = null!;
    int aiClock;

    void PrepareDriver(RaceCar car)
    {
        var b = car.Brain;
        b.Cur = b.LastValid = Net.FindInZones(car.Sim.Centre, Net.NodeAt(car.Hint).Zone);
        b.BeachPos = car.Sim.Centre;
        b.BeachCount = help.BeachTime;
        b.Zone = Net.NodeAt(car.Hint).Zone;
    }

    /// <summary>Each physics step takes 8 ms off a counter that gets 33 back when it runs out: true on those steps</summary>
    bool AiTick()
    {
        aiClock -= 8;
        if (aiClock >= 0) return false;
        aiClock += 33;
        return true;
    }

    // CPU cars steer through a curve that is gentle around the middle
    static void ApplyControls(RaceCar car)
    {
        var b = car.Brain;
        car.Sim.Steer = b.Dx * MathF.Abs(b.Dx) / (float)(Full * Full);
        car.Sim.Throttle = car.Finished ? 0 : -b.Dy / (float)Full;
    }

    /// <summary>The whole number the game compares cars by: how far along the race</summary>
    static int RaceDistance(RaceCar car) => (int)car.Run;

    // Places change only by passing: equal distances keep their order
    void Places()
    {
        var order = Cars.OrderBy(c => c.Brain.Place < 0 ? Cars.IndexOf(c) : c.Brain.Place).ToList();
        for (int i = 1; i < order.Count; i++)
            for (int k = i; k > 0 && RaceDistance(order[k]) > RaceDistance(order[k - 1]); k--)
                (order[k], order[k - 1]) = (order[k - 1], order[k]);
        for (int i = 0; i < order.Count; i++) order[i].Brain.Place = i;
    }

    void Think(RaceCar car)
    {
        var b = car.Brain;
        b.Dx = b.Dy = 0;

        // Which line to aim for changes slowly: ten seconds from one to the other
        if (b.Cur != null && (b.Overtake ? 1 : 0) != b.OvertakeLast)
        {
            b.OvertakeLast = b.Overtake ? 1 : 0;
            b.LineTarget = b.Overtake ? 1 : 0;
        }
        b.Line = Approach(b.Line, b.LineTarget, 0.1f * AiDt);

        CatchUp(car);
        UpdateNode(car);
        b.Look = FlatLook(car);
        FindCars(car);
        AngleToLine(car);

        if (b.State != b.LastState)
        {
            b.LastState = b.State;
            b.StateCount = b.State >= CpuBrain.Mode.ForwardLeft ? 3 : 0;
        }
        if (b.StateCount > 0) b.StateCount -= AiDt;

        switch (b.State)
        {
            case CpuBrain.Mode.Grid: b.State = CpuBrain.Mode.Green; break;
            case CpuBrain.Mode.Green: b.State = CpuBrain.Mode.Race; break;
            case CpuBrain.Mode.Race: StateRace(car); break;
            case CpuBrain.Mode.Air:
                b.Dx = 0;
                b.Dy = -Full;
                if (car.Sim.FloorContacts > 0) b.State = CpuBrain.Mode.Race;
                break;
            case CpuBrain.Mode.Trouble: StateTrouble(car); break;
            default: StateCorrect(car); break;
        }

        if (b.State == CpuBrain.Mode.Race)
        {
            CorrectSlide(car);
            NearWalls(car);
            NodeTypeRules(car);
        }
        b.Dx = Quantise(b.Dx);
        b.Dy = Quantise(b.Dy);
    }

    static float Approach(float now, float target, float step) => target > now ? MathF.Min(now + step, target) : MathF.Max(now - step, target);

    // Controls move in steps of 8
    static int Quantise(int v)
    {
        int q = Math.Min((Math.Abs(v) + 4) & ~7, Full);
        return v < 0 ? -q : q;
    }

    static Vector3 FlatLook(RaceCar car)
    {
        var look = Vector3.Transform(Vector3.UnitZ, car.Sim.Rot);
        look.Y = 0;
        return look.LengthSquared() > 1e-8f ? Vector3.Normalize(look) : Vector3.UnitZ;
    }

    /// <summary>Where the car is among the nodes: from the node it knew, forward along the road, then back</summary>
    void UpdateNode(RaceCar car)
    {
        var b = car.Brain;
        b.Speed = car.Sim.Vel.Length();
        b.Zone = Net.NodeAt(car.Hint).Zone;
        var p = car.Sim.Centre;

        AiNode? found = null;
        var start = b.Cur ?? b.LastValid;
        if (start != null)
        {
            float reach = b.Cur != null ? 2000 : 6000;
            float left = reach;
            for (var n = start; left > 0; n = n.Next)
            {
                if (n.Over(p)) { found = n; break; }
                left -= n.Dist;
            }
            left = reach;
            for (var n = start; found == null && left > 0;)
            {
                if (n.Over(p)) { found = n; break; }
                n = n.Prev;
                left -= n.Dist;
            }
        }
        found ??= Net.FindInZones(p, b.Zone);

        b.Cur = found;
        if (found != null)
        {
            b.LastValid = found;
            var d = p - found.Centre;
            b.DistAlong = Vector3.Dot(found.Forward, d);
            b.LineSide = Vector3.Dot(found.Side, d);
            // With where the car pointed at its last thought, as the game has it
            b.FaceDir = MathF.Sign(Vector3.Dot(b.Look, found.Forward));
        }
        else b.DistAlong = b.LineSide = b.FaceDir = 0;
    }

    // The nearest car ahead of the nose, among those in this zone or the ones beside it
    void FindCars(RaceCar car)
    {
        var b = car.Brain;
        b.Front = null;
        float nearest = float.MaxValue;
        foreach (var other in Cars)
        {
            if (other == car || Math.Abs(other.Brain.Zone - b.Zone) > 1) continue;
            var d = other.Sim.Centre - car.Sim.Centre;
            if (Vector3.Dot(d, b.Look) < 0) continue;
            float len = d.Length();
            if (len < nearest) { nearest = len; b.Front = other; }
        }
    }

    /// <summary>
    /// The point to steer for, a look-ahead along the line it's on, and how far the nose is from pointing at it.
    /// Close enough, inside a band that widens while the car is off it, counts as pointing straight at it
    /// </summary>
    void AngleToLine(RaceCar car)
    {
        var b = car.Brain;
        var pos = car.Sim.Centre;
        b.LookAhead = b.State != CpuBrain.Mode.Race ? 200 : b.Cur != null ? 600 + 0.1f * b.Speed : 2000;
        var a = b.Cur ?? b.LastValid;
        if (a == null)
        {
            b.Target = pos;
            b.Cos = b.CosAbs = 1;
            b.Side = 0;
            return;
        }
        float dist = b.LookAhead + (b.Cur != null ? b.DistAlong : 0);
        for (int guard = Net.Nodes.Count * 2; guard > 0; guard--)
        {
            dist -= a.Dist;
            if (dist < 0) break;
            a = a.Next;
        }
        var z = a.Next;
        float la = a.Racing + (a.Overtake - a.Racing) * b.Line, lz = z.Racing + (z.Overtake - z.Racing) * b.Line;
        b.Target = Vector3.Lerp(a.Line(la), z.Line(lz), (dist + a.Dist) / a.Dist);

        var to = b.Target - pos;
        to.Y = 0;
        var f = to.LengthSquared() > 1e-6f ? Vector3.Normalize(to) : b.Look;
        var r = RaceTrack.Side(f);
        var probe = pos + b.Look * b.LookAhead - b.Target;
        probe.Y = 0;
        b.Side = Vector3.Dot(r, probe);

        float edge = MathF.Min(MathF.Abs(MathF.Min(a.EdgeLeft(b.Target), a.EdgeRight(b.Target)) - 150), 100);
        float cos = Vector3.Dot(f, b.Look);
        if (MathF.Abs(b.Side) > b.BiasSize) b.BiasSize = MathF.Min(b.BiasSize + 50 * AiDt, edge);
        else
        {
            b.BiasSize = MathF.Max(b.BiasSize - 300 * AiDt, 50);
            cos *= 1.5f;
        }
        b.Cos = Math.Clamp(cos, -1, 1);
        b.CosAbs = MathF.Abs(b.Cos);
    }

    // The wheel turns with the root of how far off the nose is, full lock at a right angle for a car of 45 degrees' lock
    static void Steering(RaceCar car)
    {
        var b = car.Brain;
        float lockDegrees = car.Sim.SteerLock * 180 / MathF.PI;
        float convert = lockDegrees > 0.1f ? 90 / lockDegrees : 1;
        float a = Math.Min(MathF.Sqrt(MathF.Max(1 - b.CosAbs, 0)) * convert * 0.5f, 1);
        b.Dx = (int)(Full * a);
        if (b.Side >= 0) b.Dx = -b.Dx;
    }

    void StateRace(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        // Against something and hardly moving, or facing back down the road
        if (((s.WheelColls - s.FloorContacts != 0 || s.BodyColls != 0) && b.Speed < 3 * Mph) || b.FaceDir < 0)
        {
            b.State = CpuBrain.Mode.Trouble;
            return;
        }
        if (s.FloorContacts == 0 && s.WheelsTouching < 3)
        {
            b.State = CpuBrain.Mode.Air;
            return;
        }
        Steering(car);
        b.Dy = -Full;

        // A car close ahead: take the other line from the one it's on, and stay there four seconds
        if (b.Front != null && RaceDistance(b.Front) - RaceDistance(car) < 500)
        {
            b.Overtake = !b.Front.Brain.Overtake;
            b.TimeOvertake = 4;
        }
        else if (b.TimeOvertake > 0) b.TimeOvertake -= AiDt;
        else b.Overtake = false;
    }

    void StateTrouble(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        // Scraping along a wall: steer off it
        if (s.WheelSide[0] && s.WheelSide[2]) { b.Dy = -Full; b.Dx = b.FaceDir >= 0 ? Full >> 1 : Full; return; }
        if (s.WheelSide[1] && s.WheelSide[3]) { b.Dy = -Full; b.Dx = b.FaceDir >= 0 ? -Full >> 1 : -Full; return; }
        if (b.Cos > 0.707f && b.FaceDir > 0) { b.State = CpuBrain.Mode.Race; return; }
        // Turn towards the nearer edge, as it was when last measured
        b.Dy = -Full;
        if (b.EdgeLeft < b.EdgeRight) { b.Dx = -Full; b.State = CpuBrain.Mode.ForwardLeft; }
        else { b.Dx = Full; b.State = CpuBrain.Mode.ForwardRight; }
    }

    // A three-point turn: forward on one lock, back on the other, until the nose is roughly the right way
    void StateCorrect(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        if (b.Cos > 0.2f) { b.State = CpuBrain.Mode.Race; return; }
        bool forward = b.State is CpuBrain.Mode.ForwardLeft or CpuBrain.Mode.ForwardRight;
        bool wall = forward ? s.WheelWall[0] || s.WheelWall[1] : s.WheelWall[2] || s.WheelWall[3];
        if (wall || (b.StateCount < 0 && b.Speed < 3 * Mph))
        {
            b.State = b.State switch
            {
                CpuBrain.Mode.ForwardLeft => CpuBrain.Mode.ReverseRight,
                CpuBrain.Mode.ForwardRight => CpuBrain.Mode.ReverseLeft,
                CpuBrain.Mode.ReverseLeft => CpuBrain.Mode.ForwardRight,
                _ => CpuBrain.Mode.ForwardLeft,
            };
            return;
        }
        bool slow = b.Speed < 20 * Mph;
        b.Dx = b.State is CpuBrain.Mode.ForwardLeft or CpuBrain.Mode.ReverseLeft ? -Full : Full;
        b.Dy = forward ? (slow ? -Full >> 1 : -Full >> 4) : (slow ? Full >> 1 : Full >> 4);
    }

    static readonly AiData NeutralAi = new()
    {
        UnderThresh = 150, UnderRange = 1500, UnderFront = 450, UnderRear = 335, UnderMax = 0.95,
        OverThresh = 100, OverRange = 1391, OverMax = 1, OverAccThresh = 10, OverAccRange = 400,
    };

    /// <summary>
    /// The game's correction for sliding, from the car's AI values: with three wheels on the floor, how fast the front
    /// and the rear go sideways says whether the car pushes wide or its tail comes round
    /// </summary>
    static void CorrectSlide(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        if (s.FloorContacts < 3) return;
        var ai = car.Data.Ai ?? NeutralAi;
        var (front, rear) = s.SideSlip();
        double amount = 0;
        if (MathF.Sign(front) == MathF.Sign(rear))
        {
            double over = Math.Abs(rear) - Math.Abs(front) - ai.OverThresh;
            if (over > 0) amount = Math.Min(over / ai.OverRange, ai.OverMax);
            else
            {
                double under = Math.Abs(front + rear) - ai.UnderThresh;
                if (under > 0) amount = -Math.Min(under / ai.UnderRange, ai.UnderMax);
            }
        }
        else
        {
            double snap = Math.Abs(front - rear) - ai.OverThresh;
            if (snap > 0) amount = Math.Min(snap / ai.OverRange, ai.OverMax);
        }
        if (double.IsNaN(amount)) return;

        if (amount < 0)
        {
            // Pushing wide: more lock, and off the throttle unless both ends slide a lot
            double a = -amount;
            b.Dx = Math.Clamp(b.Dx + (b.Dx >= 0 ? (int)(a * Full) : -(int)(a * Full)), -Full, Full);
            if (!(Math.Abs(front) > ai.UnderFront && Math.Abs(rear) > ai.UnderRear)) b.Dy = (int)(-Full * (1 - a));
        }
        else if (amount > 0)
        {
            // Tail coming round: steer into it from where the wheels are now, and lift, down to braking
            b.Dx = front < 0 ? Math.Max((int)((s.SteerNow - amount) * Full), -Full) : Math.Min((int)((s.SteerNow + amount) * Full), Full);
            double most = Math.Max(Math.Abs(front), Math.Abs(rear)) - ai.OverAccThresh;
            if (most > 0) b.Dy = (int)(-Full * (1 - Math.Min(most / ai.OverAccRange, 1.5)));
        }
    }

    /// <summary>
    /// Where the car will be in a third of a second: closer than 20 units to an edge it steers away, braking with
    /// what steering can't take; off the road altogether it brakes
    /// </summary>
    void NearWalls(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        if (b.Cur == null || s.FloorContacts <= 2) return;
        const float Margin = 20, Ahead = 0.35f;
        b.EdgeLeft = b.Cur.EdgeLeft(s.Centre);
        b.EdgeRight = b.Cur.EdgeRight(s.Centre);
        var future = s.Centre + s.Vel * Ahead;
        var n = b.Cur;
        int budget = 10;
        bool found = false;
        while (budget-- > 0)
        {
            if (n.Over(future)) { found = true; break; }
            n = n.Next;
        }
        if (found && budget > 0)
        {
            float e0 = n.EdgeLeft(future), e1 = n.EdgeRight(future);
            if (e0 < Margin)
            {
                int x = b.Dx + (int)(Full * (Margin - e0) / Margin);
                if (x > Full) { b.Dy = Math.Min(b.Dy + (x - Full), Full); b.Dx = Full; }
                else b.Dx = x;
            }
            else if (e1 < Margin)
            {
                int x = b.Dx - (int)(Full * (Margin - e1) / Margin);
                if (x < -Full) { b.Dy = Math.Min(b.Dy + (-Full - x), Full); b.Dx = -Full; }
                else b.Dx = x;
            }
        }
        else if (b.Speed > 30 * Mph) { b.Dx = 0; b.Dy = Full; }
        else if (b.Speed > 20 * Mph) b.Dy = Full;
    }

    // What a track maker marks on a node. The built-in roads mark nothing, the rules are here for roads that would
    void NodeTypeRules(RaceCar car)
    {
        var b = car.Brain;
        if (b.Cur == null) return;
        void Limit(float mph) { if (b.Speed > mph * Mph) b.Dy = Full; }
        switch (b.Cur.Type)
        {
            case AiNet.Slow15: Limit(15); break;
            case AiNet.Slow20: Limit(20); break;
            case AiNet.Slow25: Limit(25); break;
            case AiNet.Slow30: Limit(30); break;
            case AiNet.TitleSlow: if (b.Speed > 0.25f * (float)car.Data.TopSpeed * Mph) b.Dy = Full; break;
            case AiNet.OffThrottle: b.Dy = b.Speed > 20 * Mph ? 0 : (int)(-Full * 0.5); break;
        }
    }

    /// <summary>
    /// The game's rubber band, measured against you: a CPU car far behind gets a little more top speed and grip, one
    /// far ahead a little less top speed. Nothing else, and never a jump along the road
    /// </summary>
    void CatchUp(RaceCar car)
    {
        var b = car.Brain;
        b.CatchUp = false;
        b.CatchLevel = 0;
        if (Player is null || car == Player) return;
        var h = help;
        int dist = RaceDistance(Player) - RaceDistance(car);
        int places = Player.Brain.Place - b.Place;
        if (places > 1) dist = Math.Min(dist + (int)h.PlaceAdjust * (places - 1), 0);
        if (dist > h.SpeedUpFrom)
        {
            b.CatchLevel = Math.Min(dist / (int)h.SpeedUpFrom, 3);
            b.CatchUp = true;
            b.CatchMode = 1;
        }
        else if (dist < -h.SlowDownFrom)
        {
            b.CatchLevel = Math.Min(-dist / (int)h.SlowDownFrom, 3);
            b.CatchMode = -1;
        }
        else if (b.CatchMode == 1 && h.KeepSpeedUp) b.CatchUp = true;
        else if (b.CatchMode == -1 && h.KeepSlowDown) b.CatchLevel = 1;
        else b.CatchMode = 0;
    }

    // What the catch-up asks for takes effect on every step, and not on the grid or after the flag
    void ApplyCatchUp(RaceCar car, bool cpu)
    {
        var b = car.Brain;
        var s = car.Sim;
        if (!cpu || !Started || car.Finished || (!b.CatchUp && b.CatchLevel == 0)) s.SetCatchUp(0, 1);
        else if (b.CatchUp) s.SetCatchUp(help.SpeedUpMph[b.CatchLevel], 1 + (b.CatchLevel + 1) / 24f);
        // Slowing leaves the grip as it was
        else s.SetCatchUp(car.Data.TopSpeed < Player.Data.TopSpeed ? 0 : help.SlowDownMph[b.CatchLevel - 1], s.CatchFriction);
    }

    /// <summary>The game's watch over a CPU car: off the nodes for ten seconds, on its side or roof, or going nowhere</summary>
    void Helper(RaceCar car)
    {
        var b = car.Brain;
        var s = car.Sim;
        bool free = Started && !car.Finished; // on the grid and after the flag, neither button works

        b.OutNode = b.Cur != null ? 0 : b.OutNode + Dt;
        if (b.OutNode > 10)
        {
            b.OutNode = 0;
            if (free) StartReposition(car);
        }

        bool stuck = false;
        if (!s.Righting)
        {
            if (s.FloorContacts == 0 && (s.WorldContacts != 0 || s.NoContactTime < 0.1f || s.WheelColls > 0)) stuck = true;
            else if (free)
            {
                // Every few seconds: has it moved at all since the last look?
                b.BeachCount -= Dt;
                if (b.BeachCount <= 0)
                {
                    b.BeachCount = help.BeachTime;
                    bool beached = Vector3.DistanceSquared(s.Centre, b.BeachPos) <= help.BeachDist2;
                    b.BeachPos = s.Centre;
                    if (beached) StartReposition(car);
                }
            }
            else b.BeachCount = help.BeachTime;
        }
        b.StuckFor = stuck ? b.StuckFor + Dt : 0;
        if (b.StuckFor > help.FlipTime)
        {
            b.StuckFor = 0;
            b.State = CpuBrain.Mode.Race;
            // Rotor drives on either side, and is left alone
            if (free && car.ReposTimer <= 0 && !car.Folder.Equals("rotor", StringComparison.OrdinalIgnoreCase)) s.Flip();
        }
    }

    // ---------------------------- Reposition ----------------------------

    /// <summary>The game's reposition takes this long: the car slows to a stop, is put back at the half, and picks up again</summary>
    public const float RepositionTime = 2;

    /// <summary>The R key, and what the game does for a lost or stuck CPU car. Nothing if one is under way</summary>
    public void StartReposition(RaceCar car)
    {
        if (car.ReposTimer > 0) return;
        car.ReposTimer = RepositionTime;
        car.ReposHalf = false;
        car.ReposClock = 0;
        car.Brain.State = CpuBrain.Mode.Race;
    }

    /// <summary>One step of a reposition. True when the car moves on this step: time runs slower the nearer the half</summary>
    bool StepReposition(RaceCar car)
    {
        if (car.ReposTimer <= 0) return true;
        car.ReposTimer -= Dt;
        if (car.ReposTimer <= 0)
        {
            car.ReposTimer = 0;
            return true;
        }
        if (car.ReposTimer < RepositionTime / 2 && !car.ReposHalf)
        {
            car.ReposHalf = true;
            ResetCar(car);
        }
        // The physics has one size of step, so a slower clock is fewer of them
        car.ReposClock += MathF.Abs(car.ReposTimer - 1);
        if (car.ReposClock < 1) return false;
        car.ReposClock -= 1;
        return true;
    }

    /// <summary>
    /// Back on the racing line, 100 units on from the node it was last over and 100 above the road, at rest, facing
    /// the way of the race. Laps and distance aren't touched
    /// </summary>
    void ResetCar(RaceCar car)
    {
        var b = car.Brain;
        b.State = CpuBrain.Mode.Race;
        b.BeachCount = help.BeachTime;
        var node = b.Cur ?? b.LastValid;
        if (node == null) return;
        // Never onto a stretch that is a jump over a wall
        for (int guard = Net.Nodes.Count; guard > 0 && node.Next.Type == AiNet.JumpWall; guard--) node = node.Next;
        b.Cur = b.LastValid = node;
        var at = Vector3.Lerp(node.Centre, node.Next.Centre, 100 / node.Dist);
        car.Sim.Place(at, MathF.Atan2(node.Forward.X, node.Forward.Z));
        car.Sim.Pos -= Vector3.UnitY * 100;
    }
}
