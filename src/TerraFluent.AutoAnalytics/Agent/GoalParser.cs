using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TerraFluent.AutoAnalytics.Profiling;

namespace TerraFluent.AutoAnalytics.Agent;

/// <summary>
/// A deterministic, keyword-driven parser that maps a free-text question to an <see cref="AnalyticGoal"/>.
/// No AI: intent is inferred from keyword matches and any dataset column names mentioned in the text.
/// </summary>
public static class GoalParser
{
    private static readonly (GoalKind Kind, string[] Keywords)[] IntentMap =
    {
        (GoalKind.Forecast,    new[] { "forecast", "project", "predict", "future", "next month", "next quarter", "next year", "will be", "expected", "outlook" }),
        (GoalKind.Compare,     new[] { "compare", "versus", " vs ", "month over month", "quarter over quarter", "year over year", "period over period", "previous period", "last month", "last quarter", "last year", "since last" }),
        (GoalKind.RootCause,   new[] { "why", "root cause", "root-cause", "caused", "cause of", "reason", "driven by", "attribut", "contribut", "explain the" }),
        (GoalKind.Segment,     new[] { "segment", "cluster", "grouping", "persona", "natural groups", "natural group", "cohort" }),
        (GoalKind.Trend,       new[] { "trend", "growth", "grow", "rising", "declin", "increase", "decrease", "over time", "trajectory" }),
        (GoalKind.Correlation, new[] { "correlat", "relationship", "related", "driver", "drive", "influence", "depend", "move together" }),
        (GoalKind.Anomaly,     new[] { "anomal", "outlier", "spike", "unusual", "abnormal", "irregular", "unexpected" }),
        (GoalKind.Dominance,   new[] { "top", "leading", "leader", "dominant", "concentrat", "share", "breakdown", "biggest", "largest", "highest", "most", "greatest", "rank", "which " })
    };

    /// <summary>Parses a question into a goal, resolving any column names mentioned against the profile.</summary>
    public static AnalyticGoal Parse(string? question, DatasetProfile profile)
    {
        if (string.IsNullOrWhiteSpace(question))
            return AnalyticGoal.Explore();

        string text = question.ToLowerInvariant();
        var columns = ResolveColumns(text, profile);

        foreach (var (kind, keywords) in IntentMap)
        {
            if (keywords.Any(k => text.Contains(k, StringComparison.Ordinal)))
            {
                return new AnalyticGoal
                {
                    Kind = kind,
                    TargetColumns = columns,
                    RawText = question
                };
            }
        }

        return AnalyticGoal.Explore(question);
    }

    private static IReadOnlyList<string> ResolveColumns(string text, DatasetProfile profile)
    {
        var questionTokens = Tokenize(text).Select(Normalize).ToHashSet(StringComparer.Ordinal);
        var matched = new List<string>();

        foreach (var col in profile.Columns)
        {
            string name = col.Name;
            if (string.IsNullOrEmpty(name)) continue;
            string lower = name.ToLowerInvariant();
            var tokens = Tokenize(lower).ToList();

            // 1) Exact name reference: whole word for single-token names (so "age" does NOT match
            // "manager"); substring for distinctive multi-word names (e.g. "gross margin").
            bool direct = tokens.Count > 1
                ? text.Contains(lower, StringComparison.Ordinal)
                : questionTokens.Contains(Normalize(lower));
            if (direct) { matched.Add(name); continue; }

            // 2) A significant word from the column name appears in the question.
            var significant = tokens
                .Where(t => t.Length >= 3 && Array.IndexOf(StopTokens, t) < 0)
                .Select(Normalize)
                .ToList();
            if (significant.Any(questionTokens.Contains)) { matched.Add(name); continue; }

            // 3) A synonym bridge: a column word and a question word share a synonym group
            // (e.g. "salary" ↔ "pay", "years_of_experience" ↔ "experience"/"tenure").
            if (SynonymGroups.Any(g =>
                    significant.Any(g.Contains) && questionTokens.Any(g.Contains)))
                matched.Add(name);
        }
        return matched;
    }

    // Splits text into lower-case word tokens, breaking on camelCase and non-alphanumerics.
    private static IEnumerable<string> Tokenize(string text)
    {
        var spaced = Regex.Replace(text, "([a-z0-9])([A-Z])", "$1 $2");
        foreach (var token in Regex.Split(spaced.ToLowerInvariant(), "[^a-z0-9]+"))
            if (token.Length > 0) yield return token;
    }

    // Light singularization so "salaries"→"salary", "roles"→"role", "pays"→"pay" all unify.
    private static string Normalize(string word)
    {
        if (word.Length > 4 && word.EndsWith("ies", StringComparison.Ordinal))
            return word[..^3] + "y";
        if (word.Length > 3 && word.EndsWith("s", StringComparison.Ordinal))
            return word[..^1];
        return word;
    }

    private static readonly string[] StopTokens =
        { "the", "per", "and", "num", "for", "with", "avg" };

    // Interchangeable business terms (stored singular). If a question word and a column word fall in
    // the same group, the column is treated as referenced by the question.
    private static readonly HashSet<string>[] SynonymGroups =
        new[]
        {
            new[] { "salary", "pay", "compensation", "wage", "income", "earning", "remuneration", "payroll" },
            new[] { "experience", "tenure", "seniority" },
            new[] { "revenue", "sale", "turnover", "gmv" },
            new[] { "cost", "expense", "spend", "spending", "cog", "overhead" },
            new[] { "profit", "margin" },
            new[] { "quantity", "unit", "count", "volume", "order" },
            new[] { "headcount", "employee", "staff", "workforce", "people", "worker" },
            new[] { "department", "dept", "division", "team" },
            new[] { "role", "position", "title", "designation", "job" },
            new[] { "region", "area", "territory", "geography", "location", "country" },
            new[] { "product", "item", "sku" },
            new[] { "customer", "client", "account" },
        }
        .Select(g => g.Select(Normalize).ToHashSet(StringComparer.Ordinal))
        .ToArray();
}
