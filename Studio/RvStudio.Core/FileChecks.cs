using System.Text;

namespace RvStudio.Core;

/// <summary>
/// Is a picked file really what it says? Each check gives null when the file is fine, else why not, in words a car
/// maker understands. Pick buttons use them so a wrong file never reaches the car.
/// </summary>
public static class FileChecks
{
    /// <summary>A picture the texture, shadow or carbox can be made from</summary>
    public static string? Picture(string path)
    {
        try
        {
            using var b = Images.Load(path);
            if (b == null) return Names.File(path) + " isn't a picture this can read (use .bmp, .png or .jpg)";
            if (b.Width < 8 || b.Height < 8) return Names.File(path) + " is too small: " + b.Width + " x " + b.Height;
            return null;
        }
        catch (Exception ex) { return Names.File(path) + " can't be read: " + ex.Message; }
    }

    /// <summary>A texture page: a picture, square with a power of two side (other sizes are made so when copied in)</summary>
    public static string? Texture(string path) => Picture(path);

    /// <summary>A collision hull: a .hul the game can read, with something in it</summary>
    public static string? Hull(string path)
    {
        if (!Path.GetExtension(path).Equals(".hul", StringComparison.OrdinalIgnoreCase)) return "A hull is a .hul file, not " + Path.GetExtension(path);
        try
        {
            var h = Core.Hull.Load(path);
            if (h.Convex.Count == 0 && h.Spheres.Count == 0) return Names.File(path) + " has no hull parts and no spheres in it";
            if (h.Convex.Any(c => c.Points.Length < 4 || c.Faces.Length < 4)) return Names.File(path) + " has a hull part that isn't a solid";
            return null;
        }
        catch (Exception) { return Names.File(path) + " isn't a hull the game can read"; }
    }

    /// <summary>A model: a .prm (or an .obj, made into one) with polygons</summary>
    public static string? Model(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".prm" or ".obj" or ".m")) return "A model is a .prm or .obj file, not " + (ext == "" ? "a file without an ending" : ext);
        try
        {
            var m = CarTools.LoadModelFile(path);
            if (m.Faces.Length == 0) return Names.File(path) + " has no polygons in it";
            if (!m.IsValid()) return Names.File(path) + " is damaged: polygons point at corners it doesn't have";
            return null;
        }
        catch (Exception ex) { return Names.File(path) + " can't be read: " + ex.Message; }
    }

    /// <summary>
    /// A sound: a plain (PCM) .wav, 8 or 16 bit, mono or stereo, as the game plays them. A compressed WAV or an MP3
    /// renamed .wav doesn't play.
    /// </summary>
    public static string? Wav(string path)
    {
        var name = Names.File(path);
        if (!Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)) return "A sound is a .wav file, not " + Path.GetExtension(path);
        try
        {
            using var f = File.OpenRead(path);
            using var r = new BinaryReader(f);
            if (f.Length < 44) return name + " is too short to be a sound";
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "RIFF") return name + " isn't a WAV (it may be an MP3 or another file renamed .wav)";
            r.ReadInt32();
            if (Encoding.ASCII.GetString(r.ReadBytes(4)) != "WAVE") return name + " isn't a WAV";
            bool fmt = false, data = false;
            while (f.Position + 8 <= f.Length && !(fmt && data))
            {
                var id = Encoding.ASCII.GetString(r.ReadBytes(4));
                int size = r.ReadInt32();
                long next = f.Position + size + (size & 1);
                if (id == "fmt ")
                {
                    int format = r.ReadInt16(), channels = r.ReadInt16();
                    int rate = r.ReadInt32(); r.ReadInt32(); r.ReadInt16();
                    int bits = r.ReadInt16();
                    if (format != 1) return name + " is a compressed WAV: save it as plain PCM (16 bit) in your sound editor";
                    if (channels is < 1 or > 2) return name + $" has {channels} channels: mono or stereo please";
                    if (bits is not (8 or 16)) return name + $" is {bits} bit: save it as 16 bit";
                    if (rate is < 8000 or > 48000) return name + $" plays at {rate} Hz: 22050 or 44100 is usual";
                    fmt = true;
                }
                else if (id == "data") { if (size <= 0) return name + " has no sound in it"; data = true; }
                if (next <= f.Position && id != "fmt ") break;
                f.Position = Math.Min(next, f.Length);
            }
            if (!fmt || !data) return name + " is missing part of a WAV (no " + (!fmt ? "format" : "sound data") + ")";
            return null;
        }
        catch (Exception ex) { return name + " can't be read: " + ex.Message; }
    }
}
