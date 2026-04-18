using GardenCompanion.Api.Domain.Enums;

namespace GardenCompanion.Api.Domain.Entities;

public class WeatherStationIntegration
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public WeatherProvider Provider { get; set; }
    public string? StationId { get; set; }
    /// <summary>API key encrypted at application layer before persistence.</summary>
    public string? ApiKey { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public Household Household { get; set; } = null!;
}
