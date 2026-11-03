using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RvStudio.Core;
using System.Numerics;

namespace RvStudio.App;

public sealed partial class MainWindow
{
    void AddTab(string name, Control c, string? tip = null)
    {
        var t = new TabItem { Header = name, Content = c, FontSize = 13 };
        if (tip != null) ToolTip.SetTip(t, tip);
        Tabs.Items.Add(t);
    }

    Page NewPage() { var p = new Page(Doc); pages.Add(p); return p; }

    void BuildTabs()
    {
        AddTab("Selected", selectedHost, "The part you clicked in the 3D view");
        AddTab("Car", CarPage());
        AddTab("Handling", HandlingPage());
        AddTab("Menu", FrontendPage());
        AddTab("AI", AiPage());
        AddTab("RVGL", RvglPage());
        AddTab("Tools", ToolsPage());
        AddTab("Run", RunPage());
        var rawHost = new DockPanel();
        var hint = new TextBlock { Text = "The file as it is. Changes reach the car a moment after you stop typing.", FontSize = 12, Foreground = Page.Faint, Margin = new Thickness(8, 6), TextWrapping = TextWrapping.Wrap };
        DockPanel.SetDock(hint, Dock.Top);
        rawHost.Children.Add(hint);
        rawHost.Children.Add(rawText);
        rawText.TextChanged += (_, _) =>
        {
            if (rawText.Text == rawLast) return;
            rawTimer.Stop(); rawTimer.Start();
        };
        AddTab("File", rawHost);
        Tabs.SelectionChanged += (_, _) =>
        {
            if (Tabs.SelectedItem is TabItem { Content: Page p }) p.Refresh();
            if (Tabs.SelectedItem is TabItem { Content: DockPanel } && rawDirty) RefreshRaw();
        };
    }

    void BuildGameDependent()
    {
        levelBox.ItemsSource = Doc.Game == null ? null : new TestRun(Doc.Game).Levels().Select(l => l.Name + "  (" + l.Folder + ")").ToList();
        levelFolders = Doc.Game == null ? new List<string>() : new TestRun(Doc.Game).Levels().Select(l => l.Folder).ToList();
    }

    // ---------------------------- The part you clicked ----------------------------

