using System;
using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Enums;

namespace TerraFluent.AutoAnalytics.Schema;

/// <summary>
/// Rule-based semantic inference: maps a column name (and its discovered type) to a business
/// <see cref="SemanticRole"/> using deterministic keyword patterns. Fully explainable — the
/// matched keyword is what drove the decision.
/// </summary>
public sealed class SemanticInferenceEngine
{
    // Ordered by specificity; first hit wins. Keyword matched as a case-insensitive substring.
    private static readonly (SemanticRole Role, string[] Keywords)[] Rules =
    {
        (SemanticRole.ProfitMetric,      new[] { "profit", "margin", "gross", "net income", "ebitda" }),
        (SemanticRole.CostMetric,        new[] { "cost", "expense", "spend", "cogs", "overhead" }),
        (SemanticRole.RevenueMetric,     new[] { "revenue", "sales", "income", "turnover", "gmv", "amount", "price" }),
        (SemanticRole.GeographyDimension,new[] { "country", "state", "province", "city", "region", "territory", "continent", "location", "zip", "postal" }),
        (SemanticRole.DateDimension,     new[] { "date", "time", "year", "month", "quarter", "week", "day", "period", "timestamp" }),
        (SemanticRole.QuantityMetric,    new[] { "quantity", "qty", "units", "count", "volume", "number", "orders", "visitors", "sessions" }),
        (SemanticRole.CategoryDimension, new[] { "product", "customer", "category", "segment", "channel", "type", "group", "department", "brand", "status", "name" }),
        (SemanticRole.IdentifierRole,    new[] { "id", "guid", "uuid", "key", "code", "sku", "reference" }),
    };

    /// <summary>Infers the business role for a column from its name and discovered type.</summary>
    public SemanticRole Infer(string columnName, ColumnType type, double uniqueness)
    {
        var name = (columnName ?? string.Empty).Trim().ToLowerInvariant();

        // Type-driven certainties first.
        if (type == ColumnType.Identifier || (type != ColumnType.Date && uniqueness >= 0.98 && MatchesAny(name, "id", "key", "code", "guid", "uuid")))
            return SemanticRole.IdentifierRole;
        if (type == ColumnType.Date)
            return SemanticRole.DateDimension;

        foreach (var (role, keywords) in Rules)
        {
            if (MatchesAny(name, keywords))
            {
                // Don't tag a text/category column as a numeric metric.
                bool wantsMeasure = role is SemanticRole.RevenueMetric or SemanticRole.CostMetric
                    or SemanticRole.ProfitMetric or SemanticRole.QuantityMetric;
                bool isMeasureType = type is ColumnType.Numeric or ColumnType.Currency or ColumnType.Percentage;
                if (wantsMeasure && !isMeasureType) continue;
                return role;
            }
        }

        if (type is ColumnType.Numeric or ColumnType.Currency or ColumnType.Percentage)
            return SemanticRole.QuantityMetric;
        if (type is ColumnType.Category or ColumnType.Boolean)
            return SemanticRole.CategoryDimension;

        return SemanticRole.Unknown;
    }

    private static bool MatchesAny(string name, params string[] keywords)
    {
        foreach (var k in keywords)
            if (name.IndexOf(k, StringComparison.Ordinal) >= 0) return true;
        return false;
    }
}
