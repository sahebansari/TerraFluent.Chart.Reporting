using System;
using System.Globalization;

namespace TerraFluent.Chart.Reporting.Models
{
    /// <summary>
    /// A catalogue of named CSS colours and utility methods for building colour strings.
    /// <para>
    /// Use these constants anywhere a colour string is accepted — <c>cfg.Color(ChartColor.SteelBlue)</c>,
    /// <c>.Background(ChartColor.Gold)</c>, <c>.BorderColor(ChartColor.DarkSlateGray)</c>, etc.
    /// </para>
    /// </summary>
    public static class ChartColor
    {
        // =================================================================
        // Reds
        // =================================================================

        /// <summary>#FF0000 — pure red.</summary>
        public const string Red = "#FF0000";

        /// <summary>#8B0000 — deep dark red.</summary>
        public const string DarkRed = "#8B0000";

        /// <summary>#DC143C — strong, vivid red.</summary>
        public const string Crimson = "#DC143C";

        /// <summary>#B22222 — medium dark red-brown.</summary>
        public const string Firebrick = "#B22222";

        /// <summary>#CD5C5C — medium-light red.</summary>
        public const string IndianRed = "#CD5C5C";

        /// <summary>#F08080 — light pastel red.</summary>
        public const string LightCoral = "#F08080";

        /// <summary>#FA8072 — orange-tinted salmon.</summary>
        public const string Salmon = "#FA8072";

        /// <summary>#E9967A — muted salmon.</summary>
        public const string DarkSalmon = "#E9967A";

        /// <summary>#FF7F50 — orange-red coral.</summary>
        public const string Coral = "#FF7F50";

        /// <summary>#FF6347 — warm tomato red.</summary>
        public const string Tomato = "#FF6347";

        /// <summary>#FF4500 — vivid orange-red.</summary>
        public const string OrangeRed = "#FF4500";

        /// <summary>#FFE4E1 — very pale pinkish white.</summary>
        public const string MistyRose = "#FFE4E1";

        /// <summary>#FFF5EE — pale seashell tint.</summary>
        public const string Seashell = "#FFF5EE";

        // =================================================================
        // Pinks
        // =================================================================

        /// <summary>#FFC0CB — classic pink.</summary>
        public const string Pink = "#FFC0CB";

        /// <summary>#FFB6C1 — light pink.</summary>
        public const string LightPink = "#FFB6C1";

        /// <summary>#FF69B4 — vibrant hot pink.</summary>
        public const string HotPink = "#FF69B4";

        /// <summary>#FF1493 — strong deep pink / magenta-pink.</summary>
        public const string DeepPink = "#FF1493";

        /// <summary>#DB7093 — soft rosy medium pink.</summary>
        public const string PaleVioletRed = "#DB7093";

        /// <summary>#C71585 — deep violet-pink.</summary>
        public const string MediumVioletRed = "#C71585";

        /// <summary>#FF00FF — pure magenta / fuchsia.</summary>
        public const string Magenta = "#FF00FF";

        /// <summary>#FF00FF — alias for <see cref="Magenta"/>.</summary>
        public const string Fuchsia = "#FF00FF";

        /// <summary>#FFF0F5 — very pale lavender-pink.</summary>
        public const string LavenderBlush = "#FFF0F5";

        // =================================================================
        // Oranges
        // =================================================================

        /// <summary>#FFA500 — pure orange.</summary>
        public const string Orange = "#FFA500";

        /// <summary>#FF8C00 — rich dark orange.</summary>
        public const string DarkOrange = "#FF8C00";

        /// <summary>#FFDAB9 — pale peachy orange.</summary>
        public const string PeachPuff = "#FFDAB9";

        /// <summary>#FFE4C4 — light bisque.</summary>
        public const string Bisque = "#FFE4C4";

        /// <summary>#FFE4B5 — pale moccasin.</summary>
        public const string Moccasin = "#FFE4B5";

        /// <summary>#FAEBD7 — warm antique white.</summary>
        public const string AntiqueWhite = "#FAEBD7";

        // =================================================================
        // Yellows
        // =================================================================

        /// <summary>#FFFF00 — pure yellow.</summary>
        public const string Yellow = "#FFFF00";

        /// <summary>#FFD700 — rich golden yellow.</summary>
        public const string Gold = "#FFD700";

        /// <summary>#DAA520 — medium golden brown.</summary>
        public const string Goldenrod = "#DAA520";

        /// <summary>#B8860B — dark golden brown.</summary>
        public const string DarkGoldenrod = "#B8860B";

        /// <summary>#FFFFE0 — very pale yellow.</summary>
        public const string LightYellow = "#FFFFE0";

        /// <summary>#FFFACD — pale lemon yellow.</summary>
        public const string LemonChiffon = "#FFFACD";

        /// <summary>#FAFAD2 — light golden-yellow.</summary>
        public const string LightGoldenrodYellow = "#FAFAD2";

        /// <summary>#EEE8AA — pale olive-yellow.</summary>
        public const string PaleGoldenrod = "#EEE8AA";

        /// <summary>#F0E68C — medium pastel yellow.</summary>
        public const string Khaki = "#F0E68C";

        /// <summary>#BDB76B — olive-yellow.</summary>
        public const string DarkKhaki = "#BDB76B";

        /// <summary>#FFFFF0 — ivory off-white with yellow tint.</summary>
        public const string Ivory = "#FFFFF0";

        /// <summary>#FFF8DC — pale cornsilk yellow.</summary>
        public const string Cornsilk = "#FFF8DC";

        // =================================================================
        // Greens
        // =================================================================

        /// <summary>#008000 — standard web green.</summary>
        public const string Green = "#008000";

        /// <summary>#006400 — deep forest green.</summary>
        public const string DarkGreen = "#006400";

        /// <summary>#228B22 — forest green.</summary>
        public const string ForestGreen = "#228B22";

        /// <summary>#2E8B57 — medium sea green.</summary>
        public const string SeaGreen = "#2E8B57";

        /// <summary>#3CB371 — medium green.</summary>
        public const string MediumSeaGreen = "#3CB371";

        /// <summary>#00FF7F — bright spring green.</summary>
        public const string SpringGreen = "#00FF7F";

        /// <summary>#00FA9A — medium spring green.</summary>
        public const string MediumSpringGreen = "#00FA9A";

        /// <summary>#7FFF00 — yellow-green chartreuse.</summary>
        public const string Chartreuse = "#7FFF00";

        /// <summary>#9ACD32 — yellow-green.</summary>
        public const string YellowGreen = "#9ACD32";

        /// <summary>#ADFF2F — vivid green-yellow.</summary>
        public const string GreenYellow = "#ADFF2F";

        /// <summary>#00FF00 — pure lime (maximum green).</summary>
        public const string Lime = "#00FF00";

        /// <summary>#32CD32 — bright lime green.</summary>
        public const string LimeGreen = "#32CD32";

        /// <summary>#808000 — dark olive-yellow.</summary>
        public const string Olive = "#808000";

        /// <summary>#6B8E23 — olive drab.</summary>
        public const string OliveDrab = "#6B8E23";

        /// <summary>#556B2F — dark olive green.</summary>
        public const string DarkOliveGreen = "#556B2F";

        /// <summary>#90EE90 — light green.</summary>
        public const string LightGreen = "#90EE90";

        /// <summary>#98FB98 — pale green.</summary>
        public const string PaleGreen = "#98FB98";

        /// <summary>#8FBC8F — muted sea-green.</summary>
        public const string DarkSeaGreen = "#8FBC8F";

        /// <summary>#66CDAA — medium aquamarine.</summary>
        public const string MediumAquamarine = "#66CDAA";

        /// <summary>#7FFFD4 — aquamarine.</summary>
        public const string Aquamarine = "#7FFFD4";

        // =================================================================
        // Teals / Cyans
        // =================================================================

        /// <summary>#008080 — standard teal.</summary>
        public const string Teal = "#008080";

        /// <summary>#00FFFF — pure cyan / aqua.</summary>
        public const string Cyan = "#00FFFF";

        /// <summary>#00FFFF — alias for <see cref="Cyan"/>.</summary>
        public const string Aqua = "#00FFFF";

        /// <summary>#008B8B — dark cyan.</summary>
        public const string DarkCyan = "#008B8B";

        /// <summary>#E0FFFF — very pale cyan.</summary>
        public const string LightCyan = "#E0FFFF";

        /// <summary>#48D1CC — medium turquoise.</summary>
        public const string MediumTurquoise = "#48D1CC";

        /// <summary>#40E0D0 — turquoise.</summary>
        public const string Turquoise = "#40E0D0";

        /// <summary>#AFEEEE — pale turquoise.</summary>
        public const string PaleTurquoise = "#AFEEEE";

        /// <summary>#20B2AA — light sea green (teal-ish).</summary>
        public const string LightSeaGreen = "#20B2AA";

        /// <summary>#5F9EA0 — cadet blue.</summary>
        public const string CadetBlue = "#5F9EA0";

        // =================================================================
        // Blues
        // =================================================================

        /// <summary>#0000FF — pure blue.</summary>
        public const string Blue = "#0000FF";

        /// <summary>#00008B — deep dark blue.</summary>
        public const string DarkBlue = "#00008B";

        /// <summary>#000080 — navy.</summary>
        public const string Navy = "#000080";

