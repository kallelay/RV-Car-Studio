using System.Diagnostics.CodeAnalysis;

namespace RvStudio.Core;

/// <summary>
/// File names out of paths. The game writes its paths with backslashes; Path knows those only on Windows, so on Linux and
/// macOS "cars\rc\body.prm" would be one name. Here both slashes count everywhere.
/// </summary>
public static class Names
{
    [return: NotNullIfNotNull(nameof(path))]
    public static string? File(string? path)
    {
        if (path == null) return null;
        int i = Math.Max(path.LastIndexOf('\\'), path.LastIndexOf('/'));
        // A drive ("c:body.prm") counts too, as Path has it on Windows
        if (i < 0 && OperatingSystem.IsWindows()) return Path.GetFileName(path);
        return path[(i + 1)..];
    }

    [return: NotNullIfNotNull(nameof(path))]
    public static string? Stem(string? path)
    {
        var f = File(path);
        if (f == null) return null;
        int dot = f.LastIndexOf('.');
        return dot < 0 ? f : f[..dot];
    }
}
