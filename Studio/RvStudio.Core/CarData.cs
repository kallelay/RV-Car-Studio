using System.Globalization;
using System.Numerics;
using System.Text.Json;

namespace RvStudio.Core;

// All vectors are as in the file: X right, Y down, Z forward, 1 unit = 0.5 cm

public sealed class WheelData
{
    public int Model = -1;
    public Vector3 Offset1, Offset2;
    public bool Present, Powered, Turnable;
    public double SteerRatio, EngineRatio, Radius, Mass, MaxPos, SkidWidth, ToeIn, AxleFriction, Grip, StaticFriction, KineticFriction, Camber;
    public WheelData Clone() => (WheelData)MemberwiseClone();
}
public sealed class SpringData
{
    public int Model = -1;
    public Vector3 Offset;
    public double Length, Stiffness, Damping, Restitution;
    public SpringData Clone() => (SpringData)MemberwiseClone();
}
public sealed class LinkData // pin and axle
{
    public int Model = -1;
    public Vector3 Offset;
    public double Length;
    public LinkData Clone() => (LinkData)MemberwiseClone();
}
public sealed class SpinnerData
{
    public int Model = -1, Type = 1;
    public Vector3 Offset, Axis, Trans;
    public double AngVel, TransVel;
    public SpinnerData Clone() => (SpinnerData)MemberwiseClone();
}
public sealed class AerialData
{
    public int SecModel = -1, TopModel = -1;
    public Vector3 Offset, Direction;
    public double Length, Stiffness, Damping;
    public AerialData Clone() => (AerialData)MemberwiseClone();
}
public sealed class AiData
{
    public double UnderThresh, UnderRange, UnderFront, UnderRear, UnderMax, OverThresh, OverRange, OverMax,
        OverAccThresh, OverAccRange, PickupBias, BlockBias, OvertakeBias, Suspension, Aggression;
    public AiData Clone() => (AiData)MemberwiseClone();
}
public sealed class CameraData
{
    public Vector3 HoodOffset, RearOffset;
    public double HoodLook, RearLook;
    public bool FixedOffset = true, FixedLook = true;
    public CameraData Clone() => (CameraData)MemberwiseClone();
}

/// <summary>Everything parameters.txt says about a car, with all RVGL additions</summary>
public sealed class CarData
{
    public string Name = "";
    public string[] Models = Enumerable.Repeat("NONE", 19).ToArray();
    public string TPage = "", Coll = "", TCarBox = "", TShadow = "", SfxEngine = "", SfxHonk = "", SfxServo = "";
    public int EnvR, EnvG, EnvB;
    public int ShadowIndex = -1;
    public bool HasShadowIndex, HasShadowTable, HasWeapon2, HasWeaponOffset1;
    public double[] ShadowTable = new double[5]; // left right front back height

    public bool BestTime, Selectable, CpuSelectable = true, Statistics, Flippable, Flying, ClothFx;
    public int Class = 2, Obtain = -1, Rating = 4, Trans;
    public double TopEnd, Acc, Weight, Handling, MaxRevs;

    public double SteerRate, SteerMod, EngineRate, TopSpeed, DownForceMod;
    public Vector3 CoM, Weapon, Weapon2;

    public int BodyModel = -1;
    public Vector3 BodyOffset;
    public double BodyMass, Hardness, Resistance, AngRes, ResMod, BodyGrip, BodyStatic, BodyKinetic;
    public Vector3[] Inertia = new Vector3[3];

    public WheelData[] Wheels = { new(), new(), new(), new() };
    public SpringData[] Springs = { new(), new(), new(), new() };
    public LinkData[] Pins = { new(), new(), new(), new() };
    public LinkData[] Axles = { new(), new(), new(), new() };
    public SpinnerData Spinner = new();
    public AerialData Aerial = new();
    public AiData? Ai;
    public CameraData? Camera;

