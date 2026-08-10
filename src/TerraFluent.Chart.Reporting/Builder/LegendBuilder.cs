using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for configuring a chart <see cref="Legend"/>.
    /// Obtain an instance via <see cref="ChartBuilder.Legend(Action{LegendBuilder})"/>.
    /// </summary>
    public sealed class LegendBuilder
    {
        private readonly Legend _legend;

        internal LegendBuilder(Legend legend)
        {
            _legend = legend ?? throw new ArgumentNullException(nameof(legend));
            _legend.Enabled = true;   // configuring the legend implies showing it
        }

        // ------------------------------------------------------------------ visibility

        /// <summary>Hides the legend.</summary>
        public LegendBuilder Disable() { _legend.Enabled = false; return this; }

        // ------------------------------------------------------------------ horizontal alignment

        /// <summary>Aligns the legend horizontally: "left", "center", or "right".</summary>
        public LegendBuilder Align(string align)
        {
            if (string.IsNullOrWhiteSpace(align)) throw new ArgumentException("Align value must not be empty.", nameof(align));
            _legend.Align = align;
            return this;
        }

        /// <summary>Left-aligns the legend.</summary>
        public LegendBuilder AlignLeft() => Align("left");

        /// <summary>Center-aligns the legend (default).</summary>
        public LegendBuilder AlignCenter() => Align("center");

        /// <summary>Right-aligns the legend.</summary>
        public LegendBuilder AlignRight() => Align("right");

        // ------------------------------------------------------------------ vertical alignment

        /// <summary>Sets the vertical position of the legend: "top", "middle", or "bottom".</summary>
        public LegendBuilder VerticalAlign(string verticalAlign)
        {
            if (string.IsNullOrWhiteSpace(verticalAlign)) throw new ArgumentException("VerticalAlign value must not be empty.", nameof(verticalAlign));
            _legend.VerticalAlign = verticalAlign;
            return this;
        }

        /// <summary>Places the legend at the top of the chart.</summary>
        public LegendBuilder AtTop() => VerticalAlign("top");

        /// <summary>Places the legend in the middle of the chart.</summary>
        public LegendBuilder AtMiddle() => VerticalAlign("middle");

        /// <summary>Places the legend at the bottom of the chart (default).</summary>
        public LegendBuilder AtBottom() => VerticalAlign("bottom");

        // ------------------------------------------------------------------ combined position shorthands

        /// <summary>Positions the legend at the top-left corner.</summary>
        public LegendBuilder TopLeft()     => Align("left").VerticalAlign("top");

        /// <summary>Positions the legend at the top-center (common for titles).</summary>
        public LegendBuilder TopCenter()   => Align("center").VerticalAlign("top");

        /// <summary>Positions the legend at the top-right corner.</summary>
        public LegendBuilder TopRight()    => Align("right").VerticalAlign("top");

        /// <summary>Positions the legend at the bottom-left corner.</summary>
        public LegendBuilder BottomLeft()  => Align("left").VerticalAlign("bottom");

        /// <summary>Positions the legend at the bottom-center (default position).</summary>
        public LegendBuilder BottomCenter() => Align("center").VerticalAlign("bottom");

        /// <summary>Positions the legend at the bottom-right corner.</summary>
        public LegendBuilder BottomRight() => Align("right").VerticalAlign("bottom");

        // ------------------------------------------------------------------ pixel offset

        /// <summary>Applies pixel offsets after the Align/VerticalAlign position is computed.</summary>
        public LegendBuilder Offset(int x, int y) { _legend.X = x; _legend.Y = y; return this; }

        /// <summary>Sets only the horizontal pixel offset.</summary>
        public LegendBuilder OffsetX(int x) { _legend.X = x; return this; }

        /// <summary>Sets only the vertical pixel offset.</summary>
        public LegendBuilder OffsetY(int y) { _legend.Y = y; return this; }

        // ------------------------------------------------------------------ layout

        /// <summary>Sets item flow direction: "horizontal" or "vertical".</summary>
        public LegendBuilder Layout(string layout)
        {
            if (string.IsNullOrWhiteSpace(layout)) throw new ArgumentException("Layout value must not be empty.", nameof(layout));
            _legend.Layout = layout;
            return this;
        }

        /// <summary>Lays out legend items horizontally (default).</summary>
        public LegendBuilder Horizontal() => Layout("horizontal");

        /// <summary>Lays out legend items vertically in a single column.</summary>
        public LegendBuilder Vertical() => Layout("vertical");

        /// <summary>Sets the inner padding (px) between the legend border and its items.</summary>
        public LegendBuilder Padding(int px)
        {
            if (px < 0) throw new ArgumentOutOfRangeException(nameof(px), "Padding must be >= 0.");
            _legend.Padding = px;
            return this;
        }

        // ------------------------------------------------------------------ item text style

        /// <summary>Applies a raw CSS string to every legend item label element.</summary>
        public LegendBuilder ItemStyle(string css) { _legend.ItemStyle = css; return this; }

        /// <summary>Sets the font size (px) for legend item labels.</summary>
        public LegendBuilder ItemFontSize(int px)
        {
            if (px <= 0) throw new ArgumentOutOfRangeException(nameof(px), "Font size must be > 0.");
            _legend.ItemFontSize = px;
            return this;
        }

        /// <summary>Sets the text color for legend item labels.</summary>
        public LegendBuilder ItemFontColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _legend.ItemFontColor = color;
            return this;
        }

        // ------------------------------------------------------------------ symbol

        /// <summary>Sets the dimensions of the color swatch symbol.</summary>
        public LegendBuilder SymbolSize(int width, int height)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            _legend.SymbolWidth  = width;
            _legend.SymbolHeight = height;
            return this;
        }

        /// <summary>Sets the corner radius (px) of the color swatch rectangle.</summary>
        public LegendBuilder SymbolRadius(int radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _legend.SymbolRadius = radius;
            return this;
        }

        // ------------------------------------------------------------------ border

        /// <summary>Sets all border properties at once.</summary>
        public LegendBuilder Border(string color, int width = 1, int radius = 0)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _legend.BorderColor  = color;
            _legend.BorderWidth  = width;
            _legend.BorderRadius = radius;
            return this;
        }

        /// <summary>Sets the legend box border color.</summary>
        public LegendBuilder BorderColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _legend.BorderColor = color;
            return this;
        }

        /// <summary>Sets the legend box border stroke-width (px).</summary>
        public LegendBuilder BorderWidth(int width)
        {
            if (width < 0) throw new ArgumentOutOfRangeException(nameof(width));
            _legend.BorderWidth = width;
            return this;
        }

        /// <summary>Sets the corner radius (px) of the legend border rectangle.</summary>
        public LegendBuilder BorderRadius(int radius)
        {
            if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));
            _legend.BorderRadius = radius;
            return this;
        }

        // ------------------------------------------------------------------ background

        /// <summary>Sets the background fill color of the legend box.</summary>
        public LegendBuilder BackgroundColor(string color)
        {
            if (string.IsNullOrWhiteSpace(color)) throw new ArgumentException("Color must not be empty.", nameof(color));
            _legend.BackgroundColor = color;
            return this;
        }

        // ------------------------------------------------------------------ margin

        /// <summary>
        /// Sets the outer margin (px) between the legend box and the nearest chart edge or title.
        /// Applies to left/right horizontal alignment and top vertical alignment. Default 10.
        /// </summary>
        public LegendBuilder Margin(int px)
        {
            if (px < 0) throw new ArgumentOutOfRangeException(nameof(px), "Margin must be >= 0.");
            _legend.Margin = px;
            return this;
        }
    }
}
