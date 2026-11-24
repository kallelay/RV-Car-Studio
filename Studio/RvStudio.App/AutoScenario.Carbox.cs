using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

// The carbox's name: every direction, length and spacing, drawn where it should be
public static partial class AutoScenario
{
    static async Task CarboxText(CarboxWindow cw)
    {
        var keep = cw.Style;
        // Only the name, black on white: where its ink is
        SKRectI Ink(Carbox.Style s)
        {
            using var none = new SKBitmap(8, 8, SKColorType.Bgra8888, SKAlphaType.Unpremul);
            none.Erase(SKColors.Transparent);
            using var b = Carbox.Compose(none, s with { Text = Carbox.NameStyle.Plain, Shadow = false, NameColour = SKColors.Black });
            int x0 = 256, y0 = 256, x1 = -1, y1 = -1;
            for (int y = 0; y < 256; y++)
                for (int x = 0; x < 256; x++)
                    if (b.GetPixel(x, y).Red < 128) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
            return new SKRectI(x0, y0, x1 + 1, y1 + 1);
        }
        foreach (var nm in new[] { "AMW", "RC Bandit", "XM250 Tork Special Edition" })
        {
            var st = Carbox.Stock(nm);
            var a = Ink(st);
            Check(Math.Abs(a.Width - 246) <= 4 && a.Top > 120 && a.Bottom <= 252, $"{nm}: across, as wide as the box, at the bottom: {a}");
            var dn = Ink(st with { Way = Carbox.NameWay.Down, Place = Carbox.NamePlace.Top });
            Check(Math.Abs(dn.Height - 246) <= 4 && dn.Left < 20 && dn.Right < 136, $"{nm}: down the left side: {dn}");
            var up = Ink(st with { Way = Carbox.NameWay.Up });
            Check(Math.Abs(up.Height - 246) <= 4 && up.Right > 236 && up.Left > 120, $"{nm}: up the right side: {up}");
            var ud = Ink(st with { Way = Carbox.NameWay.UpsideDown, Place = Carbox.NamePlace.Top });
            Check(Math.Abs(ud.Width - 246) <= 4 && ud.Top < 20 && ud.Bottom < 136, $"{nm}: upside down at the top: {ud}");
            var sk = Ink(st with { Way = Carbox.NameWay.Stacked, Place = Carbox.NamePlace.Top });
            Check(Math.Abs(sk.Height - 246) <= 4 && sk.Left < 20 && sk.Right < 136, $"{nm}: letters one under the other, at the left: {sk}");
            var md = Ink(st with { Place = Carbox.NamePlace.Middle });
            Check(Math.Abs(md.MidY - 128) <= 4, $"{nm}: in the middle: {md}");
            var sp = Ink(st with { Spacing = 0.5f });
            Check(Math.Abs(sp.Width - 246) <= 4, $"{nm}: kerning keeps it as wide as the box: {sp}");
        }
        var free = Carbox.Stock("AMW") with { Fit = Carbox.NameFit.Free, NameSize = 0.8f };
        Check(Ink(free with { Spacing = 0.4f }).Width > Ink(free).Width + 10, "kerning spreads the letters");
        Check(Ink(free with { Spacing = -0.1f }).Width < Ink(free).Width, "and closes them up");
        Check(Ink(free with { NameWidth = 2 }).Width > Ink(free).Width * 1.8, "width stretches it");
        Check(Ink(free with { NameSize = 1.6f }).Height > Ink(free).Height * 1.8, "height makes it taller");
        Check(Ink(Carbox.Stock("Toyeca") with { NameWidth = 0.5f }).Width is > 115 and < 131, "half the box's width");
        Check(Ink(Carbox.Stock("XM250 Tork Special Edition") with { Fit = Carbox.NameFit.Shrink }).Width <= 248, "smaller only if too long: it fits");
        Check(Ink(free with { NameTilt = 30 }).Height > Ink(free).Height + 10, "slant turns it");
        foreach (var way in Enum.GetValues<Carbox.NameWay>())
            foreach (var nm in new[] { keep.Name, "XM250 Tork SE" })
            {
                cw.Style = keep with { Way = way, Place = Carbox.PlaceFor(way), Name = nm };
                await cw.Shoot();
                SavePng(cw.Result!, "carbox_way_" + way + (nm == keep.Name ? "" : "_long"));
            }
        cw.Style = keep;
        cw.UseLook(4);
        await Wait(200);
        Check(cw.Style.Way == Carbox.NameWay.Down && cw.Style.Place == Carbox.NamePlace.Top, "the Side look: down the left side");
        SaveWin(cw, "carbox_side");
    }
}
