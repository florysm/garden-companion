using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.GardenTasks;

// ── Request / Response ──────────────────────────────────────────────────────

public record CreateGardenTaskCommand(
    Guid CurrentUserId,
    Guid GardenId,
    Guid? GardenBedId,
    Guid? PlantingId,
    Guid? AssignedToUserId,
    string Title,
    string? Description,
    TaskType TaskType,
    DateOnly? DueDate) : IRequest<GardenTaskDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public class CreateGardenTaskValidator : AbstractValidator<CreateGardenTaskCommand>
{
    public CreateGardenTaskValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreateGardenTaskHandler(AppDbContext db)
    : IRequestHandler<CreateGardenTaskCommand, GardenTaskDto>
{
    public async Task<GardenTaskDto> Handle(
        CreateGardenTaskCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(
            db, request.GardenId, request.CurrentUserId, cancellationToken);

        await ValidateForeignKeysAsync(db, request.GardenId, request.GardenBedId, request.PlantingId, request.AssignedToUserId, cancellationToken);

        var now = DateTime.UtcNow;

        var task = new GardenTask
        {
            Id = Guid.NewGuid(),
            GardenId = request.GardenId,
            GardenBedId = request.GardenBedId,
            PlantingId = request.PlantingId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title,
            Description = request.Description,
            TaskType = request.TaskType,
            DueDate = request.DueDate,
            CreatedAt = now
        };

        db.GardenTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        string? assigneeName = null;
        if (request.AssignedToUserId.HasValue)
        {
            var assignee = await db.Users.FindAsync([request.AssignedToUserId.Value], cancellationToken);
            assigneeName = assignee?.DisplayName;
        }

        return ToDto(task, assigneeName);
    }

    internal static async Task ValidateForeignKeysAsync(
        AppDbContext db, Guid gardenId, Guid? gardenBedId, Guid? plantingId, Guid? assignedToUserId, CancellationToken ct)
    {
        if (gardenBedId.HasValue)
        {
            var bedExists = await db.GardenBeds
                .AnyAsync(b => b.Id == gardenBedId.Value && b.GardenId == gardenId, ct);
            if (!bedExists)
                throw new KeyNotFoundException($"Garden bed {gardenBedId} not found in garden {gardenId}.");
        }

        if (plantingId.HasValue)
        {
            var plantingExists = await db.Plantings
                .AnyAsync(p => p.Id == plantingId.Value && p.GardenBed.GardenId == gardenId, ct);
            if (!plantingExists)
                throw new KeyNotFoundException($"Planting {plantingId} not found in garden {gardenId}.");
        }

        if (assignedToUserId.HasValue)
        {
            var role = await GardenAccess.GetRoleAsync(db, gardenId, assignedToUserId.Value, ct);
            if (role is null)
                throw new KeyNotFoundException($"User {assignedToUserId} is not a member of garden {gardenId}.");
        }
    }

    internal static GardenTaskDto ToDto(GardenTask t, string? assigneeName)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new GardenTaskDto(
            t.Id, t.GardenId, t.GardenBedId, t.PlantingId,
            t.AssignedToUserId, assigneeName,
            t.Title, t.Description, t.TaskType.ToString(),
            t.DueDate,
            IsOverdue: t.CompletedAt == null && t.DueDate.HasValue && t.DueDate.Value < today,
            t.CompletedAt, t.CreatedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreateGardenTaskEndpoint
{
    public record Request(
        Guid? GardenBedId,
        Guid? PlantingId,
        Guid? AssignedToUserId,
        string Title,
        string? Description,
        string TaskType,
        DateOnly? DueDate);

    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/tasks", async (
            Guid gardenId,
            Request req,
            HttpContext ctx,
            IValidator<CreateGardenTaskCommand> validator,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var userId = ctx.User.GetUserId();

            if (!Enum.TryParse<TaskType>(req.TaskType, true, out var taskType))
                return Results.ValidationProblem(new Dictionary<string, string[]>
                    { ["taskType"] = [$"Invalid task type '{req.TaskType}'."] });

            var command = new CreateGardenTaskCommand(
                userId, gardenId, req.GardenBedId, req.PlantingId,
                req.AssignedToUserId, req.Title, req.Description, taskType, req.DueDate);

            var validation = await validator.ValidateAsync(command, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/gardens/{gardenId}/tasks/{result.Id}", result);
            }
            catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
        })
        .RequireAuthorization()
        .WithTags("GardenTasks")
        .WithName("CreateGardenTask");
    }
}
