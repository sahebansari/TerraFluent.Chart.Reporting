namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// Defines the visual palette, typography and background colours applied to a chart.
    /// Apply via <c>ChartBuilder.WithTheme(ChartTheme.Dark)</c>.
    /// </summary>
    public class ChartTheme
    {
        // ------------------------------------------------------------------ built-in themes

        /// <summary>HighCharts-inspired default theme (white background, blue-orange palette).</summary>
        public static readonly ChartTheme Default = new ChartTheme();

        /// <summary>Dark theme (navy background, neon-adjacent palette).</summary>
        public static readonly ChartTheme Dark = new ChartTheme
        {
            BackgroundColor     = ChartColor.MidnightNavy,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.NightSlate,
            TextColor           = ChartColor.LavenderMist,
            AxisLineColor       = ChartColor.DuskyIndigo,
            Colors              = ChartColor.Palette.Dark,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.MidnightNavy, 0.94),
            TooltipTextColor    = ChartColor.LavenderWhite,
            PositiveColor       = ChartColor.FlatEmerald,
            NegativeColor       = ChartColor.CoralRed,
            AccentColor         = ChartColor.SoftLavender
        };

        /// <summary>Soft pastel theme (off-white background, muted palette).</summary>
        public static readonly ChartTheme Pastel = new ChartTheme
        {
            BackgroundColor     = ChartColor.OffWhite,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.PlatinumGray,
            TextColor           = ChartColor.CoolGray,
            AxisLineColor       = ChartColor.PlatinumGray,
            Colors              = ChartColor.Palette.Pastel,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.DuskyIndigo, 0.90),
            TooltipTextColor    = ChartColor.Black,
            PositiveColor       = ChartColor.SoftMintGreen,
            NegativeColor       = ChartColor.SoftSalmon,
            AccentColor         = ChartColor.LavenderPurple
        };

        /// <summary>High-contrast monochrome theme suitable for print or greyscale export.</summary>
        public static readonly ChartTheme Monochrome = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.PearlGray,
            TextColor           = ChartColor.Black,
            AxisLineColor       = ChartColor.AshGray,
            Colors              = ChartColor.Palette.Monochrome,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.CoolGray, 0.88),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.GraphiteGray,
            NegativeColor       = ChartColor.AshGray,
            AccentColor         = ChartColor.CoolCharcoal
        };

        /// <summary>Deep ocean theme (near-black navy background, blue-teal palette).</summary>
        public static readonly ChartTheme Ocean = new ChartTheme
        {
            BackgroundColor     = ChartColor.OceanDepth,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.OceanGrid,
            TextColor           = ChartColor.AzureHaze,
            AxisLineColor       = ChartColor.SteelBlue,
            Colors              = ChartColor.Palette.Ocean,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.OceanDepth, 0.94),
            TooltipTextColor    = ChartColor.SoftAquaBlue,
            PositiveColor       = ChartColor.OceanGreen,
            NegativeColor       = ChartColor.CoralRed,
            AccentColor         = ChartColor.BrightSkyBlue
        };

        /// <summary>Warm sunset theme (deep purple-navy background, vivid warm palette).</summary>
        public static readonly ChartTheme Sunset = new ChartTheme
        {
            BackgroundColor     = ChartColor.SunsetDusk,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.SunsetGrid,
            TextColor           = ChartColor.SunsetGlow,
            AxisLineColor       = ChartColor.DuskyRose,
            Colors              = ChartColor.Palette.Sunset,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.SunsetDusk, 0.94),
            TooltipTextColor    = ChartColor.PeachGlow,
            PositiveColor       = ChartColor.WarmAmber,
            NegativeColor       = ChartColor.BrightRose,
            AccentColor         = ChartColor.BrightFuchsia
        };

        /// <summary>Natural forest theme (warm cream background, earthy green palette).</summary>
        public static readonly ChartTheme Forest = new ChartTheme
        {
            BackgroundColor     = ChartColor.ForestCream,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.MossyGreen,
            TextColor           = ChartColor.EarthBrown,
            AxisLineColor       = ChartColor.FernGreen,
            Colors              = ChartColor.Palette.Forest,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.EarthBrown, 0.90),
            TooltipTextColor    = ChartColor.FernMist,
            PositiveColor       = ChartColor.SignalGreen,
            NegativeColor       = ChartColor.NegativeRed,
            AccentColor         = ChartColor.BrightEmerald
        };

        /// <summary>High-contrast neon theme (near-black background, electric palette).</summary>
        public static readonly ChartTheme Neon = new ChartTheme
        {
            BackgroundColor     = ChartColor.NeonDark,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.NeonGridDark,
            TextColor           = ChartColor.LavenderMist,
            AxisLineColor       = ChartColor.DimCharcoal,
            Colors              = ChartColor.Palette.Neon,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.NeonDark, 0.96),
            TooltipTextColor    = ChartColor.SoftLavenderGlow,
            PositiveColor       = ChartColor.NeonGreen,
            NegativeColor       = ChartColor.NeonRoseRed,
            AccentColor         = ChartColor.NeonAqua
        };

        /// <summary>Clean minimal theme (white background, muted professional palette).</summary>
        public static readonly ChartTheme Minimal = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.FrostGray,
            TextColor           = ChartColor.GraphiteGray,
            AxisLineColor       = ChartColor.LightSilver,
            Colors              = ChartColor.Palette.Minimal,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.GraphiteGray, 0.88),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.SignalGreen,
            NegativeColor       = ChartColor.WarningRed,
            AccentColor         = ChartColor.IndigoBlue
        };

        /// <summary>Warm earth-tones theme (parchment background, amber-brown palette).</summary>
        public static readonly ChartTheme Warm = new ChartTheme
        {
            BackgroundColor     = ChartColor.WarmParchment,
            PlotBackgroundColor = ChartColor.None,
            PositiveColor       = ChartColor.GoldenAmber,
            NegativeColor       = ChartColor.BurntRed,
            AccentColor         = ChartColor.FireOrange,
            GridLineColor       = ChartColor.WarmSand,
            TextColor           = ChartColor.EspressoBlack,
            AxisLineColor       = ChartColor.CinnamonBrown,
            Colors              = ChartColor.Palette.Warm,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.EspressoBlack, 0.90),
            TooltipTextColor    = ChartColor.WarmCream
        };

        /// <summary>Arctic theme (ice-blue background, cool crisp blue palette).</summary>
        public static readonly ChartTheme Arctic = new ChartTheme
        {
            BackgroundColor     = ChartColor.AliceBlue,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.IcyBlue,
            TextColor           = ChartColor.PolarNight,
            AxisLineColor       = ChartColor.GlacierBlue,
            Colors              = ChartColor.Palette.Arctic,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.PolarNight, 0.90),
            TooltipTextColor    = ChartColor.ArcticGlow,
            PositiveColor       = ChartColor.DeepCyanBlue,
            NegativeColor       = ChartColor.IndigoDeep,
            AccentColor         = ChartColor.ClearBlue
        };

        /// <summary>Professional business theme (white background, corporate blue-red palette).</summary>
        public static readonly ChartTheme Business = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.PlatinumGray,
            TextColor           = ChartColor.GraphiteGray,
            AxisLineColor       = ChartColor.CeruleanBlue,
            Colors              = ChartColor.Palette.Business,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.GraphiteGray, 0.90),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.EmeraldTeal,
            NegativeColor       = ChartColor.NegativeRed,
            AccentColor         = ChartColor.TrueBlue
        };

        /// <summary>Material Design theme (white background, Google Material 500-level palette).</summary>
        public static readonly ChartTheme Material = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.FrostGray,
            TextColor           = ChartColor.DimCharcoal,
            AxisLineColor       = ChartColor.MaterialBlue,
            Colors              = ChartColor.Palette.Material,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.DimCharcoal, 0.92),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.MaterialGreen,
            NegativeColor       = ChartColor.MaterialRed,
            AccentColor         = ChartColor.MaterialBlue
        };

        /// <summary>Traffic-light theme (light background, status-indicator palette).</summary>
        public static readonly ChartTheme TrafficLight = new ChartTheme
        {
            BackgroundColor     = ChartColor.OffWhite,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.PearlGray,
            TextColor           = ChartColor.GraphiteGray,
            AxisLineColor       = ChartColor.BlueGray,
            Colors              = ChartColor.Palette.TrafficLight,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.GraphiteGray, 0.90),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.SignalGreen,
            NegativeColor       = ChartColor.WarningRed,
            AccentColor         = ChartColor.SunAmber
        };

        /// <summary>Accessible theme (white background, Wong 2011 colour-blind-safe palette).</summary>
        public static readonly ChartTheme Accessible = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.SilkGray,
            TextColor           = ChartColor.GraphiteGray,
            AxisLineColor       = ChartColor.WongBlue,
            Colors              = ChartColor.Palette.Accessible,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.GraphiteGray, 0.88),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.WongGreen,
            NegativeColor       = ChartColor.Vermilion,
            AccentColor         = ChartColor.WongBlue
        };

        /// <summary>
        /// HighCharts-inspired vivid theme (white background, full-spectrum distinct palette).
        /// Every series colour pops clearly on the clean white canvas — ideal for dashboards
        /// and presentations where visual impact matters.
        /// </summary>
        public static readonly ChartTheme Vivid = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = ChartColor.GrayMist,
            TextColor           = ChartColor.WetAsphalt,
            AxisLineColor       = ChartColor.VividBlue,
            Colors              = ChartColor.Palette.Vivid,
            TooltipBackground   = ChartColor.WithOpacity(ChartColor.WetAsphalt, 0.92),
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.VividGreen,
            NegativeColor       = ChartColor.VividRed,
            AccentColor         = ChartColor.VividBlue
        };

        /// <summary>
        /// High-contrast WCAG-AA theme (white background, all palette colours maintain ≥ 4.5:1
        /// contrast ratio against white). Suitable for accessibility-critical contexts.
        /// </summary>
        public static readonly ChartTheme HighContrast = new ChartTheme
        {
            BackgroundColor     = ChartColor.White,
            PlotBackgroundColor = ChartColor.None,
            GridLineColor       = "#444444",
            TextColor           = ChartColor.Black,
            AxisLineColor       = ChartColor.Black,
            Colors              = ChartColor.Palette.HighContrast,
            TooltipBackground   = "rgba(0,0,0,0.92)",
            TooltipTextColor    = ChartColor.White,
            PositiveColor       = ChartColor.DarkGreen,
            NegativeColor       = ChartColor.DarkRed,
            AccentColor         = "#003399"
        };

        // ------------------------------------------------------------------ factory

        /// <summary>
        /// Creates a fully customised theme in a single call.
        /// Any parameter left <c>null</c> falls back to the <see cref="Default"/> value.
        /// </summary>
        /// <param name="backgroundColor">Chart background colour (hex or CSS colour string).</param>
        /// <param name="plotBackgroundColor">Plot area background; use <c>ChartColor.None</c> or a hex colour.</param>
        /// <param name="gridLineColor">Colour for axis grid lines.</param>
        /// <param name="axisLineColor">Colour for axis border lines.</param>
        /// <param name="textColor">Default text fill colour.</param>
        /// <param name="fontFamily">CSS font-family applied to all chart text.</param>
        /// <param name="colors">Ordered colour palette; at least one entry required when supplied.</param>
        /// <param name="tooltipBackground">Tooltip box fill colour. Null falls back to the default tooltip background.</param>
        /// <param name="tooltipTextColor">Tooltip text colour. Null falls back to white.</param>
        /// <param name="positiveColor">Colour used for positive/up indicators (e.g. Waterfall gains, candlestick up bodies). Null falls back to the default.</param>
        /// <param name="negativeColor">Colour used for negative/down indicators (e.g. Waterfall losses, candlestick down bodies). Null falls back to the default.</param>
        /// <param name="accentColor">Accent colour used for hover overlays and focus rings. Null falls back to the default.</param>
        public static ChartTheme Custom(
            string?   backgroundColor     = null,
            string?   plotBackgroundColor = null,
            string?   gridLineColor       = null,
            string?   axisLineColor       = null,
            string?   textColor           = null,
            string?   fontFamily          = null,
            string[]? colors              = null,
            string?   tooltipBackground   = null,
            string?   tooltipTextColor    = null,
            string?   positiveColor       = null,
            string?   negativeColor       = null,
            string?   accentColor         = null)
        {
            if (colors != null && colors.Length == 0)
                throw new System.ArgumentException("Provide at least one colour.", nameof(colors));

            return new ChartTheme
            {
                BackgroundColor     = backgroundColor     ?? Default.BackgroundColor,
                PlotBackgroundColor = plotBackgroundColor ?? Default.PlotBackgroundColor,
                GridLineColor       = gridLineColor       ?? Default.GridLineColor,
                AxisLineColor       = axisLineColor       ?? Default.AxisLineColor,
                TextColor           = textColor           ?? Default.TextColor,
                FontFamily          = fontFamily          ?? Default.FontFamily,
                Colors              = colors              ?? Default.Colors,
                TooltipBackground   = tooltipBackground   ?? Default.TooltipBackground,
                TooltipTextColor    = tooltipTextColor    ?? Default.TooltipTextColor,
                PositiveColor       = positiveColor       ?? Default.PositiveColor,
                NegativeColor       = negativeColor       ?? Default.NegativeColor,
                AccentColor         = accentColor         ?? Default.AccentColor
            };
        }

        // ------------------------------------------------------------------ properties


        /// <summary>SVG/page background colour. Default <c>#ffffff</c> (white).</summary>
        public string BackgroundColor { get; set; } = ChartColor.White;

        /// <summary>Plot area background; use <c>ChartColor.None</c> or a hex colour.</summary>
        public string PlotBackgroundColor { get; set; } = ChartColor.None;

        /// <summary>Colour for axis grid lines.</summary>
        public string GridLineColor { get; set; } = ChartColor.SilkGray;

        /// <summary>Colour for axis border lines.</summary>
        public string AxisLineColor { get; set; } = ChartColor.PaleSteelBlue;

        /// <summary>Default text fill colour (titles, labels, legends).</summary>
        public string TextColor { get; set; } = ChartColor.GraphiteGray;

        /// <summary>CSS font-family string applied to all chart text.</summary>
        public string FontFamily { get; set; } = "sans-serif";

        /// <summary>Ordered colour palette used for series and pie slices.</summary>
        public string[] Colors { get; set; } = ChartColor.Palette.Default;

        /// <summary>
        /// Tooltip box fill colour derived from the theme.
        /// Used when <see cref="TooltipOptions.BackgroundColor"/> is <c>null</c> (theme default).
        /// Default is a dark semi-transparent blue-purple that works against light chart backgrounds.
        /// </summary>
        public string TooltipBackground { get; set; } = "rgba(35,35,70,0.90)";

        /// <summary>
        /// Tooltip text colour derived from the theme.
        /// Used when <see cref="TooltipOptions.TextColor"/> is <c>null</c> (theme default).
        /// Default is white, readable on the dark tooltip background.
        /// </summary>
        public string TooltipTextColor { get; set; } = ChartColor.White;

        // ------------------------------------------------------------------ semantic chart-type colours

        /// <summary>
        /// Fill colour for positive-value <b>Waterfall</b> bars, up-body <b>Candlestick/OHLC</b> bars,
        /// AutoInsight peak-high badges, and positive-trend narrative text.
        /// Defaults to <c>#2ecc71</c> (emerald green). Override per series via
        /// <c>AddCandlestick(..., cfg =&gt; cfg.Color("#custom"))</c> which takes priority.
        /// </summary>
        public string PositiveColor { get; set; } = "#2ecc71";

        /// <summary>
        /// Fill colour for negative-value <b>Waterfall</b> bars, down-body <b>Candlestick/OHLC</b> bars,
        /// AutoInsight peak-low badges, and negative-trend narrative text.
        /// Defaults to <c>#e74c3c</c> (alizarin red).
        /// </summary>
        public string NegativeColor { get; set; } = "#e74c3c";

        /// <summary>
        /// Accent colour used for interactive hit-area hover overlays, keyboard focus rings,
        /// and gauge/ring hover-highlight arcs. Should contrast well against the series palette.
        /// Defaults to <c>#8b5cf6</c> (violet-500).
        /// </summary>
        public string AccentColor { get; set; } = "#8b5cf6";
    }
}
