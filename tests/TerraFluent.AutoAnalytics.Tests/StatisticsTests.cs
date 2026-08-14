using System.Collections.Generic;
using TerraFluent.AutoAnalytics.Statistics;
using Xunit;

namespace TerraFluent.AutoAnalytics.Tests;

public class StatisticsTests
{
    private static readonly double[] Sample = { 2, 4, 4, 4, 5, 5, 7, 9 };

    [Fact]
    public void Mean_Median_StdDev_AreCorrect()
    {
        Assert.Equal(5.0, DescriptiveStatistics.Mean(Sample), 3);
        Assert.Equal(4.5, DescriptiveStatistics.Median(Sample), 3);
        // population-like sample: sample StdDev (n-1) of this set ≈ 2.138
        Assert.Equal(2.138, DescriptiveStatistics.StdDev(Sample), 2);
    }

    [Fact]
    public void Mode_ReturnsMostFrequent()
        => Assert.Equal(4.0, DescriptiveStatistics.Mode(Sample), 3);

    [Fact]
    public void Percentiles_UseLinearInterpolation()
    {
        var sorted = DescriptiveStatistics.Sorted(Sample);
        Assert.Equal(4.0, DescriptiveStatistics.Percentile(sorted, 25), 3);
        Assert.Equal(5.5, DescriptiveStatistics.Percentile(sorted, 75), 3);
    }

    [Fact]
    public void Pearson_PerfectPositive_IsOne()
    {
        var x = new List<double> { 1, 2, 3, 4, 5 };
        var y = new List<double> { 2, 4, 6, 8, 10 };
        Assert.Equal(1.0, Correlation.Pearson(x, y), 6);
    }

    [Fact]
    public void Pearson_PerfectNegative_IsMinusOne()
    {
        var x = new List<double> { 1, 2, 3, 4, 5 };
        var y = new List<double> { 10, 8, 6, 4, 2 };
        Assert.Equal(-1.0, Correlation.Pearson(x, y), 6);
    }

    [Fact]
    public void Spearman_MonotonicNonLinear_IsOne()
    {
        var x = new List<double> { 1, 2, 3, 4, 5 };
        var y = new List<double> { 1, 4, 9, 16, 25 };
        Assert.Equal(1.0, Correlation.Spearman(x, y), 6);
    }

    [Fact]
    public void Regression_FitsLine_WithHighRSquared()
    {
        var y = new List<double> { 2, 4, 6, 8, 10 };
        var fit = Regression.FitOverIndex(y);
        Assert.Equal(2.0, fit.Slope, 6);
        Assert.Equal(2.0, fit.Intercept, 6);
        Assert.Equal(1.0, fit.RSquared, 6);
    }

    [Fact]
    public void GrowthRate_ComputesFractionalChange()
    {
        var y = new List<double> { 100, 110, 134 };
        Assert.Equal(0.34, Regression.GrowthRate(y), 3);
    }

    [Fact]
    public void TheilSen_IsRobustToASingleOutlier()
    {
        // Clean line y = 2x (slope 2). One wild outlier barely moves the median-of-slopes estimate,
        // whereas least squares would be dragged toward it.
        var y = new List<double> { 0, 2, 4, 6, 8, 10, 12, 999, 16, 18 };
        double robust = Regression.TheilSenSlope(y);
        Assert.Equal(2.0, robust, 1);
    }

    [Fact]
    public void PValue_StrongCorrelation_IsSignificant()
    {
        // Near-perfect correlation over 8 points is highly significant (p ≪ 0.05).
        Assert.True(Correlation.PValue(0.98, 8) < 0.01);
    }

    [Fact]
    public void PValue_WeakCorrelation_SmallSample_IsNotSignificant()
    {
        // A weak coefficient on a tiny sample cannot be distinguished from noise.
        Assert.True(Correlation.PValue(0.20, 5) > 0.05);
    }

    [Fact]
    public void PValue_TooFewPoints_ReturnsOne()
        => Assert.Equal(1.0, Correlation.PValue(0.9, 2), 6);

    [Fact]
    public void DetectSeasonLength_FindsRepeatingPeriod()
    {
        // Three cycles of a length-4 seasonal pattern.
        var series = new List<double>();
        for (int c = 0; c < 3; c++) series.AddRange(new double[] { 10, 20, 15, 5 });
        Assert.Equal(4, Forecasting.DetectSeasonLength(series));
    }

    [Fact]
    public void HoltWinters_ProjectsSeasonalPattern()
    {
        // Rising level with a strong length-4 season; the seasonal forecast should track the cycle.
        var series = new List<double>();
        for (int c = 0; c < 4; c++)
        {
            double baseLevel = 100 + c * 20;
            series.AddRange(new[] { baseLevel + 10, baseLevel + 30, baseLevel + 20, baseLevel });
        }
        var f = Forecasting.Forecast(series, horizon: 4);

        Assert.Equal("holt-winters", f.Method);
        Assert.Equal(4, f.SeasonLength);
        Assert.Equal(4, f.Points.Count);
        // Peak of the next cycle (2nd step) should exceed its trough (4th step).
        Assert.True(f.Points[1].Value > f.Points[3].Value);
    }
}
