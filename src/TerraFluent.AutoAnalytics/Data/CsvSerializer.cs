using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TerraFluent.AutoAnalytics.Data;

/// <summary>
/// Serialises a <see cref="Dataset"/> back to RFC 4180 CSV text. Used to hand a normalised, textual
/// form of a binary source (e.g. an <c>.xlsx</c> workbook) to callers that only speak CSV.
/// </summary>
public static class CsvSerializer
{
    /// <summary>Writes the dataset as CSV with a header row, using <see cref="CultureInfo.InvariantCulture"/>.</summary>
    public static string ToCsv(Dataset dataset)
    {
        if (dataset is null) throw new ArgumentNullException(nameof(dataset));

        var sb = new StringBuilder();
        var columns = dataset.Columns;
        if (columns.Count == 0) return string.Empty;

        for (int c = 0; c < columns.Count; c++)
        {
            if (c > 0) sb.Append(',');
            sb.Append(Escape(columns[c].Name));
        }
        sb.Append('\n');

        for (int r = 0; r < dataset.RowCount; r++)
        {
            for (int c = 0; c < columns.Count; c++)
            {
                if (c > 0) sb.Append(',');
                var values = columns[c].Values;
                sb.Append(Escape(Format(r < values.Count ? values[r] : null)));
            }
            sb.Append('\n');
        }

        return sb.ToString();
    }

    // Renders a raw cell object in a round-trippable, culture-invariant form.
    private static string Format(object? value) => value switch
    {
        null            => string.Empty,
        string s        => s,
        bool b          => b ? "true" : "false",
        double d        => d.ToString("R", CultureInfo.InvariantCulture),
        float f         => f.ToString("R", CultureInfo.InvariantCulture),
        decimal m       => m.ToString(CultureInfo.InvariantCulture),
        // A midnight timestamp is a plain date; keep it short so date discovery still recognises it.
        DateTime dt     => dt.TimeOfDay == TimeSpan.Zero
                             ? dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                             : dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
        IFormattable fm => fm.ToString(null, CultureInfo.InvariantCulture),
        _               => value.ToString() ?? string.Empty
    };

    // Quotes a field only when it contains a delimiter, quote or line break (RFC 4180 §2.6).
    private static string Escape(string field)
    {
        if (field.Length == 0) return field;
        bool needsQuotes = field.IndexOf(',') >= 0
                        || field.IndexOf('"') >= 0
                        || field.IndexOf('\n') >= 0
                        || field.IndexOf('\r') >= 0;
        if (!needsQuotes) return field;
        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }
}
