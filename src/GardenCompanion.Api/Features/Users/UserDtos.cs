using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Features.Users;

public record UserProfileDto(
    Guid Id,
    string Email,
    string DisplayName,
    DateTime CreatedAt);

public record UserSettingsDto(
    Guid Id,
    decimal? LocationLatitude,
    decimal? LocationLongitude,
    string PreferredLanguage,
    TemperatureUnit TemperatureUnit,
    LengthUnit LengthUnit,
    WeightUnit WeightUnit,
    VolumeUnit VolumeUnit,
    string? UsdaHardinessZone,
    DateOnly? AverageFrostDateSpring,
    DateOnly? AverageFrostDateFall,
    bool ShareWeatherData,
    bool SharePlantingData,
    bool ShareHarvestData);
