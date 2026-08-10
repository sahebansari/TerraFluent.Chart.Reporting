namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Configuration for the interactive range-selector strip rendered below the main chart.
    /// Only active when <see cref="ChartOptions.RenderMode"/> is
    /// <see cref="TerraFluent.Chart.Reporting.Enums.SvgMode.Interactive"/>.
    /// </summary>
    public class RangeSelectorOptions
    {
        /// <summary>Enables the range-selector strip.</summary>
        public bool Enabled { get; set; }

        /// <summary>Height of the navigator strip in pixels. Default is 60.</summary>
        public int Height { get; set; } = 60;

        /// <summary>Fill colour of the brush selection overlay. Accepts any CSS colour string.</summary>
        public string FillColor { get; set; } = "rgba(100,150,255,0.18)";

        /// <summary>Colour of the left/right drag handles.</summary>
        public string HandleColor { get; set; } = "#6688cc";

        /// <summary>
        /// Initial start of the selected window as a fraction 0–1 of the full data range.
        /// Default 0.75 (shows the last 25% of the data on load).
        /// </summary>
        public double InitialStart { get; set; } = 0.75;

        /// <summary>
        /// Initial end of the selected window as a fraction 0–1. Default 1.0 (end of data).
        /// </summary>
        public double InitialEnd { get; set; } = 1.0;

        /// <summary>Returns a shallow copy of these options.</summary>
        public RangeSelectorOptions Clone() => new RangeSelectorOptions
        {
            Enabled      = Enabled,
            Height       = Height,
            FillColor    = FillColor,
            HandleColor  = HandleColor,
            InitialStart = InitialStart,
            InitialEnd   = InitialEnd,
        };
    }
}
