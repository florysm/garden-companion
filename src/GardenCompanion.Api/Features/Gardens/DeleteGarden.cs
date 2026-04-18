using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Gardens;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteGardenCommand(Guid GardenId, Guid CurrentUserId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class DeleteGardenHandler(AppDbContext db)
    : IRequestHandler<DeleteGardenCommand>
{
    public async Task Handle(DeleteGardenCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireOwnerAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var garden = await db.Gardens
            .FirstOrDefaultAsync(g => g.Id == request.GardenId, cancellationToken)
            ?? throw new KeyNotFoundException($"Garden {request.GardenId} not found.");

        db.Gardens.Remove(garden);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class DeleteGardenEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/gardens/{id:guid}", async (
            Guid id,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new DeleteGardenCommand(id, userId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (UnauthorizedAccessException) { return Results.Forbid(); }
        })
        .RequireAuthorization()
        .WithTags("Gardens")
        .WithName("DeleteGarden");
    }
}
