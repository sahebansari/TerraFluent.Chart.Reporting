using System;
using System.Collections.Generic;
using System.Linq;
using TerraFluent.Chart.Reporting.Analysis;
using TerraFluent.Chart.Reporting.Builder;
using TerraFluent.Chart.Reporting.Enums;
using TerraFluent.Chart.Reporting.Models;
using Xunit;

namespace TerraFluent.Chart.Reporting.Tests;

public class DataQualityAnalyzerTests
{
    // ── helpers ───────────────────────────────────────────────────────────────

    private static ChartOptions Options(Action<ChartOptions>? configure = null)
    {
        var o = new ChartOptions();
        configure?.Invoke(o);
        return o;
    }

    private static Series LineSeries(string name, params double?[] values)
        => new Series { Name = name, Type = ChartType.Line, Data = new List<double?>(values) };

    private static Series PieSeries(string name, params double?[] values)
        => new Series { Name = name, Type = ChartType.Pie, Data = new List<double?>(values) };

    // ── clean data → no warnings ──────────────────────────────────────────────

    [Fact]
    public void Analyze_CleanOptions_ReturnsCleanReport()
    {
        var opts = Options(o =>
        {
            o.XAxis.Categories.AddRange(new[] { "Jan", "Feb", "Mar" });
            o.Series.Add(LineSeries("Revenue", 100, 200, 150));
        });

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.True(report.IsClean);
        Assert.False(report.HasErrors);
        Assert.Empty(report.Warnings);
    }

    // ── ChartBuilder integration ──────────────────────────────────────────────

    [Fact]
    public void ChartBuilder_AnalyzeDataQuality_ReturnsReport()
    {
        var report = ChartBuilder.Create()
            .Series(s => s.AddLine("Data", new double?[] { 1, 2, 3 }))
            .AnalyzeDataQuality();

        Assert.NotNull(report);
        Assert.True(report.IsClean);
    }

    // ── Check 1 : Missing values ──────────────────────────────────────────────

    [Fact]
    public void Analyze_SeriesWithNulls_RaisesMissingValuesWarning()
    {
        var opts = Options(o => o.Series.Add(LineSeries("S", 1.0, null, 3.0)));

        var report = DataQualityAnalyzer.Analyze(opts);

        var w = Assert.Single(report.Warnings);
        Assert.Equal("MissingValues", w.Category);
        Assert.Equal(WarningSeverity.Warning, w.Severity);
        Assert.Equal("S", w.SeriesName);
    }

    [Fact]
    public void Analyze_SeriesWithMultipleNulls_CountsCorrectly()
    {
        var opts = Options(o => o.Series.Add(LineSeries("S", null, null, 5.0)));

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.Contains(report.Warnings, w => w.Category == "MissingValues" && w.Message.Contains("2 missing"));
    }

    // ── Check 2 : Duplicate categories ───────────────────────────────────────

    [Fact]
    public void Analyze_DuplicateCategories_RaisesError()
    {
        var opts = Options(o =>
        {
            o.XAxis.Categories.AddRange(new[] { "A", "B", "A" });
            o.Series.Add(LineSeries("S", 1, 2, 3));
        });

        var report = DataQualityAnalyzer.Analyze(opts);

        var w = Assert.Single(report.Warnings);
        Assert.Equal("DuplicateCategories", w.Category);
        Assert.Equal(WarningSeverity.Error, w.Severity);
        Assert.Null(w.SeriesName);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void Analyze_UniqueCategories_NoDuplicateWarning()
    {
        var opts = Options(o =>
        {
            o.XAxis.Categories.AddRange(new[] { "X", "Y", "Z" });
            o.Series.Add(LineSeries("S", 1, 2, 3));
        });

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "DuplicateCategories");
    }

    // ── Check 3 : Negative pie values ────────────────────────────────────────

