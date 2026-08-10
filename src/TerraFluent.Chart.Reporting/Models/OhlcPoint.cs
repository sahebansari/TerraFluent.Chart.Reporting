namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A single financial data point for <see cref="Enums.ChartType.Candlestick"/> and
    /// <see cref="Enums.ChartType.Ohlc"/> series. Carries the four prices that describe one
    /// trading period: opening, session high, session low, and closing price.
    /// </summary>
    public struct OhlcPoint
    {
        /// <summary>Opening price for the period.</summary>
        public double Open { get; set; }
        /// <summary>Highest price reached during the period.</summary>
        public double High { get; set; }
        /// <summary>Lowest price reached during the period.</summary>
        public double Low { get; set; }
        /// <summary>Closing price for the period.</summary>
        public double Close { get; set; }

        /// <summary>Initialises an OHLC point from its open, high, low and close prices.</summary>
        public OhlcPoint(double open, double high, double low, double close)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }
    }
}