    Page BuildSelectedPage()
    {
        var p = new Page(Doc);
        var s = Doc.Sel;
        CarData D() => Doc.D!;
        p.Header(s.Label);
        switch (s.Kind)
        {
            case SelKind.None:
                p.Note("Click a part of the car in the 3D view, or one of the crosses: centre of mass, weapons, aerial, cameras, suspension mounts.");
                break;
            case SelKind.Body:
                p.Int("Model slot", () => D().BodyModel, v => D().BodyModel = v, "Which line of the MODEL list", -1, 18);
                ModelFile(p, () => D().BodyModel);
                p.Vec("Position", () => D().BodyOffset, v => D().BodyOffset = v, "Offset of the model. X right, Y down, Z forward");
                p.Num("Mass", () => D().BodyMass, v => D().BodyMass = v, "Weight in kg. R6 Turbo 0.8, Bertha 3", 0.01);
                p.Vec("Inertia", () => D().Inertia[0], v => D().Inertia[0] = v, "First line of the matrix. Higher: harder to turn around that axis");
                p.Vec("", () => D().Inertia[1], v => D().Inertia[1] = v);
                p.Vec("", () => D().Inertia[2], v => D().Inertia[2] = v);
                p.Button("Compute inertia from the shape", () => Tool("Inertia", () => CarTools.ComputeInertia(Doc.Car!)));
                p.Num("Hardness", () => D().Hardness, v => D().Hardness = v, "How much the body bounces when it hits something");
                p.Num("Air resistance", () => D().Resistance, v => D().Resistance = v, "Linear");
                p.Num("Angular res.", () => D().AngRes, v => D().AngRes = v);
                p.Num("In the air", () => D().ResMod, v => D().ResMod = v, "Angular resistance scale when in the air");
                p.Num("Grip", () => D().BodyGrip, v => D().BodyGrip = v, "Turns downforce into friction");
                p.Num("Static friction", () => D().BodyStatic, v => D().BodyStatic = v, "Sliding against walls, on its roof...");
                p.Num("Kinetic friction", () => D().BodyKinetic, v => D().BodyKinetic = v);
                break;
            case SelKind.Wheel:
                {
                    int i = s.Index;
                    WheelData W() => D().Wheels[i];
                    p.Check("Present", () => W().Present, v => W().Present = v);
                    p.Check("Powered", () => W().Powered, v => W().Powered = v);
                    p.Check("Steers", () => W().Turnable, v => W().Turnable = v);
                    p.Int("Model slot", () => W().Model, v => W().Model = v, null, -1, 18);
                    ModelFile(p, () => W().Model);
                    p.Vec("Position", () => W().Offset1, v => W().Offset1 = v, "Centre of the visible wheel");
                    p.Vec("Contact shift", () => W().Offset2, v => W().Offset2 = v, "The collision sphere from the visible wheel. Outwards: wider stance than it looks");
                    p.Num("Radius", () => W().Radius, v => W().Radius = v, "Of the collision sphere. Should match the model");
                    p.Num("Mass", () => W().Mass, v => W().Mass = v);
                    p.Num("Steer ratio", () => W().SteerRatio, v => W().SteerRatio = v, "0.25 is about 45 degrees. Negative at the front");
                    p.Num("Engine ratio", () => W().EngineRatio, v => W().EngineRatio = v, "How much torque this wheel gets");
                    p.Num("Travel", () => W().MaxPos, v => W().MaxPos = v, "Up and down from rest");
                    p.Num("Skid width", () => W().SkidWidth, v => W().SkidWidth = v);
                    p.Num("Toe in", () => W().ToeIn, v => W().ToeIn = v);
                    p.Num("Axle friction", () => W().AxleFriction, v => W().AxleFriction = v, "Slows the wheel. Keep it low");
                    p.Num("Grip", () => W().Grip, v => W().Grip = v);
                    p.Num("Static friction", () => W().StaticFriction, v => W().StaticFriction = v);
                    p.Num("Kinetic friction", () => W().KineticFriction, v => W().KineticFriction = v, "When it slides");
                    p.Num("Camber", () => W().Camber, v => W().Camber = v, "Degrees, RVGL. Negative: the bottom of the wheel goes out", -90, 90);
                    p.Button("Same values on all four wheels", () => Tool("Copy wheel", () =>
                    {
                        for (int k = 0; k < 4; k++)
                            if (k != i)
                            {
                                var o = W().Clone();
                                o.Model = D().Wheels[k].Model;
                                o.Offset1 = D().Wheels[k].Offset1;
                                o.Offset2 = D().Wheels[k].Offset2;
                                o.SteerRatio = D().Wheels[k].SteerRatio;
                                o.Turnable = D().Wheels[k].Turnable;
                                o.Powered = D().Wheels[k].Powered;
                                D().Wheels[k] = o;
                            }
                    }), "Keeps each wheel's place, model, steering and drive");
                    break;
                }
            case SelKind.Spring:
                {
                    int i = s.Index;
                    SpringData S() => D().Springs[i];
                    p.Int("Model slot", () => S().Model, v => S().Model = v, null, -1, 18);
                    ModelFile(p, () => S().Model);
                    p.Vec("Mount", () => S().Offset, v => S().Offset = v, "Where it's fixed to the chassis");
                    p.Num("Length", () => S().Length, v => S().Length = v, "Of the model. The game stretches it: distance to the wheel / this");
                    p.Num("Stiffness", () => S().Stiffness, v => S().Stiffness = v);
                    p.Num("Damping", () => S().Damping, v => S().Damping = v);
                    p.Num("Rest height", () => S().Restitution, v => S().Restitution = v, "-1 fully compressed at rest, 0 not at all");
                    break;
                }
            case SelKind.Axle:
                {
                    int i = s.Index;
                    p.Int("Model slot", () => D().Axles[i].Model, v => D().Axles[i].Model = v, null, -1, 18);
                    ModelFile(p, () => D().Axles[i].Model);
                    p.Vec("Mount", () => D().Axles[i].Offset, v => D().Axles[i].Offset = v);
                    p.Num("Length", () => D().Axles[i].Length, v => D().Axles[i].Length = v, "Of the model. The game stretches it to reach the wheel");
                    break;
                }
            case SelKind.Pin:
                {
                    int i = s.Index;
                    p.Int("Model slot", () => D().Pins[i].Model, v => D().Pins[i].Model = v, null, -1, 18);
                    ModelFile(p, () => D().Pins[i].Model);
                    p.Num("Length", () => D().Pins[i].Length, v => D().Pins[i].Length = v, "Multiplies the model, usually negative. Pins start at the spring's mount and don't stretch");
                    break;
                }
            case SelKind.Spinner:
                p.Int("Model slot", () => D().Spinner.Model, v => D().Spinner.Model = v, null, -1, 18);
                ModelFile(p, () => D().Spinner.Model);
                p.Vec("Position", () => D().Spinner.Offset, v => D().Spinner.Offset = v);
                p.Vec("Axis", () => D().Spinner.Axis, v => D().Spinner.Axis = v);
                p.Num("Speed", () => D().Spinner.AngVel, v => D().Spinner.AngVel = v, "0 means it doesn't turn");
                p.Choice("Type", new[] { "0: nothing", "1: turns", "2: turns with steering", "4: moves with speed", "6: both" },
                    () => D().Spinner.Type switch { 0 => 0, 1 => 1, 2 => 2, 4 => 3, 6 => 4, _ => 1 },
                    v => D().Spinner.Type = new[] { 0, 1, 2, 4, 6 }[v]);
                p.Vec("Translation", () => D().Spinner.Trans, v => D().Spinner.Trans = v, "How far it moves with speed");
                p.Num("Trans. speed", () => D().Spinner.TransVel, v => D().Spinner.TransVel = v);
                break;
            case SelKind.AerialBase:
            case SelKind.AerialTip:
                p.Int("Section model", () => D().Aerial.SecModel, v => D().Aerial.SecModel = v, null, -1, 18);
                p.Int("Top model", () => D().Aerial.TopModel, v => D().Aerial.TopModel = v, null, -1, 18);
                p.Vec("Base", () => D().Aerial.Offset, v => D().Aerial.Offset = v);
                p.Vec("Direction", () => D().Aerial.Direction, v => D().Aerial.Direction = v, "Y goes down, so straight up is 0 -1 0");
                p.Num("Length", () => D().Aerial.Length, v => D().Aerial.Length = v, "Each section of the physics. The whole aerial is about 2.5 x this", 0);
                p.Num("Stiffness", () => D().Aerial.Stiffness, v => D().Aerial.Stiffness = v, "About 2000 on stock cars");
                p.Num("Damping", () => D().Aerial.Damping, v => D().Aerial.Damping = v, "About 5. 0 wiggles forever");
                p.Button("Straight up", () => Tool("Aerial", () => D().Aerial.Direction = new Vector3(0, -1, 0)));
                break;
            case SelKind.CoM:
                p.Vec("Position", () => D().CoM, v => D().CoM = v, "From the model centre");
                p.Note("Y goes down: positive is a low centre, hard to roll over. Keep X at 0 unless you want it to pull.");
                break;
            case SelKind.Weapon1:
                p.Vec("Position", () => D().Weapon, v => D().Weapon = v, "Where shockwaves and fireworks start");
                break;
            case SelKind.Weapon2:
                p.Vec("Position", () => D().Weapon2, v => { D().Weapon2 = v; D().HasWeapon2 = true; }, "Where water bombs start");
                break;
            case SelKind.HoodCam:
            case SelKind.RearCam:
                p.Note("See the RVGL tab for all camera values.");
                break;
        }
        return p;
    }

