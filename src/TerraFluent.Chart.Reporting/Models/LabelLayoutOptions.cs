namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Controls how axis category labels are automatically laid out to prevent
    /// overlapping. Apply via <c>ChartBuilder.LabelLayout(cfg =&gt; ...)</c>.
    /// </summary>
    public sealed class LabelLayoutOptions
    {
        // ------------------------------------------------------------------ rotation

        /// <summary>
        /// Explicit rotation angle in degrees for all category labels.
        /// Negative = counter-clockwise (e.g. <c>-45</c> = diagonal, <c>-90</c> = vertical).
        /// <c>null</c> = determined by <see cref="AutoRotate"/> / collision detection.
        /// </summary>
        public int? Rotation { get; set; }

        /// <summary>
        /// When <c>true</c> (default), the renderer automatically rotates labels when they
        /// would overlap at their natural horizontal angle.
        /// </summary>
        public bool AutoRotate { get; set; } = true;

        /// <summary>
        /// Absolute maximum rotation angle the auto-rotate algorithm may apply, in degrees (0–90).
        /// Default is 90 (allows vertical labels).
        /// </summary>
        public int MaxRotation { get; set; } = 90;

        // ------------------------------------------------------------------ wrapping

        /// <summary>
        /// When <c>true</c>, long labels are word-wrapped at whitespace boundaries before
        /// layout is applied (multi-line SVG text via <c>&lt;tspan&gt;</c> elements).
        /// Default <c>false</c>.
        /// </summary>
        public bool WordWrap { get; set; } = false;

        /// <summary>
        /// Maximum number of characters per wrapped line.
        /// Ignored when <see cref="WordWrap"/> is <c>false</c>. Default 12.
        /// </summary>
        public int MaxCharsPerLine { get; set; } = 12;

        // ------------------------------------------------------------------ skipping

        /// <summary>
        /// When &gt; 1, only every <em>n</em>-th label is drawn (stride).
        /// <c>null</c> or <c>1</c> = show every label; the renderer may override this
        /// automatically when <see cref="AutoSkip"/> is <c>true</c>.
        /// </summary>
        public int? Stride { get; set; }

        /// <summary>
        /// When <c>true</c> (default), the renderer automatically skips labels when
        /// they still overlap even after rotation and font scaling.
        /// </summary>
        public bool AutoSkip { get; set; } = true;

        // ------------------------------------------------------------------ dynamic font scaling

        /// <summary>
        /// Minimum font size in pixels the auto-scaler may shrink to. Default 8.
        /// </summary>
        public int MinFontSize { get; set; } = 8;

        /// <summary>
        /// Maximum / base font size in pixels. Default 11.
        /// The renderer starts at this size and scales down toward <see cref="MinFontSize"/>
        /// as needed.
        /// </summary>
        public int MaxFontSize { get; set; } = 11;

        /// <summary>
        /// When <c>true</c> (default), the renderer automatically reduces the font size before
        /// falling back to rotation or skipping.
        /// </summary>
        public bool AutoScale { get; set; } = true;

        // ------------------------------------------------------------------ collision detection

        /// <summary>
        /// When <c>true</c> (default), the renderer runs a bounding-box collision pass
        /// over estimated label extents and applies the best strategy (font scale →
        /// rotation → skip) to resolve overlaps.
        /// Disable to fall back to the legacy static behaviour.
        /// </summary>
        public bool CollisionDetection { get; set; } = true;

        // ------------------------------------------------------------------ smart positioning

        /// <summary>
        /// When <c>true</c>, alternating labels are offset vertically (staggered rows)
        /// to create extra horizontal breathing room for dense axes.
        /// Default <c>false</c>.
        /// </summary>
        public bool Stagger { get; set; } = false;

        /// <summary>
        /// Vertical stagger offset in pixels applied to every other label row.
        /// Ignored when <see cref="Stagger"/> is <c>false</c>. Default 10.
        /// </summary>
        public int StaggerOffset { get; set; } = 10;

        /// <summary>
        /// Horizontal padding in pixels added between adjacent estimated label bounding boxes
        /// during collision detection. Increase for looser spacing. Default 4.
        /// </summary>
        public int HorizontalPadding { get; set; } = 4;

        // ------------------------------------------------------------------ clone

        internal LabelLayoutOptions Clone() => new LabelLayoutOptions
        {
            Rotation          = Rotation,
            AutoRotate        = AutoRotate,
            MaxRotation       = MaxRotation,
            WordWrap          = WordWrap,
            MaxCharsPerLine   = MaxCharsPerLine,
            Stride            = Stride,
            AutoSkip          = AutoSkip,
            MinFontSize       = MinFontSize,
            MaxFontSize       = MaxFontSize,
            AutoScale         = AutoScale,
            CollisionDetection = CollisionDetection,
            Stagger           = Stagger,
            StaggerOffset     = StaggerOffset,
            HorizontalPadding = HorizontalPadding,
        };
    }
}
