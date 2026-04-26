using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.ExternalData;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plants;

// ── Request / Response ───────────────────────────────────────────────────────

public record SearchPlantsQuery(Guid UserId, string? Q, string? Family, bool? IsGlobal)
    : IRequest<List<PlantSummaryDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class SearchPlantsHandler(AppDbContext db, IPlantDataService plantDataService, ILogger<SearchPlantsHandler> logger)
    : IRequestHandler<SearchPlantsQuery, List<PlantSummaryDto>>
{
    // Fall back to scraper when a text search returns fewer than this many local results.
    private const int FallbackThreshold = 3;

    public async Task<List<PlantSummaryDto>> Handle(
        SearchPlantsQuery request, CancellationToken cancellationToken)
    {
        var localResults = await RunLocalQueryAsync(request, cancellationToken);

        if (ShouldFallback(request, localResults))
        {
            var externalResults = await FetchExternalResultsAsync(request.Q!, cancellationToken);

            // Only include external hits not already represented in local results.
            var localExternalIds = localResults
                .Where(p => p.ExternalId != null)
                .Select(p => p.ExternalId!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var ext in externalResults)
            {
                if (!localExternalIds.Contains(ext.ExternalId))
                    localResults.Add(ToSummaryDto(ext));
            }
        }

        return localResults;
    }

    private static bool ShouldFallback(SearchPlantsQuery request, List<PlantSummaryDto> results)
        => !string.IsNullOrWhiteSpace(request.Q)
            && request.IsGlobal != false
            && results.Count < FallbackThreshold;

    private async Task<List<PlantSummaryDto>> RunLocalQueryAsync(
        SearchPlantsQuery request, CancellationToken ct)
    {
        var query = db.Plants
            .Where(p => (p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId);

        if (!string.IsNullOrWhiteSpace(request.Q))
        {
            var q = request.Q.ToLower();
            query = query.Where(p =>
                p.CommonName.ToLower().Contains(q) ||
                (p.ScientificName != null && p.ScientificName.ToLower().Contains(q)) ||
                (p.Family != null && p.Family.ToLower().Contains(q)));
        }

        if (!string.IsNullOrWhiteSpace(request.Family))
            query = query.Where(p => p.Family != null && p.Family.ToLower() == request.Family.ToLower());

        if (request.IsGlobal.HasValue)
            query = query.Where(p => p.IsGlobal == request.IsGlobal.Value);

        return await query
            .OrderBy(p => p.CommonName)
            .Take(100)
            .Select(p => new PlantSummaryDto(
                p.Id,
                p.CommonName,
                p.ScientificName,
                p.Family,
                p.DaysToMaturity,
                p.IsGlobal,
                p.IsApproved,
                p.ExternalSource,
                p.ExternalId))
            .ToListAsync(ct);
    }

    private async Task<List<ExternalPlantResult>> FetchExternalResultsAsync(string q, CancellationToken ct)
    {
        try { return await plantDataService.SearchAsync(q, ct); }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Plant data scraping failed for query '{Query}' — falling back to local results", q);
            return [];
        }
    }

    private static PlantSummaryDto ToSummaryDto(ExternalPlantResult ext) => new(
        Guid.Empty,
        ext.CommonName,
        ext.ScientificName,
        ext.Family,
        ext.DaysToMaturity,
        IsGlobal: true,
        IsApproved: false,
        ExternalSource: ExternalSource.Scraped,
        ExternalId: ext.ExternalId);
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class SearchPlantsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plants", async (
            string? q,
            string? family,
            bool? isGlobal,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            var result = await mediator.Send(new SearchPlantsQuery(userId, q, family, isGlobal), ct);
            return Results.Ok(result);
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("SearchPlants");
    }
}
