using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class UserSettings
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal? LocationLatitude { get; set; }
    public decimal? LocationLongitude { get; set; }
    public string PreferredLanguage { get; set; } = "en-US";
    public TemperatureUnit TemperatureUnit { get; set; } = TemperatureUnit.Fahrenheit;
    public LengthUnit LengthUnit { get; set; } = LengthUnit.Inches;
    public WeightUnit WeightUnit { get; set; } = WeightUnit.Pounds;
    public VolumeUnit VolumeUnit { get; set; } = VolumeUnit.Gallons;
    public string? UsdaHardinessZone { get; set; }
    public DateOnly? AverageFrostDateSpring { get; set; }
    public DateOnly? AverageFrostDateFall { get; set; }
    public bool ShareWeatherData { get; set; }
    public bool SharePlantingData { get; set; }
    public bool ShareHarvestData { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
