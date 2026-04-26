using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Features.Plantings;

public record PlantingSummaryDto(
    Guid Id,
    Guid GardenBedId,
    string GardenBedName,
    Guid PlantId,
    string PlantCommonName,
    DateOnly PlantedDate,
    DateOnly? ExpectedHarvestDate,
    DateOnly? ActualEndDate,
    PlantingStatus Status,
    PlantingType PlantingType,
    PlantingSource Source,
    int Quantity,
    int SeasonYear,
    SeasonType SeasonType,
    bool IsActive);

public record PlantingDetailDto(
    Guid Id,
    Guid GardenBedId,
    string GardenBedName,
    Guid GardenId,
    Guid PlantId,
    string PlantCommonName,
    string? PlantScientificName,
    string? PlantFamily,
    DateOnly PlantedDate,
    DateOnly? ExpectedHarvestDate,
    DateOnly? ActualEndDate,
    PlantingStatus Status,
    PlantingType PlantingType,
    PlantingSource Source,
    int Quantity,
    int SeasonYear,
    SeasonType SeasonType,
    bool IsActive,
    int ObservationCount,
    int HarvestCount);
