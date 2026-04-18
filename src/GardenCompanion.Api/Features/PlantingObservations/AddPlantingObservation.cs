using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;

namespace GardenCompanion.Api.Features.PlantingObservations;

// ── DTOs ─────────────────────────────────────────────────────────────────────

public record PlantingObservationDto(
    Guid Id,
    Guid PlantingId,
    ObservationType ObservationType,
    string Note,
    DateTime ObservedAt);

// ── Request / Response ───────────────────────────────────────────────────────

public record AddPlantingObservationCommand(
    Guid PlantingId,
    Guid UserId,
    ObservationType ObservationType,
    string Note,
    DateTime? ObservedAt) : IRequest<PlantingObservationDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record AddPlantingObservationBody(
    ObservationType ObservationType,
    string Note,
    DateTime? ObservedAt);

public class AddPlantingObservationValidator : AbstractValidator<AddPlantingObservationBody>
{
    public AddPlantingObservationValidator()
    {
        RuleFor(x => x.ObservationType).IsInEnum();
        RuleFor(x => x.Note).NotEmpty().MaximumLength(2000);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class AddPlantingObservationHandler(AppDbContext db)
    : IRequestHandler<AddPlantingObservationCommand, PlantingObservationDto>
{
    public async Task<PlantingObservationDto> Handle(
        AddPlantingObservationCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var observation = new PlantingObservation
        {
            Id = Guid.NewGuid(),
            PlantingId = request.PlantingId,
            ObservationType = request.ObservationType,
            Note = request.Note,
            ObservedAt = request.ObservedAt ?? DateTime.UtcNow
        };

        db.PlantingObservations.Add(observation);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantingObservationDto(
            observation.Id,
            observation.PlantingId,
            observation.ObservationType,
            observation.Note,
            observation.ObservedAt);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class AddPlantingObservationEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/plantings/{plantingId:guid}/observations", async (
            Guid plantingId,
            AddPlantingObservationBody body,
            IValidator<AddPlantingObservationBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new AddPlantingObservationCommand(
                plantingId, userId, body.ObservationType, body.Note, body.ObservedAt);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/plantings/{plantingId}/observations/{result.Id}", result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("PlantingObservations")
        .WithName("AddPlantingObservation");
    }
}
