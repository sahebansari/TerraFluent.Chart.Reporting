using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Schema;

/// <summary>
/// Deterministic rules deciding whether a numeric measure is <b>additive</b> — its values can be
/// meaningfully summed (revenue, cost, quantity, currency amounts) — or <b>non-additive</b>: a
/// per-row attribute such as age, tenure, a rating, a temperature or an index, where a grand total
/// is nonsensical and an average is the correct default aggregation.
/// <para>Domain-agnostic: the decision combines the column's semantic role and type, a cross-domain
/// vocabulary of attribute/measurement terms, and a structural signal (values bounded in [0,1] are
/// treated as ratios/probabilities). It is not specific to any dataset.</para>
/// </summary>
public static class MeasureSemantics
{
    /// <summary>True when the measure's values can be meaningfully summed.</summary>
    /// <param name="profile">The column's inferred schema.</param>
    /// <param name="min">Optional observed minimum, used to detect ratio-like [0,1] columns.</param>
    /// <param name="max">Optional observed maximum, used to detect ratio-like [0,1] columns.</param>
    public static bool IsAdditive(ColumnProfile? profile, double? min = null, double? max = null)
    {
        if (profile is null) return false;

        // Percentages, ratios and rates are averaged, never summed.
        if (profile.Type == ColumnType.Percentage) return false;

        // Structural signal (name-independent): values confined to [0,1] are ratios / proportions /
        // probabilities — averaging is meaningful, summing is not.
        if (min is >= 0 && max is > 0 and <= 1) return false;

        // A recognized per-entity attribute name vetoes summing regardless of role.
        if (HasNonAdditiveToken(profile.Name)) return false;

        // Otherwise, only sum what we positively recognize as summable: money, or a
        // quantity/revenue/cost/profit measure.
        return profile.Type == ColumnType.Currency
            || profile.Role is SemanticRole.RevenueMetric or SemanticRole.CostMetric
                or SemanticRole.ProfitMetric or SemanticRole.QuantityMetric;
    }

    /// <summary>Default aggregation for a measure: sum when additive, otherwise average.</summary>
    public static AggregationKind DefaultAggregation(ColumnProfile? profile) =>
        IsAdditive(profile) ? AggregationKind.Sum : AggregationKind.Average;

    // Whole-word attribute tokens (matched after light singularization).
    private static readonly HashSet<string> NonAdditiveExact = new(StringComparer.Ordinal)
    {
        "age", "rate", "year", "mean", "median", "avg", "average", "level", "grade", "rank",
        "bmi", "iq", "gpa", "ph", "lat", "lng", "lon", "min", "max", "std", "iqr",
        "tenure", "seniority", "dob", "temp", "angle", "coordinate", "percentile", "quantile"
    };

    // Attribute/measurement stems (matched as a token prefix, e.g. "experien" → "experience").
    private static readonly string[] NonAdditiveStems =
    {
        "experien", "rating", "score", "ratio", "percent", "temperat", "humidit", "height",
        "weight", "latitude", "longitude", "senior", "index", "price", "altitud", "elevat",
        "depth", "pressur", "densit", "voltag", "frequenc", "velocit", "probabilit", "likelihood",
        "coordinat", "speed", "pace", "accelerat"
    };

    private static bool HasNonAdditiveToken(string name)
    {
        foreach (var token in Tokenize(name))
        {
            string t = Singularize(token);
            if (NonAdditiveExact.Contains(t)) return true;
            foreach (var stem in NonAdditiveStems)
                if (t.StartsWith(stem, StringComparison.Ordinal)) return true;
        }
        return false;
    }

    // Splits a column name into lower-case word tokens, breaking on camelCase and non-alphanumerics.
    private static IEnumerable<string> Tokenize(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) yield break;
        var spaced = Regex.Replace(name, "([a-z0-9])([A-Z])", "$1 $2");
        foreach (var token in Regex.Split(spaced, "[^A-Za-z0-9]+"))
            if (token.Length > 0) yield return token.ToLowerInvariant();
    }

    // Light singularization so "salaries"→"salary", "years"→"year", "ratings"→"rating" unify.
    private static string Singularize(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal))
            return word[..^3] + "y";
        if (word.Length > 3 && word.EndsWith("s", StringComparison.Ordinal))
            return word[..^1];
        return word;
    }
}
