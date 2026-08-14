using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace TerraFluent.AutoAnalytics;

/// <summary>
/// Converts raw data-field identifiers (e.g. <c>years_of_experience</c>, <c>salaryUSD</c>) into
/// human-friendly display text (e.g. <c>Years of Experience</c>, <c>Salary USD</c>) for use in
/// chart titles, axis/legend labels and KPI captions. Never use for lookups or computation —
/// only for presentation.
/// </summary>
public static class DisplayText
{
    // Minor words stay lowercase in the middle of a title (but are capitalised if first/last).
    private static readonly HashSet<string> MinorWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "of", "and", "or", "the", "in", "on", "at", "to", "by", "for",
        "vs", "a", "an", "with", "per", "from", "as", "nor",
    };

    // Common short tokens that read better fully upper-cased.
    private static readonly HashSet<string> Acronyms = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "hr", "it", "kpi", "url", "api", "sku", "roi", "usd", "eur", "gbp",
        "ceo", "cto", "cfo", "yoy", "ytd", "qtd", "mtd", "eps", "arr", "mrr",
    };

    /// <summary>Humanises a raw field name into title-cased display text.</summary>
    public static string Humanize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

        var words = SplitWords(raw);
        if (words.Count == 0) return raw.Trim();

        var sb = new StringBuilder();
        for (int i = 0; i < words.Count; i++)
        {
            if (i > 0) sb.Append(' ');
            sb.Append(FormatWord(words[i], isFirst: i == 0, isLast: i == words.Count - 1));
        }
        return sb.ToString();
    }

    // Splits on separators (_ - whitespace) and camelCase / letter-digit boundaries.
    private static List<string> SplitWords(string s)
    {
        var words = new List<string>();
        var cur = new StringBuilder();
        char prev = '\0';

        foreach (char ch in s)
        {
            if (ch == '_' || ch == '-' || char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0) { words.Add(cur.ToString()); cur.Clear(); }
                prev = ch;
                continue;
            }

            if (cur.Length > 0)
            {
                bool boundary =
                    (char.IsUpper(ch) && char.IsLower(prev)) ||   // camelCase: aB
                    (char.IsDigit(ch) && !char.IsDigit(prev)) ||  // letter → digit
                    (!char.IsDigit(ch) && char.IsDigit(prev));    // digit → letter
                if (boundary) { words.Add(cur.ToString()); cur.Clear(); }
            }

            cur.Append(ch);
            prev = ch;
        }
        if (cur.Length > 0) words.Add(cur.ToString());
        return words;
    }

    private static string FormatWord(string w, bool isFirst, bool isLast)
    {
        // Preserve source acronyms already written in caps (e.g. "USD", "EBITDA").
        bool sourceAllCaps = w.Length >= 2 && w.Any(char.IsLetter)
                             && w.All(ch => !char.IsLetter(ch) || char.IsUpper(ch));
        if (sourceAllCaps) return w;

        if (Acronyms.Contains(w)) return w.ToUpperInvariant();

        if (!isFirst && !isLast && MinorWords.Contains(w)) return w.ToLowerInvariant();

        if (w.Length == 1) return w.ToUpperInvariant();
        return char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
    }

    /// <summary>
    /// Rounds a computed figure to a sensible, magnitude-aware precision for display so charts and
    /// narratives never show noisy fractions (e.g. an average of 40.7333 becomes 40.7, 405.72 → 406).
    /// </summary>
    public static double Round(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return value;
        double abs = Math.Abs(value);
        int decimals = abs >= 100 ? 0 : abs >= 10 ? 1 : abs >= 1 ? 2 : 3;
        return Math.Round(value, decimals, MidpointRounding.AwayFromZero);
    }

    /// <summary>Rounds (see <see cref="Round"/>) then formats with thousands separators for prose.</summary>
    public static string FormatNumber(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value)) return value.ToString(CultureInfo.InvariantCulture);
        double abs = Math.Abs(value);
        string fmt = abs >= 100 ? "#,##0" : abs >= 10 ? "#,##0.#" : abs >= 1 ? "#,##0.##" : "0.###";
        return Round(value).ToString(fmt, CultureInfo.InvariantCulture);
    }
}
