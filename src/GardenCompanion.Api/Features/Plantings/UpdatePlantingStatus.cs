using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpdatePlantingStatusCommand(
    Guid PlantingId,
    Guid UserId,
    PlantingStatus Status) : IRequest<PlantingSummaryDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpdatePlantingStatusBody(PlantingStatus Status);

public class UpdatePlantingStatusValidator : AbstractValidator<UpdatePlantingStatusBody>
{
    public UpdatePlantingStatusValidator()
    {
        RuleFor(x => x.Status).IsInEnum();
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdatePlantingStatusHandler(AppDbContext db)
    : IRequestHandler<UpdatePlantingStatusCommand, PlantingSummaryDto>
{
    private static readonly HashSet<PlantingStatus> TerminalStatuses =
        [PlantingStatus.Harvested, PlantingStatus.Failed];

    public async Task<PlantingSummaryDto> Handle(
        UpdatePlantingStatusCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var planting = await db.Plantings
            .Include(p => p.GardenBed)
            .Include(p => p.Plant)
            .FirstOrDefaultAsync(p => p.Id == request.PlantingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Planting {request.PlantingId} not found.");

        planting.Status = request.Status;

        if (TerminalStatuses.Contains(request.Status))
        {
            planting.IsActive = false;
            planting.ActualEndDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new PlantingSummaryDto(
            planting.Id,
            planting.GardenBedId,
            planting.GardenBed.Name,
            planting.PlantId,
            planting.Plant.CommonName,
            planting.PlantedDate,
            planting.ExpectedHarvestDate,
            planting.ActualEndDate,
            planting.Status,
            planting.PlantingType,
            planting.Quantity,
            planting.SeasonYear,
            planting.SeasonType,
            planting.IsActive);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdatePlantingStatusEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPatch("/plantings/{id:guid}/status", async (
            Guid id,
            UpdatePlantingStatusBody body,
            IValidator<UpdatePlantingStatusBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            try
            {
                var result = await mediator.Send(new UpdatePlantingStatusCommand(id, userId, body.Status), ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("UpdatePlantingStatus");
    }
}