    void ModelFile(Page p, Func<int> slot)
    {
        p.Info(() =>
        {
            var n = slot();
            if (n < 0 || n > 18 || Doc.D == null) return "No model";
            var m = Doc.Model(n);
            return Doc.D.Models[n] + (m != null ? $"  ({m.Faces.Length} polygons)" : "  (not found)");
        });
    }

    // ---------------------------- Car ----------------------------

    Page CarPage()
    {
        var p = NewPage();
        CarData D() => Doc.D!;
        p.Header("The car");
        p.Text("Name", () => D().Name, v => D().Name = v);
        p.Info(() => "Folder: cars\\" + (Doc.Car?.Folder ?? ""));
        p.Header("Files");
        p.Text("Texture", () => D().TPage, v => { D().TPage = v; Doc.TextureVersion++; }, "From the Re-Volt folder");
        p.Info(() =>
        {
            if (Doc.Car == null) return "";
            var skins = Doc.Car.FindSkins();
            return skins.Count > 1 ? "Skins: " + string.Join(", ", skins.Select(Path.GetFileNameWithoutExtension)) : "No other skins";
        });
        var skinRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
        var prev = new Button { Content = "Next skin", FontSize = 12, Padding = new Thickness(8, 3) };
        prev.Click += (_, _) =>
        {
            if (Doc.Car == null) return;
            var skins = Doc.Car.FindSkins();
            if (skins.Count == 0) return;
            var cur = Doc.SkinOverride ?? Doc.Car.Game.Find(Doc.D!.TPage);
            int i = skins.FindIndex(s => string.Equals(s, cur, StringComparison.OrdinalIgnoreCase));
            Doc.SkinOverride = skins[(i + 1) % skins.Count];
            Say("Showing " + Path.GetFileName(Doc.SkinOverride));
            View.Redraw();
        };
        skinRow.Children.Add(prev);
        p.Add(skinRow);
        p.Text("Hull", () => D().Coll, v => { D().Coll = v; LoadHull(); }, "Collision hull, .hul");
        p.Text("Carbox", () => D().TCarBox, v => D().TCarBox = v);
        p.Header("Model list");
        for (int i = 0; i < 19; i++)
        {
            int k = i;
            p.Text("Model " + k, () => D().Models[k], v => { D().Models[k] = v; Doc.ReloadModels(); });
        }
        p.Header("Reflection");
        p.Int("EnvRGB red", () => D().EnvR, v => D().EnvR = v, "How much the body reflects the world", 0, 255);
        p.Int("Green", () => D().EnvG, v => D().EnvG = v, null, 0, 255);
        p.Int("Blue", () => D().EnvB, v => D().EnvB = v, null, 0, 255);
        return p;
    }

