namespace GardenCompanion.Api.Features.GardenBeds;

public record GardenBedDetailDto(
    Guid Id,
    Guid GardenId,
    string Name,
    string Type,
    string Shape,
    decimal? LengthFeet,
    decimal? WidthFeet,
    decimal? DiameterFeet,
    decimal? DepthInches,
    decimal? VolumeGallons,
    string? SoilType,
    string SunExposure,
    string? Notes,
    int ActivePlantingCount);
