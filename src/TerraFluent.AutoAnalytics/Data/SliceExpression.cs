using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TerraFluent.AutoAnalytics.Data;

/// <summary>The comparison operator of a single slice condition.</summary>
public enum SliceOperator
{
    Equal,
    NotEqual,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual,
    Contains
}

/// <summary>One <c>column op value</c> condition within a <see cref="SliceExpression"/>.</summary>
public sealed class SliceCondition
{
    public string Column { get; init; } = string.Empty;
    public SliceOperator Operator { get; init; }
    public string Value { get; init; } = string.Empty;

    /// <summary>Evaluates the condition against a raw cell value.</summary>
    public bool Matches(object? cell)
    {
        // Numeric comparison when both sides parse as numbers; otherwise string comparison.
        bool cellNum = ValueParsing.TryToDouble(cell, out double cv);
        bool valNum = double.TryParse(Value, NumberStyles.Any, CultureInfo.InvariantCulture, out double tv);

        if (cellNum && valNum)
        {
            return Operator switch
            {
                SliceOperator.Equal              => cv == tv,
                SliceOperator.NotEqual           => cv != tv,
                SliceOperator.GreaterThan        => cv > tv,
                SliceOperator.GreaterThanOrEqual => cv >= tv,
                SliceOperator.LessThan           => cv < tv,
                SliceOperator.LessThanOrEqual    => cv <= tv,
                SliceOperator.Contains           => cv.ToString(CultureInfo.InvariantCulture).Contains(Value, StringComparison.OrdinalIgnoreCase),
                _                                => false
            };
        }

        string s = (cell as string ?? cell?.ToString() ?? string.Empty).Trim();
        return Operator switch
        {
            SliceOperator.Equal    => string.Equals(s, Value, StringComparison.OrdinalIgnoreCase),
            SliceOperator.NotEqual => !string.Equals(s, Value, StringComparison.OrdinalIgnoreCase),
            SliceOperator.Contains => s.Contains(Value, StringComparison.OrdinalIgnoreCase),
            // Ordinal string comparison for the relational operators on non-numeric data.
            SliceOperator.GreaterThan        => string.Compare(s, Value, StringComparison.OrdinalIgnoreCase) > 0,
            SliceOperator.GreaterThanOrEqual => string.Compare(s, Value, StringComparison.OrdinalIgnoreCase) >= 0,
            SliceOperator.LessThan           => string.Compare(s, Value, StringComparison.OrdinalIgnoreCase) < 0,
            SliceOperator.LessThanOrEqual    => string.Compare(s, Value, StringComparison.OrdinalIgnoreCase) <= 0,
            _                                => false
        };
    }
}

/// <summary>
/// A deterministic, AND-combined set of row conditions parsed from a compact expression such as
/// <c>region = EU and revenue &gt; 10000</c>. No AI: purely token-based. Conditions are combined
/// with logical AND (all must match).
/// </summary>
public sealed class SliceExpression
{
    /// <summary>The parsed conditions; all must match for a row to be included.</summary>
    public IReadOnlyList<SliceCondition> Conditions { get; }

    /// <summary>The original expression text.</summary>
    public string RawText { get; }

    private SliceExpression(IReadOnlyList<SliceCondition> conditions, string rawText)
    {
        Conditions = conditions;
        RawText = rawText;
    }

    public bool IsEmpty => Conditions.Count == 0;

    // Longer operators first so ">=" is matched before ">".
    private static readonly (string Token, SliceOperator Op)[] Operators =
    {
        (">=", SliceOperator.GreaterThanOrEqual),
        ("<=", SliceOperator.LessThanOrEqual),
        ("!=", SliceOperator.NotEqual),
        ("<>", SliceOperator.NotEqual),
        ("==", SliceOperator.Equal),
        ("~",  SliceOperator.Contains),
        (">",  SliceOperator.GreaterThan),
        ("<",  SliceOperator.LessThan),
        ("=",  SliceOperator.Equal)
    };

    /// <summary>
    /// Parses an expression into conditions. Unrecognised clauses are skipped. Returns an empty
    /// expression when <paramref name="text"/> is null/blank so callers can no-op safely.
    /// </summary>
    public static SliceExpression Parse(string? text)
    {
        var conditions = new List<SliceCondition>();
        if (string.IsNullOrWhiteSpace(text))
            return new SliceExpression(conditions, text ?? string.Empty);

        // Split on " and " / "," / ";" — all treated as logical AND.
        var clauses = text.Split(new[] { " and ", " AND ", ",", ";" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var clause in clauses)
        {
            var condition = ParseClause(clause);
            if (condition is not null) conditions.Add(condition);
        }

        return new SliceExpression(conditions, text);
    }

    private static SliceCondition? ParseClause(string clause)
    {
        foreach (var (token, op) in Operators)
        {
            int idx = clause.IndexOf(token, StringComparison.Ordinal);
            if (idx <= 0) continue;

            string column = clause.Substring(0, idx).Trim();
            string value = clause.Substring(idx + token.Length).Trim().Trim('\'', '"');
            if (column.Length == 0 || value.Length == 0) return null;

            return new SliceCondition { Column = column, Operator = op, Value = value };
        }
        return null;
    }

    /// <summary>Returns a new dataset containing only the rows that satisfy every condition.</summary>
    public Dataset Apply(Dataset dataset)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        if (IsEmpty) return dataset;

        var columnNames = dataset.Columns.Select(c => c.Name).ToList();

        // Resolve each condition to a concrete column index (case-insensitive); drop unknown columns.
        var resolved = Conditions
            .Select(c => (Condition: c,
                          Index: dataset.Columns
                              .Select((col, i) => (col, i))
                              .Where(x => string.Equals(x.col.Name, c.Column, StringComparison.OrdinalIgnoreCase))
                              .Select(x => (int?)x.i)
                              .FirstOrDefault()))
            .Where(x => x.Index.HasValue)
            .Select(x => (x.Condition, Index: x.Index!.Value))
            .ToList();

        if (resolved.Count == 0) return dataset;

        var keptRows = new List<object?[]>();
        for (int r = 0; r < dataset.RowCount; r++)
        {
            bool match = resolved.All(rc => rc.Condition.Matches(
                r < dataset.Columns[rc.Index].Values.Count ? dataset.Columns[rc.Index].Values[r] : null));
            if (!match) continue;

            var row = new object?[columnNames.Count];
            for (int c = 0; c < columnNames.Count; c++)
                row[c] = r < dataset.Columns[c].Values.Count ? dataset.Columns[c].Values[r] : null;
            keptRows.Add(row);
        }

        return Dataset.FromRows(dataset.Name, columnNames, keptRows);
    }
}
