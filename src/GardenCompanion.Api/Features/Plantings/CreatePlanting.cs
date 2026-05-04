using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request / Response ───────────────────────────────────────────────────────

public record CreatePlantingCommand(
    Guid GardenId,
    Guid UserId,
    Guid GardenBedId,
    Guid PlantId,
    DateOnly PlantedDate,
    DateOnly? ExpectedHarvestDate,
    PlantingType PlantingType,
    PlantingSource Source,
    int Quantity,
    int? SeasonYear,
    SeasonType? SeasonType) : IRequest<PlantingDetailDto>;

// ── Validator ────────────────────────────────────────────────────────────────

public record CreatePlantingBody(
    Guid GardenBedId,
    Guid PlantId,
    DateOnly PlantedDate,
    DateOnly? ExpectedHarvestDate,
    PlantingType PlantingType,
    PlantingSource Source,
    int Quantity,
    int? SeasonYear,
    SeasonType? SeasonType);

public class CreatePlantingValidator : AbstractValidator<CreatePlantingBody>
{
    public CreatePlantingValidator()
    {
        RuleFor(x => x.GardenBedId).NotEmpty();
        RuleFor(x => x.PlantId).NotEmpty();
        RuleFor(x => x.PlantedDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(10000);
        RuleFor(x => x.SeasonYear).InclusiveBetween(2000, 2100).When(x => x.SeasonYear.HasValue);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreatePlantingHandler(AppDbContext db)
    : IRequestHandler<CreatePlantingCommand, PlantingDetailDto>
{
    public async Task<PlantingDetailDto> Handle(
        CreatePlantingCommand request, CancellationToken cancellationToken)
    {
        await GardenAccess.RequireMemberAsync(db, request.GardenId, request.UserId, cancellationToken);

        // Verify bed belongs to the requested garden
        var bed = await db.GardenBeds
            .Where(b => b.Id == request.GardenBedId && b.GardenId == request.GardenId)
            .Select(b => new { b.Id, b.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Garden bed {request.GardenBedId} not found in garden {request.GardenId}.");

        var plant = await db.Plants
            .Where(p => p.Id == request.PlantId
                && ((p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException($"Plant {request.PlantId} not found.");

        var plantedDate = request.PlantedDate;
        var seasonYear = request.SeasonYear ?? plantedDate.Year;
        var seasonType = request.SeasonType ?? InferSeason(plantedDate.Month);

        DateOnly? expectedHarvest = request.ExpectedHarvestDate;
        if (expectedHarvest is null && plant.DaysToMaturity.HasValue)
            expectedHarvest = plantedDate.AddDays(plant.DaysToMaturity.Value);

        var planting = new Planting
        {
            Id = Guid.NewGuid(),
            GardenBedId = request.GardenBedId,
            PlantId = plant.Id,
            PlantedDate = plantedDate,
            ExpectedHarvestDate = expectedHarvest,
            PlantingType = request.PlantingType,
            Source = request.Source,
            Quantity = request.Quantity,
            SeasonYear = seasonYear,
            SeasonType = seasonType,
            Status = PlantingStatus.Planted,
            IsActive = true
        };

        db.Plantings.Add(planting);
        await db.SaveChangesAsync(cancellationToken);

        return new PlantingDetailDto(
            planting.Id,
            planting.GardenBedId,
            bed.Name,
            request.GardenId,
            plant.Id,
            plant.CommonName,
            plant.ScientificName,
            plant.Family,
            planting.PlantedDate,
            planting.ExpectedHarvestDate,
            planting.ActualEndDate,
            planting.Status,
            planting.PlantingType,
            planting.Source,
            planting.Quantity,
            planting.SeasonYear,
            planting.SeasonType,
            planting.IsActive,
            ObservationCount: 0,
            HarvestCount: 0);
    }

    private static SeasonType InferSeason(int month) => month switch
    {
        3 or 4 or 5 => SeasonType.Spring,
        6 or 7 or 8 => SeasonType.Summer,
        9 or 10 or 11 => SeasonType.Fall,
        _ => SeasonType.Winter
    };
}

// ── Endpoint ─────────────────────────────────────────────────────────────────

public static class CreatePlantingEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/gardens/{gardenId:guid}/plantings", async (
            Guid gardenId,
            CreatePlantingBody body,
            IValidator<CreatePlantingBody> validator,
            IMediator mediator,
            HttpContext ctx,
            CancellationToken ct) =>
        {
            var validation = await validator.ValidateAsync(body, ct);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var userId = ctx.User.GetUserId();
            var command = new CreatePlantingCommand(
                gardenId,
                userId,
                body.GardenBedId,
                body.PlantId,
                body.PlantedDate,
                body.ExpectedHarvestDate,
                body.PlantingType,
                body.Source,
                body.Quantity,
                body.SeasonYear,
                body.SeasonType);

            try
            {
                var result = await mediator.Send(command, ct);
                return Results.Created($"/api/plantings/{result.Id}", result);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        })
        .RequireAuthorization()
        .WithTags("Plantings")
        .WithName("CreatePlanting");
    }
}
