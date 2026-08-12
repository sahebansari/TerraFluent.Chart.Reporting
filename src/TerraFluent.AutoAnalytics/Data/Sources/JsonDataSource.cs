using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace TerraFluent.AutoAnalytics.Data.Sources;

/// <summary>
/// Adapter for a JSON array of flat objects, e.g. <c>[{"a":1,"b":"x"}, {"a":2,"b":"y"}]</c>.
/// The union of all object keys (in first-seen order) defines the columns.
/// </summary>
public sealed class JsonDataSource : IDataSource
{
    private readonly string _json;
    private readonly string _name;

    public JsonDataSource(string json, string? name = null)
    {
        _json = json ?? throw new ArgumentNullException(nameof(json));
        _name = name ?? "JSON";
    }

    public Dataset Load()
    {
        using var doc = JsonDocument.Parse(_json);
        JsonElement root = doc.RootElement;

        // Accept either a bare array or an object with a single array property.
        if (root.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in root.EnumerateObject())
                if (prop.Value.ValueKind == JsonValueKind.Array) { root = prop.Value; break; }
        }

        if (root.ValueKind != JsonValueKind.Array)
            throw new FormatException("JSON source must be an array of objects (or an object containing one).");

        var columnOrder = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var records = new List<Dictionary<string, object?>>();

        foreach (var el in root.EnumerateArray())
        {
            if (el.ValueKind != JsonValueKind.Object) continue;
            var rec = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var prop in el.EnumerateObject())
            {
                if (seen.Add(prop.Name)) columnOrder.Add(prop.Name);
                rec[prop.Name] = ConvertValue(prop.Value);
            }
            records.Add(rec);
        }

        var rows = records.Select(rec =>
        {
            var row = new object?[columnOrder.Count];
            for (int c = 0; c < columnOrder.Count; c++)
                row[c] = rec.TryGetValue(columnOrder[c], out var v) ? v : null;
            return row;
        });

        return Dataset.FromRows(_name, columnOrder, rows);
    }

    private static object? ConvertValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Number => el.TryGetInt64(out var l) ? l : el.GetDouble(),
        JsonValueKind.String => el.GetString(),
        JsonValueKind.True   => true,
        JsonValueKind.False  => false,
        JsonValueKind.Null   => null,
        _                    => el.GetRawText()
    };
}
