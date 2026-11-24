using System.Numerics;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

/// <summary>
/// The carbox maker: the car from the angle you like, on a background of your own, with its name as you want it,
/// the carbox as it will be on the selection screen beside, changing as you go. Save writes carbox.bmp. The choices
/// are remembered for the car, so the next carbox starts from them.
/// </summary>
public sealed class CarboxWindow : Window
{
    readonly MainWindow main;
    readonly Doc doc;
    readonly Image preview = new() { Width = 384, Height = 384, Stretch = Stretch.Uniform };
    readonly TextBlock note = new() { FontSize = 12, Foreground = Look.Faint, TextWrapping = TextWrapping.Wrap };
    SKBitmap? cut;
    public SKBitmap? Result { get; private set; }
    public string? SavedPath { get; private set; }

    // The choices
    public float CarTurn = Carbox.StockTurn, CarHeight = Carbox.StockHeight;
    public Carbox.Style Style;
    readonly DispatcherTimer shoot;

    public static readonly (string Name, float Turn, float Height)[] Angles =
    {
        ("Front, from above (stock)", Carbox.StockTurn, Carbox.StockHeight), ("Front left", 45, 18), ("Front right", -45, 18), ("Side", 90, 4), ("Front", 0, 6),
        ("Back left", 135, 18), ("From above", 30, 55),
    };

    /// <summary>Ready-made looks: one click for a good carbox, every option still there to change after</summary>
    public static readonly (string Name, Func<string, Carbox.Style> Make)[] Looks =
    {
        ("Stock: as the game's own, the name in the car's colour", n => Carbox.Stock(n, StockColour)),
        ("Night: dark blue, gold name", n => new Carbox.Style(Carbox.Back.Gradient, new SKColor(12, 18, 40), new SKColor(50, 80, 150), null, n, Carbox.NameStyle.Outline, Carbox.NamePlace.Bottom, SKColors.Gold, 1, 1, 0, true, false)),
        ("Sky: blue to white", n => new Carbox.Style(Carbox.Back.Gradient, new SKColor(90, 160, 235), SKColors.White, null, n, Carbox.NameStyle.Outline, Carbox.NamePlace.Top, SKColors.White, 1.2f, 1.1f, 0, false, true)),
        ("Racing: red, white name", n => new Carbox.Style(Carbox.Back.Gradient, new SKColor(205, 25, 35), new SKColor(90, 0, 12), null, n, Carbox.NameStyle.Plain, Carbox.NamePlace.Bottom, SKColors.White, 1.1f, 1, 0, true, false)),
        ("Side: the name down the left side, its echoes behind the car", n => Carbox.Stock(n, StockColour) with { Way = Carbox.NameWay.Down, Place = Carbox.NamePlace.Top }),
        ("Clean: white, no name", n => new Carbox.Style(Name: n, Text: Carbox.NameStyle.None, CarSize: 1.25f)),
    };

    /// <summary>The name's colour in the stock look: from the car's own texture</summary>
    static SKColor StockColour = new(0xD0, 0x30, 0x30);

    Action sync = () => { };
    Slider turn = null!, height = null!, tiltSlider = null!;

    // ---------------------------- variations: the same carbox from other angles, side by side ----------------------------

    /// <summary>Angles to try, each a click away (turn, height, tilt)</summary>
    public static readonly (string Name, float Turn, float Height, float Tilt)[] Variations =
    {
        ("Stock", Carbox.StockTurn, Carbox.StockHeight, 15), ("Mirrored", -Carbox.StockTurn, Carbox.StockHeight, -15),
        ("Steeper", -30, 52, 22), ("Flatter", -20, 18, 8), ("From the side", -80, 14, 4), ("From behind", 155, 35, -15),
    };

    readonly Image[] variationImages = new Image[Variations.Length];
    readonly SKBitmap?[] variationCuts = new SKBitmap?[Variations.Length];
    public bool VariationsReady { get; private set; }

