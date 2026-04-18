using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetPlantingsQuery(
    Guid GardenId,
    Guid UserId,
    Guid? BedId,
    int? SeasonYear,
    SeasonType? SeasonType,
    PlantingStatus? Status,
    string? PlantFamily,
    Guid? PlantId,
    PlantingType? PlantingType,
    bool? IsActive,
    int Limit) : IRequest<List<PlantingSummaryDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetPlantingsHandler(AppDbContext db)
    : IRequestHandler<GetPlantingsQuery, List<PlantingSummaryDto>>
{
    public async Task<List<PlantingSummaryDto>> Handle(
        GetPlantingsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        var query = db.Plantings
            .Where(p => p.GardenBed.GardenId == request.GardenId);

        if (request.BedId.HasValue)
            query = query.Where(p => p.GardenBedId == request.BedId.Value);

        if (request.SeasonYear.HasValue)
            query = query.Where(p => p.SeasonYear == request.SeasonYear.Value);

        if (request.SeasonType.HasValue)
            query = query.Where(p => p.SeasonType == request.SeasonType.Value);

        if (request.Status.HasValue)
            query = query.Where(p => p.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.PlantFamily))
            query = query.Where(p => p.Plant.Family != null && p.Plant.Family.ToLower() == request.PlantFamily.ToLower());

        if (request.PlantId.HasValue)
            query = query.Where(p => p.PlantId == request.PlantId.Value);

        if (request.PlantingType.HasValue)
            query = query.Where(p => p.PlantingType == request.PlantingType.Value);

        if (request.IsActive.HasValue)
            query = query.Where(p => p.IsActive == request.IsActive.Value);

        return await query
            .OrderByDescending(p => p.PlantedDate)
            .Take(request.Limit)
            .Select(p => new PlantingSummaryDto(
                p.Id,
                p.GardenBedId,
                p.GardenBed.Name,
                p.PlantId,
                p.Plant.CommonName,
                p.PlantedDate,
                p.ExpectedHarvestDate,
                p.ActualEndDate,
                p.Status,
                p.PlantingType,
                p.Quantity,
                p.SeasonYear,
                p.SeasonType,
                p.IsActive))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetPlantingsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/plantings", async (
            Guid gardenId,
            Guid? bedId,
            int? seasonYear,
            SeasonType? seasonType,
            PlantingStatus? status,
            string? plantFamily,
            Guid? plantId,
            PlantingType? plantingType,
            bool? isActive,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetPlantingsQuery(gardenId, userId, bedId, seasonYear, seasonType, status, plantFamily, plantId, plantingType, isActive, limit ?? 200),
                    ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("GetPlantings");
    }
}