    Page HandlingPage()
    {
        var p = NewPage();
        CarData D() => Doc.D!;
        p.Header("Engine and steering");
        p.Num("Top speed", () => D().TopSpeed, v => D().TopSpeed = v, "mph, in theory. The car usually reaches 1 to 3 less");
        p.Num("Engine rate", () => D().EngineRate, v => D().EngineRate = v, "How fast the throttle responds. Above 4.5 it hardly matters");
        p.Num("Steer rate", () => D().SteerRate, v => D().SteerRate = v, "How fast the wheels reach where you steer");
        p.Num("Steer mod", () => D().SteerMod, v => D().SteerMod = v);
        p.Num("Downforce", () => D().DownForceMod, v => D().DownForceMod = v, "Tendency to stay on the floor. 2 is about average");
        p.Vec("Centre of mass", () => D().CoM, v => D().CoM = v, "You can drag it in the 3D view too");
        p.Header("Suspension, all springs");
        p.Num("Stiffness", () => D().Springs[0].Stiffness, v => { foreach (var s in D().Springs) s.Stiffness = v; });
        p.Num("Damping", () => D().Springs[0].Damping, v => { foreach (var s in D().Springs) s.Damping = v; });
        p.Num("Rest height", () => D().Springs[0].Restitution, v => { foreach (var s in D().Springs) s.Restitution = v; });
        p.Header("Drive, all wheels");
        p.Num("Grip", () => D().Wheels[0].Grip, v => { foreach (var w in D().Wheels) w.Grip = v; });
        p.Num("Static friction", () => D().Wheels[0].StaticFriction, v => { foreach (var w in D().Wheels) w.StaticFriction = v; });
        p.Num("Kinetic friction", () => D().Wheels[0].KineticFriction, v => { foreach (var w in D().Wheels) w.KineticFriction = v; });
        p.Choice("Drive", new[] { "All wheels", "Front wheels", "Back wheels" },
            () => D().Wheels[0].Powered && D().Wheels[2].Powered ? 0 : D().Wheels[0].Powered ? 1 : 2,
            v =>
            {
                for (int i = 0; i < 4; i++) D().Wheels[i].Powered = v == 0 || (v == 1) == (i < 2);
                D().Trans = v;
            });
        return p;
    }