    /// <summary>One click: the maker takes that angle and tilt</summary>
    public void UseVariation(int i)
    {
        var v = Variations[i];
        Style = Style with { Tilt = v.Tilt };
        sync();
        CarTurn = v.Turn; CarHeight = v.Height;
        if (variationCuts[i] is { } c) { cut?.Dispose(); cut = c.Copy(); }
        syncing = true;
        turn.Value = v.Turn; height.Value = v.Height; tiltSlider.Value = v.Tilt;
        syncing = false;
        shoot.Stop();
        Compose();
        note.Text = v.Name + ": turn " + v.Turn + ", height " + v.Height + ", tilt " + v.Tilt + ". Fine-tune with the sliders";
    }

    /// <summary>The car cut out from each variation's angle, once; the previews follow every other choice</summary>
    async Task ShootVariations()
    {
        for (int i = 0; i < Variations.Length; i++)
        {
            variationCuts[i]?.Dispose();
            variationCuts[i] = await CutAt(Variations[i].Turn, Variations[i].Height);
        }
        VariationsReady = true;
        ComposeVariations();
    }

    void ComposeVariations()
    {
        for (int i = 0; i < Variations.Length; i++)
        {
            if (variationCuts[i] is not { } c || variationImages[i] == null) continue;
            using var box = Carbox.Compose(c, Style with { Tilt = Variations[i].Tilt }, 128);
            using var data = box.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream(data.ToArray());
            variationImages[i].Source = new Bitmap(ms);
        }
    }
    bool syncing;

    /// <summary>A ready-made look, the name kept</summary>
    public void UseLook(int i)
    {
        Style = Looks[i].Make(Style.Name);
        sync();
        Compose();
    }

    public CarboxWindow(MainWindow main, Doc doc)
    {
        this.main = main;
        this.doc = doc;
        Title = "Carbox maker - " + doc.D!.Name;
        Width = 1060; Height = 820; MinWidth = 880; MinHeight = 600;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = Look.Panel;
        Foreground = Look.Text;
        using (var tex = doc.Car!.Game.Find(doc.D.TPage) is { } tf ? Images.Load(tf) : null) StockColour = Carbox.NameColourFrom(tex);
        Style = Load() ?? Carbox.Stock(doc.D.Name, StockColour);
        if (Style.Name == "") Style = Style with { Name = doc.D.Name };

        var form = new StackPanel { Spacing = 5, Margin = new Thickness(14, 12, 12, 12) };
        void Head(string t) => form.Children.Add(new TextBlock { Text = t, FontSize = 13, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 8, 0, 0) });
        Control Row(string label, Control c)
        {
            var g = new Grid { ColumnDefinitions = new ColumnDefinitions("104,*") };
            g.Children.Add(new TextBlock { Text = label, FontSize = 12, VerticalAlignment = VerticalAlignment.Center });
            Grid.SetColumn(c, 1);
            g.Children.Add(c);
            form.Children.Add(g);
            return c;
        }
        Slider Slide(string label, double min, double max, double value, Action<double> set)
        {
            var s = new Slider { Minimum = min, Maximum = max, Value = value, Margin = new Thickness(0, -6, 0, -6) };
            s.PropertyChanged += (_, e) => { if (e.Property == Avalonia.Controls.Primitives.RangeBase.ValueProperty) set(s.Value); };
            Row(label, s);
            return s;
        }
        ComboBox Pick(string label, string[] items, int index, Action<int> set)
        {
            var b = new ComboBox { ItemsSource = items, SelectedIndex = index, FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
            b.SelectionChanged += (_, _) => { if (b.SelectedIndex >= 0) set(b.SelectedIndex); };
            Row(label, b);
            return b;
        }
        ColourButton Colour(string label, SKColor c, Action<SKColor> set)
        {
            var b = new ColourButton(Color.FromRgb(c.Red, c.Green, c.Blue)) { HorizontalAlignment = HorizontalAlignment.Stretch };
            b.Changed += x => set(new SKColor(x.R, x.G, x.B));
            Row(label, b);
            return b;
        }

        Head("Ready-made looks");
        var looks = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < Looks.Length; i++)
        {
            int k = i;
            var b = new Button { Content = Looks[i].Name.Split(':')[0], FontSize = 12, Padding = new Thickness(10, 3), Margin = new Thickness(0, 2, 6, 2) };
            ToolTip.SetTip(b, Looks[i].Name);
            b.Click += (_, _) => UseLook(k);
            looks.Children.Add(b);
        }
        form.Children.Add(looks);
        form.Children.Add(new TextBlock { Text = "One click for a look, then change anything below.", FontSize = 11, Foreground = Look.Faint });