    public CarData Clone()
    {
        var c = (CarData)MemberwiseClone();
        c.Models = (string[])Models.Clone();
        c.ShadowTable = (double[])ShadowTable.Clone();
        c.Inertia = (Vector3[])Inertia.Clone();
        c.Wheels = Wheels.Select(w => w.Clone()).ToArray();
        c.Springs = Springs.Select(w => w.Clone()).ToArray();
        c.Pins = Pins.Select(w => w.Clone()).ToArray();
        c.Axles = Axles.Select(w => w.Clone()).ToArray();
        c.Spinner = Spinner.Clone();
        c.Aerial = Aerial.Clone();
        c.Ai = Ai?.Clone();
        c.Camera = Camera?.Clone();
        return c;
    }

    static Vector3 V(ParamSection s, string key)
    {
        var n = s.Numbers(key, 3);
        return new Vector3((float)n[0], (float)n[1], (float)n[2]);
    }

    public static CarData From(ParamFile f, bool stock)
    {
        var d = new CarData();
        var m = f.Main;
        d.Name = m.Str("Name");
        for (int i = 0; i < 19; i++) d.Models[i] = m.Str("MODEL " + i, "NONE");
        d.TPage = m.Str("TPAGE");
        d.Coll = m.Str("COLL");
        var rgb = m.Numbers("EnvRGB", 3);
        (d.EnvR, d.EnvG, d.EnvB) = ((int)rgb[0], (int)rgb[1], (int)rgb[2]);
        d.TCarBox = m.Str("TCARBOX");
        d.TShadow = m.Str("TSHADOW");
        d.HasShadowIndex = m.Has("SHADOWINDEX");
        d.ShadowIndex = m.Int("SHADOWINDEX", -1);
        d.HasShadowTable = m.Has("SHADOWTABLE");
        d.ShadowTable = m.Numbers("SHADOWTABLE", 5);
        d.SfxEngine = m.Str("SFXENGINE");
        d.SfxHonk = m.Str("SFXHONK");
        d.SfxServo = m.Str("SFXSERVO");
        d.Statistics = m.Bool("Statistics", stock);
        d.BestTime = m.Bool("BestTime");
        d.Selectable = m.Bool("Selectable");
        d.CpuSelectable = m.Bool("CPUSelectable", true);
        d.Class = m.Int("Class", 2);
        d.Obtain = m.Int("Obtain", -1);
        d.Rating = m.Int("Rating", 4);
        d.TopEnd = m.Num("TopEnd");
        d.Acc = m.Num("Acc");
        d.Weight = m.Num("Weight");
        d.Handling = m.Num("Handling");
        d.Trans = m.Int("Trans");
        d.MaxRevs = m.Num("MaxRevs");
        d.ClothFx = m.Bool("ClothFx");
        d.Flippable = m.Bool("Flippable");
        d.Flying = m.Bool("Flying");
        d.SteerRate = m.Num("SteerRate");
        d.SteerMod = m.Num("SteerMod");
        d.EngineRate = m.Num("EngineRate");
        d.TopSpeed = m.Num("TopSpeed");
        d.DownForceMod = m.Num("DownForceMod");
        d.CoM = V(m, "CoM");
        d.HasWeaponOffset1 = m.Has("WeaponOffset1");
        d.Weapon = V(m, d.HasWeaponOffset1 ? "WeaponOffset1" : "Weapon");
        d.HasWeapon2 = m.Has("WeaponOffset2");
        d.Weapon2 = V(m, "WeaponOffset2");

        var b = f.Section("BODY") ?? ParamSection.Empty;
        d.BodyModel = b.Int("ModelNum", -1);
        d.BodyOffset = V(b, "Offset");
        d.BodyMass = b.Num("Mass");
        d.Inertia[0] = V(b, "Inertia");
        d.Inertia[1] = V(b, "Inertia#1");
        d.Inertia[2] = V(b, "Inertia#2");
        d.Hardness = b.Num("Hardness");
        d.Resistance = b.Num("Resistance");
        d.AngRes = b.Num("AngRes");
        d.ResMod = b.Num("ResMod");
        d.BodyGrip = b.Num("Grip");
        d.BodyStatic = b.Num("StaticFriction");
        d.BodyKinetic = b.Num("KineticFriction");

        for (int i = 0; i < 4; i++)
        {
            var s = f.Section("WHEEL " + i) ?? ParamSection.Empty;
            var w = d.Wheels[i];
            w.Model = s.Int("ModelNum", -1);
            w.Offset1 = V(s, "Offset1");
            w.Offset2 = V(s, "Offset2");
            w.Present = s.Bool("IsPresent");
            w.Powered = s.Bool("IsPowered");
            w.Turnable = s.Bool("IsTurnable");
            w.SteerRatio = s.Num("SteerRatio");
            w.EngineRatio = s.Num("EngineRatio");
            w.Radius = s.Num("Radius");
            w.Mass = s.Num("Mass");
            w.MaxPos = s.Num("MaxPos");
            w.SkidWidth = s.Num("SkidWidth");
            w.ToeIn = s.Num("ToeIn");
            w.AxleFriction = s.Num("AxleFriction");
            w.Grip = s.Num("Grip");
            w.StaticFriction = s.Num("StaticFriction");
            w.KineticFriction = s.Num("KineticFriction");
            w.Camber = s.Num("Camber");

            s = f.Section("SPRING " + i) ?? ParamSection.Empty;
            var sp = d.Springs[i];
            sp.Model = s.Int("ModelNum", -1);
            sp.Offset = V(s, "Offset");
            sp.Length = s.Num("Length");
            sp.Stiffness = s.Num("Stiffness");
            sp.Damping = s.Num("Damping");
            sp.Restitution = s.Num("Restitution");

            s = f.Section("PIN " + i) ?? ParamSection.Empty;
            d.Pins[i].Model = s.Int("ModelNum", -1);
            d.Pins[i].Offset = V(s, "Offset");
            d.Pins[i].Length = s.Num("Length");

            s = f.Section("AXLE " + i) ?? ParamSection.Empty;
            d.Axles[i].Model = s.Int("ModelNum", -1);
            d.Axles[i].Offset = V(s, "Offset");
            d.Axles[i].Length = s.Num("Length");
        }

        var sn = f.Section("SPINNER") ?? ParamSection.Empty;
        d.Spinner.Model = sn.Int("ModelNum", -1);
        d.Spinner.Offset = V(sn, "Offset");
        d.Spinner.Axis = V(sn, "Axis");
        d.Spinner.AngVel = sn.Num("AngVel");
        d.Spinner.Type = sn.Int("Type", 1);
        d.Spinner.Trans = V(sn, "Trans");
        d.Spinner.TransVel = sn.Num("TransVel");

        var ae = f.Section("AERIAL") ?? ParamSection.Empty;
        d.Aerial.SecModel = ae.Int("SecModelNum", -1);
        d.Aerial.TopModel = ae.Int("TopModelNum", -1);
        d.Aerial.Offset = V(ae, "Offset");
        d.Aerial.Direction = V(ae, "Direction");
        d.Aerial.Length = ae.Num("Length");
        d.Aerial.Stiffness = ae.Num("Stiffness");
        d.Aerial.Damping = ae.Num("Damping");

        if (f.Section("AI") is { } a)
            d.Ai = new AiData
            {
                UnderThresh = a.Num("UnderThresh"), UnderRange = a.Num("UnderRange"), UnderFront = a.Num("UnderFront"),
                UnderRear = a.Num("UnderRear"), UnderMax = a.Num("UnderMax"), OverThresh = a.Num("OverThresh"),
                OverRange = a.Num("OverRange"), OverMax = a.Num("OverMax"), OverAccThresh = a.Num("OverAccThresh"),
                OverAccRange = a.Num("OverAccRange"), PickupBias = a.Num("PickupBias"), BlockBias = a.Num("BlockBias"),
                OvertakeBias = a.Num("OvertakeBias"), Suspension = a.Num("Suspension"), Aggression = a.Num("Aggression")
            };
        if (f.Section("CAMATTACHED") is { } cam)
            d.Camera = new CameraData
            {
                HoodOffset = V(cam, "HoodOffset"), HoodLook = cam.Num("HoodLook"), RearOffset = V(cam, "RearOffset"),
                RearLook = cam.Num("RearLook"), FixedOffset = cam.Bool("FixedOffset", true), FixedLook = cam.Bool("FixedLook", true)
            };
        return d;
    }

