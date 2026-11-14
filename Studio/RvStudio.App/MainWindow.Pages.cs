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
        PaintTabIndex = Tabs.ItemCount;
        AddTab("Paint", PaintPage(), "Colours of the car, seen live");
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
        GuideTab = GuidePage();
        AddTab("Guide", GuideTab, "This car's numbers, worked out as you change them (F1)");
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
        stockAi = null;
        if (aiFrom != null && Doc.Game != null) aiFrom.ItemsSource = Doc.Game.CarFolders().Where(GameFolder.IsStock).ToList();
    }

    // ---------------------------- The part you clicked ----------------------------

    Page BuildSelectedPage()
    {
        var p = new Page(Doc);
        var s = Doc.Sel;
        CarData D() => Doc.D!;
        p.Header(s.Label);
        if (s.IsHull)
        {
            HullPage(p, s);
            return p;
        }
        if (Doc.HasModel(s) || s.Kind is SelKind.AerialBase or SelKind.AerialTip)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,*,4,*") };
            var replace = new Button { Content = "Replace...", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
            ToolTip.SetTip(replace, "Use another model file. From outside the game folder it's copied into the car's folder");
            replace.Click += async (_, _) => await PickModelFor(s);
            var hide = new Button { Content = Doc.Hidden.Contains(s) ? "Show" : "Hide", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
            ToolTip.SetTip(hide, "Only in the view, to reach what's behind it. The file doesn't change");
            hide.Click += (_, _) =>
            {
                Doc.ToggleHidden(s);
                hide.Content = Doc.Hidden.Contains(s) ? "Show" : "Hide";
                FillParts();
            };
            var remove = new Button { Content = "Remove", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
            ToolTip.SetTip(remove, "Take it off the car. Follows the corner mode, undo brings it back");
            remove.Click += (_, _) => Doc.RemovePart(s);
            Grid.SetColumn(hide, 2);
            Grid.SetColumn(remove, 4);
            row.Children.Add(replace);
            row.Children.Add(hide);
            row.Children.Add(remove);
            p.Add(row);
        }
        switch (s.Kind)
        {
            case SelKind.None:
                p.Note("Click a part of the car in the 3D view, or one of the crosses: centre of mass, weapons, aerial, cameras, suspension mounts.");
                break;
            case SelKind.Body:
                p.Check("Hull follows the body", () => Doc.HullFollowsBody, v => Doc.HullFollowsBody = v, "Moving, turning or sizing the body does the same to the collision hull");
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
                p.Num("Length", () => D().Aerial.Length, v => D().Aerial.Length = v, "Of the whole aerial, from base to tip", 0);
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
        p.Note("Stock cars come from packs. Saved changes to them go in packs\\local, on top: File has the way back.");
        p.Text("Hull", () => D().Coll, v => { D().Coll = v; Doc.LoadHull(); }, "Collision hull, .hul");
        p.Text("Carbox", () => D().TCarBox, v => D().TCarBox = v);
        p.Header("Model list");
        for (int i = 0; i < 19; i++)
        {
            int k = i;
            p.TextWithButton("Model " + k, () => D().Models[k], v => { D().Models[k] = v; Doc.ReloadModels(); }, "Pick", async () =>
            {
                var rel = await AskModel("Model " + k);
                if (rel == null) return;
                Doc.Edit("Model " + k, () => { D().Models[k] = rel == "" ? "NONE" : rel; Doc.ReloadModels(); });
                Doc.EndGesture();
            });
        }
        p.Header("Reflection");
        p.ColourOf("Reflection colour", () => Avalonia.Media.Color.FromRgb((byte)Math.Clamp(D().EnvR, 0, 255), (byte)Math.Clamp(D().EnvG, 0, 255), (byte)Math.Clamp(D().EnvB, 0, 255)),
            c => { D().EnvR = c.R; D().EnvG = c.G; D().EnvB = c.B; }, "Colour and strength of what the body reflects. Black: none");
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
        p.Button("Fill from a simulated run", () => Tool("Stats", () => CarSim.FillStats(Doc.D!, Doc.Hull)), "A close estimate. RVGL's dev mode has the exact Calc Car Stats");
        return p;
    }

    Page AiPage()
    {
        var p = NewPage();
        AiData A() { Doc.D!.Ai ??= new AiData(); return Doc.D!.Ai; }
        p.Check("CPU cars can pick it", () => Doc.D!.CpuSelectable, v => Doc.D!.CpuSelectable = v);
        p.Header("What this is");
        p.Note("How the computer drives this car, from the original game's code. The Under and Over rows correct its steering and throttle when the car slides. Speeds are in game units per second, 89 is about 1 mph. RVGL may have changed some of this.");
        void Row(string label, Func<AiData, double> f, Action<double> set, string guess, double min = double.MinValue, double max = double.MaxValue)
        {
            p.Num(label, () => f(A()), set, guess, min, max);
            p.Hint(() => AiRange(f, Doc.D!.Ai == null ? 0 : f(Doc.D.Ai)));
        }
        p.Header("Understeer: the front won't turn");
        Row("Threshold", a => a.UnderThresh, v => A().UnderThresh = v, "Sideways speed of front and rear axles added together. Above this the CPU steers harder");
        Row("Range", a => a.UnderRange, v => A().UnderRange = v, "Speed over the threshold that makes a full correction of 1");
        Row("Front", a => a.UnderFront, v => A().UnderFront = v, "If the front slides sideways faster than this, and the rear faster than Rear, it stays on the throttle. Otherwise it lifts by the correction");
        Row("Rear", a => a.UnderRear, v => A().UnderRear = v, "The rear half of the Front test");
        Row("Max", a => a.UnderMax, v => A().UnderMax = v, "Largest correction, as a share of full steering");
        p.Header("Oversteer: the back slides out");
        Row("Threshold", a => a.OverThresh, v => A().OverThresh = v, "How much faster the rear slides sideways than the front before the CPU countersteers");
        Row("Range", a => a.OverRange, v => A().OverRange = v, "Speed over the threshold that makes a full correction of 1");
        Row("Max", a => a.OverMax, v => A().OverMax = v, "Largest countersteer, added to the steering angle");
        Row("Acc. threshold", a => a.OverAccThresh, v => A().OverAccThresh = v, "While countersteering: sideways speed above this makes it lift the throttle");
        Row("Acc. range", a => a.OverAccRange, v => A().OverAccRange = v, "Speed above that for no throttle at all. At 1.5 times this it brakes");
        p.Header("Choosing routes, 0 to 32767");
        Row("Pickups", a => a.PickupBias, v => A().PickupBias = v, "At a fork with a pickup route: it takes it if this value minus a random 0 to 32767 beats the other way. 32767 nearly always", 0, 32767);
        Row("Blocking", a => a.BlockBias, v => A().BlockBias = v, "Read but never used in the original code", 0, 32767);
        Row("Overtaking", a => a.OvertakeBias, v => A().OvertakeBias = v, "Read but never used in the original code", 0, 32767);
        Row("Suspension", a => a.Suspension, v => A().Suspension = v, "The same as Pickups, for bumpy and soft suspension routes", 0, 32767);
        Row("Aggression", a => a.Aggression, v => A().Aggression = v, "The original code sets this to 0 when it loads", 0, 32767);
        p.Header("Trying it");
        p.Note("Take the values of a stock car that drives alike, then race against the CPU in Test run and watch. RVGL chooses the CPU cars itself, so your car shows up among them only if it's allowed and picked.");
        var from = new ComboBox { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
        p.Add(from);
        aiFrom = from;
        p.Button("Copy the AI values of that car", () =>
        {
            if (Doc.Game == null || from.SelectedItem is not string folder) return;
            var other = new Car(Doc.Game, folder).Data.Ai;
            if (other == null) { Say(folder + " has no AI values"); return; }
            Tool("AI from " + folder, () => Doc.D!.Ai = other.Clone());
        });
        return p;
    }

    ComboBox? aiFrom;
    List<AiData>? stockAi;

    // How a value stands against the stock cars
    string AiRange(Func<AiData, double> f, double value)
    {
        if (Doc.Game == null) return "";
        stockAi ??= Doc.Game.CarFolders().Where(GameFolder.IsStock)
            .Select(x => { try { return new Car(Doc.Game, x).Data.Ai; } catch (Exception) { return null; } })
            .Where(a => a != null).Select(a => a!).ToList();
        if (stockAi.Count == 0) return "";
        var v = stockAi.Select(f).OrderBy(x => x).ToList();
        double lo = v[0], hi = v[^1], mid = v[v.Count / 2];
        var word = value < lo ? "  ▲ below all stock cars" : value > hi ? "  ▲ above all stock cars" : "";
        return $"Stock cars: {lo:0.##} to {hi:0.##}, middle {mid:0.##}{word}";
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
        Slider amb = null!, sun = null!, occ = null!;
        Action live = () => { if (amb != null && sun != null && occ != null) _ = ShadeLive((float)amb.Value, (float)sun.Value, (float)occ.Value); };
        amb = p.Slide("Sky light", 0, 1, 0.45, _ => live());
        sun = p.Slide("Sun", 0, 1, 0.55, _ => live());
        occ = p.Slide("Dark corners", 0, 1, 1, _ => live());
        ShadeSliderSun = sun;
        p.Check("See the shading without the texture", () => View.Shade == ShadeMode.VertexColours, v => SetShadeView(v ? ShadeMode.VertexColours : ShadeMode.Textured), "Also on the toolbar: Texture, Shading, Clay");

        ShadeSmoothBox = new ComboBox { FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0,
            ItemsSource = new[] { "As the model's normals", "Smooth, by vertices", "By angle: smooth with sharp edges", "By faces: faceted" } };
        ShadeSmoothBox.SelectionChanged += (_, _) =>
        {
            ShadeSmoothing = (ShadeSmooth)Math.Max(ShadeSmoothBox.SelectedIndex, 0);
            live();
        };
        p.Labeled("Surface", ShadeSmoothBox, "Colours sit on each polygon's corners, so this works on any model and adds no vertices");
        p.Slide("Edge angle", 5, 120, 45, v => { ShadeAngle = (float)v; if (ShadeSmoothing == ShadeSmooth.ByAngle) live(); }, "With By angle: polygons meeting at less than this blend together");
        p.Slide("Sun around", 0, 360, 54, v => { ShadeAround = (float)v; live(); }, "Direction of the sun, seen from above. 0 is the car's front");
        p.Slide("Sun height", 5, 90, 66, v => { ShadeHeight = (float)v; live(); });
        p.Slide("Soft shadows", 0, 1, 0, v => { ShadeSoft = (float)v; live(); }, "From the sharp edge of a small sun to a soft one");
        p.Slide("Floor bounce", 0, 1, 0, v => { ShadeBounce = (float)v; live(); }, "Light coming back up from the floor to what faces down");
        p.Slide("Contrast", 0.5, 2, 1, v => { ShadeContrast = (float)v; live(); });
        p.Colour("Sky colour", Avalonia.Media.Colors.White, c => { ShadeSky = c; live(); }, "Tints the soft light from all around");
        p.Colour("Sun colour", Avalonia.Media.Colors.White, c => { ShadeSunColour = c; live(); });
        p.Colour("Bounce colour", Avalonia.Media.Colors.White, c => { ShadeBounceColour = c; live(); }, "Grass makes the underside green, a red floor red");
        var whole = new CheckBox { Content = "The whole car blocks light", FontSize = 12, MinHeight = 24, IsChecked = true };
        ToolTip.SetTip(whole, "Wheels shade the body and the body shades the wheels, as they stand. Off: each model alone");
        whole.IsCheckedChanged += (_, _) => { ShadeWholeCar = whole.IsChecked == true; live(); };
        p.Add(whole);
        p.Button("Shade the car", async () => await ShadeAsked((float)amb.Value, (float)sun.Value, (float)occ.Value), "Same as moving a slider. It shows at once, Save keeps it. The first version of each model stays as .bak", true);
        p.Button("Back to the shading it had", ShadeUndo);

        p.Header("Vertex normals");
        p.Note("The game takes reflections and its moving lights from these, not the shading above. A vertex has one normal, so a sharp edge needs two vertices in one place: By faces and By angle add vertices, Smooth doesn't. Show has Vertex normals, to see them.");
        var normalMode = new ComboBox { FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0, ItemsSource = new[] { "Smooth, by vertices", "By angle", "By faces" } };
        p.Labeled("Make them", normalMode);
        var normalAngle = p.Slide("Edge angle", 5, 120, 45);
        p.Button("Rewrite the normals", async () => await RewriteNormals(normalMode.SelectedIndex switch { 1 => ShadeSmooth.ByAngle, 2 => ShadeSmooth.Faces, _ => ShadeSmooth.Smooth }, (float)normalAngle.Value), "Undo takes it back, Save keeps it. The first version of each model stays as .bak");

        p.Header("Shadow");
        p.Button("Make shadow.bmp from the car", () => Tool("Shadow", () => { Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!)); Doc.TextureVersion++; }), "Top view of body, wheels and suspension, white on black, and the shadow table around it");

        p.Header("Carbox");
        p.Button("Make carbox.bmp", async () => await MakeCarbox(), "A picture of the car with its name, on white");

        p.Header("Collision");
        p.Button("Make a hull from the body", MakeHull, "One convex part and spheres inside, to edit from there. Save keeps it");
        p.Button("Show and edit the hull", () => { View.ShowHull = true; FillParts(); View.Redraw(); }, "Its parts and spheres appear in the Parts list and in the view");

        p.Header("Fit to the models");
        p.Button("Radius, skid width, spring and axle length", () => Tool("Fit", () => CarTools.FitToModels(Doc.Car!)));
        p.Button("Inertia from the body's shape", () => Tool("Inertia", () => CarTools.ComputeInertia(Doc.Car!)));
        p.Button("Place wheels on a new body", () => Tool("Place wheels", () => CarTools.PlaceWheels(Doc.Car!)));
        p.Button("Make right side like the left", () => Tool("Symmetry", () => CarTools.Symmetrize(Doc.D!)));

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
            var file = Doc.Car.Game.WritePath("cars\\" + Doc.Car.Folder + "\\readme.txt");
            Files.Backup(file);
            File.WriteAllText(file, text, ParamFile.FileEncoding);
            Say("Written: " + file);
        });
        p.Button("Pack as zip...", async () => await PackZip());
        return p;
    }

    // ---------------------------- Paint ----------------------------

    public int PaintTabIndex;
    public Slider ShadeSliderSun = null!;
    public Slider PaintHue = null!, PaintColour = null!, PaintBright = null!, PaintContrast = null!, PaintWarmth = null!, PaintToneAmount = null!, PaintTolerance = null!;
    public ColourButton PaintTone = null!, PaintFrom = null!, PaintTo = null!;
    public CheckBox PaintSwap = null!;
    public ComboBox ShadeSmoothBox = null!;
    readonly WrapPanel swatches = new();
    public readonly HashSet<int> PaintOnly = new();
    SkiaSharp.SKBitmap? paintSrc;
    int paintRun;
    bool paintLoading;
    FileSystemWatcher? texWatcher;

    Page PaintPage()
    {
        var p = NewPage();
        p.Header("Repaint");
        p.Note("Everything shows on the car at once. Nothing is written until you choose below.");
        PaintHue = p.Slide("Hue", -180, 180, 0, _ => PaintChanged(), "Turns the colours around the colour wheel");
        PaintColour = p.Slide("Colour", 0, 2, 1, _ => PaintChanged(), "From grey to very strong");
        PaintBright = p.Slide("Brightness", -0.6, 0.6, 0, _ => PaintChanged());
        PaintContrast = p.Slide("Contrast", 0.5, 2, 1, _ => PaintChanged());
        PaintWarmth = p.Slide("Warm or cool", -1, 1, 0, _ => PaintChanged(), "Towards orange or blue");
        p.Header("All in one colour");
        PaintTone = p.Colour("Colour", Avalonia.Media.Color.Parse("#D7263D"), _ => PaintChanged(), "Keeps the light and dark of the painting");
        PaintToneAmount = p.Slide("Amount", 0, 1, 0, _ => PaintChanged());
        p.Header("Swap a colour");
        PaintSwap = new CheckBox { Content = "Swap this colour for that one", FontSize = 12, MinHeight = 24 };
        PaintSwap.IsCheckedChanged += (_, _) => PaintChanged();
        p.Add(PaintSwap);
        PaintFrom = p.Colour("This", Avalonia.Media.Colors.Red, _ => PaintChanged(), "The texture's own colours are in the list when you click");
        PaintTo = p.Colour("For that", Avalonia.Media.Color.Parse("#1B98E0"), _ => PaintChanged());
        PaintTolerance = p.Slide("How close", 0.02, 0.6, 0.2, _ => PaintChanged(), "How close to This a colour has to be");
        p.Header("Only these colours");
        p.Note("Click colours of this texture to change only those. None clicked: all of them. Black and greys stay.");
        p.Add(swatches);
        p.Button("Back to the texture as it is", () => { ResetPaint(); }, null);
        p.Header("Keep it");
        p.Button("Save as a new skin", () => PaintSaveSkin(), "A new file car-alt1.bmp next to the texture. RVGL lists it as a skin, the original stays", true);
        p.Button("Change this car's texture", async () => await PaintApply(), "The first version stays as .bak");
        p.Header("Your image editor");
        p.Note("Paint on the real texture: open it, and each time you save there the car here changes.");
        p.Button("Open the texture and watch it", OpenTextureEditor);
        p.Button("Next skin", NextSkin);
        p.Info(() =>
        {
            if (Doc.Car == null) return "";
            var skins = Doc.Car.FindSkins();
            return skins.Count > 1 ? "Skins: " + string.Join(", ", skins.Select(Path.GetFileNameWithoutExtension)) : "No other skins";
        });
        return p;
    }

    void NextSkin()
    {
        if (Doc.Car == null) return;
        var skins = Doc.Car.FindSkins();
        if (skins.Count == 0) return;
        var cur = Doc.SkinOverride ?? Doc.Car.Game.Find(Doc.D!.TPage);
        int i = skins.FindIndex(s => string.Equals(s, cur, StringComparison.OrdinalIgnoreCase));
        Doc.SkinOverride = skins[(i + 1) % skins.Count];
        ResetPaintKeepSkin();
        Say("Showing " + Path.GetFileName(Doc.SkinOverride));
    }

    string? PaintFile() => Doc.Car == null ? null : Doc.SkinOverride ?? Doc.Car.Game.Find(Doc.D!.TPage);

    void ReloadPaintSource()
    {
        paintSrc?.Dispose();
        paintSrc = Images.Load(PaintFile());
        swatches.Children.Clear();
        PaintOnly.Clear();
        if (paintSrc == null) return;
        var own = CarTools.MainColours(paintSrc).Take(10).Select(c => Avalonia.Media.Color.FromRgb(c.Sample.Red, c.Sample.Green, c.Sample.Blue)).ToList();
        PaintFrom.SetOwn(own);
        if (own.Count > 0) PaintFrom.SetValue(own[0]);
        foreach (var (bucket, _, sample) in CarTools.MainColours(paintSrc).Take(10))
        {
            var t = new Avalonia.Controls.Primitives.ToggleButton
            {
                Width = 34, Height = 26, Margin = new Thickness(0, 0, 4, 4), MinWidth = 0, MinHeight = 0, Tag = bucket,
                Background = new SolidColorBrush(Color.FromRgb(sample.Red, sample.Green, sample.Blue)), BorderThickness = new Thickness(2)
            };
            t.IsCheckedChanged += (_, _) =>
            {
                if (t.IsChecked == true) { PaintOnly.Add(bucket); t.BorderBrush = Look.Text; }
                else { PaintOnly.Remove(bucket); t.BorderBrush = null; }
                PaintChanged();
            };
            swatches.Children.Add(t);
        }
    }

    void ResetPaintKeepSkin()
    {
        paintLoading = true;
        PaintHue.Value = 0; PaintColour.Value = 1; PaintBright.Value = 0;
        PaintContrast.Value = 1; PaintWarmth.Value = 0; PaintToneAmount.Value = 0; PaintSwap.IsChecked = false;
        paintLoading = false;
        Doc.PaintPreview = null;
        ReloadPaintSource();
        Doc.TextureVersion++;
        View.Redraw();
    }

    public void ResetPaint()
    {
        Doc.SkinOverride = null;
        texWatcher?.Dispose();
        texWatcher = null;
        ResetPaintKeepSkin();
    }

    static SkiaSharp.SKColor Sk(Avalonia.Media.Color c) => new(c.R, c.G, c.B);

    public PaintOptions PaintNow() => new(
        (float)PaintBright.Value, (float)PaintColour.Value, (float)PaintHue.Value, PaintOnly.ToArray(),
        (float)PaintContrast.Value, (float)PaintWarmth.Value, Sk(PaintTone.Value), (float)PaintToneAmount.Value,
        PaintSwap.IsChecked == true ? Sk(PaintFrom.Value) : null, PaintSwap.IsChecked == true ? Sk(PaintTo.Value) : null, (float)PaintTolerance.Value);

    SkiaSharp.SKBitmap PaintResult(SkiaSharp.SKBitmap src) => CarTools.AdjustTexture(src, PaintNow());

    /// <summary>Work out the look in the background: the newest one wins, the sliders never wait</summary>
    public void PaintChanged()
    {
        if (paintLoading || paintSrc == null) return;
        var look = PaintNow();
        if (look.IsPlain)
        {
            Doc.PaintPreview = null;
            View.Redraw();
            return;
        }
        int run = ++paintRun;
        var src = paintSrc;
        Task.Run(() =>
        {
            using var o = CarTools.AdjustTexture(src, look);
            var rgba = Doc.ToRgba(o, out int w, out int ht);
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (run != paintRun) return;
                Doc.PaintPreview = rgba;
                Doc.PaintW = w; Doc.PaintH = ht;
                Doc.PaintVersion++;
                View.Redraw();
            });
        });
    }

    public string? PaintSaveSkin()
    {
        if (paintSrc == null || PaintFile() is not { } tex) { Say("No texture"); return null; }
        using var o = PaintResult(paintSrc);
        var game = Doc.Car!.Game;
        var mainRel = Doc.D!.TPage.Replace('/', '\\');
        var cut = mainRel.LastIndexOf('\\');
        var relDir = cut >= 0 ? mainRel[..cut] : "";
        var stem = Path.GetFileNameWithoutExtension(mainRel);
        int n = 1;
        string rel;
        do rel = relDir + "\\" + stem + "-alt" + n++ + ".bmp"; while (game.Find(rel) != null);
        var path = game.WritePath(rel);
        CarTools.SaveTexture(o, path, paintSrc.AlphaType != SkiaSharp.SKAlphaType.Opaque);
        Doc.SkinOverride = path;
        ResetPaintKeepSkin();
        Say("Skin saved: " + Path.GetFileName(path) + ". RVGL shows it on the car selection screen");
        return path;
    }

    public async Task PaintApply()
    {
        if (paintSrc == null || Doc.Car == null || PaintFile() is not { } tex) { Say("No texture"); return; }
        var rel = Doc.D!.TPage;
        if (Doc.IsStockCar)
        {
            var r = await Dialogs.Ask(this, Doc.D.Name + " is a stock car, this is the game's own texture.", "Make a copy", "Change it", "Cancel");
            if (r == 2) return;
            if (r == 0)
            {
                if (!await CopyCar()) return;
                tex = PaintFile()!;
            }
        }
        else if (Doc.SharedProblem(rel) is { } why && !await AskSharedFiles(new() { (rel, why) })) return;
        tex = PaintFile() ?? tex;
        WritePaint(tex);
    }

    public void WritePaint(string tex)
    {
        using var o = PaintResult(paintSrc!);
        var game = Doc.Car!.Game;
        var target = game.RelOf(tex) is { } rel ? game.WritePath(rel) : tex;
        Files.Backup(target);
        CarTools.SaveTexture(o, target, paintSrc!.AlphaType != SkiaSharp.SKAlphaType.Opaque);
        ResetPaintKeepSkin();
        Say("Texture changed: " + Path.GetFileName(tex) + ". The first version stays as .bak");
    }

    void OpenTextureEditor()
    {
        if (PaintFile() is not { } tex0) { Say("No texture"); return; }
        // A pack's file is left alone: you paint on a copy the game reads instead
        var tex = Doc.Car!.Game.RelOf(tex0) is { } texRel ? Doc.Car.Game.EnsureWritable(texRel) : tex0;
        WatchTexture(tex);
        try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tex) { UseShellExecute = true }); }
        catch (Exception ex) { Say("Couldn't open it: " + ex.Message + ". It's still being watched: " + tex); return; }
        Say("Watching " + Path.GetFileName(tex) + ": save in your editor and it shows here");
    }

    public void WatchTexture(string tex)
    {
        texWatcher?.Dispose();
        texWatcher = new FileSystemWatcher(Path.GetDirectoryName(tex)!, Path.GetFileName(tex)) { NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName };
        FileSystemEventHandler onChange = (_, _) => Avalonia.Threading.DispatcherTimer.RunOnce(() =>
        {
            ResetPaintKeepSkin(); // an editor may still be writing: this comes a moment later
            Say("Texture reloaded " + DateTime.Now.ToShortTimeString());
        }, TimeSpan.FromMilliseconds(400));
        texWatcher.Changed += onChange;
        texWatcher.Created += onChange;
        texWatcher.EnableRaisingEvents = true;
    }

    // ---------------------------- Hull ----------------------------

    void HullPage(Page p, Selection s)
    {
        if (Doc.Hull is not { } h0) { p.Note("This car has no hull. Tools can make one."); return; }
        if (s.Kind == SelKind.HullPart)
        {
            int i = s.Index;
            p.Info(() => Doc.Hull is { } h && i < h.Convex.Count ? $"{h.Convex[i].Points.Length} points, {h.Convex[i].Faces.Length} faces. Size {h.Convex[i].Max.X * 2:0.#} x {h.Convex[i].Max.Y * 2:0.#} x {h.Convex[i].Max.Z * 2:0.#}" : "");
            p.Note("Use the Move, Rotate and Scale handles in the view. The whole part goes together, so it stays a solid.");
        }
        else
        {
            int i = s.Index;
            p.Vec("Position", () => Doc.Hull != null && i < Doc.Hull.Spheres.Count ? Doc.Hull.Spheres[i].Pos : default, v => Doc.SetSphere(i, v, Doc.Hull!.Spheres[i].Radius));
            p.Num("Radius", () => Doc.Hull != null && i < Doc.Hull.Spheres.Count ? Doc.Hull.Spheres[i].Radius : 0, v => Doc.SetSphere(i, Doc.Hull!.Spheres[i].Pos, (float)v), "Keep spheres inside the body", 0.1);
            p.Note("Spheres give the car its bumps against walls and other cars. Keep them inside the green shape. With Mirror L/R the one on the other side follows.");
        }
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,4,*") };
        var add = new Button { Content = "Add a sphere", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        add.Click += (_, _) => Doc.AddSphere(Doc.Anchor(s) ?? default, 8);
        var remove = new Button { Content = "Remove", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Center };
        remove.Click += (_, _) => Doc.RemoveHullItem(s);
        Grid.SetColumn(remove, 2);
        row.Children.Add(add);
        row.Children.Add(remove);
        p.Add(row);
        p.Button("Make again from the body", MakeHull);
    }

    readonly ComboBox levelBox = new() { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
    List<string> levelFolders = new();
    readonly TextBlock runReport = new() { FontSize = 12, TextWrapping = TextWrapping.Wrap };

    Page RunPage()
    {
        var p = NewPage();
        p.Header("In the real game");
        p.Note("Saves the car and starts RVGL in a window, with this car in a race. It uses its own settings folder, your profile and best times stay as they are.");
        levelBox.SelectionChanged += (_, _) => SelectedLevel = levelBox.SelectedIndex >= 0 && levelBox.SelectedIndex < levelFolders.Count ? levelFolders[levelBox.SelectedIndex] : null;
        p.Add(new TextBlock { Text = "Track", FontSize = 12 });
        p.Add(levelBox);
        p.Check("Go straight into the race", () => RaceByItself, v => RaceByItself = v, "Goes through RVGL's menus with key presses, only while its window is in front. Unlocks all cars on the test profile with the CARNIVAL code");
        p.Int("Cars in the race", () => RaceCars, v => RaceCars = v, "1 is you alone", 1, 16);
        p.Int("Laps", () => RaceLaps, v => RaceLaps = v, null, 1, 20);
        p.Button("▶  Test run (F5)", () => TestRunGame(false), null, true);
        p.Button("Test run, dev mode", () => TestRunGame(true), "Ctrl+R in the game reloads the car after you save here. Start Race has Calc Car Stats");

        p.Header("Race, here");
        p.Note("You against CPU cars on a built-in road. Cars use the same physics as Drive here. The CPU driver, road and walls are this editor's own, so times won't match RVGL. Its corrections for sliding work as the game's do, from each car's AI values.");
        var trackBox = new ComboBox { FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0, ItemsSource = RaceTrack.Names };
        trackBox.SelectionChanged += (_, _) => RaceTrackIndex = Math.Max(trackBox.SelectedIndex, 0);
        p.Labeled("Road", trackBox);
        var groundBox = new ComboBox { FontSize = 12, MinHeight = 26, HorizontalAlignment = HorizontalAlignment.Stretch, SelectedIndex = 0, ItemsSource = RaceTrack.GroundNames };
        groundBox.SelectionChanged += (_, _) => RaceGroundIndex = Math.Max(groundBox.SelectedIndex, 0);
        p.Labeled("Ground", groundBox, "Plain, bumps all the way, or a jump after the start");
        var opponents = new NumberBox(0, 9, true);
        opponents.Show(RaceOpponents);
        opponents.Edited += v => RaceOpponents = (int)v;
        p.Labeled("CPU cars", opponents);
        var laps = new NumberBox(1, 20, true);
        laps.Show(RaceLapsHere);
        laps.Edited += v => RaceLapsHere = (int)v;
        p.Labeled("Laps", laps);
        p.Slide("CPU skill", 0.6, 1.05, RaceSkill, v => RaceSkill = (float)v, "The CPU cars' level, as in the game: below 0.75 Very easy, below 0.9 Easy, below 1 Medium, 1 and up Hard");
        var sameCar = new CheckBox { Content = "All CPU cars are this car", FontSize = 12, MinHeight = 24 };
        ToolTip.SetTip(sameCar, "Off: stock cars closest to it in speed");
        sameCar.IsCheckedChanged += (_, _) => RaceSameCar = sameCar.IsChecked == true;
        p.Add(sameCar);
        var aiDrives = new CheckBox { Content = "The AI drives my car", FontSize = 12, MinHeight = 24 };
        ToolTip.SetTip(aiDrives, "Watch your car's AI values at work. The I key switches during a race");
        aiDrives.IsCheckedChanged += (_, _) => RaceAiDrives = aiDrives.IsChecked == true;
        p.Add(aiDrives);
        p.Button("▶  Race (F7)", () => StartRace(), null, true);

        p.Header("Quick drive, here");
        p.Note("The original game's car physics on a plain floor. Arrows or WASD, Space flips a car on its roof, R restarts, Esc leaves.");
        p.Button("Drive on a flat floor (F6)", () => StartDrive(SimGround.Flat));
        p.Button("Drive on bumps", () => StartDrive(SimGround.Bumps));
        p.Button("Drive to a ramp", () => StartDrive(SimGround.Ramp));

        p.Header("Automatic runs");
        p.Button("Run them", () =>
        {
            if (Doc.D == null) return;
            var d = Doc.D.Clone();
            var a = CarSim.RunAcceleration(d, Doc.Hull);
            runReport.Text = $"{a.Note}\nIn the file: TopEnd {d.TopEnd:0}, Acc {d.Acc:0.0}\nSimulated: TopEnd {a.TopEnd:0}, Acc {a.Acc:0.0}\n{CarSim.RunCircle(d, Doc.Hull).Note}\n{CarSim.RunBumps(d, Doc.Hull).Note}";
        }, "Full throttle, hard turn, bumps");
        p.Add(runReport);
        return p;
    }
}