    Page FrontendPage()
    {
        var p = NewPage();
        CarData D() => Doc.D!;
        p.Header("In the game's menus");
        p.Check("Players can pick it", () => D().Selectable, v => D().Selectable = v);
        p.Check("CPU cars can pick it", () => D().CpuSelectable, v => D().CpuSelectable = v);
        p.Check("Best times count", () => D().BestTime, v => D().BestTime = v);
        p.Check("Show stat bars", () => D().Statistics, v => D().Statistics = v, "Speed, acceleration and weight bars");
        p.Choice("Class", CarTools.ClassNames, () => D().Class, v => D().Class = v, "Also how a custom engine sound is used");
        p.Choice("Rating", CarTools.RatingNames, () => D().Rating, v => D().Rating = v, "Super Pro is RVGL only");
        p.Choice("Unlocked", new[] { "Carnival only", "Always", "Championship", "Time trial", "Practice", "Single races", "Training / Stunt Arena" },
            () => D().Obtain + 1, v => D().Obtain = v - 1);
        p.Header("Stat bars");
        p.Num("Top end", () => D().TopEnd, v => D().TopEnd = v, "Top speed it really reaches, in game units/s. 89.4 is 1 mph");
        p.Num("Acceleration", () => D().Acc, v => D().Acc = v, "Seconds to top speed");
        p.Num("Weight", () => D().Weight, v => D().Weight = v);
        p.Num("Handling", () => D().Handling, v => D().Handling = v, "Your own opinion, 0 to 100");
        p.Int("Trans", () => D().Trans, v => D().Trans = v, "0 all wheels, 1 front, 2 back");
        p.Num("Max revs", () => D().MaxRevs, v => D().MaxRevs = v);
        p.Button("Fill from a simulated run", () => Tool("Stats", () => CarSim.FillStats(Doc.D!)), "A close estimate. RVGL's dev mode has the exact Calc Car Stats");
        return p;
    }

    Page AiPage()
    {
        var p = NewPage();
        AiData A() { Doc.D!.Ai ??= new AiData(); return Doc.D!.Ai; }
        p.Check("CPU cars can pick it", () => Doc.D!.CpuSelectable, v => Doc.D!.CpuSelectable = v);
        p.Header("Understeer");
        p.Num("Threshold", () => A().UnderThresh, v => A().UnderThresh = v);
        p.Num("Range", () => A().UnderRange, v => A().UnderRange = v);
        p.Num("Front", () => A().UnderFront, v => A().UnderFront = v);
        p.Num("Rear", () => A().UnderRear, v => A().UnderRear = v);
        p.Num("Max", () => A().UnderMax, v => A().UnderMax = v);
        p.Header("Oversteer");
        p.Num("Threshold", () => A().OverThresh, v => A().OverThresh = v);
        p.Num("Range", () => A().OverRange, v => A().OverRange = v);
        p.Num("Max", () => A().OverMax, v => A().OverMax = v);
        p.Num("Acc. threshold", () => A().OverAccThresh, v => A().OverAccThresh = v);
        p.Num("Acc. range", () => A().OverAccRange, v => A().OverAccRange = v);
        p.Header("Behaviour, 0 to 32767");
        p.Num("Pickups", () => A().PickupBias, v => A().PickupBias = v, null, 0, 32767);
        p.Num("Blocking", () => A().BlockBias, v => A().BlockBias = v, null, 0, 32767);
        p.Num("Overtaking", () => A().OvertakeBias, v => A().OvertakeBias = v, null, 0, 32767);
        p.Num("Suspension", () => A().Suspension, v => A().Suspension = v, null, 0, 32767);
        p.Num("Aggression", () => A().Aggression, v => A().Aggression = v, null, 0, 32767);
        return p;
    }