        Head("The car");
        Pick("Angle", Angles.Select(a => a.Name).ToArray(), 0, i => { turn.Value = Angles[i].Turn; height.Value = Angles[i].Height; });
        turn = Slide("Turn", -180, 180, CarTurn, v => { CarTurn = (float)v; Reshoot(); });
        height = Slide("Height", 0, 70, CarHeight, v => { CarHeight = (float)v; Reshoot(); });
        tiltSlider = null!;
        var tilt = Slide("Tilt", -45, 45, Style.Tilt, v => Set(Style with { Tilt = (float)v }));
        tiltSlider = tilt;
        var carSize = Slide("Size", 0.6, 1.3, Style.CarSize, v => Set(Style with { CarSize = (float)v }));
        var carLift = Slide("Up, down", -0.5, 0.5, Style.CarLift, v => Set(Style with { CarLift = (float)v }));

        Head("Behind it");
        var back = Pick("Background", Carbox.BackNames, (int)Style.Background, i => Set(Style with { Background = (Carbox.Back)i }));
        var col1 = Colour("Colour", Style.Colour1 ?? SKColors.White, c => Set(Style with { Colour1 = c }));
        var col2 = Colour("Second colour", Style.Colour2 ?? new SKColor(0xB8, 0xC8, 0xE0), c => Set(Style with { Colour2 = c }));
        var picBtn = new Button { Content = Style.Picture != null ? Names.File(Style.Picture) : "Choose a picture...", FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
        picBtn.Click += async (_, _) =>
        {
            var f = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "A picture behind the car",
                FileTypeFilter = new[] { new FilePickerFileType("Pictures") { Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" } } }
            });
            if (f.FirstOrDefault()?.TryGetLocalPath() is not { } path) return;
            picBtn.Content = Names.File(path);
            Set(Style with { Picture = path, Background = Carbox.Back.Picture });
        };
        Row("Picture", picBtn);

        Head("The name");
        var nameBox = new TextBox { Text = Style.Name, FontSize = 12 };
        nameBox.TextChanged += (_, _) => Set(Style with { Name = nameBox.Text ?? "" });
        Row("Text", nameBox);
        var nameStyle = Pick("Style", Carbox.NameStyleNames, (int)Style.Text, i => Set(Style with { Text = (Carbox.NameStyle)i }));
        // Turned, it goes to the side it belongs at; "Where" changes it after
        var nameWay = Pick("Direction", Carbox.NameWayNames, (int)Style.Way, i =>
        {
            if (syncing) return;
            Style = Style with { Way = (Carbox.NameWay)i, Place = Carbox.PlaceFor((Carbox.NameWay)i) };
            sync();
            Compose();
        });
        ToolTip.SetTip(nameWay, "Across, down or up the side, upside down, or its letters one under the other");
        var namePlace = Pick("Where", Carbox.NamePlaceNames, (int)Style.Place, i => Set(Style with { Place = (Carbox.NamePlace)i }));
        var nameCol = Colour("Colour", Style.NameColour ?? new SKColor(0xD0, 0x30, 0x30), c => Set(Style with { NameColour = c }));
        var nameFit = Pick("Length", Carbox.NameFitNames, (int)Style.Fit, i => Set(Style with { Fit = (Carbox.NameFit)i }));
        ToolTip.SetTip(nameFit, "As wide as the box: any name fills it from side to side (or top to bottom), as the stock cars' do");
        var nameSize = Slide("Height", 0.4, 2.4, Style.NameSize, v => Set(Style with { NameSize = (float)v }));
        ToolTip.SetTip(nameSize, "How tall the letters are");
        var nameWidth = Slide("Width", 0.3, 2, Style.NameWidth, v => Set(Style with { NameWidth = (float)v }));
        ToolTip.SetTip(nameWidth, "Stretched or squeezed along the way it runs");
        var spacing = Slide("Kerning", -0.3, 1, Style.Spacing, v => Set(Style with { Spacing = (float)v }));
        ToolTip.SetTip(spacing, "The room between the letters: left closes them up, right spreads them");
        var nameTilt = Slide("Slant", -45, 45, Style.NameTilt, v => Set(Style with { NameTilt = (float)v }));
        ToolTip.SetTip(nameTilt, "The name turned a little, on top of its direction");
        var nameShift = Slide("Move", -0.3, 0.6, Style.NameShift, v => Set(Style with { NameShift = (float)v }));
        ToolTip.SetTip(nameShift, "Away from its edge, into the box");
        var resetName = new Button { Content = "Letters as in the stock look", FontSize = 12, Padding = new Thickness(10, 3), HorizontalAlignment = HorizontalAlignment.Right };
        ToolTip.SetTip(resetName, "Direction, place, length, height, width, kerning, slant and move back; the text, style and colour stay");
        resetName.Click += (_, _) =>
        {
            var d0 = new Carbox.Style();
            Style = Style with { Way = d0.Way, Place = d0.Place, Fit = d0.Fit, NameSize = d0.NameSize, NameWidth = d0.NameWidth, Spacing = d0.Spacing, NameTilt = d0.NameTilt, NameShift = d0.NameShift };
            sync();
            Compose();
        };
        form.Children.Add(resetName);
        var behind = new CheckBox { Content = "The name behind the car (the stock way)", IsChecked = Style.NameBehind, FontSize = 12 };
        behind.IsCheckedChanged += (_, _) => Set(Style with { NameBehind = behind.IsChecked == true });
        form.Children.Add(behind);
        var shadow = new CheckBox { Content = "A soft shadow under the car", IsChecked = Style.Shadow, FontSize = 12 };
        shadow.IsCheckedChanged += (_, _) => Set(Style with { Shadow = shadow.IsChecked == true });
        form.Children.Add(shadow);
        var frame = new CheckBox { Content = "A frame round it, in the name's colour", IsChecked = Style.Frame, FontSize = 12 };
        frame.IsCheckedChanged += (_, _) => Set(Style with { Frame = frame.IsChecked == true });
        form.Children.Add(frame);

        // A look (or a test) changed the choices: the controls show them, without each one composing again
        static Color A(SKColor c) => Color.FromRgb(c.Red, c.Green, c.Blue);
        sync = () =>
        {
            syncing = true;
            var s = Style;
            carSize.Value = s.CarSize; carLift.Value = s.CarLift; tilt.Value = s.Tilt;
            back.SelectedIndex = (int)s.Background;
            col1.SetValue(A(s.Colour1 ?? SKColors.White)); col2.SetValue(A(s.Colour2 ?? new SKColor(0xB8, 0xC8, 0xE0)));
            nameBox.Text = s.Name; nameStyle.SelectedIndex = (int)s.Text; namePlace.SelectedIndex = (int)s.Place;
            nameCol.SetValue(A(s.NameColour ?? new SKColor(0xD0, 0x30, 0x30))); nameSize.Value = s.NameSize;
            nameWay.SelectedIndex = (int)s.Way; nameFit.SelectedIndex = (int)s.Fit; nameWidth.Value = s.NameWidth; spacing.Value = s.Spacing;
            nameTilt.Value = s.NameTilt; nameShift.Value = s.NameShift;
            frame.IsChecked = s.Frame;
            behind.IsChecked = s.NameBehind;
            shadow.IsChecked = s.Shadow;
            syncing = false;
        };

        var save = Look.Primary(new Button { Content = "Save as the carbox", FontSize = 12, MinHeight = 30, Padding = new Thickness(14, 4) });
        save.Click += (_, _) => { if (SaveBox()) Close(); };
        var cancel = new Button { Content = "Cancel", FontSize = 12, MinHeight = 30, Padding = new Thickness(14, 4) };
        cancel.Click += (_, _) => Close();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right, Children = { save, cancel } };

