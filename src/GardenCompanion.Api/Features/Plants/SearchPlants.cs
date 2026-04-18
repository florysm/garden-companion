using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plants;

// ── Request / Response ───────────────────────────────────────────────────────

public record SearchPlantsQuery(Guid UserId, string? Q, string? Family, bool? IsGlobal)
    : IRequest<List<PlantSummaryDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class SearchPlantsHandler(AppDbContext db)
    : IRequestHandler<SearchPlantsQuery, List<PlantSummaryDto>>
{
    public async Task<List<PlantSummaryDto>> Handle(
        SearchPlantsQuery request, CancellationToken cancellationToken)
    {
        var query = db.Plants.AsQueryable();

        // Return approved global plants or plants contributed by the requesting user
        query = query.Where(p => (p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId);

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
                p.ExternalSource))
            .ToListAsync(cancellationToken);
    }
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
