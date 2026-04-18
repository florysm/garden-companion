using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request / Response ──────────────────────────────────────────────────────

public record CompleteGardenTaskCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid TaskId) : IRequest<GardenTaskDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class CompleteGardenTaskHandler(AppDbContext db)
    : IRequestHandler<CompleteGardenTaskCommand, GardenTaskDto>
{
    public async Task<GardenTaskDto> Handle(
        CompleteGardenTaskCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var task = await db.GardenTasks
            .FirstOrDefaultAsync(
                t => t.Id == request.TaskId && t.GardenId == request.GardenId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found.");

        if (task.CompletedAt.HasValue)
            throw new InvalidOperationException("Task is already completed.");

        task.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        string? assigneeName = null;
        if (task.AssignedToUserId.HasValue)
        {
            var assignee = await db.Users.FindAsync([task.AssignedToUserId.Value], cancellationToken);
            assigneeName = assignee?.DisplayName;
        }

        return CreateGardenTaskHandler.ToDto(task, assigneeName);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CompleteGardenTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/tasks/{taskId:guid}/complete", async (
            Guid gardenId,
            Guid taskId,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(
                    new CompleteGardenTaskCommand(userId, gardenId, taskId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("CompleteGardenTask");
    }
}
