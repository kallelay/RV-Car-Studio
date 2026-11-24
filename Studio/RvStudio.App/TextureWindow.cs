using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RvStudio.Core;
using SkiaSharp;
using Point = Avalonia.Point;

namespace RvStudio.App;

public enum TexTool { Select, Brush, Eraser, Fill, Pick, Line, Polygon, Blur, Sharpen, Lighten, Burn }

/// <summary>
/// The UV and texture editor: the car's texture with a part's faces laid over it. Select faces and move, turn, size,
/// flip, project or unwrap them; paint with a brush, fill and pick colours; generate stripes, checks, carbon, grunge,
/// numbers, panel lines, light and shade or a whole livery, on the selected faces or the page. The car in the 3D view
/// shows it all as you go. The texture is written only by Save as a new skin or Change the car's texture; the faces'
/// places are kept with the car's other changes and written by Save (Ctrl+S).
/// </summary>
public sealed class TextureWindow : Window
{
    readonly MainWindow main;
    readonly Doc doc;
    public SKBitmap Pic;
    SKBitmap original;
    readonly bool hasAlpha;
    public bool TexDirty { get; private set; }

    public sealed record PartEntry(string Label, Selection Sel, Prm Model);
    public readonly List<PartEntry> Parts = new();
    public PartEntry? Part { get; private set; }
    public Prm? Model => Part?.Model;
    public readonly HashSet<int> Picked = new();

    public TexTool Tool { get; private set; } = TexTool.Brush;
    public float BrushSize = 8, Hardness = 0.6f, BrushOpacity = 1;
    public TexturePaint.Tip BrushTip = TexturePaint.Tip.Round;

    /// <summary>What a brush-like tool does to the pixels</summary>
    public TexturePaint.Effect EffectOf(TexTool t) => t switch
    {
        TexTool.Eraser => TexturePaint.Effect.Restore, TexTool.Blur => TexturePaint.Effect.Blur, TexTool.Sharpen => TexturePaint.Effect.Sharpen,
        TexTool.Lighten => TexturePaint.Effect.Lighten, TexTool.Burn => TexturePaint.Effect.Burn, _ => TexturePaint.Effect.Paint
    };
    public bool OnlyPicked = true;
    public SKColor Ink = new(0xD7, 0x26, 0x3D);

    /// <summary>The faces' lines: 0 Auto (dark on a light texture, light on a dark one, the selection in a colour cars
    /// rarely have), or one colour. Every line also gets a thin edge of the opposite brightness, so it reads on any texture</summary>
    public static readonly string[] LineNames = { "Auto (stands out)", "Magenta", "Red", "Lime", "Cyan", "Yellow", "Black", "Gray", "White" };
    static readonly Avalonia.Media.Color[] LineSwatch =
    {
        Colors.Magenta, Avalonia.Media.Color.FromRgb(230, 0, 190), Avalonia.Media.Color.FromRgb(235, 20, 20), Avalonia.Media.Color.FromRgb(40, 230, 40),
        Avalonia.Media.Color.FromRgb(0, 210, 255), Avalonia.Media.Color.FromRgb(255, 225, 0), Avalonia.Media.Color.FromRgb(15, 15, 18),
        Avalonia.Media.Color.FromRgb(128, 128, 128), Avalonia.Media.Color.FromRgb(248, 248, 255),
    };
    public int LineColour = Math.Clamp(Settings.UvLines, 0, LineNames.Length - 1);
    public double Dim = Math.Clamp(Settings.UvDim, 0, 0.8);

