using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;
using RvStudio.Core;

namespace RvStudio.App;

/// <summary>A step of a tour: what to do and why, where it is, how to tell it's done, and the app doing it for you</summary>
public sealed record TourStep(string Title, string Text, string? Where = null, Func<bool>? IsDone = null, Action? DoIt = null, string DoItLabel = "Do it for me");

public sealed record Tour(string Name, string Intro, TourStep[] Steps);

public sealed partial class MainWindow
{
    // ---------------------------- Tours: a card over the 3D view that walks you through making a car ----------------------------
    //
    // Each step says what to do and points at it (Show me lights the button, the tab or the menu up). It ticks itself
    // off when you've done it, from what the app saw you do, and goes on. Do it for me does the step with the usual
    // settings. Next and Back move freely: nothing is forced.

    readonly Border tourBox = new() { IsVisible = false, Width = 400, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(12), CornerRadius = new CornerRadius(10), Padding = new Thickness(14, 12), BorderThickness = new Thickness(1), ZIndex = 50 };
    public Tour? CurrentTour { get; private set; }
    public int TourStepIndex { get; private set; }
    readonly HashSet<string> tourSaw = new();
    DispatcherTimer? tourTimer;
    bool tourStepDone;
    DateTime tourDoneAt;

    bool Saw(string what) => tourSaw.Contains(what);
    bool SawAny(params string[] what) => what.Any(tourSaw.Contains);

    public static readonly string[] TourNames = { "Make a new car", "Remodel a car", "Repaint a car" };

