using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace RvStudio.Core;

/// <summary>
/// parameters.txt as text. Saving only touches lines whose value changed, so comments, ;) lines,
/// unknown sections and the checksum at the end of stock files stay byte for byte.
/// </summary>
public sealed class ParamFile
{
    // Latin-1 maps every byte to one char, so nothing in the file can be damaged by a round trip
    public static readonly Encoding FileEncoding = Encoding.Latin1;

    readonly List<string> lines = new();
    readonly List<string> eols = new();
    readonly Dictionary<string, ParamSection> sections = new(StringComparer.OrdinalIgnoreCase);
    int mainClose = -1;

    public ParamSection Main { get; private set; } = new("");

    public ParamFile(string text) => SetText(text);
    public static ParamFile Load(string path) =>
        new(File.Exists(path) ? File.ReadAllText(path, FileEncoding) : "");

    public string Text
    {
        get
        {
            var sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++) sb.Append(lines[i]).Append(eols[i]);
            return sb.ToString();
        }
    }

    public ParamSection? Section(string header) =>
        string.IsNullOrWhiteSpace(header) ? Main : sections.GetValueOrDefault(header.Trim());

    public IEnumerable<string> SectionNames => sections.Keys;

    void SetText(string txt)
    {
        lines.Clear();
        eols.Clear();
        int st = 0;
        for (int i = 0; i < txt.Length; i++)
        {
            if (txt[i] != '\n') continue;
            bool cr = i > st && txt[i - 1] == '\r';
            lines.Add(txt.Substring(st, i - st - (cr ? 1 : 0)));
            eols.Add(cr ? "\r\n" : "\n");
            st = i + 1;
        }
        if (st < txt.Length) { lines.Add(txt[st..]); eols.Add(""); }
        Parse();
    }

    static readonly Regex ModelLine = new("^(?:MODEL\\s*)?(\\d+)\\s*(\"[^\"]*\"?.*)$", RegexOptions.IgnoreCase);
    static readonly Regex KeyLine = new("^([A-Za-z_][A-Za-z0-9_]*)\\s*(.*)$");

    void Parse()
    {
        Main = new ParamSection("");
        sections.Clear();
        mainClose = -1;
        int depth = 0;
        var cur = Main;
        ParamLine? last = null;
        int cont = 0;

        for (int i = 0; i < lines.Count; i++)
        {
            var code = CodeOf(lines[i], out bool ext, out int codeStart);
            var t = code.Trim();
            if (t.Length == 0) continue;

            int brace = t.IndexOf('{');
            if (brace >= 0 && t[0] != '"')
            {
                var hdr = Regex.Replace(t[..brace].Trim(), "\\s+", " ");
                if (hdr == "" && depth == 0) { depth = 1; continue; }
                // header on the line before the brace
                if (hdr == "" && last is { Value: "" }) { hdr = last.Key; cur.Items.Remove(last); }
                if (depth == 0) depth = 1;
                cur = new ParamSection(hdr) { OpenLine = i };
                sections[hdr] = cur;
                depth = 2;
                last = null;
                continue;
            }
            if (t[0] == '}')
            {
                depth--;
                if (depth == 1) cur.CloseLine = i;
                cur = Main;
                last = null;
                if (depth <= 0) { mainClose = i; break; }
                continue;
            }
            if (depth == 0) depth = 1;

            var item = new ParamLine { Line = i, Ext = ext };
            var mm = cur == Main ? ModelLine.Match(t) : Match.Empty;
            if (mm.Success)
            {
                item.Key = "MODEL " + int.Parse(mm.Groups[1].Value);
                SetRange(item, codeStart, code, mm.Groups[2].Value);
            }
            else if (IsNumberStart(t) && last != null)
            {
                // continuation of the value above (Inertia takes 3 lines)
                cont++;
                item.Key = BaseKey(last.Key) + "#" + cont;
                SetRange(item, codeStart, code, t);
                cur.Items.Add(item);
                last = item;
                continue;
            }
            else
            {
                var km = KeyLine.Match(t);
                if (!km.Success) continue;
                item.Key = km.Groups[1].Value;
                SetRange(item, codeStart, code, km.Groups[2].Value);
            }
            cont = 0;
            cur.Items.Add(item);
            last = item;
        }
    }

    static string BaseKey(string k) { int p = k.IndexOf('#'); return p < 0 ? k : k[..p]; }
    static bool IsNumberStart(string s) =>
        char.IsDigit(s[0]) || ((s[0] is '-' or '.' or '+') && s.Length > 1 && (char.IsDigit(s[1]) || s[1] == '.'));

    static void SetRange(ParamLine item, int codeStart, string code, string value)
    {
        value = value.Trim();
        item.Value = value;
        int end = codeStart + code.TrimEnd().Length;
        item.ValueEnd = end;
        item.ValueStart = end - value.Length;
    }

    // The part of a line that isn't a comment. ";)" at the start marks lines only RVGL reads
    static string CodeOf(string line, out bool ext, out int codeStart)
    {
        int i = 0;
        while (i < line.Length && line[i] is ' ' or '\t') i++;
        ext = false; codeStart = 0;
        if (i + 1 < line.Length && line[i] == ';' && line[i + 1] == ')') { ext = true; codeStart = i + 2; }
        bool q = false;
        for (int j = codeStart; j < line.Length; j++)
        {
            if (line[j] == '"') q = !q;
            if (line[j] == ';' && !q) return line[codeStart..j];
        }
        return line[codeStart..];
    }

    /// <summary>Write values that differ between two states of the car into the text</summary>
    public void Apply(IReadOnlyList<ParamEntry> before, IReadOnlyList<ParamEntry> now)
    {
        var was = new Dictionary<string, string>();
        bool fresh = lines.Count == 0;
        if (fresh)
        {
            lines.Add("{"); eols.Add("\r\n");
            lines.Add("}"); eols.Add("\r\n");
            Parse();
        }
        else foreach (var e in before) was[e.Block + "|" + e.Key] = e.Value;

        foreach (var e in now)
        {
            if (was.TryGetValue(e.Block + "|" + e.Key, out var old) && old == e.Value) continue;
            if (old == null && e.Value == "") continue; // optional value never set

            var it = Section(e.Block)?.Find(e.Key);
            if (it == null && e.Key == "Weapon") it = Section(e.Block)?.Find("WeaponOffset1");
            if (it != null)
            {
                var l = lines[it.Line];
                var v = e.Value;
                if (it.ValueEnd == it.ValueStart && it.ValueEnd > 0 && l[it.ValueEnd - 1] is not (' ' or '\t')) v = "\t" + v;
                lines[it.Line] = l[..it.ValueStart] + v + l[it.ValueEnd..];
            }
            else Insert(e);
            Parse();
        }
    }

    void Insert(ParamEntry e)
    {
        var sec = Section(e.Block);
        int at;
        if (sec == null)
        {
            at = mainClose >= 0 ? mainClose : lines.Count;
            lines.Insert(at, e.Block + " {"); eols.Insert(at, "\r\n");
            lines.Insert(at + 1, "}"); eols.Insert(at + 1, "\r\n");
            Parse();
            sec = Section(e.Block)!;
        }
        if (sec.Items.Count > 0) at = sec.Items[^1].Line + 1;
        else if (sec.CloseLine >= 0) at = sec.CloseLine;
        else at = mainClose >= 0 ? mainClose : lines.Count;

        if (at > 0 && at <= eols.Count && eols[at - 1] == "") eols[at - 1] = "\r\n";
        var key = e.Key;
        if (key.StartsWith("MODEL ")) key = "MODEL \t" + key[6..];
        if (key.Contains('#')) key = "";
        lines.Insert(at, (e.Ext ? ";)" : "") + key + "\t" + e.Value);
        eols.Insert(at, "\r\n");
    }
}

