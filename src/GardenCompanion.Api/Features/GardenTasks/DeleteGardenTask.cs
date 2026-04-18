using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request ──────────────────────────────────────────────────────────────────

public record DeleteGardenTaskCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid TaskId) : IRequest;

// ── Handler ──────────────────────────────────────────────────────────────────

public class DeleteGardenTaskHandler(AppDbContext db)
    : IRequestHandler<DeleteGardenTaskCommand>
{
    public async Task Handle(DeleteGardenTaskCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var task = await db.GardenTasks
            .FirstOrDefaultAsync(
                t => t.Id == request.TaskId && t.GardenId == request.GardenId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found.");

        db.GardenTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class DeleteGardenTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapDelete("/gardens/{gardenId:guid}/tasks/{taskId:guid}", async (
            Guid gardenId,
            Guid taskId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                await mediator.Send(new DeleteGardenTaskCommand(userId, gardenId, taskId), ct);
                return Results.NoContent();
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("DeleteGardenTask");
    }
}