    readonly TexCanvas canvas;
    public TexCanvas Canvas => canvas;
    readonly ComboBox partBox = new() { MinWidth = 220, FontSize = 12 };
    readonly Dictionary<TexTool, ToggleButtonLike> toolButtons = new();
    readonly ColourButton inkButton;
    readonly TextBlock status = new() { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
    readonly DispatcherTimer previewTimer;
    bool previewDue;

    // Undo here: pictures for paint, the main undo for the faces (so it also undoes in the main window)
    readonly Stack<SKBitmap?> undo = new(), redo = new();
    const int Kept = 40;

    // Generator settings
    public int GenKind = 2;
    public SKColor GenA = new(0xFF, 0xFF, 0xFF), GenB = new(0x14, 0x21, 0x3D);
    public float GenSize = 0.12f, GenAngle = 0, GenAmount = 1;
    public int GenSeed = 1;
    public string GenText = "7";
    Button genButton = null!;

    public TextureWindow(MainWindow main, Doc doc, SKBitmap? start, string? sourceName, bool alpha)
    {
        this.main = main;
        this.doc = doc;
        hasAlpha = alpha;
        Pic = start != null ? TexturePaint.Working(start) : TexturePaint.Blank(256, 256, SKColors.White);
        original = Pic.Copy();
        Title = "UV and texture editor" + (sourceName != null ? " - " + sourceName : "");
        Width = 1180; Height = 780;
        MinWidth = 760; MinHeight = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Look.Panel;
        Icon = main.Icon;

        canvas = new TexCanvas(this);
        inkButton = new ColourButton(Avalonia.Media.Color.FromRgb(Ink.Red, Ink.Green, Ink.Blue));
        inkButton.Changed += c => Ink = new SKColor(c.R, c.G, c.B);

        // Top: the part, the tools, the brush
        var top = new WrapPanel { Margin = new Thickness(8, 6), Orientation = Orientation.Horizontal };
        top.Children.Add(Label("Part"));
        top.Children.Add(partBox);
        partBox.SelectionChanged += (_, _) => { if (partBox.SelectedIndex >= 0 && partBox.SelectedIndex < Parts.Count) SetPart(Parts[partBox.SelectedIndex]); };
        top.Children.Add(Gap());
        foreach (var (t, name, key, tip) in new[]
        {
            (TexTool.Select, "Select", "V", "Click faces (Shift adds, Ctrl takes away, drag a box, double click: the piece). Drag them to move, the corners to size, the round handle to turn"),
            (TexTool.Brush, "Brush", "B", "Paint. [ and ] change the size"),
            (TexTool.Eraser, "Eraser", "E", "Paints the texture back as it was when you opened it"),
            (TexTool.Fill, "Fill", "G", "Fills the joined area of one colour"),
            (TexTool.Pick, "Pick", "I", "Takes the colour under the mouse (also Alt+click with the brush)"),
            (TexTool.Line, "Line", "L", "Drag a straight line with the brush. Shift: in steps of 45 degrees"),
            (TexTool.Polygon, "Shape", "P", "Click its corners, then double click, Enter or a click on the first corner fills it. Esc lets go"),
            (TexTool.Blur, "Blur", "", "Softens what's there: hard edges, noise, pixels"),
            (TexTool.Sharpen, "Sharpen", "", "Makes what's there crisper"),
            (TexTool.Lighten, "Lighten", "", "Lightens what's there, as light falls on it (dodge)"),
            (TexTool.Burn, "Burn", "", "Darkens what's there, as dirt or shade (burn)"),
        })
        {
            var b = new ToggleButtonLike(key == "" ? name : name + " (" + key + ")", tip);
            Sym.Set(b, t switch
            {
                TexTool.Select => Sym.Pointer, TexTool.Brush => Sym.Brush, TexTool.Eraser => Sym.Eraser, TexTool.Fill => Sym.Drop, TexTool.Pick => Sym.Dropper,
                TexTool.Line => Sym.Line, TexTool.Polygon => Sym.Shape, TexTool.Blur => Sym.Waves, TexTool.Sharpen => Sym.Focus, TexTool.Lighten => Sym.Sun, _ => Sym.Flame
            }, key == "" ? name : name + " (" + key + ")");
            b.Click += (_, _) => SetTool(t);
            toolButtons[t] = b;
            top.Children.Add(b);
        }
        top.Children.Add(Gap());
        top.Children.Add(Label("Colour"));
        inkButton.Width = 80;
        top.Children.Add(inkButton);
        var tipBox = new ComboBox { ItemsSource = new[] { "Round", "Square" }, SelectedIndex = 0, FontSize = 12, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(tipBox, "The brush's tip: round, or square for crisp edges and pixel work");
        tipBox.SelectionChanged += (_, _) => { BrushTip = tipBox.SelectedIndex == 1 ? TexturePaint.Tip.Square : TexturePaint.Tip.Round; canvas.InvalidateVisual(); };
        top.Children.Add(Label("Tip"));
        top.Children.Add(tipBox);
        top.Children.Add(SmallSlide("Size", 1, 64, BrushSize, v => BrushSize = (float)v));
        top.Children.Add(SmallSlide("Hard", 0, 1, Hardness, v => Hardness = (float)v));
        top.Children.Add(SmallSlide("Opacity", 0.05, 1, BrushOpacity, v => BrushOpacity = (float)v));
        var only = new CheckBox { Content = "Only inside the selected faces", IsChecked = OnlyPicked, FontSize = 12, Margin = new Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(only, "With faces selected, paint, fill and generators stay inside them");
        only.IsCheckedChanged += (_, _) => OnlyPicked = only.IsChecked == true;
        top.Children.Add(only);

        // Right: UV, generators, the texture
        var side = new Page(doc) { Width = 300 };
        BuildSide(side);

        // Bottom: what happened, keep it
        var bottom = new DockPanel { Margin = new Thickness(8, 6), LastChildFill = true };
        var keep = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
        keep.Children.Add(Btn("Save as a new skin", async () => await SaveSkin(), "A new file car-alt1.bmp next to the texture. RVGL lists it as a skin, the original stays"));
        keep.Children.Add(Look.Primary(Btn("Change this car's texture", async () => await ApplyTexture(), "The first version stays as .bak")));
        keep.Children.Add(Btn("Close", Close));
        DockPanel.SetDock(keep, Dock.Right);
        bottom.Children.Add(keep);
        bottom.Children.Add(status);

        var dock = new DockPanel();
        DockPanel.SetDock(top, Dock.Top); dock.Children.Add(top);
        DockPanel.SetDock(bottom, Dock.Bottom); dock.Children.Add(bottom);
        DockPanel.SetDock(side, Dock.Right); dock.Children.Add(side);
        dock.Children.Add(new Border { Child = canvas, ClipToBounds = true, BorderBrush = Look.Line, BorderThickness = new Thickness(1), Margin = new Thickness(8, 0, 4, 0) });
        Content = dock;

        previewTimer = new DispatcherTimer(TimeSpan.FromMilliseconds(60), DispatcherPriority.Background, (_, _) => { if (previewDue) PushPreview(); });
        previewTimer.Start();

        LoadParts();
        SetTool(TexTool.Brush);
        Say("Paint on the texture or pick a part's faces with Select (V). Ctrl+Z undoes");
        Opened += (_, _) => { canvas.FitPage(); PushPreview(); };
        Closing += OnClosing;
        // A model read again (saved in Blender), replaced, or another car opened (a copy of a stock car): the parts follow,
        // or the faces' changes would go to models the car no longer has and never be saved
        doc.PartsChanged += OnPartsChanged;
        doc.CarOpened += OnCarOpened;
        Closed += (_, _) =>
        {
            previewTimer.Stop();
            doc.PartsChanged -= OnPartsChanged;
            doc.CarOpened -= OnCarOpened;
            main.TextureEditorClosed();
        };
        KeyDown += OnKey;
    }

    // ------------------------------------------------------------ UI bits ------------------------------------------------------------

    static TextBlock Label(string t) => new() { Text = t, FontSize = 12, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 6, 0) };
    static Control Gap() => new Border { Width = 14 };

    static Button Btn(string text, Action click, string? tip = null)
    {
        var b = new Button { Content = text, FontSize = 12, MinHeight = 28, Padding = new Thickness(10, 3) };
        b.Click += (_, _) => click();
        if (tip != null) ToolTip.SetTip(b, tip);
        return b;
    }

    static Control SmallSlide(string label, double min, double max, double start, Action<double> changed)
    {
        var s = new Slider { Minimum = min, Maximum = max, Value = start, Width = 90, Margin = new Thickness(0, -8, 0, -8), VerticalAlignment = VerticalAlignment.Center };
        var value = new TextBlock { FontSize = 11, Width = 34, VerticalAlignment = VerticalAlignment.Center, Foreground = Look.Faint };
        void Show() => value.Text = max <= 1 ? s.Value.ToString("0.00") : s.Value.ToString("0");
        Show();
        s.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty) { changed(s.Value); Show(); } };
        var p = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(8, 0, 0, 0) };
        p.Children.Add(Label(label));
        p.Children.Add(s);
        p.Children.Add(value);
        return p;
    }

    /// <summary>A button that stays lit while its tool is on.</summary>
    sealed class ToggleButtonLike : Button
    {
        protected override Type StyleKeyOverride => typeof(Button);
        public ToggleButtonLike(string text, string tip)
        {
            Content = text; FontSize = 12; MinHeight = 28; Padding = new Thickness(9, 3); Margin = new Thickness(0, 0, 4, 0);
            ToolTip.SetTip(this, tip);
        }
        public void Lit(bool on)
        {
            if (on) { Background = Look.AccentFill; Foreground = Look.OnAccent; }
            else { ClearValue(BackgroundProperty); ClearValue(ForegroundProperty); }
        }
    }

    void BuildSide(Page p)
    {
        p.Header("Faces (UV)");
        p.Choice("Lines", LineNames, () => LineColour, v => { LineColour = v; Settings.UvLines = v; canvas.InvalidateVisual(); },
            "The colour of the faces' lines. Auto: dark lines on a light texture, light on a dark one, the selected faces in magenta. Each line has a thin edge of the opposite brightness");
        p.Slide("Dim the texture", 0, 0.8, Dim, v => { Dim = v; Settings.UvDim = v; canvas.InvalidateVisual(); }, "Darkens the texture under the lines while you work on the faces (only here, nothing is painted)");
        p.Info(() => Model == null ? "This car has no textured parts" :
            $"{Picked.Count} of {Model.Faces.Count(UvKit.Textured)} faces selected, {UvKit.Islands(Model).Count} pieces. They cover {UvKit.Coverage(Model, AllFaces()) * 100:0}% of the page");
        p.Button("Select all (A)", () => { PickAll(); });
        p.Button("Select the whole piece", () => { GrowToIslands(); }, "Each selected face's joined piece (double click does it on one face)");
        p.Button("Flip across", () => UvEdit("Flip UVs", f => UvKit.Flip(Model!, f, true)));
        p.Button("Flip up and down", () => UvEdit("Flip UVs", f => UvKit.Flip(Model!, f, false)));
        p.Button("Turn a quarter (R)", () => UvEdit("Turn UVs", f => UvKit.Rotate(Model!, f, 90, null, Pic.Width / (float)Pic.Height)));
        p.Button("Snap to pixels", () => UvEdit("Snap UVs", f => UvKit.Snap(Model!, f, Pic.Width, Pic.Height)), "Every corner onto a pixel corner: crisp edges in the game");
        p.Button("Fit to the page", () => UvEdit("Fit UVs", f => UvKit.Fit(Model!, f, new Vector2(0.01f), new Vector2(0.99f))));
        p.Choice("Project from", new[] { "The side", "Above", "The front" }, () => projectAxis, v => projectAxis = v, "A flat picture of the faces as seen from there, fitted where they were");
        p.Button("Project", () => UvEdit("Project UVs", f => UvKit.Project(Model!, f, projectAxis)));
        p.Button("Unwrap by sides", () => UvEdit("Unwrap UVs", f => { int n = UvKit.Unwrap(Model!, f); Say($"Unwrapped into {n} pieces, all at one scale, packed into the page"); }),
            "Faces split by the way they face (sides, top, bottom, front, back), each laid flat, packed into the page");
        p.Button("Pack the pieces", () => UvEdit("Pack UVs", f => UvKit.Pack(Model!, UvKit.Islands(Model!, f))), "Lays the pieces out side by side, filling the page, all at one scale");
        p.Button("Save the layout as a picture...", async () => await ExportTemplate(), "The texture with the faces' edges on it, to paint over in another editor");
        p.Note("Faces with nothing selected: the buttons act on all of them. Arrows nudge a pixel (Shift: 10).");

        p.Header("Generate");
        p.Choice("Kind", TexturePaint.Kinds, () => GenKind, v => { GenKind = v; }, "What to make. Panel lines, a colour per piece and light and shade follow the faces");
        p.Colour("Colour", Avalonia.Media.Color.FromRgb(GenA.Red, GenA.Green, GenA.Blue), c => GenA = new SKColor(c.R, c.G, c.B));
        p.Colour("Second colour", Avalonia.Media.Color.FromRgb(GenB.Red, GenB.Green, GenB.Blue), c => GenB = new SKColor(c.R, c.G, c.B));
        p.Slide("Size", 0.01, 0.5, GenSize, v => GenSize = (float)v);
        p.Slide("Angle", -180, 180, GenAngle, v => GenAngle = (float)v);
        p.Slide("Amount", 0, 1, GenAmount, v => GenAmount = (float)v, "How strong: 1 covers, less lets the texture show through");
        p.Text("Number", () => GenText, v => GenText = v);
        p.Int("Seed", () => GenSeed, v => GenSeed = v, "Another number: another pattern for grunge and colours per piece", 0, 99999);
        genButton = p.Button("Generate", () => Generate(), null, true);
        p.Button("Make a livery", () => Livery(), "A whole new paint job on the selected faces (or the whole part): a colour, stripes, panel lines, light and shade and a number. Again for another");

        p.Header("Texture");
        p.Choice("New page size", new[] { "256", "512", "1024" }, () => newSize, v => newSize = v);
        p.Button("New blank page", () => NewPage(), "A white page of that size. The faces keep their places");
        p.Button("Bring in a picture...", async () => await Import(), "An image file as the texture, stretched to the page");
        p.Button("Back to the texture as it was", () => { PushUndo(); Pic.Dispose(); Pic = original.Copy(); TexDirty = false; Changed(); });
        p.Header("Undo");
        p.Button("Undo (Ctrl+Z)", Undo);
        p.Button("Redo (Ctrl+Y)", Redo);
    }

    int projectAxis, newSize = 1;

    void Say(string s) => status.Text = s;

    // ------------------------------------------------------------ parts and selection ------------------------------------------------------------

    public void LoadParts()
    {
        Parts.Clear();
        var seen = new HashSet<Prm>();
        void Add(string label, Selection s)
        {
            if (!doc.HasModel(s)) return;
            foreach (var m in doc.ModelsOf(s))
                if (seen.Add(m) && m.Faces.Any(UvKit.Textured))
                    Parts.Add(new PartEntry(label + "  (" + Path.GetFileName(doc.ModelName(m) ?? "?") + ")", s, m));
        }
        if (doc.D != null)
        {
            Add("Body", new Selection(SelKind.Body));
            for (int i = 0; i < 4; i++) Add("Wheel", new Selection(SelKind.Wheel, i));
            for (int i = 0; i < 4; i++) Add("Spring", new Selection(SelKind.Spring, i));
            for (int i = 0; i < 4; i++) Add("Axle", new Selection(SelKind.Axle, i));
            for (int i = 0; i < 4; i++) Add("Pin", new Selection(SelKind.Pin, i));
            Add("Spinner", new Selection(SelKind.Spinner));
            Add("Aerial", new Selection(SelKind.AerialBase));
            Add("Aerial top", new Selection(SelKind.AerialTip));
        }
        partBox.ItemsSource = Parts.Select(x => x.Label).ToList();
        var keep = Parts.FindIndex(x => x.Model == Part?.Model);
        if (keep < 0 && Part != null) keep = Parts.FindIndex(x => x.Sel == Part.Sel);   // the same part, its model read again
        int idx = keep >= 0 ? keep : Parts.Count > 0 ? 0 : -1;
        partBox.SelectedIndex = idx;
        // The box may not report a change (same index): the part must still be the new entry, not the old model
        var want = idx >= 0 ? Parts[idx] : null;
        if (!ReferenceEquals(Part?.Model, want?.Model)) SetPart(want);
    }

    void OnPartsChanged()
    {
        if (Parts.Any(p => !doc.ModelsOf(p.Sel).Contains(p.Model))) { LoadParts(); Say("The car's models changed: the parts are read again"); }
    }

    void OnCarOpened()
    {
        LoadParts();
        Title = "UV and texture editor - " + (doc.D?.Name ?? "");
    }

    public void SetPart(PartEntry? p)
    {
        Part = p;
        Picked.Clear();
        canvas.InvalidateVisual();
        Refresh();
    }

    /// <summary>Picks the part whose label starts with `label` (Body, Wheel...). False if there's none.</summary>
    public bool ChoosePart(string label)
    {
        int i = Parts.FindIndex(x => x.Label.StartsWith(label, StringComparison.OrdinalIgnoreCase));
        if (i < 0) return false;
        partBox.SelectedIndex = i;
        SetPart(Parts[i]);
        return true;
    }

    public IEnumerable<int> AllFaces() => Model == null ? Enumerable.Empty<int>() : Enumerable.Range(0, Model.Faces.Length).Where(i => UvKit.Textured(Model.Faces[i]));

    /// <summary>What the UV buttons act on: the selected faces, or all with none selected.</summary>
    public List<int> Target() => Picked.Count > 0 ? Picked.ToList() : AllFaces().ToList();

    public void PickAll() { Picked.Clear(); foreach (var f in AllFaces()) Picked.Add(f); canvas.InvalidateVisual(); Refresh(); }

    void GrowToIslands()
    {
        if (Model == null || Picked.Count == 0) return;
        foreach (var isl in UvKit.Islands(Model)) if (isl.Any(Picked.Contains)) foreach (var f in isl) Picked.Add(f);
        canvas.InvalidateVisual(); Refresh();
    }

    public void Refresh()
    {
        if (Content is DockPanel d) foreach (var c in d.Children) if (c is Page pg) pg.Refresh();
        if (genButton != null) genButton.Content = Picked.Count > 0 && OnlyPicked ? $"Generate on the {Picked.Count} selected faces" : "Generate on the whole page";
    }

    public void SetTool(TexTool t)
    {
        Tool = t;
        foreach (var (k, b) in toolButtons) b.Lit(k == t);
        canvas.Cursor = new Cursor(t == TexTool.Select ? StandardCursorType.Arrow : StandardCursorType.Cross);
        canvas.InvalidateVisual();
    }

    // ------------------------------------------------------------ faces' edits ------------------------------------------------------------

    /// <summary>A model others use is asked about once; false while asking.</summary>
    bool UvAllowed()
    {
        if (Model == null || doc.ModelName(Model) is not { } rel || doc.SharedProblem(rel) is not { } why) return Model != null;
        _ = AskShared(rel, why);
        return false;
    }

    async Task AskShared(string rel, string why)
    {
        if (!await main.AskSharedFiles(new() { (rel, why) })) return;
        LoadParts();
        Say("You can change its faces now");
    }

    /// <summary>Starts a change of the faces' places that undo (here and in the main window) can take back.</summary>
    public bool BeginUv(string label)
    {
        if (!UvAllowed()) return false;
        doc.EditModels(label, new[] { Model! }, () => { });
        undo.Push(null);
        ClearRedo();
        return true;
    }

    public void UvChanged()
    {
        doc.ModelVersion++;
        doc.Touch();
        canvas.InvalidateVisual();
        Refresh();
    }

    public void UvEdit(string label, Action<List<int>> change)
    {
        if (Model == null) { Say("No textured part"); return; }
        var target = Target();
        if (target.Count == 0 || !BeginUv(label)) return;
        change(target);
        UvChanged();
        if (!status.Text!.StartsWith("Unwrapped")) Say(label + $": {target.Count} faces. Save (Ctrl+S in the main window) writes the model");
    }

    // ------------------------------------------------------------ painting ------------------------------------------------------------

    public SKPath? Mask() => OnlyPicked && Picked.Count > 0 && Model != null ? UvKit.Outline(Model, Picked, Pic.Width, Pic.Height) : null;

    public void PushUndo()
    {
        undo.Push(Pic.Copy());
        ClearRedo();
        Trim();
    }

    void Trim()
    {
        if (undo.Count <= Kept) return;
        var all = undo.ToArray(); // newest first
        undo.Clear();
        for (int i = Kept - 1; i >= 0; i--) undo.Push(all[i]);
        for (int i = Kept; i < all.Length; i++) all[i]?.Dispose();
    }

    void ClearRedo() { while (redo.Count > 0) redo.Pop()?.Dispose(); }

    /// <summary>The texture changed: show it here and soon on the car.</summary>
    public void Changed()
    {
        TexDirty = true;
        canvas.PictureChanged();
        previewDue = true;
    }

    public void Undo()
    {
        if (undo.Count == 0) { Say("Nothing to undo here"); return; }
        var top = undo.Pop();
        if (top == null)
        {
            // Only if the main window's newest step is still this change of the faces, not a wheel moved there since
            if (!doc.UndoTopHas(Parts.Select(x => x.Model))) { undo.Push(null); Say("The main window has changes made after this one: undo them there first (Ctrl+Z)"); return; }
            doc.Undo(); redo.Push(null); canvas.InvalidateVisual(); Refresh(); Say("Undid a change of the faces"); return;
        }
        redo.Push(Pic);
        Pic = top;
        Changed();
        Say("Undid the painting");
    }

    public void Redo()
    {
        if (redo.Count == 0) return;
        var top = redo.Pop();
        if (top == null)
        {
            if (!doc.RedoTopHas(Parts.Select(x => x.Model))) { redo.Push(null); Say("The main window's redo isn't this change of the faces now: redo there first (Ctrl+Y)"); return; }
            doc.Redo(); undo.Push(null); canvas.InvalidateVisual(); Refresh(); return;
        }
        undo.Push(Pic);
        Pic = top;
        Changed();
    }

    public void Generate()
    {
        PushUndo();
        using var mask = Mask();
        var faces = Picked.Count > 0 ? Picked.ToList() : AllFaces().ToList();
        TexturePaint.Generate(Pic, GenKind, Options(), mask, Model, faces);
        Changed();
        Say(TexturePaint.Kinds[GenKind] + (mask != null ? $" on the {Picked.Count} selected faces" : " on the page"));
    }

    TexturePaint.GenOptions Options() => new(GenA, GenB, GenSize, GenAngle, GenAmount, GenSeed, GenText);

    readonly Random liveryRandom = new();

    /// <summary>A whole paint job in one go: base colour, stripes, panel lines, light and shade, a number.</summary>
    public void Livery(int? seed = null)
    {
        var r = seed is { } s ? new Random(s) : liveryRandom;
        PushUndo();
        float hue = (float)r.NextDouble() * 360;
        var baseC = SKColor.FromHsl(hue, 55 + (float)r.NextDouble() * 35, 35 + (float)r.NextDouble() * 20);
        var stripe = r.Next(3) switch { 0 => SKColors.White, 1 => SKColor.FromHsl((hue + 180) % 360, 80, 55), _ => new SKColor(20, 20, 24) };
        var edge = SKColor.FromHsl((hue + 30) % 360, 70, 70);
        var faces = Picked.Count > 0 ? Picked.ToList() : AllFaces().ToList();
        using var mask = Model != null ? UvKit.Outline(Model, faces, Pic.Width, Pic.Height) : null;
        TexturePaint.Generate(Pic, 0, new(baseC, baseC), mask, Model, faces);
        TexturePaint.Generate(Pic, 2, new(stripe, edge, 0.05f + (float)r.NextDouble() * 0.08f, r.Next(2) == 0 ? 0 : 90), mask, Model, faces);
        TexturePaint.Generate(Pic, 9, new(SKColors.Black, SKColors.Black, Amount: 0.7f), mask, Model, faces);
        TexturePaint.Generate(Pic, 7, new(new SKColor(0, 0, 0, 170), new SKColor(255, 255, 255, 60), 0.1f), mask, Model, faces);
        // The number on the biggest piece that faces sideways
        if (Model != null)
        {
            var sideways = UvKit.Islands(Model, faces).Where(i => UvKit.Facing(Model, i[0]).Axis == 0)
                .OrderByDescending(i => { var (lo, hi) = UvKit.Bounds(Model, i); return (hi - lo).X * (hi - lo).Y; }).FirstOrDefault();
            if (sideways != null)
            {
                using var plate = UvKit.Outline(Model, sideways, Pic.Width, Pic.Height);
                TexturePaint.Generate(Pic, 6, new(new SKColor(20, 20, 24), SKColors.White, Amount: 0.7f, Text: (1 + r.Next(99)).ToString()), plate);
            }
        }
        Changed();
        Say("A new livery. Again for another, Ctrl+Z to go back");
    }

    void NewPage()
    {
        int s = newSize switch { 0 => 256, 2 => 1024, _ => 512 };
        PushUndo();
        Pic = TexturePaint.Blank(s, s, SKColors.White);
        Changed();
        canvas.FitPage();
        Say($"A blank {s} x {s} page");
    }

    async Task Import()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "A picture for the texture",
            FileTypeFilter = new[] { new FilePickerFileType("Pictures") { Patterns = new[] { "*.bmp", "*.png", "*.jpg", "*.jpeg" } } }
        });
        if (files.Count == 0 || files[0].TryGetLocalPath() is not { } path) return;
        using var src = Images.Load(path);
        if (src == null) { Say("Couldn't read " + Path.GetFileName(path)); return; }
        PushUndo();
        var b = TexturePaint.Blank(Pic.Width, Pic.Height, SKColors.Black);
        using (var c = new SKCanvas(b)) c.DrawBitmap(src, new SKRect(0, 0, b.Width, b.Height), new SKPaint { FilterQuality = SKFilterQuality.High });
        Pic = b;
        Changed();
        Say("Brought in " + Path.GetFileName(path));
    }

    async Task ExportTemplate()
    {
        if (Model == null) return;
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save the layout",
            SuggestedFileName = Path.GetFileNameWithoutExtension(doc.ModelName(Model) ?? "layout") + "_uv.png",
            FileTypeChoices = new[] { new FilePickerFileType("PNG") { Patterns = new[] { "*.png" } } }
        });
        if (file?.TryGetLocalPath() is not { } path) return;
        try { SaveTemplate(path); Say("Layout saved: " + path); }
        catch (Exception ex) { Say("Couldn't save the layout: " + ex.Message); }
    }

    public void SaveTemplate(string path)
    {
        using var t = UvKit.Template(Model!, AllFaces(), Math.Max(512, Pic.Width), Math.Max(512, Pic.Height), Pic);
        using var data = t.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
    }

    // ------------------------------------------------------------ the car's view ------------------------------------------------------------

    /// <summary>The picture onto the car in the 3D view, as the game shows it.</summary>
    public void PushPreview()
    {
        previewDue = false;
        int w = Pic.Width, h = Pic.Height;
        var px = Pic.GetPixelSpan();
        var o = new byte[w * h * 4];
        for (int i = 0; i < w * h; i++)
        {
            byte b = px[i * 4], g = px[i * 4 + 1], r = px[i * 4 + 2], a = px[i * 4 + 3];
            if (a > 0 && a < 255) { r = (byte)Math.Min(255, r * 255 / a); g = (byte)Math.Min(255, g * 255 / a); b = (byte)Math.Min(255, b * 255 / a); }
            // Without an alpha channel pure black is see-through in the game
            if (!hasAlpha && r == 0 && g == 0 && b == 0) a = 0;
            o[i * 4] = r; o[i * 4 + 1] = g; o[i * 4 + 2] = b; o[i * 4 + 3] = a;
        }
        doc.PaintPreview = o;
        doc.PaintW = w; doc.PaintH = h;
        doc.PaintVersion++;
        main.View.Redraw();
    }

    bool KeepAlpha()
    {
        if (hasAlpha) return true;
        return false;
    }

    /// <summary>A new skin file. False (and why, in the status line) if it couldn't be written: the painting stays here</summary>
    public async Task<bool> SaveSkin()
    {
        string? path;
        try { path = main.PaintSaveSkin(Pic, KeepAlpha()); }
        catch (Exception ex) { Say("Couldn't save the skin: " + ex.Message); await Dialogs.Ask(this, "Couldn't save the skin:\n" + ex.Message, "OK"); return false; }
        if (path == null) { Say("Couldn't save the skin (the car has no texture file)"); return false; }
        TexDirty = false;
        original.Dispose(); original = Pic.Copy();
        PushPreview();
        Say("Saved as a skin: " + Path.GetFileName(path));
        return true;
    }

    public async Task<bool> ApplyTexture()
    {
        try { if (!await main.PaintApply(Pic, KeepAlpha())) return false; }
        catch (Exception ex) { Say("Couldn't change the texture: " + ex.Message); await Dialogs.Ask(this, "Couldn't change the texture:\n" + ex.Message, "OK"); return false; }
        TexDirty = false;
        original.Dispose(); original = Pic.Copy();
        PushPreview();
        Say("The car's texture is changed. The first version stays as .bak");
        return true;
    }

    bool closingAsked;
    async void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (!TexDirty || closingAsked) return;
        e.Cancel = true;
        await AskClose();
    }

    /// <summary>Closes, asking first about an unsaved painting. False: it stays open (Cancel, or the save failed)</summary>
    public async Task<bool> AskClose()
    {
        if (TexDirty && !closingAsked)
        {
            int r = await Dialogs.Ask(this, "The painting isn't saved.", "Save as a new skin", "Change this car's texture", "Throw it away", "Cancel");
            if (r == 3 || r < 0) return false;
            if (r == 0 && !await SaveSkin()) return false;
            if (r == 1 && !await ApplyTexture()) return false;
        }
        closingAsked = true;
        Close();
        return true;
    }

    /// <summary>Closes without asking, the painting thrown away (tests).</summary>
    public void CloseDiscarding() { closingAsked = true; Close(); }

    // ------------------------------------------------------------ keys ------------------------------------------------------------

    void OnKey(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox) return;
        bool ctrl = e.KeyModifiers.HasFlag(KeyModifiers.Control), shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        float px = shift ? 10 : 1;
        switch (e.Key)
        {
            case Key.Z when ctrl && shift: Redo(); break;
            case Key.Z when ctrl: Undo(); break;
            case Key.Y when ctrl: Redo(); break;
            case Key.V: SetTool(TexTool.Select); break;
            case Key.B: SetTool(TexTool.Brush); break;
            case Key.E: SetTool(TexTool.Eraser); break;
            case Key.G: SetTool(TexTool.Fill); break;
            case Key.I: SetTool(TexTool.Pick); break;
            case Key.L: SetTool(TexTool.Line); break;
            case Key.P: SetTool(TexTool.Polygon); break;
            case Key.Enter when Tool == TexTool.Polygon: canvas.ClosePolygon(); break;
            case Key.A: PickAll(); break;
            case Key.Escape when canvas.PolygonPoints.Count > 0: canvas.DropPolygon(); break;
            case Key.Escape: Picked.Clear(); canvas.InvalidateVisual(); Refresh(); break;
            case Key.F: canvas.FitPage(); break;
            case Key.R: UvEdit("Turn UVs", f => UvKit.Rotate(Model!, f, shift ? -90 : 90, null, Pic.Width / (float)Pic.Height)); break;
            case Key.H: UvEdit("Flip UVs", f => UvKit.Flip(Model!, f, true)); break;
            case Key.OemOpenBrackets: BrushSize = MathF.Max(1, BrushSize / 1.25f); canvas.InvalidateVisual(); break;
            case Key.OemCloseBrackets: BrushSize = MathF.Min(128, BrushSize * 1.25f); canvas.InvalidateVisual(); break;
            case Key.Left when Picked.Count > 0: UvEdit("Nudge UVs", f => UvKit.Move(Model!, f, new Vector2(-px / Pic.Width, 0))); break;
            case Key.Right when Picked.Count > 0: UvEdit("Nudge UVs", f => UvKit.Move(Model!, f, new Vector2(px / Pic.Width, 0))); break;
            case Key.Up when Picked.Count > 0: UvEdit("Nudge UVs", f => UvKit.Move(Model!, f, new Vector2(0, -px / Pic.Height))); break;
            case Key.Down when Picked.Count > 0: UvEdit("Nudge UVs", f => UvKit.Move(Model!, f, new Vector2(0, px / Pic.Height))); break;
            default: return;
        }
        e.Handled = true;
    }

    // ------------------------------------------------------------ the canvas ------------------------------------------------------------

    enum Drag { None, Pan, Move, Scale, Rotate, Box, Paint, Line }

    /// <summary>The page with the faces over it. Wheel zooms about the mouse, middle drag (or Space) pans.</summary>
    public sealed class TexCanvas : Control
    {
        readonly TextureWindow w;
        WriteableBitmap? shown;
        public double Zoom = 1;          // screen pixels per texture pixel
        public Point Origin;             // where the page's top left is
        Drag drag;
        Point dragFrom, last;
        Vector2 uvFrom;
        float[]? uvStart;
        int corner;
        Vector2 anchor;
        SKPoint lastPix;
        Point mouse;
        bool spaceDown;
        int hover = -1;

        public TexCanvas(TextureWindow w)
        {
            this.w = w;
            Focusable = true;
            ClipToBounds = true;
            RenderOptions.SetBitmapInterpolationMode(this, Avalonia.Media.Imaging.BitmapInterpolationMode.None);
            KeyDown += (_, e) => { if (e.Key == Key.Space) spaceDown = true; };
            KeyUp += (_, e) => { if (e.Key == Key.Space) spaceDown = false; };
        }

        double PW => w.Pic.Width;
        double PH => w.Pic.Height;

        public void FitPage()
        {
            var b = Bounds.Size;
            if (b.Width < 10) b = new Size(800, 640);
            Zoom = Math.Min((b.Width - 40) / PW, (b.Height - 40) / PH);
            Origin = new Point((b.Width - PW * Zoom) / 2, (b.Height - PH * Zoom) / 2);
            InvalidateVisual();
        }

        public void PictureChanged() { shown?.Dispose(); shown = null; light = null; InvalidateVisual(); }

        // How light the texture is (0..1), for Auto lines; read again when the picture changes
        double? light;
        double Light()
        {
            if (light is double l) return l;
            var b = w.Pic;
            double sum = 0; int n = 0;
            int sx = Math.Max(1, b.Width / 48), sy = Math.Max(1, b.Height / 48);
            for (int y = 0; y < b.Height; y += sy)
                for (int x = 0; x < b.Width; x += sx)
                {
                    var c = b.GetPixel(x, y);
                    sum += (0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue) / 255.0 * (c.Alpha / 255.0) + (1 - c.Alpha / 255.0) * 0.33;
                    n++;
                }
            return (light = n == 0 ? 0.5 : sum / n).Value;
        }

        /// <summary>The lines' colour, the selection's, and the thin edge under both</summary>
        (Avalonia.Media.Color Line, Avalonia.Media.Color Pick, Avalonia.Media.Color Edge) LineColours()
        {
            static double Lum(Avalonia.Media.Color c) => (0.299 * c.R + 0.587 * c.G + 0.114 * c.B) / 255.0;
            // On a dim texture the page is darker than the picture itself
            double bright = Light() * (1 - w.Dim);
            if (w.LineColour == 0)
            {
                var line = bright > 0.5 ? Avalonia.Media.Color.FromRgb(20, 20, 28) : Avalonia.Media.Color.FromRgb(245, 245, 250);
                var pick = Avalonia.Media.Color.FromRgb(230, 0, 190);
                return (line, pick, bright > 0.5 ? Colors.White : Colors.Black);
            }
            var c = LineSwatch[w.LineColour];
            return (c, c, Lum(c) > 0.55 ? Colors.Black : Colors.White);
        }

        public Point ToScreen(Vector2 uv) => new(Origin.X + uv.X * PW * Zoom, Origin.Y + uv.Y * PH * Zoom);
        public Vector2 ToUv(Point p) => new((float)((p.X - Origin.X) / (PW * Zoom)), (float)((p.Y - Origin.Y) / (PH * Zoom)));
        SKPoint ToPix(Point p) => new((float)((p.X - Origin.X) / Zoom), (float)((p.Y - Origin.Y) / Zoom));

        unsafe WriteableBitmap Shown()
        {
            if (shown != null && shown.PixelSize.Width == w.Pic.Width && shown.PixelSize.Height == w.Pic.Height) return shown;
            shown?.Dispose();
            shown = new WriteableBitmap(new PixelSize(w.Pic.Width, w.Pic.Height), new Avalonia.Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
            using var fb = shown.Lock();
            var src = w.Pic.GetPixelSpan();
            for (int y = 0; y < w.Pic.Height; y++)
                src.Slice(y * w.Pic.RowBytes, w.Pic.Width * 4).CopyTo(new Span<byte>((byte*)fb.Address + y * fb.RowBytes, w.Pic.Width * 4));
            return shown;
        }

        /// <summary>The topmost face under a point, -1 for none.</summary>
        public int FaceAt(Point p)
        {
            if (w.Model is not { } m) return -1;
            var uv = ToUv(p);
            for (int i = m.Faces.Length - 1; i >= 0; i--)
            {
                var f = m.Faces[i];
                if (!UvKit.Textured(f)) continue;
                if (Inside(uv, UvKit.Uv(f, 0), UvKit.Uv(f, 1), UvKit.Uv(f, 2)) || (f.IsQuad && Inside(uv, UvKit.Uv(f, 0), UvKit.Uv(f, 2), UvKit.Uv(f, 3)))) return i;
            }
            return -1;
        }

        static bool Inside(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = Cross(p, a, b), d2 = Cross(p, b, c), d3 = Cross(p, c, a);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        static float Cross(Vector2 p, Vector2 a, Vector2 b) => (p.X - b.X) * (a.Y - b.Y) - (a.X - b.X) * (p.Y - b.Y);

        Rect PickedRect()
        {
            if (w.Model == null || w.Picked.Count == 0) return default;
            var (lo, hi) = UvKit.Bounds(w.Model, w.Picked);
            return new Rect(ToScreen(lo), ToScreen(hi));
        }

        Point[] Handles(Rect r) => new[] { r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft };
        Point RotateHandle(Rect r) => new(r.Center.X, r.Top - 26);

        static readonly IBrush Checker1 = new SolidColorBrush(Avalonia.Media.Color.FromRgb(70, 70, 76)), Checker2 = new SolidColorBrush(Avalonia.Media.Color.FromRgb(90, 90, 96));

        public override void Render(DrawingContext dc)
        {
            dc.FillRectangle(new SolidColorBrush(Avalonia.Media.Color.FromRgb(36, 36, 40)), new Rect(Bounds.Size));
            var page = new Rect(Origin, new Size(PW * Zoom, PH * Zoom));
            // See-through shows as checks
            double cs = 12;
            using (dc.PushClip(page))
                for (double y = page.Top; y < page.Bottom; y += cs)
                    for (double x = page.Left; x < page.Right; x += cs)
                        dc.FillRectangle((((int)((x - page.Left) / cs) + (int)((y - page.Top) / cs)) & 1) == 0 ? Checker1 : Checker2, new Rect(x, y, cs, cs));
            dc.DrawImage(Shown(), new Rect(0, 0, PW, PH), page);
            if (w.Dim > 0.001 && w.Model != null) dc.FillRectangle(new SolidColorBrush(Colors.Black, w.Dim), page);
            dc.DrawRectangle(null, new Pen(new SolidColorBrush(Colors.White, 0.4), 1), page);

            if (w.Model is { } m)
            {
                var (lineC, acc, edgeC) = LineColours();
                double op = w.Tool == TexTool.Select ? 0.9 : 0.6;
                var thin = new Pen(new SolidColorBrush(lineC, op), 1);
                var thinEdge = new Pen(new SolidColorBrush(edgeC, op * 0.55), 3) { LineJoin = PenLineJoin.Round };
                var pickPen = new Pen(new SolidColorBrush(acc), 1.5);
                var pickEdge = new Pen(new SolidColorBrush(edgeC, 0.7), 3.5) { LineJoin = PenLineJoin.Round };
                var pickFill = new SolidColorBrush(acc, 0.28);
                var all = new StreamGeometry();
                var sel = new StreamGeometry();
                using (var g = all.Open())
                using (var gs = sel.Open())
                    for (int i = 0; i < m.Faces.Length; i++)
                    {
                        var f = m.Faces[i];
                        if (!UvKit.Textured(f)) continue;
                        var ctx = w.Picked.Contains(i) ? gs : g;
                        ctx.BeginFigure(ToScreen(UvKit.Uv(f, 0)), true);
                        for (int c = 1; c < f.Corners; c++) ctx.LineTo(ToScreen(UvKit.Uv(f, c)));
                        ctx.EndFigure(true);
                    }
                // The edge first, then the line on it: readable on white, black or anything between
                dc.DrawGeometry(null, thinEdge, all);
                dc.DrawGeometry(null, thin, all);
                dc.DrawGeometry(pickFill, pickEdge, sel);
                dc.DrawGeometry(null, pickPen, sel);
                if (hover >= 0 && hover < m.Faces.Length && w.Tool == TexTool.Select)
                {
                    var f = m.Faces[hover];
                    var hg = new StreamGeometry();
                    using (var g = hg.Open())
                    {
                        g.BeginFigure(ToScreen(UvKit.Uv(f, 0)), true);
                        for (int c = 1; c < f.Corners; c++) g.LineTo(ToScreen(UvKit.Uv(f, c)));
                        g.EndFigure(true);
                    }
                    dc.DrawGeometry(null, new Pen(new SolidColorBrush(edgeC), 4), hg);
                    dc.DrawGeometry(null, new Pen(new SolidColorBrush(acc), 2), hg);
                }
                if (w.Tool == TexTool.Select && w.Picked.Count > 0)
                {
                    var r = PickedRect();
                    var dash = new Pen(new SolidColorBrush(acc), 1) { DashStyle = DashStyle.Dash };
                    dc.DrawRectangle(null, dash, r);
                    foreach (var h in Handles(r)) dc.DrawRectangle(Brushes.White, new Pen(new SolidColorBrush(acc), 1.5), new Rect(h.X - 5, h.Y - 5, 10, 10));
                    var rh = RotateHandle(r);
                    dc.DrawLine(dash, new Point(r.Center.X, r.Top), rh);
                    dc.DrawEllipse(Brushes.White, new Pen(new SolidColorBrush(acc), 1.5), rh, 6, 6);
                }
            }
            if (drag == Drag.Box)
                dc.DrawRectangle(new SolidColorBrush(Colors.White, 0.08), new Pen(Brushes.White, 1) { DashStyle = DashStyle.Dash }, new Rect(dragFrom, last));
            if (w.Tool is TexTool.Brush or TexTool.Eraser or TexTool.Blur or TexTool.Sharpen or TexTool.Lighten or TexTool.Burn or TexTool.Line)
            {
                double rr = w.BrushSize * Zoom;
                if (w.BrushTip == TexturePaint.Tip.Square) dc.DrawRectangle(null, new Pen(Brushes.White, 1), new Rect(mouse.X - rr, mouse.Y - rr, rr * 2, rr * 2));
                else dc.DrawEllipse(null, new Pen(Brushes.White, 1), mouse, rr, rr);
            }
            // The line being dragged, the shape being clicked out
            var inkPen = new Pen(new SolidColorBrush(Avalonia.Media.Color.FromRgb(w.Ink.Red, w.Ink.Green, w.Ink.Blue)), Math.Max(1, w.BrushSize * 2 * Zoom)) { LineCap = PenLineCap.Round };
            if (drag == Drag.Line) dc.DrawLine(inkPen, ScreenOf(lineFrom), ScreenOf(LineEnd(mouse)));
            if (poly.Count > 0)
            {
                var edge = new Pen(Brushes.White, 1.5) { DashStyle = DashStyle.Dash };
                for (int i = 0; i + 1 < poly.Count; i++) dc.DrawLine(edge, ScreenOf(poly[i]), ScreenOf(poly[i + 1]));
                dc.DrawLine(edge, ScreenOf(poly[^1]), mouse);
                foreach (var q in poly) dc.DrawRectangle(Brushes.White, null, new Rect(ScreenOf(q).X - 3, ScreenOf(q).Y - 3, 6, 6));
            }
        }

        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            var p = e.GetPosition(this);
            double f = e.Delta.Y > 0 ? 1.2 : 1 / 1.2;
            double z = Math.Clamp(Zoom * f, 0.1, 64);
            Origin = p - (p - Origin) * (z / Zoom);
            Zoom = z;
            InvalidateVisual();
            e.Handled = true;
        }

        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            Focus();
            var p = e.GetPosition(this);
            var pt = e.GetCurrentPoint(this).Properties;
            dragFrom = last = p;
            e.Pointer.Capture(this);
            if (pt.IsMiddleButtonPressed || pt.IsRightButtonPressed || spaceDown) { drag = Drag.Pan; return; }
            if (!pt.IsLeftButtonPressed) return;
            var mods = e.KeyModifiers;
            switch (w.Tool)
            {
                case TexTool.Select: PressSelect(p, mods, e.ClickCount); break;
                case TexTool.Pick: PickAt(p); break;
                case TexTool.Brush when mods.HasFlag(KeyModifiers.Alt): PickAt(p); break;
                case TexTool.Line:
                    drag = Drag.Line;
                    lineFrom = ToPix(p);
                    lineShift = mods.HasFlag(KeyModifiers.Shift);
                    break;
                case TexTool.Polygon:
                    {
                        var q = ToPix(p);
                        // Double click, or a click on the first corner: the shape is filled
                        if (poly.Count >= 3 && (e.ClickCount >= 2 || (Math.Abs(ScreenOf(poly[0]).X - p.X) < 8 && Math.Abs(ScreenOf(poly[0]).Y - p.Y) < 8))) { ClosePolygon(); break; }
                        poly.Add(q);
                        w.Say($"Shape: {poly.Count} corners. Double click, Enter or the first corner fills it; Esc lets go");
                        InvalidateVisual();
                        break;
                    }
                case TexTool.Fill:
                    {
                        var px = ToPix(p);
                        w.PushUndo();
                        using var mask = w.Mask();
                        int n = TexturePaint.Fill(w.Pic, (int)px.X, (int)px.Y, w.Ink, 0.12f, mask);
                        w.Changed();
                        w.Say($"Filled {n} pixels");
                        break;
                    }
                default:
                    {
                        w.PushUndo();
                        drag = Drag.Paint;
                        lastPix = ToPix(p);
                        PaintTo(lastPix, true);
                        break;
                    }
            }
        }

        void PickAt(Point p)
        {
            var px = ToPix(p);
            var c = TexturePaint.Pick(w.Pic, (int)px.X, (int)px.Y);
            w.Ink = c.WithAlpha(255);
            w.inkButton.SetValue(Avalonia.Media.Color.FromRgb(c.Red, c.Green, c.Blue));
            w.Say($"Colour #{c.Red:X2}{c.Green:X2}{c.Blue:X2}");
        }

        // ---- line and shape ----
        SKPoint lineFrom;
        bool lineShift;
        readonly List<SKPoint> poly = new();
        public IReadOnlyList<SKPoint> PolygonPoints => poly;

        Point ScreenOf(SKPoint px) => new(Origin.X + px.X * Zoom, Origin.Y + px.Y * Zoom);

        /// <summary>Where the line ends: at the mouse, or with Shift in steps of 45 degrees</summary>
        SKPoint LineEnd(Point at)
        {
            var to = ToPix(at);
            if (!lineShift) return to;
            float dx = to.X - lineFrom.X, dy = to.Y - lineFrom.Y, len = MathF.Sqrt(dx * dx + dy * dy);
            float a = MathF.Round(MathF.Atan2(dy, dx) / (MathF.PI / 4)) * (MathF.PI / 4);
            return new SKPoint(lineFrom.X + MathF.Cos(a) * len, lineFrom.Y + MathF.Sin(a) * len);
        }

        /// <summary>A straight line with the brush, from a to b (pixels)</summary>
        public void DrawLine(SKPoint a, SKPoint b)
        {
            w.PushUndo();
            using var mask = w.Mask();
            TexturePaint.Dab(w.Pic, a.X, a.Y, w.BrushSize, w.Hardness, w.Ink, w.BrushOpacity, null, mask, w.BrushTip);
            TexturePaint.Stroke(w.Pic, a, b, w.BrushSize, w.Hardness, w.Ink, w.BrushOpacity, null, mask, w.BrushTip);
            w.Changed();
            w.Say($"Line, {SKPoint.Distance(a, b):0} pixels");
        }

        public void AddPolygonPoint(SKPoint q) { poly.Add(q); InvalidateVisual(); }

        /// <summary>The shape clicked out, filled with the colour (inside the selected faces when that's on)</summary>
        public void ClosePolygon()
        {
            if (poly.Count < 3) { w.Say("A shape needs 3 corners or more"); return; }
            w.PushUndo();
            using var mask = w.Mask();
            int n = TexturePaint.Polygon(w.Pic, poly, w.Ink, w.BrushOpacity, mask);
            poly.Clear();
            w.Changed();
            w.Say($"Shape filled: {n} pixels");
            InvalidateVisual();
        }

        public void DropPolygon() { poly.Clear(); w.Say("Shape let go"); InvalidateVisual(); }

        SKPath? strokeMask;
        void PaintTo(SKPoint to, bool first)
        {
            if (first) { strokeMask?.Dispose(); strokeMask = w.Mask(); }
            var restore = w.Tool == TexTool.Eraser ? w.original : null;
            var fx = w.EffectOf(w.Tool);
            // The effects work gently, so a stroke builds up as it goes over again
            float op = fx is TexturePaint.Effect.Paint or TexturePaint.Effect.Restore ? w.BrushOpacity : w.BrushOpacity * 0.35f;
            if (first) TexturePaint.Dab(w.Pic, to.X, to.Y, w.BrushSize, w.Hardness, w.Ink, op, restore, strokeMask, w.BrushTip, fx);
            else TexturePaint.Stroke(w.Pic, lastPix, to, w.BrushSize, w.Hardness, w.Ink, op, restore, strokeMask, w.BrushTip, fx);
            lastPix = to;
            w.Changed();
        }

        void PressSelect(Point p, KeyModifiers mods, int clicks)
        {
            var m = w.Model;
            if (m == null) return;
            // Handles of what's selected come first
            if (w.Picked.Count > 0)
            {
                var r = PickedRect();
                if (Dist(p, RotateHandle(r)) < 9) { StartUv(Drag.Rotate, p, "Turn UVs"); return; }
                var hs = Handles(r);
                for (int k = 0; k < 4; k++)
                    if (Dist(p, hs[k]) < 9)
                    {
                        corner = k;
                        var opp = hs[(k + 2) % 4];
                        anchor = ToUv(opp);
                        StartUv(Drag.Scale, p, "Size UVs");
                        return;
                    }
            }
            int f = FaceAt(p);
            if (clicks >= 2 && f >= 0)
            {
                if (!mods.HasFlag(KeyModifiers.Shift)) w.Picked.Clear();
                foreach (var i in UvKit.IslandOf(m, f)) w.Picked.Add(i);
                w.Refresh(); InvalidateVisual();
                return;
            }
            if (f < 0)
            {
                if (!mods.HasFlag(KeyModifiers.Shift) && !mods.HasFlag(KeyModifiers.Control)) w.Picked.Clear();
                drag = Drag.Box;
                w.Refresh(); InvalidateVisual();
                return;
            }
            if (mods.HasFlag(KeyModifiers.Control)) { if (!w.Picked.Remove(f)) w.Picked.Add(f); w.Refresh(); InvalidateVisual(); return; }
            if (mods.HasFlag(KeyModifiers.Shift)) w.Picked.Add(f);
            else if (!w.Picked.Contains(f)) { w.Picked.Clear(); w.Picked.Add(f); }
            w.Refresh();
            StartUv(Drag.Move, p, "Move UVs");
        }

        static double Dist(Point a, Point b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        // The change of the faces starts at the first real move: a click that only selects changes nothing, no undo step
        string? uvLabel;
        Point uvDown;

        void StartUv(Drag kind, Point p, string label)
        {
            if (w.Model == null) { drag = Drag.None; return; }
            drag = kind;
            uvLabel = label;
            uvDown = p;
            uvFrom = ToUv(p);
            uvStart = null;
            InvalidateVisual();
        }

        /// <summary>The pending UV drag has moved far enough: open the change now. False: it may not (asked about a shared file).</summary>
        bool UvGo(Point p)
        {
            if (uvStart != null) return true;
            if (Dist(p, uvDown) < 3) return false;
            if (uvLabel == null || !w.BeginUv(uvLabel)) { drag = Drag.None; uvLabel = null; return false; }
            uvLabel = null;
            uvStart = UvKit.Copy(w.Model!);
            return true;
        }

        protected override void OnPointerMoved(PointerEventArgs e)
        {
            var p = e.GetPosition(this);
            mouse = p;
            var mods = e.KeyModifiers;
            switch (drag)
            {
                case Drag.Pan: Origin += p - last; break;
                case Drag.Paint: PaintTo(ToPix(p), false); break;
                case Drag.Box: break;
                case Drag.Move or Drag.Scale or Drag.Rotate when w.Model is { } m && UvGo(p):
                    {
                        UvKit.Paste(m, uvStart!);
                        var now = ToUv(p);
                        var faces = w.Picked.ToList();
                        if (drag == Drag.Move)
                        {
                            var d = now - uvFrom;
                            // Ctrl: whole pixels
                            if (mods.HasFlag(KeyModifiers.Control)) d = new Vector2(MathF.Round(d.X * w.Pic.Width) / w.Pic.Width, MathF.Round(d.Y * w.Pic.Height) / w.Pic.Height);
                            if (mods.HasFlag(KeyModifiers.Shift)) { if (MathF.Abs(d.X) > MathF.Abs(d.Y)) d.Y = 0; else d.X = 0; }
                            UvKit.Move(m, faces, d);
                        }
                        else if (drag == Drag.Scale)
                        {
                            var a = uvFrom - anchor; var b = now - anchor;
                            var s = new Vector2(MathF.Abs(a.X) > 1e-5f ? b.X / a.X : 1, MathF.Abs(a.Y) > 1e-5f ? b.Y / a.Y : 1);
                            if (mods.HasFlag(KeyModifiers.Shift)) { float u = (s.X + s.Y) / 2; s = new Vector2(u); }
                            UvKit.Scale(m, faces, s, anchor);
                        }
                        else
                        {
                            var c = UvKit.Centre(m, faces);
                            float asp = w.Pic.Width / (float)w.Pic.Height;
                            float a0 = MathF.Atan2(uvFrom.Y - c.Y, (uvFrom.X - c.X) * asp), a1 = MathF.Atan2(now.Y - c.Y, (now.X - c.X) * asp);
                            float deg = (a1 - a0) * 180 / MathF.PI;
                            if (mods.HasFlag(KeyModifiers.Shift)) deg = MathF.Round(deg / 15) * 15;
                            UvKit.Rotate(m, faces, deg, c, asp);
                            w.Say($"Turned {deg:0} degrees");
                        }
                        w.doc.ModelVersion++;
                        w.doc.Touch();
                        break;
                    }
                default:
                    if (w.Tool == TexTool.Select)
                    {
                        int h = FaceAt(p);
                        if (h != hover) hover = h;
                    }
                    break;
            }
            last = p;
            InvalidateVisual();
        }

        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            if (drag == Drag.Box && w.Model is { } m)
            {
                var r = new Rect(dragFrom, last).Normalize();
                if (r.Width > 3 || r.Height > 3)
                    for (int i = 0; i < m.Faces.Length; i++)
                    {
                        var f = m.Faces[i];
                        if (!UvKit.Textured(f)) continue;
                        bool all = true;
                        for (int c = 0; c < f.Corners && all; c++) all = r.Contains(ToScreen(UvKit.Uv(f, c)));
                        if (!all) continue;
                        if (e.KeyModifiers.HasFlag(KeyModifiers.Control)) w.Picked.Remove(i); else w.Picked.Add(i);
                    }
                w.Refresh();
            }
            if (drag is Drag.Move or Drag.Scale or Drag.Rotate && uvStart != null) { w.doc.EndGesture(); w.UvChanged(); }
            if (drag == Drag.Paint) { strokeMask?.Dispose(); strokeMask = null; }
            if (drag == Drag.Line) DrawLine(lineFrom, LineEnd(e.GetPosition(this)));
            drag = Drag.None;
            uvStart = null;
            uvLabel = null;
            e.Pointer.Capture(null);
            InvalidateVisual();
        }

        protected override void OnPointerExited(PointerEventArgs e)
        {
            hover = -1;
            mouse = new Point(-1000, -1000);
            InvalidateVisual();
        }
    }
}