public sealed class ParamLine
{
    public string Key = "";
    public string Value = "";
    public int Line, ValueStart, ValueEnd;
    public bool Ext;
}

public sealed class ParamSection
{
    public readonly string Header;
    public readonly List<ParamLine> Items = new();
    public int OpenLine = -1, CloseLine = -1;
    public ParamSection(string h) => Header = h;
    public static readonly ParamSection Empty = new("");

    // The game reads top to bottom: when a key is there twice the last one counts
    public ParamLine? Find(string key)
    {
        for (int i = Items.Count - 1; i >= 0; i--)
            if (string.Equals(Items[i].Key, key, StringComparison.OrdinalIgnoreCase)) return Items[i];
        return null;
    }
    public bool Has(string key) => Find(key) != null;

    public string Str(string key, string def = "")
    {
        var p = Find(key);
        if (p == null) return def;
        var v = p.Value;
        if (v.StartsWith('"'))
        {
            int e = v.IndexOf('"', 1);
            v = e > 0 ? v[1..e] : v[1..];
        }
        return v;
    }
    public double Num(string key, double def = 0)
    {
        var p = Find(key);
        return p == null ? def : ToNumber(p.Value, def);
    }
    public int Int(string key, int def = 0)
    {
        var p = Find(key);
        return p == null ? def : (int)Math.Clamp(ToNumber(p.Value, def), int.MinValue, int.MaxValue);
    }
    public bool Bool(string key, bool def = false)
    {
        var p = Find(key);
        if (p == null) return def;
        return p.Value.Trim().ToUpperInvariant() switch
        {
            "TRUE" or "1" or "ON" or "YES" => true,
            "FALSE" or "0" or "OFF" or "NO" => false,
            _ => def
        };
    }
    public double[] Numbers(string key, int count)
    {
        var r = new double[count];
        var p = Find(key);
        if (p == null) return r;
        var parts = Regex.Split(p.Value.Trim(), "[\\s,]+");
        for (int i = 0; i < Math.Min(count, parts.Length); i++) r[i] = ToNumber(parts[i], 0);
        return r;
    }

    // Car makers write 1.2.3456, 1.0f and worse
    public static double ToNumber(string s, double def)
    {
        var sb = new StringBuilder();
        bool dot = false;
        foreach (var c in s.Trim())
        {
            if (char.IsDigit(c) || c is '-' or '+' or 'e' or 'E') sb.Append(c);
            else if (c == '.' && !dot) { sb.Append(c); dot = true; }
            else if (c is ' ' or '\t' && sb.Length > 0) break;
        }
        return double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d : def;
    }
}

public sealed record ParamEntry(string Block, string Key, string Value, bool Ext = false);
