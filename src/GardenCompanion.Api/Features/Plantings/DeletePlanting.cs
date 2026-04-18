using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeletePlantingCommand(Guid PlantingId, Guid UserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class DeletePlantingHandler(AppDbContext db)
    : IRequestHandler<DeletePlantingCommand>
{
    public async Task Handle(DeletePlantingCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var planting = await db.Plantings
            .FirstOrDefaultAsync(p => p.Id == request.PlantingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Planting {request.PlantingId} not found.");

        planting.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class DeletePlantingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/plantings/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new DeletePlantingCommand(id, userId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("DeletePlanting");
    }
}