        /// <summary>#191970 — midnight blue.</summary>
        public const string MidnightBlue = "#191970";

        /// <summary>#0000CD — medium blue.</summary>
        public const string MediumBlue = "#0000CD";

        /// <summary>#4169E1 — royal blue.</summary>
        public const string RoyalBlue = "#4169E1";

        /// <summary>#4682B4 — steel blue.</summary>
        public const string SteelBlue = "#4682B4";

        /// <summary>#1E90FF — dodger blue.</summary>
        public const string DodgerBlue = "#1E90FF";

        /// <summary>#6495ED — cornflower blue.</summary>
        public const string CornflowerBlue = "#6495ED";

        /// <summary>#00BFFF — deep sky blue.</summary>
        public const string DeepSkyBlue = "#00BFFF";

        /// <summary>#87CEEB — sky blue.</summary>
        public const string SkyBlue = "#87CEEB";

        /// <summary>#87CEFA — light sky blue.</summary>
        public const string LightSkyBlue = "#87CEFA";

        /// <summary>#ADD8E6 — light blue.</summary>
        public const string LightBlue = "#ADD8E6";

        /// <summary>#B0E0E6 — powder blue.</summary>
        public const string PowderBlue = "#B0E0E6";

        /// <summary>#B0C4DE — light steel blue.</summary>
        public const string LightSteelBlue = "#B0C4DE";

        /// <summary>#F0F8FF — very pale alice blue.</summary>
        public const string AliceBlue = "#F0F8FF";

        /// <summary>#F8F8FF — ghost white with blue tint.</summary>
        public const string GhostWhite = "#F8F8FF";

        // =================================================================
        // Purples / Violets
        // =================================================================

        /// <summary>#800080 — standard purple.</summary>
        public const string Purple = "#800080";

        /// <summary>#4B0082 — deep indigo.</summary>
        public const string Indigo = "#4B0082";

        /// <summary>#663399 — rebecca purple.</summary>
        public const string RebeccaPurple = "#663399";

        /// <summary>#9400D3 — dark violet.</summary>
        public const string DarkViolet = "#9400D3";

        /// <summary>#8B008B — dark magenta.</summary>
        public const string DarkMagenta = "#8B008B";

        /// <summary>#9932CC — dark orchid.</summary>
        public const string DarkOrchid = "#9932CC";

        /// <summary>#BA55D3 — medium orchid.</summary>
        public const string MediumOrchid = "#BA55D3";

        /// <summary>#DA70D6 — orchid.</summary>
        public const string Orchid = "#DA70D6";

        /// <summary>#EE82EE — violet.</summary>
        public const string Violet = "#EE82EE";

        /// <summary>#DDA0DD — plum.</summary>
        public const string Plum = "#DDA0DD";

        /// <summary>#D8BFD8 — light thistle.</summary>
        public const string Thistle = "#D8BFD8";

        /// <summary>#9370DB — medium purple.</summary>
        public const string MediumPurple = "#9370DB";

        /// <summary>#7B68EE — medium slate blue.</summary>
        public const string MediumSlateBlue = "#7B68EE";

        /// <summary>#6A5ACD — slate blue.</summary>
        public const string SlateBlue = "#6A5ACD";

        /// <summary>#483D8B — dark slate blue.</summary>
        public const string DarkSlateBlue = "#483D8B";

        /// <summary>#8A2BE2 — blue-violet.</summary>
        public const string BlueViolet = "#8A2BE2";

        /// <summary>#E6E6FA — pale lavender.</summary>
        public const string Lavender = "#E6E6FA";

        // =================================================================
        // Browns
        // =================================================================

        /// <summary>#A52A2A — warm brown.</summary>
        public const string Brown = "#A52A2A";

        /// <summary>#8B4513 — saddle brown.</summary>
        public const string SaddleBrown = "#8B4513";

        /// <summary>#A0522D — sienna.</summary>
        public const string Sienna = "#A0522D";

        /// <summary>#D2691E — chocolate brown.</summary>
        public const string Chocolate = "#D2691E";

        /// <summary>#CD853F — peru (light brown).</summary>
        public const string Peru = "#CD853F";

        /// <summary>#F4A460 — sandy brown.</summary>
        public const string SandyBrown = "#F4A460";

        /// <summary>#BC8F8F — rosy brown.</summary>
        public const string RosyBrown = "#BC8F8F";

        /// <summary>#DEB887 — burlywood.</summary>
        public const string Burlywood = "#DEB887";

        /// <summary>#D2B48C — tan.</summary>
        public const string Tan = "#D2B48C";

        /// <summary>#F5DEB3 — wheat.</summary>
        public const string Wheat = "#F5DEB3";

        /// <summary>#800000 — maroon (dark brown-red).</summary>
        public const string Maroon = "#800000";

        // =================================================================
        // Grays / Silvers
        // =================================================================

        /// <summary>#808080 — medium gray.</summary>
        public const string Gray = "#808080";

        /// <summary>#696969 — dim gray.</summary>
        public const string DimGray = "#696969";

        /// <summary>#A9A9A9 — dark gray.</summary>
        public const string DarkGray = "#A9A9A9";

        /// <summary>#D3D3D3 — light gray.</summary>
        public const string LightGray = "#D3D3D3";

        /// <summary>#C0C0C0 — silver.</summary>
        public const string Silver = "#C0C0C0";

        /// <summary>#DCDCDC — gainsboro.</summary>
        public const string Gainsboro = "#DCDCDC";

        /// <summary>#F5F5F5 — white smoke.</summary>
        public const string WhiteSmoke = "#F5F5F5";

        /// <summary>#778899 — light slate gray.</summary>
        public const string LightSlateGray = "#778899";

        /// <summary>#708090 — slate gray.</summary>
        public const string SlateGray = "#708090";

        /// <summary>#2F4F4F — dark slate gray.</summary>
        public const string DarkSlateGray = "#2F4F4F";

        /// <summary>#333333 — very dark grey; default chart text colour.</summary>
        public const string GraphiteGray = "#333333";

        /// <summary>#444444 — dark charcoal grey.</summary>
        public const string DimCharcoal = "#444444";

        /// <summary>#555555 — cool medium-dark grey.</summary>
        public const string CoolGray = "#555555";

        /// <summary>#666666 — stone grey.</summary>
        public const string StoneGray = "#666666";

        /// <summary>#777777 — neutral mid grey.</summary>
        public const string MidGray = "#777777";

        /// <summary>#888888 — ash grey.</summary>
        public const string AshGray = "#888888";

        /// <summary>#999999 — silver-mist grey.</summary>
        public const string SilverMist = "#999999";

        /// <summary>#AAAAAA — cloud grey.</summary>
        public const string CloudGray = "#AAAAAA";

        /// <summary>#BBBBBB — light silver grey.</summary>
        public const string LightSilver = "#BBBBBB";

        /// <summary>#CCCCCC — pearl grey.</summary>
        public const string PearlGray = "#CCCCCC";

        /// <summary>#DDDDDD — platinum grey.</summary>
        public const string PlatinumGray = "#DDDDDD";

        /// <summary>#E6E6E6 — silk grey; default chart grid-line colour.</summary>
        public const string SilkGray = "#E6E6E6";

        /// <summary>#EFEFEF — frost grey; very light grey for grid lines.</summary>
        public const string FrostGray = "#EFEFEF";

        /// <summary>#FAFAFA — off-white; near-pure-white background.</summary>
        public const string OffWhite = "#FAFAFA";

        // =================================================================
        // Whites / Black
        // =================================================================

        /// <summary>#FFFFFF — pure white.</summary>
        public const string White = "#FFFFFF";

        /// <summary>#FFFAFA — snow white.</summary>
        public const string Snow = "#FFFAFA";

        /// <summary>#F0FFF0 — honeydew (mint white).</summary>
        public const string Honeydew = "#F0FFF0";

        /// <summary>#F5FFFA — mint cream.</summary>
        public const string MintCream = "#F5FFFA";

        /// <summary>#F0FFFF — azure.</summary>
        public const string Azure = "#F0FFFF";

        /// <summary>#FFFAF0 — floral white.</summary>
        public const string FloralWhite = "#FFFAF0";

        /// <summary>#FAF0E6 — linen.</summary>
        public const string Linen = "#FAF0E6";

        /// <summary>#FDF5E6 — old lace.</summary>
        public const string OldLace = "#FDF5E6";

        /// <summary>#FFEFEF6 — papaya whip.</summary>
        public const string PapayaWhip = "#FFEFD5";

        /// <summary>#FFEBCD — blanched almond.</summary>
        public const string BlanchedAlmond = "#FFEBCD";

        /// <summary>#FFDEAD — navajo white.</summary>
        public const string NavajoWhite = "#FFDEAD";

        /// <summary>#000000 — pure black.</summary>
        public const string Black = "#000000";

        /// <summary>"none" — no colour / fully transparent (SVG keyword; valid for fill, stroke, and background).</summary>
        public const string None = "none";

        // =================================================================
        // Chart-Friendly Palette Colours
        // These are the exact hex values used in the library's built-in themes.
        // =================================================================

        /// <summary>#7CB5EC — sky blue (Default/Animated theme, series 1).</summary>
        public const string ChartBlue = "#7CB5EC";

        /// <summary>#F7A35C — warm orange (Default/Animated theme, series 2).</summary>
        public const string ChartOrange = "#F7A35C";

