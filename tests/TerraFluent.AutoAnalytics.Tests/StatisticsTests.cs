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
}
