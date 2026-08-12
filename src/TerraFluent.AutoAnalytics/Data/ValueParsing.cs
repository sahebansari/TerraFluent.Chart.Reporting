using System;
using System.Globalization;

namespace TerraFluent.AutoAnalytics.Data;

/// <summary>
/// Central, culture-invariant coercion helpers shared by the schema, validation and profiling
/// engines. Kept deterministic: the same input always yields the same parse result.
/// </summary>
public static class ValueParsing
{
    private static readonly char[] CurrencySymbols = { '$', '€', '£', '¥', '₹', '₽' };

    /// <summary>True when the value is <see langword="null"/>, empty, whitespace, or a NaN token.</summary>
    public static bool IsMissing(object? value)
    {
        if (value is null) return true;
        if (value is double d) return double.IsNaN(d);
        if (value is float f) return float.IsNaN(f);
        var s = value as string ?? value.ToString();
        if (string.IsNullOrWhiteSpace(s)) return true;
        s = s.Trim();
        return s.Equals("nan", StringComparison.OrdinalIgnoreCase)
            || s.Equals("null", StringComparison.OrdinalIgnoreCase)
            || s.Equals("n/a", StringComparison.OrdinalIgnoreCase)
            || s == "-";
    }

    /// <summary>Attempts to coerce a cell to a <see cref="double"/>, stripping currency/percent symbols and thousands separators.</summary>
    public static bool TryToDouble(object? value, out double result)
    {
        result = 0;
        if (value is null) return false;
        switch (value)
        {
            case double d: result = d; return !double.IsNaN(d);
            case float f: result = f; return !float.IsNaN(f);
            case decimal m: result = (double)m; return true;
            case int i: result = i; return true;
            case long l: result = l; return true;
            case short sh: result = sh; return true;
            case byte b: result = b; return true;
            case bool bo: result = bo ? 1 : 0; return true;
        }

        var s = (value as string ?? value.ToString() ?? string.Empty).Trim();
        if (s.Length == 0) return false;

        bool percent = s.EndsWith("%", StringComparison.Ordinal);
        if (percent) s = s.Substring(0, s.Length - 1).Trim();

        foreach (var sym in CurrencySymbols) s = s.Replace(sym.ToString(), string.Empty);
        s = s.Replace(",", string.Empty).Replace("(", "-").Replace(")", string.Empty).Trim();

        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
        {
            result = percent ? parsed : parsed;
            return true;
        }
        return false;
    }

    /// <summary>Attempts to parse a cell as a <see cref="DateTime"/> using invariant then common formats.</summary>
    public static bool TryToDate(object? value, out DateTime result)
    {
        result = default;
        if (value is DateTime dt) { result = dt; return true; }
        if (value is DateTimeOffset dto) { result = dto.DateTime; return true; }

        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s)) return false;

        // Reject pure integers/decimals so numeric ids aren't misread as dates.
        if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out _) && !s.Contains('-') && !s.Contains('/'))
            return false;

        return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
            || DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.None, out result);
    }

    /// <summary>Attempts to parse a cell as a boolean (true/false, yes/no, y/n, 1/0).</summary>
    public static bool TryToBool(object? value, out bool result)
    {
        result = false;
        if (value is bool b) { result = b; return true; }
        var s = (value as string ?? value?.ToString())?.Trim().ToLowerInvariant();
        switch (s)
        {
            case "true": case "yes": case "y": case "1": result = true;  return true;
            case "false": case "no": case "n": case "0": result = false; return true;
            default: return false;
        }
    }

    /// <summary>True when the trimmed string ends with a percent sign.</summary>
    public static bool LooksLikePercentage(object? value)
    {
        var s = (value as string)?.Trim();
        return s is { Length: > 0 } && s.EndsWith("%", StringComparison.Ordinal);
    }

    /// <summary>True when the trimmed string starts with a recognised currency symbol.</summary>
    public static bool LooksLikeCurrency(object? value)
    {
        var s = (value as string)?.Trim();
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var sym in CurrencySymbols)
            if (s[0] == sym || s.StartsWith(sym + " ", StringComparison.Ordinal)) return true;
        return false;
    }
}
