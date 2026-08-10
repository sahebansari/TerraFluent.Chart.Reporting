using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for adding <see cref="Annotation"/> overlays (labels, lines, rectangles and
    /// circles) to a chart. Obtain one via <see cref="ChartBuilder.Annotations(Action{AnnotationBuilder})"/>.
    /// Coordinates are in data space: X is a category index (when the X axis has categories) or a
    /// numeric X value; Y is a value-axis value.
    /// </summary>
    public sealed class AnnotationBuilder
    {
        private readonly System.Collections.Generic.List<Annotation> _annotations;
        private readonly ChartOptions _chartOptions;

        internal AnnotationBuilder(
            System.Collections.Generic.List<Annotation> annotations,
            ChartOptions chartOptions)
        {
            _annotations = annotations;
            _chartOptions = chartOptions;
            _chartOptions.AnnotationLayout ??= new AnnotationLayoutOptions();
        }

        // ------------------------------------------------------------------ smart layout

        /// <summary>
        /// Configures collision-detection layout options. SmartLayout is enabled automatically
        /// whenever <see cref="ChartBuilder.Annotations"/> is called; use this overload only when
        /// you need to customise the layout options.
        /// </summary>
        public AnnotationBuilder SmartLayout(Action<AnnotationLayoutOptions>? configure = null)
        {
            _chartOptions.AnnotationLayout ??= new AnnotationLayoutOptions();
            configure?.Invoke(_chartOptions.AnnotationLayout);
            return this;
        }

        // ------------------------------------------------------------------ core shapes

        /// <summary>Adds a text label anchored at the data coordinate (<paramref name="x"/>, <paramref name="y"/>).</summary>
        public AnnotationBuilder Label(double x, double y, string text, Action<Annotation>? configure = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var a = new Annotation { Kind = AnnotationKind.Label, X = x, Y = y, Text = text };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>Adds a straight line from (<paramref name="x1"/>, <paramref name="y1"/>) to (<paramref name="x2"/>, <paramref name="y2"/>).</summary>
        public AnnotationBuilder Line(double x1, double y1, double x2, double y2, Action<Annotation>? configure = null)
        {
            var a = new Annotation { Kind = AnnotationKind.Line, X = x1, Y = y1, X2 = x2, Y2 = y2 };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>Adds a rectangle spanning the two data corners (<paramref name="x1"/>, <paramref name="y1"/>) and (<paramref name="x2"/>, <paramref name="y2"/>).</summary>
        public AnnotationBuilder Rect(double x1, double y1, double x2, double y2, Action<Annotation>? configure = null)
        {
            var a = new Annotation { Kind = AnnotationKind.Rect, X = x1, Y = y1, X2 = x2, Y2 = y2 };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>Adds a circle centred at (<paramref name="x"/>, <paramref name="y"/>) with a pixel radius.</summary>
        public AnnotationBuilder Circle(double x, double y, double radiusPx, Action<Annotation>? configure = null)
        {
            var a = new Annotation { Kind = AnnotationKind.Circle, X = x, Y = y, Radius = radiusPx };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        // ------------------------------------------------------------------ directional placement helpers

        /// <summary>
        /// Adds a text label positioned <paramref name="offsetPx"/> pixels above the data coordinate,
        /// centred horizontally. Use to avoid overlap when the data point sits inside a highlighted region.
        /// </summary>
        public AnnotationBuilder LabelAbove(double x, double y, string text, int offsetPx = 18, Action<Annotation>? configure = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var a = new Annotation { Kind = AnnotationKind.Label, X = x, Y = y, Text = text,
                TextAnchor = "middle", OffsetY = -offsetPx };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>
        /// Adds a text label positioned <paramref name="offsetPx"/> pixels below the data coordinate,
        /// centred horizontally.
        /// </summary>
        public AnnotationBuilder LabelBelow(double x, double y, string text, int offsetPx = 18, Action<Annotation>? configure = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var a = new Annotation { Kind = AnnotationKind.Label, X = x, Y = y, Text = text,
                TextAnchor = "middle", OffsetY = offsetPx };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>
        /// Adds a text label positioned <paramref name="offsetPx"/> pixels to the left of the data
        /// coordinate, right-aligned so the text ends at the offset position.
        /// </summary>
        public AnnotationBuilder LabelLeft(double x, double y, string text, int offsetPx = 8, Action<Annotation>? configure = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var a = new Annotation { Kind = AnnotationKind.Label, X = x, Y = y, Text = text,
                TextAnchor = "end", OffsetX = -offsetPx };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>
        /// Adds a text label positioned <paramref name="offsetPx"/> pixels to the right of the data
        /// coordinate, left-aligned so the text starts at the offset position.
        /// </summary>
        public AnnotationBuilder LabelRight(double x, double y, string text, int offsetPx = 8, Action<Annotation>? configure = null)
        {
            if (text is null) throw new ArgumentNullException(nameof(text));
            var a = new Annotation { Kind = AnnotationKind.Label, X = x, Y = y, Text = text,
                TextAnchor = "start", OffsetX = offsetPx };
            configure?.Invoke(a);
            _annotations.Add(a);
            return this;
        }

        /// <summary>Adds a pre-built annotation.</summary>
        public AnnotationBuilder Add(Annotation annotation)
        {
            if (annotation is null) throw new ArgumentNullException(nameof(annotation));
            _annotations.Add(annotation);
            return this;
        }
    }
}