    Page RvglPage()
    {
        var p = NewPage();
        CarData D() => Doc.D!;
        p.Header("Special effects");
        p.Check("Flippable (Rotor)", () => D().Flippable, v => D().Flippable = v);
        p.Check("Flying (UFO)", () => D().Flying, v => D().Flying = v, "Lets flying CPU cars steer in the air");
        p.Check("Cloth (Mystery)", () => D().ClothFx, v => D().ClothFx = v, "Needs cloth polygons flagged in the model");
        p.Header("Sounds");
        p.Text("Engine", () => D().SfxEngine, v => D().SfxEngine = v, "WAV. How it's used depends on the car's class");
        p.Text("Horn", () => D().SfxHonk, v => D().SfxHonk = v);
        p.Text("Servo", () => D().SfxServo, v => D().SfxServo = v);
        p.Header("Shadow");
        p.Text("Own texture", () => D().TShadow, v => D().TShadow = v);
        p.Int("Stock index", () => D().ShadowIndex, v => { D().ShadowIndex = v; D().HasShadowIndex = true; }, "0 to 27 on cars\\misc\\shadow.bmp, -1 a rectangle. Ignored with an own texture", -1, 27);
        var names = new[] { "Left", "Right", "Front", "Back", "Height" };
        for (int i = 0; i < 5; i++)
        {
            int k = i;
            p.Num(names[k], () => D().ShadowTable[k], v => { D().ShadowTable[k] = v; D().HasShadowTable = true; });
        }
        p.Button("Fit around the car", () => Tool("Shadow table", () => CarTools.FitShadowTable(Doc.Car!)));
        p.Header("Hood and rear cameras");
        p.Check("Use own cameras", () => D().Camera != null, v => D().Camera = v ? new CameraData() : null);
        CameraData C() => D().Camera ??= new CameraData();
        p.Vec("Hood place", () => D().Camera?.HoodOffset ?? default, v => C().HoodOffset = v);
        p.Num("Hood look", () => D().Camera?.HoodLook ?? 0, v => C().HoodLook = v, "-0.25 to 0.25, 0 is straight", -0.25, 0.25);
        p.Vec("Rear place", () => D().Camera?.RearOffset ?? default, v => C().RearOffset = v);
        p.Num("Rear look", () => D().Camera?.RearLook ?? 0, v => C().RearLook = v, null, -0.25, 0.25);
        p.Check("Fixed place", () => D().Camera?.FixedOffset ?? true, v => C().FixedOffset = v);
        p.Check("Fixed look", () => D().Camera?.FixedLook ?? true, v => C().FixedLook = v);
        p.Header("Weapons");
        p.Vec("Shockwave", () => D().Weapon, v => D().Weapon = v);
        p.Vec("Water bomb", () => D().Weapon2, v => { D().Weapon2 = v; D().HasWeapon2 = true; });
        return p;
    }