        /// <summary>#90ED7D — fresh light green (Default/Animated theme, series 3).</summary>
        public const string ChartGreen = "#90ED7D";

        /// <summary>#E4D354 — golden yellow (Default/Animated theme, series 4).</summary>
        public const string ChartYellow = "#E4D354";

        /// <summary>#8085E9 — soft indigo (Default/Animated theme, series 5).</summary>
        public const string ChartIndigo = "#8085E9";

        /// <summary>#F15C80 — rose pink (Default/Animated theme, series 6).</summary>
        public const string ChartRose = "#F15C80";

        /// <summary>#2B908F — teal-green (Default/Animated theme, series 7).</summary>
        public const string ChartTeal = "#2B908F";

        /// <summary>#F45B5B — bright red (Default/Animated theme, series 8).</summary>
        public const string ChartRed = "#F45B5B";

        /// <summary>#434348 — dark charcoal — typical for static/PDF charts.</summary>
        public const string Charcoal = "#434348";

        // Pastel palette (ChartTheme.Pastel)
        /// <summary>#A8D8EA — pastel sky blue.</summary>
        public const string PastelSkyBlue = "#A8D8EA";

        /// <summary>#AA96DA — pastel purple.</summary>
        public const string PastelPurple = "#AA96DA";

        /// <summary>#FCBAD3 — pastel pink.</summary>
        public const string PastelPink = "#FCBAD3";

        /// <summary>#B5EAD7 — pastel mint.</summary>
        public const string PastelMint = "#B5EAD7";

        /// <summary>#FFDAC1 — pastel peach.</summary>
        public const string PastelPeach = "#FFDAC1";

        /// <summary>#C7CEEA — pastel periwinkle.</summary>
        public const string PastelPeriwinkle = "#C7CEEA";

        /// <summary>#E2F0CB — pastel lime.</summary>
        public const string PastelLime = "#E2F0CB";

        /// <summary>#FFFFD2 — pastel lemon.</summary>
        public const string PastelLemon = "#FFFFD2";

        // Theme UI Colours — backgrounds, grid lines and axes used by ChartTheme.*
        /// <summary>#1A1A2E — midnight navy; Dark theme background.</summary>
        public const string MidnightNavy = "#1A1A2E";

        /// <summary>#3A3A5E — night slate; Dark theme grid-line colour.</summary>
        public const string NightSlate = "#3A3A5E";

        /// <summary>#F0F0FF — lavender mist; Dark theme text colour.</summary>
        public const string LavenderMist = "#F0F0FF";

        /// <summary>#7070B0 — dusky indigo; Dark theme axis-line colour.</summary>
        public const string DuskyIndigo = "#7070B0";

        /// <summary>#CCD6EB — pale steel blue; default axis-line colour.</summary>
        public const string PaleSteelBlue = "#CCD6EB";

        /// <summary>#91E8E1 — aqua mint; 10th slot in the default series palette.</summary>
        public const string AquaMint = "#91E8E1";

        // Ocean theme UI colours
        /// <summary>#0D1B2A — deep ocean; Ocean theme background.</summary>
        public const string OceanDepth = "#0D1B2A";

        /// <summary>#1B3A5C — deep ocean grid; Ocean theme grid-line colour.</summary>
        public const string OceanGrid = "#1B3A5C";

        /// <summary>#C8E6FF — azure haze; Ocean theme text colour.</summary>
        public const string AzureHaze = "#C8E6FF";

        // Sunset theme UI colours
        /// <summary>#1A0A2E — sunset dusk; Sunset theme background (deep purple-navy).</summary>
        public const string SunsetDusk = "#1A0A2E";

        /// <summary>#3D1F5E — sunset grid; Sunset theme grid-line colour.</summary>
        public const string SunsetGrid = "#3D1F5E";

        /// <summary>#FFE8C8 — sunset glow; Sunset theme text colour (warm golden cream).</summary>
        public const string SunsetGlow = "#FFE8C8";

        /// <summary>#B06090 — dusky rose; Sunset theme axis-line colour.</summary>
        public const string DuskyRose = "#B06090";

        // Forest theme UI colours
        /// <summary>#F6F4EE — forest cream; Forest theme background (warm natural cream).</summary>
        public const string ForestCream = "#F6F4EE";

        /// <summary>#D8EAD2 — mossy green; Forest theme grid-line colour.</summary>
        public const string MossyGreen = "#D8EAD2";

        /// <summary>#2C2A1A — earth brown; Forest theme text colour (dark earthy brown).</summary>
        public const string EarthBrown = "#2C2A1A";

        /// <summary>#6B8E4E — fern green; Forest theme axis-line colour.</summary>
        public const string FernGreen = "#6B8E4E";

        // Neon theme UI colours and palette
        /// <summary>#0A0A0A — neon dark; Neon theme background (near-black).</summary>
        public const string NeonDark = "#0A0A0A";

        /// <summary>#1F1F1F — neon grid dark; Neon theme grid-line colour.</summary>
        public const string NeonGridDark = "#1F1F1F";

        /// <summary>#00FF88 — neon green; electric spring green for the Neon theme palette.</summary>
        public const string NeonGreen = "#00FF88";

        /// <summary>#FF0080 — neon pink; electric hot pink for the Neon theme palette.</summary>
        public const string NeonPink = "#FF0080";

        /// <summary>#00CFFF — neon cyan; electric sky blue for the Neon theme palette.</summary>
        public const string NeonCyan = "#00CFFF";

        /// <summary>#FF6600 — neon orange; electric orange for the Neon theme palette.</summary>
        public const string NeonOrange = "#FF6600";

        // Warm theme UI colours
        /// <summary>#FAF3E0 — warm parchment; Warm theme background.</summary>
        public const string WarmParchment = "#FAF3E0";

        /// <summary>#E8D5B0 — warm sand; Warm theme grid-line colour.</summary>
        public const string WarmSand = "#E8D5B0";

        /// <summary>#3E2723 — espresso black; Warm theme text colour (deep dark brown).</summary>
        public const string EspressoBlack = "#3E2723";

        /// <summary>#A0785A — cinnamon brown; Warm theme axis-line colour.</summary>
        public const string CinnamonBrown = "#A0785A";

        // Arctic theme UI colours
        /// <summary>#D6EAF8 — icy blue; Arctic theme grid-line colour.</summary>
        public const string IcyBlue = "#D6EAF8";

        /// <summary>#1C2E4A — polar night; Arctic theme text colour (dark polar navy).</summary>
        public const string PolarNight = "#1C2E4A";

        /// <summary>#7FB3D3 — glacier blue; Arctic theme axis-line colour.</summary>
        public const string GlacierBlue = "#7FB3D3";

        // =================================================================
        // Domain Palette Colours
        // Named constants for the Business, Material, TrafficLight and Accessible palettes.
        // =================================================================

        // Business palette
        /// <summary>#2C7BB6 — corporate cerulean blue; Business palette series 1.</summary>
        public const string CeruleanBlue = "#2C7BB6";

        /// <summary>#D7191C — vivid cinnabar red; Business palette series 2.</summary>
        public const string CinnabarRed = "#D7191C";

        /// <summary>#1A9641 — rich pine green; Business palette series 3.</summary>
        public const string PineGreen = "#1A9641";

        /// <summary>#FDAE61 — warm apricot orange; Business palette series 4.</summary>
        public const string Apricot = "#FDAE61";

        /// <summary>#762A83 — deep grape violet; Business palette series 5.</summary>
        public const string GrapeViolet = "#762A83";

        /// <summary>#00B4D8 — bright capri cyan-blue; Business palette series 6.</summary>
        public const string CapriBlue = "#00B4D8";

        // Material Design 500-level colours
        /// <summary>#F44336 — Material Design red 500.</summary>
        public const string MaterialRed = "#F44336";

        /// <summary>#2196F3 — Material Design blue 500.</summary>
        public const string MaterialBlue = "#2196F3";

        /// <summary>#4CAF50 — Material Design green 500.</summary>
        public const string MaterialGreen = "#4CAF50";

        /// <summary>#FF9800 — warm amber; Material Design orange 500.</summary>
        public const string Amber = "#FF9800";

        /// <summary>#9C27B0 — Material Design purple 500.</summary>
        public const string MaterialPurple = "#9C27B0";

        /// <summary>#00BCD4 — Material Design cyan 500.</summary>
        public const string MaterialCyan = "#00BCD4";

        /// <summary>#FF5722 — deep burnt orange; Material Design deep-orange 500.</summary>
        public const string DeepOrange = "#FF5722";

        /// <summary>#607D8B — muted blue-grey; Material Design blue-grey 500.</summary>
        public const string BlueGray = "#607D8B";

        /// <summary>#E91E63 — Material Design pink 500.</summary>
        public const string MaterialPink = "#E91E63";

        /// <summary>#3F51B5 — Material Design indigo 500.</summary>
        public const string MaterialIndigo = "#3F51B5";

        /// <summary>#009688 — Material Design teal 500.</summary>
        public const string MaterialTeal = "#009688";

        /// <summary>#FFEB3B — Material Design yellow 500.</summary>
        public const string MaterialYellow = "#FFEB3B";

        // Traffic-light colours
        /// <summary>#27AE60 — emerald green; traffic-light safe colour.</summary>
        public const string EmeraldGreen = "#27AE60";

        /// <summary>#F39C12 — dark amber; traffic-light safe colour.</summary>
        public const string DarkAmber = "#F39C12";

