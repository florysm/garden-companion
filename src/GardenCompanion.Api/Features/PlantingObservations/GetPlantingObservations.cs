using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.PlantingObservations;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetPlantingObservationsQuery(
    Guid PlantingId,
    Guid UserId,
    ObservationType? ObservationType,
    int Limit) : IRequest<List<PlantingObservationDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetPlantingObservationsHandler(AppDbContext db)
    : IRequestHandler<GetPlantingObservationsQuery, List<PlantingObservationDto>>
{
    public async Task<List<PlantingObservationDto>> Handle(
        GetPlantingObservationsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var query = db.PlantingObservations
            .Where(o => o.PlantingId == request.PlantingId);

        if (request.ObservationType.HasValue)
            query = query.Where(o => o.ObservationType == request.ObservationType.Value);

        return await query
            .OrderByDescending(o => o.ObservedAt)
            .Take(request.Limit)
            .Select(o => new PlantingObservationDto(
                o.Id,
                o.PlantingId,
                o.ObservationType,
                o.Note,
                o.ObservedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetPlantingObservationsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plantings/{plantingId:guid}/observations", async (
            Guid plantingId,
            ObservationType? observationType,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetPlantingObservationsQuery(plantingId, userId, observationType, limit ?? 100), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("PlantingObservations")
        .WithName("GetPlantingObservations");
    }
}
