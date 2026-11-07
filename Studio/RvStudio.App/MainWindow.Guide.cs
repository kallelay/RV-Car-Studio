using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RvStudio.Core;

namespace RvStudio.App;

public sealed partial class MainWindow
{
    // ---------------------------- Guide: this car's numbers, worked out as you change them ----------------------------

    public Page GuideTab = null!;
    public TextBlock GuideSpeed = null!, GuideTryResult = null!;
    public Slider TryEngine = null!, TryTop = null!, TryMass = null!, TrySprings = null!, TryGrip = null!, TrySteer = null!;

    public void ShowGuide()
    {
        foreach (var item in Tabs.Items.OfType<TabItem>())
            if (item.Content == GuideTab) Tabs.SelectedItem = item;
    }

    static string Mph(float unitsPerSecond) => $"{CarCalc.ToMph(unitsPerSecond):0.0} mph, {CarCalc.ToMph(unitsPerSecond) * 1.609f:0.0} km/h";
    static string Cm(float units) => $"{units * CarCalc.CmPerUnit:0.0} cm";

    CarCalc Calc() => CarCalc.Of(Doc.D!, Doc.Hull);

    static string Springiness(float ratio) =>
        ratio < 0.2f ? "bouncy" : ratio < 0.5f ? "lively" : ratio < 0.9f ? "settled" : "slow to come back";