        var left = new StackPanel { Margin = new Thickness(16), Spacing = 8, Children =
        {
            new TextBlock { Text = "As on the car selection screen (256 x 256)", FontSize = 12, FontWeight = FontWeight.SemiBold },
            new Border { Child = preview, BorderBrush = Look.Line, BorderThickness = new Thickness(1), HorizontalAlignment = HorizontalAlignment.Left },
            note,
            new TextBlock { Text = "Other angles and tilts (click one)", FontSize = 12, FontWeight = FontWeight.SemiBold, Margin = new Thickness(0, 6, 0, 0) },
            VariationRow()
        } };
        var bottom = new DockPanel { Margin = new Thickness(12, 8) };
        DockPanel.SetDock(buttons, Dock.Right);
        bottom.Children.Add(buttons);
        var root = new DockPanel();
        DockPanel.SetDock(bottom, Dock.Bottom);
        root.Children.Add(bottom);
        var leftScroll = new ScrollViewer { Content = left, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        DockPanel.SetDock(leftScroll, Dock.Left);
        root.Children.Add(leftScroll);
        root.Children.Add(new ScrollViewer { Content = form });
        Content = root;

        shoot = new DispatcherTimer(TimeSpan.FromMilliseconds(220), DispatcherPriority.Background, async (_, _) => { shoot!.Stop(); await Shoot(); });
        Opened += async (_, _) => await Shoot();
        note.Text = "Turn the car, choose what's behind it and how its name looks. The view in the main window stays as it was.";
    }

