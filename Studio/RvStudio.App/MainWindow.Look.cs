using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace RvStudio.App;

// The look you chose, and what the 3D view shows beside the mouse: names, amounts, a part's menu
public sealed partial class MainWindow
{
    // --------------------------- Theme ---------------------------

    readonly List<MenuItem> themeItems = new();
    readonly Button themeBtn = new();
    public Button ThemeButton => themeBtn;
    public ThemeKind? ThemeMenuChecked => themeItems.Where(i => i.IsChecked).Select(i => (ThemeKind?)i.Tag).FirstOrDefault();
    static string ThemeButtonText => "◐  " + Look.Kind;

    public void SetTheme(ThemeKind kind)
    {
        Look.Apply(kind);
        Settings.Theme = kind;
        Say(kind switch
        {
            ThemeKind.Dark => "Dark theme: black",
            ThemeKind.Light => "Light theme: ghost white",
            _ => "Classic theme: as it was"
        });
    }

    void NextTheme() => SetTheme(Look.Kind switch { ThemeKind.Dark => ThemeKind.Light, ThemeKind.Light => ThemeKind.Classic, _ => ThemeKind.Dark });

    void OnThemeChanged()
    {
        themeBtn.Content = ThemeButtonText;
        Cube.InvalidateVisual();
        foreach (var i in themeItems) i.IsChecked = Equals(i.Tag, Look.Kind);
        View.Redraw();
        raceMap.InvalidateVisual();
        // The small pictures of the cars have the look's colour behind them
        thumbs.Clear();
        if (Doc.Game != null && !CarLocked) FillList();
    }

    // --------------------------- Beside the mouse ---------------------------

    readonly TextBlock viewTipText = new() { FontSize = 12, Foreground = Look.Text };
    readonly Border viewTip = new()
    {
        IsVisible = false, IsHitTestVisible = false, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        Background = Look.TipBack, BorderBrush = Look.Edge, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(Look.Radius), Padding = new Thickness(9, 4)
    };

    /// <summary>What the label in the view says now. Empty when it's not there</summary>
    public string ViewTipText => viewTip.IsVisible ? viewTipText.Text ?? "" : "";

    void ShowViewTip(string? text, Point at)
    {
        if (string.IsNullOrEmpty(text)) { viewTip.IsVisible = false; return; }
        viewTip.Child ??= viewTipText;
        viewTipText.Text = text;
        // Below and right of the mouse, kept inside the view
        double w = Math.Max(View.Bounds.Width, 1), h = Math.Max(View.Bounds.Height, 1);
        double width = 20 + text.Length * 6.6;
        double x = Math.Clamp(at.X + 16, 4, Math.Max(4, w - width)), y = Math.Clamp(at.Y + 18, 4, Math.Max(4, h - 30));
        viewTip.Margin = new Thickness(x, y, 0, 0);
        viewTip.IsVisible = true;
    }

    // --------------------------- A part's menu ---------------------------

    readonly MenuFlyout partMenu = new();
    readonly List<RadioButton> moveButtons = new();
    public bool PartMenuOpen => partMenu.IsOpen;
    /// <summary>The open menu's own window, to take a picture of</summary>
    public Visual? PartMenuRoot => partMenu.Items.OfType<Control>().Select(i => TopLevel.GetTopLevel(i) as Visual).FirstOrDefault(t => t != null && t != this);
    public void ClosePartMenu() => partMenu.Hide();

    public void SetMoveMode(MoveMode m)
    {
        Doc.MoveMode = m;
        foreach (var b in moveButtons) if (Equals(b.Tag, m)) b.IsChecked = true;
        View.Redraw();
    }

    static MenuItem Radio(string text, bool on, Action act)
    {
        var m = Item(text, act);
        m.ToggleType = MenuItemToggleType.Radio;
        m.IsChecked = on;
        return m;
    }

    /// <summary>What a right click on this part offers</summary>
    public List<Control> PartMenuItems(Selection s)
    {
        var items = new List<Control>();
        if (s.IsHull)
            items.Add(Item("Remove", () => Doc.RemoveHullItem(s)));
        else if (Doc.HasModel(s) || s.Kind is SelKind.AerialBase or SelKind.AerialTip)
        {
            items.Add(Item(Doc.Hidden.Contains(s) ? "Show" : "Hide", () =>
            {
                Doc.ToggleHidden(s);
                FillParts();
                OnSelectionChanged();
                Say(s.Label + (Doc.Hidden.Contains(s) ? ": hidden in the view. The Parts list shows it again" : ": shown"));
            }));
            items.Add(Item("Replace model...", async () => await PickModelFor(s)));
            items.Add(Item("Remove", () => Doc.RemovePart(s)));
        }
        items.Add(Item("Frame", () => View.FramePart(s)));
        items.Add(new Separator());
        items.Add(Radio("Move", Doc.Tool == GizmoTool.Move, () => SetTool(GizmoTool.Move)));
        if (Doc.CanTransform(s) || s.Kind == SelKind.HullSphere)
        {
            if (s.Kind != SelKind.HullSphere) items.Add(Radio("Rotate", Doc.Tool == GizmoTool.Rotate, () => SetTool(GizmoTool.Rotate)));
            items.Add(Radio("Scale", Doc.Tool == GizmoTool.Scale, () => SetTool(GizmoTool.Scale)));
        }
        if (s.HasFourCorners || s.Kind == SelKind.HullSphere)
        {
            var mirror = new MenuItem { Header = "Mirror mode", FontSize = 13 };
            foreach (var (mode, text) in new[] { (MoveMode.Single, "Single"), (MoveMode.Mirror, "Mirror L/R"), (MoveMode.FrontBack, "Front/Back"), (MoveMode.All, "All four") })
                mirror.Items.Add(Radio(text, Doc.MoveMode == mode, () => SetMoveMode(mode)));
            items.Add(mirror);
        }
        return items;
    }

    void ShowPartMenu(Selection s, Point at)
    {
        partMenu.Items.Clear();
        foreach (var i in PartMenuItems(s)) partMenu.Items.Add(i);
        partMenu.ShowAt(View, true);
    }
}
