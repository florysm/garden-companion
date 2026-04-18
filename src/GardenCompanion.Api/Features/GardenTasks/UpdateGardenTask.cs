using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request / Response ──────────────────────────────────────────────────────

public record UpdateGardenTaskCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid TaskId,
    string Title,
    string? Description,
    TaskType TaskType,
    DateOnly? DueDate,
    Guid? GardenBedId,
    Guid? AssignedToUserId) : IRequest<GardenTaskDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class UpdateGardenTaskValidator : AbstractValidator<UpdateGardenTaskCommand>
{
    public UpdateGardenTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdateGardenTaskHandler(AppDbContext db)
    : IRequestHandler<UpdateGardenTaskCommand, GardenTaskDto>
{
    public async Task<GardenTaskDto> Handle(
        UpdateGardenTaskCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        var task = await db.GardenTasks
            .FirstOrDefaultAsync(
                t => t.Id == request.TaskId && t.GardenId == request.GardenId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"Task {request.TaskId} not found.");

        task.Title = request.Title;
        task.Description = request.Description;
        task.TaskType = request.TaskType;
        task.DueDate = request.DueDate;
        task.GardenBedId = request.GardenBedId;
        task.AssignedToUserId = request.AssignedToUserId;

        await db.SaveChangesAsync(cancellationToken);

        string? assigneeName = null;
        if (request.AssignedToUserId.HasValue)
        {
            var assignee = await db.Users.FindAsync([request.AssignedToUserId.Value], cancellationToken);
            assigneeName = assignee?.DisplayName;
        }

        return CreateGardenTaskHandler.ToDto(task, assigneeName);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdateGardenTaskEndpoint
{
    public record Request(
        string Title,
        string? Description,
        string TaskType,
        DateOnly? DueDate,
        Guid? GardenBedId,
        Guid? AssignedToUserId);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/gardens/{gardenId:guid}/tasks/{taskId:guid}", async (
            Guid gardenId,
            Guid taskId,
            Request req,
            HttpContext ctx,
            IValidator<UpdateGardenTaskCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            if (!Enum.TryParse<TaskType>(req.TaskType, true, out var taskType))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["taskType"] = [$"Invalid task type '{req.TaskType}'."] });

            var command = new UpdateGardenTaskCommand(
                userId, gardenId, taskId, req.Title, req.Description,
                taskType, req.DueDate, req.GardenBedId, req.AssignedToUserId);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("UpdateGardenTask");
    }
}