    void Set(Carbox.Style s) { if (syncing) return; Style = s; Compose(); }

    Control VariationRow()
    {
        var row = new WrapPanel { Orientation = Orientation.Horizontal, MaxWidth = 400, HorizontalAlignment = HorizontalAlignment.Left };
        for (int i = 0; i < Variations.Length; i++)
        {
            int k = i;
            variationImages[i] = new Image { Width = 112, Height = 112, Stretch = Stretch.Uniform };
            var b = new Button
            {
                Padding = new Thickness(3), Margin = new Thickness(0, 0, 6, 6), Background = Look.Card,
                Content = new StackPanel { Spacing = 2, Children = { variationImages[i], new TextBlock { Text = Variations[i].Name, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center } } }
            };
            ToolTip.SetTip(b, $"{Variations[i].Name}: turn {Variations[i].Turn}, height {Variations[i].Height}, tilt {Variations[i].Tilt}");
            b.Click += (_, _) => UseVariation(k);
            row.Children.Add(b);
        }
        return row;
    }

    void Reshoot() { shoot.Stop(); shoot.Start(); }

    /// <summary>The car from the angle asked, cut out of the view (drawn on white, then on black)</summary>
    public async Task Shoot()
    {
        var c = await CutAt(CarTurn, CarHeight);
        if (c != null) { cut?.Dispose(); cut = c; }
        Compose();
        if (!VariationsReady && !shootingVariations) { shootingVariations = true; await ShootVariations(); shootingVariations = false; }
    }

    bool shootingVariations;

