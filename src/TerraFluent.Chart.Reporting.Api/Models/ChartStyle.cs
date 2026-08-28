using System;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Api.Models;

/// <summary>
/// Optional per-request visual overrides applied to the auto-generated recommendation charts shown
/// on the Analyze, Dashboard and Ask-the-Agent pages. A <c>null</c> field means "leave the engine
/// default untouched". The default value (<see cref="None"/>) changes nothing.
/// </summary>
public readonly record struct ChartStyle(string? ThemeName, SvgMode? RenderMode, bool? ShowExportMenu, bool? ShowGridLines, bool? ShowDataLabels, string? LegendPosition, double? FontScale, string? BackgroundColor)
{
    /// <summary>No overrides — the chart renders exactly as the recommendation engine configured it.</summary>
    public static ChartStyle None => default;

    /// <summary>Applies the overrides to an already-built chart, preserving the recommendation font scaling.</summary>
    public ChartBuilder Apply(ChartBuilder builder)
    {
        // A global font scale applies even when no theme override is supplied, so start from the
        // requested theme or the chart's current one and clone it before mutating.
        var baseTheme = ResolveTheme(ThemeName) ?? builder.GetOptions().Theme;
        if (baseTheme is not null)
        {
            var t = baseTheme.Clone();
            t.FontScale = FontScale is double f && f > 0 ? f : 1.2; // default: 20% enlargement for web
            builder.Theme(t);
        }
        if (RenderMode is SvgMode mode) builder.GetOptions().RenderMode = mode;
        if (ShowExportMenu is bool ex)
        {
            if (ex) builder.ShowExportMenu();
            else builder.GetOptions().ExportMenuEnabled = false;
        }
        if (ShowGridLines is bool grid) builder.GridLines(grid);
        if (ShowDataLabels is bool dl && dl) builder.ShowDataLabels();
        switch (LegendPosition?.Trim().ToLowerInvariant())
        {
            case "none":   builder.HideLegend(); break;
            case "top":    builder.Legend(l => l.AtTop().AlignCenter().Horizontal()); break;
            case "bottom": builder.Legend(l => l.AtBottom().AlignCenter().Horizontal()); break;
            case "left":   builder.Legend(l => l.AtMiddle().AlignLeft().Vertical()); break;
            case "right":  builder.Legend(l => l.AtMiddle().AlignRight().Vertical()); break;
        }
        if (!string.IsNullOrWhiteSpace(BackgroundColor)) builder.Background(BackgroundColor);
        return builder;
    }

    /// <summary>Resolves a built-in theme by name (case-insensitive). Returns <c>null</c> when unknown.</summary>
    public static ChartTheme? ResolveTheme(string? name) => name?.Trim().ToLowerInvariant() switch
    {
        "vivid"        => ChartTheme.Vivid,
        "default"      => ChartTheme.Default,
        "dark"         => ChartTheme.Dark,
        "pastel"       => ChartTheme.Pastel,
        "monochrome"   => ChartTheme.Monochrome,
        "ocean"        => ChartTheme.Ocean,
        "sunset"       => ChartTheme.Sunset,
        "forest"       => ChartTheme.Forest,
        "neon"         => ChartTheme.Neon,
        "minimal"      => ChartTheme.Minimal,
        "warm"         => ChartTheme.Warm,
        "arctic"       => ChartTheme.Arctic,
        "business"     => ChartTheme.Business,
        "material"     => ChartTheme.Material,
        "trafficlight" => ChartTheme.TrafficLight,
        "accessible"   => ChartTheme.Accessible,
        "highcontrast" => ChartTheme.HighContrast,
        _              => null,
    };

    /// <summary>Builds a style from raw query-string values, parsing the render mode leniently.</summary>
    public static ChartStyle FromQuery(string? theme, string? renderMode, bool? exportMenu, bool? gridLines, bool? dataLabels = null, string? legend = null, double? fontScale = null, string? background = null)
    {
        SvgMode? mode = Enum.TryParse<SvgMode>(renderMode, ignoreCase: true, out var m) ? m : null;
        return new ChartStyle(theme, mode, exportMenu, gridLines, dataLabels, legend, fontScale, background);
    }
}
