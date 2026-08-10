using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for axis label layout — prevents overlapping via rotation,
    /// word-wrap, skipping, dynamic font scaling, collision detection, and stagger.
    /// Obtain via <see cref="ChartBuilder.LabelLayout(Action{LabelLayoutBuilder})"/>.
    /// </summary>
    public sealed class LabelLayoutBuilder
    {
        private readonly LabelLayoutOptions _opts;

        internal LabelLayoutBuilder(LabelLayoutOptions opts)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts));
        }

        // ------------------------------------------------------------------ rotation

        /// <summary>
        /// Sets an explicit rotation angle (degrees) applied to all X-axis category labels.
        /// Negative = counter-clockwise. E.g. <c>-45</c> for diagonal, <c>-90</c> for vertical.
        /// Overrides auto-rotation.
        /// </summary>
        public LabelLayoutBuilder Rotation(int degrees)
        {
            _opts.Rotation   = degrees;
            _opts.AutoRotate = false;
            return this;
        }

        /// <summary>
        /// Enables automatic label rotation. The renderer chooses the smallest angle that
        /// eliminates overlaps, up to <paramref name="maxDegrees"/> (default 90).
        /// </summary>
        public LabelLayoutBuilder AutoRotate(int maxDegrees = 90)
        {
            if (maxDegrees < 0 || maxDegrees > 90)
                throw new ArgumentOutOfRangeException(nameof(maxDegrees), maxDegrees, "maxDegrees must be between 0 and 90.");
            _opts.AutoRotate   = true;
            _opts.Rotation     = null;
            _opts.MaxRotation  = maxDegrees;
            return this;
        }

        /// <summary>Disables automatic rotation. Labels stay horizontal unless an explicit <see cref="Rotation"/> is set.</summary>
        public LabelLayoutBuilder NoRotation()
        {
            _opts.AutoRotate = false;
            _opts.Rotation   = 0;
            return this;
        }

        // ------------------------------------------------------------------ wrapping

        /// <summary>
        /// Enables word-wrapping of long labels into multiple lines.
        /// Labels are split at whitespace boundaries; each line is capped at
        /// <paramref name="maxCharsPerLine"/> characters.
        /// </summary>
        public LabelLayoutBuilder Wrap(int maxCharsPerLine = 12)
        {
            if (maxCharsPerLine < 1)
                throw new ArgumentOutOfRangeException(nameof(maxCharsPerLine), maxCharsPerLine, "maxCharsPerLine must be at least 1.");
            _opts.WordWrap        = true;
            _opts.MaxCharsPerLine = maxCharsPerLine;
            return this;
        }

        /// <summary>Disables word-wrapping.</summary>
        public LabelLayoutBuilder NoWrap()
        {
            _opts.WordWrap = false;
            return this;
        }

        // ------------------------------------------------------------------ skipping

        /// <summary>
        /// Renders only every <paramref name="n"/>-th label, skipping the rest.
        /// For example <c>Skip(2)</c> shows every other label.
        /// </summary>
        public LabelLayoutBuilder Skip(int n)
        {
            if (n < 1)
                throw new ArgumentOutOfRangeException(nameof(n), n, "Stride must be at least 1.");
            _opts.Stride   = n;
            _opts.AutoSkip = false;
            return this;
        }

        /// <summary>
        /// Enables automatic label skipping. The renderer skips labels when they still
        /// overlap after font scaling and rotation.
        /// </summary>
        public LabelLayoutBuilder AutoSkip(bool enabled = true)
        {
            _opts.AutoSkip = enabled;
            return this;
        }

        // ------------------------------------------------------------------ dynamic font scaling

        /// <summary>
        /// Configures the font size range used by dynamic scaling.
        /// The renderer starts at <paramref name="max"/> px and scales down toward
        /// <paramref name="min"/> px before falling back to rotation or skipping.
        /// </summary>
        public LabelLayoutBuilder FontSizeRange(int min, int max)
        {
            if (min < 1)
                throw new ArgumentOutOfRangeException(nameof(min), min, "min font size must be at least 1.");
            if (max < min)
                throw new ArgumentOutOfRangeException(nameof(max), max, "max font size must be >= min font size.");
            _opts.MinFontSize = min;
            _opts.MaxFontSize = max;
            return this;
        }

        /// <summary>Disables automatic font scaling; labels always render at the default size.</summary>
        public LabelLayoutBuilder NoAutoScale()
        {
            _opts.AutoScale = false;
            return this;
        }

        // ------------------------------------------------------------------ collision detection

        /// <summary>Enables bounding-box collision detection (default). The renderer resolves overlaps automatically.</summary>
        public LabelLayoutBuilder EnableCollisionDetection()
        {
            _opts.CollisionDetection = true;
            return this;
        }

        /// <summary>Disables collision detection. Labels render at their natural positions regardless of overlap.</summary>
        public LabelLayoutBuilder DisableCollisionDetection()
        {
            _opts.CollisionDetection = false;
            return this;
        }

        // ------------------------------------------------------------------ smart positioning

        /// <summary>
        /// Enables staggered (alternating two-row) rendering of dense category labels.
        /// Every other label is shifted down by <paramref name="offsetPx"/> pixels to
        /// provide more horizontal breathing room.
        /// </summary>
        public LabelLayoutBuilder Stagger(int offsetPx = 10)
        {
            if (offsetPx < 0)
                throw new ArgumentOutOfRangeException(nameof(offsetPx), offsetPx, "Stagger offset must be non-negative.");
            _opts.Stagger       = true;
            _opts.StaggerOffset = offsetPx;
            return this;
        }

        /// <summary>Disables stagger positioning.</summary>
        public LabelLayoutBuilder NoStagger()
        {
            _opts.Stagger = false;
            return this;
        }

        /// <summary>
        /// Sets the minimum horizontal gap (in pixels) between adjacent label bounding boxes
        /// during collision detection. Default is 4.
        /// </summary>
        public LabelLayoutBuilder HorizontalPadding(int px)
        {
            if (px < 0)
                throw new ArgumentOutOfRangeException(nameof(px), px, "HorizontalPadding must be non-negative.");
            _opts.HorizontalPadding = px;
            return this;
        }
    }
}
