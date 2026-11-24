using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace RvStudio.App;

public sealed partial class MainWindow
{
    // ---------------------------- Easy and Pro ----------------------------
    //
    // Pro (the start): RV Car Studio 3, with everything: model slots, the folder's files, parameters.txt as text, menu
    // stats, AI values, vertex normals, RVGL's own values (Properties (RVGL)). Easy: the window of the old RV Car
    // Studio (MainWindow.Easy.cs). One click switches, and it's remembered.

    // (Not the key "mode": before, it was set to easy without being asked)
    public bool ProMode { get; private set; } = AutoScenario.Running || Settings.Get("layout") != "easy";
    readonly List<RadioButton> modeButtons = new();
    public TextBox RawText => rawText;

    /// <summary>The Easy | Pro switch on the top bar</summary>
    Control ModeSwitch()
    {
        foreach (var (pro, text, tip) in new[]
        {
            (false, "Easy", "The old RV Car Studio's window"),
            (true, "Pro", "Everything: model slots, parameters.txt as text, menu stats, AI values, vertex normals, RVGL's own values")
        })
        {
            var r = new RadioButton { Content = text, GroupName = "mode", Tag = pro, Classes = { "seg" } };
            ToolTip.SetTip(r, tip);
            r.IsCheckedChanged += (_, _) => { if (r.IsChecked == true) SetMode(pro); };
            modeButtons.Add(r);
        }
        return Look.Group(modeButtons.ToArray());
    }

    public void SetMode(bool pro)
    {
        if (pro == ProMode) return;
        ProMode = pro;
        if (!AutoScenario.Running) Settings.Put("layout", pro ? "pro" : "easy");
        if (!pro) proTab = Tabs.SelectedIndex;
        ApplyMode();
        if (pro) Tabs.SelectedIndex = proTab;
        Say(pro ? "Pro: everything, with RVGL's own values" : "Easy: as the old RV Car Studio (Pro editor goes back)");
    }

    void ApplyMode()
    {
        foreach (var b in modeButtons) b.IsChecked = Equals(b.Tag, ProMode);
        foreach (var p in pages) p.SetEasy(!ProMode && !easyPages.Contains(p));
        ApplyLayout();
    }

    /// <summary>A tab, and a section on it: where a link goes</summary>
    public void GoTo(int tab, string? section = null)
    {
        if (!ProMode) SetMode(true);
        Tabs.SelectedIndex = tab;
        if (section != null && (Tabs.SelectedItem as TabItem)?.Content is Page p)
            Avalonia.Threading.Dispatcher.UIThread.Post(() => p.JumpTo(section), Avalonia.Threading.DispatcherPriority.Background);
    }
}
