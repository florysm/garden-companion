using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.PestDiseaseLogs;

// ── Request / Response ───────────────────────────────────────────────────────

public record GetPestDiseaseLogsQuery(
    Guid GardenId,
    Guid GardenBedId,
    Guid UserId,
    bool? IsResolved,
    int Limit) : IRequest<List<PestDiseaseLogDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetPestDiseaseLogsHandler(AppDbContext db)
    : IRequestHandler<GetPestDiseaseLogsQuery, List<PestDiseaseLogDto>>
{
    public async Task<List<PestDiseaseLogDto>> Handle(
        GetPestDiseaseLogsQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        var bedExists = await db.GardenBeds
            .AnyAsync(b => b.Id == request.GardenBedId && b.GardenId == request.GardenId, cancellationToken);
        if (!bedExists)
            throw new KeyNotFoundException($"Garden bed {request.GardenBedId} not found.");

        var query = db.PestDiseaseLogs
            .Where(l => l.GardenBedId == request.GardenBedId);

        if (request.IsResolved.HasValue)
        {
            query = request.IsResolved.Value
                ? query.Where(l => l.ResolvedAt != null)
                : query.Where(l => l.ResolvedAt == null);
        }

        return await query
            .OrderByDescending(l => l.ObservedAt)
            .Take(request.Limit)
            .Select(l => new PestDiseaseLogDto(
                l.Id,
                l.GardenBedId,
                l.PlantingId,
                l.ObservedAt,
                l.Type,
                l.Name,
                l.Severity,
                l.TreatmentApplied,
                l.ResolvedAt,
                l.Notes))
            .ToListAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetPestDiseaseLogsEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/beds/{bedId:guid}/pest-disease-logs", async (
            Guid gardenId,
            Guid bedId,
            bool? isResolved,
            int? limit,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new GetPestDiseaseLogsQuery(gardenId, bedId, userId, isResolved, limit ?? 100), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("PestDiseaseLogs")
        .WithName("GetPestDiseaseLogs");
    }
}