    [Fact]
    public void Analyze_NegativePieValue_RaisesError()
    {
        var opts = Options(o => o.Series.Add(PieSeries("P", 30, -5, 20)));

        var report = DataQualityAnalyzer.Analyze(opts);

        var w = report.Warnings.First(x => x.Category == "NegativePieValues");
        Assert.Equal(WarningSeverity.Error, w.Severity);
        Assert.Equal("P", w.SeriesName);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void Analyze_NegativeValueInLineSeries_DoesNotRaiseNegativePieWarning()
    {
        var opts = Options(o => o.Series.Add(LineSeries("L", -10, 20, 30)));

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "NegativePieValues");
    }

    [Fact]
    public void Analyze_AllPositivePie_NoNegativePieWarning()
    {
        var opts = Options(o => o.Series.Add(PieSeries("P", 30, 50, 20)));

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "NegativePieValues");
    }

    // ── Check 4 : Broken date sequences ──────────────────────────────────────

    [Fact]
    public void Analyze_OutOfOrderDates_RaisesWarning()
    {
        var opts = Options(o =>
        {
            o.XAxis.DateTimeValues.AddRange(new[]
            {
                new DateTime(2024, 1, 1),
                new DateTime(2024, 3, 1),
                new DateTime(2024, 2, 1)   // out of order
            });
            o.Series.Add(LineSeries("S", 1, 2, 3));
        });

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.Contains(report.Warnings, w => w.Category == "BrokenDateSequence");
        Assert.Equal(WarningSeverity.Warning,
            report.Warnings.First(w => w.Category == "BrokenDateSequence").Severity);
    }

    [Fact]
    public void Analyze_DuplicateDates_RaisesWarning()
    {
        var opts = Options(o =>
        {
            o.XAxis.DateTimeValues.AddRange(new[]
            {
                new DateTime(2024, 1, 1),
                new DateTime(2024, 1, 1),  // duplicate
                new DateTime(2024, 2, 1)
            });
            o.Series.Add(LineSeries("S", 1, 2, 3));
        });

        Assert.Contains(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "BrokenDateSequence");
    }

    [Fact]
    public void Analyze_AscendingDates_NoBrokenSequenceWarning()
    {
        var opts = Options(o =>
        {
            o.XAxis.DateTimeValues.AddRange(new[]
            {
                new DateTime(2024, 1, 1),
                new DateTime(2024, 2, 1),
                new DateTime(2024, 3, 1)
            });
            o.Series.Add(LineSeries("S", 1, 2, 3));
        });

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "BrokenDateSequence");
    }

    // ── Check 5 : Invalid percentages ────────────────────────────────────────

    [Fact]
    public void Analyze_ValueOver100WithPercentYAxisLabel_RaisesWarning()
    {
        var opts = Options(o =>
        {
            o.YAxis.LabelFormat = "{value}%";
            o.Series.Add(LineSeries("S", 50, 110, 80));  // 110 is out of range
        });

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.Contains(report.Warnings, w => w.Category == "InvalidPercentage");
        Assert.Equal(WarningSeverity.Warning,
            report.Warnings.First(w => w.Category == "InvalidPercentage").Severity);
    }

    [Fact]
    public void Analyze_ValidPercentageValues_NoInvalidPercentageWarning()
    {
        var opts = Options(o =>
        {
            o.YAxis.LabelFormat = "{value}%";
            o.Series.Add(LineSeries("S", 0, 50, 100));
        });

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "InvalidPercentage");
    }

    [Fact]
    public void Analyze_PercentDataLabelFormat_ChecksValues()
    {
        var opts = Options(o =>
        {
            var s = new Series
            {
                Name = "S",
                Type = ChartType.Column,
                Data = new List<double?> { 20, -5, 80 }
            };
            s.DataLabel.Show().Format("{value}%");
            o.Series.Add(s);
        });

        Assert.Contains(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "InvalidPercentage");
    }

    // ── Check 6 : Empty datasets ──────────────────────────────────────────────

    [Fact]
    public void Analyze_EmptyDataList_RaisesError()
    {
        var opts = Options(o =>
            o.Series.Add(new Series { Name = "Empty", Type = ChartType.Line }));

        var report = DataQualityAnalyzer.Analyze(opts);

        var w = Assert.Single(report.Warnings);
        Assert.Equal("EmptyDataset", w.Category);
        Assert.Equal(WarningSeverity.Error, w.Severity);
        Assert.True(report.HasErrors);
    }

    [Fact]
    public void Analyze_AllNullData_RaisesEmptyDatasetError()
    {
        var opts = Options(o =>
            o.Series.Add(LineSeries("S", null, null, null)));

        var report = DataQualityAnalyzer.Analyze(opts);

        // EmptyDataset (Error) + MissingValues (Warning) expected
        Assert.Contains(report.Warnings, w => w.Category == "EmptyDataset" && w.Severity == WarningSeverity.Error);
    }

    // ── Check 7 : Extremely skewed distributions ──────────────────────────────

    [Fact]
    public void Analyze_HighlySkewedSeries_RaisesHighSkewnessWarning()
    {
        // Extreme right-skew: one very large outlier
        var opts = Options(o =>
            o.Series.Add(LineSeries("S", 1, 1, 1, 1, 1, 1, 1, 1, 1, 1000)));

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.Contains(report.Warnings, w => w.Category == "HighSkewness");
        Assert.Equal(WarningSeverity.Warning,
            report.Warnings.First(w => w.Category == "HighSkewness").Severity);
    }

    [Fact]
    public void Analyze_SymmetricSeries_NoHighSkewnessWarning()
    {
        // Symmetric data: 1,2,3,4,5 — skewness = 0
        var opts = Options(o =>
            o.Series.Add(LineSeries("S", 1, 2, 3, 4, 5)));

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "HighSkewness");
    }

    [Fact]
    public void Analyze_ConstantSeries_NoHighSkewnessWarning()
    {
        var opts = Options(o =>
            o.Series.Add(LineSeries("S", 5, 5, 5, 5)));

        Assert.DoesNotContain(DataQualityAnalyzer.Analyze(opts).Warnings,
            w => w.Category == "HighSkewness");
    }

    // ── Severity ordering ─────────────────────────────────────────────────────

    [Fact]
    public void Analyze_MixedIssues_WarningsOrderedBySeverityDescending()
    {
        var opts = Options(o =>
        {
            // Error: duplicate category
            o.XAxis.Categories.AddRange(new[] { "A", "A" });
            // Warning: missing value
            o.Series.Add(LineSeries("S", 1.0, null, 3.0));
        });

        var report = DataQualityAnalyzer.Analyze(opts);

        Assert.True(report.HasErrors);
        Assert.False(report.IsClean);
        // First warning must be Error or higher than the next
        for (int i = 1; i < report.Warnings.Count; i++)
            Assert.True(report.Warnings[i].Severity <= report.Warnings[i - 1].Severity);
    }

    // ── Null guard ────────────────────────────────────────────────────────────

    [Fact]
    public void Analyze_NullOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DataQualityAnalyzer.Analyze(null!));
    }

    // ── ToString ──────────────────────────────────────────────────────────────

    [Fact]
    public void DataQualityWarning_ToString_IncludesSeverityAndCategory()
    {
        // Use an empty Pie series with a negative value to get a real Error warning
        var opts = Options(o => o.Series.Add(PieSeries("MySeries", -1, 10)));
        var report = DataQualityAnalyzer.Analyze(opts);

        var w = report.Warnings.First(x => x.Category == "NegativePieValues");
        string s = w.ToString();

        Assert.Contains("[Error]", s);
        Assert.Contains("NegativePieValues", s);
        Assert.Contains("MySeries", s);
    }

    [Fact]
    public void DataQualityWarning_ToString_ChartLevelWarning_OmitsSeriesName()
    {
        var opts = Options(o =>
        {
            o.XAxis.Categories.AddRange(new[] { "A", "A" });
            o.Series.Add(LineSeries("S", 1, 2));
        });

        var w = DataQualityAnalyzer.Analyze(opts).Warnings
            .First(x => x.Category == "DuplicateCategories");

        Assert.DoesNotContain("(", w.ToString()); // no "(SeriesName)" section
    }

    // ── Implicit analysis via RenderToSvg ─────────────────────────────────────

    [Fact]
    public void RenderToSvg_AutoPopulatesLastDataQualityReport()
    {
        var builder = ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double?[] { 1, null, 3 }));

        // Report is null before any render
        Assert.Null(builder.GetLastDataQualityReport());

        builder.RenderToSvg();

        var report = builder.GetLastDataQualityReport();
        Assert.NotNull(report);
        Assert.Contains(report!.Warnings, w => w.Category == "MissingValues");
    }

    [Fact]
    public void RenderToSvg_CleanData_ReportIsClean()
    {
        var builder = ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }));

        builder.RenderToSvg();

        Assert.True(builder.GetLastDataQualityReport()!.IsClean);
    }

    // ── ThrowOnDataQualityErrors ──────────────────────────────────────────────

    [Fact]
    public void ThrowOnDataQualityErrors_WithErrors_ThrowsDataQualityException()
    {
        var builder = ChartBuilder.Create()
            .ThrowOnDataQualityErrors()
            .Series(s => s.AddPie("P", new double?[] { 10, -5, 30 })); // negative pie value

        var ex = Assert.Throws<DataQualityException>(() => builder.RenderToSvg());

        Assert.NotNull(ex.Report);
        Assert.True(ex.Report.HasErrors);
        Assert.Contains("NegativePieValues", ex.Message);
    }

    [Fact]
    public void ThrowOnDataQualityErrors_WithOnlyWarnings_DoesNotThrow()
    {
        var builder = ChartBuilder.Create()
            .ThrowOnDataQualityErrors()
            .Series(s => s.AddLine("S", new double?[] { 1, null, 3 })); // Warning, not Error

        // Should render without throwing
        var svg = builder.RenderToSvg();
        Assert.NotNull(svg);
        Assert.Contains(WarningSeverity.Warning,
            builder.GetLastDataQualityReport()!.Warnings.Select(w => w.Severity));
    }

    [Fact]
    public void ThrowOnDataQualityErrors_CleanData_DoesNotThrow()
    {
        var svg = ChartBuilder.Create()
            .ThrowOnDataQualityErrors()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }))
            .RenderToSvg();

        Assert.NotNull(svg);
    }

    [Fact]
    public void ThrowOnDataQualityErrors_Exception_ContainsAllErrorMessages()
    {
        // Two error conditions: empty dataset + duplicate categories
        var builder = ChartBuilder.Create()
            .ThrowOnDataQualityErrors()
            .XAxis(x => x.Categories.AddRange(new[] { "A", "A" }));
        builder.GetOptions().Series.Add(new Series { Name = "Empty", Type = ChartType.Line });

        var ex = Assert.Throws<DataQualityException>(() => builder.RenderToSvg());

        Assert.Contains("DuplicateCategories", ex.Message);
        Assert.Contains("EmptyDataset", ex.Message);
    }

    [Fact]
    public void WithoutThrowOnDataQualityErrors_Errors_StillRenders()
    {
        // Without ThrowOnDataQualityErrors, rendering succeeds even with errors
        var svg = ChartBuilder.Create()
            .Series(s => s.AddPie("P", new double?[] { 10, -5, 30 }))
            .RenderToSvg();

        Assert.NotNull(svg);
        Assert.Contains("<svg", svg);
    }

    [Fact]
    public void ExplicitAnalyzeDataQuality_CachesReport_RetrievableAfterwards()
    {
        var builder = ChartBuilder.Create()
            .Series(s => s.AddLine("S", new double?[] { 1, 2, 3 }));

        var report = builder.AnalyzeDataQuality();

        Assert.Same(report, builder.GetLastDataQualityReport());
    }
}
