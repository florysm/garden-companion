using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Features.Plants;

public record PlantSummaryDto(
    Guid Id,
    string CommonName,
    string? ScientificName,
    string? Family,
    int? DaysToMaturity,
    bool IsGlobal,
    bool IsApproved,
    ExternalSource ExternalSource);

public record PlantDetailDto(
    Guid Id,
    string CommonName,
    string? ScientificName,
    string? Description,
    string? Family,
    int? DaysToMaturity,
    decimal? MinSpacingInches,
    decimal? MinDepthInches,
    string? SunRequirement,
    string? WaterRequirement,
    bool IsGlobal,
    bool IsApproved,
    ExternalSource ExternalSource,
    string? ExternalId,
    Guid? ContributedByUserId,
    DateTime? CachedAt);
