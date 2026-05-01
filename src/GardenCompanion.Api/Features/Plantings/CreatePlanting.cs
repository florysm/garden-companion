using FluentValidation;
using GardenCompanion.Api.Common;
using GardenCompanion.Api.Domain.Entities;
using GardenCompanion.Api.Domain.Enums;
using GardenCompanion.Api.Infrastructure.Data;
using GardenCompanion.Api.Infrastructure.ExternalData;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GardenCompanion.Api.Features.Plantings;

// ── Request / Response ───────────────────────────────────────────────────────

public record CreatePlantingCommand(
    Guid GardenId,
    Guid UserId,
    Guid GardenBedId,
    Guid? PlantId,
    string? ExternalPlantId,
    ExternalSource? ExternalPlantSource,
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
    Guid? PlantId,
    string? ExternalPlantId,
    ExternalSource? ExternalPlantSource,
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
        RuleFor(x => x)
            .Must(x => (x.PlantId.HasValue && x.PlantId != Guid.Empty) || !string.IsNullOrWhiteSpace(x.ExternalPlantId))
            .WithName("PlantId")
            .WithMessage("Either PlantId or ExternalPlantId must be provided.");
        RuleFor(x => x.ExternalPlantSource)
            .NotNull()
            .When(x => !string.IsNullOrWhiteSpace(x.ExternalPlantId))
            .WithMessage("ExternalPlantSource is required when ExternalPlantId is provided.");
        RuleFor(x => x.PlantedDate).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).LessThanOrEqualTo(10000);
        RuleFor(x => x.SeasonYear).InclusiveBetween(2000, 2100).When(x => x.SeasonYear.HasValue);
    }
}

// ── Handler ──────────────────────────────────────────────────────────────────

public class CreatePlantingHandler(AppDbContext db, IPlantDataService plantDataService)
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

        // Resolve plant — from local DB or by importing an external scraped result
        var plant = await ResolveOrImportPlantAsync(request, cancellationToken);

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

    private async Task<Plant> ResolveOrImportPlantAsync(
        CreatePlantingCommand request, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(request.ExternalPlantId))
        {
            // Check if the plant was already imported by a previous request
            var existing = await db.Plants
                .Where(p => p.ExternalId == request.ExternalPlantId)
                .FirstOrDefaultAsync(ct);

            if (existing is not null)
                return existing;

            // Scrape and store the plant data now that the user has chosen this plant
            var ext = await plantDataService.GetAsync(request.ExternalPlantId, ct)
                ?? throw new KeyNotFoundException($"Plant data not found for external ID '{request.ExternalPlantId}'.");

            var imported = new Plant
            {
                Id = Guid.NewGuid(),
                ExternalId = ext.ExternalId,
                ExternalSource = request.ExternalPlantSource ?? ExternalSource.Scraped,
                CommonName = ext.CommonName,
                ScientificName = ext.ScientificName,
                Description = ext.Description,
                MinSpacingInches = ext.MinSpacingInches,
                SunRequirement = ext.SunRequirement,
                DaysToMaturity = ext.DaysToMaturity,
                HeatLevelShu = ext.HeatLevelShu,
                WaterRequirement = ext.WaterRequirement,
                MinDepthInches = ext.MinDepthInches,
                Family = ext.Family,
                FruitSizeDescription = ext.FruitSizeDescription,
                DiseaseResistanceNotes = ext.DiseaseResistanceNotes,
                Aliases = ext.Aliases,
                IsGlobal = true,
                IsApproved = true,
                CachedAt = DateTime.UtcNow,
            };
            db.Plants.Add(imported);
            return imported;
        }

        return await db.Plants
            .Where(p => p.Id == request.PlantId
                && ((p.IsGlobal && p.IsApproved) || p.ContributedByUserId == request.UserId))
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"Plant {request.PlantId} not found.");
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
                body.ExternalPlantId,
                body.ExternalPlantSource,
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
