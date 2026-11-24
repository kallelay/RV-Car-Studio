using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RvStudio.Core;

namespace RvStudio.App;

public sealed partial class MainWindow
{
    // ---------------------------- Handling like another car ----------------------------
    //
    // The easiest way to a car that drives well: take a car you like the feel of. The list says how each one drives
    // (top speed, grip, class); one click takes its engine, steering, weight, grip, springs and, if you like, its CPU
    // driving, and leaves your models, looks and menu as they are. Compare first opens the popup to choose value by value.

    public sealed record HandlingCar(string Folder, string Name, bool Stock, string Says);

    List<HandlingCar>? handlingCars;

    /// <summary>The other cars and how each drives, the stock ones first. Read once, when the list is first opened</summary>
    public List<HandlingCar> HandlingCars()
    {
        if (handlingCars != null) return handlingCars;
        var r = new List<HandlingCar>();
        if (Doc.Game is not { } game) return r;
        foreach (var e in game.ListCars().OrderBy(c => c.Stock ? 0 : 1).ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (Doc.Car != null && e.Folder.Equals(Doc.Car.Folder, StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                var d = new Car(game, e.Folder).Data;
                var calc = CarCalc.Of(d, null);
                string cls = d.Class is >= 0 and < 3 ? CarTools.ClassNames[d.Class] : "Special";
                string rating = d.Rating is >= 0 and < 6 ? CarTools.RatingNames[d.Rating] : "";
                r.Add(new HandlingCar(e.Folder, e.Name, e.Stock,
                    $"{CarCalc.ToMph(calc.TopSpeedReal):0} mph, grip {calc.GripFront:0.0} g, {cls}{(rating != "" ? ", " + rating : "")}"));
            }
            catch (Exception) { }
        }
        return handlingCars = r;
    }

    public ComboBox HandlingBox = null!;
    public CheckBox HandlingWithAi = null!;

    void HandlingLike(Page p)
    {
        p.Header("Handling like another car");
        p.Note("Take the way a car you like drives: its engine, steering, weight, grip and springs. Your models, looks and menu stay as they are. Undo takes it back.");
        HandlingBox = new ComboBox { FontSize = 12, HorizontalAlignment = HorizontalAlignment.Stretch, PlaceholderText = "Choose a car...", MaxDropDownHeight = 420 };
        void Fill()
        {
            if (HandlingBox.ItemsSource != null) return;
            HandlingBox.ItemsSource = HandlingCars().Select(c => new TextBlock
            {
                Inlines = new Avalonia.Controls.Documents.InlineCollection
                {
                    new Avalonia.Controls.Documents.Run(c.Name) { FontWeight = FontWeight.SemiBold },
                    new Avalonia.Controls.Documents.Run((c.Stock ? "" : "  (yours)") + "   " + c.Says) { Foreground = Look.Faint }
                },
                FontSize = 12
            }).ToList();
        }
        HandlingBox.DropDownOpened += (_, _) => Fill();
        Doc.CarOpened += () => { handlingCars = null; HandlingBox.ItemsSource = null; };
        p.Labeled("Drive like", HandlingBox, "The game's own cars first, each with how it drives");
        HandlingWithAi = new CheckBox { Content = "And how CPU drivers drive it (its AI values)", IsChecked = true, FontSize = 12, MinHeight = 24 };
        ToolTip.SetTip(HandlingWithAi, "The AI values are tuned for a car's handling: taken with it, CPU cars drive yours well");
        p.Add(HandlingWithAi);
        p.Button("Take its handling", () => TakeHandling(), "In one step, with undo", true);
        p.Button("Compare first, value by value...", () =>
        {
            if (HandlingBox.SelectedIndex < 0) { OpenCompare(); return; }
            OpenCompare(HandlingCars()[HandlingBox.SelectedIndex].Folder);
        }, "A window with this car's values beside the other's: tick the ones to take");
    }

    static readonly string[] HandlingGroups = { CarCompare.Groups.Engine, CarCompare.Groups.Steering, CarCompare.Groups.Mass, CarCompare.Groups.Wheels, CarCompare.Groups.Suspension };

    /// <summary>The chosen car's handling onto this one. Returns how many values changed (0: nothing chosen)</summary>
    public int TakeHandling(string? folder = null, bool? withAi = null)
    {
        if (Doc.Car is not { } car || Doc.D == null || Doc.Game == null) return 0;
        folder ??= HandlingBox.SelectedIndex >= 0 && HandlingBox.SelectedIndex < HandlingCars().Count ? HandlingCars()[HandlingBox.SelectedIndex].Folder : null;
        if (folder == null) { Say("Choose a car to drive like first"); return 0; }
        bool ai = withAi ?? HandlingWithAi.IsChecked == true;
        Car theirs;
        try { theirs = new Car(Doc.Game, folder); }
        catch (Exception ex) { Say("Can't read that car: " + ex.Message); return 0; }
        // The handling: not where the weapons come out, nor anything the models or looks decide
        var rows = CarCompare.Diff(Doc.D, theirs.Data)
            .Where(r => (HandlingGroups.Contains(r.Group) || (ai && r.Group == CarCompare.Groups.Ai)) && !r.Key.StartsWith("Weapon"))
            .Where(r => r.Theirs != "")
            .ToList();
        if (rows.Count == 0) { Say(theirs.Data.Name + " already drives like this car"); return 0; }
        var data = CarCompare.Take(car.PreviewText(), rows, GameFolder.IsStock(car.Folder));
        Doc.ReplaceData("Handling like " + theirs.Data.Name, data);
        Say($"Drives like {theirs.Data.Name} now: {rows.Count} values taken. Undo takes them back; Drive here to feel it");
        Did("Handling");
        return rows.Count;
    }
}
