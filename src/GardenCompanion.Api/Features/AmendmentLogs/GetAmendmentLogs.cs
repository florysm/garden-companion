using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.AmendmentLogs;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetAmendmentLogsQuery(
    Guid GardenId,
    Guid GardenBedId,
    Guid UserId,
    AmendmentType? AmendmentType,
    Guid? PlantingId,
    int Limit) : IRequest<List<AmendmentLogDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetAmendmentLogsHandler(AppDbContext db)
    : IRequestHandler<GetAmendmentLogsQuery, List<AmendmentLogDto>>
{
    public async Task<List<AmendmentLogDto>> Handle(
        GetAmendmentLogsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        var bedExists = await db.GardenBeds
            .AnyAsync(b => b.Id == request.GardenBedId && b.GardenId == request.GardenId, cancellationToken);
        if (!bedExists)
            throw new KeyNotFoundException($"Garden bed {request.GardenBedId} not found.");

        var query = db.AmendmentLogs
            .Where(l => l.GardenBedId == request.GardenBedId);

        if (request.AmendmentType.HasValue)
            query = query.Where(l => l.AmendmentType == request.AmendmentType.Value);

        if (request.PlantingId.HasValue)
            query = query.Where(l => l.PlantingId == request.PlantingId.Value);

        return await query
            .OrderByDescending(l => l.AppliedAt)
            .Take(request.Limit)
            .Select(l => new AmendmentLogDto(
                l.Id,
                l.GardenBedId,
                l.PlantingId,
                l.AppliedAt,
                l.ProductName,
                l.AmendmentType,
                l.Quantity,
                l.QuantityUnit,
                l.Notes))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetAmendmentLogsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/beds/{bedId:guid}/amendments", async (
            Guid gardenId,
            Guid bedId,
            AmendmentType? amendmentType,
            Guid? plantingId,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetAmendmentLogsQuery(gardenId, bedId, userId, amendmentType, plantingId, limit ?? 100), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("AmendmentLogs")
        .WithName("GetAmendmentLogs");
    }
}
