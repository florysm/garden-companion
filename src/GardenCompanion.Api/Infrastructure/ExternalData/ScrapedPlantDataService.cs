using System.Text.Json;
using System.Text.RegularExpressions;
using AngleSharp;
using AngleSharp.Dom;

namespace GardenCompanion.Api.Infrastructure.ExternalData;

public partial class ScrapedPlantDataService(HttpClient httpClient, ILogger<ScrapedPlantDataService> logger)
    : IPlantDataService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    // Maps alias names → canonical cultivar name used in _cultivarSourceMap lookups and local DB search.
    private static readonly Dictionary<string, string> _aliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Bananarama"]                = "Sweet Banana Whopper",
        ["Burpee Sweet Bananarama"]   = "Sweet Banana Whopper",
        ["Cubanelle"]                 = "Sweet Chili Pepper",
        ["Hungarian Wax"]             = "Hot Banana Pepper",
        ["Hot Banana Hungarian Wax"]  = "Hot Banana Pepper",
        ["Marconi Red"]               = "Marconi Pepper",
        ["Giant Marconi"]             = "Marconi Pepper",
        // Tomatoes
        ["Brandywine"]               = "Brandywine Tomato",
        ["San Marzano"]              = "San Marzano Tomato",
        ["Sungold"]                  = "Sungold Tomato",
        ["Better Boy"]               = "Better Boy Tomato",
        ["Celebrity"]                = "Celebrity Tomato",
        ["Early Girl"]               = "Early Girl Tomato",
        ["Black Krim"]               = "Black Krim Tomato",
        // Cucumbers
        ["Straight Eight"]           = "Straight Eight Cucumber",
        ["Marketmore"]               = "Marketmore Cucumber",
        // Squash
        ["Black Beauty"]             = "Black Beauty Zucchini",
        // Garlic
        ["Music"]                    = "Music Garlic",
        // Herbs
        ["Genovese"]                 = "Genovese Basil",
        ["Fernleaf"]                 = "Fernleaf Dill",
    };

    // Maps canonical cultivar name → ordered list of (sourcePrefix, querySlug) to try via GetAsync.
    // The querySlug is passed as the ID portion of the ExternalId (e.g. "burpee:cajun-belle-pepper").
    private static readonly Dictionary<string, List<(string Source, string Slug)>> _cultivarSourceMap
        = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Cajun Belle"]                    = [("burpee", "cajun-belle-pepper"), ("almanac", "cajun-belle-pepper")],
        ["Jaloro Pepper"]                  = [("chilipeppermadness", "jaloro-chili-peppers"), ("almanac", "jaloro-pepper")],
        ["Sweet Banana Whopper"]           = [("burpee", "sweet-banana-whopper-pepper"), ("almanac", "sweet-banana-pepper")],
        ["Giant Ristra Chili Pepper"]      = [("almanac", "ristra-chili-pepper"), ("chilipeppermadness", "ristra-chili-peppers")],
        ["Sweet Chili Pepper"]             = [("almanac", "cubanelle-pepper"), ("chilipeppermadness", "cubanelle-peppers")],
        ["Compadre Pepper"]                = [("almanac", "compadre-pepper")],
        ["Red Knight Pepper"]              = [("almanac", "red-knight-pepper")],
        ["Keystone Resistant Giant Pepper"]= [("almanac", "keystone-resistant-giant-pepper")],
        ["Marconi Pepper"]                 = [("almanac", "marconi-pepper"), ("chilipeppermadness", "marconi-peppers")],
        ["Golden Marconi Pepper"]          = [("almanac", "golden-marconi-pepper")],
        ["Hot Banana Pepper"]              = [("almanac", "hot-banana-pepper"), ("chilipeppermadness", "banana-peppers")],
        ["Yellow Bell Pepper"]             = [("almanac", "yellow-bell-pepper")],
        ["Orange Bell Pepper"]             = [("almanac", "orange-bell-pepper")],
        ["Burpless Bush Cucumber"]         = [("almanac", "burpless-bush-cucumber")],
        ["Homemade Pickles Cucumber"]      = [("almanac", "homemade-pickles-cucumber"), ("holmesseed", "homemade-pickles-cucumber")],
        ["Early Bell Pepper"]              = [("almanac", "bell-pepper")],
        // ── Tomatoes ─────────────────────────────────────────────────────────
        ["Better Boy Tomato"]             = [("almanac", "better-boy-tomato"), ("burpee", "better-boy-tomato")],
        ["Celebrity Tomato"]              = [("almanac", "celebrity-tomato")],
        ["Early Girl Tomato"]             = [("almanac", "early-girl-tomato")],
        ["Brandywine Tomato"]             = [("almanac", "brandywine-tomato")],
        ["Cherokee Purple Tomato"]        = [("almanac", "cherokee-purple-tomato")],
        ["San Marzano Tomato"]            = [("almanac", "san-marzano-tomato")],
        ["Yellow Pear Tomato"]            = [("almanac", "yellow-pear-tomato")],
        ["Mortgage Lifter Tomato"]        = [("almanac", "mortgage-lifter-tomato")],
        ["Sungold Tomato"]                = [("almanac", "sungold-tomato"), ("burpee", "sungold-tomato")],
        ["Black Krim Tomato"]             = [("almanac", "black-krim-tomato")],
        // ── Cucumbers ────────────────────────────────────────────────────────
        ["Straight Eight Cucumber"]       = [("almanac", "straight-eight-cucumber"), ("holmesseed", "straight-eight-cucumber")],
        ["Marketmore Cucumber"]           = [("almanac", "marketmore-cucumber"), ("holmesseed", "marketmore-cucumber")],
        ["Lemon Cucumber"]                = [("almanac", "lemon-cucumber")],
        ["Armenian Cucumber"]             = [("almanac", "armenian-cucumber")],
        ["Boston Pickling Cucumber"]      = [("almanac", "boston-pickling-cucumber"), ("holmesseed", "boston-pickling-cucumber")],
        // ── Squash ───────────────────────────────────────────────────────────
        ["Black Beauty Zucchini"]         = [("almanac", "black-beauty-zucchini"), ("burpee", "black-beauty-zucchini")],
        ["Acorn Squash"]                  = [("almanac", "acorn-squash")],
        ["Butternut Squash"]              = [("almanac", "butternut-squash")],
        ["Spaghetti Squash"]              = [("almanac", "spaghetti-squash")],
        ["Delicata Squash"]               = [("almanac", "delicata-squash")],
        // ── Garlic ───────────────────────────────────────────────────────────
        ["Music Garlic"]                  = [("almanac", "music-garlic")],
        ["German Red Garlic"]             = [("almanac", "german-red-garlic")],
        ["Inchelium Red Garlic"]          = [("almanac", "inchelium-red-garlic")],
        ["California Early White Garlic"] = [("almanac", "california-early-white-garlic")],
        // ── Herbs ────────────────────────────────────────────────────────────
        ["Genovese Basil"]                = [("almanac", "genovese-basil"), ("burpee", "genovese-basil")],
        ["Fernleaf Dill"]                 = [("almanac", "fernleaf-dill"), ("burpee", "fernleaf-dill")],
        ["Italian Flat Leaf Parsley"]     = [("almanac", "italian-flat-leaf-parsley")],
        ["Greek Oregano"]                 = [("almanac", "greek-oregano")],
        ["English Lavender"]              = [("almanac", "english-lavender")],
    };

    public async Task<List<ExternalPlantResult>> SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            // Resolve alias → canonical name before any scraping attempt.
            var resolvedQuery = _aliases.TryGetValue(query.Trim(), out var canonical) ? canonical : query;

            // Check cultivar source map first — try each designated source in order.
            if (_cultivarSourceMap.TryGetValue(resolvedQuery.Trim(), out var sources))
            {
                foreach (var (source, slug) in sources)
                {
                    var cultivarResult = await GetAsync($"{source}:{slug}", ct);
                    if (cultivarResult is not null)
                        return [cultivarResult];
                }
            }

            // Generic fallback chain for everything else.
            var result = await TryAlmanacAsync(resolvedQuery, ct)
                      ?? await TryHolmesSeedAsync(resolvedQuery, ct)
                      ?? await TryChiliPepperMadnessAsync(resolvedQuery, ct)
                      ?? await TryWikipediaAsync(resolvedQuery, ct);

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
                "almanac"            => await FetchAlmanacBySlugAsync(id, ct),
                "holmesseed"         => await FetchHolmesSeedBySlugAsync(id, ct),
                "chilipeppermadness" => await TryChiliPepperMadnessAsync(id.Replace('-', ' '), ct),
                "wiki"               => await TryWikipediaAsync(id.Replace('_', ' '), ct),
                "burpee"             => await TryBurpeeAsync(id.Replace('-', ' '), ct),
                "trueleaf"           => await TryTrueLeafMarketAsync(id.Replace('-', ' '), ct),
                "botanical"          => await TryBotanicalInterestsAsync(id.Replace('-', ' '), ct),
                _                    => null
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plant data fetch failed for externalId '{ExternalId}'", externalId);
            return null;
        }
    }

    public IReadOnlyList<string> GetCultivarNames() => _cultivarSourceMap.Keys.ToList();

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

    // ── Burpee ────────────────────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryBurpeeAsync(string query, CancellationToken ct)
    {
        try
        {
            var searchUrl = $"https://www.burpee.com/search?q={Uri.EscapeDataString(query)}&view=list";
            var html = await FetchHtmlAsync(searchUrl, ct);
            if (html is null) return null;

            var context = BrowsingContext.New(Configuration.Default);
            using var searchDoc = await context.OpenAsync(req => req.Content(html), ct);

            // Find the first product link — Burpee product URLs contain "prod" and are under burpee.com
            var productUrl = searchDoc.QuerySelectorAll("a[href]")
                .Select(a => a.GetAttribute("href"))
                .FirstOrDefault(href =>
                    href != null &&
                    href.Contains("burpee.com/", StringComparison.OrdinalIgnoreCase) &&
                    (href.Contains("prod") || href.Contains("/vegetables/") || href.Contains("/flowers/")) &&
                    !href.Contains("/search"));

            if (productUrl is null) return null;

            var productHtml = await FetchHtmlAsync(productUrl, ct);
            if (productHtml is null) return null;

            return await ParseBurpeeDetailAsync(productUrl, productHtml, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Burpee scrape failed for '{Query}'", query);
            return null;
        }
    }

    private static async Task<ExternalPlantResult?> ParseBurpeeDetailAsync(
        string url, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var heading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(heading)) return null;

        // Burpee Quick Guide: try <dt>/<dd> pairs and <li> label:value patterns
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dts = doc.QuerySelectorAll("dt");
        foreach (var dt in dts)
        {
            var label = dt.TextContent.Trim();
            var value = (dt.NextElementSibling is IElement dd && dd.TagName == "DD")
                ? dd.TextContent.Trim() : null;
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        // Also try table rows
        var tds = doc.QuerySelectorAll("td");
        for (var i = 0; i + 1 < tds.Length; i += 2)
        {
            var label = tds[i].TextContent.Trim();
            var value = tds[i + 1].TextContent.Trim();
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        var description = doc.QuerySelectorAll(".product-description p, .product-detail p, main p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 60);

        var bodyText = string.Join(" ", doc.QuerySelectorAll("main p, .product-description p")
            .Select(p => p.TextContent.Trim())
            .Where(t => t.Length > 0));

        var slug = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath.Trim('/').Replace('/', '-')
            : heading.ToLowerInvariant().Replace(' ', '-');

        return new ExternalPlantResult(
            ExternalId: "burpee:" + slug,
            CommonName: heading,
            ScientificName: ExtractScientificNameFromText(bodyText.Length > 600 ? bodyText[..600] : bodyText),
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: ParseFirstNumber(GetValue(data, "Spacing", "Plant Spacing", "Space Between Plants")),
            SunRequirement: Sanitize(GetValue(data, "Sun", "Sun Exposure", "Light")),
            DaysToMaturity: ParseFirstInt(GetValue(data, "Days to Maturity", "Maturity", "Days to Harvest"))
                ?? ParseDaysToMaturityFromText(bodyText),
            HeatLevelShu: ParseShuFromText(bodyText),
            WaterRequirement: Sanitize(GetValue(data, "Water", "Water Needs", "Watering")),
            MinDepthInches: ParseDepth(GetValue(data, "Planting Depth", "Depth", "Plant Depth")),
            Family: null,
            FruitSizeDescription: Sanitize(GetValue(data, "Fruit Size", "Size", "Average Fruit Size")));
    }

    // ── True Leaf Market ──────────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryTrueLeafMarketAsync(string query, CancellationToken ct)
    {
        try
        {
            var searchUrl = $"https://www.trueleafmarket.com/search?type=product&q={Uri.EscapeDataString(query)}";
            var html = await FetchHtmlAsync(searchUrl, ct);
            if (html is null) return null;

            var context = BrowsingContext.New(Configuration.Default);
            using var searchDoc = await context.OpenAsync(req => req.Content(html), ct);

            var productUrl = searchDoc.QuerySelectorAll("a[href]")
                .Select(a => a.GetAttribute("href"))
                .FirstOrDefault(href =>
                    href != null &&
                    href.Contains("trueleafmarket.com/products/", StringComparison.OrdinalIgnoreCase));

            if (productUrl is null) return null;

            var productHtml = await FetchHtmlAsync(productUrl, ct);
            if (productHtml is null) return null;

            return await ParseTrueLeafMarketDetailAsync(productUrl, productHtml, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "True Leaf Market scrape failed for '{Query}'", query);
            return null;
        }
    }

    private static async Task<ExternalPlantResult?> ParseTrueLeafMarketDetailAsync(
        string url, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var heading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(heading)) return null;

        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // True Leaf Market uses <li> items with "Label: Value" format in spec sections
        foreach (var li in doc.QuerySelectorAll("li"))
        {
            var text = li.TextContent.Trim();
            var colonIdx = text.IndexOf(':');
            if (colonIdx > 0 && colonIdx < 40)
            {
                var label = text[..colonIdx].Trim();
                var value = text[(colonIdx + 1)..].Trim();
                if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                    data.TryAdd(label, value);
            }
        }

        // Also try <strong>Label:</strong> pairs
        foreach (var strong in doc.QuerySelectorAll("strong"))
        {
            var labelWithColon = strong.TextContent.Trim();
            var label = labelWithColon.TrimEnd(':');
            var parent = strong.ParentElement;
            if (parent is null) continue;
            var fullText = parent.TextContent.Trim();
            var value = fullText.StartsWith(labelWithColon, StringComparison.Ordinal)
                ? fullText[labelWithColon.Length..].Trim() : null;
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        var description = doc.QuerySelectorAll(".product-description p, .rte p, main p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 60);

        var bodyText = string.Join(" ", doc.QuerySelectorAll("main p, .product-description p, .rte p")
            .Select(p => p.TextContent.Trim())
            .Where(t => t.Length > 0));

        var slug = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath.Trim('/').Replace('/', '-')
            : heading.ToLowerInvariant().Replace(' ', '-');

        return new ExternalPlantResult(
            ExternalId: "trueleaf:" + slug,
            CommonName: heading,
            ScientificName: ExtractScientificNameFromText(bodyText.Length > 600 ? bodyText[..600] : bodyText),
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: ParseFirstNumber(GetValue(data, "Spacing", "Plant Spacing", "Space")),
            SunRequirement: Sanitize(GetValue(data, "Sun", "Sun Exposure", "Light Requirements")),
            DaysToMaturity: ParseFirstInt(GetValue(data, "Days to Maturity", "Maturity", "Days"))
                ?? ParseDaysToMaturityFromText(bodyText),
            HeatLevelShu: ParseShuFromText(bodyText),
            WaterRequirement: Sanitize(GetValue(data, "Water", "Water Needs")),
            MinDepthInches: ParseDepth(GetValue(data, "Planting Depth", "Depth", "Seed Depth")),
            Family: null,
            FruitSizeDescription: Sanitize(GetValue(data, "Fruit Size", "Size", "Fruit Length")),
            DiseaseResistanceNotes: Sanitize(GetValue(data, "Disease Resistance", "Resistance", "Tolerances")));
    }

    // ── Botanical Interests ───────────────────────────────────────────────────

    private async Task<ExternalPlantResult?> TryBotanicalInterestsAsync(string query, CancellationToken ct)
    {
        try
        {
            var searchUrl = $"https://www.botanicalinterests.com/search?q={Uri.EscapeDataString(query)}";
            var html = await FetchHtmlAsync(searchUrl, ct);
            if (html is null) return null;

            var context = BrowsingContext.New(Configuration.Default);
            using var searchDoc = await context.OpenAsync(req => req.Content(html), ct);

            var productUrl = searchDoc.QuerySelectorAll("a[href]")
                .Select(a => a.GetAttribute("href"))
                .FirstOrDefault(href =>
                    href != null &&
                    href.Contains("botanicalinterests.com/products/", StringComparison.OrdinalIgnoreCase));

            if (productUrl is null) return null;

            var productHtml = await FetchHtmlAsync(productUrl, ct);
            if (productHtml is null) return null;

            return await ParseBotanicalInterestsDetailAsync(productUrl, productHtml, ct);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Botanical Interests scrape failed for '{Query}'", query);
            return null;
        }
    }

    private static async Task<ExternalPlantResult?> ParseBotanicalInterestsDetailAsync(
        string url, string html, CancellationToken ct)
    {
        var context = BrowsingContext.New(Configuration.Default);
        using var doc = await context.OpenAsync(req => req.Content(html), ct);

        var heading = doc.QuerySelector("h1")?.TextContent?.Trim();
        if (string.IsNullOrWhiteSpace(heading)) return null;

        // Botanical Interests has structured Sowing + Growing sections with <dt>/<dd> pairs
        var data = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var dts = doc.QuerySelectorAll("dt");
        foreach (var dt in dts)
        {
            var label = dt.TextContent.Trim();
            var value = (dt.NextElementSibling is IElement dd && dd.TagName == "DD")
                ? dd.TextContent.Trim() : null;
            if (!string.IsNullOrWhiteSpace(label) && !string.IsNullOrWhiteSpace(value))
                data.TryAdd(label, value);
        }

        var description = doc.QuerySelectorAll(".product-description p, .description p, main p")
            .Select(p => p.TextContent.Trim())
            .FirstOrDefault(t => t.Length > 60);

        var bodyText = string.Join(" ", doc.QuerySelectorAll("main p, .product-description p")
            .Select(p => p.TextContent.Trim())
            .Where(t => t.Length > 0));

        var slug = Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.AbsolutePath.Trim('/').Replace('/', '-')
            : heading.ToLowerInvariant().Replace(' ', '-');

        return new ExternalPlantResult(
            ExternalId: "botanical:" + slug,
            CommonName: heading,
            ScientificName: ExtractScientificNameFromText(bodyText.Length > 600 ? bodyText[..600] : bodyText),
            Description: description is null ? null : Truncate(description, 1800),
            MinSpacingInches: ParseFirstNumber(GetValue(data, "Spacing", "Plant Spacing", "Thin to")),
            SunRequirement: Sanitize(GetValue(data, "Sun", "Light", "Sun Exposure")),
            DaysToMaturity: ParseFirstInt(GetValue(data, "Days to Maturity", "Maturity", "Days from Transplant"))
                ?? ParseDaysToMaturityFromText(bodyText),
            HeatLevelShu: ParseShuFromText(bodyText),
            WaterRequirement: Sanitize(GetValue(data, "Water", "Water Needs", "Watering")),
            MinDepthInches: ParseDepth(GetValue(data, "Planting Depth", "Depth", "Sow Depth")),
            Family: null,
            FruitSizeDescription: Sanitize(GetValue(data, "Fruit Size", "Size", "Fruit")),
            DiseaseResistanceNotes: Sanitize(GetValue(data, "Disease Resistance", "Resistance", "Tolerant to")));
    }

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