        /// <summary>#E74C3C — alizarin red; traffic-light safe colour.</summary>
        public const string Alizarin = "#E74C3C";

        // Wong 2011 accessible colours
        /// <summary>#E69F00 — Wong orange; accessible palette (Wong 2011) colour 1.</summary>
        public const string WongOrange = "#E69F00";

        /// <summary>#56B4E9 — Wong sky blue; accessible palette (Wong 2011) colour 2.</summary>
        public const string WongSkyBlue = "#56B4E9";

        /// <summary>#009E73 — Wong bluish-green; accessible palette (Wong 2011) colour 3.</summary>
        public const string WongGreen = "#009E73";

        /// <summary>#F0E442 — Wong yellow; accessible palette (Wong 2011) colour 4.</summary>
        public const string WongYellow = "#F0E442";

        /// <summary>#0072B2 — Wong blue; accessible palette (Wong 2011) colour 5.</summary>
        public const string WongBlue = "#0072B2";

        /// <summary>#D55E00 — vermilion; accessible palette (Wong 2011) colour 6.</summary>
        public const string Vermilion = "#D55E00";

        /// <summary>#CC79A7 — rose purple; accessible palette (Wong 2011) colour 7.</summary>
        public const string RosePurple = "#CC79A7";

        // =================================================================
        // Extended Theme Palette Colours
        // Named constants for every colour referenced in a built-in palette
        // array.  Grouped by the theme / purpose they were introduced for.
        // =================================================================

        // ── Vivid / bright — high-saturation colours for dark backgrounds ──────────

        /// <summary>#60A5FA — bright cornflower blue; Dark theme palette series 1.</summary>
        public const string BrightCornflower = "#60A5FA";

        /// <summary>#FB923C — bright orange; Dark theme palette series 2.</summary>
        public const string BrightOrange = "#FB923C";

        /// <summary>#4ADE80 — bright emerald green; Dark / Ocean theme palettes.</summary>
        public const string BrightEmerald = "#4ADE80";

        /// <summary>#FACC15 — bright golden yellow; Dark theme palette series 4.</summary>
        public const string BrightGolden = "#FACC15";

        /// <summary>#A78BFA — soft lavender-violet; Dark theme palette series 5.</summary>
        public const string SoftLavender = "#A78BFA";

        /// <summary>#F472B6 — bright rose-pink; Dark theme palette series 6.</summary>
        public const string BrightRosePink = "#F472B6";

        /// <summary>#2DD4BF — cyan-turquoise; Dark theme palette series 7.</summary>
        public const string CyanTurquoise = "#2DD4BF";

        /// <summary>#F87171 — soft salmon-red; Dark theme palette series 8.</summary>
        public const string SalmonPink = "#F87171";

        /// <summary>#38BDF8 — bright sky blue; Dark / Ocean theme palettes.</summary>
        public const string BrightSkyBlue = "#38BDF8";

        /// <summary>#E879F9 — bright fuchsia; Dark theme palette series 10.</summary>
        public const string BrightFuchsia = "#E879F9";

        /// <summary>#FCD34D — soft golden amber; Dark theme palette series 11.</summary>
        public const string SoftGolden = "#FCD34D";

        /// <summary>#86EFAC — light mint green; Dark theme palette series 12.</summary>
        public const string LightMintGreen = "#86EFAC";

        // ── Pastel palette extras ─────────────────────────────────────────────────

        /// <summary>#C8E6C9 — pastel mint-green; Pastel theme palette series 7.</summary>
        public const string PastelGreenMint = "#C8E6C9";

        /// <summary>#FFCC80 — pastel amber-orange; Pastel theme palette series 8.</summary>
        public const string PastelAmberOrange = "#FFCC80";

        /// <summary>#F4BCCA — pastel rose-pink; Pastel theme palette series 11.</summary>
        public const string PastelRosePink = "#F4BCCA";

        // ── Ocean palette colours ─────────────────────────────────────────────────

        /// <summary>#06B6D4 — deep cyan-blue; Ocean theme palette series 3.</summary>
        public const string DeepCyan = "#06B6D4";

        /// <summary>#22D3EE — clear cyan; Ocean theme palette series 4.</summary>
        public const string ClearCyan = "#22D3EE";

        /// <summary>#7DD3FC — sky mist (pale sky blue); Ocean theme palette series 5.</summary>
        public const string SkyMist = "#7DD3FC";

        /// <summary>#34D399 — teal-green; Ocean theme palette series 6.</summary>
        public const string TealGreen = "#34D399";

        /// <summary>#67E8F9 — glacial cyan; Ocean theme palette series 7.</summary>
        public const string GlacialCyan = "#67E8F9";

        /// <summary>#6EE7B7 — sea-mint green; Ocean theme palette series 8.</summary>
        public const string SeaMint = "#6EE7B7";

        /// <summary>#0EA5E9 — clear blue; Ocean theme palette series 9.</summary>
        public const string ClearBlue = "#0EA5E9";

        /// <summary>#14B8A6 — ocean teal; Ocean theme palette series 10.</summary>
        public const string OceanTeal = "#14B8A6";

        /// <summary>#93C5FD — lavender-blue; Ocean theme palette series 11.</summary>
        public const string LavenderBlue = "#93C5FD";

        /// <summary>#A5F3FC — ice cyan (very pale); Ocean theme palette series 12.</summary>
        public const string IceCyan = "#A5F3FC";

        // ── Sunset palette colours ────────────────────────────────────────────────

        /// <summary>#FF7043 — coral-orange; Sunset theme palette series 1.</summary>
        public const string CoralOrange = "#FF7043";

        /// <summary>#FFB300 — amber-gold; Sunset theme palette series 2.</summary>
        public const string AmberGold = "#FFB300";

        /// <summary>#EC407A — raspberry pink; Sunset theme palette series 3.</summary>
        public const string RaspberryPink = "#EC407A";

        /// <summary>#F48FB1 — soft pink-rose; Sunset theme palette series 5.</summary>
        public const string SoftPinkRose = "#F48FB1";

        /// <summary>#CE93D8 — soft lilac-purple; Sunset theme palette series 6.</summary>
        public const string SoftLilac = "#CE93D8";

        /// <summary>#FFCA28 — bright amber; Sunset theme palette series 7.</summary>
        public const string BrightAmber = "#FFCA28";

        /// <summary>#FF5252 — hot red; Sunset theme palette series 8.</summary>
        public const string HotRed = "#FF5252";

        /// <summary>#E040FB — electric purple; Sunset theme palette series 9.</summary>
        public const string ElectricPurple = "#E040FB";

        /// <summary>#FF6D00 — burnt tangerine; Sunset theme palette series 10.</summary>
        public const string BurntTangerine = "#FF6D00";

        /// <summary>#FFD740 — golden shine; Sunset theme palette series 11.</summary>
        public const string GoldenShine = "#FFD740";

        /// <summary>#EA80FC — soft orchid; Sunset theme palette series 12.</summary>
        public const string SoftOrchid = "#EA80FC";

        // ── Forest palette colours ────────────────────────────────────────────────

        /// <summary>#1B5E20 — deep forest green; Forest theme palette series 1.</summary>
        public const string DeepForest = "#1B5E20";

        /// <summary>#E65100 — deep burnt-orange; Forest theme palette series 2.</summary>
        public const string DeepBurntOrange = "#E65100";

        /// <summary>#1A237E — deep indigo; Forest theme palette series 3.</summary>
        public const string DeepIndigo = "#1A237E";

        /// <summary>#33691E — dark moss-green; Forest theme palette series 4.</summary>
        public const string DarkMossGreen = "#33691E";

        /// <summary>#BF360C — brick red; Forest / TrafficLight theme palettes.</summary>
        public const string BrickRed = "#BF360C";

        /// <summary>#4E342E — dark earth-brown; Forest theme palette series 6.</summary>
        public const string DarkEarth = "#4E342E";

        /// <summary>#2E7D32 — dark forest green; Forest theme palette series 7.</summary>
        public const string DarkForestGreen = "#2E7D32";

        /// <summary>#558B2F — olive-forest green; Forest theme palette series 8.</summary>
        public const string OliveForestGreen = "#558B2F";

        /// <summary>#6D4C41 — terracotta brown; Forest theme palette series 9.</summary>
        public const string TerracottaBrown = "#6D4C41";

        /// <summary>#F57F17 — deep amber; Forest theme palette series 10.</summary>
        public const string AmberDeep = "#F57F17";

        /// <summary>#00695C — dark teal; Forest / Arctic theme palettes.</summary>
        public const string DarkTeal = "#00695C";

        /// <summary>#827717 — dark olive-yellow; Forest theme palette series 12.</summary>
        public const string DarkOliveYellow = "#827717";

        // ── Neon extended colours ─────────────────────────────────────────────────

        /// <summary>#FF4444 — neon red; Neon theme palette series 8.</summary>
        public const string NeonRed = "#FF4444";

        /// <summary>#00EEFF — neon cyan-blue; Neon theme palette series 9.</summary>
        public const string NeonCyanBlue = "#00EEFF";

        /// <summary>#FF9900 — neon amber; Neon theme palette series 10.</summary>
        public const string NeonAmber = "#FF9900";

        /// <summary>#39FF14 — electric lime; Neon theme palette series 11.</summary>
        public const string ElectricLime = "#39FF14";

