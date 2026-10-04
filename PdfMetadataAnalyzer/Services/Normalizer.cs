using System.Text.RegularExpressions;

namespace PdfMetadataAnalyzer.Services;

public static class Normalizer
{
    private static readonly Regex NonPrintable =
        new(@"[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]", RegexOptions.Compiled);

    private static readonly Regex MultiSpace =
        new(@"\s{2,}", RegexOptions.Compiled);

    private static readonly Regex PdfDate = new(
        @"D:(\d{4})(\d{2})(\d{2})(\d{2})?(\d{2})?(\d{2})?([+\-Z])?(\d{2})?'?(\d{2})?",
        RegexOptions.Compiled);

    public static string? Clean(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;

        var s = raw.TrimStart('\uFEFF', '\u200B');
        s = NonPrintable.Replace(s, " ");
        s = s.Replace("\r\n", " ").Replace('\r', ' ').Replace('\n', ' ');
        s = MultiSpace.Replace(s, " ").Trim();

        if (s.StartsWith('(') && s.EndsWith(')'))
            s = s[1..^1].Trim();

        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    public static string? Keywords(string? raw)
    {
        var c = Clean(raw);
        if (c == null) return null;

        var parts = c.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Trim().ToLowerInvariant())
                     .Where(p => p.Length > 0)
                     .Distinct()
                     .OrderBy(p => p);

        var result = string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(result) ? null : result;
    }

    public static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var s = raw.Trim();

        var m = PdfDate.Match(s);
        if (m.Success)
        {
            try
            {
                int y = int.Parse(m.Groups[1].Value);
                int mo = G(m.Groups[2], 1);
                int d = G(m.Groups[3], 1);
                int h = G(m.Groups[4], 0);
                int mi = G(m.Groups[5], 0);
                int se = G(m.Groups[6], 0);

                var dt = new DateTime(
                    y,
                    Math.Clamp(mo, 1, 12),
                    Math.Clamp(d, 1, 28),
                    Math.Clamp(h, 0, 23),
                    Math.Clamp(mi, 0, 59),
                    Math.Clamp(se, 0, 59));

                if (m.Groups[7].Value == "Z")
                    return DateTime.SpecifyKind(dt, DateTimeKind.Utc);

                if (m.Groups[7].Success && m.Groups[8].Success)
                {
                    int tzh = G(m.Groups[8], 0);
                    int tzm = G(m.Groups[9], 0);
                    int sign = m.Groups[7].Value == "-" ? -1 : 1;
                    return new DateTimeOffset(dt, new TimeSpan(tzh, tzm, 0) * sign).UtcDateTime;
                }
                return dt;
            }
            catch { }
        }

        if (Regex.IsMatch(s, @"^\d{14}$") &&
            DateTime.TryParseExact(s, "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var d2))
            return d2;

        if (DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dg))
            return dg;

        return null;
    }

    public static double ValueScore(string? v) =>
        v == null ? 0 : v.Length < 2 ? 0.1 : v.Length < 5 ? 0.5 : 1.0;

    private static int G(Group g, int fallback) =>
        g.Success && int.TryParse(g.Value, out int v) ? v : fallback;
}