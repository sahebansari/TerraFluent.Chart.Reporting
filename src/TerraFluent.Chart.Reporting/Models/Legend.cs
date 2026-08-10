namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Legend configuration. Mirrors HighCharts <c>legend</c>.
    /// </summary>
    public class Legend
    {
        // ---- visibility ----
        /// <summary>Whether to render the legend. Default <c>true</c>.</summary>
        public bool Enabled { get; set; } = true;

        // ---- position ----
        /// <summary>Horizontal alignment of the legend block: "left", "center" (default), or "right".</summary>
        public string Align { get; set; } = "center";
        /// <summary>Vertical alignment: "top", "middle", or "bottom" (default).</summary>
        public string VerticalAlign { get; set; } = "bottom";
        /// <summary>Pixel offset applied after Align/VerticalAlign. Positive moves right/down.</summary>
        public int X { get; set; } = 0;
        /// <summary>Pixel offset applied after Align/VerticalAlign. Positive moves right/down.</summary>
        public int Y { get; set; } = 0;

        // ---- layout ----
        /// <summary>Item flow direction: "horizontal" (default) or "vertical".</summary>
        public string Layout { get; set; } = "horizontal";
        /// <summary>Inner padding (px) between the legend border and its items.</summary>
        public int Padding { get; set; } = 8;
        /// <summary>Outer margin (px) between the legend box and the nearest chart edge / title. Default 10.</summary>
        public int Margin { get; set; } = 10;

        // ---- item text style ----
        /// <summary>Raw CSS string applied to every legend item &lt;text&gt; element (overrides other font settings).</summary>
        public string? ItemStyle { get; set; }
        /// <summary>Font size (px) for legend item labels.</summary>
        public int ItemFontSize { get; set; } = 12;
        /// <summary>Font color for legend item labels. Defaults to the theme text color when null.</summary>
        public string? ItemFontColor { get; set; }

        // ---- symbol ----
        /// <summary>Width (px) of the color swatch / symbol.</summary>
        public int SymbolWidth { get; set; } = 12;
        /// <summary>Height (px) of the color swatch / symbol.</summary>
        public int SymbolHeight { get; set; } = 12;
        /// <summary>Corner radius (px) of the color swatch rectangle.</summary>
        public int SymbolRadius { get; set; } = 2;

        // ---- border ----
        /// <summary>Border color of the legend box. No border is drawn when null or BorderWidth is 0.</summary>
        public string? BorderColor { get; set; }
        /// <summary>Border stroke-width (px). Default 0 = no border.</summary>
        public int BorderWidth { get; set; } = 0;
        /// <summary>Corner radius (px) of the legend border rectangle.</summary>
        public int BorderRadius { get; set; } = 0;

        // ---- background ----
        /// <summary>Background fill color of the legend box. No background when null.</summary>
        public string? BackgroundColor { get; set; }

        // ---- pagination ----
        /// <summary>
        /// Maximum number of legend item rows to show per page.
        /// <c>0</c> (default) disables pagination and shows all items at once.
        /// When greater than zero and the items would require more rows than this limit,
        /// previous / next page arrows are rendered in
        /// <see cref="Enums.SvgMode.Interactive"/> mode.
        /// In <see cref="Enums.SvgMode.Static"/> and <see cref="Enums.SvgMode.Animated"/>
        /// modes all items are always shown regardless of this setting.
        /// </summary>
        public int MaxLegendRows { get; set; } = 0;
    }
}
