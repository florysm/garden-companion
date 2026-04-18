using GardenCompanion.Api.Common;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetGardenTaskQuery(Guid TaskId, Guid GardenId, Guid CurrentUserId) : IRequest<GardenTaskDto>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardenTaskHandler(AppDbContext db)
    : IRequestHandler<GetGardenTaskQuery, GardenTaskDto>
{
    public async Task<GardenTaskDto> Handle(
        GetGardenTaskQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var row = await db.GardenTasks
            .Where(t => t.Id == request.TaskId && t.GardenId == request.GardenId)
            .Select(t => new { Task = t, AssigneeName = t.AssignedTo != null ? t.AssignedTo.DisplayName : null })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Garden task {request.TaskId} not found.");

        return CreateGardenTaskHandler.ToDto(row.Task, row.AssigneeName);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardenTaskEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/tasks/{taskId:guid}", async (
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
                    new GetGardenTaskQuery(taskId, gardenId, userId), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("GetGardenTask");
    }
}
