using SkiaSharp;

namespace RvStudio.Core;

/// <summary>
/// The carbox: the picture of the car on the selection screen. The car is cut out of the view (once on white, once on
/// black: the difference is how see-through each pixel is, so soft edges and shadows stay soft), then put on a
/// background with its name, as the options say.
/// </summary>
public static class Carbox
{
    public enum Back { Plain, Gradient, Picture }
    public enum NameStyle { Echoes, Plain, Outline, None }
    public enum NamePlace { Top, Bottom, Middle }
    /// <summary>Which way the name runs</summary>
    public enum NameWay { Across, Down, Up, UpsideDown, Stacked }
    /// <summary>How long the name is: as wide as the box, smaller only when it would run off, or as it comes</summary>
    public enum NameFit { Box, Shrink, Free }

    public static readonly string[] BackNames = { "One colour", "Two colours, top to bottom", "A picture" };
    public static readonly string[] NameStyleNames = { "Echoes, as the stock cars", "Plain", "Outlined", "No name" };
    public static readonly string[] NamePlaceNames = { "At the top (or left)", "At the bottom (or right)", "In the middle" };
    public static readonly string[] NameWayNames = { "Across", "Down the side (turned right)", "Up the side (turned left)", "Upside down (180°)", "Letters one under the other" };
    public static readonly string[] NameFitNames = { "As wide as the box", "Smaller only if too long", "As it comes" };

    /// <summary>Where the name goes by itself when it's turned: its feet to the box's edge, its echoes into the box</summary>
    public static NamePlace PlaceFor(NameWay way) => way is NameWay.Across or NameWay.Up ? NamePlace.Bottom : NamePlace.Top;

    /// <summary>
    /// How a carbox looks. The defaults are the game's own: white, the car big and angled in front, its name at the
    /// bottom in heavy capitals as wide as the box, faded copies of it stacked up behind the car.
    /// </summary>
    public sealed record Style(
        Back Background = Back.Plain, SKColor? Colour1 = null, SKColor? Colour2 = null, string? Picture = null,
        string Name = "", NameStyle Text = NameStyle.Echoes, NamePlace Place = NamePlace.Bottom, SKColor? NameColour = null,
        float NameSize = 1.6f, float CarSize = 0.95f, float CarLift = 0.12f, bool Frame = false, bool NameBehind = true, bool Shadow = true, float Tilt = 15,
        NameWay Way = NameWay.Across, NameFit Fit = NameFit.Box, float Spacing = 0, float NameWidth = 1, float NameTilt = 0, float NameShift = 0);

    /// <summary>
    /// The name's shape: feet at y = 0 and its middle at x = 0 (letters one under the other: its middle at 0, 0), as high
    /// as asked and as long as the fit says. Spacing is the room between letters, in letter heights (below 0 they close up).
    /// </summary>
    static SKPath NamePath(string name, SKTypeface face, Style s, int size, out float height)
    {
        bool stacked = s.Way == NameWay.Stacked;
        // At 100 first, then scaled: the letters' own outlines, so the measure is exact
        using var p = new SKPaint { Typeface = face, TextSize = 100, IsAntialias = true };
        using var hp = p.GetTextPath("H", 0, 0);
        float cap = Math.Max(1, -hp.TightBounds.Top);
        float gap = Math.Clamp(s.Spacing, -0.4f, 2f) * cap;
        var path = new SKPath();
        float at = 0;
        // Whole letters, also those made of two chars
        var letters = System.Globalization.StringInfo.GetTextElementEnumerator(name);
        while (letters.MoveNext())
        {
            var ch = letters.GetTextElement();
            using var g = p.GetTextPath(ch, 0, 0);
            if (stacked)
            {
                // A column: each letter under the last, its middle on the line
                if (ch.Trim() == "") { at += cap * 0.5f; continue; }
                path.AddPath(g, -g.TightBounds.MidX, at + cap);
                at += cap * 1.12f + gap;
            }
            else
            {
                path.AddPath(g, at, 0);
                at += p.MeasureText(ch) + gap;
            }
        }
        var b = path.TightBounds;
        if (path.IsEmpty || b.Width < 1 || b.Height < 1) { height = 0; return path; }

        // Its length along the way it runs, and its thickness across
        float length = stacked ? b.Height : b.Width, thick = stacked ? b.Width : cap;
        float scale = size / 6f * Math.Clamp(s.NameSize, 0.2f, 3f) * 0.79f / thick;
        float room = size * 0.96f, span = Math.Clamp(s.NameWidth, 0.2f, 3f);
        float along;
        if (s.Fit == NameFit.Box)
        {
            // Exactly as long as the box. The height gives way first, so the letters keep their shape as far as it
            // reads; the rest is stretch
            along = room / length;
            scale *= Math.Clamp(along / scale, 0.6f, 1.35f);
            along *= span;
        }
        else
        {
            along = scale * span;
            if (s.Fit == NameFit.Shrink && length * along > room) { float k = room / (length * along); along *= k; scale *= k; }
        }
        height = thick * scale;
        path.Transform(stacked
            ? SKMatrix.CreateTranslation(-b.MidX, -b.MidY).PostConcat(SKMatrix.CreateScale(scale, along))
            : SKMatrix.CreateTranslation(-b.MidX, 0).PostConcat(SKMatrix.CreateScale(along, scale)));
        return path;
    }

