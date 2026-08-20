using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TerraFluent.Chart.Reporting.Api.Rendering;

/// <summary>
/// Embeds the self-hosted theme fonts (Inter, Plus Jakarta Sans, Source Serif 4, Space Grotesk,
/// Atkinson Hyperlegible) as base64 <c>@font-face</c> rules so exports render in the chart's theme
/// font while staying fully self-contained (no network fetch — works offline / inside an isolated
/// <c>&lt;img&gt;</c>). The woff2 files are embedded resources under <c>Rendering/Fonts</c>.
/// </summary>
internal static class EmbeddedFontCss
{
    // unicode-range values mirror the Google Fonts latin / latin-ext subsets (shared across families).
    private const string LatinRange =
        "U+0000-00FF, U+0131, U+0152-0153, U+02BB-02BC, U+02C6, U+02DA, U+02DC, U+0304, U+0308, U+0329, U+2000-206F, U+20AC, U+2122, U+2191, U+2193, U+2212, U+2215, U+FEFF, U+FFFD";
    private const string LatinExtRange =
        "U+0100-02BA, U+02BD-02C5, U+02C7-02CC, U+02CE-02D7, U+02DD-02FF, U+0304, U+0308, U+0329, U+1D00-1DBF, U+1E00-1E9F, U+1EF2-1EFF, U+2020, U+20A0-20AB, U+20AD-20C0, U+2113, U+2C60-2C7F, U+A720-A7FF";

    // file slug -> @font-face family display name (longest slugs first so none is masked by a prefix).
    private static readonly (string Slug, string Family)[] KnownFonts =
    {
        ("atkinson-hyperlegible", "Atkinson Hyperlegible"),
        ("plus-jakarta-sans",     "Plus Jakarta Sans"),
        ("source-serif-4",        "Source Serif 4"),
        ("space-grotesk",         "Space Grotesk"),
        ("inter",                 "Inter"),
    };

    // family (lower-case) -> "<style>@font-face...</style>"
    private static readonly Dictionary<string, string> _blocks = Build();

    /// <summary>Inter <c>@font-face</c> block — the default theme font used by the HTML report exports.</summary>
    public static string StyleBlock => _blocks.TryGetValue("inter", out var b) ? b : string.Empty;

    /// <summary>Returns the <c>@font-face</c> block for the first self-hosted family named in the
    /// given CSS font-family stack, or an empty string when none is self-hosted.</summary>
    public static string StyleBlockForStack(string? fontFamilyStack)
    {
        if (string.IsNullOrEmpty(fontFamilyStack)) return string.Empty;
        string lower = fontFamilyStack!.ToLowerInvariant();
        foreach (var (_, family) in KnownFonts)
        {
            string key = family.ToLowerInvariant();
            if (lower.Contains(key) && _blocks.TryGetValue(key, out var b)) return b;
        }
        return string.Empty;
    }

    /// <summary>
    /// Inserts the <c>@font-face</c> block for the font the SVG uses as the first child of the root
    /// <c>&lt;svg&gt;</c>, so a rasterized/isolated copy (e.g. loaded via <c>&lt;img&gt;</c>) still
    /// renders in the theme font. No-op when the SVG uses no self-hosted family.
    /// </summary>
    public static string InjectInto(string svg)
    {
        if (string.IsNullOrEmpty(svg)) return svg;
        string block = StyleBlockForStack(ExtractFontFamily(svg));
        if (block.Length == 0) return svg;
        int gt = svg.IndexOf('>');
        if (gt < 0) return svg;
        return svg.Substring(0, gt + 1) + block + svg.Substring(gt + 1);
    }

    private static string? ExtractFontFamily(string svg)
    {
        // Capture the whole stack (which may contain quoted names, e.g. 'Source Serif 4').
        var m = Regex.Match(svg, "font-family:\\s*([^;}]+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static Dictionary<string, string> Build()
    {
        var asm = Assembly.GetExecutingAssembly();
        const string marker = ".Rendering.Fonts.";
        var facesByFamily = new Dictionary<string, List<string>>();

        foreach (var name in asm.GetManifestResourceNames())
        {
            int idx = name.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0 || !name.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)) continue;
            string file = name.Substring(idx + marker.Length); // e.g. "source-serif-4-400-latin.woff2"

            string? family = null, slug = null;
            foreach (var (s, fam) in KnownFonts)
            {
                if (file.StartsWith(s + "-", StringComparison.OrdinalIgnoreCase)) { family = fam; slug = s; break; }
            }
            if (family is null || slug is null) continue;

            // Remainder is "{weight}-{subset}" (subset = latin | latin-ext).
            string rest = file.Substring(slug.Length + 1, file.Length - slug.Length - 1 - ".woff2".Length);
            int dash = rest.IndexOf('-');
            if (dash < 0) continue;
            string weight = rest.Substring(0, dash);
            string subset = rest.Substring(dash + 1);
            string range = subset.Equals("latin-ext", StringComparison.OrdinalIgnoreCase) ? LatinExtRange : LatinRange;

            using var stream = asm.GetManifestResourceStream(name);
            if (stream is null) continue;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            string b64 = Convert.ToBase64String(ms.ToArray());

            string css =
                $"@font-face{{font-family:'{family}';font-style:normal;font-weight:{weight};font-display:swap;" +
                $"src:url(data:font/woff2;base64,{b64}) format('woff2');unicode-range:{range};}}";

            string familyKey = family.ToLowerInvariant();
            if (!facesByFamily.TryGetValue(familyKey, out var list)) { list = new List<string>(); facesByFamily[familyKey] = list; }
            list.Add(css);
        }

        var blocks = new Dictionary<string, string>();
        foreach (var kv in facesByFamily)
        {
            var sb = new StringBuilder("<style>");
            foreach (var f in kv.Value) sb.Append(f);
            sb.Append("</style>");
            blocks[kv.Key] = sb.ToString();
        }
        return blocks;
    }
}
