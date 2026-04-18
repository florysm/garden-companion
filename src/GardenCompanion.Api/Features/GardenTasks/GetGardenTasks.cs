using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request ──────────────────────────────────────────────────────────────────

public record GetGardenTasksQuery(
    Guid CurrentUserId,
    Guid GardenId,
    Guid? GardenBedId,
    Guid? PlantingId,
    bool? IsCompleted,
    bool? IsOverdue,
    TaskType? TaskType) : IRequest<List<GardenTaskDto>>;

// ── Handler ──────────────────────────────────────────────────────────────────

public class GetGardenTasksHandler(AppDbContext db)
    : IRequestHandler<GetGardenTasksQuery, List<GardenTaskDto>>
{
    public async Task<List<GardenTaskDto>> Handle(
        GetGardenTasksQuery request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var query = db.GardenTasks
            .Where(t => t.GardenId == request.GardenId);

        if (request.GardenBedId.HasValue)
            query = query.Where(t => t.GardenBedId == request.GardenBedId.Value);

        if (request.PlantingId.HasValue)
            query = query.Where(t => t.PlantingId == request.PlantingId.Value);

        if (request.IsCompleted.HasValue)
            query = request.IsCompleted.Value
                ? query.Where(t => t.CompletedAt != null)
                : query.Where(t => t.CompletedAt == null);

        if (request.IsOverdue == true)
            query = query.Where(t => t.CompletedAt == null
                                  && t.DueDate != null
                                  && t.DueDate < today);

        if (request.TaskType.HasValue)
            query = query.Where(t => t.TaskType == request.TaskType.Value);

        var tasks = await query
            .OrderBy(t => t.CompletedAt == null ? 0 : 1)
            .ThenBy(t => t.DueDate == null ? DateOnly.MaxValue : t.DueDate)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new
            {
                Task = t,
                AssigneeName = t.AssignedTo != null ? t.AssignedTo.DisplayName : null
            })
            .ToListAsync(cancellationToken);

        return tasks.Select(x => CreateGardenTaskHandler.ToDto(x.Task, x.AssigneeName)).ToList();
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class GetGardenTasksEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/gardens/{gardenId:guid}/tasks", async (
            Guid gardenId,
            Guid? gardenBedId,
            Guid? plantingId,
            bool? isCompleted,
            bool? isOverdue,
            string? taskType,
            HttpContext ctx,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            TaskType? parsedType = null;
            if (!string.IsNullOrEmpty(taskType))
            {
                if (!Enum.TryParse<TaskType>(taskType, true, out var t))
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                        { ["taskType"] = [$"Invalid task type '{taskType}'."] });
                parsedType = t;
            }

            try
            {
                var result = await mediator.Send(
                    new GetGardenTasksQuery(userId, gardenId, gardenBedId, plantingId, isCompleted, isOverdue, parsedType), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("GetGardenTasks");
    }
}
