using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request / Response ───────────────────────────────────────────────────────

public record UpdatePlantingCommand(
    Guid PlantingId,
    Guid UserId,
    DateOnly? ExpectedHarvestDate,
    PlantingType PlantingType,
    int Quantity,
    int SeasonYear,
    SeasonType SeasonType) : IRequest<PlantingDetailDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record UpdatePlantingBody(
    DateOnly? ExpectedHarvestDate,
    PlantingType PlantingType,
    int Quantity,
    int SeasonYear,
    SeasonType SeasonType);

public class UpdatePlantingValidator : AbstractValidator<UpdatePlantingBody>
{
    public UpdatePlantingValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(10000);
        RuleFor(x => x.SeasonYear).InclusiveBetween(2000, 2100);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class UpdatePlantingHandler(AppDbContext db)
    : IRequestHandler<UpdatePlantingCommand, PlantingDetailDto>
{
    public async Task<PlantingDetailDto> Handle(
        UpdatePlantingCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequirePlantingMemberAsync(db, request.PlantingId, request.UserId, cancellationToken);

        var planting = await db.Plantings
            .Include(p => p.GardenBed)
            .Include(p => p.Plant)
            .FirstOrDefaultAsync(p => p.Id == request.PlantingId, cancellationToken)
            ?? throw new KeyNotFoundException($"Planting {request.PlantingId} not found.");

        planting.ExpectedHarvestDate = request.ExpectedHarvestDate;
        planting.PlantingType = request.PlantingType;
        planting.Quantity = request.Quantity;
        planting.SeasonYear = request.SeasonYear;
        planting.SeasonType = request.SeasonType;

        await db.SaveChangesAsync(cancellationToken);

        return await db.Plantings
            .Where(p => p.Id == planting.Id)
            .Select(p => new PlantingDetailDto(
                p.Id,
                p.GardenBedId,
                p.GardenBed.Name,
                p.GardenBed.GardenId,
                p.PlantId,
                p.Plant.CommonName,
                p.Plant.ScientificName,
                p.Plant.Family,
                p.PlantedDate,
                p.ExpectedHarvestDate,
                p.ActualEndDate,
                p.Status,
                p.PlantingType,
                p.Quantity,
                p.SeasonYear,
                p.SeasonType,
                p.IsActive,
                p.Observations.Count,
                p.HarvestLogs.Count))
            .FirstAsync(cancellationToken);
    }
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class UpdatePlantingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/plantings/{id:guid}", async (
            Guid id,
            UpdatePlantingBody body,
            IValidator<UpdatePlantingBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new UpdatePlantingCommand(
                id, userId, body.ExpectedHarvestDate, body.PlantingType,
                body.Quantity, body.SeasonYear, body.SeasonType);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Ok(result);
            }
            catch (KeyNotFoundException)
            {
                return Results.NotFound();
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("UpdatePlanting");
    }
}