    /// <summary>The stock carboxes' view of the car: nose towards you, turned 25 degrees, seen from 35 degrees above (tilted 15 in the picture)</summary>
    public const float StockTurn = -25, StockHeight = 35;

    /// <summary>The game's own look, the name in the car's main colour</summary>
    public static Style Stock(string name, SKColor? colour = null) => new(Name: name, NameColour: colour);

    /// <summary>A strong colour from the car's texture for its name: the most common colourful one (red when there's none)</summary>
    public static SKColor NameColourFrom(SKBitmap? texture)
    {
        if (texture == null) return new SKColor(0xD0, 0x30, 0x30);
        foreach (var (_, _, c) in CarTools.MainColours(texture))
        {
            c.ToHsv(out _, out var s, out var v);
            if (s > 45 && v > 35)
            {
                // As bright and strong as the stock carboxes' lettering
                c.ToHsv(out var h, out _, out _);
                var col = SKColor.FromHsv(h, Math.Max(s, 75), Math.Max(v, 80));
                // Light colours (yellow, lime, cyan) darkened until they read on white, as the stock lettering does
                for (float vv = Math.Max(v, 80); Luma(col) > 0.58 && vv > 30; vv -= 5) col = SKColor.FromHsv(h, Math.Max(s, 85), vv);
                return col;
            }
        }
        return new SKColor(0xD0, 0x30, 0x30);
    }