        /// <summary>#DA00FF — electric violet; Neon theme palette series 12.</summary>
        public const string ElectricViolet = "#DA00FF";

        // ── Minimal (Tableau-10) palette colours ──────────────────────────────────

        /// <summary>#4E79A7 — Tableau steel blue; Minimal theme palette series 1.</summary>
        public const string TableauBlue = "#4E79A7";

        /// <summary>#F28E2B — Tableau orange; Minimal theme palette series 2.</summary>
        public const string TableauOrange = "#F28E2B";

        /// <summary>#E15759 — Tableau red; Minimal theme palette series 3.</summary>
        public const string TableauRed = "#E15759";

        /// <summary>#76B7B2 — Tableau teal; Minimal theme palette series 4.</summary>
        public const string TableauTeal = "#76B7B2";

        /// <summary>#59A14F — Tableau green; Minimal theme palette series 5.</summary>
        public const string TableauGreen = "#59A14F";

        /// <summary>#EDC948 — Tableau yellow; Minimal theme palette series 6.</summary>
        public const string TableauYellow = "#EDC948";

        /// <summary>#B07AA1 — Tableau purple; Minimal theme palette series 7.</summary>
        public const string TableauPurple = "#B07AA1";

        /// <summary>#FF9DA7 — Tableau salmon pink; Minimal theme palette series 8.</summary>
        public const string TableauPink = "#FF9DA7";

        /// <summary>#9C755F — Tableau brown; Minimal theme palette series 9.</summary>
        public const string TableauBrown = "#9C755F";

        /// <summary>#BAB0AC — Tableau gray; Minimal theme palette series 10.</summary>
        public const string TableauGray = "#BAB0AC";

        /// <summary>#499894 — Tableau dark teal; Minimal theme palette series 11.</summary>
        public const string TableauDarkTeal = "#499894";

        /// <summary>#86BCB6 — Tableau light teal; Minimal theme palette series 12.</summary>
        public const string TableauLightTeal = "#86BCB6";

        // ── Warm earth palette colours ────────────────────────────────────────────

        /// <summary>#8B2500 — dark mahogany; Warm theme palette series 1.</summary>
        public const string DarkMahogany = "#8B2500";

        /// <summary>#C0392B — pomegranate red; Warm theme palette series 2.</summary>
        public const string Pomegranate = "#C0392B";

        /// <summary>#D35400 — pumpkin orange; Warm theme palette series 3.</summary>
        public const string PumpkinOrange = "#D35400";

        /// <summary>#A3722A — bronze brown; Warm theme palette series 4.</summary>
        public const string BronzeBrown = "#A3722A";

        /// <summary>#6B3A2A — dark mocha; Warm theme palette series 5.</summary>
        public const string DarkMocha = "#6B3A2A";

        /// <summary>#9E3D19 — rustic red; Warm theme palette series 6.</summary>
        public const string RusticRed = "#9E3D19";

        /// <summary>#8B6914 — dark olive-brown; Warm theme palette series 7.</summary>
        public const string DarkOliveBrown = "#8B6914";

        /// <summary>#5C3D11 — deep mocha; Warm theme palette series 8.</summary>
        public const string DeepMocha = "#5C3D11";

        /// <summary>#7B3F00 — dark chocolate; Warm theme palette series 9.</summary>
        public const string DarkChocolate = "#7B3F00";

        /// <summary>#C25D00 — burnt copper-orange; Warm theme palette series 10.</summary>
        public const string BurntCopperOrange = "#C25D00";

        /// <summary>#2B4D0A — forest olive; Warm theme palette series 11.</summary>
        public const string ForestOlive = "#2B4D0A";

        /// <summary>#1A3A5C — deep navy blue; Warm theme palette series 12.</summary>
        public const string DeepNavyBlue = "#1A3A5C";

        // ── Arctic / Material Design deep-shade palette colours ───────────────────

        /// <summary>#1565C0 — Material Blue 800; Arctic / TrafficLight theme palettes.</summary>
        public const string MaterialBlue800 = "#1565C0";

        /// <summary>#0288D1 — Material Light Blue 700; Arctic theme palette series 2.</summary>
        public const string MaterialLightBlue700 = "#0288D1";

        /// <summary>#00838F — Material Cyan 700; Arctic theme palette series 3.</summary>
        public const string MaterialCyan700 = "#00838F";

        /// <summary>#283593 — Material Indigo 800; Arctic theme palette series 4.</summary>
        public const string MaterialIndigo800 = "#283593";

        /// <summary>#1976D2 — Material Blue 700; Arctic theme palette series 5.</summary>
        public const string MaterialBlue700 = "#1976D2";

        /// <summary>#00796B — Material Teal 700; Arctic theme palette series 6.</summary>
        public const string MaterialTeal700 = "#00796B";

        /// <summary>#0277BD — Material Light Blue 800; Arctic theme palette series 7.</summary>
        public const string MaterialLightBlue800 = "#0277BD";

        /// <summary>#5E35B1 — Material Deep Purple 600; Arctic theme palette series 8.</summary>
        public const string MaterialDeepPurple600 = "#5E35B1";

        /// <summary>#01579B — Material Light Blue 900; Arctic theme palette series 10.</summary>
        public const string MaterialLightBlue900 = "#01579B";

        /// <summary>#6A1B9A — Material Purple 800; Arctic theme palette series 11.</summary>
        public const string MaterialPurple800 = "#6A1B9A";

        /// <summary>#006064 — Material Cyan 900; Arctic theme palette series 12.</summary>
        public const string MaterialCyan900 = "#006064";

        /// <summary>#795548 — Material Brown 500; Material theme palette series 12.</summary>
        public const string MaterialBrown = "#795548";

        // ── Traffic-light extended palette colours ────────────────────────────────

        /// <summary>#7D3C98 — deep royal purple; TrafficLight theme palette series 5.</summary>
        public const string DeepRoyalPurple = "#7D3C98";

        /// <summary>#E08000 — deep amber-orange; TrafficLight theme palette series 8.</summary>
        public const string DeepAmber = "#E08000";

        /// <summary>#2980B9 — Belize blue; TrafficLight theme palette series 9.</summary>
        public const string BelizeBlue = "#2980B9";

        /// <summary>#117A65 — dark emerald teal; TrafficLight theme palette series 11.</summary>
        public const string DarkEmeraldTeal = "#117A65";

        // ── Accessible (Wong 2011 extended) palette colours ───────────────────────

        /// <summary>#44AA99 — accessible teal; Accessible theme palette series 8.</summary>
        public const string AccessibleTeal = "#44AA99";

        /// <summary>#332288 — accessible indigo; Accessible theme palette series 10.</summary>
        public const string AccessibleIndigo = "#332288";

        /// <summary>#117733 — accessible forest green; Accessible theme palette series 11.</summary>
        public const string AccessibleGreen = "#117733";

        /// <summary>#AA4499 — accessible purple; Accessible theme palette series 12.</summary>
        public const string AccessiblePurple = "#AA4499";

        // ── Vivid theme palette colours ────────────────────

        /// <summary>#2CAFFE — vivid sky blue; Vivid theme palette series 1.</summary>
        public const string VividBlue = "#2CAFFE";

        /// <summary>#544FC5 — vivid indigo; Vivid theme palette series 2.</summary>
        public const string VividIndigo = "#544FC5";

        /// <summary>#00E272 — vivid electric green; Vivid theme palette series 3.</summary>
        public const string VividGreen = "#00E272";

        /// <summary>#FE6A35 — vivid orange-red; Vivid theme palette series 4.</summary>
        public const string VividOrangeRed = "#FE6A35";

        /// <summary>#D568FB — vivid orchid; Vivid theme palette series 5.</summary>
        public const string VividOrchid = "#D568FB";

        /// <summary>#FA4B42 — vivid red; Vivid theme palette series 6.</summary>
        public const string VividRed = "#FA4B42";

        /// <summary>#2EE0CA — vivid cyan; Vivid theme palette series 7.</summary>
        public const string VividCyan = "#2EE0CA";

        /// <summary>#FEB56A — vivid peach; Vivid theme palette series 8.</summary>
        public const string VividPeach = "#FEB56A";

        /// <summary>#6B8ABC — cool blue-gray; Vivid theme palette series 9.</summary>
        public const string CoolBlue = "#6B8ABC";

        /// <summary>#F7C948 — vivid sun gold; Vivid theme palette series 10.</summary>
        public const string VividSunGold = "#F7C948";

        /// <summary>#E84B7A — vivid hot pink; Vivid theme palette series 11.</summary>
        public const string VividHotPink = "#E84B7A";

        // ── Palette expansion — additional colours to bring each palette to 20 entries ──────────

        // Dark palette extras (Tailwind-400 series)
        /// <summary>#818CF8 — bright indigo; Dark theme palette series 13.</summary>
        public const string BrightIndigo = "#818CF8";
        /// <summary>#FB7185 — bright rose-red; Dark theme palette series 14.</summary>
        public const string BrightRose = "#FB7185";
        /// <summary>#FBBF24 — warm amber; Dark theme palette series 15.</summary>
        public const string WarmAmber = "#FBBF24";
        /// <summary>#C084FC — bright violet-purple; Dark theme palette series 16.</summary>
        public const string BrightViolet = "#C084FC";
        /// <summary>#A3E635 — bright lime-green; Dark theme palette series 19.</summary>
        public const string BrightLime = "#A3E635";
        /// <summary>#FCA5A5 — soft coral; Dark theme palette series 20.</summary>
        public const string SoftCoral = "#FCA5A5";

