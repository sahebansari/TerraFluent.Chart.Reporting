using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Data;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Schema;

/// <summary>
/// Phase 1 — classifies every column of a <see cref="Dataset"/> into a <see cref="ColumnProfile"/>
/// by sampling cell values and voting on the most likely <see cref="ColumnType"/>, then delegating
/// to <see cref="SemanticInferenceEngine"/> for the business role.
/// </summary>
public sealed class SchemaDiscoveryEngine
{
    private readonly SemanticInferenceEngine _semantics;

    public SchemaDiscoveryEngine(SemanticInferenceEngine? semantics = null)
        => _semantics = semantics ?? new SemanticInferenceEngine();

    /// <summary>Produces one <see cref="ColumnProfile"/> per column.</summary>
    public IReadOnlyList<ColumnProfile> Discover(Dataset dataset)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));
        var profiles = new List<ColumnProfile>(dataset.ColumnCount);
        foreach (var column in dataset.Columns)
            profiles.Add(Classify(column, dataset.RowCount));
        return profiles;
    }

    private ColumnProfile Classify(DataColumn column, int rowCount)
    {
        int missing = 0, considered = 0;
        int numeric = 0, date = 0, boolean = 0, currency = 0, percentage = 0, integer = 0;
        var distinct = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in column.Values)
        {
            if (ValueParsing.IsMissing(raw)) { missing++; continue; }
            considered++;
            distinct.Add(raw!.ToString()!);

            if (ValueParsing.LooksLikePercentage(raw)) percentage++;
            if (ValueParsing.LooksLikeCurrency(raw)) currency++;

            if (ValueParsing.TryToBool(raw, out _)) boolean++;
            if (ValueParsing.TryToDate(raw, out _)) date++;
            if (ValueParsing.TryToDouble(raw, out var d))
            {
                numeric++;
                if (Math.Abs(d - Math.Round(d)) < 1e-9) integer++;
            }
        }

        int distinctCount = distinct.Count;
        double uniqueness  = considered == 0 ? 0 : (double)distinctCount / considered;
        var (type, confidence) = DecideType(column.Name, considered, numeric, integer, date, boolean,
                                             currency, percentage, distinctCount, uniqueness);

        var role = _semantics.Infer(column.Name, type, uniqueness);

        return new ColumnProfile
        {
            Index          = column.Index,
            Name           = column.Name,
            Type           = type,
            Role           = role,
            DistinctCount  = distinctCount,
            MissingCount   = missing,
            TotalCount     = rowCount,
            TypeConfidence = confidence
        };
    }

    private static (ColumnType type, double confidence) DecideType(
        string name, int considered, int numeric, int integer, int date, int boolean,
        int currency, int percentage, int distinctCount, double uniqueness)
    {
        if (considered == 0) return (ColumnType.Text, 0);

        double numericRatio = (double)numeric / considered;
        double dateRatio     = (double)date / considered;
        double boolRatio     = (double)boolean / considered;

        // Boolean: two-valued and every cell parses as bool.
        if (boolRatio >= 0.95 && distinctCount <= 2)
            return (ColumnType.Boolean, boolRatio);

        // Date beats numeric only when clearly date-like (avoids treating years as dates unless named).
        if (dateRatio >= 0.9 && numericRatio < 0.9)
            return (ColumnType.Date, dateRatio);

        if (numericRatio >= 0.9)
        {
            if (percentage >= considered * 0.6) return (ColumnType.Percentage, numericRatio);
            if (currency   >= considered * 0.6) return (ColumnType.Currency,   numericRatio);

            // Integer + id-ish name => identifier, not a measure (duplicates lower uniqueness but
            // an "…Id" column is still a key, which lets duplicate-key validation fire).
            bool integerHeavy = integer >= considered * 0.98;
            if (integerHeavy && IsIdName(name))
                return (ColumnType.Identifier, numericRatio);

            return (ColumnType.Numeric, numericRatio);
        }

        // Low-cardinality text => category; high-cardinality text => free text or identifier.
        if (uniqueness >= 0.98 && IsIdName(name))
            return (ColumnType.Identifier, 1);
        if (distinctCount <= Math.Max(20, considered * 0.2))
            return (ColumnType.Category, 1 - numericRatio);

        return (ColumnType.Text, 1 - numericRatio);
    }

    private static bool IsIdName(string name)
    {
        var n = (name ?? string.Empty).ToLowerInvariant();
        return n.EndsWith("id") || n == "id" || n.Contains("key") || n.Contains("code")
            || n.Contains("guid") || n.Contains("uuid") || n.Contains("sku");
    }
}
