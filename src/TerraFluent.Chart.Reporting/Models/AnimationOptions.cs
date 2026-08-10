using System;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Animation configuration for chart load effects.
    /// </summary>
    public class AnimationOptions
    {
        /// <summary>Whether load animation is enabled. Default <c>true</c>.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Total duration of the load animation. Must be greater than zero when animation is enabled.
        /// Default is 800 ms.
        /// </summary>
        public TimeSpan Duration { get; set; } = TimeSpan.FromMilliseconds(800);

        /// <summary>
        /// Easing function applied to the animation.
        /// <see cref="Enums.Easing.Bounce"/> and <see cref="Enums.Easing.Elastic"/> are approximated
        /// as <see cref="Enums.Easing.EaseOut"/> in SMIL output.
        /// Default is <see cref="Enums.Easing.EaseOut"/>.
        /// </summary>
        public Enums.Easing Easing { get; set; } = Enums.Easing.EaseOut;
    }
}