        // Pastel palette extras
        /// <summary>#E8D5F5 — pastel violet; Pastel theme palette series 19.</summary>
        public const string PastelViolet = "#E8D5F5";
        /// <summary>#D5EAF5 — pastel ice blue; Pastel theme palette series 20.</summary>
        public const string PastelIceBlue = "#D5EAF5";

        // Sunset palette extras
        /// <summary>#FF8F00 — deep amber gold; Sunset theme palette series 20.</summary>
        public const string DeepAmberGold = "#FF8F00";

        // Forest palette extras
        /// <summary>#355E3B — hunter green; Forest theme palette series 19.</summary>
        public const string HunterGreen = "#355E3B";
        /// <summary>#5D4037 — warm wood-brown (Material Brown 700); Forest theme palette series 20.</summary>
        public const string WoodBrown = "#5D4037";

        // Neon palette extras
        /// <summary>#4400FF — ultra violet electric; Neon theme palette series 19.</summary>
        public const string UltraViolet = "#4400FF";
        /// <summary>#FF6EC7 — neon bubblegum pink; Neon theme palette series 20.</summary>
        public const string NeonBubblegum = "#FF6EC7";

        // Minimal (Tableau-20 lighter variants)
        /// <summary>#AEC7E8 — Tableau light blue; Minimal theme palette series 13.</summary>
        public const string TableauLightBlue = "#AEC7E8";
        /// <summary>#FFBB78 — Tableau light orange; Minimal theme palette series 14.</summary>
        public const string TableauLightOrange = "#FFBB78";
        /// <summary>#98DF8A — Tableau light green; Minimal theme palette series 15.</summary>
        public const string TableauLightGreen = "#98DF8A";
        /// <summary>#FF9896 — Tableau light red; Minimal theme palette series 16.</summary>
        public const string TableauLightRed = "#FF9896";
        /// <summary>#C5B0D5 — Tableau light purple; Minimal theme palette series 17.</summary>
        public const string TableauLightPurple = "#C5B0D5";
        /// <summary>#C49C94 — Tableau light brown; Minimal theme palette series 18.</summary>
        public const string TableauLightBrown = "#C49C94";
        /// <summary>#F7B6D2 — Tableau light pink; Minimal theme palette series 19.</summary>
        public const string TableauLightPink = "#F7B6D2";
        /// <summary>#DBDB8D — Tableau light yellow; Minimal theme palette series 20.</summary>
        public const string TableauLightYellow = "#DBDB8D";

        // Arctic palette extras (Material Design deeper shades)
        /// <summary>#0D47A1 — Material Blue 900; Arctic theme palette series 19.</summary>
        public const string MaterialBlue900 = "#0D47A1";
        /// <summary>#3949AB — Material Indigo 600; Arctic theme palette series 20.</summary>
        public const string MaterialIndigo600 = "#3949AB";

        // Material Design 500 palette extras
        /// <summary>#673AB7 — Material Deep Purple 500; Material theme palette series 14.</summary>
        public const string MaterialDeepPurple = "#673AB7";
        /// <summary>#03A9F4 — Material Light Blue 500; Material theme palette series 15.</summary>
        public const string MaterialLightBlue = "#03A9F4";
        /// <summary>#8BC34A — Material Light Green 500; Material theme palette series 16.</summary>
        public const string MaterialLightGreen = "#8BC34A";
        /// <summary>#CDDC39 — Material Lime 500; Material theme palette series 17.</summary>
        public const string MaterialLime = "#CDDC39";
        /// <summary>#FFC107 — Material Amber 500; Material theme palette series 18.</summary>
        public const string MaterialAmber = "#FFC107";
        /// <summary>#9E9E9E — Material Grey 500; Material theme palette series 19.</summary>
        public const string MaterialGray = "#9E9E9E";
        /// <summary>#455A64 — Material Blue Grey 700; Material theme palette series 20.</summary>
        public const string MaterialBlueGray700 = "#455A64";

        // TrafficLight palette extras
        /// <summary>#D50000 — alert red; TrafficLight theme palette series 19.</summary>
        public const string AlertRed = "#D50000";

        // Accessible (Paul Tol colorblind-safe set, second half)
        /// <summary>#EE7733 — Tol orange; Accessible theme palette series 13.</summary>
        public const string TolOrange = "#EE7733";
        /// <summary>#0077BB — Tol blue; Accessible theme palette series 14.</summary>
        public const string TolBlue = "#0077BB";
        /// <summary>#33BBEE — Tol cyan; Accessible theme palette series 15.</summary>
        public const string TolCyan = "#33BBEE";
        /// <summary>#EE3377 — Tol magenta; Accessible theme palette series 16.</summary>
        public const string TolMagenta = "#EE3377";
        /// <summary>#CC3311 — Tol red; Accessible theme palette series 17.</summary>
        public const string TolRed = "#CC3311";
        /// <summary>#009988 — Tol teal; Accessible theme palette series 18.</summary>
        public const string TolTeal = "#009988";

        // Vivid theme extras (Apple / Google design-system inspired)
        /// <summary>#0CE5FF — vivid electric cyan; Vivid theme palette series 13.</summary>
        public const string VividElectricCyan = "#0CE5FF";
        /// <summary>#FF9500 — vivid orange; Vivid theme palette series 14.</summary>
        public const string VividOrange = "#FF9500";
        /// <summary>#3DDC84 — vivid leaf green; Vivid theme palette series 15.</summary>
        public const string VividLeafGreen = "#3DDC84";
        /// <summary>#FF2D55 — vivid crimson; Vivid theme palette series 16.</summary>
        public const string VividCrimson = "#FF2D55";
        /// <summary>#5856D6 — vivid purple-indigo; Vivid theme palette series 17.</summary>
        public const string VividPurpleIndigo = "#5856D6";
        /// <summary>#FFCC00 — vivid bright yellow; Vivid theme palette series 18.</summary>
        public const string VividBrightYellow = "#FFCC00";
        /// <summary>#30B0C7 — vivid teal-blue; Vivid theme palette series 19.</summary>
        public const string VividTealBlue = "#30B0C7";
        /// <summary>#AF52DE — vivid medium purple; Vivid theme palette series 20.</summary>
        public const string VividMediumPurple = "#AF52DE";

        // ── Theme positive / negative / accent / tooltip colours ─────────────────

        // Shared semantic colours
        /// <summary>#FF6B6B — coral-red; negative/error indicator used across Ocean and Dark themes.</summary>
        public const string CoralRed = "#FF6B6B";
        /// <summary>#22C55E — signal green; positive/success indicator used across several themes.</summary>
        public const string SignalGreen = "#22C55E";
        /// <summary>#DC2626 — negative deep-red; error indicator for Forest and Business themes.</summary>
        public const string NegativeRed = "#DC2626";
        /// <summary>#EF4444 — warning red; alert indicator for Minimal and TrafficLight themes.</summary>
        public const string WarningRed = "#EF4444";

        // Dark theme
        /// <summary>#D0D0F0 — lavender-white; tooltip text on the Dark theme.</summary>
        public const string LavenderWhite = "#D0D0F0";
        /// <summary>#2ECC71 — Flat UI emerald; positive indicator on the Dark theme.</summary>
        public const string FlatEmerald = "#2ECC71";

        // Pastel theme
        /// <summary>#48BB78 — soft mint-green; positive indicator on the Pastel theme.</summary>
        public const string SoftMintGreen = "#48BB78";
        /// <summary>#FC8181 — soft salmon; negative indicator on the Pastel theme.</summary>
        public const string SoftSalmon = "#FC8181";
        /// <summary>#9F7AEA — lavender-purple; accent on the Pastel theme.</summary>
        public const string LavenderPurple = "#9F7AEA";

        // Monochrome theme
        /// <summary>#4B5563 — cool dark charcoal; accent on the Monochrome theme.</summary>
        public const string CoolCharcoal = "#4B5563";

        // Ocean theme
        /// <summary>#A8D8F0 — soft aqua-blue; tooltip text on the Ocean theme.</summary>
        public const string SoftAquaBlue = "#A8D8F0";
        /// <summary>#20E3B2 — vivid ocean-green; positive indicator on the Ocean theme.</summary>
        public const string OceanGreen = "#20E3B2";

        // Sunset theme
        /// <summary>#FFD4A8 — peach glow; tooltip text on the Sunset theme.</summary>
        public const string PeachGlow = "#FFD4A8";

        // Forest theme
        /// <summary>#E8F0D8 — fern mist (light cream-green); tooltip text on the Forest theme.</summary>
        public const string FernMist = "#E8F0D8";

        // Neon theme
        /// <summary>#E0E0FF — soft lavender glow; tooltip text on the Neon theme.</summary>
        public const string SoftLavenderGlow = "#E0E0FF";
        /// <summary>#FF3366 — neon rose-red; negative indicator on the Neon theme.</summary>
        public const string NeonRoseRed = "#FF3366";
        /// <summary>#00D4FF — neon aqua; accent on the Neon theme.</summary>
        public const string NeonAqua = "#00D4FF";

        // Minimal theme
        /// <summary>#6366F1 — indigo-blue; accent on the Minimal theme.</summary>
        public const string IndigoBlue = "#6366F1";

