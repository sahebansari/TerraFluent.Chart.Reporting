namespace TerraFluent.Chart.Reporting.Enums
{
    /// <summary>
    /// Easing functions for chart animations.
    /// </summary>
    public enum Easing
    {
        /// <summary>Constant rate — no acceleration or deceleration.</summary>
        Linear,
        /// <summary>Starts slow, accelerates toward the end.</summary>
        EaseIn,
        /// <summary>Starts fast, decelerates toward the end.</summary>
        EaseOut,
        /// <summary>Starts slow, accelerates through the middle, then decelerates at the end.</summary>
        EaseInOut,
        /// <summary>Overshoots and bounces at the end of the animation.</summary>
        Bounce,
        /// <summary>Oscillates past the target value, simulating an elastic spring.</summary>
        Elastic
    }
}
