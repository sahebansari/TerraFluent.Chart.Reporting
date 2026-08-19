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
public readonly record struct ChartStyle(string? ThemeName, SvgMode? RenderMode, bool? ShowExportMenu, bool? ShowGridLines)
{
    /// <summary>No overrides — the chart renders exactly as the recommendation engine configured it.</summary>
    public static ChartStyle None => default;

    /// <summary>Applies the overrides to an already-built chart, preserving the recommendation font scaling.</summary>
    public ChartBuilder Apply(ChartBuilder builder)
    {
        var theme = ResolveTheme(ThemeName);
        if (theme is not null)
        {
            var t = theme.Clone();
            t.FontScale = 1.2; // keep the 20% enlargement used across dashboards/reports
            builder.Theme(t);
        }
        if (RenderMode is SvgMode mode) builder.GetOptions().RenderMode = mode;
        if (ShowExportMenu is bool ex)
        {
            if (ex) builder.ShowExportMenu();
            else builder.GetOptions().ExportMenuEnabled = false;
        }
        if (ShowGridLines is bool grid) builder.GridLines(grid);
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
    public static ChartStyle FromQuery(string? theme, string? renderMode, bool? exportMenu, bool? gridLines)
    {
        SvgMode? mode = Enum.TryParse<SvgMode>(renderMode, ignoreCase: true, out var m) ? m : null;
        return new ChartStyle(theme, mode, exportMenu, gridLines);
    }
}
