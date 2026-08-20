using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Enums;
using TerraFluent.AutoAnalytics.Schema;

namespace TerraFluent.AutoAnalytics.Validation;

/// <summary>
/// Phase 2 — detects missing values, duplicate rows, duplicate keys, invalid formats and
/// out-of-range values, producing a severity-graded <see cref="ValidationReport"/>.
/// </summary>
public sealed class DataValidationEngine
{
    /// <summary>Runs all validation rules against the dataset and its discovered schema.</summary>
    public ValidationReport Validate(Dataset dataset, IReadOnlyList<ColumnProfile> profiles)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        if (profiles is null) throw new ArgumentNullException(nameof(profiles));

        var issues = new List<ValidationIssue>();
        CheckEmptyDataset(dataset, issues);
        CheckMissingValues(profiles, issues);
        CheckDuplicateRows(dataset, issues);
        CheckDuplicateKeys(dataset, profiles, issues);
        CheckInvalidFormats(dataset, profiles, issues);
        CheckOutOfRange(dataset, profiles, issues);
        return new ValidationReport(issues);
    }

    private static void CheckEmptyDataset(Dataset dataset, List<ValidationIssue> issues)
    {
        if (dataset.RowCount == 0)
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Error,
                Code = "EMPTY_DATASET",
                Message = "The dataset contains no data rows."
            });
        else if (dataset.RowCount < 3)
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Code = "LOW_ROW_COUNT",
                Message = $"Only {dataset.RowCount} row(s) present — statistical results will be unreliable.",
                AffectedCount = dataset.RowCount
            });
    }

    private static void CheckMissingValues(IReadOnlyList<ColumnProfile> profiles, List<ValidationIssue> issues)
    {
        foreach (var p in profiles)
        {
            if (p.MissingCount == 0 || p.TotalCount == 0) continue;
            double pct = (double)p.MissingCount / p.TotalCount;
            var severity = pct >= 0.5 ? ValidationSeverity.Error
                         : pct >= 0.1 ? ValidationSeverity.Warning
                         : ValidationSeverity.Info;
            issues.Add(new ValidationIssue
            {
                Severity = severity,
                Code = "MISSING_VALUES",
                Column = p.Name,
                Message = $"Column '{p.Name}' has {p.MissingCount} missing value(s) ({pct:P0}).",
                AffectedCount = p.MissingCount
            });
        }
    }

    private static void CheckDuplicateRows(Dataset dataset, List<ValidationIssue> issues)
    {
        if (dataset.RowCount == 0 || dataset.ColumnCount == 0) return;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int duplicates = 0;
        for (int r = 0; r < dataset.RowCount; r++)
        {
            var key = string.Join("\u001f", dataset.Columns.Select(c =>
                r < c.Values.Count ? c.Values[r]?.ToString() ?? string.Empty : string.Empty));
            if (!seen.Add(key)) duplicates++;
        }
        if (duplicates > 0)
            issues.Add(new ValidationIssue
            {
                Severity = duplicates > dataset.RowCount * 0.1 ? ValidationSeverity.Warning : ValidationSeverity.Info,
                Code = "DUPLICATE_ROWS",
                Message = $"{duplicates} duplicate row(s) detected.",
                AffectedCount = duplicates
            });
    }

    private static void CheckDuplicateKeys(Dataset dataset, IReadOnlyList<ColumnProfile> profiles, List<ValidationIssue> issues)
    {
        foreach (var p in profiles.Where(p => p.Role == SemanticRole.IdentifierRole || p.Type == ColumnType.Identifier))
        {
            var column = dataset.Columns[p.Index];
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int dups = 0;
            foreach (var v in column.Values)
            {
                if (ValueParsing.IsMissing(v)) continue;
                if (!seen.Add(v!.ToString()!)) dups++;
            }
            if (dups > 0)
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Error,
                    Code = "DUPLICATE_KEYS",
                    Column = p.Name,
                    Message = $"Identifier column '{p.Name}' has {dups} duplicate key(s).",
                    AffectedCount = dups
                });
        }
    }

    private static void CheckInvalidFormats(Dataset dataset, IReadOnlyList<ColumnProfile> profiles, List<ValidationIssue> issues)
    {
        foreach (var p in profiles)
        {
            var column = dataset.Columns[p.Index];
            int invalid = 0;
            foreach (var v in column.Values)
            {
                if (ValueParsing.IsMissing(v)) continue;
                bool ok = p.Type switch
                {
                    ColumnType.Numeric or ColumnType.Currency or ColumnType.Percentage => ValueParsing.TryToDouble(v, out _),
                    ColumnType.Date => ValueParsing.TryToDate(v, out _),
                    ColumnType.Boolean => ValueParsing.TryToBool(v, out _),
                    _ => true
                };
                if (!ok) invalid++;
            }
            if (invalid > 0)
                issues.Add(new ValidationIssue
                {
                    // A value that doesn't match the column's type is a genuine data error: at least a
                    // Warning, escalating to Error when it affects a large share of the column.
                    Severity = invalid > p.TotalCount * 0.1 ? ValidationSeverity.Error : ValidationSeverity.Warning,
                    Code = "INVALID_FORMAT",
                    Column = p.Name,
                    Message = $"Column '{p.Name}' has {invalid} value(s) that do not match its inferred type ({p.Type}).",
                    AffectedCount = invalid
                });
        }
    }

    private static void CheckOutOfRange(Dataset dataset, IReadOnlyList<ColumnProfile> profiles, List<ValidationIssue> issues)
    {
        foreach (var p in profiles)
        {
            var column = dataset.Columns[p.Index];
            var name = p.Name.ToLowerInvariant();
            int negativeRevenue = 0, badPercent = 0, badAge = 0;

            foreach (var v in column.Values)
            {
                if (ValueParsing.IsMissing(v) || !ValueParsing.TryToDouble(v, out var d)) continue;

                if (p.Type == ColumnType.Percentage && (d < 0 || d > 100)) badPercent++;
                if (p.Role is SemanticRole.RevenueMetric && d < 0) negativeRevenue++;
                if (name.Contains("age") && (d < 0 || d > 130)) badAge++;
            }

            if (badPercent > 0)
                issues.Add(Range("PERCENT_OUT_OF_RANGE", p.Name, $"{badPercent} percentage value(s) fall outside 0–100.", badPercent));
            if (negativeRevenue > 0)
                issues.Add(Range("NEGATIVE_REVENUE", p.Name, $"{negativeRevenue} negative value(s) in revenue-like column '{p.Name}'.", negativeRevenue));
            if (badAge > 0)
                issues.Add(Range("AGE_OUT_OF_RANGE", p.Name, $"{badAge} age value(s) outside 0–130.", badAge));
        }
    }

    private static ValidationIssue Range(string code, string column, string message, int count) => new()
    {
        Severity = ValidationSeverity.Warning,
        Code = code,
        Column = column,
        Message = message,
        AffectedCount = count
    };
}