        // Warm theme
        /// <summary>#FFE8CC — warm cream; tooltip text on the Warm theme.</summary>
        public const string WarmCream = "#FFE8CC";
        /// <summary>#D97706 — golden amber; positive indicator on the Warm theme.</summary>
        public const string GoldenAmber = "#D97706";
        /// <summary>#B91C1C — burnt deep-red; negative indicator on the Warm theme.</summary>
        public const string BurntRed = "#B91C1C";
        /// <summary>#C2410C — fire orange; accent on the Warm theme.</summary>
        public const string FireOrange = "#C2410C";

        // Arctic theme
        /// <summary>#DAEEFF — arctic glow (very pale icy-blue); tooltip text on the Arctic theme.</summary>
        public const string ArcticGlow = "#DAEEFF";
        /// <summary>#0891B2 — deep cyan-blue; positive indicator on the Arctic theme.</summary>
        public const string DeepCyanBlue = "#0891B2";
        /// <summary>#7C3AED — deep indigo-violet; negative indicator on the Arctic theme.</summary>
        public const string IndigoDeep = "#7C3AED";

        // Business theme
        /// <summary>#059669 — emerald teal; positive indicator on the Business theme.</summary>
        public const string EmeraldTeal = "#059669";
        /// <summary>#2563EB — true blue; accent on the Business theme.</summary>
        public const string TrueBlue = "#2563EB";

        // TrafficLight theme
        /// <summary>#F59E0B — sun amber; accent on the TrafficLight theme.</summary>
        public const string SunAmber = "#F59E0B";

        // Vivid theme
        /// <summary>#E8ECEF — gray mist; grid line colour on the Vivid theme.</summary>
        public const string GrayMist = "#E8ECEF";
        /// <summary>#2C3E50 — wet asphalt (Flat UI dark navy-gray); text colour on the Vivid theme.</summary>
        public const string WetAsphalt = "#2C3E50";

        // =================================================================
        // Curated Business Palettes (string[] for use with .Colors(…))
        // =================================================================

        /// <summary>
        /// Pre-built named colour palettes — ready to pass to <c>.Colors(…)</c> or
        /// <c>ChartTheme.Custom(colors: …)</c>.
        /// Every built-in <see cref="ChartTheme"/> references the matching palette here,
        /// making this class the single source of truth for all theme colour lists.
        /// </summary>
        public static class Palette
        {
            // ── Theme palettes ────────────────────────────────────────────────────────

            /// <summary>Blue-orange palette (matches <see cref="ChartTheme.Default"/>, 20 colours).</summary>
            public static readonly string[] Default = new[]
            {
                ChartBlue, ChartOrange, ChartGreen, ChartYellow,
                ChartIndigo, ChartRose, ChartTeal, ChartRed,
                AquaMint, MediumOrchid, SandyBrown, YellowGreen,
                Charcoal, SteelBlue, Salmon, GrapeViolet,
                Sienna, Goldenrod, CadetBlue, DarkSeaGreen
            };

            /// <summary>Dark navy palette (matches <see cref="ChartTheme.Dark"/>, 20 colours).</summary>
            public static readonly string[] Dark = new[]
            {
                BrightCornflower, BrightOrange, BrightEmerald, BrightGolden,
                SoftLavender, BrightRosePink, CyanTurquoise, SalmonPink,
                BrightSkyBlue, BrightFuchsia, SoftGolden, LightMintGreen,
                BrightIndigo, BrightRose, WarmAmber, BrightViolet,
                TealGreen, GlacialCyan, BrightLime, SoftCoral
            };

            /// <summary>Soft pastel palette (matches <see cref="ChartTheme.Pastel"/>, 20 colours).</summary>
            public static readonly string[] Pastel = new[]
            {
                PastelSkyBlue, PastelPurple, PastelPink, PastelMint,
                PastelPeach, PastelPeriwinkle, PastelGreenMint, PastelAmberOrange,
                Thistle, PowderBlue, PastelRosePink, PaleGreen,
                Lavender, PastelLime, PastelLemon, Honeydew,
                MistyRose, PaleTurquoise, PastelViolet, PastelIceBlue
            };

            /// <summary>Full greyscale palette (matches <see cref="ChartTheme.Monochrome"/>, 20 colours).</summary>
            public static readonly string[] Monochrome = new[]
            {
                Black, DimCharcoal, StoneGray, MidGray,
                GraphiteGray, AshGray, SilverMist, CloudGray,
                LightSilver, PearlGray, PlatinumGray, FrostGray,
                CoolGray, DimGray, Gray, Silver,
                LightGray, SilkGray, WhiteSmoke, OffWhite
            };

            /// <summary>Blue-teal ocean palette (matches <see cref="ChartTheme.Ocean"/>, 20 colours).</summary>
            public static readonly string[] Ocean = new[]
            {
                BrightSkyBlue, BrightEmerald, DeepCyan, ClearCyan,
                SkyMist, TealGreen, GlacialCyan, SeaMint,
                ClearBlue, OceanTeal, LavenderBlue, IceCyan,
                WongBlue, MaterialTeal, MaterialCyan, WongGreen,
                IcyBlue, AzureHaze, GlacierBlue, PaleSteelBlue
            };

            /// <summary>Warm sunset palette (matches <see cref="ChartTheme.Sunset"/>, 20 colours).</summary>
            public static readonly string[] Sunset = new[]
            {
                CoralOrange, AmberGold, RaspberryPink, Amber,
                SoftPinkRose, SoftLilac, BrightAmber, HotRed,
                ElectricPurple, BurntTangerine, GoldenShine, SoftOrchid,
                Tomato, OrangeRed, DeepOrange, DuskyRose,
                RosePurple, GrapeViolet, Gold, DeepAmberGold
            };

            /// <summary>Earthy green forest palette (matches <see cref="ChartTheme.Forest"/>, 20 colours).</summary>
            public static readonly string[] Forest = new[]
            {
                DeepForest, DeepBurntOrange, DeepIndigo, DarkMossGreen,
                BrickRed, DarkEarth, DarkForestGreen, OliveForestGreen,
                TerracottaBrown, AmberDeep, DarkTeal, DarkOliveYellow,
                EspressoBlack, FernGreen, DarkSlateGray, SaddleBrown,
                Sienna, EarthBrown, HunterGreen, WoodBrown
            };

            /// <summary>Electric neon palette (matches <see cref="ChartTheme.Neon"/>, 20 colours).</summary>
            public static readonly string[] Neon = new[]
            {
                NeonGreen, NeonPink, NeonCyan, NeonOrange,
                Yellow, Magenta, Lime, NeonRed,
                NeonCyanBlue, NeonAmber, ElectricLime, ElectricViolet,
                DeepPink, SpringGreen, Chartreuse, DeepSkyBlue,
                GreenYellow, HotPink, UltraViolet, NeonBubblegum
            };

            /// <summary>Muted professional palette (matches <see cref="ChartTheme.Minimal"/>, 20 colours).</summary>
            public static readonly string[] Minimal = new[]
            {
                TableauBlue, TableauOrange, TableauRed, TableauTeal,
                TableauGreen, TableauYellow, TableauPurple, TableauPink,
                TableauBrown, TableauGray, TableauDarkTeal, TableauLightTeal,
                TableauLightBlue, TableauLightOrange, TableauLightGreen, TableauLightRed,
                TableauLightPurple, TableauLightBrown, TableauLightPink, TableauLightYellow
            };

            /// <summary>Warm earth-tones palette (matches <see cref="ChartTheme.Warm"/>, 20 colours).</summary>
            public static readonly string[] Warm = new[]
            {
                DarkMahogany, Pomegranate, PumpkinOrange, BronzeBrown,
                DarkMocha, RusticRed, DarkOliveBrown, DeepMocha,
                DarkChocolate, BurntCopperOrange, ForestOlive, DeepNavyBlue,
                Chocolate, Peru, SaddleBrown, RosyBrown,
                Burlywood, Tan, Wheat, NavajoWhite
            };

            /// <summary>Cool arctic palette (matches <see cref="ChartTheme.Arctic"/>, 20 colours).</summary>
            public static readonly string[] Arctic = new[]
            {
                MaterialBlue800, MaterialLightBlue700, MaterialCyan700, MaterialIndigo800,
                MaterialBlue700, MaterialTeal700, MaterialLightBlue800, MaterialDeepPurple600,
                DarkTeal, MaterialLightBlue900, MaterialPurple800, MaterialCyan900,
                WongBlue, MidnightNavy, PolarNight, OceanDepth,
                OceanGrid, DeepIndigo, MaterialBlue900, MaterialIndigo600
            };

            // ── Curated / domain palettes ──────────────────────────────────────────────

            /// <summary>A strong, professional 20-colour business palette.</summary>
            public static readonly string[] Business = new[]
            {
                CeruleanBlue, CinnabarRed, PineGreen, Apricot,
                GrapeViolet, CapriBlue, Goldenrod, SteelBlue,
                MediumOrchid, CadetBlue, Sienna, DarkSeaGreen,
                MaterialBlue, MaterialGreen, MaterialRed, WongBlue,
                EmeraldGreen, Alizarin, DarkAmber, DuskyIndigo
            };

