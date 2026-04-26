using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace GardenCompanion.Api.Infrastructure.ExternalData;

public partial class ScrapedPlantDataService(HttpClient httpClient, ILogger<ScrapedPlantDataService> logger)
    : IPlantDataService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<List<ExternalPlantResult>> SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            var result = await TryAlmanacAsync(query, ct)
                      ?? await TryHolmesSeedAsync(query, ct)
                      ?? await TryChiliPepperMadnessAsync(query, ct)
                      ?? await TryWikipediaAsync(query, ct);

            return result is null ? [] : [result];
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plant scraping failed for query '{Query}'", query);
            return [];
        }
    }

    public async Task<ExternalPlantResult?> GetAsync(string externalId, CancellationToken ct)
    {
        var colonIdx = externalId.IndexOf(':');
        if (colonIdx < 0) return null;

        var source = externalId[..colonIdx].ToLowerInvariant();
        var id = externalId[(colonIdx + 1)..];

        try
        {
            return source switch
            {
                "almanac" => await FetchAlmanacBySlugAsync(id, ct),
                "holmesseed" => await FetchHolmesSeedBySlugAsync(id, ct),
                "chilipeppermadness" => await TryChiliPepperMadnessAsync(id.Replace('-', ' '), ct),
                "wiki" => await TryWikipediaAsync(id.Replace('_', ' '), ct),
                _ => null
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plant data fetch failed for externalId '{ExternalId}'", externalId);
            return null;
        }
    }

    private async Task<ExternalPlantResult?> FetchAlmanacBySlugAsync(string slug, CancellationToken ct)
    {
        var html = await FetchHtmlAsync($"https://www.almanac.com/plant/{slug}", ct);
        return html is null ? null : await ParseAlmanacDetailAsync(slug, html, ct);
    }

    private async Task<ExternalPlantResult?> FetchHolmesSeedBySlugAsync(string slug, CancellationToken ct)
    {
        var html = await FetchHtmlAsync($"https://www.holmesseed.com/{slug}/", ct);
        return html is null ? null : await ParseHolmesSeedDetailAsync(slug, html, ct);
    }

    // ── Almanac ───────────────────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryAlmanacAsync(string query, CancellationToken ct)
    {
        try
        {
            var slug = query.ToLowerInvariant().Trim()
                .Replace(' ', '-')
                .Replace("'", "")
                .Replace(",", "");

            // Try the direct slug URL first; fall back to search if we get a 404.
            var detailUrl = $"https://www.almanac.com/plant/{slug}";
            var html = await FetchHtmlAsync(detailUrl, ct);

            if (html is null)
            {
                detailUrl = await FindAlmanacDetailUrlAsync(query, ct);
                if (detailUrl is null) return null;
                html = await FetchHtmlAsync(detailUrl, ct);
            }

            if (html is null) return null;

            return await ParseAlmanacDetailAsync(slug, html, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Almanac scrape failed for '{Query}'", query);
            return null;
        }
    }

    private async Task<string?> FindAlmanacDetailUrlAsync(string query, CancellationToken ct)
    {
        var searchUrl = $"https://www.almanac.com/search?query={Uri.EscapeDataString(query)}";
        var html = await FetchHtmlAsync(searchUrl, ct);
        if (html is null) return null;

        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var link = doc.QuerySelectorAll("a[href]")
            .FirstOrDefault(a => a.GetAttribute("href")?.StartsWith("/plant/", StringComparison.OrdinalIgnoreCase) == true
                               && a.GetAttribute("href") != "/plant/");

        var href = link?.GetAttribute("href");
        return href is null ? null : $"https://www.almanac.com{href}";
    }

    private static async Task<ExternalPlantResult?> ParseAlmanacDetailAsync(
        string slug, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        // Bail out if this isn't a real plant page.
        var heading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(heading)) return null;

        // Scrape the growing-data table: <dt> labels paired with <dd> values.
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dts = doc.QuerySelectorAll("dt");
        foreach (var dt in dts)
        {
            var label = dt.TextContent.Trim();
            var value = (dt.NextElementSibling is IElement dd && dd.TagName == "DD")
                ? dd.TextContent.Trim()
                : null;
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        // Also try <td> pairs (some Almanac pages use a table layout).
        var tds = doc.QuerySelectorAll("td");
        for (var i = 0; i + 1 < tds.Length; i += 2)
        {
            var label = tds[i].TextContent.Trim();
            var value = tds[i + 1].TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        // Description: first substantial paragraph in the main content area.
        var description = doc
            .QuerySelectorAll("article p, .field--name-body p, .plant-description p, main p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 60);

        var commonName = heading;
        var scientificName = ExtractScientificNameFromText(doc.QuerySelector("em, i")?.TextContent);

        return new ExternalPlantResult(
            ExternalId: "almanac:" + slug,
            CommonName: commonName,
            ScientificName: scientificName,
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: ParseFirstNumber(GetValue(data, "Spacing", "Plant Spacing", "Row Spacing")),
            SunRequirement: Sanitize(GetValue(data, "Sun Exposure", "Sun", "Sunlight")),
            DaysToMaturity: ParseFirstInt(GetValue(data, "Days to Maturity", "Maturity", "Days to Harvest")),
            HeatLevelShu: null,
            WaterRequirement: Sanitize(GetValue(data, "Water Needs", "Water", "Watering")),
            MinDepthInches: ParseDepth(GetValue(data, "Planting Depth", "Depth", "Plant Depth")),
            Family: null);
    }

    // ── Holmes Seed ───────────────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryHolmesSeedAsync(string query, CancellationToken ct)
    {
        try
        {
            // Try direct slug first, then fall back to search.
            var slug = query.ToLowerInvariant().Trim()
                .Replace(' ', '-')
                .Replace("'", "")
                .Replace(",", "");

            var detailUrl = $"https://www.holmesseed.com/{slug}/";
            var html = await FetchHtmlAsync(detailUrl, ct);

            if (html is null)
            {
                detailUrl = await FindHolmesSeedDetailUrlAsync(query, ct);
                if (detailUrl is null) return null;
                html = await FetchHtmlAsync(detailUrl, ct);
            }

            if (html is null) return null;
            return await ParseHolmesSeedDetailAsync(slug, html, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Holmes Seed scrape failed for '{Query}'", query);
            return null;
        }
    }

    private async Task<string?> FindHolmesSeedDetailUrlAsync(string query, CancellationToken ct)
    {
        var searchUrl = $"https://www.holmesseed.com/?post_type=product&s={Uri.EscapeDataString(query)}";
        var html = await FetchHtmlAsync(searchUrl, ct);
        if (html is null) return null;

        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        // Product cards wrap an <img> — nav and category links don't.
        return doc.QuerySelectorAll("a[href]")
            .Where(a => a.QuerySelector("img") != null)
            .Select(a => a.GetAttribute("href"))
            .FirstOrDefault(href =>
                href != null &&
                href.StartsWith("https://www.holmesseed.com/", StringComparison.OrdinalIgnoreCase) &&
                !href.Contains('?'));
    }

    private static async Task<ExternalPlantResult?> ParseHolmesSeedDetailAsync(
        string slug, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var heading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(heading)) return null;

        // Prefer JSON-LD Product schema for description — it's already clean text.
        string? description = null;
        foreach (var script in doc.QuerySelectorAll("script[type='application/ld+json']"))
        {
            try
            {
                using var jsonDoc = JsonDocument.Parse(script.TextContent);
                var root = jsonDoc.RootElement;
                if (root.TryGetProperty("@type", out var type) && type.GetString() == "Product"
                    && root.TryGetProperty("description", out var desc))
                {
                    description = Truncate(desc.GetString() ?? "", 1800);
                    break;
                }
            }
            catch { /* malformed JSON-LD — fall through */ }
        }

        // Fall back to first substantial paragraph.
        description ??= doc.QuerySelectorAll("p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 60);

        // Parse <strong>Label:</strong> value pairs.
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var strong in doc.QuerySelectorAll("strong"))
        {
            var labelWithColon = strong.TextContent.Trim();
            var label = labelWithColon.TrimEnd(':');
            var parent = strong.ParentElement;
            if (parent is null || string.IsNullOrWhiteSpace(label)) continue;

            var fullText = parent.TextContent.Trim();
            var value = fullText.StartsWith(labelWithColon, StringComparison.Ordinal)
                ? fullText[labelWithColon.Length..].Trim()
                : null;

            if (!string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        var scientificName = ExtractScientificNameFromText(doc.QuerySelector("em, i")?.TextContent);

        return new ExternalPlantResult(
            ExternalId: "holmesseed:" + slug,
            CommonName: heading,
            ScientificName: scientificName,
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: ParseFirstNumber(GetValue(data, "Spacing", "Plant Spacing", "Row Spacing")),
            SunRequirement: Sanitize(GetValue(data, "Sun", "Sun Exposure", "Light")),
            DaysToMaturity: ParseFirstInt(GetValue(data, "Relative Days", "Days to Maturity", "Maturity")),
            HeatLevelShu: null,
            WaterRequirement: Sanitize(GetValue(data, "Water", "Water Needs", "Irrigation")),
            MinDepthInches: ParseDepth(GetValue(data, "Planting Depth", "Depth", "Seed Depth")),
            Family: null);
    }

    // ── Wikipedia ─────────────────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryWikipediaAsync(string query, CancellationToken ct)
    {
        try
        {
            var title = await WikipediaSearchTitleAsync(query, ct);
            if (title is null) return null;

            var extract = await WikipediaExtractAsync(title, ct);
            if (extract is null) return null;

            var scientificName = ExtractScientificNameFromText(extract);
            var description = Truncate(extract, 1800);

            return new ExternalPlantResult(
                ExternalId: "wiki:" + title.ToLowerInvariant().Replace(' ', '_'),
                CommonName: title,
                ScientificName: scientificName,
                Description: description,
                MinSpacingInches: null,
                SunRequirement: null,
                DaysToMaturity: null,
                HeatLevelShu: null,
                WaterRequirement: null,
                MinDepthInches: null,
                Family: null);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Wikipedia fallback failed for '{Query}'", query);
            return null;
        }
    }

    // ── Chili Pepper Madness ──────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryChiliPepperMadnessAsync(string query, CancellationToken ct)
    {
        try
        {
            var detailUrl = await FindChiliPepperMadnessUrlAsync(query, ct);
            if (detailUrl is null) return null;

            var html = await FetchHtmlAsync(detailUrl, ct);
            if (html is null) return null;

            return await ParseChiliPepperMadnessDetailAsync(detailUrl, html, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Chili Pepper Madness scrape failed for '{Query}'", query);
            return null;
        }
    }

    private async Task<string?> FindChiliPepperMadnessUrlAsync(string query, CancellationToken ct)
    {
        var searchUrl = $"https://www.chilipeppermadness.com/?s={Uri.EscapeDataString(query)}";
        var html = await FetchHtmlAsync(searchUrl, ct);
        if (html is null) return null;

        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        // Pepper variety pages live under /chili-pepper-types/; recipes and articles do not.
        return doc.QuerySelectorAll("a[href]")
            .Select(a => a.GetAttribute("href"))
            .FirstOrDefault(href =>
                href != null &&
                href.Contains("/chili-pepper-types/", StringComparison.OrdinalIgnoreCase) &&
                !href.TrimEnd('/').EndsWith("/chili-pepper-types", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<ExternalPlantResult?> ParseChiliPepperMadnessDetailAsync(
        string url, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var rawHeading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(rawHeading)) return null;

        // h1 often reads "Jaloro Chili Peppers - All About Them" — strip the suffix.
        var heading = SuffixStripRegex().Replace(rawHeading, "").Trim();

        // Gather all paragraph text for regex extraction.
        var bodyText = string.Join(" ", doc.QuerySelectorAll("article p, .entry-content p, main p")
            .Select(p => p.TextContent.Trim())
            .Where(t => t.Length > 0));

        var description = doc.QuerySelectorAll("article p, .entry-content p, main p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 80);

        var slug = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath.Trim('/').Replace('/', '-')
            : heading.ToLowerInvariant().Replace(' ', '-');

        var scientificName = ExtractScientificNameFromText(bodyText.Length > 600 ? bodyText[..600] : bodyText);

        return new ExternalPlantResult(
            ExternalId: "chilipeppermadness:" + slug,
            CommonName: heading,
            ScientificName: scientificName,
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: null,
            SunRequirement: null,
            DaysToMaturity: ParseDaysToMaturityFromText(bodyText),
            HeatLevelShu: ParseShuFromText(bodyText),
            WaterRequirement: null,
            MinDepthInches: null,
            Family: null);
    }

    [GeneratedRegex(@"\s*[-–]\s*(all about them|guide|overview|profile|info|information|facts).*$",
        RegexOptions.IgnoreCase)]
    private static partial Regex SuffixStripRegex();

    [GeneratedRegex(@"([\d,]+)\s*(?:to\s*[\d,]+\s*)?(?:SHU|Scoville\s+Heat\s+Units?|Scoville\s+units?)",
        RegexOptions.IgnoreCase)]
    private static partial Regex ShuRegex();

    [GeneratedRegex(@"(\d+)\s*days?\s*(?:to\s*(?:maturity|ripen|harvest)|to\s*fully\s*ripen)",
        RegexOptions.IgnoreCase)]
    private static partial Regex DaysToMaturityTextRegex();

    private static int? ParseShuFromText(string text)
    {
        var match = ShuRegex().Match(text);
        if (!match.Success) return null;
        var raw = match.Groups[1].Value.Replace(",", "");
        return int.TryParse(raw, out var n) ? n : null;
    }

    private static int? ParseDaysToMaturityFromText(string text)
    {
        var match = DaysToMaturityTextRegex().Match(text);
        if (!match.Success) return null;
        return int.TryParse(match.Groups[1].Value, out var n) ? n : null;
    }

    private async Task<string?> WikipediaSearchTitleAsync(string query, CancellationToken ct)
    {
        var url = $"https://en.wikipedia.org/w/api.php?action=opensearch&search={Uri.EscapeDataString(query)}&limit=1&format=json";
        var json = await FetchJsonAsync(url, ct);
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var titles = doc.RootElement[1];
        return titles.GetArrayLength() > 0 ? titles[0].GetString() : null;
    }

    private async Task<string?> WikipediaExtractAsync(string title, CancellationToken ct)
    {
        var url = $"https://en.wikipedia.org/w/api.php?action=query&prop=extracts&exintro=true&explaintext=true&titles={Uri.EscapeDataString(title)}&format=json";
        var json = await FetchJsonAsync(url, ct);
        if (json is null) return null;

        using var doc = JsonDocument.Parse(json);
        var pages = doc.RootElement.GetProperty("query").GetProperty("pages");
        foreach (var page in pages.EnumerateObject())
        {
            if (page.Value.TryGetProperty("extract", out var extract))
                return extract.GetString();
        }
        return null;
    }

    // ── HTTP helpers ──────────────────────────────────────────────────────────

    private async Task<string?> FetchHtmlAsync(string url, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
        if (!contentType.Contains("html")) return null;

        return await response.Content.ReadAsStringAsync(ct);
    }

    private async Task<string?> FetchJsonAsync(string url, CancellationToken ct)
    {
        using var response = await httpClient.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadAsStringAsync(ct);
    }

    // ── Parsing helpers ───────────────────────────────────────────────────────

    [GeneratedRegex(@"\(([A-Z][a-z]+ (?:[a-z×]+ ?)+)\)")]
    private static partial Regex ScientificNameRegex();

    private static string? ExtractScientificNameFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = ScientificNameRegex().Match(text.Length > 600 ? text[..600] : text);
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    [GeneratedRegex(@"(\d+(?:\.\d+)?)")]
    private static partial Regex FirstNumberRegex();

    private static decimal? ParseFirstNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = FirstNumberRegex().Match(value);
        return match.Success && decimal.TryParse(match.Groups[1].Value, out var d) ? d : null;
    }

    private static int? ParseFirstInt(string? value)
    {
        var d = ParseFirstNumber(value);
        return d.HasValue ? (int)d.Value : null;
    }

    [GeneratedRegex(@"([\d]+)/([\d]+)|(\d+(?:\.\d+)?)")]
    private static partial Regex DepthRegex();

    private static decimal? ParseDepth(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var match = DepthRegex().Match(value);
        if (!match.Success) return null;

        // fraction like "1/4"
        if (match.Groups[1].Success && match.Groups[2].Success
            && int.TryParse(match.Groups[1].Value, out var num)
            && int.TryParse(match.Groups[2].Value, out var den) && den != 0)
            return Math.Round((decimal)num / den, 2);

        // whole or decimal
        if (match.Groups[3].Success && decimal.TryParse(match.Groups[3].Value, out var d))
            return d;

        return null;
    }

    private static string? GetValue(Dictionary<string, string> data, params string[] keys)
    {
        foreach (var key in keys)
            if (data.TryGetValue(key, out var value)) return value;
        return null;
    }

    private static string? Sanitize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string Truncate(string value, int max)
        => value.Length <= max ? value : value[..max];
}
