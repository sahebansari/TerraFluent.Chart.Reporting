using System;
using System.Linq;
using TerraFluent.Chart.Reporting.Builder;
using ChartType = TerraFluent.Chart.Reporting.Enums.ChartType;

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

        var chart = ChartBuilder.Create()
            .Title(spec.Title)
            .Size(spec.Width, spec.Height);

        if (spec.Categories.Count > 0)
            chart.Labels(spec.Categories.ToArray());

        chart.Series(s =>
        {
            foreach (var series in spec.Series)
            {
                var values = series.Values;
                switch (spec.Type)
                {
                    case ChartType.Line:    s.AddLine(series.Name, values); break;
                    case ChartType.Spline:  s.AddSpline(series.Name, values); break;
                    case ChartType.Area:    s.AddArea(series.Name, values); break;
                    case ChartType.Bar:     s.AddBar(series.Name, values); break;
                    case ChartType.Column:  s.AddColumn(series.Name, values); break;
                    case ChartType.Pie:     s.AddPie(series.Name, values); break;
                    case ChartType.Scatter: s.AddScatter(series.Name, values); break;
                    case ChartType.Radar:   s.AddRadar(series.Name, values); break;
                    case ChartType.Gauge:   s.AddGauge(series.Name, series.ScalarValue ?? 0); break;
                    case ChartType.DataRing: s.AddDataRing(series.Name, series.ScalarValue ?? 0); break;
                    default:                s.AddColumn(series.Name, values); break;
                }
            }
        });

        return chart;
    }

    /// <summary>Convenience: builds and renders the spec directly to an SVG string.</summary>
    public static string ToSvg(ChartSpec spec) => ToChartBuilder(spec).RenderToSvg();
}