    Page GuidePage()
    {
        var p = NewPage();
        p.Note("The numbers of the car you have open, worked out from the formulas of the original game's car physics. They change as you edit. Point at a row for what it means.");

        p.Header("To look at");
        p.Info(() =>
        {
            var w = Calc().Warnings;
            return w.Count == 0 ? "Nothing stands out." : string.Join("\n", w.Select(x => "▲ " + x));
        });

        p.Header("Speed");
        GuideSpeed = p.Value("Real top speed", () => Mph(Calc().TopSpeedReal), "What the engine reaches against the air. The file's TopEnd is only the menu's bar: compare it here");
        p.Value("TopSpeed set", () => Mph(Calc().TopSpeedSet), "The engine gives nothing at this speed. The car stays below it");
        p.Value("In the file", () => $"TopEnd {CarCalc.ToMph((float)Doc.D!.TopEnd):0.0} mph, Acc {Doc.D.Acc:0.0} s", "What the car selection screen shows. Run, Fill stats, sets them");
        p.Value("From rest", () => $"{Calc().Accel0:0} units/s², {CarCalc.ToMph(Calc().Accel0):0.0} mph per second", "Pushing with all the powered wheels, before air and traction");
        p.Value("Time to top", () => $"{Calc().TimeToTop:0.0} s", "To 99.5% of top speed");
        p.Value("Engine responds", () => $"{Calc().EngineResponse:0.00} s", "Time from nothing to full throttle: 1 / EngineRate");
        p.Value("Wheelspin", () =>
        {
            var c = Calc();
            return c.WheelSpinFront && c.WheelSpinRear ? "All powered wheels at the start" : c.WheelSpinFront ? "Front wheels at the start" : c.WheelSpinRear ? "Rear wheels at the start" : "None: the tyres hold the engine";
        }, "When EngineRatio / Radius beats StaticFriction x load");

        p.Header("Weight and size");
        p.Value("Balance", () => $"{Calc().FrontShare * 100:0}% front, {(1 - Calc().FrontShare) * 100:0}% rear", "From where CoM sits between the axles");
        p.Value("Wheelbase, track", () => $"{Cm(Calc().Wheelbase)}, {Cm(Calc().Track)}", "1 unit is 0.5 cm");
        p.Value("Centre height", () => Cm(Calc().Height), "Centre of mass over the floor");

        p.Header("Suspension");
        p.Value("Pressed at rest", () => $"front {Calc().FrontSag:0.0} of {Calc().FrontTravel:0.0}, rear {Calc().RearSag:0.0} of {Calc().RearTravel:0.0} units", "The car's weight on Stiffness, against MaxPos. Past MaxPos it sits on the stops");
        p.Value("Bounce", () => $"front {Calc().FrontHz:0.0} Hz, rear {Calc().RearHz:0.0} Hz", "How fast it bounces. Goes up with Stiffness, down with Mass");
        p.Value("Damping", () => $"front {Calc().FrontDamping:0.00} {Springiness(Calc().FrontDamping)}, rear {Calc().RearDamping:0.00} {Springiness(Calc().RearDamping)}", "1 stops a bounce with no swing. Below 0.2 it keeps bouncing");

        p.Header("Grip and turning");
        p.Value("Sideways grip", () => $"front {Calc().GripFront:0.00} g, rear {Calc().GripRear:0.00} g", "StaticFriction: the most the tyres hold. Less at the rear than the front lets the back slide out");
        p.Value("Full grip at", () => $"{Mph(Calc().SlideFront)} sliding, front", "Grip grows with sideways speed until here: StaticFriction / (Grip x 0.336)");
        p.Value("Steering lock", () => $"{Calc().LockDeg:0} degrees", "SteerRatio is in radians");
        p.Value("Turns in, back", () => $"{Calc().TurnInTime:0.00} s, {Calc().StraightenTime:0.00} s", "SteerRate: straightening is 8 times quicker than turning in");
        p.Value("Tightest turn", () => $"{Cm(Calc().TurnRadius)} radius, {Mph(Calc().CornerSpeed)} at the limit", "Wheelbase over the steering angle");
        p.Value("Roll", () => Calc().RollIndex > 0 ? $"rolls above {Calc().RollIndex:0.00} g" : "", "Half the track over the height: it rolls if grip beats this");

        p.Header("Try it, nothing changes");
        p.Note("Move these to see what would happen. Use these numbers puts them on the car, with undo.");
        Action update = UpdateTry;
        TryEngine = p.Slide("Engine", 0.5, 2, 1, _ => update(), "EngineRatio");
        TryTop = p.Slide("TopSpeed", 0.5, 2, 1, _ => update());
        TryMass = p.Slide("Weight", 0.5, 2, 1, _ => update(), "Body Mass");
        TrySprings = p.Slide("Springs", 0.5, 2, 1, _ => update(), "Stiffness");
        TryGrip = p.Slide("Grip", 0.5, 2, 1, _ => update(), "Grip, StaticFriction, KineticFriction");
        TrySteer = p.Slide("Steering", 0.5, 2, 1, _ => update(), "SteerRatio");
        GuideTryResult = new TextBlock { TextWrapping = TextWrapping.Wrap, FontSize = 12, Margin = new Thickness(0, 4) };
        p.Add(GuideTryResult);
        p.Button("Use these numbers", ApplyTry);
        p.Button("Back to 1", ResetTry);

        p.Header("Units");
        UnitRow(p, "Speed", new[] { ("mph", 1.0), ("km/h", 1.609), ("units/s", CarCalc.UnitsPerMph) });
        UnitRow(p, "Length", new[] { ("units", 1.0), ("cm", 0.5), ("inches", 0.5 / 2.54) });

        p.Header("What each value does");
        var filter = new TextBox { Watermark = "Search", FontSize = 12 };
        p.Add(filter);
        var list = new StackPanel { Spacing = 7 };
        p.Add(list);
        var rows = new List<(string Text, Control Row)>();
        foreach (var (name, value, what) in Reference())
        {
            var g = new StackPanel();
            var head = new TextBlock { FontSize = 12, FontWeight = FontWeight.SemiBold };
            p.Live(() => head.Text = name + (Doc.D == null ? "" : "   " + value()));
            g.Children.Add(head);
            g.Children.Add(new TextBlock { Text = what, FontSize = 12, TextWrapping = TextWrapping.Wrap, Foreground = Page.Faint });
            list.Children.Add(g);
            rows.Add((name + " " + what, g));
        }
        filter.TextChanged += (_, _) =>
        {
            foreach (var (text, row) in rows)
                row.IsVisible = string.IsNullOrWhiteSpace(filter.Text) || text.Contains(filter.Text.Trim(), StringComparison.OrdinalIgnoreCase);
        };

        p.Header("Making a car");
        p.Note("Repaint: Paint tab, then save as a new skin.\nRemodel: copy a stock car, edit parts in the view, then Tools, Fit to the models.\nOriginal: New car, then Place wheels, Inertia from the body's shape, Make a hull, Make shadow, Shade the car, Make carbox.\nThen Drive here or Race for a first feel, and Test run in RVGL for the final word.");
        p.Header("Keys");
        p.Note("F1 this guide. F5 Test run. F6 Drive here. F7 Race.\nDriving: arrows or WASD, or an XInput gamepad: left stick steers with the game's curve, triggers or stick for throttle, X flips, Y puts you back. Space flips a car on its roof. R restarts, or in a race puts you back on the road. I lets the AI drive. Esc leaves.\nView: left drag turns, click selects, right drag turns, middle drag pans, wheel zooms. 1 2 3 4 views, F frames the car. W E R move, rotate, scale.");
        return p;
    }

