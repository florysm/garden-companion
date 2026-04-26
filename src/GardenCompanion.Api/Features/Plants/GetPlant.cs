using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plants;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetPlantQuery(Guid PlantId, Guid UserId) : IRequest<PlantDetailDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetPlantHandler(AppDbContext db)
    : IRequestHandler<GetPlantQuery, PlantDetailDto>
{
    public async Task<PlantDetailDto> Handle(
        GetPlantQuery request, CancellationToken cancellationToken)
    {
        var plant = await db.Plants
            .Where(p => p.Id == request.PlantId && ((p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId))
            .Select(p => new PlantDetailDto(
                p.Id,
                p.CommonName,
                p.ScientificName,
                p.Description,
                p.Family,
                p.DaysToMaturity,
                p.HeatLevelShu,
                p.MinSpacingInches,
                p.MinDepthInches,
                p.SunRequirement,
                p.WaterRequirement,
                p.IsGlobal,
                p.IsApproved,
                p.ExternalSource,
                p.ExternalId,
                p.ContributedByUserId,
                p.CachedAt))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Plant {request.PlantId} not found.");

        return plant;
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetPlantEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plants/{id:guid}", async (
            Guid id,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            try
            {
                var userId = ctx.User.GetUserId();
                var result = await mediator.Send(new GetPlantQuery(id, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plants")
        .WithName("GetPlant");
    }
}