    static string F(double d) => d.ToString("0.000000", CultureInfo.InvariantCulture);
    static string V3(Vector3 v) => F(v.X) + " " + F(v.Y) + " " + F(v.Z);
    static string B(bool v) => v ? "TRUE" : "FALSE";
    static string Q(string s) => "\"" + s + "\"";
    static string I(int v) => v.ToString(CultureInfo.InvariantCulture);
    static string QOpt(string s) => s == "" ? "" : Q(s);

    /// <summary>Every value as it goes into the file</summary>
    public List<ParamEntry> Emit()
    {
        var r = new List<ParamEntry>(256);
        void A(string b, string k, string v) => r.Add(new(b, k, v));
        void X(string b, string k, string v) => r.Add(new(b, k, v, true));

        A("", "Name", Q(Name));
        for (int i = 0; i < 19; i++) A("", "MODEL " + i, Q(Models[i]));
        A("", "TPAGE", Q(TPage));
        A("", "COLL", Q(Coll));
        A("", "EnvRGB", $"{EnvR} {EnvG} {EnvB}");
        X("", "TCARBOX", QOpt(TCarBox));
        X("", "TSHADOW", QOpt(TShadow));
        X("", "SHADOWINDEX", HasShadowIndex ? I(ShadowIndex) : "");
        X("", "SHADOWTABLE", HasShadowTable ? string.Join(" ", ShadowTable.Select(d => d.ToString("0.000", CultureInfo.InvariantCulture))) : "");
        X("", "SFXENGINE", QOpt(SfxEngine));
        X("", "SFXHONK", QOpt(SfxHonk));
        X("", "SFXSERVO", QOpt(SfxServo));
        A("", "BestTime", B(BestTime));
        A("", "Selectable", B(Selectable));
        X("", "CPUSelectable", B(CpuSelectable));
        X("", "Statistics", B(Statistics));
        A("", "Class", I(Class));
        A("", "Obtain", I(Obtain));
        A("", "Rating", I(Rating));
        A("", "TopEnd", F(TopEnd));
        A("", "Acc", F(Acc));
        A("", "Weight", F(Weight));
        A("", "Handling", F(Handling));
        A("", "Trans", I(Trans));
        A("", "MaxRevs", F(MaxRevs));
        A("", "SteerRate", F(SteerRate));
        A("", "SteerMod", F(SteerMod));
        A("", "EngineRate", F(EngineRate));
        A("", "TopSpeed", F(TopSpeed));
        A("", "DownForceMod", F(DownForceMod));
        A("", "CoM", V3(CoM));
        A("", "Weapon", V3(Weapon));
        X("", "WeaponOffset2", HasWeapon2 ? V3(Weapon2) : "");
        X("", "Flippable", B(Flippable));
        X("", "Flying", B(Flying));
        X("", "ClothFx", B(ClothFx));

        A("BODY", "ModelNum", I(BodyModel));
        A("BODY", "Offset", V3(BodyOffset));
        A("BODY", "Mass", F(BodyMass));
        A("BODY", "Inertia", V3(Inertia[0]));
        A("BODY", "Inertia#1", V3(Inertia[1]));
        A("BODY", "Inertia#2", V3(Inertia[2]));
        A("BODY", "Gravity", "2200");
        A("BODY", "Hardness", F(Hardness));
        A("BODY", "Resistance", F(Resistance));
        A("BODY", "AngRes", F(AngRes));
        A("BODY", "ResMod", F(ResMod));
        A("BODY", "Grip", F(BodyGrip));
        A("BODY", "StaticFriction", F(BodyStatic));
        A("BODY", "KineticFriction", F(BodyKinetic));

        for (int i = 0; i < 4; i++)
        {
            var s = "WHEEL " + i; var w = Wheels[i];
            A(s, "ModelNum", I(w.Model));
            A(s, "Offset1", V3(w.Offset1));
            A(s, "Offset2", V3(w.Offset2));
            A(s, "IsPresent", B(w.Present));
            A(s, "IsPowered", B(w.Powered));
            A(s, "IsTurnable", B(w.Turnable));
            A(s, "SteerRatio", F(w.SteerRatio));
            A(s, "EngineRatio", F(w.EngineRatio));
            A(s, "Radius", F(w.Radius));
            A(s, "Mass", F(w.Mass));
            A(s, "Gravity", "2200");
            A(s, "MaxPos", F(w.MaxPos));
            A(s, "SkidWidth", F(w.SkidWidth));
            A(s, "ToeIn", F(w.ToeIn));
            A(s, "AxleFriction", F(w.AxleFriction));
            A(s, "Grip", F(w.Grip));
            A(s, "StaticFriction", F(w.StaticFriction));
            A(s, "KineticFriction", F(w.KineticFriction));
            X(s, "Camber", F(w.Camber));
        }
        for (int i = 0; i < 4; i++)
        {
            var s = "SPRING " + i; var sp = Springs[i];
            A(s, "ModelNum", I(sp.Model));
            A(s, "Offset", V3(sp.Offset));
            A(s, "Length", F(sp.Length));
            A(s, "Stiffness", F(sp.Stiffness));
            A(s, "Damping", F(sp.Damping));
            A(s, "Restitution", F(sp.Restitution));
        }
        for (int i = 0; i < 4; i++)
        {
            var s = "PIN " + i;
            A(s, "ModelNum", I(Pins[i].Model));
            A(s, "Offset", V3(Pins[i].Offset));
            A(s, "Length", F(Pins[i].Length));
        }
        for (int i = 0; i < 4; i++)
        {
            var s = "AXLE " + i;
            A(s, "ModelNum", I(Axles[i].Model));
            A(s, "Offset", V3(Axles[i].Offset));
            A(s, "Length", F(Axles[i].Length));
        }
        A("SPINNER", "ModelNum", I(Spinner.Model));
        A("SPINNER", "Offset", V3(Spinner.Offset));
        A("SPINNER", "Axis", V3(Spinner.Axis));
        A("SPINNER", "AngVel", F(Spinner.AngVel));
        X("SPINNER", "Type", I(Spinner.Type));
        X("SPINNER", "Trans", V3(Spinner.Trans));
        X("SPINNER", "TransVel", F(Spinner.TransVel));
        A("AERIAL", "SecModelNum", I(Aerial.SecModel));
        A("AERIAL", "TopModelNum", I(Aerial.TopModel));
        A("AERIAL", "Offset", V3(Aerial.Offset));
        A("AERIAL", "Direction", V3(Aerial.Direction));
        A("AERIAL", "Length", F(Aerial.Length));
        A("AERIAL", "Stiffness", F(Aerial.Stiffness));
        A("AERIAL", "Damping", F(Aerial.Damping));
        if (Ai is { } a)
        {
            A("AI", "UnderThresh", F(a.UnderThresh)); A("AI", "UnderRange", F(a.UnderRange));
            A("AI", "UnderFront", F(a.UnderFront)); A("AI", "UnderRear", F(a.UnderRear));
            A("AI", "UnderMax", F(a.UnderMax)); A("AI", "OverThresh", F(a.OverThresh));
            A("AI", "OverRange", F(a.OverRange)); A("AI", "OverMax", F(a.OverMax));
            A("AI", "OverAccThresh", F(a.OverAccThresh)); A("AI", "OverAccRange", F(a.OverAccRange));
            A("AI", "PickupBias", F(a.PickupBias)); A("AI", "BlockBias", F(a.BlockBias));
            A("AI", "OvertakeBias", F(a.OvertakeBias)); A("AI", "Suspension", F(a.Suspension));
            A("AI", "Aggression", F(a.Aggression));
        }
        if (Camera is { } c)
        {
            A("CAMATTACHED", "HoodOffset", V3(c.HoodOffset));
            A("CAMATTACHED", "HoodLook", F(c.HoodLook));
            A("CAMATTACHED", "RearOffset", V3(c.RearOffset));
            A("CAMATTACHED", "RearLook", F(c.RearLook));
            A("CAMATTACHED", "FixedOffset", c.FixedOffset ? "true" : "false");
            A("CAMATTACHED", "FixedLook", c.FixedLook ? "true" : "false");
        }
        return r;
    }
}