    Page ToolsPage()
    {
        var p = NewPage();
        p.Header("Auto shade");
        p.Note("Soft light with the shadows of the car's own shape, baked in the vertex colours, the way the game shows them.");
        var amb = p.Slide("Sky light", 0, 1, 0.45);
        var sun = p.Slide("Sun", 0, 1, 0.55);
        var occ = p.Slide("Dark corners", 0, 1, 1);
        p.Button("Shade the car", () => DoAutoShade((float)amb.Value, (float)sun.Value, (float)occ.Value), "See it, then Save. The first version of each model stays as .bak", true);

        p.Header("Shadow");
        p.Button("Make shadow.bmp from the car", () => Tool("Shadow", () => Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!))), "Top view, white on black, and the shadow table around it");

        p.Header("Carbox");
        p.Button("Make carbox.bmp", async () => await MakeCarbox(), "A picture of the car with its name, on white");

        p.Header("Collision");
        p.Button("Make a hull from the body", MakeHull, "One convex part and spheres inside. For a first drive: a hull made by hand fits better");

        p.Header("Fit to the models");
        p.Button("Radius, skid width, spring and axle length", () => Tool("Fit", () => CarTools.FitToModels(Doc.Car!)));
        p.Button("Inertia from the body's shape", () => Tool("Inertia", () => CarTools.ComputeInertia(Doc.Car!)));
        p.Button("Place wheels on a new body", () => Tool("Place wheels", () => CarTools.PlaceWheels(Doc.Car!)));
        p.Button("Make right side like the left", () => Tool("Symmetry", () => CarTools.Symmetrize(Doc.D!)));

        p.Header("Texture");
        var bright = p.Slide("Brightness", -0.6, 0.6, 0);
        var satur = p.Slide("Colour", 0, 2, 1);
        var hue = p.Slide("Hue", -180, 180, 0);
        p.Button("Save as a new skin", SaveSkin(bright, satur, hue), "Makes car-<name>.bmp, RVGL lists it as a skin. The original stays");

        p.Header("Readme");
        var author = new TextBox { Watermark = "Your name", FontSize = 12, Text = Settings.Author };
        var comments = new TextBox { Watermark = "Comments", FontSize = 12, AcceptsReturn = true, Height = 60, TextWrapping = TextWrapping.Wrap };
        p.Add(author);
        p.Add(comments);
        p.Button("Write readme.txt", () =>
        {
            if (Doc.Car == null) return;
            var tpl = Path.Combine(AppContext.BaseDirectory, "data", "readme", "Template1.txt");
            if (!File.Exists(tpl)) { Say("Template not found"); return; }
            Settings.Author = author.Text ?? "";
            var text = CarTools.FillReadme(File.ReadAllText(tpl, ParamFile.FileEncoding), Doc.Car, new(author.Text ?? "", Comments: comments.Text ?? ""));
            var file = Path.Combine(Doc.Car.Dir, "readme.txt");
            Files.Backup(file);
            File.WriteAllText(file, text, ParamFile.FileEncoding);
            Say("Written: " + file);
        });
        p.Button("Pack as zip...", async () => await PackZip());
        return p;
    }

    Action SaveSkin(Slider bright, Slider satur, Slider hue) => () =>
    {
        if (Doc.Car == null || Doc.Car.Game.Find(Doc.D!.TPage) is not { } tex) { Say("No texture"); return; }
        using var src = Images.Load(tex);
        if (src == null) return;
        using var o = CarTools.AdjustTexture(src, (float)bright.Value, (float)satur.Value, (float)hue.Value);
        var stem = Path.GetFileNameWithoutExtension(tex);
        var dir = Path.GetDirectoryName(tex)!;
        int n = 1;
        string path;
        do path = Path.Combine(dir, stem + "-alt" + n++ + ".bmp"); while (File.Exists(path));
        Images.SaveBmp24(o, path);
        Doc.SkinOverride = path;
        Say("Skin saved: " + Path.GetFileName(path));
        View.Redraw();
    };

    readonly ComboBox levelBox = new() { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    List<string> levelFolders = new();
    readonly TextBlock runReport = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };

    Page RunPage()
    {
        var p = NewPage();
        p.Header("In the real game");
        p.Note("Saves the car and starts RVGL in a window with this car picked. It uses its own settings folder, your profile and best times stay as they are.");
        levelBox.SelectionChanged += (_, _) => SelectedLevel = levelBox.SelectedIndex >= 0 && levelBox.SelectedIndex < levelFolders.Count ? levelFolders[levelBox.SelectedIndex] : null;
        p.Add(new TextBlock { Text = "Track", FontSize = 12 });
        p.Add(levelBox);
        p.Button("▶  Test run (F5)", () => TestRunGame(false), null, true);
        p.Button("Test run, dev mode", () => TestRunGame(true), "Ctrl+R in the game reloads the car after you save here. Start Race has Calc Car Stats");

        p.Header("Quick drive, here");
        p.Note("A simulation from the parameters: good to feel a change at once, not the same as the game. Arrows or WASD, R restarts, Esc leaves.");
        p.Button("Drive on a flat floor (F6)", () => StartDrive(SimGround.Flat));
        p.Button("Drive on bumps", () => StartDrive(SimGround.Bumps));
        p.Button("Drive to a ramp", () => StartDrive(SimGround.Ramp));

        p.Header("Automatic runs");
        p.Button("Run them", () =>
        {
            if (Doc.D == null) return;
            var d = Doc.D.Clone();
            var a = CarSim.RunAcceleration(d);
            runReport.Text = $"{a.Note}\nIn the file: TopEnd {d.TopEnd:0}, Acc {d.Acc:0.0}\nSimulated: TopEnd {a.TopEnd:0}, Acc {a.Acc:0.0}\n{CarSim.RunCircle(d).Note}\n{CarSim.RunBumps(d).Note}";
        }, "Full throttle, hard turn, bumps");
        p.Add(runReport);
        return p;
    }
}