    (string Name, Func<string> Value, string What)[] Reference()
    {
        string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);
        CarData D() => Doc.D!;
        WheelData W() => Doc.D!.Wheels[0];
        SpringData S() => Doc.D!.Springs[0];
        return new (string, Func<string>, string)[]
        {
            ("TopSpeed", () => F(D().TopSpeed) + " mph", "The engine's push fades to nothing at this speed. Not the speed the car reaches: air stops it earlier."),
            ("TopEnd, Acc, Weight, Handling", () => $"{F(D().TopEnd)}, {F(D().Acc)}, {F(D().Weight)}, {F(D().Handling)}", "Only the bars on the car selection screen. They change nothing in a race."),
            ("SteerRate", () => F(D().SteerRate), "How fast the steering follows your input. Turning in runs at half of it, straightening or changing side at 4 times."),
            ("EngineRate", () => F(D().EngineRate), "How fast the throttle follows your input. Going from forward to reverse first drops it to zero."),
            ("DownForceMod", () => F(D().DownForceMod), "Only when the car is on the two wheels of one side: pushes it back down, more with speed."),
            ("CoM", () => D().CoM.ToString(), "Centre of mass from the model's centre. Everything on the car, hull included, moves by this when the game loads it."),
            ("Body Mass", () => F(D().BodyMass), "The whole weight of the car. Wheel Mass doesn't add to it, it only moves the wheel in its suspension."),
            ("Inertia", () => $"{F(D().Inertia[0].X)}, {F(D().Inertia[1].Y)}, {F(D().Inertia[2].Z)}", "How hard it is to turn the car: pitch, spin, roll. Tools has Inertia from the body's shape."),
            ("Hardness", () => F(D().Hardness), "How much the body bounces when it hits the floor. 0 doesn't bounce."),
            ("Resistance", () => F(D().Resistance), "Air. Takes this x 120 of the speed each second, so it decides how far under TopSpeed the car stays."),
            ("AngRes, ResMod", () => $"{F(D().AngRes)}, {F(D().ResMod)}", "How quickly spinning dies down. With no wheel on the floor AngRes is multiplied by ResMod."),
            ("Body Grip, Friction", () => $"{F(D().BodyGrip)}, {F(D().BodyStatic)}, {F(D().BodyKinetic)}", "When the body itself slides on the floor, on its roof for example."),
            ("SteerRatio", () => F(W().SteerRatio), "Steering angle in radians at full lock. Negative at the front turns the right way. Each axle follows its left wheel."),
            ("EngineRatio", () => F(W().EngineRatio), "The wheel's push. Divide by Radius for the force, by Mass for the acceleration."),
            ("Radius", () => F(W().Radius), "Size of the wheel's collision sphere. A bigger wheel pushes less for the same EngineRatio."),
            ("MaxPos", () => F(W().MaxPos), "How far the wheel moves up and down from where it's placed."),
            ("AxleFriction", () => F(W().AxleFriction), "Slows the car when you let off the throttle. It also lets the car go a little over what the engine alone would give."),
            ("Wheel Grip", () => F(W().Grip), "How fast the sideways push grows with sliding speed. Big: reacts at once. Small: floaty."),
            ("StaticFriction", () => F(W().StaticFriction), "The most the tyre can hold, as a share of its load. Past it the tyre slides and also spins."),
            ("KineticFriction", () => F(W().KineticFriction), "What's left once it slides. Far under StaticFriction, and a slide is hard to catch."),
            ("Stiffness", () => F(S().Stiffness), "Spring force per unit pressed. Decides how much the car sits down and how fast it bounces."),
            ("Damping", () => F(S().Damping), "Slows the spring's movement, and stops the bounce."),
            ("Restitution", () => F(S().Restitution), "Negative softens a hit: how much of a bump the suspension takes away."),
            ("AI values", () => D().Ai == null ? "none" : "see the AI tab", "They only correct the CPU's driving when the car slides, and choose routes at a fork."),
            ("EnvRGB", () => $"{D().EnvR} {D().EnvG} {D().EnvB}", "Colour and strength of the reflection on the body. Uses the vertex normals."),
        };
    }

    string TryText(CarCalc c) =>
        $"{CarCalc.ToMph(c.TopSpeedReal):0.0} mph in {c.TimeToTop:0.0} s, pressed {c.FrontSag:0.0} of {c.FrontTravel:0.0}, grip {c.GripFront:0.00} g, lock {c.LockDeg:0} degrees";

    public CarData TriedCar() => CarCalc.Scaled(Doc.D!, (float)TryEngine.Value, (float)TryTop.Value, (float)TryMass.Value, (float)TrySprings.Value, (float)TryGrip.Value, (float)TrySteer.Value);

    void UpdateTry()
    {
        if (Doc.D == null || GuideTryResult == null || TrySteer == null) return;
        var after = CarCalc.Of(TriedCar(), Doc.Hull);
        GuideTryResult.Text = "Now: " + TryText(Calc()) + "\nThen: " + TryText(after) + (after.Warnings.Count > 0 ? "\n▲ " + after.Warnings[0] : "");
    }

    void ResetTry()
    {
        foreach (var s in new[] { TryEngine, TryTop, TryMass, TrySprings, TryGrip, TrySteer }) s.Value = 1;
        UpdateTry();
    }

    public void ApplyTry()
    {
        if (Doc.D == null) return;
        var t = TriedCar();
        Doc.Edit("Numbers from the Guide", () =>
        {
            var d = Doc.D!;
            d.TopSpeed = t.TopSpeed;
            d.BodyMass = t.BodyMass;
            for (int i = 0; i < 4; i++)
            {
                d.Wheels[i].EngineRatio = t.Wheels[i].EngineRatio;
                d.Wheels[i].Grip = t.Wheels[i].Grip;
                d.Wheels[i].StaticFriction = t.Wheels[i].StaticFriction;
                d.Wheels[i].KineticFriction = t.Wheels[i].KineticFriction;
                d.Wheels[i].SteerRatio = t.Wheels[i].SteerRatio;
                d.Springs[i].Stiffness = t.Springs[i].Stiffness;
            }
        });
        Doc.EndGesture();
        ResetTry();
        Say("Numbers put on the car. Undo takes them back");
    }

    // Type in any box, the others follow
    void UnitRow(Page p, string label, (string Name, double Factor)[] units)
    {
        var g = new Grid { ColumnDefinitions = new ColumnDefinitions(string.Join(",", units.Select(_ => "*"))) };
        var boxes = new TextBox[units.Length];
        bool busy = false;
        for (int i = 0; i < units.Length; i++)
        {
            int k = i;
            var b = new TextBox { Watermark = units[i].Name, FontSize = 12, MinHeight = 26, Height = 26, Padding = new Thickness(5, 2), Margin = new Thickness(0, 0, 3, 0), MinWidth = 0 };
            ToolTip.SetTip(b, units[i].Name);
            boxes[i] = b;
            Grid.SetColumn(b, i);
            g.Children.Add(b);
            b.TextChanged += (_, _) =>
            {
                if (busy) return;
                if (!double.TryParse((b.Text ?? "").Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)) return;
                busy = true;
                for (int o = 0; o < units.Length; o++)
                    if (o != k) boxes[o].Text = (v / units[k].Factor * units[o].Factor).ToString("0.##", CultureInfo.InvariantCulture);
                busy = false;
            };
        }
        p.Labeled(label + " (" + string.Join(", ", units.Select(u => u.Name)) + ")", g);
    }
}
