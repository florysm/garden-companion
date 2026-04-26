using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetPlantingQuery(Guid PlantingId, Guid UserId) : IRequest<PlantingDetailDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetPlantingHandler(AppDbContext db)
    : IRequestHandler<GetPlantingQuery, PlantingDetailDto>
{
    public async Task<PlantingDetailDto> Handle(
        GetPlantingQuery request, CancellationToken cancellationToken)
    {
        // Auth + data in one query — membership check inlined into the WHERE clause.
        // KeyNotFoundException covers both "not found" and "no access" to prevent info leakage.
        return await db.Plantings
            .Where(p => p.Id == request.PlantingId)
            .Where(p =>
                p.GardenBed.Garden.Members.Any(m => m.UserId == request.UserId) ||
                p.GardenBed.Garden.Household.Members.Any(m => m.UserId == request.UserId))
            .Select(p => new PlantingDetailDto(
                p.Id,
                p.GardenBedId,
                p.GardenBed.Name,
                p.GardenBed.GardenId,
                p.PlantId,
                p.Plant.CommonName,
                p.Plant.ScientificName,
                p.Plant.Family,
                p.PlantedDate,
                p.ExpectedHarvestDate,
                p.ActualEndDate,
                p.Status,
                p.PlantingType,
                p.Source,
                p.Quantity,
                p.SeasonYear,
                p.SeasonType,
                p.IsActive,
                p.Observations.Count,
                p.HarvestLogs.Count))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Planting {request.PlantingId} not found.");
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetPlantingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plantings/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetPlantingQuery(id, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("GetPlanting");
    }
}
