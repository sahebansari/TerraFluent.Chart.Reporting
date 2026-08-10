namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>The kind of shape an <see cref="Annotation"/> draws on the plot area.</summary>
    public enum AnnotationKind
    {
        /// <summary>A free text label (optionally boxed) anchored at a data coordinate.</summary>
        Label,
        /// <summary>A straight line segment between two data coordinates.</summary>
        Line,
        /// <summary>A rectangle spanning two data coordinates.</summary>
        Rect,
        /// <summary>A circle centred on a data coordinate with a pixel radius.</summary>
        Circle
    }

    /// <summary>
    /// A free-form annotation drawn on top of the plot area at data coordinates. Use annotations to
    /// call out points of interest with text labels, connector lines, highlight rectangles or circles.
    /// Coordinates are expressed in data space: <see cref="X"/>/<see cref="X2"/> are category indices
    /// (when the X axis has categories) or numeric X values otherwise; <see cref="Y"/>/<see cref="Y2"/>
    /// are value-axis values. Add via <c>ChartBuilder.Annotations(...)</c>.
    /// </summary>
    public class Annotation
    {
        /// <summary>The shape this annotation renders. Defaults to <see cref="AnnotationKind.Label"/>.</summary>
        public AnnotationKind Kind { get; set; } = AnnotationKind.Label;

        /// <summary>Text to display (for <see cref="AnnotationKind.Label"/>, or an optional caption on shapes).</summary>
        public string? Text { get; set; }

        /// <summary>Primary X coordinate (category index or numeric X value).</summary>
        public double X { get; set; }

        /// <summary>Primary Y coordinate (value-axis value).</summary>
        public double Y { get; set; }

        /// <summary>Secondary X coordinate — the line end or the opposite rectangle corner.</summary>
        public double X2 { get; set; }

        /// <summary>Secondary Y coordinate — the line end or the opposite rectangle corner.</summary>
        public double Y2 { get; set; }

        /// <summary>Circle radius in pixels (for <see cref="AnnotationKind.Circle"/>).</summary>
        public double Radius { get; set; } = 8;

        /// <summary>Stroke / text colour. <c>null</c> inherits the theme's <c>TextColor</c>.</summary>
        public string? Color { get; set; }

        /// <summary>Optional fill for label boxes, rectangles and circles. <c>null</c> = no fill (transparent).</summary>
        public string? BackgroundColor { get; set; }

        /// <summary>Stroke width in pixels for line, rect and circle outlines.</summary>
        public int StrokeWidth { get; set; } = 1;

        /// <summary>Optional HighCharts-style dash pattern (e.g. "Dash", "Dot", "LongDash").</summary>
        public string? DashStyle { get; set; }

        /// <summary>Font size in pixels for label text.</summary>
        public int FontSize { get; set; } = 12;

        /// <summary>Horizontal text anchor for labels: "start", "middle" (default) or "end".</summary>
        public string TextAnchor { get; set; } = "middle";

        /// <summary>Pixel offset applied to the label X position after coordinate mapping.</summary>
        public double OffsetX { get; set; }

        /// <summary>Pixel offset applied to the label Y position after coordinate mapping.</summary>
        public double OffsetY { get; set; }

        /// <summary>Creates a deep copy of this annotation.</summary>
        public Annotation Clone() => new Annotation
        {
            Kind            = Kind,
            Text            = Text,
            X               = X,
            Y               = Y,
            X2              = X2,
            Y2              = Y2,
            Radius          = Radius,
            Color           = Color,
            BackgroundColor = BackgroundColor,
            StrokeWidth     = StrokeWidth,
            DashStyle       = DashStyle,
            FontSize        = FontSize,
            TextAnchor      = TextAnchor,
            OffsetX         = OffsetX,
            OffsetY         = OffsetY
        };
    }
}
