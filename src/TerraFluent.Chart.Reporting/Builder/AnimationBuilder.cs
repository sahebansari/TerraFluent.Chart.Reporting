using System;
using TerraFluent.Chart.Reporting.Models;

namespace TerraFluent.Chart.Reporting.Builder
{
    /// <summary>
    /// Fluent builder for configuring chart animation.
    /// Obtain an instance via <see cref="ChartBuilder.Animation(Action{AnimationBuilder})"/>:
    /// <code>
    /// .Animation(a => a.Duration(600).Bounce())
    /// </code>
    /// For the simple case (default 800 ms, EaseOut) use the shorthand:
    /// <code>
    /// .Animate()        // 800 ms EaseOut
    /// .Animate(600)     // 600 ms EaseOut
    /// </code>
    /// </summary>
    public sealed class AnimationBuilder
    {
        private readonly AnimationOptions _options;

        internal AnimationBuilder(AnimationOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // ------------------------------------------------------------------ duration

        /// <summary>Sets the animation duration.</summary>
        /// <param name="duration">Must be a positive, non-zero <see cref="TimeSpan"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="duration"/> is zero or negative.</exception>
        public AnimationBuilder Duration(TimeSpan duration)
        {
            if (duration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration), duration, "Animation duration must be greater than zero.");
            _options.Duration = duration;
            return this;
        }

        /// <summary>Sets the animation duration in milliseconds. Must be greater than zero.</summary>
        /// <param name="milliseconds">Duration in milliseconds. Must be greater than zero.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="milliseconds"/> is zero or negative.</exception>
        public AnimationBuilder Duration(int milliseconds)
        {
            if (milliseconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "Animation duration must be greater than zero.");
            return Duration(TimeSpan.FromMilliseconds(milliseconds));
        }

        // ------------------------------------------------------------------ easing shortcuts

        /// <summary>Uses a constant-speed linear easing.</summary>
        public AnimationBuilder Linear()    { _options.Easing = Enums.Easing.Linear;    return this; }

        /// <summary>Accelerates from rest — slow start, fast finish.</summary>
        public AnimationBuilder EaseIn()    { _options.Easing = Enums.Easing.EaseIn;    return this; }

        /// <summary>Decelerates to rest — fast start, slow finish (default).</summary>
        public AnimationBuilder EaseOut()   { _options.Easing = Enums.Easing.EaseOut;   return this; }

        /// <summary>Slow start, fast middle, slow finish.</summary>
        public AnimationBuilder EaseInOut() { _options.Easing = Enums.Easing.EaseInOut; return this; }

        /// <summary>Overshoots and bounces at the end of the animation.</summary>
        public AnimationBuilder Bounce()    { _options.Easing = Enums.Easing.Bounce;    return this; }

        /// <summary>Spring-like oscillation at the end of the animation.</summary>
        public AnimationBuilder Elastic()   { _options.Easing = Enums.Easing.Elastic;   return this; }
    }
}
