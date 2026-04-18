using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.PestDiseaseLogs;

// ── Request / Response ───────────────────────────────────────────────────────

public record ResolvePestDiseaseLogCommand(
    Guid GardenId,
    Guid GardenBedId,
    Guid LogId,
    Guid UserId) : IRequest<PestDiseaseLogDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class ResolvePestDiseaseLogHandler(AppDbContext db)
    : IRequestHandler<ResolvePestDiseaseLogCommand, PestDiseaseLogDto>
{
    public async Task<PestDiseaseLogDto> Handle(
        ResolvePestDiseaseLogCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        var log = await db.PestDiseaseLogs
            .FirstOrDefaultAsync(
                l => l.Id == request.LogId && l.GardenBedId == request.GardenBedId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Pest/disease log {request.LogId} not found.");

        if (log.ResolvedAt is not null)
            return ToDto(log); // Already resolved — idempotent

        log.ResolvedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return ToDto(log);
    }

    private static PestDiseaseLogDto ToDto(Domain.Entities.PestDiseaseLog l) => new(
        l.Id, l.GardenBedId, l.PlantingId, l.ObservedAt,
        l.Type, l.Name, l.Severity, l.TreatmentApplied, l.ResolvedAt, l.Notes);
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class ResolvePestDiseaseLogEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch(
            "/gardens/{gardenId:guid}/beds/{bedId:guid}/pest-disease-logs/{id:guid}/resolve",
            async (
                Guid gardenId,
                Guid bedId,
                Guid id,
                HttpContext ctx,
                IMediator mediator,
                CancellationToken ct) =>
            {
                var userId = ctx.User.GetUserId();
                try
                {
                    var result = await mediator.Send(
                        new ResolvePestDiseaseLogCommand(gardenId, bedId, id, userId), ct);
                    return Results.Ok(result);
                }
                catch (KeyNotFoundException) { return Results.NotFound(); }
            })
        .RequireAuthorization()
        .WithTags("PestDiseaseLogs")
        .WithName("ResolvePestDiseaseLog");
    }
}