    public Tour MakeTour(int which) => which switch
    {
        2 => new Tour("Repaint a car", "A new paint job on a car's shape: your own copy, with its own colours, carbox and readme, ready to share.", new[]
        {
            new TourStep("Make your copy",
                "File > New car..., choose Repaint a car. Pick the car whose shape you like, give yours a folder and a name. You work on your own copy: the original car stays as it is.",
                "menu:File>New car...", () => Saw("NewCar:Repaint"), () => _ = NewCar(), "Open New car"),
            new TourStep("Change the colours",
                "The Paint tab changes the whole texture as you watch: hue, colour, brightness, contrast and warmth. Click a swatch to change only that colour, or turn on Swap to replace one colour with another.",
                "tab:Paint"),
            new TourStep("Paint by hand",
                "For stripes, numbers, a livery or your own drawing: Open the UV and texture editor (U). Brush, fill and the generators paint on the texture and the car in 3D shows it live. You can also paint in your own image editor: the car follows the file when you save it there.",
                "button:Open the UV and texture editor", () => Saw("TextureEditor"), () => OpenUvEditor(), "Open the texture"),
            new TourStep("Keep the paint",
                "Change this car's texture writes it into car.bmp (the first version stays as .bak). Save as a new skin keeps both: RVGL lists the new one as a skin of the same car.",
                "button:Change this car's texture", () => SawAny("Texture", "Skin")),
            new TourStep("A carbox with the new colours",
                "Tools > Make carbox: the picture players see on the car selection screen, with your car's name on it.",
                "menu:Tools>Make carbox", () => Saw("Carbox"), () => _ = MakeCarbox(), "Make the carbox"),
            new TourStep("See it drive",
                "Run > Drive here (F6) drives it right here. Run > Test run in RVGL (F5) starts the game with your car picked.",
                "menu:Run>Drive here", () => SawAny("Drive", "Race", "RVGL"), () => StartDrive(SimGround.Flat), "Drive it here"),
            new TourStep("Readme and zip",
                "The Package tab says if the car is ready, then Pack as zip saves the car, asks for the readme (who made it, what it's based on, what others may do with it) and writes a zip players unzip on their game folder.",
                "tab:Package", () => Saw("Pack"), () => _ = PackZip(), "Pack it"),
        }),
        1 => new Tour("Remodel a car", "Your own body model on a car that already drives well: the base car's engine, grip and suspension, your shape.", new[]
        {
            new TourStep("Start the remodel",
                "File > New car..., choose Remodel a car. Pick the car whose driving you want to keep, then your copy's folder and name.",
                "menu:File>New car...", () => Saw("NewCar:Remodel"), () => _ = NewCar(), "Open New car"),
            new TourStep("Bring in your model",
                "Click the body, then File > Import a model from .obj for the selected part... (or Replace on its page for a .prm). Made in Blender: metres, Y up, front towards -Z. Saving the .obj again brings it in again by itself.",
                "menu:File>Import a model from .obj for the selected part...", () => SawAny("Import")),
            new TourStep("Wheels where your model has them",
                "Tools > Place wheels on a new body puts the wheels in the model's wheel arches; Fit radius, skid and lengths sizes them. Then look: drag a wheel (W) if one is off.",
                "menu:Tools>Place wheels on a new body", () => Saw("Place wheels") && Saw("Fit"),
                () => { Tool("Place wheels", () => CarTools.PlaceWheels(Doc.Car!)); Tool("Fit", () => CarTools.FitToModels(Doc.Car!)); }, "Place and fit them"),
            new TourStep("A hull for the new shape",
                "Tools > Make collision hull: what the car bumps the world with. It's kept clear of the road, as on the stock cars, so it doesn't catch on kerbs.",
                "menu:Tools>Make collision hull", () => Saw("Hull"), () => MakeHull(), "Make the hull"),
            new TourStep("Light and shade",
                "Tools > Auto shade the car bakes soft light into the model, as the game's own cars have. The Shade tab has the sliders to change it.",
                "menu:Tools>Auto shade the car", () => Saw("Shade"), () => AutoShade(), "Shade it"),
            new TourStep("Shadow and carbox",
                "Tools > Make shadow (the dark patch under the car in the game), then Tools > Make carbox (its picture on the car selection screen).",
                "menu:Tools>Make shadow", () => Saw("Shadow") && Saw("Carbox"),
                async () => { Tool("Shadow", () => Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!))); await MakeCarbox(); }, "Make both"),
            new TourStep("Check and drive",
                "The panel under the 3D view lists what's wrong, with a fix for most. Then Run > Drive here (F6), Race here (F7), or Test run in RVGL (F5).",
                "menu:Run>Drive here", () => SawAny("Drive", "Race", "RVGL"), () => StartDrive(SimGround.Flat), "Drive it here"),
            new TourStep("Readme and zip",
                "The Package tab says if the car is ready; Pack as zip saves the car, writes its readme (with the base car's credit) and the zip to share.",
                "tab:Package", () => Saw("Pack"), () => _ = PackZip(), "Pack it"),
        }),
        _ => new Tour("Make a new car", "A car of your own from scratch: your body, wheel and texture, made into a car that drives, ready to share.", new[]
        {
            new TourStep("Bring in your models",
                "File > New car..., choose Original car. Pick your body (.obj from Blender or .prm), your wheel and your texture (.png, .jpg or .bmp). The base car only gives the driving to start from.",
                "menu:File>New car...", () => Saw("NewCar:Original"), () => _ = NewCar(), "Open New car"),
            new TourStep("Look it over",
                "Your model is in with the wheels placed, sized and a first hull. Turn around it (right drag) and check the wheels sit in their arches: click one and drag (W) if not.",
                "tab:Selected"),
            new TourStep("Weight and balance",
                "Tools > Compute inertia from the body: how the car turns and rolls, from its shape. How it drives, at the top of Properties (F1), shows its balance, top speed and grip as you change things.",
                "menu:Tools>Compute inertia from the body", () => Saw("Inertia"), () => Tool("Inertia", () => CarTools.ComputeInertia(Doc.Car!)), "Compute it"),
            new TourStep("Collision hull",
                "Tools > Make collision hull: what the car bumps the world with, kept clear of the road so it doesn't catch on kerbs.",
                "menu:Tools>Make collision hull", () => Saw("Hull"), () => MakeHull(), "Make the hull"),
            new TourStep("Light and shade",
                "Tools > Auto shade the car bakes soft light into your model, like the game's own cars.",
                "menu:Tools>Auto shade the car", () => Saw("Shade"), () => AutoShade(), "Shade it"),
            new TourStep("Shadow",
                "Tools > Make shadow, then Fit shadow to the car: the dark patch under it in the game.",
                "menu:Tools>Make shadow", () => Saw("Shadow"), () => { Tool("Shadow", () => Say("Shadow saved: " + CarTools.MakeShadow(Doc.Car!))); Tool("Shadow table", () => CarTools.FitShadowTable(Doc.Car!)); }, "Make it"),
            new TourStep("Carbox",
                "Tools > Make carbox: your car's picture with its name, on the car selection screen.",
                "menu:Tools>Make carbox", () => Saw("Carbox"), () => _ = MakeCarbox(), "Make the carbox"),
            new TourStep("Its numbers on the menu",
                "Tools > Fill frontend stats from a simulated run: the speed and acceleration bars players see, measured, not guessed.",
                "menu:Tools>Fill frontend stats from a simulated run", () => Saw("Stats"), () => Tool("Stats", () => CarSim.FillStats(Doc.D!, Doc.Hull)), "Fill them"),
            new TourStep("Tune how it drives",
                "Properties has the engine, grip and springs, with How it drives at the top saying what each change does. Drive here (F6) or Race here (F7) on the top bar to feel it.",
                "tab:Properties", () => SawAny("Drive", "Race"), () => StartDrive(SimGround.Flat), "Drive it here"),
            new TourStep("Try it in the game",
                "Run > Test run in RVGL (F5) starts RVGL with your car picked. The dev mode one (Shift+F5) reloads the car with Ctrl+R while you change it here.",
                "menu:Run>Test run in RVGL", () => Saw("RVGL")),
            new TourStep("Readme and zip",
                "The Package tab says if the car is ready; Pack as zip saves the car, writes its readme and the zip players unzip on their game folder.",
                "tab:Package", () => Saw("Pack"), () => _ = PackZip(), "Pack it"),
        }),
    };

    Control TourBox()
    {
        tourBox.Background = Look.Card;
        tourBox.BorderBrush = Look.Accent;
        tourBox.BoxShadow = BoxShadows.Parse("0 4 18 0 #60000000");
        DidSomething += what => { if (CurrentTour != null) tourSaw.Add(what); };
        return tourBox;
    }

    /// <summary>Starts a tour (0 new car, 1 remodel, 2 repaint) at its first step</summary>
    public void StartTour(int which)
    {
        // The tours point at Pro's tabs and buttons
        if (!ProMode) SetMode(true);
        CurrentTour = MakeTour(which);
        tourSaw.Clear();
        TourStepIndex = -1; // the intro card first
        tourTimer ??= new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Background, (_, _) => TourTick());
        tourTimer.Start();
        Settings.Put("tours", "seen");
        ShowTourCard();
    }

    public void EndTour()
    {
        CurrentTour = null;
        tourTimer?.Stop();
        tourBox.IsVisible = false;
        ClearHighlight();
    }

    public void TourGo(int index)
    {
        if (CurrentTour == null) return;
        if (index >= CurrentTour.Steps.Length) { EndTour(); Say("Tour finished: " + CurrentTour?.Name + ". Help > Tours has the others"); return; }
        TourStepIndex = Math.Max(-1, index);
        tourStepDone = false;
        ShowTourCard();
        if (TourStepIndex >= 0) _ = Highlight(CurrentTour.Steps[TourStepIndex].Where, false);
    }

    void TourTick()
    {
        if (CurrentTour == null || TourStepIndex < 0 || TourStepIndex >= CurrentTour.Steps.Length) return;
        var s = CurrentTour.Steps[TourStepIndex];
        if (!tourStepDone && s.IsDone != null && SafeDone(s))
        {
            tourStepDone = true;
            tourDoneAt = DateTime.UtcNow;
            ShowTourCard();
        }
        // Done: a moment to see the tick, then on to the next step
        else if (tourStepDone && (DateTime.UtcNow - tourDoneAt).TotalSeconds > 1.6) TourGo(TourStepIndex + 1);
    }

    static bool SafeDone(TourStep s) { try { return s.IsDone!(); } catch (Exception) { return false; } }

    void ShowTourCard()
    {
        if (CurrentTour is not { } t) return;
        var stack = new StackPanel { Spacing = 6 };
        var head = new DockPanel();
        var close = new Button { Content = "✕", FontSize = 11, Padding = new Thickness(7, 2), Background = Brushes.Transparent };
        ToolTip.SetTip(close, "End the tour (Help > Tours starts it again)");
        close.Click += (_, _) => EndTour();
        DockPanel.SetDock(close, Dock.Right);
        head.Children.Add(close);
        head.Children.Add(new TextBlock
        {
            Text = "Tour: " + t.Name + (TourStepIndex >= 0 ? $"  ·  step {TourStepIndex + 1} of {t.Steps.Length}" : ""),
            FontSize = 11, Foreground = Look.Faint, VerticalAlignment = VerticalAlignment.Center
        });
        stack.Children.Add(head);

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Margin = new Thickness(0, 4, 0, 0) };
        Button B(string text, Action a, bool primary = false)
        {
            var b = new Button { Content = text, FontSize = 12, Padding = new Thickness(10, 3), MinHeight = 28 };
            if (primary) Look.Primary(b);
            b.Click += (_, _) => { a(); Focus(); };
            row.Children.Add(b);
            return b;
        }

        if (TourStepIndex < 0)
        {
            stack.Children.Add(new TextBlock { Text = t.Name, FontSize = 16, FontWeight = FontWeight.SemiBold });
            stack.Children.Add(new TextBlock { Text = t.Intro, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            stack.Children.Add(new TextBlock { Text = $"{t.Steps.Length} steps. Each one points at what to click and ticks itself off when you've done it. Do it for me does a step for you.", TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = Look.Faint });
            B("Start", () => TourGo(0), true);
            B("Not now", EndTour);
        }
        else
        {
            var s = t.Steps[TourStepIndex];
            var title = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
            if (tourStepDone) title.Children.Add(new TextBlock { Text = "✓", FontSize = 16, FontWeight = FontWeight.Bold, Foreground = Look.Good });
            title.Children.Add(new TextBlock { Text = s.Title, FontSize = 15, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            stack.Children.Add(title);
            stack.Children.Add(new TextBlock { Text = s.Text, TextWrapping = TextWrapping.Wrap, FontSize = 12 });
            stack.Children.Add(new TextBlock
            {
                Text = tourStepDone ? "Done. On to the next step..." : s.IsDone != null ? "It ticks itself off when you've done it." : "Look around, then Next when you're ready.",
                FontSize = 11, Foreground = tourStepDone ? Look.Good : Look.Faint
            });
            if (TourStepIndex > 0) B("Back", () => TourGo(TourStepIndex - 1));
            if (s.Where != null) B("Show me", () => _ = Highlight(s.Where, true));
            if (s.DoIt != null && !tourStepDone && Doc.Car != null || s.DoIt != null && TourStepIndex == 0) B(s.DoItLabel, () => { try { s.DoIt!(); } catch (Exception ex) { Say(ex.Message); } });
            B(TourStepIndex == t.Steps.Length - 1 ? "Finish" : "Next", () => TourGo(TourStepIndex + 1), s.IsDone == null || tourStepDone);
        }
        stack.Children.Add(row);
        tourBox.Child = stack;
        tourBox.IsVisible = true;
    }

    // ---------------------------- pointing at things ----------------------------

    Control? lit;
    Border? litMark;

    void ClearHighlight()
    {
        if (litMark != null && AdornerLayer.GetAdornerLayer(lit!) is { } layer) layer.Children.Remove(litMark);
        litMark = null; lit = null;
    }

    /// <summary>
    /// Lights up where a step happens: "tab:Paint", "button:Text" (its tab is opened first), "menu:Tools>Make carbox"
    /// (the menu is opened when you asked: open is true; otherwise only its name in the menu bar lights up)
    /// </summary>
    public async Task<Control?> Highlight(string? where, bool open)
    {
        ClearHighlight();
        if (where == null) return null;
        var (kind, what) = (where[..where.IndexOf(':')], where[(where.IndexOf(':') + 1)..]);
        Control? c = null;
        switch (kind)
        {
            case "tab":
                if (Tabs.Items.OfType<TabItem>().FirstOrDefault(x => (x.Header as string) == what) is { } tab)
                {
                    if (open) Tabs.SelectedItem = tab;
                    c = tab;
                }
                break;
            case "button":
                var b = this.GetLogicalDescendants().OfType<Button>().FirstOrDefault(x => (x.Content as string) == what);
                if (b == null) break;
                // Its tab first, then into view
                if (b.FindLogicalAncestorOfType<TabItem>() is { } t2) Tabs.SelectedItem = t2;
                await Task.Delay(120);
                b.BringIntoView();
                c = b;
                break;
            case "menu":
                var path = what.Split('>');
                var top = this.GetLogicalDescendants().OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == path[0]);
                if (top == null) break;
                c = top;
                if (open && path.Length > 1)
                {
                    top.Open();
                    await Task.Delay(150);
                    var item = top.Items.OfType<MenuItem>().FirstOrDefault(m => (m.Header as string) == path[1]);
                    if (item != null) { c = item; item.Focus(); }
                }
                break;
        }
        if (c == null) return null;
        await Task.Delay(30);
        if (AdornerLayer.GetAdornerLayer(c) is { } layer)
        {
            var mark = new Border { BorderBrush = Look.Accent, BorderThickness = new Thickness(3), CornerRadius = new CornerRadius(6), Margin = new Thickness(-3), IsHitTestVisible = false };
            AdornerLayer.SetAdornedElement(mark, c);
            layer.Children.Add(mark);
            lit = c; litMark = mark;
            // A few blinks, then it stays lit until the step changes
            for (int i = 0; i < 6 && litMark == mark; i++) { mark.Opacity = i % 2 == 0 ? 0.25 : 1; await Task.Delay(220); }
        }
        return c;
    }

    // ---------------------------- the first start ----------------------------

    /// <summary>The first time: a card offering the tours (Help > Tours has them any time)</summary>
    void OfferToursOnce()
    {
        if (AutoScenario.Running || Settings.Get("tours") != null) return;
        var stack = new StackPanel { Spacing = 6 };
        stack.Children.Add(new TextBlock { Text = "New to RV Car Studio?", FontSize = 15, FontWeight = FontWeight.SemiBold });
        stack.Children.Add(new TextBlock { Text = "A tour walks you through it step by step, pointing at what to click. Help > Tours has them any time.", TextWrapping = TextWrapping.Wrap, FontSize = 12 });
        var row = new WrapPanel { Orientation = Orientation.Horizontal };
        for (int i = 0; i < TourNames.Length; i++)
        {
            int k = i;
            var b = new Button { Content = TourNames[i], FontSize = 12, Padding = new Thickness(10, 3), Margin = new Thickness(0, 4, 6, 0) };
            if (i == 0) Look.Primary(b);
            b.Click += (_, _) => StartTour(k);
            row.Children.Add(b);
        }
        var no = new Button { Content = "No thanks", FontSize = 12, Padding = new Thickness(10, 3), Margin = new Thickness(0, 4, 6, 0) };
        no.Click += (_, _) => { Settings.Put("tours", "seen"); tourBox.IsVisible = false; };
        row.Children.Add(no);
        stack.Children.Add(row);
        tourBox.Child = stack;
        tourBox.IsVisible = true;
    }
}
