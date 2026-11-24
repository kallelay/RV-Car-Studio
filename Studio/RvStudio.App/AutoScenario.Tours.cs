using Avalonia.Controls;
using Avalonia.LogicalTree;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>
/// RvCarStudio -rvpath COPY --tours MODELS SHOTS: the tours and the readme, in the real window. Every step of the three
/// tours must point at something that's there; the New car tour is gone through with Do it for me, each step ticking
/// itself off; the readme window writes a readme that reads right.
/// </summary>
public static partial class AutoScenario
{
    public static async Task RunTours(MainWindow w, string models, string shotDir)
    {
        Running = true;
        shots = shotDir;
        Directory.CreateDirectory(shots);
        log = new StreamWriter(Path.Combine(shots, "tours.txt"));
        var d = w.Doc;
        AppDomain.CurrentDomain.UnhandledException += (_, e) => Log("CRASH " + e.ExceptionObject);
        try
        {
            await Wait(1500);
            var game = d.Game!;

            // Every step of every tour points at something real
            for (int k = 0; k < MainWindow.TourNames.Length; k++)
            {
                var tour = w.MakeTour(k);
                foreach (var s in tour.Steps.Where(s => s.Where != null))
                {
                    var c = await w.Highlight(s.Where, s.Where!.StartsWith("menu:"));
                    bool ok = c != null && (!s.Where.StartsWith("menu:") || s.Where.Split('>').Length < 2 || (c as MenuItem)?.Header as string == s.Where.Split('>')[1]);
                    Check(ok, $"tour {tour.Name}: '{s.Title}' points at {s.Where}");
                    foreach (var m in w.GetLogicalDescendants().OfType<MenuItem>().Where(m => m.IsSubMenuOpen).ToList()) m.Close();
                    await Wait(80);
                }
            }

            // New car: stock cars first, Toyeca to start from; bad files are said in the wizard, which stays open
            {
                var nw = new NewCarWizard(game, null);
                nw.Show(w);
                await Wait(300);
                Check(nw.BaseFolder == "toyeca", "New car starts from Toyeca: " + nw.BaseFolder);
                Check(nw.BaseList.Take(5).All(x => x.Stock), "New car lists the stock cars first");
                var badObj = Path.Combine(shots, "not_a_model.obj");
                File.WriteAllLines(badObj, new[] { "this is a shopping list, not a model", "milk", "eggs" });
                var badPrm = Path.Combine(shots, "broken.prm");
                File.WriteAllBytes(badPrm, Enumerable.Range(0, 300).Select(i => (byte)(i * 37)).ToArray());
                var badTex = Path.Combine(shots, "not_a_picture.png");
                File.WriteAllText(badTex, "not a png");
                foreach (var (body, tex, what) in new[] { (badObj, (string?)null, "an .obj with no polygons"), (badPrm, null, "a damaged .prm"),
                    (Path.Combine(models, "body.obj"), badTex, "a texture that isn't a picture"), (Path.Combine(shots, "missing.obj"), null, "a body that isn't there") })
                {
                    nw.Fill(NewCarKind.Original, "toyeca", "Bad Car", body, null, tex);
                    await Wait(300);
                    bool made = nw.Confirm();
                    await Wait(200);
                    Check(!made && nw.IsVisible && nw.Problem != "", $"New car refuses {what} and says why: {nw.Problem}");
                }
                nw.Fill(NewCarKind.Original, "toyeca", "", Path.Combine(models, "body.obj"));
                await Wait(300);
                Check(!nw.Confirm() && nw.Problem != "", "New car with no name says so: " + nw.Problem);
                SaveWin(nw, "newcar_bad_model");
                nw.Close();
                Check(game.CarLayer("badcar") == null, "nothing was made for the bad files");
            }

            // The New car tour, step by step
            w.StartTour(0);
            await Wait(300);
            Check(w.CurrentTour != null && w.TourStepIndex == -1, "the tour opens on its intro card");
            WindowShot(w, "tour_intro");
            w.TourGo(0);
            await Wait(300);
            WindowShot(w, "tour_step1");
            var step0 = w.CurrentTour!.Steps[0];
            step0.DoIt!();
            await Wait(700);
            var wiz = w.OwnedWindows.OfType<NewCarWizard>().FirstOrDefault();
            Check(wiz != null, "Do it for me opens New car");
            wiz!.Fill(NewCarKind.Original, "toyeca", "Tour Car", Path.Combine(models, "body.obj"), Path.Combine(models, "wheel.obj"), Path.Combine(models, "texture.png"));
            await Wait(300); // the folder box follows the name a moment later
            Log("wizard: " + (wiz.Confirm() ? "ok" : "refused: " + wiz.Problem));
            for (int i = 0; i < 40 && w.TourStepIndex == 0; i++) await Wait(100);
            Check(w.TourStepIndex == 1 && d.Car?.Folder == "tourcar", "making the car ticks the first step off and goes on: " + w.TourStepIndex + " / " + d.Car?.Folder);
            Log("status: " + w.StatusText.Text);
            if (d.Car == null) return;

            for (int guard = 0; guard < 30 && w.CurrentTour != null; guard++)
            {
                int at = w.TourStepIndex;
                var s = w.CurrentTour.Steps[at];
                if (s.Title.StartsWith("Readme")) break;   // the zip needs a file picker: the readme is tried below
                if (s.IsDone != null && s.DoIt != null && !s.Title.Contains("game"))
                {
                    s.DoIt();
                    for (int i = 0; i < 80 && w.TourStepIndex == at; i++) await Wait(100);
                    Check(w.TourStepIndex == at + 1, $"tour step '{s.Title}': Do it for me ticks it off");
                    if (w.TourStepIndex == at) w.TourGo(at + 1);
                    if (s.Title.StartsWith("Drive") || s.Title.StartsWith("Tune")) w.StopDrive();
                }
                else w.TourGo(at + 1);
                await Wait(200);
                if (at == 4) WindowShot(w, "tour_step_shade");
            }

            // The readme: answers in, the readme as it will be, written into the car's folder
            var rw = new ReadmeWindow(d.Car!, d.Hull, false);
            rw.Show(w);
            await Wait(400);
            rw.Fill("Tester", "A red GT built from a Blender model for the release test. Quick, low and a little nervous over bumps.", Readme.Kind.Original);
            await Wait(500);
            SaveWin(rw, "readme_window");
            Check(rw.Write() && rw.WrittenPath != null, "the readme is written: " + rw.WrittenPath);
            var text = File.ReadAllText(rw.WrittenPath!, ParamFile.FileEncoding);
            var lines = text.Replace("\r\n", "\n").Split('\n');
            Check(lines.All(l => l.Length <= 78), "every readme line fits 78 columns: " + lines.Max(l => l.Length));
            Check(text.All(ch => ch < 0x80 || ch >= 0xA0), "no control characters in the readme");
            Check(text.Contains("Tour Car") && text.Contains("Tester") && text.Contains("Install") && text.Contains("cars\\tourcar") && text.Contains("Top speed"), "the readme has the car, its maker, install and speed");
            Log("readme:\n" + text);
            rw.Close();
            w.EndTour();

            // Vertices: the body's, picked with a click where one shows, moved, undone; then a box; then the hull's corners
            d.Select(new Selection(SelKind.Body));
            w.ToggleVertexMode();
            await Wait(200);
            var (bodyModel, _) = d.VertexTarget(new Selection(SelKind.Body));
            var pts = d.VertexPositions();
            Check(d.VertexMode && bodyModel != null && pts.Length == bodyModel.Verts.Length, $"Tab edits the body's vertices: {pts.Length}");
            w.View.SetView(ViewMode.Perspective); w.View.FrameCar(); w.View.Redraw();
            await Wait(200);
            // The vertex nearest the eye: on screen, not behind others
            int target = Enumerable.Range(0, pts.Length).OrderBy(i => (pts[i] - w.View.EyePos).Length()).First();
            var vAt = w.View.ScreenOf(pts[target]);
            Check(vAt != null, "the vertex shows on the screen");
            if (vAt is { } scr)
            {
                w.View.PickVerticesAt(scr, scr, false, Avalonia.Input.KeyModifiers.None);
                Check(d.PickedVerts.Count == 1 && (d.VertexPositions()[d.PickedVerts.First()] - pts[target]).Length() < 0.5f, "a click picks the vertex under the mouse: " + string.Join(",", d.PickedVerts));
                int depth = d.UndoDepth;
                var before = d.VertexPositions()[d.PickedVerts.First()];
                d.Move(d.Sel, new System.Numerics.Vector3(0, -6, 0));
                d.EndGesture();
                var after = d.VertexPositions()[d.PickedVerts.First()];
                Check((after - before - new System.Numerics.Vector3(0, -6, 0)).Length() < 0.01f && d.DirtyModels.Contains(bodyModel!) && d.UndoDepth == depth + 1, $"moving it moves that vertex only, one undo step: {before} -> {after}");
                Check(d.Anchor(d.Sel) is { } an && (an - after).Length() < 0.01f, "the handles sit on the picked vertex");
                await GlShot(w, "vertices_moved", 720, false);
                d.Undo();
                Check((d.VertexPositions()[d.PickedVerts.First()] - before).Length() < 0.01f, "undo puts it back");
            }
            w.View.PickVerticesAt(new Avalonia.Point(0, 0), new Avalonia.Point(w.View.Bounds.Width, w.View.Bounds.Height), true, Avalonia.Input.KeyModifiers.None);
            Check(d.PickedVerts.Count > pts.Length / 2, $"a box over the view picks the vertices in it: {d.PickedVerts.Count} of {pts.Length}");
            w.View.Nudge(1, 0, 0, 2);
            Check(d.Modified, "the arrows move the picked vertices");
            d.Undo();
            await GlShot(w, "vertices_box", 720, false);

            // The hull: a corner pulled out, it stays a closed convex solid; undo puts it back
            if (d.Hull is { Convex.Count: > 0 })
            {
                d.LeaveVertexMode();
                d.Select(new Selection(SelKind.HullPart, 0));
                w.ToggleVertexMode();
                var hp = d.VertexPositions();
                Check(d.VertexMode && hp.Length >= 4, "the hull part's corners: " + hp.Length);
                int far = Enumerable.Range(0, hp.Length).OrderByDescending(i => hp[i].X).First();
                d.PickVertices(new[] { far }, false);
                var hullBefore = d.Hull.Clone();
                d.Move(d.Sel, new System.Numerics.Vector3(8, 0, 0));
                d.EndGesture();
                var c = d.Hull.Convex[0];
                Check(c.Points.Length - c.Edges.Length + c.Faces.Length == 2, "the hull is still a closed solid (Euler)");
                Check(c.Faces.All(f => c.Points.All(q => System.Numerics.Vector3.Dot(f.Normal, q) + f.D < 0.15f)), "every corner is behind every face");
                Check(c.Points.Max(q => q.X) > hullBefore.Convex[0].Points.Max(q => q.X) + 7, "the corner went out");
                Check(d.PickedVerts.Count == 1, "the moved corner is still picked");
                await GlShot(w, "hull_corner", 720, false);
                d.Undo();
                Check(Math.Abs(d.Hull.Convex[0].Points.Max(q => q.X) - hullBefore.Convex[0].Points.Max(q => q.X)) < 0.01f, "undo puts the hull back");
            }
            w.ToggleVertexMode();
            Check(!d.VertexMode, "Tab again: back to whole parts");

            // Properties: the section bar jumps, and the car's handling comes from another car in one click
            w.Tabs.SelectedIndex = w.PropertiesTabIndex;
            await Wait(400);
            var props = w.GuideTab;
            Check(props.JumpNames.Contains("Car") && props.JumpNames.Contains("Handling") && props.JumpNames.Count >= 5, "Properties has a section bar: " + string.Join(" | ", props.JumpNames));
            Check(props.JumpTo("Handling"), "a section button jumps");
            await Wait(400);
            Check(props.Offset.Y > 100 && props.CurrentSection == "Handling", $"the page is at Handling ({props.Offset.Y:0}), lit: {props.CurrentSection}");
            WindowShot(w, "properties_handling");
            var before2 = d.D!.Clone();
            int took = w.TakeHandling("cougar", true);
            var cougar = new Car(game, "cougar").Data;
            Check(took > 5 && Math.Abs(d.D!.TopSpeed - cougar.TopSpeed) < 0.01 && Math.Abs(d.D.Wheels[0].StaticFriction - cougar.Wheels[0].StaticFriction) < 0.01,
                $"Take its handling: Cougar's engine and grip ({took} values)");
            Check(d.D.Models.SequenceEqual(before2.Models) && d.D.Wheels[0].Offset1 == before2.Wheels[0].Offset1 && d.D.Name == before2.Name && d.D.TCarBox == before2.TCarBox,
                "the models, wheels' places, name and carbox stay");
            d.Undo();
            Check(Math.Abs(d.D!.TopSpeed - before2.TopSpeed) < 0.01, "undo takes the handling back");
            Check(w.HandlingCars().Count > 10 && w.HandlingCars().Take(5).All(c => c.Stock) && w.HandlingCars()[0].Says.Contains("mph"), "the list: stock cars first, each with how it drives: " + w.HandlingCars()[0].Says);

            await LivePaint(w, d);

            // The hull from the body: its spheres have the body's outline, the other detail, the spheres alone
            w.HullDetail = HullMaker.Detail.Normal;
            Check(d.RebuildHull(), "a hull from the body");
            var hq = d.HullQuality();
            Check(hq != null && hq.Spheres >= 8 && hq.Spheres <= 22 && hq.Gap95 < 6.5 && hq.OutMax < 8 && hq.Outline < 2.5, "its spheres stand for the body: " + w.HullReport());
            Check(d.Hull!.Spheres.All(s => Math.Abs(s.Pos.X) < 0.01 || d.Hull.Spheres.Any(o => (o.Pos - s.Pos * new System.Numerics.Vector3(-1, 1, 1)).Length() < 0.01 && Math.Abs(o.Radius - s.Radius) < 0.01)), "left and right the same");
            int nparts = d.Hull.Convex.Sum(c => c.Points.Length), many = d.Hull.Spheres.Count;
            Check(d.RefillSpheres(HullMaker.Detail.Few) && d.Hull!.Spheres.Count <= 12 && d.Hull.Convex.Sum(c => c.Points.Length) == nparts, "the spheres alone, fewer: " + d.Hull!.Spheres.Count);
            d.Undo();
            Check(d.Hull!.Spheres.Count == many, "undo brings them back: " + d.Hull.Spheres.Count);
            w.ShowHull(true);
            w.View.HullSolid = true;
            using (var hp = await w.View.Capture(640, false)) SavePng(hp, "hull_spheres");
            w.View.HullSolid = false;
            w.ShowHull(false);

            // The Parts list: the hull is one row that opens to its parts and spheres, not a dozen rows among the rest
            w.ShowHull(false);
            await Wait(200);
            var items = w.PartsItems();
            int hullRows = items.Count(i => i.Tag is Selection hs && hs.IsHull);
            Check(items.Any(i => i.Tag is "hull") && hullRows == 0, "the hull is one closed row in the Parts list: " + hullRows + " hull rows showing");
            w.ShowHull(true);
            await Wait(200);
            items = w.PartsItems();
            hullRows = items.Count(i => i.Tag is Selection hs2 && hs2.IsHull);
            Check(hullRows == d.Hull!.Convex.Count + d.Hull.Spheres.Count && items.Where(i => i.Tag is Selection hs3 && hs3.IsHull).All(i => i.Padding.Left > 8),
                $"opened, its {hullRows} parts and spheres are indented under it");
            WindowShot(w, "parts_hull_open");
            w.ShowHull(false);
            await Wait(200);
            // As a click does: the hull's row selected in the list (it once broke the list); then twice more, then another car
            int failsBefore = fail;
            for (int k = 0; k < 3; k++)
            {
                var hullRow = w.PartsItems().First(i => i.Tag is "hull");
                w.ClickPartsItem(hullRow);
                await Wait(300);
            }
            Check(fail == failsBefore && w.PartsItems().Any(i => i.Tag is Selection hs4 && hs4.IsHull), "clicking the hull's row in the Parts list opens it, no error");
            w.ClickPartsItem(w.PartsItems().First(i => i.Tag is "hull"));
            await Wait(300);
            w.PickCar("cougar");
            await Wait(500);
            Check(fail == failsBefore && w.PartsItems().Any(i => i.Tag is Selection bs && bs.Kind == SelKind.Body), "another car opens fine after it");
            w.PickCar("tourcar");
            await Wait(500);

            // The texture editor's new tools: a line and a shape with the real canvas
            var tx = w.OpenUvEditor();
            await Wait(600);
            if (tx != null)
            {
                tx.Ink = SkiaSharp.SKColors.Lime;
                tx.Picked.Clear();
                tx.BrushTip = TexturePaint.Tip.Square;
                tx.Canvas.DrawLine(new SkiaSharp.SKPoint(20, 20), new SkiaSharp.SKPoint(120, 20));
                Check(tx.Pic.GetPixel(70, 20).Green > 200 && tx.Pic.GetPixel(70, 20).Red < 60, "the line tool draws a line");
                tx.SetTool(TexTool.Polygon);
                foreach (var q in new[] { new SkiaSharp.SKPoint(200, 200), new SkiaSharp.SKPoint(300, 200), new SkiaSharp.SKPoint(250, 290) }) tx.Canvas.AddPolygonPoint(q);
                tx.Canvas.ClosePolygon();
                Check(tx.Pic.GetPixel(250, 230).Green > 200 && tx.Pic.GetPixel(250, 230).Red < 60, "the shape tool fills a polygon");
                SaveWin(tx, "texture_tools");
                tx.CloseDiscarding();
                await Wait(300);
            }

            // Easy: the Pro parts are out of the way, the RVGL tab too; links go where things live
            w.SetMode(false);
            await Wait(300);
            var rvglTab = w.Tabs.Items.OfType<TabItem>().First(x => (x.Header as string) == "Properties (RVGL)");
            Check(!rvglTab.IsVisible, "Easy hides Properties (RVGL)");
            await EasyLook(w, d);
            w.SetMode(true);
            await Wait(300);
            Check(rvglTab.IsVisible && rvglTab.IsEffectivelyVisible, "Pro shows Properties (RVGL)");
            Check(!w.EasyShown && w.Tabs.Items.OfType<TabItem>().All(x => x.IsVisible == !((string)x.Header!).StartsWith("Easy: ")), "and none of Easy's");
            w.GoTo(w.FilesTabIndex, "Sounds");
            await Wait(400);
            Check(w.Tabs.SelectedIndex == w.FilesTabIndex, "a link goes to the tab where the thing lives");

            // From the old Car Studio: details, presets, centring, the menu card, messages, templates, outside edits
            Check(w.CarDetailsText.Contains("mph") && w.CarDetailsText.Contains("polygons"), "the car's details under its name: " + w.CarDetailsText);
            Check(w.ReadmeButtonShown, "Read its readme shows when the car has one");
            var comBefore = d.D!.CoM;
            w.SetComPreset(1);
            float zMid = (d.D.Wheels[0].Offset1.Z + d.D.Wheels[2].Offset1.Z) / 2;
            Check(Math.Abs(d.D.CoM.Z - zMid) < 0.01f && Math.Abs(d.D.CoM.Y - comBefore.Y) < 0.01f, $"centre of mass balanced front and back: z {d.D.CoM.Z:0.0} (axles' middle {zMid:0.0})");
            d.Undo();
            Check((d.D.CoM - comBefore).Length() < 0.01f, "undo puts the centre of mass back");
            w.WeaponAuto();
            var bodyM = d.Model(d.D.BodyModel)!;
            Check(Math.Abs(d.D.Weapon.Z - (bodyM.Max.Z + d.D.BodyOffset.Z)) < 0.01f && d.D.Weapon.Y < bodyM.Max.Y, "the weapon placed at the front, near the top: " + d.D.Weapon);
            d.Undo();
            w.CentreModel(new Selection(SelKind.Body));
            var bc = (d.Model(d.D.BodyModel)!.Min + d.Model(d.D.BodyModel)!.Max) / 2;
            Check(Math.Abs(bc.X) < 0.01f && Math.Abs(bc.Z) < 0.01f, $"Centre the model: its middle on its origin ({bc.X:0.00}, {bc.Z:0.00})");
            d.Undo();
            w.Tabs.SelectedIndex = w.PropertiesTabIndex;
            await Wait(300);
            Check(w.GetLogicalDescendants().OfType<TextBlock>().Any(x => x.Text == "Acceleration") , "the menu card with its bars, as the game draws it");
            Check(w.Messages.Count > 5, "the messages so far: " + w.Messages.Count);
            var rw2 = new ReadmeWindow(d.Car!, d.Hull, false);
            rw2.Show(w);
            await Wait(300);
            if (rw2.Layouts > 1)
            {
                rw2.UseLayout(1);
                Check(rw2.Text.Contains(d.D.Name) && rw2.Text.Contains("+"), "a readme from the old Car Studio's template");
            }
            else Log("no readme templates next to the app (data/readme): skipped");
            rw2.Close();
            // parameters.txt changed in another program: the car reads it again
            w.Save();
            await Wait(3500);
            var pfile = d.Car!.ParamPath;
            var ptext = File.ReadAllText(pfile, ParamFile.FileEncoding);
            var top0 = d.D!.TopSpeed;
            var tm = System.Text.RegularExpressions.Regex.Match(ptext, @"TopSpeed\s+([0-9.]+)");
            if (tm.Success)
            {
                File.WriteAllText(pfile, ptext.Replace(tm.Value, "TopSpeed   " + (top0 + 3).ToString("0.000000", System.Globalization.CultureInfo.InvariantCulture)), ParamFile.FileEncoding);
                for (int i = 0; i < 40 && Math.Abs(d.D!.TopSpeed - (top0 + 3)) > 0.01; i++) await Wait(100);
                Check(Math.Abs(d.D!.TopSpeed - (top0 + 3)) < 0.01, $"an outside edit of parameters.txt comes in: TopSpeed {d.D.TopSpeed}");
            }

            // Files: the rows with Pick are back, each model slot says what it's for
            w.Tabs.SelectedIndex = w.FilesTabIndex;
            await Wait(300);
            var picks = w.GetLogicalDescendants().OfType<Button>().Count(b => (b.Content as string) == "Pick");
            Check(picks >= 23, "the Files tab has Pick for the texture, hull, shadow, carbox and the 19 model slots: " + picks);
            var shownList = w.GetLogicalDescendants().OfType<Button>().Where(b => (b.Content as string) == "Pick" && b.IsEffectivelyVisible).ToList();
            var shown = shownList.Distinct().Count(b => b.FindLogicalAncestorOfType<TabItem>() == w.Tabs.SelectedItem);
            Check(shown > 4 && shown < 23, "only the model slots in use show (the free ones are one click away): " + shown);
            var hints = w.GetLogicalDescendants().OfType<TextBlock>().Select(x => x.Text ?? "").ToList();
            Check(hints.Any(x => x == "body") && hints.Any(x => x.StartsWith("wheel")), "model slots say what they're for (body, wheel...)");
            WindowShot(w, "files_tab");

            // Quick carbox: the game's own look in one click
            var quick = await w.MakeCarbox();
            Check(quick != null && File.Exists(quick), "Quick carbox makes the stock look: " + quick);
            if (quick != null) using (var qb = Images.Load(quick)!) SavePng(qb, "quick_carbox");

            // The carbox maker: the car cut out of the view, a background, the name; saved as carbox.bmp
            var cw = new CarboxWindow(w, d);
            cw.Show(w);
            for (int i = 0; i < 40 && cw.Result == null; i++) await Wait(100);
            Check(cw.Result != null && cw.Result.Width == 256, "the carbox maker shows the carbox");
            if (cw.Result != null)
            {
                var corner = cw.Result.GetPixel(2, 250);
                Check(corner.Red > 240 && corner.Green > 240, "on white by default: " + corner);
                SavePng(cw.Result!, "carbox_stock_default");
                // Other angles and tilts, side by side; one click takes one
                for (int i = 0; i < 100 && !cw.VariationsReady; i++) await Wait(100);
                Check(cw.VariationsReady, "the maker shows the carbox from other angles and tilts");
                SaveWin(cw, "carbox_variations");
                cw.UseVariation(1);
                await Wait(200);
                Check(Math.Abs(cw.Style.Tilt + 15) < 0.01 && Math.Abs(cw.CarTurn + Carbox.StockTurn) < 0.01, $"a click on Mirrored takes its angle and tilt: turn {cw.CarTurn}, tilt {cw.Style.Tilt}");
                await CarboxText(cw);
                cw.UseLook(1); // Night: one click
                cw.CarTurn = -45;
                await cw.Shoot();
                await Wait(200);
                var dark = cw.Result!.GetPixel(8, 8);
                Check(dark.Blue > dark.Red && dark.Red < 80, "a gradient behind the car, not white: " + dark);
                SaveWin(cw, "carbox_maker");
                Check(cw.SaveBox() && cw.SavedPath != null && File.Exists(cw.SavedPath), "Save writes carbox.bmp: " + cw.SavedPath);
                using var saved = Images.Load(cw.SavedPath!);
                Check(saved != null && saved.Width == 256 && saved.Height == 256, "carbox.bmp is 256 x 256");
                Check(d.D!.TCarBox.EndsWith("carbox.bmp", StringComparison.OrdinalIgnoreCase), "the car points at it: " + d.D.TCarBox);
            }
            cw.Close();
            await Wait(300);
            w.Tabs.SelectedIndex = w.PackageTabIndex;
            await Wait(400);
            WindowShot(w, "package_tab");
            var ready = w.GetLogicalDescendants().OfType<TextBlock>().Select(x => x.Text ?? "").FirstOrDefault(x => x.Contains("The car check"));
            Check(ready != null && ready.Contains("✓ A carbox"), "the Package tab says what's ready: " + ready?.Replace(Environment.NewLine, " | "));
            Check(true, "tours done");
        }
        catch (Exception ex) { Check(false, "tours test crashed: " + ex); }
        finally
        {
            Log($"TOURS {pass} passed, {fail} failed");
            log?.Flush();
            Environment.Exit(fail == 0 ? 0 : 1);
        }
    }
}