    /// <summary>The car on its own: from the same view drawn on white and on black</summary>
    public static SKBitmap Cutout(SKBitmap onWhite, SKBitmap onBlack)
    {
        int w = Math.Min(onWhite.Width, onBlack.Width), h = Math.Min(onWhite.Height, onBlack.Height);
        var o = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var a = onWhite.GetPixel(x, y);
                var b = onBlack.GetPixel(x, y);
                // On white a pixel is c*alpha + (1-alpha), on black c*alpha: the gap is 1 - alpha
                float gap = ((a.Red - b.Red) + (a.Green - b.Green) + (a.Blue - b.Blue)) / (3f * 255);
                float alpha = Math.Clamp(1 - gap, 0, 1);
                if (alpha < 0.004f) { o.SetPixel(x, y, SKColors.Transparent); continue; }
                byte C(byte v) => (byte)Math.Clamp(v / alpha, 0, 255);
                o.SetPixel(x, y, new SKColor(C(b.Red), C(b.Green), C(b.Blue), (byte)(alpha * 255)));
            }
        return o;
    }

    /// <summary>Where the car is in a cutout: the box around what isn't see-through</summary>
    public static SKRectI Bounds(SKBitmap cut)
    {
        int x0 = cut.Width, y0 = cut.Height, x1 = -1, y1 = -1;
        for (int y = 0; y < cut.Height; y++)
            for (int x = 0; x < cut.Width; x++)
                if (cut.GetPixel(x, y).Alpha > 12) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        return x1 < 0 ? new SKRectI(0, 0, cut.Width, cut.Height) : new SKRectI(x0, y0, x1 + 1, y1 + 1);
    }

    /// <summary>The carbox, size x size (256 as the game's own)</summary>
    public static SKBitmap Compose(SKBitmap cut, Style s, int size = 256)
    {
        var bmp = new SKBitmap(size, size, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var cv = new SKCanvas(bmp);
        var c1 = s.Colour1 ?? SKColors.White;
        var c2 = s.Colour2 ?? new SKColor(0xB8, 0xC8, 0xE0);

        // Background
        cv.Clear(c1);
        if (s.Background == Back.Gradient)
        {
            using var sh = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, size), new[] { c1, c2 }, SKShaderTileMode.Clamp);
            using var gp = new SKPaint { Shader = sh };
            cv.DrawRect(0, 0, size, size, gp);
        }
        else if (s.Background == Back.Picture && s.Picture != null && Images.Load(s.Picture) is { } pic)
        {
            using (pic)
            {
                // Filled, the middle kept
                float k = Math.Max(size / (float)pic.Width, size / (float)pic.Height);
                float pw = pic.Width * k, ph = pic.Height * k;
                using var pp = new SKPaint { FilterQuality = SKFilterQuality.High };
                cv.DrawBitmap(pic, new SKRect((size - pw) / 2, (size - ph) / 2, (size + pw) / 2, (size + ph) / 2), pp);
            }
        }

        var name = s.Name.Trim().ToUpperInvariant();
        bool named = s.Text != NameStyle.None && name != "";
        var nameColour = s.NameColour ?? new SKColor(0xD0, 0x30, 0x30);
        // Heavy condensed capitals, as the stock carboxes (Impact on Windows; the bold sans elsewhere)
        using var typeface = SKTypeface.FromFamilyName("Impact") is { } imp && imp.FamilyName.Equals("Impact", StringComparison.OrdinalIgnoreCase) ? imp : SKTypeface.FromFamilyName(null, SKFontStyle.Bold);
        using var paint = new SKPaint { IsAntialias = true };
        float nameH = 0;
        using var namePath = named ? NamePath(name, typeface, s, size, out nameH) : new SKPath();
        if (nameH <= 0) named = false;

        void Name()
        {
            if (!named) return;
            // Drawn across, then turned as a whole: so the edge it sits at is worked out the other way round
            bool stacked = s.Way == NameWay.Stacked;
            float turn = s.Way switch { NameWay.Down => 90, NameWay.Up => -90, NameWay.UpsideDown => 180, _ => 0 };
            bool low = s.Way is NameWay.Down or NameWay.UpsideDown ? s.Place == NamePlace.Top : s.Place == NamePlace.Bottom;
            bool mid = s.Place == NamePlace.Middle;
            float edge = size * 0.04f, step = nameH * 1.09f;
            // The solid one's middle, across the way it runs; the echoes go from its edge into the box (both ways from the middle)
            float main = mid ? size / 2f : low ? size - edge - nameH / 2 : edge + nameH / 2;
            int dir = low ? -1 : 1;
            main += dir * s.NameShift * size;

            cv.Save();
            cv.RotateDegrees(turn, size / 2f, size / 2f);
            if (Math.Abs(s.NameTilt) > 0.01f)
            {
                if (stacked) cv.RotateDegrees(s.NameTilt, main, size / 2f);
                else cv.RotateDegrees(s.NameTilt, size / 2f, main);
            }
            void One(float at)
            {
                cv.Save();
                if (stacked) cv.Translate(at, size / 2f); else cv.Translate(size / 2f, at + nameH / 2);
                cv.DrawPath(namePath, paint);
                cv.Restore();
            }
            switch (s.Text)
            {
                case NameStyle.Echoes:
                    {
                        // The stock carboxes: the name solid, and paler copies of it stacked away from it (up from the
                        // bottom, down from the top) until the box is full
                        int copies = Math.Clamp((int)(size * 1.2f / step), 1, 40);
                        for (int i = copies; i >= 0; i--)
                        {
                            byte a = (byte)(i == 0 ? 255 : Math.Max(18, 120 - i * 26));
                            paint.Color = nameColour.WithAlpha(a);
                            One(main + dir * i * step);
                            if (mid && i > 0) One(main - dir * i * step);
                        }
                        break;
                    }
                case NameStyle.Outline:
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeJoin = SKStrokeJoin.Round;
                    paint.StrokeWidth = Math.Max(2, nameH / 7);
                    paint.Color = Contrast(nameColour);
                    One(main);
                    paint.Style = SKPaintStyle.Fill;
                    paint.Color = nameColour;
                    One(main);
                    break;
                default:
                    paint.Color = nameColour;
                    One(main);
                    break;
            }
            cv.Restore();
        }

        // The name behind the car (the stock way), or in front of it
        if (s.NameBehind) Name();

        // The car: fitted into the box (over the name when it's behind, beside it when in front), at the size and height asked
        var b = Bounds(cut);
        bool inFront = named && !s.NameBehind && (s.Way == NameWay.Across || s.Way == NameWay.UpsideDown);
        float roomTop = inFront && s.Place == NamePlace.Top ? size * 0.16f : size * 0.03f;
        float roomBottom = inFront && s.Place == NamePlace.Bottom ? size * 0.2f : size * 0.03f;
        float boxW = size * 0.94f, boxH = size - roomTop - roomBottom;
        float scale = Math.Min(boxW / b.Width, boxH / b.Height) * Math.Clamp(s.CarSize, 0.4f, 1.6f);
        // Bigger may reach past the top and bottom, as the stock ones do, but not off the sides
        scale = Math.Min(scale, size * 0.98f / b.Width);
        float cw = b.Width * scale, ch = b.Height * scale;
        float cx = (size - cw) / 2, cy = roomTop + (boxH - ch) / 2 - s.CarLift * size * 0.25f;
        var dest = new SKRect(cx, cy, cx + cw, cy + ch);
        // Tilted on a diagonal, as the stock carboxes: turned round its middle
        cv.Save();
        if (Math.Abs(s.Tilt) > 0.01f) cv.RotateDegrees(s.Tilt, dest.MidX, dest.MidY);
        // The stock carboxes' soft shadow: the car's own outline, dark and blurred, a little down and to the right
        if (s.Shadow)
        {
            float off = size * 0.03f;
            using var sh = new SKPaint
            {
                FilterQuality = SKFilterQuality.High, IsAntialias = true,
                ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(0, 0, 0, 120), SKBlendMode.SrcIn),
                ImageFilter = SKImageFilter.CreateBlur(size * 0.018f, size * 0.018f)
            };
            var sd = dest;
            sd.Offset(off, off * 1.3f);
            cv.DrawBitmap(cut, new SKRect(b.Left, b.Top, b.Right, b.Bottom), sd, sh);
        }
        using (var cp = new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true })
            cv.DrawBitmap(cut, new SKRect(b.Left, b.Top, b.Right, b.Bottom), dest, cp);
        cv.Restore();

        if (!s.NameBehind) Name();

        if (s.Frame)
        {
            using var fp = new SKPaint { Style = SKPaintStyle.Stroke, StrokeWidth = size / 64f, Color = nameColour, IsAntialias = true };
            cv.DrawRect(fp.StrokeWidth / 2, fp.StrokeWidth / 2, size - fp.StrokeWidth, size - fp.StrokeWidth, fp);
        }

        return bmp;
    }

    static double Luma(SKColor c) => (0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue) / 255;

    static SKColor Contrast(SKColor c) => (0.299 * c.Red + 0.587 * c.Green + 0.114 * c.Blue) > 140 ? new SKColor(20, 20, 24) : SKColors.White;
}
