using RvStudio.Core;
using SkiaSharp;

namespace RvStudio.App;

// Repainting live, as a hand on the slider does it: many small moves, a big texture
public static partial class AutoScenario
{
    static async Task LivePaint(MainWindow w, Doc d)
    {
        var big = Path.Combine(Path.GetTempPath(), "rvcs_big_" + Guid.NewGuid().ToString("N") + ".png");
        using (var b = new SKBitmap(2048, 2048, SKColorType.Bgra8888, SKAlphaType.Opaque))
        {
            using (var cv = new SKCanvas(b))
            {
                cv.Clear(SKColors.Crimson);
                using var p = new SKPaint { Color = SKColors.Gold };
                for (int i = 0; i < 64; i++) cv.DrawRect(i * 32, 0, 16, 2048, p);
            }
            using var data = b.Encode(SKEncodedImageFormat.Png, 100);
            File.WriteAllBytes(big, data.ToArray());
        }
        d.SkinOverride = big;
        w.ResetPaintTo(big);
        await Wait(800);
        int first = d.PaintVersion, shown = 0, last = first;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        long firstShown = -1;
        for (int i = 1; i <= 90; i++)
        {
            w.PaintHue.Value = i;
            await Wait(16);
            if (d.PaintVersion != last) { last = d.PaintVersion; shown++; if (firstShown < 0) firstShown = sw.ElapsedMilliseconds; }
        }
        long dragged = sw.ElapsedMilliseconds;
        var want = w.PaintNow();
        var wantPx = CarTools.AdjustTexture(new SKBitmap(1, 1) is var one && Set(one, SKColors.Crimson) ? one : one, want).GetPixel(0, 0);
        long end = -1;
        for (int k = 0; k < 200 && end < 0; k++)
        {
            await Wait(25);
            if (d.PaintPreview is { } px && d.PaintW == 2048 && Math.Abs(px[24 * 4] - wantPx.Red) <= 2 && Math.Abs(px[24 * 4 + 1] - wantPx.Green) <= 2 && Math.Abs(px[24 * 4 + 2] - wantPx.Blue) <= 2) end = sw.ElapsedMilliseconds - dragged;
        }
        Log($"live paint, 2048 x 2048: {shown} looks during a drag of {dragged} ms, the first after {firstShown} ms, the final one {end} ms after it");
        Check(shown >= 8 && firstShown >= 0 && firstShown < 400, $"the car follows the slider while it's dragged: {shown} looks in {dragged} ms, the first after {firstShown} ms");
        Check(end >= 0 && end < 700, $"and ends on the last look: {end} ms after the drag");
        // Back to plain at once, and nothing late comes after it
        w.PaintHue.Value = 120;
        w.PaintHue.Value = 0;
        await Wait(900);
        Check(d.PaintPreview == null, "back at 0 it's the texture itself, also a moment later");
        // Another texture while a look is still being worked out: the old one doesn't come back over it
        w.PaintHue.Value = 60;
        w.ResetPaint();
        await Wait(900);
        Check(d.PaintPreview == null && d.SkinOverride == null, "the last car's look doesn't come back");
        // A texture picked on the Files tab is the one the sliders work on
        var small = Path.Combine(d.Car!.Dir, "pickedtex.bmp");
        using (var s = new SKBitmap(64, 64, SKColorType.Bgra8888, SKAlphaType.Opaque)) { s.Erase(SKColors.Lime); CarTools.SaveTexture(s, small, false); }
        var was = d.D!.TPage;
        d.Edit("Texture", () => { d.D!.TPage = @"cars\" + d.Car.Folder + @"\pickedtex.bmp"; d.TextureVersion++; });
        d.EndGesture();
        await Wait(300);
        w.PaintHue.Value = 30;
        await Wait(600);
        Check(d.PaintW == 64 && d.PaintPreview != null, $"a texture picked on Files is the one repainted: {d.PaintW}");
        w.PaintHue.Value = 0;
        d.Undo();
        await Wait(300);
        Check(d.D!.TPage == was, "undo");
        w.PaintHue.Value = 45;
        await Wait(600);
        Check(d.PaintPreview != null && d.PaintW != 64, "and after undo it's the car's own again: " + d.PaintW);
        w.ResetPaint();
        File.Delete(big); File.Delete(small);
    }

    static bool Set(SKBitmap b, SKColor c) { b.Erase(c); return true; }
}
