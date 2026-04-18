using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.HarvestLogs;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetHarvestLogsQuery(Guid PlantingId, Guid UserId, int Limit) : IRequest<List<HarvestLogDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetHarvestLogsHandler(AppDbContext db)
    : IRequestHandler<GetHarvestLogsQuery, List<HarvestLogDto>>
{
    public async Task<List<HarvestLogDto>> Handle(
        GetHarvestLogsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        return await db.HarvestLogs
            .Where(h => h.PlantingId == request.PlantingId)
            .OrderByDescending(h => h.HarvestDate)
            .Take(request.Limit)
            .Select(h => new HarvestLogDto(
                h.Id,
                h.PlantingId,
                h.HarvestedByUserId,
                h.HarvestedBy.DisplayName,
                h.HarvestDate,
                h.Quantity,
                h.QuantityUnit,
                h.Notes,
                h.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetHarvestLogsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/plantings/{plantingId:guid}/harvests", async (
            Guid plantingId,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new GetHarvestLogsQuery(plantingId, userId, limit ?? 100), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("HarvestLogs")
        .WithName("GetHarvestLogs");
    }
}
