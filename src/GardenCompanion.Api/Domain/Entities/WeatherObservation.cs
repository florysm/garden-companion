using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class WeatherObservation
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public DateTime ObservedAt { get; set; }
    public decimal TemperatureF { get; set; }
    public decimal Humidity { get; set; }
    public decimal WindSpeedMph { get; set; }
    public int? WindDirectionDegrees { get; set; }
    public decimal PrecipitationRateInPerHr { get; set; }
    public decimal PrecipitationTotalIn { get; set; }
    public decimal? UvIndex { get; set; }
    public decimal? DewPointF { get; set; }
    public decimal? PressureInHg { get; set; }
    public WeatherProvider Source { get; set; }
    public string? StationId { get; set; }

    // Navigation
    public Household Household { get; set; } = null!;
}