    /// <summary>The car cut out of the view from this angle (drawn on white, then on black); the main view stays as it was</summary>
    async Task<SKBitmap?> CutAt(float turnDeg, float heightDeg)
    {
        var v = main.View;
        var (yaw, pitch, dist, tgt, mode) = (v.Yaw, v.Pitch, v.Distance, v.Target, v.View);
        try
        {
            v.SetView(ViewMode.Perspective);
            v.Yaw = turnDeg * MathF.PI / 180; v.Pitch = heightDeg * MathF.PI / 180;
            v.FrameCar();
            using var white = await v.Capture(512, true, 0, new Vector3(1, 1, 1));
            using var black = await v.Capture(512, true, 0, new Vector3(0, 0, 0));
            return Carbox.Cutout(white, black);
        }
        catch (Exception ex) { note.Text = "Couldn't take the car's picture: " + ex.Message; return null; }
        finally
        {
            v.SetView(mode);
            (v.Yaw, v.Pitch, v.Distance, v.Target) = (yaw, pitch, dist, tgt);
            v.Redraw();
        }
    }

    void Compose()
    {
        if (cut == null) return;
        Result?.Dispose();
        Result = Carbox.Compose(cut, Style);
        ComposeVariations();
        using var data = Result.Encode(SKEncodedImageFormat.Png, 100);
        using var ms = new MemoryStream(data.ToArray());
        preview.Source = new Bitmap(ms);
    }

    /// <summary>carbox.bmp written into the car's folder (the first one stays as .bak), the choices kept for next time</summary>
    public bool SaveBox()
    {
        if (Result == null || doc.Car == null) return false;
        SavedPath = main.SaveCarboxPicture(Result);
        if (SavedPath == null) return false;
        Keep();
        return true;
    }

    // ---------------------------- the choices, per car ----------------------------

    sealed record Kept(int Back, string? C1, string? C2, string? Picture, string Name, int Text, int Place, string? NameColour, float NameSize, float CarSize, float CarLift, bool Frame, float Turn, float Height, bool NameBehind = true, bool Shadow = true, float Tilt = 15,
        int Way = 0, int Fit = 0, float Spacing = 0, float NameWidth = 1, float NameTilt = 0, float NameShift = 0);

    string Key => "carbox:" + doc.Car!.Folder.ToLowerInvariant();

    void Keep()
    {
        static string? Hex(SKColor? c) => c?.ToString();
        var s = Style;
        Settings.Put(Key, JsonSerializer.Serialize(new Kept((int)s.Background, Hex(s.Colour1), Hex(s.Colour2), s.Picture, s.Name, (int)s.Text, (int)s.Place, Hex(s.NameColour), s.NameSize, s.CarSize, s.CarLift, s.Frame, CarTurn, CarHeight, s.NameBehind, s.Shadow, s.Tilt,
            (int)s.Way, (int)s.Fit, s.Spacing, s.NameWidth, s.NameTilt, s.NameShift)));
    }

    Carbox.Style? Load()
    {
        try
        {
            if (Settings.Get(Key) is not { } j || JsonSerializer.Deserialize<Kept>(j) is not { } k) return null;
            static SKColor? C(string? h) => h != null && SKColor.TryParse(h, out var c) ? c : null;
            CarTurn = k.Turn; CarHeight = k.Height;
            return new Carbox.Style((Carbox.Back)k.Back, C(k.C1), C(k.C2), k.Picture, k.Name, (Carbox.NameStyle)k.Text, (Carbox.NamePlace)k.Place, C(k.NameColour), k.NameSize, k.CarSize, k.CarLift, k.Frame, k.NameBehind, k.Shadow, k.Tilt,
                (Carbox.NameWay)k.Way, (Carbox.NameFit)k.Fit, k.Spacing, k.NameWidth, k.NameTilt, k.NameShift);
        }
        catch (Exception) { return null; }
    }
}
