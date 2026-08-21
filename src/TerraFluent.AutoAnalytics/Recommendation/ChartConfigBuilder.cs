using System;
using System.Globalization;
using System.Linq;
using TerraFluent.Chart.Reporting.Builder;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;
using Stacking = TerraFluent.Chart.Reporting.Enums.Stacking;

namespace TerraFluent.AutoAnalytics.Recommendation;

/// <summary>
/// Phase 8 — bridges a rendering-agnostic <see cref="ChartSpec"/> to a concrete
/// TerraFluent.Chart.Reporting <see cref="ChartBuilder"/>, ready to render to SVG/PNG/etc.
/// </summary>
public static class ChartConfigBuilder
{
    /// <summary>Builds a configured <see cref="ChartBuilder"/> from a spec.</summary>
    public static ChartBuilder ToChartBuilder(ChartSpec spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));

        // Enlarge all chart text by 20% for readability on the web dashboards/reports.
        var theme = TerraFluent.Chart.Reporting.Models.ChartTheme.Default.Clone();
        theme.FontScale = 1.2;

        var chart = ChartBuilder.Create()
            .Title(spec.Title)
            .Size(spec.Width, spec.Height)
            .Theme(theme)
            .AsInteractive()   // Interactive mode is required for the export menu JS to run.
            .ShowExportMenu();

        if (spec.Categories.Count > 0)
        {
            var categories = spec.Categories.Select(FormatCategoryLabel).ToArray();
            chart.Labels(categories);

            // Category axis: render every tick label (no thinning/skipping) and never abbreviate.
            // Range-style ticks (e.g. "12k–22k") are wide, so always angle them regardless of count:
            // diagonal (-45), or vertical (-90) once there are many (>12).
            int rangeTicks = categories.Count(IsRangeLabel);
            bool mostlyRanges = rangeTicks > categories.Length / 2;
            chart.LabelLayout(l =>
            {
                l.Skip(1);
                if (mostlyRanges) l.Rotation(categories.Length > 12 ? -90 : -45);
            });
        }
        else
        {
            chart.LabelLayout(l => l.Skip(1));
        }

        chart.Series(s =>
        {
            foreach (var series in spec.Series)
            {
                // Plot the actual series values (no rounding).
                var values = series.Values.ToList();
                double scalar = series.ScalarValue ?? 0;
                switch (spec.Type)
                {
                    case ChartType.Line:    s.AddLine(series.Name, values); break;
                    case ChartType.Spline:  s.AddSpline(series.Name, values); break;
                    case ChartType.Area:    s.AddArea(series.Name, values); break;
                    case ChartType.Bar:     s.AddBar(series.Name, values); break;
                    case ChartType.Column:  s.AddColumn(series.Name, values); break;
                    // Pie labels sit just outside each slice (with a leader line) so shares are readable.
                    case ChartType.Pie:     s.AddPie(series.Name, values, cfg => cfg.DataLabel.Show().Radius(1.5)); break;
                    case ChartType.Scatter: s.AddScatter(series.Name, values); break;
                    case ChartType.Radar:   s.AddRadar(series.Name, values); break;
                    case ChartType.Gauge:   s.AddGauge(series.Name, scalar); break;
                    case ChartType.DataRing: s.AddDataRing(series.Name, scalar); break;
                    default:                s.AddColumn(series.Name, values); break;
                }
            }
        });

        // Axis titles (rendered on cartesian charts; harmless on pie/gauge/etc.).
        if (!string.IsNullOrWhiteSpace(spec.XAxisTitle))
            chart.XAxis(x => x.Title = spec.XAxisTitle);
        if (!string.IsNullOrWhiteSpace(spec.YAxisTitle))
            chart.YAxis(y => y.Title = spec.YAxisTitle);

        if (spec.StackingMode == Stacking.Normal) chart.StackNormal();
        else if (spec.StackingMode == Stacking.Percent) chart.StackPercent();

        return chart;
    }

    /// <summary>Convenience: builds and renders the spec directly to an SVG string.</summary>
    public static string ToSvg(ChartSpec spec) => ToChartBuilder(spec).RenderToSvg();

    // Numeric category labels are rounded to whole numbers so tick labels never show fractions.
    // Non-numeric labels (dates, dimension names, etc.) pass through unchanged.
    private static string FormatCategoryLabel(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return category;
        return double.TryParse(category, NumberStyles.Any, CultureInfo.InvariantCulture, out double num)
            ? Math.Round(num, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)
            : category;
    }

    // True for range/bin tick labels such as "12k–22k" (en-dash joins the lower and upper bound),
    // as emitted by the histogram and binned-correlation charts.
    private static bool IsRangeLabel(string category) =>
        !string.IsNullOrEmpty(category) && category.IndexOf('\u2013') >= 0;
}
