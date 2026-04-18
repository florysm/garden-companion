using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenBeds;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteGardenBedCommand(Guid GardenId, Guid BedId, Guid CurrentUserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class DeleteGardenBedHandler(AppDbContext db)
    : IRequestHandler<DeleteGardenBedCommand>
{
    public async Task Handle(DeleteGardenBedCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var bed = await db.GardenBeds
            .FirstOrDefaultAsync(
                b => b.Id == request.BedId && b.GardenId == request.GardenId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Bed {request.BedId} not found.");

        // Planting.GardenBedId is Restrict — refuse if active plantings exist.
        var activePlantings = await db.Plantings
            .CountAsync(p => p.GardenBedId == request.BedId, cancellationToken);

        if (activePlantings > 0)
            throw new InvalidOperationException(
                $"Cannot delete bed '{bed.Name}': it has {activePlantings} active planting(s). " +
                "Archive or remove all plantings first.");

        db.GardenBeds.Remove(bed);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class DeleteGardenBedEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/gardens/{gardenId:guid}/beds/{bedId:guid}", async (
            Guid gardenId,
            Guid bedId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new DeleteGardenBedCommand(gardenId, bedId, userId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("GardenBeds")
        .WithName("DeleteGardenBed");
    }
}
