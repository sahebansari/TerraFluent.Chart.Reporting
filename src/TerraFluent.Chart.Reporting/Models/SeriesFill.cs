using System.Collections.Generic;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>Kind of paint used to fill a series' filled shapes (area, column, bar).</summary>
    public enum SeriesFillKind
    {
        /// <summary>A linear (directional) gradient between two or more colour stops.</summary>
        LinearGradient,
        /// <summary>A radial gradient emanating from the centre outward.</summary>
        RadialGradient,
        /// <summary>A tiling pattern (dots, lines, grid, etc.).</summary>
        Pattern
    }

    /// <summary>Built-in geometric patterns for <see cref="SeriesFillKind.Pattern"/> fills.</summary>
    public enum PatternKind
    {
        /// <summary>Evenly spaced filled dots.</summary>
        Dots,
        /// <summary>45° diagonal lines (bottom-left to top-right).</summary>
        DiagonalLines,
        /// <summary>Horizontal lines.</summary>
        HorizontalLines,
        /// <summary>Vertical lines.</summary>
        VerticalLines,
        /// <summary>Square grid (horizontal + vertical lines).</summary>
        Grid,
        /// <summary>Crossed 45° diagonal lines.</summary>
        CrossHatch
    }

    /// <summary>A single colour stop within a gradient.</summary>
    public sealed class GradientStop
    {
        /// <summary>Position along the gradient, 0.0 (start) to 1.0 (end).</summary>
        public double Offset { get; set; }

        /// <summary>CSS colour of the stop.</summary>
        public string Color { get; set; } = "#000000";

        /// <summary>Stop opacity, 0.0 (transparent) to 1.0 (opaque). Defaults to 1.0.</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Creates an empty gradient stop.</summary>
        public GradientStop() { }

        /// <summary>Creates a gradient stop at <paramref name="offset"/> with the given colour and opacity.</summary>
        public GradientStop(double offset, string color, double opacity = 1.0)
        {
            Offset = offset;
            Color = color;
            Opacity = opacity;
        }
    }

    /// <summary>
    /// Describes a gradient or pattern paint applied to a series' filled shapes. Attach to a series
    /// via the fluent <c>SeriesBuilder</c> fill helpers; the renderer emits the corresponding SVG
    /// <c>&lt;linearGradient&gt;</c>, <c>&lt;radialGradient&gt;</c>, or <c>&lt;pattern&gt;</c> in a
    /// <c>&lt;defs&gt;</c> block and references it from the shape's <c>fill</c> attribute.
    /// </summary>
    public sealed class SeriesFill
    {
        /// <summary>Which kind of paint this fill produces.</summary>
        public SeriesFillKind Kind { get; set; }

        /// <summary>
        /// Direction of a <see cref="SeriesFillKind.LinearGradient"/> in degrees. 90 = top→bottom
        /// (the default), 0 = left→right, 45 = diagonal. Ignored for radial/pattern fills.
        /// </summary>
        public int Angle { get; set; } = 90;

        /// <summary>Colour stops for gradient fills (ordered by offset). Ignored for pattern fills.</summary>
        public List<GradientStop> Stops { get; set; } = new List<GradientStop>();

        /// <summary>Geometric pattern for <see cref="SeriesFillKind.Pattern"/> fills.</summary>
        public PatternKind Pattern { get; set; }

        /// <summary>Foreground (line/dot) colour of a pattern fill.</summary>
        public string PatternForeground { get; set; } = "#000000";

        /// <summary>Background colour of a pattern fill. Null = transparent tile.</summary>
        public string? PatternBackground { get; set; }

        /// <summary>Tile size in pixels for a pattern fill. Defaults to 8.</summary>
        public double PatternSize { get; set; } = 8;

        /// <summary>Overall opacity multiplier applied to the fill (0..1). Defaults to 1.0.</summary>
        public double Opacity { get; set; } = 1.0;

        /// <summary>Creates a linear gradient fill at the given angle from the supplied stops.</summary>
        public static SeriesFill Linear(int angleDegrees, params GradientStop[] stops)
        {
            var f = new SeriesFill { Kind = SeriesFillKind.LinearGradient, Angle = angleDegrees };
            f.Stops.AddRange(stops);
            return f;
        }

        /// <summary>Creates a radial gradient fill from the supplied stops.</summary>
        public static SeriesFill Radial(params GradientStop[] stops)
        {
            var f = new SeriesFill { Kind = SeriesFillKind.RadialGradient };
            f.Stops.AddRange(stops);
            return f;
        }

        /// <summary>Creates a pattern fill of the given kind and colours.</summary>
        public static SeriesFill PatternFill(PatternKind pattern, string foreground,
            string? background = null, double size = 8)
            => new SeriesFill
            {
                Kind = SeriesFillKind.Pattern,
                Pattern = pattern,
                PatternForeground = foreground,
                PatternBackground = background,
                PatternSize = size
            };

        /// <summary>Deep-copies this fill.</summary>
        public SeriesFill Clone()
        {
            var c = new SeriesFill
            {
                Kind = Kind,
                Angle = Angle,
                Pattern = Pattern,
                PatternForeground = PatternForeground,
                PatternBackground = PatternBackground,
                PatternSize = PatternSize,
                Opacity = Opacity
            };
            foreach (var s in Stops)
                c.Stops.Add(new GradientStop(s.Offset, s.Color, s.Opacity));
            return c;
        }
    }
}
