namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Controls automatic collision detection and smart repositioning for free-form
    /// annotation labels. Enabled automatically whenever
    /// <see cref="TerraFluent.Chart.Reporting.Builder.ChartBuilder.Annotations"/> is called;
    /// use <c>AnnotationBuilder.SmartLayout(cfg => …)</c> only when customising the layout options.
    /// </summary>
    public sealed class AnnotationLayoutOptions
    {
        /// <summary>Whether to run the collision-detection pass. Default <c>true</c>.</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Minimum horizontal gap in pixels between adjacent label bounding boxes
        /// and obstacle shapes during collision checks. Default 6.
        /// </summary>
        public int HorizontalPadding { get; set; } = 6;

        /// <summary>
        /// Minimum vertical gap in pixels between label bounding boxes and obstacle
        /// shapes during collision checks. Default 4.
        /// </summary>
        public int VerticalPadding { get; set; } = 4;

        /// <summary>
        /// When <c>true</c> (default), a thin dashed leader line is drawn from the
        /// original data-point anchor to the relocated label whenever the label is
        /// nudged from its initial position.
        /// </summary>
        public bool DrawConnectors { get; set; } = true;

        /// <summary>
        /// Opacity of the connector leader line (0.0 – 1.0). Default 0.45.
        /// </summary>
        public double ConnectorOpacity { get; set; } = 0.45;

        internal AnnotationLayoutOptions Clone() => new AnnotationLayoutOptions
        {
            Enabled           = Enabled,
            HorizontalPadding = HorizontalPadding,
            VerticalPadding   = VerticalPadding,
            DrawConnectors    = DrawConnectors,
            ConnectorOpacity  = ConnectorOpacity,
        };
    }
}