            /// <summary>Material Design 500-level palette (20 colours).</summary>
            public static readonly string[] Material = new[]
            {
                MaterialRed, MaterialBlue, MaterialGreen, Amber,
                MaterialPurple, MaterialCyan, DeepOrange, BlueGray,
                MaterialPink, MaterialIndigo, MaterialTeal, MaterialBrown,
                MaterialYellow, MaterialDeepPurple, MaterialLightBlue, MaterialLightGreen,
                MaterialLime, MaterialAmber, MaterialGray, MaterialBlueGray700
            };

            /// <summary>Traffic-light / status-indicator palette (20 colours): greens, ambers, reds plus info and neutral tones.</summary>
            public static readonly string[] TrafficLight = new[]
            {
                EmeraldGreen, DarkAmber, Alizarin, MaterialBlue800,
                DeepRoyalPurple, CinnabarRed, PineGreen, DeepAmber,
                BelizeBlue, BlueGray, DarkEmeraldTeal, BrickRed,
                MaterialGreen, MaterialRed, MaterialYellow, Amber,
                WongBlue, MaterialTeal700, AlertRed, OliveForestGreen
            };

            /// <summary>Accessible high-contrast palette safe for colour-blind users (Wong 2011 + Paul Tol extended, 20 colours).</summary>
            public static readonly string[] Accessible = new[]
            {
                WongOrange, WongSkyBlue, WongGreen, WongBlue,
                Vermilion, RosePurple, Black, AccessibleTeal,
                WongYellow, AccessibleIndigo, AccessibleGreen, AccessiblePurple,
                TolOrange, TolBlue, TolCyan, TolMagenta,
                TolRed, TolTeal, White, LightSilver
            };

            /// <summary>
            /// Vivid full-spectrum palette of modern, distinct colours (20 colours).
            /// Every hue is maximally distinct and pops on a white background.
            /// </summary>
            public static readonly string[] Vivid = new[]
            {
                VividBlue, VividIndigo, VividGreen, VividOrangeRed,
                VividOrchid, VividRed, VividCyan, VividPeach,
                CoolBlue, VividSunGold, VividHotPink, AquaMint,
                VividElectricCyan, VividOrange, VividLeafGreen, VividCrimson,
                VividPurpleIndigo, VividBrightYellow, VividTealBlue, VividMediumPurple
            };

            /// <summary>
            /// WCAG AA compliant high-contrast palette — every colour achieves ≥ 4.5:1 contrast
            /// against a white background. Ideal for accessibility-critical reports and print.
            /// </summary>
            public static readonly string[] HighContrast = new[]
            {
                "#000000", "#003399", "#8B0000", "#006400",
                "#4B0082", "#7A3B00", "#005555", "#551A8B",
                "#333333", "#005000", "#6B0000", "#003A6B",
                "#3B0070", "#5C3317", "#004444", "#2B004F"
            };
        }

        // =================================================================
        // Utility Methods
        // =================================================================

        /// <summary>
        /// Builds a hex colour string from 8-bit RGB components.
        /// </summary>
        /// <param name="r">Red channel 0–255.</param>
        /// <param name="g">Green channel 0–255.</param>
        /// <param name="b">Blue channel 0–255.</param>
        /// <returns>A hex colour string, e.g. <c>"#1A6090"</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when any component is outside 0–255.</exception>
        public static string FromRgb(int r, int g, int b)
        {
            if (r < 0 || r > 255) throw new ArgumentOutOfRangeException(nameof(r), r, "Red must be 0–255.");
            if (g < 0 || g > 255) throw new ArgumentOutOfRangeException(nameof(g), g, "Green must be 0–255.");
            if (b < 0 || b > 255) throw new ArgumentOutOfRangeException(nameof(b), b, "Blue must be 0–255.");
            return string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", r, g, b);
        }

        /// <summary>
        /// Converts a hex colour string to an <c>rgba(r,g,b,alpha)</c> string with the given opacity.
        /// Use this to create semi-transparent variants of any named colour.
        /// <code>
        /// ChartColor.WithOpacity(ChartColor.SteelBlue, 0.3)   // → "rgba(70,130,180,0.3)"
        /// ChartColor.WithOpacity("#7CB5EC", 0.15)              // → "rgba(124,181,236,0.15)"
        /// </code>
        /// </summary>
        /// <param name="hexColor">A 6- or 7-character hex colour string (e.g. <c>"#7CB5EC"</c> or <c>"7CB5EC"</c>).</param>
        /// <param name="alpha">Opacity from <c>0.0</c> (fully transparent) to <c>1.0</c> (fully opaque).</param>
        /// <returns>An <c>rgba(…)</c> CSS colour string.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hexColor"/> is not a valid 6-digit hex colour.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="alpha"/> is outside [0.0, 1.0].</exception>
        public static string WithOpacity(string hexColor, double alpha)
        {
            if (alpha < 0.0 || alpha > 1.0)
                throw new ArgumentOutOfRangeException(nameof(alpha), alpha, "Alpha must be between 0.0 and 1.0.");

            if (!TryParseHex(hexColor, out int r, out int g, out int b))
                throw new ArgumentException(
                    $"'{hexColor}' is not a valid 6-digit hex colour string (e.g. '#7CB5EC').",
                    nameof(hexColor));

            return string.Format(CultureInfo.InvariantCulture, "rgba({0},{1},{2},{3})", r, g, b, alpha);
        }

        /// <summary>
        /// Returns a lighter variant of a hex colour by blending it toward white.
        /// </summary>
        /// <param name="hexColor">Base hex colour (e.g. <c>"#4169E1"</c>).</param>
        /// <param name="amount">Blend amount 0.0 (original) – 1.0 (white). Default is 0.3.</param>
        /// <returns>Lightened hex colour string.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hexColor"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is outside [0.0, 1.0].</exception>
        public static string Lighten(string hexColor, double amount = 0.3)
        {
            if (amount < 0.0 || amount > 1.0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be between 0.0 and 1.0.");
            if (!TryParseHex(hexColor, out int r, out int g, out int b))
                throw new ArgumentException($"'{hexColor}' is not a valid 6-digit hex colour.", nameof(hexColor));

            int lr = Clamp((int)Math.Round(r + (255 - r) * amount));
            int lg = Clamp((int)Math.Round(g + (255 - g) * amount));
            int lb = Clamp((int)Math.Round(b + (255 - b) * amount));
            return FromRgb(lr, lg, lb);
        }

        /// <summary>
        /// Returns a darker variant of a hex colour by blending it toward black.
        /// </summary>
        /// <param name="hexColor">Base hex colour (e.g. <c>"#7CB5EC"</c>).</param>
        /// <param name="amount">Blend amount 0.0 (original) – 1.0 (black). Default is 0.3.</param>
        /// <returns>Darkened hex colour string.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="hexColor"/> is invalid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is outside [0.0, 1.0].</exception>
        public static string Darken(string hexColor, double amount = 0.3)
        {
            if (amount < 0.0 || amount > 1.0)
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Amount must be between 0.0 and 1.0.");
            if (!TryParseHex(hexColor, out int r, out int g, out int b))
                throw new ArgumentException($"'{hexColor}' is not a valid 6-digit hex colour.", nameof(hexColor));

            int dr = Clamp((int)Math.Round(r * (1.0 - amount)));
            int dg = Clamp((int)Math.Round(g * (1.0 - amount)));
            int db = Clamp((int)Math.Round(b * (1.0 - amount)));
            return FromRgb(dr, dg, db);
        }

        /// <summary>
        /// Mixes two hex colours together at the given ratio.
        /// </summary>
        /// <param name="hexA">First colour (e.g. <c>"#FF0000"</c>).</param>
        /// <param name="hexB">Second colour (e.g. <c>"#0000FF"</c>).</param>
        /// <param name="weight">
        /// 0.0 = 100 % colour A; 1.0 = 100 % colour B; 0.5 = equal mix. Default is 0.5.
        /// </param>
        /// <returns>Mixed hex colour string.</returns>
        public static string Mix(string hexA, string hexB, double weight = 0.5)
        {
            if (weight < 0.0 || weight > 1.0)
                throw new ArgumentOutOfRangeException(nameof(weight), weight, "Weight must be between 0.0 and 1.0.");
            if (!TryParseHex(hexA, out int r1, out int g1, out int b1))
                throw new ArgumentException($"'{hexA}' is not a valid 6-digit hex colour.", nameof(hexA));
            if (!TryParseHex(hexB, out int r2, out int g2, out int b2))
                throw new ArgumentException($"'{hexB}' is not a valid 6-digit hex colour.", nameof(hexB));

            int mr = Clamp((int)Math.Round(r1 * (1 - weight) + r2 * weight));
            int mg = Clamp((int)Math.Round(g1 * (1 - weight) + g2 * weight));
            int mb = Clamp((int)Math.Round(b1 * (1 - weight) + b2 * weight));
            return FromRgb(mr, mg, mb);
        }

        // =================================================================
        // Private helpers
        // =================================================================

        private static bool TryParseHex(string hex, out int r, out int g, out int b)
        {
            r = g = b = 0;
            if (string.IsNullOrWhiteSpace(hex)) return false;

            string h = hex.TrimStart('#');
            if (h.Length != 6) return false;

            if (!int.TryParse(h.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)) return false;
            if (!int.TryParse(h.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)) return false;
            if (!int.TryParse(h.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b)) return false;

            return true;
        }

        private static int Clamp(int value) => value < 0 ? 0 : value > 255 ? 255 : value;
    }
}
